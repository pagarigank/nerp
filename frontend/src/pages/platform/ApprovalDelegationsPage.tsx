import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle, Clock, Trash2, Ban } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getApprovalDelegations,
  createApprovalDelegation,
  revokeApprovalDelegation,
  deleteApprovalDelegation,
  getApprovalEscalations,
  createApprovalEscalation,
  deleteApprovalEscalation,
} from '@api/platform'
import type { ApprovalDelegation, CreateApprovalDelegationRequest, CreateApprovalEscalationRequest } from '@/types/platform'

export function ApprovalDelegationsPage() {
  const qc = useQueryClient()
  const [openDel, setOpenDel] = useState(false)
  const [openEsc, setOpenEsc] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [delegator, setDelegator] = useState('')
  const [delegate, setDelegate] = useState('')
  const [startsOn, setStartsOn] = useState('')
  const [endsOn, setEndsOn] = useState('')
  const [module, setModule] = useState('')

  const [wfId, setWfId] = useState('')
  const [stepOrder, setStepOrder] = useState('1')
  const [sla, setSla] = useState('1440')
  const [escRole, setEscRole] = useState('')
  const [escUser, setEscUser] = useState('')

  const { data: delegations = [], isLoading } = useQuery({
    queryKey: ['platform', 'approval-delegations'],
    queryFn: () => getApprovalDelegations(),
  })
  const { data: escalations = [] } = useQuery({
    queryKey: ['platform', 'approval-escalations'],
    queryFn: () => getApprovalEscalations(),
  })

  const createDelMut = useMutation({
    mutationFn: (d: CreateApprovalDelegationRequest) => createApprovalDelegation(d),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['platform', 'approval-delegations'] })
      setOpenDel(false)
    },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const revokeDelMut = useMutation({
    mutationFn: (id: string) => revokeApprovalDelegation(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'approval-delegations'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const deleteDelMut = useMutation({
    mutationFn: (id: string) => deleteApprovalDelegation(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'approval-delegations'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const createEscMut = useMutation({
    mutationFn: (d: CreateApprovalEscalationRequest) => createApprovalEscalation(d),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['platform', 'approval-escalations'] })
      setOpenEsc(false)
    },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const deleteEscMut = useMutation({
    mutationFn: (id: string) => deleteApprovalEscalation(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'approval-escalations'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const submitDel = () => {
    setFormError(null)
    if (!delegator || !delegate || !startsOn || !endsOn) {
      setFormError('Delegator, delegate, start and end are required')
      return
    }
    createDelMut.mutate({
      delegatorUserId: delegator,
      delegateUserId: delegate,
      startsOn: new Date(startsOn).toISOString(),
      endsOn: new Date(endsOn).toISOString(),
      module: module || null,
    })
  }
  const submitEsc = () => {
    setFormError(null)
    if (!wfId) {
      setFormError('Workflow id is required')
      return
    }
    createEscMut.mutate({
      workflowId: wfId,
      stepOrder: Number(stepOrder),
      slaMinutes: Number(sla),
      escalateToRoleId: escRole || null,
      escalateToUserId: escUser || null,
    })
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
          title="Approval Delegations"
          description="Temporary approver substitution (vacation, leave)"
          action={
            <Button variant="primary" size="sm" onClick={() => { setFormError(null); setOpenDel(true) }} leftIcon={<Plus className="h-4 w-4" />}>
              New Delegation
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-gray-500 py-8 text-center">Loading…</p>
          ) : delegations.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">No delegations yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Delegator</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Delegate</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Module</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Window</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {delegations.map((d: ApprovalDelegation) => (
                    <tr key={d.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-mono text-xs text-gray-900 dark:text-white">{d.delegatorUserId.slice(0, 8)}</td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-900 dark:text-white">{d.delegateUserId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{d.module ?? 'Any'}</td>
                      <td className="px-3 py-3 text-gray-500 text-xs">
                        {new Date(d.startsOn).toLocaleDateString()} – {new Date(d.endsOn).toLocaleDateString()}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={d.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {d.isActive ? 'Active' : 'Revoked'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3 text-right">
                        {d.isActive ? (
                          <Button size="sm" variant="ghost" className="text-amber-600" disabled={revokeDelMut.isPending} onClick={() => revokeDelMut.mutate(d.id)}>
                            <Ban className="h-3.5 w-3.5" /> Revoke
                          </Button>
                        ) : (
                          <Button size="sm" variant="ghost" className="text-red-600" disabled={deleteDelMut.isPending} onClick={() => deleteDelMut.mutate(d.id)}>
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader
          title="Escalation Policies"
          description="Auto-escalate a stalled approval after its SLA"
          action={
            <Button variant="primary" size="sm" onClick={() => { setFormError(null); setOpenEsc(true) }} leftIcon={<Plus className="h-4 w-4" />}>
              New Policy
            </Button>
          }
        />
        <CardContent>
          {escalations.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">No escalation policies yet.</p>
          ) : (
            <div className="space-y-2">
              {escalations.map((p) => (
                <div key={p.id} className="flex items-center justify-between rounded-md border border-gray-200 dark:border-gray-700 px-3 py-2">
                  <div>
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      <Clock className="h-3.5 w-3.5 inline mr-1" /> Step {p.stepOrder} · SLA {p.slaMinutes} min
                    </p>
                    <p className="text-xs text-gray-500">
                      {p.escalateToRoleId ? `Role ${p.escalateToRoleId.slice(0, 8)}` : p.escalateToUserId ? `User ${p.escalateToUserId.slice(0, 8)}` : 'No target'}
                    </p>
                  </div>
                  <Button size="sm" variant="ghost" className="text-red-600" disabled={deleteEscMut.isPending} onClick={() => deleteEscMut.mutate(p.id)}>
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Modal isOpen={openDel} onClose={() => setOpenDel(false)} title="New Delegation"
        footer={<><Button variant="secondary" onClick={() => setOpenDel(false)} disabled={createDelMut.isPending}>Cancel</Button><Button variant="primary" onClick={submitDel} isLoading={createDelMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <Input value={delegator} onChange={(e) => setDelegator(e.target.value)} label="Delegator User ID" required />
          <Input value={delegate} onChange={(e) => setDelegate(e.target.value)} label="Delegate User ID" required />
          <div className="grid grid-cols-2 gap-3">
            <Input type="date" value={startsOn} onChange={(e) => setStartsOn(e.target.value)} label="Start" required />
            <Input type="date" value={endsOn} onChange={(e) => setEndsOn(e.target.value)} label="End" required />
          </div>
          <Input value={module} onChange={(e) => setModule(e.target.value)} label="Module (optional)" />
        </div>
      </Modal>

      <Modal isOpen={openEsc} onClose={() => setOpenEsc(false)} title="New Escalation Policy"
        footer={<><Button variant="secondary" onClick={() => setOpenEsc(false)} disabled={createEscMut.isPending}>Cancel</Button><Button variant="primary" onClick={submitEsc} isLoading={createEscMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <Input value={wfId} onChange={(e) => setWfId(e.target.value)} label="Workflow ID" required />
          <div className="grid grid-cols-2 gap-3">
            <Input value={stepOrder} onChange={(e) => setStepOrder(e.target.value)} label="Step order" />
            <Input value={sla} onChange={(e) => setSla(e.target.value)} label="SLA minutes" />
          </div>
          <Input value={escRole} onChange={(e) => setEscRole(e.target.value)} label="Escalate to Role ID (optional)" />
          <Input value={escUser} onChange={(e) => setEscUser(e.target.value)} label="Escalate to User ID (optional)" />
        </div>
      </Modal>
    </div>
  )
}
