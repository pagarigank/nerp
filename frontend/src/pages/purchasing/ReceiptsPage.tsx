// <copyright file="ReceiptsPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle, Trash2 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { UomSelect } from '@components/ui/UomSelect'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getReceipts,
  createReceipt,
  postReceipt,
  reverseReceipt,
  newReceiptDefaults,
} from '@api/purchasing'
import { getPurchaseOrders } from '@api/purchasing'
import { getVendors } from '@api/ap'
import { getItems, getWarehouses, getItemUomConversions } from '@api/inventory'
import type { UomConversionDto } from '@/types/inventory'

const lineSchema = z.object({
  lineNumber: z.number(),
  purchaseOrderLineId: z.string().optional(),
  itemId: z.string().optional(),
  description: z.string().min(1, 'Description is required'),
  quantityReceived: z.coerce.number().positive('Qty must be > 0'),
  unitOfMeasure: z.string().min(1, 'UOM is required'),
  lotNumber: z.string().optional(),
  serialNumber: z.string().optional(),
  qualityInspectionRequired: z.boolean().optional(),
  warehouseId: z.string().optional(),
  binLocationId: z.string().optional(),
})

const receiptSchema = z.object({
  receiptNumber: z.string().trim().min(1, 'Receipt # is required'),
  purchaseOrderId: z.string().optional(),
  vendorId: z.string().optional(),
  receivedDate: z.string().min(1, 'Date is required'),
  receivedBy: z.string().optional(),
  packingSlipNumber: z.string().optional(),
  notes: z.string().optional(),
  lines: z.array(lineSchema).min(1, 'At least one line is required'),
})

type Form = z.infer<typeof receiptSchema>

function fieldError(message?: string) { return message ? { error: message } : {} }

function statusBadge(status: string, reversed: boolean) {
  if (reversed) return <Badge variant="neutral" size="sm" dot>Reversed</Badge>
  const s = status.toLowerCase()
  if (s.includes('post')) return <Badge variant="success" size="sm" dot>{status}</Badge>
  if (s.includes('draft')) return <Badge variant="warning" size="sm" dot>{status}</Badge>
  return <Badge variant="neutral" size="sm" dot>{status}</Badge>
}

export function ReceiptsPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [reverseId, setReverseId] = useState<string | null>(null)
  const [reverseReason, setReverseReason] = useState('')
  const [lineUomOptions, setLineUomOptions] = useState<Record<number, { value: string; label: string }[]>>({})

  const defaults = useMemo(() => newReceiptDefaults(), [open])

  const { register, handleSubmit, reset, watch, control, setValue, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(receiptSchema),
    defaultValues: {
      ...defaults,
      lines: [{ lineNumber: 1, description: '', quantityReceived: 1, unitOfMeasure: 'EA', qualityInspectionRequired: false }],
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })
  const watchedLines = watch('lines')

  // Lookups
  const { data: rows = [], isLoading } = useQuery({ queryKey: ['purchasing', 'receipts'], queryFn: () => getReceipts() })
  const { data: pos = [] } = useQuery({ queryKey: ['purchasing', 'purchase-orders'], queryFn: () => getPurchaseOrders() })
  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })

  const poOptions = useMemo(() => [
    { value: '', label: 'None (standalone)' },
    ...pos.map((p: any) => ({ value: p.id, label: `${p.poNumber} - ${p.status}` })),
  ], [pos])

  const vendorOptions = useMemo(() => vendors.map((v: any) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })), [vendors])
  const itemOptions = useMemo(() => items.map((i: any) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])
  const warehouseOptions = useMemo(() => warehouses.map((w: any) => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` })), [warehouses])

  const selectedPO = useMemo(() => pos.find((p: any) => p.id === watch('purchaseOrderId')), [pos, watch('purchaseOrderId')])

  // When PO is selected, auto-fill vendor and populate lines from PO
  const handlePOChange = (poId: string) => {
    setValue('purchaseOrderId', poId || undefined)
    if (poId) {
      const po = pos.find((p: any) => p.id === poId)
      if (po) {
        setValue('vendorId', po.vendorId || '')
      }
    }
  }

  function addLine() {
    const nextNum = (watchedLines?.length ?? 0) + 1
    append({ lineNumber: nextNum, description: '', quantityReceived: 1, unitOfMeasure: 'EA', qualityInspectionRequired: false })
  }

  const createMutation = useMutation({
    mutationFn: (d: Form) => createReceipt({
      companyId: defaults.companyId,
      receiptNumber: d.receiptNumber,
      purchaseOrderId: d.purchaseOrderId || null,
      vendorId: d.vendorId || null,
      receivedDate: d.receivedDate,
      receivedBy: d.receivedBy || null,
      packingSlipNumber: d.packingSlipNumber || null,
      notes: d.notes || null,
      lines: d.lines.map((l, i) => ({
        lineNumber: i + 1,
        purchaseOrderLineId: l.purchaseOrderLineId || null,
        itemId: l.itemId || null,
        description: l.description,
        quantityReceived: l.quantityReceived,
        unitOfMeasure: l.unitOfMeasure,
        lotNumber: l.lotNumber || null,
        serialNumber: l.serialNumber || null,
        qualityInspectionRequired: l.qualityInspectionRequired ?? false,
        warehouseId: l.warehouseId || null,
        binLocationId: l.binLocationId || null,
      })),
    }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'receipts'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const postMut = useMutation({ mutationFn: (id: string) => postReceipt(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['purchasing', 'receipts'] }), onError: e => setFormError(getErrorMessage(e)) })
  const revMut = useMutation({ mutationFn: (id: string) => reverseReceipt(id, reverseReason), onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'receipts'] }); setReverseId(null); setReverseReason('') }, onError: e => setFormError(getErrorMessage(e)) })

  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => {
    setFormError(null)
    reset({
      ...defaults,
      lines: [{ lineNumber: 1, description: '', quantityReceived: 1, unitOfMeasure: 'EA', qualityInspectionRequired: false }],
    })
    setOpen(true)
  }
  const onSubmit = (d: Form) => { setFormError(null); createMutation.mutate(d) }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter((r: any) => r.receiptNumber.toLowerCase().includes(q))
  }, [rows, search])

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Goods Receipts" description={`${rows.length} receipt(s)`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New Receipt</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search..." aria-label="Search receipts" leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No receipts yet.'}</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Receipt #</th>
                  <th className="px-3 py-2 font-medium text-gray-500">PO</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Received</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Packing Slip</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map((r: any) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.receiptNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.purchaseOrderId ? r.purchaseOrderId.slice(0, 8) + '…' : '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.vendorId ? r.vendorId.slice(0, 8) + '…' : '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.receivedDate).toLocaleDateString()}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.packingSlipNumber ?? '—'}</td>
                      <td className="px-3 py-3">{statusBadge(r.status, r.isReversed)}</td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          {!r.isReversed && r.status === 'Draft' && <Button size="sm" variant="primary" disabled={postMut.isPending} onClick={() => postMut.mutate(r.id)}>Post</Button>}
                          {!r.isReversed && r.status === 'Posted' && <Button size="sm" variant="ghost" className="text-red-600" disabled={revMut.isPending || reverseId === r.id} onClick={() => setReverseId(r.id)}>Reverse</Button>}
                        </div>
                        {reverseId === r.id && (
                          <div className="mt-2 flex gap-1 justify-end">
                            <Input value={reverseReason} onChange={e => setReverseReason(e.target.value)} placeholder="Reason" className="h-7 text-xs" />
                            <Button size="sm" variant="destructive" onClick={() => revMut.mutate(r.id)}>Confirm</Button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Modal isOpen={open} onClose={close} title="New Goods Receipt" size="lg"
        footer={<><Button variant="secondary" onClick={close} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>Create Receipt</Button></>}>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          {/* Header */}
          <div className="grid grid-cols-2 gap-3">
            <Input {...register('receiptNumber')} label="Receipt #" {...fieldError(errors.receiptNumber?.message)} required />
            <Input {...register('receivedDate')} label="Received Date" type="date" {...fieldError(errors.receivedDate?.message)} required />
            <Select {...register('purchaseOrderId')} label="Purchase Order" options={poOptions} onChange={e => handlePOChange(e.target.value)} />
            <Select {...register('vendorId')} label="Vendor" options={[{ value: '', label: 'Select vendor...' }, ...vendorOptions]} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Input {...register('receivedBy')} label="Received By" placeholder="Name of person receiving" />
            <Input {...register('packingSlipNumber')} label="Packing Slip #" placeholder="Vendor packing slip number" />
          </div>
          <Input {...register('notes')} label="Notes" placeholder="Additional notes about this receipt" />

          {/* Lines */}
          <div className="border-t pt-4 border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-medium text-gray-900 dark:text-white">Receipt Lines</h3>
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
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Lot #</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Serial #</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Warehouse</th>
                    <th className="px-2 py-2 text-center text-xs font-medium uppercase text-gray-500">QC</th>
                    <th className="px-2 py-2 w-8" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {fields.map((field, index) => (
                    <tr key={field.id} className="bg-white dark:bg-gray-900">
                      <td className="px-2 py-1.5 text-sm text-gray-500">{index + 1}</td>
                      <td className="px-2 py-1.5">
                        <select {...register(`lines.${index}.itemId`)} onChange={(e) => {
                          register(`lines.${index}.itemId`).onChange(e)
                          const itemId = e.target.value
                          if (itemId) {
                            const item = items.find((i: any) => i.id === itemId)
                            const baseUom = item?.baseUnitOfMeasure || 'EA'
                            setLineUomOptions(prev => ({ ...prev, [index]: [{ value: baseUom, label: baseUom + ' (base)' }] }))
                            void getItemUomConversions(itemId).then((convs: UomConversionDto[]) => {
                              const opts = [{ value: baseUom, label: baseUom + ' (base)' }]
                              for (const c of convs) { if (c.fromUOM === baseUom) opts.push({ value: c.toUOM, label: `${c.toUOM} (${c.conversionFactor}x)` }) }
                              setLineUomOptions(prev => ({ ...prev, [index]: opts }))
                            }).catch(() => {})
                          }
                        }} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                          <option value="">Select...</option>
                          {itemOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                        </select>
                      </td>
                      <td className="px-2 py-1.5">
                        <input {...register(`lines.${index}.description`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" />
                      </td>
                      <td className="px-2 py-1.5">
                        <input type="number" step="0.01" {...register(`lines.${index}.quantityReceived`)} className="w-20 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" />
                      </td>
                      <td className="px-2 py-1.5">
                        <UomSelect {...register(`lines.${index}.unitOfMeasure`)} className="w-20 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-1 py-1" />
                      </td>
                      <td className="px-2 py-1.5">
                        <input {...register(`lines.${index}.lotNumber`)} className="w-24 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" placeholder="Lot" />
                      </td>
                      <td className="px-2 py-1.5">
                        <input {...register(`lines.${index}.serialNumber`)} className="w-24 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" placeholder="Serial" />
                      </td>
                      <td className="px-2 py-1.5">
                        <select {...register(`lines.${index}.warehouseId`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                          <option value="">Select...</option>
                          {warehouseOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                        </select>
                      </td>
                      <td className="px-2 py-1.5 text-center">
                        <input type="checkbox" {...register(`lines.${index}.qualityInspectionRequired`)} className="h-4 w-4" />
                      </td>
                      <td className="px-2 py-1.5">
                        {fields.length > 1 && (
                          <button type="button" onClick={() => remove(index)} className="text-red-500 hover:text-red-700">
                            <Trash2 className="h-3.5 w-3.5" />
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </form>
      </Modal>
    </div>
  )
}
