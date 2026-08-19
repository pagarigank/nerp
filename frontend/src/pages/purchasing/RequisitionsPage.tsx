// <copyright file="RequisitionsPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle, Check, X, Trash2 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getRequisitions,
  createRequisition,
  submitRequisition,
  approveRequisition,
  rejectRequisition,
  cancelRequisition,
  convertRequisitionToPo,
  newRequisitionDefaults,
} from '@api/purchasing'
import { getVendors } from '@api/ap'
import { getItems } from '@api/inventory'

const lineSchema = z.object({
  lineNumber: z.number(),
  itemId: z.string().optional(),
  description: z.string().min(1, 'Description is required'),
  quantity: z.coerce.number().positive('Qty must be > 0'),
  unitOfMeasure: z.string().min(1, 'UOM is required'),
  estimatedUnitPrice: z.coerce.number().min(0),
  needByDate: z.string().optional(),
  preferredVendorId: z.string().optional(),
})

const reqSchema = z.object({
  requisitionNumber: z.string().trim().min(1, 'Requisition # is required'),
  description: z.string().optional(),
  requestDate: z.string().min(1, 'Request date is required'),
  needByDate: z.string().optional(),
  lines: z.array(lineSchema).min(1, 'At least one line is required'),
})

type Form = z.infer<typeof reqSchema>

function fieldError(message?: string) { return message ? { error: message } : {} }

function statusBadge(status: string) {
  const s = status.toLowerCase()
  if (s.includes('approv')) return <Badge variant="warning" size="sm" dot>{status}</Badge>
  if (s.includes('reject') || s.includes('cancel')) return <Badge variant="error" size="sm" dot>{status}</Badge>
  if (s.includes('convert') || s.includes('order')) return <Badge variant="success" size="sm" dot>{status}</Badge>
  return <Badge variant="neutral" size="sm" dot>{status}</Badge>
}

export function RequisitionsPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [rejectId, setRejectId] = useState<string | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  const defaults = useMemo(() => newRequisitionDefaults(), [open])

  const { register, handleSubmit, reset, watch, control, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(reqSchema),
    defaultValues: {
      ...defaults,
      lines: [{ lineNumber: 1, description: '', quantity: 1, unitOfMeasure: 'EA', estimatedUnitPrice: 0 }],
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })
  const watchedLines = watch('lines')

  // Lookups
  const { data: rows = [], isLoading } = useQuery({ queryKey: ['purchasing', 'requisitions'], queryFn: () => getRequisitions() })
  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })

  const vendorOptions = useMemo(() => vendors.map((v: any) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })), [vendors])
  const itemOptions = useMemo(() => items.map((i: any) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])

  // Running totals
  const lineTotal = useMemo(() => {
    return (watchedLines ?? []).reduce((sum, l) => sum + (l.quantity || 0) * (l.estimatedUnitPrice || 0), 0)
  }, [watchedLines])

  function addLine() {
    const nextNum = (watchedLines?.length ?? 0) + 1
    append({ lineNumber: nextNum, description: '', quantity: 1, unitOfMeasure: 'EA', estimatedUnitPrice: 0 })
  }

  function selectItem(index: number, itemId: string) {
    const item = items.find((i: any) => i.id === itemId)
    if (item) {
      setValue(`lines.${index}.itemId`, itemId)
      if (!watch(`lines.${index}.description`)) {
        setValue(`lines.${index}.description`, item.description)
      }
    }
  }

  const createMutation = useMutation({
    mutationFn: (d: Form) => createRequisition({
      companyId: defaults.companyId,
      requestorId: defaults.requestorId,
      requisitionNumber: d.requisitionNumber,
      requestDate: d.requestDate,
      needByDate: d.needByDate || null,
      description: d.description || null,
      lines: d.lines.map((l, i) => ({
        lineNumber: i + 1,
        itemId: l.itemId || null,
        description: l.description,
        quantity: l.quantity,
        unitOfMeasure: l.unitOfMeasure,
        estimatedUnitPrice: l.estimatedUnitPrice,
        needByDate: l.needByDate || null,
        preferredVendorId: l.preferredVendorId || null,
        accountId: null,
        projectId: null,
        taskId: null,
      })),
    }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'requisitions'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const actionMutation = useMutation({
    mutationFn: (p: { id: string; op: 'submit' | 'approve' | 'cancel' }) => {
      if (p.op === 'submit') return submitRequisition(p.id)
      if (p.op === 'approve') return approveRequisition(p.id)
      return cancelRequisition(p.id)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['purchasing', 'requisitions'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const doReject = (id: string) => {
    setFormError(null)
    rejectRequisition(id, rejectReason).then(() => { qc.invalidateQueries({ queryKey: ['purchasing', 'requisitions'] }); setRejectId(null); setRejectReason('') }).catch(e => setFormError(getErrorMessage(e)))
  }

  const convertMut = useMutation({
    mutationFn: (id: string) => convertRequisitionToPo({ requisitionId: id, preferredVendorId: null }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['purchasing', 'requisitions'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => {
    setFormError(null)
    reset({
      ...defaults,
      lines: [{ lineNumber: 1, description: '', quantity: 1, unitOfMeasure: 'EA', estimatedUnitPrice: 0 }],
    })
    setOpen(true)
  }
  const onSubmit = (d: Form) => { setFormError(null); createMutation.mutate(d) }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter((r: any) => r.requisitionNumber.toLowerCase().includes(q) || (r.description ?? '').toLowerCase().includes(q))
  }, [rows, search])

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Requisitions" description={`${rows.length} requisition(s)`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New Requisition</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search..." aria-label="Search requisitions" leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No requisitions yet.'}</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">#</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Description</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Request Date</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Need By</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Total</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map((r: any) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.requisitionNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.description ?? '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.requestDate).toLocaleDateString()}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.needByDate ? new Date(r.needByDate).toLocaleDateString() : '—'}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${r.totalAmount.toFixed(2)}</td>
                      <td className="px-3 py-3">{statusBadge(r.status)}</td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          {r.status === 'Draft' && <Button size="sm" variant="outline" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: r.id, op: 'submit' })}>Submit</Button>}
                          {r.status === 'PendingApproval' && <><Button size="sm" variant="primary" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: r.id, op: 'approve' })}><Check className="h-3.5 w-3.5" /> Approve</Button>
                            <Button size="sm" variant="ghost" className="text-red-600" disabled={actionMutation.isPending || rejectId === r.id} onClick={() => setRejectId(r.id)}><X className="h-3.5 w-3.5" /></Button></>}
                          {r.status === 'Draft' && <Button size="sm" variant="ghost" className="text-red-600" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: r.id, op: 'cancel' })}>Cancel</Button>}
                          {!['Draft', 'Rejected', 'Cancelled'].includes(r.status) && <Button size="sm" variant="outline" disabled={convertMut.isPending} onClick={() => convertMut.mutate(r.id)}>Convert</Button>}
                        </div>
                        {rejectId === r.id && (
                          <div className="mt-2 flex gap-1 justify-end">
                            <Input value={rejectReason} onChange={e => setRejectReason(e.target.value)} placeholder="Reason" className="h-7 text-xs" />
                            <Button size="sm" variant="destructive" onClick={() => doReject(r.id)}>Reject</Button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Modal isOpen={open} onClose={close} title="New Requisition" size="lg"
        footer={<><Button variant="secondary" onClick={close} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>Create Requisition</Button></>}>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          {/* Header */}
          <div className="grid grid-cols-2 gap-3">
            <Input {...register('requisitionNumber')} label="Requisition #" {...fieldError(errors.requisitionNumber?.message)} required />
            <Input {...register('requestDate')} label="Request Date" type="date" {...fieldError(errors.requestDate?.message)} required />
            <Input {...register('description')} label="Description" placeholder="Brief description of this requisition" />
            <Input {...register('needByDate')} label="Need By Date" type="date" />
          </div>

          {/* Lines */}
          <div className="border-t pt-4 border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-medium text-gray-900 dark:text-white">Requisition Lines</h3>
              <Button type="button" variant="outline" size="sm" onClick={addLine} leftIcon={<Plus className="h-4 w-4" />}>Add Line</Button>
            </div>
            {errors.lines?.message && <p className="text-sm text-red-600 mb-2">{errors.lines.message}</p>}
            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
              <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-800">
                  <tr>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500 w-8">#</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Item</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Description</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">UOM</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Est. Price</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Ext. Total</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Need By</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Vendor</th>
                    <th className="px-2 py-2 w-8" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {fields.map((field, index) => {
                    const line = watchedLines?.[index]
                    const ext = (line?.quantity ?? 0) * (line?.estimatedUnitPrice ?? 0)
                    return (
                      <tr key={field.id} className="bg-white dark:bg-gray-900">
                        <td className="px-2 py-1.5 text-sm text-gray-500">{index + 1}</td>
                        <td className="px-2 py-1.5">
                          <select {...register(`lines.${index}.itemId`)} onChange={(e) => { register(`lines.${index}.itemId`).onChange(e); selectItem(index, e.target.value) }}
                            className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                            <option value="">Select...</option>
                            {itemOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                          </select>
                        </td>
                        <td className="px-2 py-1.5">
                          <input {...register(`lines.${index}.description`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" />
                        </td>
                        <td className="px-2 py-1.5">
                          <input type="number" step="0.01" {...register(`lines.${index}.quantity`)} className="w-20 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" />
                        </td>
                        <td className="px-2 py-1.5">
                          <input {...register(`lines.${index}.unitOfMeasure`)} className="w-16 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" />
                        </td>
                        <td className="px-2 py-1.5">
                          <input type="number" step="0.01" {...register(`lines.${index}.estimatedUnitPrice`)} className="w-24 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" />
                        </td>
                        <td className="px-2 py-1.5 text-right text-sm font-medium tabular-nums">${ext.toFixed(2)}</td>
                        <td className="px-2 py-1.5">
                          <input type="date" {...register(`lines.${index}.needByDate`)} className="w-28 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" />
                        </td>
                        <td className="px-2 py-1.5">
                          <select {...register(`lines.${index}.preferredVendorId`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                            <option value="">Any</option>
                            {vendorOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                          </select>
                        </td>
                        <td className="px-2 py-1.5">
                          {fields.length > 1 && (
                            <button type="button" onClick={() => remove(index)} className="text-red-500 hover:text-red-700">
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
                <tfoot>
                  <tr className="bg-gray-50 dark:bg-gray-800">
                    <td colSpan={6} className="px-2 py-2 text-right text-sm font-medium text-gray-700 dark:text-gray-300">Total:</td>
                    <td className="px-2 py-2 text-right text-sm font-bold tabular-nums">${lineTotal.toFixed(2)}</td>
                    <td colSpan={3} />
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>
        </form>
      </Modal>
    </div>
  )
}


