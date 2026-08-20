import { useState } from 'react'
import { Plus } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getChangeOrders, createChangeOrder, submitChangeOrder, approveChangeOrder, executeChangeOrder } from '@api/projectAccounting'
import { useQuery, useMutation } from '@tanstack/react-query'
import { ProjectSectionPage } from './ProjectSectionPage'
import type { ProjectSummary, ChangeOrder } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')

export function ProjectChangeOrdersPage() {
  return (
    <ProjectSectionPage title="Change Orders">
      {({ project, setError, queryClient }) => <ChangeOrdersContent project={project} setError={setError} queryClient={queryClient} />}
    </ProjectSectionPage>
  )
}

function ChangeOrdersContent({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ description: '', amount: 0, category: 'Materials', reason: '' })

  const { data: cos = [] } = useQuery({ queryKey: ['projects', project.id, 'change-orders'], queryFn: () => getChangeOrders(project.id) })

  const submitMutation = useMutation({ mutationFn: (coId: string) => submitChangeOrder(project.id, coId), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'change-orders'] }), onError: (e: any) => setError(getErrorMessage(e)) })
  const approveMutation = useMutation({ mutationFn: (coId: string) => approveChangeOrder(project.id, coId), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'change-orders'] }), onError: (e: any) => setError(getErrorMessage(e)) })
  const executeMutation = useMutation({ mutationFn: (coId: string) => executeChangeOrder(project.id, coId), onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'change-orders'] }), onError: (e: any) => setError(getErrorMessage(e)) })
  const addMutation = useMutation({ mutationFn: (data: any) => createChangeOrder(project.id, data), onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'change-orders'] }); setShowAdd(false) }, onError: (e: any) => setError(getErrorMessage(e)) })

  const columns: DataTableColumn<ChangeOrder>[] = [
    { key: 'description', header: 'Description' },
    { key: 'amount', header: 'Amount', align: 'right', render: (r: ChangeOrder) => MONEY(r.amount) },
    { key: 'category', header: 'Category' },
    { key: 'reason', header: 'Reason' },
    { key: 'status', header: 'Status', render: (r: ChangeOrder) => (
      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
        r.status === 'Approved' ? 'bg-green-100 text-green-800' : r.status === 'Rejected' ? 'bg-red-100 text-red-800' : r.status === 'Submitted' ? 'bg-yellow-100 text-yellow-800' : 'bg-gray-100 text-gray-800'
      }`}>{r.status}</span>
    )},
    { key: 'actions', header: 'Actions', render: (_: unknown, r: ChangeOrder) => (
      <div className="flex gap-1">
        {r.status === 'Draft' && <Button size="sm" variant="outline" onClick={() => submitMutation.mutate(r.id)}>Submit</Button>}
        {r.status === 'Submitted' && <Button size="sm" onClick={() => approveMutation.mutate(r.id)}>Approve</Button>}
        {r.status === 'Approved' && <Button size="sm" onClick={() => executeMutation.mutate(r.id)}>Execute</Button>}
      </div>
    )},
  ]

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button onClick={() => setShowAdd(true)}><Plus className="h-4 w-4 mr-1" /> New Change Order</Button>
      </div>
      <DataTable data={cos as ChangeOrder[]} columns={columns} emptyMessage="No change orders." />
      {showAdd && (
        <Modal title="New Change Order" isOpen={showAdd} onClose={() => setShowAdd(false)}>
          <div className="space-y-4">
            <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Amount" type="number" step="0.01" value={form.amount} onChange={e => setForm(f => ({ ...f, amount: Number(e.target.value) }))} />
              <Select label="Category" value={form.category} onChange={e => setForm(f => ({ ...f, category: e.target.value }))}
                options={['Labor', 'Materials', 'Subcontract', 'Equipment', 'Overhead', 'Other'].map(c => ({ value: c, label: c }))} />
            </div>
            <Input label="Reason" value={form.reason} onChange={e => setForm(f => ({ ...f, reason: e.target.value }))} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => setShowAdd(false)}>Cancel</Button>
              <Button onClick={() => addMutation.mutate(form)}>Create</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
