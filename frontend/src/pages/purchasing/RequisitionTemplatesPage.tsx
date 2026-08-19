import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getRequisitionTemplates, createRequisitionTemplate, updateRequisitionTemplate, companyId } from '@api/purchasing'
import type { CreateRequisitionTemplateRequest, RequisitionTemplate } from '@/types/purchasing'

export function RequisitionTemplatesPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateRequisitionTemplateRequest>({
    companyId: companyId(), templateCode: '', templateName: '', description: null, isActive: true,
    lines: [{ lineNumber: 1, itemId: null, description: '', defaultQuantity: 1, unitOfMeasure: 'EA', accountId: null, projectId: null }],
  })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['purchasing', 'requisition-templates'], queryFn: () => getRequisitionTemplates() })
  const createMut = useMutation({ mutationFn: (d: CreateRequisitionTemplateRequest) => createRequisitionTemplate(d), onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'requisition-templates'] }); close() }, onError: e => setFormError(getErrorMessage(e)) })
  const toggleMut = useMutation({ mutationFn: (r: RequisitionTemplate) => updateRequisitionTemplate(r.id, { isActive: !r.isActive }), onSuccess: () => qc.invalidateQueries({ queryKey: ['purchasing', 'requisition-templates'] }), onError: e => setFormError(getErrorMessage(e)) })

  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); setForm({ companyId: companyId(), templateCode: '', templateName: '', description: null, isActive: true, lines: [{ lineNumber: 1, itemId: null, description: '', defaultQuantity: 1, unitOfMeasure: 'EA', accountId: null, projectId: null }] }); setOpen(true) }
  const submit = () => { setFormError(null); if (!form.templateCode || !form.templateName) { setFormError('Template Code and Name are required'); return } createMut.mutate(form) }
  const set = (k: keyof CreateRequisitionTemplateRequest, v: string | number | null | boolean) => setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="Requisition Templates" description={`${rows.length} recurring requisition template(s)`} action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No requisition templates yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Code</th><th className="px-3 py-2 font-medium text-gray-500">Name</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Lines</th><th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: RequisitionTemplate) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.templateCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.templateName}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.lineCount}</td>
                      <td className="px-3 py-3">
                        <button onClick={() => toggleMut.mutate(r)} className="focus:outline-none">
                          <Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge>
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Modal isOpen={open} onClose={close} title="New Requisition Template"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <Input value={form.templateCode} onChange={e => set('templateCode', e.target.value)} label="Template Code" required />
          <Input value={form.templateName} onChange={e => set('templateName', e.target.value)} label="Template Name" required />
          <Input value={form.description ?? ''} onChange={e => set('description', e.target.value || null)} label="Description" />
          <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
            <input type="checkbox" checked={form.isActive} onChange={e => set('isActive', e.target.checked)} /> Active
          </label>
        </div>
      </Modal>
    </div>
  )
}
