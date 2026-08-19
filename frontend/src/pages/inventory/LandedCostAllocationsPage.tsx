import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getLandedCostAllocations, createLandedCostAllocation, autoAllocateLandedCost, postLandedCostAllocation, cancelLandedCostAllocation, companyId } from '@api/inventory'
import type { CreateLandedCostAllocationRequest, LandedCostAllocationDto } from '@/types/inventory'

export function LandedCostAllocationsPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateLandedCostAllocationRequest>({
    companyId: companyId(), receiptTransactionId: '', allocationNumber: '', allocationDate: new Date().toISOString().slice(0, 10), notes: null,
    lines: [{ itemId: '', quantityReceived: 1, unitCost: 0, allocationMethod: 'ByQuantity', allocatedAmount: 0, landedCostId: null, description: null }],
  })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'landed-cost-allocations'], queryFn: () => getLandedCostAllocations() })
  const createMut = useMutation({ mutationFn: (d: CreateLandedCostAllocationRequest) => createLandedCostAllocation(d), onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'landed-cost-allocations'] }); close() }, onError: e => setFormError(getErrorMessage(e)) })
  const actMut = useMutation({
    mutationFn: (p: { id: string; op: 'auto' | 'post' | 'cancel' }) => {
      if (p.op === 'auto') return autoAllocateLandedCost(p.id)
      if (p.op === 'post') return postLandedCostAllocation(p.id)
      return cancelLandedCostAllocation(p.id)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'landed-cost-allocations'] }),
    onError: e => setFormError(getErrorMessage(e)),
  })

  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); setForm({ companyId: companyId(), receiptTransactionId: '', allocationNumber: '', allocationDate: new Date().toISOString().slice(0, 10), notes: null, lines: [{ itemId: '', quantityReceived: 1, unitCost: 0, allocationMethod: 'ByQuantity', allocatedAmount: 0, landedCostId: null, description: null }] }); setOpen(true) }
  const submit = () => { setFormError(null); if (!form.receiptTransactionId || !form.allocationNumber) { setFormError('Receipt Transaction ID and Allocation # are required'); return } createMut.mutate(form) }
  const set = (k: keyof CreateLandedCostAllocationRequest, v: string | null) => setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="Landed Cost Allocations" description={`${rows.length} allocation(s) — allocate freight/duty/insurance to receipt costs`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No allocations yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">#</th><th className="px-3 py-2 font-medium text-gray-500">Receipt Tx</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Allocated</th><th className="px-3 py-2 font-medium text-gray-500">Status</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: LandedCostAllocationDto) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.allocationNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.receiptTransactionId.slice(0, 8)}…</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.totalAllocatedCost.toFixed(2)}</td>
                      <td className="px-3 py-3"><Badge variant={r.status === 'Posted' ? 'success' : r.status === 'Cancelled' ? 'error' : 'neutral'} size="sm" dot>{r.status}</Badge></td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          {r.status === 'Draft' && <><Button size="sm" variant="outline" disabled={actMut.isPending} onClick={() => actMut.mutate({ id: r.id, op: 'auto' })}>Auto</Button>
                            <Button size="sm" variant="primary" disabled={actMut.isPending} onClick={() => actMut.mutate({ id: r.id, op: 'post' })}>Post</Button></>}
                          {r.status === 'Draft' && <Button size="sm" variant="ghost" className="text-red-600" disabled={actMut.isPending} onClick={() => actMut.mutate({ id: r.id, op: 'cancel' })}>Cancel</Button>}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Modal isOpen={open} onClose={close} title="New Landed Cost Allocation"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <Input value={form.receiptTransactionId} onChange={e => set('receiptTransactionId', e.target.value)} label="Receipt Transaction ID" placeholder="Guid" required />
          <Input value={form.allocationNumber} onChange={e => set('allocationNumber', e.target.value)} label="Allocation #" required />
          <Input type="date" value={form.allocationDate} onChange={e => set('allocationDate', e.target.value)} label="Allocation Date" required />
          <Input value={form.notes ?? ''} onChange={e => set('notes', e.target.value || null)} label="Notes" />
        </div>
      </Modal>
    </div>
  )
}
