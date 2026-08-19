import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getNumberSequences, createNumberSequence, companyId } from '@api/platform'
import type { NumberSequence, CreateNumberSequenceRequest } from '@/types/platform'

export function NumberSequencesPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateNumberSequenceRequest>({ companyId: companyId(), name: '', prefix: '', nextValue: 1, increment: 1, minValue: 1, maxValue: 9999999 })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['platform', 'number-sequences'], queryFn: () => getNumberSequences() })
  const createMut = useMutation({ mutationFn: (d: CreateNumberSequenceRequest) => createNumberSequence(d), onSuccess: () => { qc.invalidateQueries({ queryKey: ['platform', 'number-sequences'] }); close() }, onError: e => setFormError(getErrorMessage(e)) })
  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); setForm({ companyId: companyId(), name: '', prefix: '', nextValue: 1, increment: 1, minValue: 1, maxValue: 9999999 }); setOpen(true) }
  const submit = () => { setFormError(null); if (!form.name || !form.prefix) { setFormError('Name and Prefix are required'); return } createMut.mutate(form) }
  const set = (k: keyof CreateNumberSequenceRequest, v: string | number) => setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="Number Sequences" description={`${rows.length} sequence(s)`} action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No number sequences yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Name</th><th className="px-3 py-2 font-medium text-gray-500">Prefix</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Next</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Increment</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: NumberSequence) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.name}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.prefix}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.nextValue}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.increment}</td>
                      <td className="px-3 py-3"><Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
      <Modal isOpen={open} onClose={close} title="New Number Sequence"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input value={form.name} onChange={e => set('name', e.target.value)} label="Name" placeholder="Invoice" required />
            <Input value={form.prefix} onChange={e => set('prefix', e.target.value)} label="Prefix" placeholder="INV-" required />
            <Input type="number" min="0" value={String(form.nextValue)} onChange={e => set('nextValue', Number(e.target.value))} label="Next Value" />
            <Input type="number" min="1" value={String(form.increment)} onChange={e => set('increment', Number(e.target.value))} label="Increment" />
            <Input type="number" min="0" value={String(form.minValue)} onChange={e => set('minValue', Number(e.target.value))} label="Min Value" />
            <Input type="number" min="0" value={String(form.maxValue)} onChange={e => set('maxValue', Number(e.target.value))} label="Max Value" />
          </div>
        </div>
      </Modal>
    </div>
  )
}
