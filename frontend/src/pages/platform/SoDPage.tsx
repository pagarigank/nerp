import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getSoDRules, createSoDRule, getSoDConflicts } from '@api/platform'
import type { SoDRule, CreateSoDRuleRequest, SoDConflict } from '@/types/platform'

function ConflictList() {
  const { data = [], isLoading } = useQuery({ queryKey: ['platform', 'sod', 'conflicts'], queryFn: () => getSoDConflicts() })
  if (isLoading) return <p className="text-sm text-gray-500 py-4 text-center">Loading…</p>
  if (data.length === 0) return <p className="text-sm text-gray-500 py-4 text-center">No conflicts detected.</p>
  return (
    <div className="overflow-x-auto"><table className="w-full text-sm">
      <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
        <th className="px-3 py-2 font-medium text-gray-500">User</th><th className="px-3 py-2 font-medium text-gray-500">Module</th>
        <th className="px-3 py-2 font-medium text-gray-500">Document</th><th className="px-3 py-2 font-medium text-gray-500">Type</th>
        <th className="px-3 py-2 font-medium text-gray-500">Status</th>
      </tr></thead>
      <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
        {data.map((c: SoDConflict) => (
          <tr key={c.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
            <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{c.userId.slice(0, 8)}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{c.module}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{c.documentType}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{c.conflictType}</td>
            <td className="px-3 py-3"><Badge variant={c.resolved ? 'success' : 'error'} size="sm" dot>{c.resolved ? 'Resolved' : 'Open'}</Badge></td>
          </tr>
        ))}
      </tbody>
    </table></div>
  )
}

export function SoDPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateSoDRuleRequest>({ module: 'Purchasing', actionA: 'Create', actionB: 'Approve', description: '', documentType: null, thresholdAmount: null })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['platform', 'sod', 'rules'], queryFn: () => getSoDRules() })
  const createMut = useMutation({ mutationFn: (d: CreateSoDRuleRequest) => createSoDRule(d), onSuccess: () => { qc.invalidateQueries({ queryKey: ['platform', 'sod', 'rules'] }); close() }, onError: e => setFormError(getErrorMessage(e)) })
  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); setForm({ module: 'Purchasing', actionA: 'Create', actionB: 'Approve', description: '', documentType: null, thresholdAmount: null }); setOpen(true) }
  const submit = () => { setFormError(null); if (!form.module || !form.actionA || !form.actionB || !form.description) { setFormError('Module, Action A, Action B and Description are required'); return } createMut.mutate(form) }
  const set = (k: keyof CreateSoDRuleRequest, v: string | null) => setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="Segregation of Duties Rules" description={`${rows.length} rule(s) — blocks same-user create+approve above threshold`} action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New Rule</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No SoD rules yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Module</th><th className="px-3 py-2 font-medium text-gray-500">Action A</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Action B</th><th className="px-3 py-2 font-medium text-gray-500">Description</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: SoDRule) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.module}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.actionA}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.actionB}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.description}</td>
                      <td className="px-3 py-3"><Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Detected Conflicts" description="Segregation-of-duties conflicts flagged by the engine" />
        <CardContent><ConflictList /></CardContent>
      </Card>

      <Modal isOpen={open} onClose={close} title="New SoD Rule"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input value={form.module} onChange={e => set('module', e.target.value)} label="Module" required />
            <Input value={form.documentType ?? ''} onChange={e => set('documentType', e.target.value || null)} label="Document Type" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Input value={form.actionA} onChange={e => set('actionA', e.target.value)} label="Action A" required />
            <Input value={form.actionB} onChange={e => set('actionB', e.target.value)} label="Action B" required />
          </div>
          <Input value={form.description} onChange={e => set('description', e.target.value)} label="Description" required />
        </div>
      </Modal>
    </div>
  )
}
