import { useState } from 'react'
import { Settings2, Plus, Pencil, Trash2 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/orderManagement'
import { getWorkCenters, createWorkCenter, updateWorkCenter, deleteWorkCenter } from '@api/bom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { WorkCenterSummary } from '@/types/bom'

const YESNO = (v: boolean) => (v ? 'Yes' : 'No')
const MONEY = (v: number) => `$${Number(v).toFixed(2)}`

export function WorkCentersPage() {
  const queryClient = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [editId, setEditId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [form, setForm] = useState({
    code: '', name: '', department: '', capacityHoursPerDay: 8,
    efficiencyPercentage: 100, costRatePerHour: 0, isActive: true,
  })

  const { data: centers = [], isLoading } = useQuery({
    queryKey: ['bom', 'workCenters'],
    queryFn: () => getWorkCenters(),
  })

  const createMutation = useMutation({
    mutationFn: createWorkCenter,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bom', 'workCenters'] })
      setShowForm(false)
    },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => updateWorkCenter(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bom', 'workCenters'] })
      setShowForm(false)
      setEditId(null)
    },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteWorkCenter,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bom', 'workCenters'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  function openEdit(row: WorkCenterSummary) {
    setEditId(row.id)
    setForm({
      code: row.code, name: row.name, department: row.department ?? '',
      capacityHoursPerDay: row.capacityHoursPerDay,
      efficiencyPercentage: row.efficiencyPercentage,
      costRatePerHour: row.costRatePerHour, isActive: row.isActive,
    })
    setShowForm(true)
  }

  function openCreate() {
    setEditId(null)
    setForm({ code: '', name: '', department: '', capacityHoursPerDay: 8, efficiencyPercentage: 100, costRatePerHour: 0, isActive: true })
    setShowForm(true)
  }

  function save() {
    if (editId) {
      updateMutation.mutate({ id: editId, data: { ...form, department: form.department || null } })
    } else {
      createMutation.mutate({ ...form, companyId: currentCompanyId(), department: form.department || null })
    }
  }

  const columns: DataTableColumn<WorkCenterSummary>[] = [
    { key: 'code', header: 'Code', sortable: true, searchValue: (r) => r.code },
    { key: 'name', header: 'Name', sortable: true, searchValue: (r) => r.name },
    { key: 'department', header: 'Department', sortable: true, searchValue: (r) => r.department ?? '', render: (r: WorkCenterSummary) => r.department ?? '—' },
    { key: 'capacityHoursPerDay', header: 'Capacity Hrs/Day', align: 'right', sortable: true, sortValue: (r) => r.capacityHoursPerDay ?? 0 },
    { key: 'efficiencyPercentage', header: 'Efficiency %', align: 'right', sortable: true, sortValue: (r) => r.efficiencyPercentage ?? 0, render: (r: WorkCenterSummary) => `${r.efficiencyPercentage}%` },
    { key: 'costRatePerHour', header: 'Cost Rate/Hr', align: 'right', sortable: true, sortValue: (r) => r.costRatePerHour ?? 0, render: (r: WorkCenterSummary) => MONEY(r.costRatePerHour) },
    { key: 'isActive', header: 'Active', sortable: true, searchValue: (r) => r.isActive ? 'active' : 'inactive', render: (r: WorkCenterSummary) => YESNO(r.isActive) },
    {
      key: 'actions', header: 'Actions',
      render: (_: unknown, row: WorkCenterSummary) => (
        <div className="flex gap-1">
          <Button size="sm" variant="outline" onClick={() => openEdit(row)}><Pencil className="h-3.5 w-3.5" /></Button>
          <Button size="sm" variant="destructive" onClick={() => {
            if (confirm('Delete work center?')) deleteMutation.mutate(row.id)
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
            <Settings2 className="h-6 w-6" /> Work Centers
          </h1>
          <p className="mt-1 text-sm text-gray-500">Define production work centers for labor/overhead costing</p>
        </div>
        <Button onClick={openCreate}><Plus className="h-4 w-4 mr-1" /> New Work Center</Button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{error}</div>
      )}

      <DataTable
        data={centers as WorkCenterSummary[]}
        columns={columns}
        isLoading={isLoading}
        searchable
        searchPlaceholder="Search work centers by code, name, or department..."
        clientSort
        pageSize={25}
        emptyMessage="No work centers defined."
      />

      <Modal title={editId ? 'Edit Work Center' : 'New Work Center'} isOpen={showForm} onClose={() => { setShowForm(false); setEditId(null) }}>
        <div className="space-y-4">
          <Input label="Code" value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))} disabled={!!editId} />
          <Input label="Name" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
          <Input label="Department" value={form.department} onChange={e => setForm(f => ({ ...f, department: e.target.value }))} />
          <div className="grid grid-cols-3 gap-4">
            <Input label="Capacity Hrs/Day" type="number" value={form.capacityHoursPerDay} onChange={e => setForm(f => ({ ...f, capacityHoursPerDay: Number(e.target.value) }))} />
            <Input label="Efficiency %" type="number" value={form.efficiencyPercentage} onChange={e => setForm(f => ({ ...f, efficiencyPercentage: Number(e.target.value) }))} />
            <Input label="Cost Rate/Hr" type="number" step="0.01" value={form.costRatePerHour} onChange={e => setForm(f => ({ ...f, costRatePerHour: Number(e.target.value) }))} />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={form.isActive} onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))} className="rounded" />
            Active
          </label>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => { setShowForm(false); setEditId(null) }}>Cancel</Button>
            <Button onClick={save}>{editId ? 'Save' : 'Create'}</Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
