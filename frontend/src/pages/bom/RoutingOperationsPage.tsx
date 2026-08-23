import { useState } from 'react'
import { GitBranch, Plus, Pencil, Trash2 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/orderManagement'
import {
  getRoutingOperations,
  createRoutingOperation,
  updateRoutingOperation,
  activateRoutingOperation,
  deactivateRoutingOperation,
  deleteRoutingOperation,
} from '@api/bom'
import { getWorkCenters } from '@api/bom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { RoutingOperationSummary, WorkCenterSummary } from '@/types/bom'

const MIN = (v: number) => `${Number(v).toFixed(1)} min`

export function RoutingOperationsPage() {
  const queryClient = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [form, setForm] = useState({
    operationCode: '',
    description: '',
    workCenterId: '',
    standardSetupTimeMinutes: 0,
    standardRunTimeMinutesPerUnit: 0,
  })

  const { data: operations = [], isLoading } = useQuery({
    queryKey: ['bom', 'routingOperations'],
    queryFn: () => getRoutingOperations(),
  })

  const { data: workCenters = [] } = useQuery({
    queryKey: ['bom', 'workCenters'],
    queryFn: () => getWorkCenters(),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['bom', 'routingOperations'] })

  const createMutation = useMutation({
    mutationFn: createRoutingOperation,
    onSuccess: () => { invalidate(); setShowForm(false); setEditId(null) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => updateRoutingOperation(id, data),
    onSuccess: () => { invalidate(); setShowForm(false); setEditId(null) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const toggleMutation = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      active ? activateRoutingOperation(id) : deactivateRoutingOperation(id),
    onSuccess: invalidate,
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteRoutingOperation,
    onSuccess: invalidate,
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  function openEdit(row: RoutingOperationSummary) {
    setEditId(row.id)
    setForm({
      operationCode: row.operationCode,
      description: row.description ?? '',
      workCenterId: row.workCenterId ?? '',
      standardSetupTimeMinutes: row.standardSetupTimeMinutes,
      standardRunTimeMinutesPerUnit: row.standardRunTimeMinutesPerUnit,
    })
    setShowForm(true)
  }

  function openCreate() {
    setEditId(null)
    setForm({ operationCode: '', description: '', workCenterId: '', standardSetupTimeMinutes: 0, standardRunTimeMinutesPerUnit: 0 })
    setShowForm(true)
  }

  function save() {
    const data = { ...form, workCenterId: form.workCenterId || null }
    if (editId) {
      updateMutation.mutate({ id: editId, data })
    } else {
      createMutation.mutate({ ...data, companyId: currentCompanyId() })
    }
  }

  const columns: DataTableColumn<RoutingOperationSummary>[] = [
    { key: 'operationCode', header: 'Code', sortable: true },
    { key: 'description', header: 'Description', render: (r: RoutingOperationSummary) => r.description ?? '—' },
    {
      key: 'workCenterId',
      header: 'Work Center',
      render: (r: RoutingOperationSummary) =>
        (workCenters as WorkCenterSummary[]).find(wc => wc.id === r.workCenterId)?.code ?? '—',
    },
    { key: 'standardSetupTimeMinutes', header: 'Setup Time', align: 'right', render: (r: RoutingOperationSummary) => MIN(r.standardSetupTimeMinutes) },
    { key: 'standardRunTimeMinutesPerUnit', header: 'Run Time/Unit', align: 'right', render: (r: RoutingOperationSummary) => MIN(r.standardRunTimeMinutesPerUnit) },
    {
      key: 'isActive', header: 'Active',
      render: (r: RoutingOperationSummary) => (
        <button
          onClick={() => toggleMutation.mutate({ id: r.id, active: !r.isActive })}
          className={`px-2 py-0.5 rounded text-xs font-medium ${r.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-200 text-gray-500'}`}
        >
          {r.isActive ? 'Active' : 'Inactive'}
        </button>
      ),
    },
    {
      key: 'actions', header: 'Actions',
      render: (_: unknown, row: RoutingOperationSummary) => (
        <div className="flex gap-1">
          <Button size="sm" variant="outline" onClick={() => openEdit(row)}><Pencil className="h-3.5 w-3.5" /></Button>
          <Button size="sm" variant="destructive" onClick={() => {
            if (confirm('Delete routing operation?')) deleteMutation.mutate(row.id)
          }}><Trash2 className="h-3.5 w-3.5" /></Button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <GitBranch className="h-6 w-6" /> Routing Operations
          </h1>
          <p className="mt-1 text-sm text-gray-500">Standard operations with work centers and setup/run times</p>
        </div>
        <Button onClick={openCreate}><Plus className="h-4 w-4 mr-1" /> New Operation</Button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{error}</div>
      )}

      <DataTable data={operations as RoutingOperationSummary[]} columns={columns} isLoading={isLoading} emptyMessage="No routing operations defined." />

      <Modal title={editId ? 'Edit Routing Operation' : 'New Routing Operation'} isOpen={showForm} onClose={() => { setShowForm(false); setEditId(null) }}>
        <div className="space-y-4">
          <Input label="Code" value={form.operationCode} onChange={e => setForm(f => ({ ...f, operationCode: e.target.value }))} disabled={!!editId} />
          <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
          <label className="block text-sm">
            <span className="mb-1 block text-gray-700 dark:text-gray-300">Work Center</span>
            <select
              value={form.workCenterId}
              onChange={e => setForm(f => ({ ...f, workCenterId: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-800"
            >
              <option value="">— None —</option>
              {(workCenters as WorkCenterSummary[]).map(wc => (
                <option key={wc.id} value={wc.id}>{wc.code} · {wc.name}</option>
              ))}
            </select>
          </label>
          <div className="grid grid-cols-2 gap-4">
            <Input label="Setup Time (min)" type="number" step="0.1" value={form.standardSetupTimeMinutes} onChange={e => setForm(f => ({ ...f, standardSetupTimeMinutes: Number(e.target.value) }))} />
            <Input label="Run Time (min/unit)" type="number" step="0.01" value={form.standardRunTimeMinutesPerUnit} onChange={e => setForm(f => ({ ...f, standardRunTimeMinutesPerUnit: Number(e.target.value) }))} />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => { setShowForm(false); setEditId(null) }}>Cancel</Button>
            <Button onClick={save}>{editId ? 'Save' : 'Create'}</Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
