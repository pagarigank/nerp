import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle, Trash2, Eye, Pencil } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { UomSelect } from '@components/ui/UomSelect'
import { Input, Textarea } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getShipments, getSalesOrders, getSalesOrder,
  createShipment, updateShipment, deleteShipment, confirmShipment, companyId,
} from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import { getItems, getItemUomConversions } from '@api/inventory'
import type { ShipmentSummary, SalesOrderSummary, SalesOrderDetail } from '@/types/orderManagement'
import type { ArCustomer } from '@/types/ar'
import type { ItemSummary, UomConversionDto } from '@/types/inventory'

const lineSchema = z.object({
  lineNumber: z.number(),
  itemId: z.string().min(1, 'Item is required'),
  description: z.string().min(1, 'Description is required'),
  quantity: z.coerce.number().positive('Qty must be > 0'),
  unitPrice: z.coerce.number().min(0),
  unitOfMeasure: z.string().min(1, 'UOM is required'),
  warehouseId: z.string().optional(),
  salesOrderLineId: z.string().optional(),
  projectId: z.string().optional(),
  accountId: z.string().optional(),
  discountPercent: z.coerce.number().min(0).max(100).optional(),
  taxPercent: z.coerce.number().min(0).max(100).optional(),
})

const shipmentSchema = z.object({
  shipmentNumber: z.string().min(1, 'Shipment # is required'),
  customerId: z.string().min(1, 'Customer is required'),
  salesOrderId: z.string().optional(),
  shipmentDate: z.string().min(1, 'Date is required'),
  carrier: z.string().optional(),
  trackingNumber: z.string().optional(),
  freightCost: z.coerce.number().min(0).optional(),
  notes: z.string().optional(),
  lines: z.array(lineSchema).min(1, 'At least one line required'),
})
type ShipmentForm = z.infer<typeof shipmentSchema>

function fieldError(msg?: string) { return msg ? { error: msg } : {} }
function formatCurrency(n: number) { return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n) }

export function ShipmentsPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [customerSearch, setCustomerSearch] = useState('')
  const [showCustomerDropdown, setShowCustomerDropdown] = useState(false)
  const [soSearch, setSoSearch] = useState('')
  const [lineUomOptions, setLineUomOptions] = useState<Record<number, { value: string; label: string }[]>>({})

  const { register, handleSubmit, control, watch, setValue, reset, formState: { errors } } = useForm<ShipmentForm>({
    resolver: zodResolver(shipmentSchema),
    defaultValues: {
      shipmentNumber: `SHP-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}`,
      customerId: '',
      salesOrderId: '',
      shipmentDate: new Date().toISOString().slice(0, 10),
      carrier: '',
      trackingNumber: '',
      freightCost: 0,
      notes: '',
      lines: [{ lineNumber: 1, itemId: '', description: '', quantity: 1, unitPrice: 0, unitOfMeasure: 'EA', discountPercent: 0, taxPercent: 0 }],
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })
  const watchedLines = watch('lines')

  // Lookups
  const { data: shipments = [], isLoading } = useQuery({ queryKey: ['om', 'shipments'], queryFn: () => getShipments() })
  const { data: customers = [] } = useQuery({ queryKey: ['ar', 'customers'], queryFn: getCustomers })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: salesOrders = [] } = useQuery({ queryKey: ['om', 'sales-orders'], queryFn: () => getSalesOrders() })

  const filteredCustomers = useMemo(() => {
    const q = customerSearch.trim().toLowerCase()
    if (!q) return customers.slice(0, 10)
    return customers.filter(c => c.name.toLowerCase().includes(q) || c.customerId.toLowerCase().includes(q)).slice(0, 10)
  }, [customers, customerSearch])

  const selectedCustomer = useMemo(() => customers.find((c: ArCustomer) => c.id === watch('customerId')), [customers, watch('customerId')])

  const filteredSOs = useMemo(() => {
    const q = soSearch.trim().toLowerCase()
    if (!q) return salesOrders.slice(0, 10)
    return salesOrders.filter((o: SalesOrderSummary) => o.orderNumber.toLowerCase().includes(q)).slice(0, 10)
  }, [salesOrders, soSearch])

  // Auto-open from Sales Order → Create Shipment wiring
  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const soId = params.get('salesOrderId')
    if (soId && salesOrders.length > 0) {
      const so = salesOrders.find(o => o.id === soId)
      if (so) {
        setValue('salesOrderId', soId)
        setSoSearch(so.orderNumber)
        loadSO(soId)
        setShowCreate(true)
      }
    }
  }, [salesOrders])

  const itemOptions = useMemo(() => items.map((i: ItemSummary) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])

  const totals = useMemo(() => {
    let subtotal = 0
    for (const line of watchedLines ?? []) {
      subtotal += (line.quantity || 0) * (line.unitPrice || 0) * (1 - (line.discountPercent || 0) / 100)
    }
    return { subtotal, freight: Number(watch('freightCost')) || 0, grandTotal: subtotal + (Number(watch('freightCost')) || 0) }
  }, [watchedLines, watch('freightCost')])

  function selectCustomer(c: ArCustomer) {
    setValue('customerId', c.id, { shouldValidate: true })
    setCustomerSearch(c.name)
    setShowCustomerDropdown(false)
  }

  async function loadSO(soId: string) {
    if (!soId) return
    try {
      const detail: SalesOrderDetail = await getSalesOrder(soId)
      setValue('customerId', detail.customerId, { shouldValidate: true })
      setCustomerSearch(detail.customerId.slice(0, 8))
      // Populate lines from SO
      const currentLines = watch('lines')
      if (currentLines.length === 1 && !currentLines[0].itemId) {
        remove(0)
      }
      for (const soLine of detail.lines) {
        const remaining = soLine.quantity - soLine.shippedQuantity
        if (remaining > 0) {
          append({
            lineNumber: (watch('lines')?.length ?? 0) + 1,
            itemId: soLine.itemId,
            description: soLine.description,
            quantity: remaining,
            unitPrice: soLine.unitPrice,
            unitOfMeasure: soLine.unitOfMeasure,
            warehouseId: soLine.warehouseId ?? undefined,
            salesOrderLineId: soLine.id,
            projectId: soLine.projectId ?? undefined,
            accountId: soLine.accountId ?? undefined,
            discountPercent: soLine.discountPercent,
            taxPercent: soLine.taxPercent,
          })
        }
      }
    } catch (e) {
      setErr(getErrorMessage(e))
    }
  }

  function addLine() {
    append({ lineNumber: (watchedLines?.length ?? 0) + 1, itemId: '', description: '', quantity: 1, unitPrice: 0, unitOfMeasure: 'EA', discountPercent: 0, taxPercent: 0 })
  }

  const createMut = useMutation({
    mutationFn: (d: ShipmentForm) => createShipment({
      shipmentNumber: d.shipmentNumber,
      companyId: companyId(),
      customerId: d.customerId,
      salesOrderId: d.salesOrderId || null,
      shipmentDate: new Date(d.shipmentDate).toISOString(),
      carrier: d.carrier || null,
      trackingNumber: d.trackingNumber || null,
      freightCost: Number(d.freightCost) || 0,
      lines: d.lines.map((l, i) => ({
        lineNumber: i + 1,
        itemId: l.itemId,
        description: l.description,
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        unitOfMeasure: l.unitOfMeasure,
        warehouseId: l.warehouseId || null,
        salesOrderLineId: l.salesOrderLineId || null,
        projectId: l.projectId || null,
        accountId: l.accountId || null,
        discountPercent: l.discountPercent || 0,
        taxPercent: l.taxPercent || 0,
      })),
    }),
    onSuccess: (id) => { qc.invalidateQueries({ queryKey: ['om', 'shipments'] }); setShowCreate(false); reset(); navigate(`/om/shipments/${id}`) },
    onError: e => setErr(getErrorMessage(e)),
  })

  const [editingShipment, setEditingShipment] = useState<ShipmentSummary | null>(null)

  const updateMut = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => updateShipment(id, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['om', 'shipments'] }); setEditingShipment(null); setShowCreate(false); reset(); },
    onError: e => setErr(getErrorMessage(e)),
  })

  const deleteMut = useMutation({
    mutationFn: (id: string) => deleteShipment(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['om', 'shipments'] }),
    onError: e => setErr(getErrorMessage(e)),
  })

  const confirmMut = useMutation({
    mutationFn: (id: string) => confirmShipment(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['om', 'shipments'] }),
    onError: e => setErr(getErrorMessage(e)),
  })

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return shipments
    return shipments.filter((s: ShipmentSummary) => s.shipmentNumber.toLowerCase().includes(q))
  }, [shipments, search])

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"><AlertCircle className="h-5 w-5" /> {err}</div>}

      {/* Create Shipment Modal */}
      <Modal isOpen={showCreate} onClose={() => { setShowCreate(false); setEditingShipment(null); reset() }} title={editingShipment ? `Edit Shipment ${editingShipment.shipmentNumber}` : 'New Shipment'} size="xl"
        footer={<><Button variant="secondary" onClick={() => { setShowCreate(false); setEditingShipment(null); reset() }} disabled={createMut.isPending || updateMut.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(d => editingShipment ? updateMut.mutate({ id: editingShipment.id, data: { carrier: d.carrier || null, trackingNumber: d.trackingNumber || null, freightCost: Number(d.freightCost) || 0, notes: d.notes || null, lines: d.lines.map((l, i) => ({ lineNumber: i+1, itemId: l.itemId, description: l.description, quantity: l.quantity, unitPrice: l.unitPrice, unitOfMeasure: l.unitOfMeasure, warehouseId: l.warehouseId || null, salesOrderLineId: l.salesOrderLineId || null, projectId: l.projectId || null, accountId: l.accountId || null, discountPercent: l.discountPercent || 0, taxPercent: l.taxPercent || 0 })) }}) : createMut.mutate(d))} isLoading={createMut.isPending || updateMut.isPending}>{editingShipment ? 'Update Shipment' : 'Create Shipment'}</Button></>}>
        <form className="space-y-5" noValidate>
          {/* Header */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
            <Input {...register('shipmentNumber')} label="Shipment #" {...fieldError(errors.shipmentNumber?.message)} required />
            <Input {...register('shipmentDate')} type="date" label="Ship Date" {...fieldError(errors.shipmentDate?.message)} required />
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
            <div className="relative">
              <Input value={soSearch} onChange={e => { setSoSearch(e.target.value); setValue('salesOrderId', '') }}
                label="From Sales Order (optional)" placeholder="Search SO..." />
              {soSearch && filteredSOs.length > 0 && (
                <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
                  {filteredSOs.map((o: SalesOrderSummary) => (
                    <button key={o.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                      onMouseDown={() => { setValue('salesOrderId', o.id); setSoSearch(o.orderNumber); loadSO(o.id) }}>
                      <span className="font-medium">{o.orderNumber}</span> <span className="text-gray-500 text-xs">{o.status}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <Input {...register('carrier')} label="Carrier" placeholder="UPS, FedEx..." />
            <Input {...register('trackingNumber')} label="Tracking #" placeholder="Tracking number" />
            <Input {...register('freightCost')} type="number" step="0.01" min="0" label="Freight Cost" />
          </div>

          {/* Lines */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <h4 className="text-sm font-medium text-gray-900 dark:text-white">Shipment Lines</h4>
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
                    <th className="px-2 py-2 text-right text-xs font-medium uppercase text-gray-500">UOM</th>
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
                          <select {...register(`lines.${idx}.itemId`)} onChange={(e) => { register(`lines.${idx}.itemId`).onChange(e); const itemId = e.target.value; if (itemId) { const item = items.find((i: ItemSummary) => i.id === itemId); const baseUom = item?.baseUnitOfMeasure || 'EA'; setLineUomOptions(prev => ({ ...prev, [idx]: [{ value: baseUom, label: baseUom + ' (base)' }] })); void getItemUomConversions(itemId).then((convs: UomConversionDto[]) => { const opts = [{ value: baseUom, label: baseUom + ' (base)' }]; for (const c of convs) { if (c.fromUOM === baseUom) opts.push({ value: c.toUOM, label: `${c.toUOM} (${c.conversionFactor}x)` }); } setLineUomOptions(prev => ({ ...prev, [idx]: opts })); }).catch(() => {}); } }} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                            <option value="">Select...</option>
                            {itemOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                          </select>
                        </td>
                        <td className="px-2 py-1.5"><input {...register(`lines.${idx}.description`)} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" /></td>
                        <td className="px-2 py-1.5"><input type="number" step="0.01" {...register(`lines.${idx}.quantity`)} className="w-20 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" /></td>
                        <td className="px-2 py-1.5"><input type="number" step="0.01" {...register(`lines.${idx}.unitPrice`)} className="w-24 text-sm text-right rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 tabular-nums" /></td>
                        <td className="px-2 py-1.5"><UomSelect {...register(`lines.${idx}.unitOfMeasure`)} className="w-20 text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-1 py-1" /></td>
                        <td className="px-2 py-1.5 text-right text-sm font-medium tabular-nums">{formatCurrency(total)}</td>
                        <td className="px-2 py-1.5">{fields.length > 1 && <button type="button" onClick={() => remove(idx)} className="text-red-500 hover:text-red-700"><Trash2 className="h-3.5 w-3.5" /></button>}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>

          {/* Totals */}
          <div className="flex justify-end">
            <div className="w-72 space-y-1">
              <div className="flex justify-between text-sm"><span className="text-gray-500">Subtotal:</span><span className="tabular-nums">{formatCurrency(totals.subtotal)}</span></div>
              <div className="flex justify-between text-sm"><span className="text-gray-500">Freight:</span><span className="tabular-nums">{formatCurrency(totals.freight)}</span></div>
              <div className="border-t pt-1 flex justify-between font-bold"><span>Grand Total:</span><span className="tabular-nums">{formatCurrency(totals.grandTotal)}</span></div>
            </div>
          </div>

          <Textarea {...register('notes')} label="Notes" placeholder="Shipment notes..." rows={2} />
        </form>
      </Modal>

      {/* List */}
      <Card>
        <CardHeader title="Shipments" description={`${shipments.length} shipment(s)`}
          action={<Button variant="primary" size="sm" onClick={() => { setErr(null); setCustomerSearch(''); setSoSearch(''); setEditingShipment(null); reset({ shipmentNumber: `SHP-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}`, customerId: '', salesOrderId: '', shipmentDate: new Date().toISOString().slice(0, 10), carrier: '', trackingNumber: '', freightCost: 0, notes: '', lines: [{ lineNumber: 1, itemId: '', description: '', quantity: 1, unitPrice: 0, unitOfMeasure: 'EA', discountPercent: 0, taxPercent: 0 }] }); setShowCreate(true) }} leftIcon={<Plus className="h-4 w-4" />}>New Shipment</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by shipment #..." leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading...</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No shipments yet.'}</p> :
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Shipment #</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500">SO</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {filtered.map((s: ShipmentSummary) => (
                      <tr key={s.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{s.shipmentNumber}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{customers.find((c: ArCustomer) => c.id === s.customerId)?.name ?? s.customerId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(s.shipmentDate).toLocaleDateString()}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{s.salesOrderId ? (salesOrders.find((o: SalesOrderSummary) => o.id === s.salesOrderId)?.orderNumber ?? s.salesOrderId.slice(0, 8)) : '—'}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{formatCurrency(s.totalAmount)}</td>
                        <td className="px-3 py-3"><Badge variant={s.status === 'Confirmed' ? 'success' : s.status === 'Draft' ? 'neutral' : 'info'} size="sm" dot>{s.status}</Badge></td>
                        <td className="px-3 py-3 text-right">
                          <div className="flex justify-end gap-1">
                            <Button size="sm" variant="ghost" onClick={() => navigate(`/om/shipments/${s.id}`)}><Eye className="h-4 w-4" /></Button>
                            {s.status === 'Draft' && (
                              <>
                                <Button size="sm" variant="ghost" onClick={async () => {
                                  try {
                                    const detail = await getShipment(s.id)
                                    setEditingShipment(s)
                                    reset({
                                      shipmentNumber: detail.shipmentNumber,
                                      customerId: detail.customerId,
                                      salesOrderId: detail.salesOrderId ?? '',
                                      shipmentDate: detail.shipmentDate.slice(0, 10),
                                      carrier: detail.carrier ?? '',
                                      trackingNumber: detail.trackingNumber ?? '',
                                      freightCost: detail.freightCost,
                                      notes: (detail as any).notes ?? '',
                                      lines: detail.lines.map(l => ({
                                        lineNumber: l.lineNumber,
                                        itemId: l.itemId,
                                        description: l.description,
                                        quantity: l.quantity,
                                        unitPrice: l.unitPrice,
                                        unitOfMeasure: l.unitOfMeasure,
                                        warehouseId: l.warehouseId ?? undefined,
                                        salesOrderLineId: l.salesOrderLineId ?? undefined,
                                        projectId: l.projectId ?? undefined,
                                        accountId: l.accountId ?? undefined,
                                        discountPercent: l.discountPercent,
                                        taxPercent: l.taxPercent,
                                      })),
                                    })
                                    setCustomerSearch(customers.find(c => c.id === detail.customerId)?.name ?? '')
                                    setSoSearch(detail.salesOrderId ? salesOrders.find(o => o.id === detail.salesOrderId)?.orderNumber ?? '' : '')
                                    setShowCreate(true)
                                  } catch (e) { setErr(getErrorMessage(e)) }
                                }}><Pencil className="h-4 w-4" /></Button>
                                <Button size="sm" variant="ghost" onClick={() => { if (confirm(`Delete shipment ${s.shipmentNumber}?`)) deleteMut.mutate(s.id) }}><Trash2 className="h-4 w-4 text-red-500" /></Button>
                                <Button size="sm" variant="primary" disabled={confirmMut.isPending || s.status !== 'Draft'} onClick={() => confirmMut.mutate(s.id)}>Confirm</Button>
                              </>
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
