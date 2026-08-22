import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle, Trash2, Eye } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  RETURN_APPROVAL_THRESHOLD,
  approveReturn,
  companyId,
  confirmReturn,
  createReturn,
  getReturns,
  getSalesOrders,
  getShipments,
  rejectReturn,
  submitReturnForApproval,
} from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import { getItems, getWarehouses } from '@api/inventory'
import type { ReturnStatus, ReturnSummary, SalesOrderSummary, ShipmentSummary } from '@/types/orderManagement'
import type { ArCustomer } from '@/types/ar'
import type { ItemSummary, WarehouseSummary } from '@/types/inventory'

const lineSchema = z.object({
  lineNumber: z.number(),
  itemId: z.string().min(1, 'Item is required'),
  description: z.string().min(1, 'Description is required'),
  quantity: z.coerce.number().positive('Qty must be > 0'),
  unitPrice: z.coerce.number().min(0),
  unitOfMeasure: z.string().min(1, 'UOM is required'),
  warehouseId: z.string().optional(),
  shipmentLineId: z.string().optional(),
  salesOrderLineId: z.string().optional(),
  accountId: z.string().optional(),
  discountPercent: z.coerce.number().min(0).max(100).optional(),
  taxPercent: z.coerce.number().min(0).max(100).optional(),
  restockDisposition: z.string().optional(),
})

const returnSchema = z.object({
  returnNumber: z.string().min(1, 'Return # is required'),
  customerId: z.string().min(1, 'Customer is required'),
  shipmentId: z.string().optional(),
  salesOrderId: z.string().optional(),
  returnDate: z.string().min(1, 'Date is required'),
  reasonCode: z.string().optional(),
  note: z.string().optional(),
  lines: z.array(lineSchema).min(1, 'At least one line required'),
})
type ReturnForm = z.infer<typeof returnSchema>

const dispositionOptions = [
  { value: 'Restock', label: 'Restock to Inventory' },
  { value: 'Scrap', label: 'Scrap / Dispose' },
  { value: 'ReturnToVendor', label: 'Return to Vendor' },
]

const reasonOptions = [
  { value: 'Defective', label: 'Defective' },
  { value: 'WrongItem', label: 'Wrong Item' },
  { value: 'DamagedInTransit', label: 'Damaged in Transit' },
  { value: 'CustomerCancel', label: 'Customer Cancel' },
  { value: 'QualityIssue', label: 'Quality Issue' },
  { value: 'Other', label: 'Other' },
]

function fieldError(msg?: string) { return msg ? { error: msg } : {} }
function formatCurrency(n: number) { return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n) }

type BadgeVariant = 'success' | 'warning' | 'info' | 'neutral'
function returnStatusVariant(status: ReturnStatus): BadgeVariant {
  if (status === 'Confirmed') return 'success'
  if (status === 'PendingApproval') return 'warning'
  if (status === 'Draft') return 'neutral'
  return 'info'
}

export function ReturnsPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [customerSearch, setCustomerSearch] = useState('')
  const [showCustomerDropdown, setShowCustomerDropdown] = useState(false)

  const { register, handleSubmit, control, watch, setValue, reset, formState: { errors } } = useForm<ReturnForm>({
    resolver: zodResolver(returnSchema),
    defaultValues: {
      returnNumber: `RMA-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}`,
      customerId: '',
      shipmentId: '',
      salesOrderId: '',
      returnDate: new Date().toISOString().slice(0, 10),
      reasonCode: '',
      note: '',
      lines: [{ lineNumber: 1, itemId: '', description: '', quantity: 1, unitPrice: 0, unitOfMeasure: 'EA', discountPercent: 0, taxPercent: 0, restockDisposition: 'Restock' }],
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })
  const watchedLines = watch('lines')

  // Lookups
  const { data: returns = [], isLoading } = useQuery({ queryKey: ['om', 'returns'], queryFn: () => getReturns() })
  const { data: customers = [] } = useQuery({ queryKey: ['ar', 'customers'], queryFn: getCustomers })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: salesOrders = [] } = useQuery({ queryKey: ['om', 'sales-orders'], queryFn: () => getSalesOrders() })
  const { data: shipments = [] } = useQuery({ queryKey: ['om', 'shipments'], queryFn: () => getShipments() })

  const soOptions = useMemo(() => salesOrders.map((o: SalesOrderSummary) => ({ value: o.id, label: `${o.orderNumber} (${o.status})` })), [salesOrders])
  const shipmentOptions = useMemo(() => shipments.map((s: ShipmentSummary) => ({ value: s.id, label: `${s.shipmentNumber} (${s.status})` })), [shipments])

  const filteredCustomers = useMemo(() => {
    const q = customerSearch.trim().toLowerCase()
    if (!q) return customers.slice(0, 10)
    return customers.filter(c => c.name.toLowerCase().includes(q) || c.customerId.toLowerCase().includes(q)).slice(0, 10)
  }, [customers, customerSearch])

  const selectedCustomer = useMemo(() => customers.find((c: ArCustomer) => c.id === watch('customerId')), [customers, watch('customerId')])

  const itemOptions = useMemo(() => items.map((i: ItemSummary) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])

  const totals = useMemo(() => {
    let subtotal = 0
    for (const line of watchedLines ?? []) {
      subtotal += (line.quantity || 0) * (line.unitPrice || 0) * (1 - (line.discountPercent || 0) / 100)
    }
    return { subtotal }
  }, [watchedLines])

  function selectCustomer(c: ArCustomer) {
    setValue('customerId', c.id, { shouldValidate: true })
    setCustomerSearch(c.name)
    setShowCustomerDropdown(false)
  }

  function addLine() {
    append({ lineNumber: (watchedLines?.length ?? 0) + 1, itemId: '', description: '', quantity: 1, unitPrice: 0, unitOfMeasure: 'EA', discountPercent: 0, taxPercent: 0, restockDisposition: 'Restock' })
  }

  const createMut = useMutation({
    mutationFn: (d: ReturnForm) => createReturn({
      returnNumber: d.returnNumber,
      companyId: companyId(),
      customerId: d.customerId,
      shipmentId: d.shipmentId || null,
      salesOrderId: d.salesOrderId || null,
      returnDate: new Date(d.returnDate).toISOString(),
      reasonCode: d.reasonCode || null,
      note: d.note || null,
      lines: d.lines.map((l, i) => ({
        lineNumber: i + 1,
        itemId: l.itemId,
        description: l.description,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        unitOfMeasure: l.unitOfMeasure,
        warehouseId: l.warehouseId || null,
        shipmentLineId: l.shipmentLineId || null,
        salesOrderLineId: l.salesOrderLineId || null,
        accountId: l.accountId || null,
        discountPercent: l.discountPercent || 0,
        taxPercent: l.taxPercent || 0,
        restockDisposition: l.restockDisposition || null,
      })),
    }),
    onSuccess: (id) => { qc.invalidateQueries({ queryKey: ['om', 'returns'] }); setShowCreate(false); reset(); navigate(`/om/returns/${id}`) },
    onError: e => setErr(getErrorMessage(e)),
  })

  const confirmMut = useMutation({
    mutationFn: (id: string) => confirmReturn(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['om', 'returns'] }),
    onError: e => setErr(getErrorMessage(e)),
  })

  const submitApprovalMut = useMutation({
    mutationFn: (id: string) => submitReturnForApproval(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['om', 'returns'] }),
    onError: e => setErr(getErrorMessage(e)),
  })

  const approveMut = useMutation({
    mutationFn: (id: string) => approveReturn(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['om', 'returns'] }),
    onError: e => setErr(getErrorMessage(e)),
  })

  const [rejectTarget, setRejectTarget] = useState<ReturnSummary | null>(null)
  const [rejectReason, setRejectReason] = useState('')
  const rejectMut = useMutation({
    mutationFn: (d: { id: string; reason: string }) => rejectReturn(d.id, d.reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['om', 'returns'] })
      setRejectTarget(null)
      setRejectReason('')
    },
    onError: e => setErr(getErrorMessage(e)),
  })

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return returns
    return returns.filter((r: ReturnSummary) => r.returnNumber.toLowerCase().includes(q))
  }, [returns, search])

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"><AlertCircle className="h-5 w-5" /> {err}</div>}

      {/* Create Return Modal */}
      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)} title="New Return (RMA)" size="xl"
        footer={<><Button variant="secondary" onClick={() => setShowCreate(false)} disabled={createMut.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(d => createMut.mutate(d))} isLoading={createMut.isPending}>Create Return</Button></>}>
        <form className="space-y-5" noValidate>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
            <Input {...register('returnNumber')} label="Return #" {...fieldError(errors.returnNumber?.message)} required />
            <Input {...register('returnDate')} type="date" label="Return Date" {...fieldError(errors.returnDate?.message)} required />
            <div className="relative">
              <Input value={selectedCustomer?.name ?? customerSearch} onChange={e => { setCustomerSearch(e.target.value); setShowCustomerDropdown(true); setValue('customerId', '', { shouldValidate: true }) }}
                onFocus={() => setShowCustomerDropdown(true)} onBlur={() => setTimeout(() => setShowCustomerDropdown(false), 200)}
                label="Customer" placeholder="Search customer..." {...fieldError(errors.customerId?.message)} required />
              {showCustomerDropdown && filteredCustomers.length > 0 && (
                <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
                  {filteredCustomers.map((c: ArCustomer) => (
                    <button key={c.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                      onMouseDown={() => selectCustomer(c)}>
                      <span className="font-medium">{c.name}</span> <span className="text-gray-500 text-xs">{c.customerId}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
            <Select {...register('reasonCode')} label="Reason Code" placeholder="Select reason..." options={reasonOptions} />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Select {...register('shipmentId')} label="Original Shipment (optional)" placeholder="Select shipment..." options={[{ value: '', label: '— None —' }, ...shipmentOptions]} />
            <Select {...register('salesOrderId')} label="Original Sales Order (optional)" placeholder="Select sales order..." options={[{ value: '', label: '— None —' }, ...soOptions]} />
          </div>

          {/* Lines */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <h4 className="text-sm font-medium text-gray-900 dark:text-white">Return Lines</h4>
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
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Price</th>
                    <th className="px-2 py-2 text-left text-xs font-medium uppercase text-gray-500">Disposition</th>
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">Total</th>
                    <th className="px-2 py-2 w-8" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {fields.map((field, idx) => {
                    const line = watchedLines?.[idx]
                    const total = ((line?.quantity ?? 0) * (line?.unitPrice ?? 0)) * (1 - (line?.discountPercent ?? 0) / 100)
                    return (
                      <tr key={field.id} className="bg-white dark:bg-gray-900">
                        <td className="px-2 py-1.5 text-sm text-gray-500">{idx + 1}</td>
                        <td className="px-2 py-1.5">
                          <select {...register(`lines.${idx}.itemId`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                            <option value="">Select...</option>
                            {itemOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                          </select>
                        </td>
                        <td className="px-2 py-1.5"><input {...register(`lines.${idx}.description`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" /></td>
                        <td className="px-2 py-1.5"><input type="number" step="0.01" {...register(`lines.${idx}.quantity`)} className="w-20 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" /></td>
                        <td className="px-2 py-1.5"><input type="number" step="0.01" {...register(`lines.${idx}.unitPrice`)} className="w-24 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" /></td>
                        <td className="px-2 py-1.5">
                          <select {...register(`lines.${idx}.restockDisposition`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                            {dispositionOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                          </select>
                        </td>
                        <td className="px-2 py-1.5 text-right text-sm font-medium tabular-nums">{formatCurrency(total)}</td>
                        <td className="px-2 py-1.5">{fields.length > 1 && <button type="button" onClick={() => remove(idx)} className="text-red-500 hover:text-red-700"><Trash2 className="h-3.5 w-3.5" /></button>}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>

          <div className="flex justify-end">
            <div className="w-72 space-y-1">
              <div className="flex justify-between text-sm"><span className="text-gray-500">Return Total:</span><span className="tabular-nums font-bold">{formatCurrency(totals.subtotal)}</span></div>
            </div>
          </div>

          <Textarea {...register('note')} label="Notes" placeholder="Return notes / reason details..." rows={2} />
        </form>
      </Modal>

      {/* Reject (with reason) modal */}
      <Modal isOpen={rejectTarget !== null} onClose={() => setRejectTarget(null)} title={`Reject ${rejectTarget?.returnNumber ?? ''}`}
        footer={<><Button variant="secondary" onClick={() => setRejectTarget(null)} disabled={rejectMut.isPending}>Cancel</Button>
          <Button variant="destructive" isLoading={rejectMut.isPending}
            onClick={() => rejectTarget && rejectMut.mutate({ id: rejectTarget.id, reason: rejectReason || 'Rejected without reason.' })}>Reject Return</Button></>}>
        <div className="space-y-3">
          <p className="text-sm text-gray-600 dark:text-gray-400">The return goes back to Draft and stays blocked from confirmation while above the {formatCurrency(RETURN_APPROVAL_THRESHOLD)} approval threshold.</p>
          <Textarea label="Rejection Reason" rows={3} value={rejectReason} onChange={e => setRejectReason(e.target.value)} placeholder="Why is this return rejected?" />
        </div>
      </Modal>

      {/* List */}
      <Card>
        <CardHeader title="Returns (RMA)" description={`${returns.length} return(s)`}
          action={<Button variant="primary" size="sm" onClick={() => { setErr(null); setCustomerSearch(''); reset(); setShowCreate(true) }} leftIcon={<Plus className="h-4 w-4" />}>New Return</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by return #..." leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading...</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No returns yet.'}</p> :
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Return #</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {filtered.map((r: ReturnSummary) => (
                      <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.returnNumber}</td>                         <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{customers.find((c: ArCustomer) => c.id === r.customerId)?.name ?? r.customerId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.returnDate).toLocaleDateString()}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{formatCurrency(r.totalAmount)}</td>
                        <td className="px-3 py-3"><Badge variant={returnStatusVariant(r.status)} size="sm" dot>{r.status}</Badge></td>
                        <td className="px-3 py-3 text-right">
                          <div className="flex justify-end gap-1">
                            <Button size="sm" variant="ghost" onClick={() => navigate(`/om/returns/${r.id}`)}><Eye className="h-4 w-4" /></Button>
                            {r.status === 'Draft' && r.returnValue > RETURN_APPROVAL_THRESHOLD && !r.isApproved && (
                              <Button size="sm" variant="outline" disabled={submitApprovalMut.isPending} onClick={() => submitApprovalMut.mutate(r.id)}>Submit for Approval</Button>
                            )}
                            {r.status === 'Draft' && (r.returnValue <= RETURN_APPROVAL_THRESHOLD || r.isApproved) && (
                              <Button size="sm" variant="primary" disabled={confirmMut.isPending} onClick={() => confirmMut.mutate(r.id)}>Confirm</Button>
                            )}
                            {r.status === 'PendingApproval' && (
                              <Button size="sm" variant="success" disabled={approveMut.isPending} onClick={() => approveMut.mutate(r.id)}>Approve</Button>
                            )}
                            {r.status === 'PendingApproval' && (
                              <Button size="sm" variant="destructive" disabled={rejectMut.isPending} onClick={() => setRejectTarget(r)}>Reject</Button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>}
        </CardContent>
      </Card>
    </div>
  )
}
