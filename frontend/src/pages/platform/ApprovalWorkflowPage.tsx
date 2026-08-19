import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle, Check, Trash2, ChevronDown, ChevronRight, ListOrdered } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getApprovalWorkflows,
  createApprovalWorkflow,
  activateApprovalWorkflow,
  deactivateApprovalWorkflow,
  addApprovalStep,
  removeApprovalStep,
} from '@api/platform'
import type { ApprovalWorkflow, ApprovalStep, CreateApprovalWorkflowRequest, CreateApprovalStepRequest } from '@/types/platform'

const emptyStep = (): CreateApprovalStepRequest => ({
  stepOrder: 1,
  description: '',
  approverRoleId: null,
  specificApproverUserId: null,
  requiredApprovals: 1,
  minAmount: null,
  maxAmount: null,
})

export function ApprovalWorkflowPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [expanded, setExpanded] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateApprovalWorkflowRequest>({
    module: 'Purchasing',
    documentType: 'PurchaseOrder',
    description: '',
    companyId: null,
    thresholdAmount: null,
    steps: [{ ...emptyStep(), stepOrder: 1, description: 'Manager Approval' }],
  })

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['platform', 'approval-workflows'],
    queryFn: () => getApprovalWorkflows(),
  })

  const createMut = useMutation({
    mutationFn: (d: CreateApprovalWorkflowRequest) => createApprovalWorkflow(d),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['platform', 'approval-workflows'] })
      close()
    },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const toggleMut = useMutation({
    mutationFn: (p: { id: string; op: 'activate' | 'deactivate' }) =>
      p.op === 'activate' ? activateApprovalWorkflow(p.id) : deactivateApprovalWorkflow(p.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'approval-workflows'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const addStepMut = useMutation({
    mutationFn: (p: { id: string; step: CreateApprovalStepRequest }) => addApprovalStep(p.id, p.step),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'approval-workflows'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const removeStepMut = useMutation({
    mutationFn: (p: { id: string; stepId: string }) => removeApprovalStep(p.id, p.stepId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'approval-workflows'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const close = () => {
    setOpen(false)
    setFormError(null)
  }
  const openForm = () => {
    setFormError(null)
    setForm({
      module: 'Purchasing',
      documentType: 'PurchaseOrder',
      description: '',
      companyId: null,
      thresholdAmount: null,
      steps: [{ ...emptyStep(), stepOrder: 1, description: 'Manager Approval' }],
    })
    setOpen(true)
  }
  const set = (k: keyof CreateApprovalWorkflowRequest, v: string | number | null) =>
    setForm((f) => ({ ...f, [k]: v }))
  const submit = () => {
    setFormError(null)
    if (!form.module || !form.documentType || !form.description) {
      setFormError('Module, Document Type and Description are required')
      return
    }
    createMut.mutate({
      ...form,
      steps: form.steps.map((s, i) => ({ ...s, stepOrder: i + 1 })),
    })
  }

  const toggleExpand = (id: string) => setExpanded((cur) => (cur === id ? null : id))

  const addStep = (wf: ApprovalWorkflow) => {
    const nextOrder = (wf.steps?.length ?? 0) + 1
    addStepMut.mutate({
      id: wf.id,
      step: { ...emptyStep(), stepOrder: nextOrder, description: `Approval Step ${nextOrder}` },
    })
  }
  const removeStep = (wf: ApprovalWorkflow, step: ApprovalStep) => {
    if (wf.steps.length <= 1) {
      setFormError('A workflow must keep at least one step.')
      return
    }
    removeStepMut.mutate({ id: wf.id, stepId: step.id })
  }

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader
          title="Approval Workflow Designer"
          description={`${rows.length} workflow(s) — define multi-step approval routing`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>
              New
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-gray-500 py-8 text-center">Loading…</p>
          ) : rows.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">No approval workflows yet.</p>
          ) : (
            <div className="space-y-3">
              {rows.map((r: ApprovalWorkflow) => (
                <div key={r.id} className="border border-gray-200 dark:border-gray-700 rounded-lg">
                  <div className="flex items-center justify-between px-3 py-3">
                    <button className="flex items-center gap-2 text-left" onClick={() => toggleExpand(r.id)}>
                      {expanded === r.id ? <ChevronDown className="h-4 w-4 text-gray-400" /> : <ChevronRight className="h-4 w-4 text-gray-400" />}
                      <div>
                        <p className="font-medium text-gray-900 dark:text-white">
                          {r.module} · {r.documentType}
                        </p>
                        <p className="text-sm text-gray-500">{r.description}</p>
                      </div>
                    </button>
                    <div className="flex items-center gap-2">
                      <Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>
                        {r.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                      {r.isActive ? (
                        <Button size="sm" variant="ghost" className="text-red-600" disabled={toggleMut.isPending} onClick={() => toggleMut.mutate({ id: r.id, op: 'deactivate' })}>
                          Deactivate
                        </Button>
                      ) : (
                        <Button size="sm" variant="primary" disabled={toggleMut.isPending} onClick={() => toggleMut.mutate({ id: r.id, op: 'activate' })}>
                          <Check className="h-3.5 w-3.5" /> Activate
                        </Button>
                      )}
                    </div>
                  </div>

                  {expanded === r.id && (
                    <div className="border-t border-gray-200 dark:border-gray-700 px-3 py-3 bg-gray-50 dark:bg-gray-800/40">
                      <div className="flex items-center justify-between mb-2">
                        <p className="text-sm font-medium text-gray-700 dark:text-gray-300 flex items-center gap-2">
                          <ListOrdered className="h-4 w-4" /> Steps ({r.steps.length})
                        </p>
                        <Button size="sm" variant="secondary" onClick={() => addStep(r)} disabled={addStepMut.isPending}>
                          <Plus className="h-3.5 w-3.5" /> Add Step
                        </Button>
                      </div>
                      <ol className="space-y-2">
                        {r.steps.map((s: ApprovalStep) => (
                          <li key={s.id} className="flex items-center justify-between gap-3 rounded-md bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 px-3 py-2">
                            <div>
                              <p className="text-sm font-medium text-gray-900 dark:text-white">
                                {s.stepOrder}. {s.description}
                              </p>
                              <p className="text-xs text-gray-500">
                                {s.approverRoleId ? 'Role approval' : s.specificApproverUserId ? 'Specific approver' : 'Any approver'}
                                {s.minAmount != null || s.maxAmount != null
                                  ? ` · ${s.minAmount ?? 0}–${s.maxAmount ?? '∞'}`
                                  : ''}
                              </p>
                            </div>
                            <Button size="sm" variant="ghost" className="text-red-600" disabled={removeStepMut.isPending} onClick={() => removeStep(r, s)}>
                              <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                          </li>
                        ))}
                      </ol>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={open}
        onClose={close}
        title="New Approval Workflow"
        footer={
          <>
            <Button variant="secondary" onClick={close} disabled={createMut.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={submit} isLoading={createMut.isPending}>
              Create
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input value={form.module} onChange={(e) => set('module', e.target.value)} label="Module" required />
            <Input value={form.documentType} onChange={(e) => set('documentType', e.target.value)} label="Document Type" required />
          </div>
          <Input value={form.description} onChange={(e) => set('description', e.target.value)} label="Description" required />
          <Input
            type="number"
            step="0.01"
            min="0"
            value={String(form.thresholdAmount ?? '')}
            onChange={(e) => set('thresholdAmount', e.target.value ? Number(e.target.value) : null)}
            label="Threshold Amount (optional)"
          />
          <div className="border-t pt-3 border-gray-200 dark:border-gray-700">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              {form.steps.length} step(s) — first step created; add more in the designer after saving.
            </p>
            {form.steps.map((s, i) => (
              <Input
                key={i}
                value={s.description}
                onChange={(e) =>
                  setForm((f) => ({
                    ...f,
                    steps: f.steps.map((st, j) => (j === i ? { ...st, description: e.target.value } : st)),
                  }))
                }
                label={`Step ${i + 1} description`}
              />
            ))}
          </div>
        </div>
      </Modal>
    </div>
  )
}
