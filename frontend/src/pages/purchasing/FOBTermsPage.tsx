import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getFOBTerms, createFOBTerm, companyId } from '@api/purchasing'
import type { CreateFOBTermRequest } from '@/types/purchasing'

export function FOBTermsPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateFOBTermRequest>({ companyId: companyId(), code: '', description: '', freightResponsibility: null })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['purchasing', 'fob-terms'], queryFn: () => getFOBTerms() })
  const createMut = useMutation({ mutationFn: (d: CreateFOBTermRequest) => createFOBTerm(d), onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'fob-terms'] }); close() }, onError: e => setFormError(getErrorMessage(e)) })
  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); setForm({ companyId: companyId(), code: '', description: '', freightResponsibility: null }); setOpen(true) }
  const submit = () => { setFormError(null); if (!form.code || !form.description) { setFormError('Code and Description are required'); return } createMut.mutate(form) }
  const set = (k: keyof CreateFOBTermRequest, v: string | null) => setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="FOB Terms" description={`${rows.length} term(s)`} action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No FOB terms yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Code</th><th className="px-3 py-2 font-medium text-gray-500">Description</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Freight Responsibility</th><th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.code}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.description}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.freightResponsibility ?? '—'}</td>
                      <td className="px-3 py-3"><Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
      <Modal isOpen={open} onClose={close} title="New FOB Term"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <Input value={form.code} onChange={e => set('code', e.target.value)} label="Code" required />
          <Input value={form.description} onChange={e => set('description', e.target.value)} label="Description" required />
          <Input value={form.freightResponsibility ?? ''} onChange={e => set('freightResponsibility', e.target.value || null)} label="Freight Responsibility" />
        </div>
      </Modal>
    </div>
  )
}
