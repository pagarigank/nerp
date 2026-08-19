import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getQuarantine, createQuarantine, releaseQuarantine, disposeQuarantine, companyId, getItems, getWarehouses } from '@api/inventory'
import type { QuarantineSummary, ItemSummary, WarehouseSummary } from '@/types/inventory'

const schema = z.object({
  itemId: z.string().min(1, 'Item is required'),
  warehouseId: z.string().min(1, 'Warehouse is required'),
  quantity: z.coerce.number().positive('Qty must be > 0'),
  reason: z.string().min(1, 'Reason is required'),
  notes: z.string().optional(),
})
type Form = z.infer<typeof schema>

const reasonOptions = [
  { value: 'Quality Hold', label: 'Quality Hold' },
  { value: 'Damage', label: 'Damage' },
  { value: 'Recall', label: 'Recall' },
  { value: 'Pending Inspection', label: 'Pending Inspection' },
  { value: 'Other', label: 'Other' },
]

function fieldError(msg?: string) { return msg ? { error: msg } : {} }

export function QuarantinePage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  const { register, handleSubmit, reset, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: { itemId: '', warehouseId: '', quantity: 1, reason: '', notes: '' },
  })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'quarantine'], queryFn: () => getQuarantine() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })

  const itemOptions = useMemo(() => items.map((i: ItemSummary) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])
  const whOptions = useMemo(() => warehouses.map((w: WarehouseSummary) => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` })), [warehouses])

  const createMut = useMutation({
    mutationFn: (d: Form) => createQuarantine({ companyId: companyId(), itemId: d.itemId, warehouseId: d.warehouseId, quantity: d.quantity, reason: d.reason, notes: d.notes || null }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'quarantine'] }); setShowCreate(false); reset() },
    onError: e => setErr(getErrorMessage(e)),
  })
  const release = useMutation({ mutationFn: (id: string) => releaseQuarantine(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'quarantine'] }), onError: e => setErr(getErrorMessage(e)) })
  const dispose = useMutation({ mutationFn: (id: string) => disposeQuarantine(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'quarantine'] }), onError: e => setErr(getErrorMessage(e)) })

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"><AlertCircle className="h-5 w-5" /> {err}</div>}

      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)} title="New Quarantine Hold" size="lg"
        footer={<><Button variant="secondary" onClick={() => setShowCreate(false)} disabled={createMut.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(d => createMut.mutate(d))} isLoading={createMut.isPending}>Create Hold</Button></>}>
        <form className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-4">
            <Select {...register('itemId')} label="Item" options={itemOptions} placeholder="Select item..." {...fieldError(errors.itemId?.message)} required />
            <Select {...register('warehouseId')} label="Warehouse" options={whOptions} placeholder="Select warehouse..." {...fieldError(errors.warehouseId?.message)} required />
            <Input {...register('quantity')} type="number" step="0.01" min="0.01" label="Quantity" {...fieldError(errors.quantity?.message)} required />
            <Select {...register('reason')} label="Reason" options={reasonOptions} placeholder="Select reason..." {...fieldError(errors.reason?.message)} required />
          </div>
          <Textarea {...register('notes')} label="Notes" placeholder="Additional details..." rows={2} />
        </form>
      </Modal>

      <Card>
        <CardHeader title="Quarantine (Quality Holds)" description={`${rows.length} held item(s)`}
          action={<Button variant="primary" size="sm" onClick={() => { setErr(null); reset(); setShowCreate(true) }} leftIcon={<Plus className="h-4 w-4" />}>New Hold</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading...</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No quarantine records.</p> :
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Item</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Qty</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Reason</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {rows.map((r: QuarantineSummary) => (
                      <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.itemId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{r.quantity}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.reason}</td>
                        <td className="px-3 py-3"><Badge variant={r.status === 'Released' ? 'success' : r.status === 'Disposed' ? 'error' : 'warning'} size="sm" dot>{r.status}</Badge></td>
                        <td className="px-3 py-3 text-right">
                          <div className="flex justify-end gap-1">
                            {r.status === 'Held' && <Button size="sm" variant="outline" disabled={release.isPending} onClick={() => release.mutate(r.id)}>Release</Button>}
                            {r.status === 'Held' && <Button size="sm" variant="ghost" className="text-red-600" disabled={dispose.isPending} onClick={() => dispose.mutate(r.id)}>Dispose</Button>}
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
