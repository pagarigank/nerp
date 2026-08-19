import { useState } from 'react'
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
import { getRevaluations, createRevaluation, approveRevaluation, postRevaluation, companyId } from '@api/inventory'

const methodOptions = [
  { value: 'StandardCost', label: 'Standard Cost Update' },
  { value: 'AverageCost', label: 'Average Cost Recalculation' },
  { value: 'FIFOAdjustment', label: 'FIFO Layer Adjustment' },
]

const schema = z.object({
  revaluationDate: z.string().min(1, 'Date is required'),
  method: z.string().min(1, 'Method is required'),
  notes: z.string().optional(),
})
type Form = z.infer<typeof schema>

function fieldError(msg?: string) { return msg ? { error: msg } : {} }
function money(n: number) { return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n) }

export function RevaluationPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)

  const { register, handleSubmit, reset, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: { revaluationDate: new Date().toISOString().slice(0, 10), method: 'StandardCost', notes: '' },
  })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'revaluations'], queryFn: () => getRevaluations() })

  const createMut = useMutation({
    mutationFn: (d: Form) => createRevaluation({ companyId: companyId(), revaluationDate: d.revaluationDate, method: d.method, notes: d.notes || null }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'revaluations'] }); setShowCreate(false); reset() },
    onError: e => setErr(getErrorMessage(e)),
  })
  const approve = useMutation({ mutationFn: (id: string) => approveRevaluation(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'revaluations'] }), onError: e => setErr(getErrorMessage(e)) })
  const post = useMutation({ mutationFn: (id: string) => postRevaluation(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'revaluations'] }), onError: e => setErr(getErrorMessage(e)) })

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"><AlertCircle className="h-5 w-5" /> {err}</div>}

      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)} title="New Revaluation" size="lg"
        footer={<><Button variant="secondary" onClick={() => setShowCreate(false)} disabled={createMut.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(d => createMut.mutate(d))} isLoading={createMut.isPending}>Create Revaluation</Button></>}>
        <form className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-4">
            <Input {...register('revaluationDate')} type="date" label="Revaluation Date" {...fieldError(errors.revaluationDate?.message)} required />
            <Select {...register('method')} label="Method" options={methodOptions} {...fieldError(errors.method?.message)} required />
          </div>
          <Textarea {...register('notes')} label="Notes" placeholder="Revaluation notes..." rows={2} />
          <p className="text-xs text-gray-500">After creation, use "Generate Lines" to auto-populate items for revaluation based on the selected method.</p>
        </form>
      </Modal>

      <Card>
        <CardHeader title="Item Revaluation" description={`${rows.length} revaluation(s)`}
          action={<Button variant="primary" size="sm" onClick={() => { setErr(null); reset(); setShowCreate(true) }} leftIcon={<Plus className="h-4 w-4" />}>New Revaluation</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading...</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No revaluations.</p> :
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Number</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Method</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Adj. Value</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {rows.map((r) => (
                      <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.revaluationNumber}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.revaluationDate).toLocaleDateString()}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.method}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{money(r.totalAdjustmentValue)}</td>
                        <td className="px-3 py-3"><Badge variant={r.status === 'Posted' ? 'success' : r.status === 'Approved' ? 'warning' : 'neutral'} size="sm" dot>{r.status}</Badge></td>
                        <td className="px-3 py-3 text-right">
                          <div className="flex justify-end gap-1">
                            {r.status === 'Draft' && <Button size="sm" variant="outline" disabled={approve.isPending} onClick={() => approve.mutate(r.id)}>Approve</Button>}
                            {r.status === 'Approved' && <Button size="sm" variant="primary" disabled={post.isPending} onClick={() => post.mutate(r.id)}>Post</Button>}
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
