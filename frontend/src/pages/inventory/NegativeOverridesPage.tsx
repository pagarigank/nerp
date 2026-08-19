import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { UomSelect } from '@components/ui/UomSelect'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getNegativeOverrides, createNegativeOverride, approveNegativeOverride, rejectNegativeOverride, companyId, getItems, getWarehouses } from '@api/inventory'
import type { NegativeOverrideSummary, ItemSummary, WarehouseSummary, UomConversionDto } from '@/types/inventory'

const schema = z.object({
  itemId: z.string().min(1, 'Item is required'),
  warehouseId: z.string().min(1, 'Warehouse is required'),
  requestedQuantity: z.coerce.number().positive('Qty must be > 0'),
  unitOfMeasure: z.string().min(1, 'UOM is required'),
  reason: z.string().min(1, 'Reason is required'),
  referenceNumber: z.string().optional(),
})
type Form = z.infer<typeof schema>

function fieldError(msg?: string) { return msg ? { error: msg } : {} }

export function NegativeOverridesPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  const [uomOptions, setUomOptions] = useState<{ value: string; label: string }[]>([])
  const { register, handleSubmit, watch, reset, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: { itemId: '', warehouseId: '', requestedQuantity: 1, unitOfMeasure: 'EA', reason: '', referenceNumber: '' },
  })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'negative-overrides'], queryFn: () => getNegativeOverrides() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })

  const itemOptions = useMemo(() => items.map((i: ItemSummary) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])
  const whOptions = useMemo(() => warehouses.map((w: WarehouseSummary) => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` })), [warehouses])

  const createMut = useMutation({
    mutationFn: (d: Form) => createNegativeOverride({ companyId: companyId(), itemId: d.itemId, warehouseId: d.warehouseId, requestedQuantity: d.requestedQuantity, unitOfMeasure: d.unitOfMeasure, reason: d.reason, referenceNumber: d.referenceNumber || null }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'negative-overrides'] }); setShowCreate(false); reset() },
    onError: e => setErr(getErrorMessage(e)),
  })
  const approve = useMutation({ mutationFn: (id: string) => approveNegativeOverride(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'negative-overrides'] }), onError: e => setErr(getErrorMessage(e)) })
  const reject = useMutation({ mutationFn: (id: string) => rejectNegativeOverride(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'negative-overrides'] }), onError: e => setErr(getErrorMessage(e)) })

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"><AlertCircle className="h-5 w-5" /> {err}</div>}

      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)} title="Request Negative Override" size="lg"
        footer={<><Button variant="secondary" onClick={() => setShowCreate(false)} disabled={createMut.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(d => createMut.mutate(d))} isLoading={createMut.isPending}>Submit Request</Button></>}>
        <form className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-4">
            <Select {...register('itemId')} label="Item" options={itemOptions} placeholder="Select item..." {...fieldError(errors.itemId?.message)} required onChange={(e) => { register('itemId').onChange(e); const itemId = e.target.value; if (itemId) { const item = items.find((i: ItemSummary) => i.id === itemId); const baseUom = item?.baseUnitOfMeasure || 'EA'; setUomOptions([{ value: baseUom, label: baseUom + ' (base)' }]); void getItemUomConversions(itemId).then((convs: UomConversionDto[]) => { const opts = [{ value: baseUom, label: baseUom + ' (base)' }]; for (const c of convs) { if (c.fromUOM === baseUom) opts.push({ value: c.toUOM, label: `${c.toUOM} (${c.conversionFactor}x)` }); } setUomOptions(opts); }).catch(() => {}); } }} />
            <Select {...register('warehouseId')} label="Warehouse" options={whOptions} placeholder="Select warehouse..." {...fieldError(errors.warehouseId?.message)} required />
            <Input {...register('requestedQuantity')} type="number" step="0.01" min="0.01" label="Requested Quantity" {...fieldError(errors.requestedQuantity?.message)} required />
            <UomSelect {...register('unitOfMeasure')} />
            <Input {...register('referenceNumber')} label="Reference #" placeholder="SO / Project #" />
          </div>
          <Textarea {...register('reason')} label="Reason" placeholder="Why is negative inventory needed?" rows={2} {...fieldError(errors.reason?.message)} />
        </form>
      </Modal>

      <Card>
        <CardHeader title="Negative Inventory Overrides" description={`${rows.length} request(s)`}
          action={<Button variant="primary" size="sm" onClick={() => { setErr(null); reset(); setShowCreate(true) }} leftIcon={<Plus className="h-4 w-4" />}>New Request</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading...</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No override requests.</p> :
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Item</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Qty</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Reason</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Requested By</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {rows.map((r: NegativeOverrideSummary) => (
                      <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.itemId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{r.requestedQuantity}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.reason}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.requestedBy}</td>
                        <td className="px-3 py-3"><Badge variant={r.status === 'Approved' ? 'success' : r.status === 'Rejected' ? 'error' : 'warning'} size="sm" dot>{r.status}</Badge></td>
                        <td className="px-3 py-3 text-right">
                          <div className="flex justify-end gap-1">
                            {r.status === 'Pending' && <Button size="sm" variant="primary" disabled={approve.isPending} onClick={() => approve.mutate(r.id)}>Approve</Button>}
                            {r.status === 'Pending' && <Button size="sm" variant="ghost" className="text-red-600" disabled={reject.isPending} onClick={() => reject.mutate(r.id)}>Reject</Button>}
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
