import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle, Check, Trash2 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getPurchaseOrders,
  createPurchaseOrder,
  approvePurchaseOrder,
  submitPurchaseOrder,
  closePurchaseOrder,
  cancelPurchaseOrder,
  releasePurchaseOrder,
  printPurchaseOrder,
  emailPurchaseOrderToVendor,
  newPurchaseOrderDefaults,
} from '@api/purchasing'
import { getVendors, getPaymentTerms } from '@api/ap'
import { getItems, getItemUomConversions } from '@api/inventory'
import type { PurchaseOrderSummary } from '@/types/purchasing'
import type { Vendor, PaymentTerm } from '@/types/ap'
import type { ItemSummary, UomConversionDto } from '@/types/inventory'

const lineSchema = z.object({
  lineNumber: z.number(),
  itemId: z.string().optional(),
  description: z.string().min(1, 'Description is required'),
  quantity: z.coerce.number().positive('Qty must be > 0'),
  unitOfMeasure: z.string().min(1, 'UOM is required'),
  unitPrice: z.coerce.number().min(0),
  taxCode: z.string().optional(),
  taxRate: z.coerce.number().min(0).optional(),
  needByDate: z.string().optional(),
  accountId: z.string().optional(),
  projectId: z.string().optional(),
  taskId: z.string().optional(),
})

const poSchema = z.object({
  poNumber: z.string().trim().min(1, 'PO # is required'),
  vendorId: z.string().min(1, 'Vendor is required'),
  orderDate: z.string().min(1, 'Order date is required'),
  orderType: z.string().min(1, 'Type is required'),
  shipToName: z.string().optional(),
  shipToAddress: z.string().optional(),
  paymentTermId: z.string().optional(),
  buyerId: z.string().optional(),
  buyerNotes: z.string().optional(),
  vendorReference: z.string().optional(),
  blanketLimit: z.string().optional(),
  freightAmount: z.coerce.number().min(0).optional(),
  freightTaxAmount: z.coerce.number().min(0).optional(),
  taxExempt: z.boolean().optional(),
  lines: z.array(lineSchema).min(1, 'At least one line is required'),
})

type Form = z.infer<typeof poSchema>

const orderTypeOptions = [
  { value: 'Standard', label: 'Standard' },
  { value: 'Blanket', label: 'Blanket' },
  { value: 'Standing', label: 'Standing' },
  { value: 'DropShip', label: 'Drop Ship' },
]

function fieldError(message?: string) { return message ? { error: message } : {} }
function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount)
}

function statusBadge(status: string) {
  const s = status.toLowerCase()
  if (s.includes('approv')) return <Badge variant="warning" size="sm" dot>{status}</Badge>
  if (s.includes('reject') || s.includes('cancel')) return <Badge variant="error" size="sm" dot>{status}</Badge>
  if (s.includes('closed')) return <Badge variant="neutral" size="sm" dot>{status}</Badge>
  if (s.includes('open') || s.includes('draft') || s.includes('order')) return <Badge variant="success" size="sm" dot>{status}</Badge>
  return <Badge variant="neutral" size="sm" dot>{status}</Badge>
}

export function PurchaseOrdersPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [cancelId, setCancelId] = useState<string | null>(null)
  const [cancelReason, setCancelReason] = useState('')
  const [releaseId, setReleaseId] = useState<string | null>(null)
  const [releaseAmount, setReleaseAmount] = useState('')
  const [vendorSearch, setVendorSearch] = useState('')
  const [showVendorDropdown, setShowVendorDropdown] = useState(false)
  const [lineUomOptions, setLineUomOptions] = useState<Record<number, { value: string; label: string }[]>>({})

  const defaults = useMemo(() => newPurchaseOrderDefaults(), [open])

  const { register, handleSubmit, reset, watch, control, setValue, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(poSchema),
    defaultValues: {
      ...defaults,
      lines: [{ lineNumber: 1, description: '', quantity: 1, unitOfMeasure: 'EA', unitPrice: 0 }],
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })
  const watchedLines = watch('lines')
  const watchOrderType = watch('orderType')

  // Lookups
  const { data: rows = [], isLoading } = useQuery({ queryKey: ['purchasing', 'purchase-orders'], queryFn: () => getPurchaseOrders() })
  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: paymentTerms = [] } = useQuery({ queryKey: ['ap', 'paymentTerms'], queryFn: () => getPaymentTerms(true) })

  const filteredVendors = useMemo(() => {
    const q = vendorSearch.trim().toLowerCase()
    if (!q) return vendors.slice(0, 10)
    return vendors.filter(v => v.name.toLowerCase().includes(q) || v.vendorId.toLowerCase().includes(q)).slice(0, 10)
  }, [vendors, vendorSearch])

  const selectedVendor = useMemo(() => vendors.find((v: Vendor) => v.id === watch('vendorId')), [vendors, watch('vendorId')])

  const itemOptions = useMemo(
    () => items.map((i: ItemSummary) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })),
    [items]
  )

  const paymentTermOptions = useMemo(
    () => paymentTerms.map((t: PaymentTerm) => ({ value: t.id, label: `${t.name} (${t.dueDays}d)` })),
    [paymentTerms]
  )

  // Running totals
  const totals = useMemo(() => {
    let subtotal = 0
    let totalTax = 0
    for (const line of watchedLines ?? []) {
      const ext = (line.quantity || 0) * (line.unitPrice || 0)
      const tax = ext * ((line.taxRate || 0) / 100)
      subtotal += ext
      totalTax += tax
    }
    const freight = Number(watch('freightAmount')) || 0
    const freightTax = Number(watch('freightTaxAmount')) || 0
    return { subtotal, totalTax, freight, freightTax, grandTotal: subtotal + totalTax + freight + freightTax }
  }, [watchedLines, watch('freightAmount'), watch('freightTaxAmount')])

  function selectVendor(vendor: Vendor) {
    setValue('vendorId', vendor.id, { shouldValidate: true })
    setVendorSearch(vendor.name)
    setShowVendorDropdown(false)
  }

  function selectItem(index: number, itemId: string) {
    const item = items.find((i: ItemSummary) => i.id === itemId)
    if (item) {
      setValue(`lines.${index}.itemId`, itemId)
      if (!watch(`lines.${index}.description`)) {
        setValue(`lines.${index}.description`, item.description)
      }
      // Load UOM conversions
      const baseUom = item.baseUnitOfMeasure || 'EA'
      setLineUomOptions(prev => ({ ...prev, [index]: [{ value: baseUom, label: baseUom + ' (base)' }] }))
      void getItemUomConversions(itemId).then((convs: UomConversionDto[]) => {
        const opts = [{ value: baseUom, label: baseUom + ' (base)' }]
        for (const c of convs) {
          if (c.fromUOM === baseUom) opts.push({ value: c.toUOM, label: `${c.toUOM} (${c.conversionFactor}x)` })
        }
        setLineUomOptions(prev => ({ ...prev, [index]: opts }))
      }).catch(() => {
        setLineUomOptions(prev => ({ ...prev, [index]: [{ value: baseUom, label: baseUom + ' (base)' }] }))
      })
    }
  }

  function addLine() {
    const nextNum = (watchedLines?.length ?? 0) + 1
    append({ lineNumber: nextNum, description: '', quantity: 1, unitOfMeasure: 'EA', unitPrice: 0 })
  }

  const createMutation = useMutation({
    mutationFn: (d: Form) => createPurchaseOrder({
      companyId: defaults.companyId,
      vendorId: d.vendorId,
      poNumber: d.poNumber,
      orderDate: d.orderDate,
      orderType: d.orderType,
      shipToName: d.shipToName || null,
      shipToAddress: d.shipToAddress || null,
      paymentTermId: d.paymentTermId || null,
      buyerId: d.buyerId || null,
      buyerNotes: d.buyerNotes || null,
      vendorReference: d.vendorReference || null,
      blanketAmountLimit: d.orderType === 'Blanket' || d.orderType === 'Standing' ? Number(d.blanketLimit) || null : null,
      freightAmount: Number(d.freightAmount) || 0,
      freightTaxAmount: Number(d.freightTaxAmount) || 0,
      taxExempt: Boolean(d.taxExempt),
      lines: d.lines.map((l, i) => ({
        lineNumber: i + 1,
        itemId: l.itemId || null,
        description: l.description,
        quantity: l.quantity,
        unitOfMeasure: l.unitOfMeasure,
        unitPrice: l.unitPrice,
        taxCode: l.taxCode || null,
        taxRate: Number(l.taxRate) || 0,
        needByDate: l.needByDate || null,
        accountId: l.accountId || null,
        projectId: l.projectId || null,
        taskId: l.taskId || null,
        requisitionLineId: null,
      })),
    }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'purchase-orders'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const actionMutation = useMutation({
    mutationFn: (p: { id: string; op: 'approve' | 'submit' | 'close' }) => {
      if (p.op === 'approve') return approvePurchaseOrder(p.id)
      if (p.op === 'submit') return submitPurchaseOrder(p.id)
      return closePurchaseOrder(p.id)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['purchasing', 'purchase-orders'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const doCancel = (id: string) => {
    setFormError(null)
    cancelPurchaseOrder(id, cancelReason).then(() => { qc.invalidateQueries({ queryKey: ['purchasing', 'purchase-orders'] }); setCancelId(null); setCancelReason('') }).catch(e => setFormError(getErrorMessage(e)))
  }

  const releaseMutation = useMutation({
    mutationFn: (id: string) => releasePurchaseOrder(id, Number(releaseAmount)),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'purchase-orders'] }); setReleaseId(null); setReleaseAmount('') },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const doRelease = (id: string) => { setFormError(null); releaseMutation.mutate(id) }

  const doPrint = (id: string) => {
    setFormError(null)
    printPurchaseOrder(id).then(() => qc.invalidateQueries({ queryKey: ['purchasing', 'purchase-orders'] })).catch(e => setFormError(getErrorMessage(e)))
  }
  const doEmail = (id: string) => {
    setFormError(null)
    emailPurchaseOrderToVendor(id).then(() => qc.invalidateQueries({ queryKey: ['purchasing', 'purchase-orders'] })).catch(e => setFormError(getErrorMessage(e)))
  }

  const close = () => { setOpen(false); setFormError(null); setVendorSearch('') }
  const openForm = () => {
    setFormError(null); setVendorSearch('')
    reset({
      ...defaults,
      lines: [{ lineNumber: 1, description: '', quantity: 1, unitOfMeasure: 'EA', unitPrice: 0 }],
    })
    setOpen(true)
  }
  const onSubmit = (d: Form) => { setFormError(null); createMutation.mutate(d) }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter((r: PurchaseOrderSummary) => r.poNumber.toLowerCase().includes(q))
  }, [rows, search])

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Purchase Orders" description={`${rows.length} PO(s)`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New PO</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search..." aria-label="Search POs" leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading...</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No purchase orders yet.'}</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">PO #</th><th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Order Date</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Total</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Remaining</th><th className="px-3 py-2 font-medium text-gray-500">Status</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map((r: PurchaseOrderSummary) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.poNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.vendorId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.orderDate).toLocaleDateString()}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{formatCurrency(r.totalAmount)}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{formatCurrency(r.remainingAmount)}</td>
                      <td className="px-3 py-3">{statusBadge(r.status)}</td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          {r.status === 'Draft' && <Button size="sm" variant="outline" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: r.id, op: 'submit' })}>Submit</Button>}
                          {r.status === 'PendingApproval' && <Button size="sm" variant="primary" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: r.id, op: 'approve' })}><Check className="h-3.5 w-3.5" /> Approve</Button>}
                          {(r.status === 'Open' || r.status === 'Approved' || r.status === 'PartiallyReceived') && <Button size="sm" variant="outline" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: r.id, op: 'close' })}>Close</Button>}
                          {r.status === 'Open' && <Button size="sm" variant="ghost" className="text-red-600" disabled={actionMutation.isPending || cancelId === r.id} onClick={() => setCancelId(r.id)}>Cancel</Button>}
                          {(r.status === 'Draft' || r.status === 'Approved') && <Button size="sm" variant="ghost" onClick={() => doEmail(r.id)}>Email</Button>}
                          <Button size="sm" variant="ghost" onClick={() => doPrint(r.id)}>Print</Button>
                          {(r.status === 'Approved' || r.status === 'Open') && <Button size="sm" variant="ghost" onClick={() => setReleaseId(r.id)}>Release</Button>}
                        </div>
                        {cancelId === r.id && (
                          <div className="mt-2 flex gap-1 justify-end">
                            <Input value={cancelReason} onChange={e => setCancelReason(e.target.value)} placeholder="Reason" className="h-7 text-xs" />
                            <Button size="sm" variant="destructive" onClick={() => doCancel(r.id)}>Confirm</Button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      {/* Create PO Modal */}
      <Modal isOpen={open} onClose={close} title="New Purchase Order" size="lg"
        footer={<><Button variant="secondary" onClick={close} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>Create PO</Button></>}>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          {/* Header */}
          <div className="grid grid-cols-2 gap-3">
            <Input {...register('poNumber')} label="PO #" {...fieldError(errors.poNumber?.message)} required />
            <div className="relative">
              <Input
                value={selectedVendor?.name ?? vendorSearch}
                onChange={(e) => { setVendorSearch(e.target.value); setShowVendorDropdown(true); setValue('vendorId', '', { shouldValidate: true }) }}
                onFocus={() => setShowVendorDropdown(true)}
                onBlur={() => setTimeout(() => setShowVendorDropdown(false), 200)}
                label="Vendor"
                placeholder="Search vendor..."
                {...fieldError(errors.vendorId?.message)}
                required
              />
              {showVendorDropdown && filteredVendors.length > 0 && (
                <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg max-h-60 overflow-auto">
                  {filteredVendors.map((v: Vendor) => (
                    <button key={v.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                      onMouseDown={() => selectVendor(v)}>
                      <span className="font-medium">{v.name}</span>
                      <span className="ml-2 text-gray-500 text-xs">{v.vendorId}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
            <Input {...register('orderDate')} label="Order Date" type="date" {...fieldError(errors.orderDate?.message)} required />
            <Select {...register('orderType')} label="Type" options={orderTypeOptions} {...fieldError(errors.orderType?.message)} required />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <Input {...register('shipToName')} label="Ship To Name" placeholder="Attention / Department" />
            <Input {...register('vendorReference')} label="Vendor Reference" placeholder="Vendor's reference #" />
            <Input {...register('buyerId')} label="Buyer ID" placeholder="Buyer user ID" />
            <Select {...register('paymentTermId')} label="Payment Terms" placeholder="Select payment terms..." options={paymentTermOptions} />
          </div>

          <Textarea {...register('shipToAddress')} label="Ship To Address" placeholder="Full shipping address..." rows={2} />
          <Textarea {...register('buyerNotes')} label="Buyer Notes" placeholder="Internal notes..." rows={2} />

          {/* Lines */}
          <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-medium text-gray-900 dark:text-white">Line Items</h3>
              <Button type="button" variant="outline" size="sm" onClick={addLine} leftIcon={<Plus className="h-4 w-4" />}>Add Line</Button>
            </div>
            {errors.lines?.message && <p className="text-sm text-red-600 mb-2">{errors.lines.message}</p>}
            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
              <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-800">
                  <tr>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500 w-8">#</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Description</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Item</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">UOM</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Price</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Tax%</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Ext. Total</th>
                    <th className="px-2 py-2 w-8" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {fields.map((field, index) => {
                    const line = watchedLines?.[index]
                    const ext = ((line?.quantity ?? 0) * (line?.unitPrice ?? 0)) * (1 + (line?.taxRate ?? 0) / 100)
                    return (
                      <tr key={field.id} className="bg-white dark:bg-gray-900">
                        <td className="px-2 py-1.5 text-sm text-gray-500">{index + 1}</td>
                        <td className="px-2 py-1.5">
                          <input {...register(`lines.${index}.description`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" />
                        </td>
                        <td className="px-2 py-1.5">
                          <select {...register(`lines.${index}.itemId`)} onChange={(e) => { register(`lines.${index}.itemId`).onChange(e); selectItem(index, e.target.value) }}
                            className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                            <option value="">Select...</option>
                            {itemOptions.map(opt => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                          </select>
                        </td>
                        <td className="px-2 py-1.5">
                          <input type="number" step="0.01" {...register(`lines.${index}.quantity`)} className="w-20 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" />
                        </td>
                        <td className="px-2 py-1.5">
                          <select {...register(`lines.${index}.unitOfMeasure`)} className="w-20 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-1 py-1">
                            {(lineUomOptions[index] ?? [{ value: 'EA', label: 'EA' }]).map(o => (
                              <option key={o.value} value={o.value}>{o.label}</option>
                            ))}
                          </select>
                        </td>
                        <td className="px-2 py-1.5">
                          <input type="number" step="0.01" {...register(`lines.${index}.unitPrice`)} className="w-24 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" />
                        </td>
                        <td className="px-2 py-1.5">
                          <input type="number" step="0.01" {...register(`lines.${index}.taxRate`)} className="w-16 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" />
                        </td>
                        <td className="px-2 py-1.5 text-right text-sm font-medium tabular-nums">{formatCurrency(ext)}</td>
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
              </table>
            </div>
          </div>

          {/* Freight & Totals */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 border-t border-gray-200 dark:border-gray-700 pt-4">
            <div className="space-y-3">
              <h3 className="text-sm font-medium text-gray-900 dark:text-white">Freight & Tax</h3>
              <Input {...register('freightAmount')} label="Freight Amount" type="number" step="0.01" min="0" placeholder="0.00" />
              <Input {...register('freightTaxAmount')} label="Freight Tax" type="number" step="0.01" min="0" placeholder="0.00" />
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                <input type="checkbox" {...register('taxExempt')} className="h-4 w-4" /> Tax Exempt
              </label>
              {(watchOrderType === 'Blanket' || watchOrderType === 'Standing') && (
                <Input {...register('blanketLimit')} label="Blanket Amount Limit" type="number" step="0.01" min="0" placeholder="0.00" />
              )}
            </div>
            <div className="flex justify-end">
              <div className="w-72 space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Subtotal:</span>
                  <span className="font-medium tabular-nums">{formatCurrency(totals.subtotal)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Tax:</span>
                  <span className="font-medium tabular-nums">{formatCurrency(totals.totalTax)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Freight:</span>
                  <span className="font-medium tabular-nums">{formatCurrency(totals.freight)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Freight Tax:</span>
                  <span className="font-medium tabular-nums">{formatCurrency(totals.freightTax)}</span>
                </div>
                <div className="border-t border-gray-200 dark:border-gray-700 pt-2 flex justify-between">
                  <span className="text-base font-bold text-gray-900 dark:text-white">Grand Total:</span>
                  <span className="text-base font-bold tabular-nums text-gray-900 dark:text-white">{formatCurrency(totals.grandTotal)}</span>
                </div>
              </div>
            </div>
          </div>
        </form>
      </Modal>

      {/* Release Modal */}
      <Modal isOpen={releaseId !== null} onClose={() => setReleaseId(null)} title="Release Against Blanket PO"
        footer={<><Button variant="secondary" onClick={() => setReleaseId(null)} disabled={releaseMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={() => releaseId && doRelease(releaseId)} isLoading={releaseMutation.isPending}>Release</Button></>}>
        <div className="space-y-4">
          <Input type="number" step="0.01" min="0.01" value={releaseAmount} onChange={e => setReleaseAmount(e.target.value)} label="Release Amount" placeholder="0.00" required />
          <p className="text-xs text-gray-500">Draws down the blanket/standing PO's available amount. The released amount accumulates and cannot exceed the PO total.</p>
        </div>
      </Modal>
    </div>
  )
}
