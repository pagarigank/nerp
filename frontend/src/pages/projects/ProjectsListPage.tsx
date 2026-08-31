import { useState, useMemo, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { FolderKanban, Plus, Pencil } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import {
  getProjects, createProject, updateProject,
} from '@api/projectAccounting'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { ProjectSummary } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')
const PCT = (v?: number | null) => (v != null && !Number.isNaN(v) ? `${v.toFixed(1)}%` : '—')

export function ProjectsListPage() {
  const queryClient = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [showEdit, setShowEdit] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const { data: projects = [], isLoading } = useQuery({
    queryKey: ['projects', statusFilter],
    queryFn: () => getProjects(undefined, statusFilter || undefined),
  })

  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: () => getCustomers(),
  })

  const selectedProject = (projects as ProjectSummary[]).find((p: ProjectSummary) => p.id === selectedId)

  const createMutation = useMutation({
    mutationFn: createProject,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      setShowCreate(false)
    },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const columns: DataTableColumn<ProjectSummary>[] = [
    { key: 'projectCode', header: 'Code', sortable: true, searchValue: (r) => r.projectCode },
    { key: 'name', header: 'Name', sortable: true, searchValue: (r) => r.name },
    { key: 'projectType', header: 'Type', searchValue: (r) => r.projectType },
    { key: 'projectManager', header: 'PM', sortable: true, searchValue: (r) => r.projectManager ?? '' },
    { key: 'contractValue', header: 'Contract', align: 'right', sortable: true, render: (r: ProjectSummary) => MONEY(r.contractValue), sortValue: (r) => r.contractValue ?? 0 },
    { key: 'costsToDate', header: 'Costs', align: 'right', sortable: true, render: (r: ProjectSummary) => MONEY(r.costsToDate), sortValue: (r) => r.costsToDate ?? 0 },
    { key: 'revenueToDate', header: 'Revenue', align: 'right', sortable: true, render: (r: ProjectSummary) => MONEY(r.revenueToDate), sortValue: (r) => r.revenueToDate ?? 0 },
    { key: 'percentComplete', header: '% Complete', align: 'right', sortable: true, render: (r: ProjectSummary) => PCT(r.percentComplete), sortValue: (r) => r.percentComplete ?? 0 },
    { key: 'status', header: 'Status', sortable: true, searchValue: (r) => r.status, render: (r: ProjectSummary) => (
      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
        r.status === 'Active' ? 'bg-green-100 text-green-800'
        : r.status === 'Completed' ? 'bg-blue-100 text-blue-800'
        : r.status === 'OnHold' ? 'bg-yellow-100 text-yellow-800'
        : 'bg-gray-100 text-gray-800'
      }`}>{r.status}</span>
    )},
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <FolderKanban className="h-6 w-6" /> Project Accounting
          </h1>
          <p className="mt-1 text-sm text-gray-500">Manage projects, budgets, costs, and billing</p>
        </div>
        <Button onClick={() => setShowCreate(true)}><Plus className="h-4 w-4 mr-1" /> New Project</Button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{error}</div>
      )}

      <div className="flex gap-2">
        {['', 'Planning', 'Active', 'OnHold', 'Completed', 'Closed'].map(s => (
          <button key={s} onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 text-sm rounded-md ${statusFilter === s ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-300'}`}
          >{s || 'All'}</button>
        ))}
      </div>

      <DataTable
        data={projects as ProjectSummary[]}
        columns={columns}
        isLoading={isLoading}
        onRowClick={(row) => setSelectedId(row.id)}
        rowKey={selectedId ?? undefined}
        rowKeyField="id"
        searchable
        searchPlaceholder="Search projects by code, name, type, PM, or status..."
        clientSort
        pageSize={25}
        getRowKey={(r) => r.id}
        emptyMessage="No projects yet."
      />

      <CreateProjectModal open={showCreate} onClose={() => setShowCreate(false)} onSubmit={(d) => createMutation.mutate(d)} customers={customers as any[]} />
      {showEdit && selectedProject && (
        <EditProjectModal open={showEdit} onClose={() => setShowEdit(false)} project={selectedProject}
          onSubmit={(d) => updateProject(selectedProject.id, d).then(() => { queryClient.invalidateQueries({ queryKey: ['projects'] }); setShowEdit(false) }).catch((e: any) => setError(getErrorMessage(e)))}
          customers={customers as any[]} />
      )}
    </div>
  )
}

function CreateProjectModal({ open, onClose, onSubmit, customers }: { open: boolean; onClose: () => void; onSubmit: (d: any) => void; customers: any[] }) {
  const [form, setForm] = useState({
    projectCode: '', name: '', description: '', projectType: 'TimeAndMaterials',
    customerId: '', projectManager: '', contractValue: '', retainagePercentage: 0,
    plannedStartDate: '', plannedEndDate: '',
  })
  if (!open) return null

  return (
    <Modal title="New Project" isOpen={open} onClose={onClose}>
      <div className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input label="Project Code" value={form.projectCode} onChange={e => setForm(f => ({ ...f, projectCode: e.target.value }))} />
          <Input label="Name" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
        </div>
        <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
        <div className="grid grid-cols-2 gap-4">
          <Select label="Project Type" value={form.projectType} onChange={e => setForm(f => ({ ...f, projectType: e.target.value }))}
            options={['TimeAndMaterials', 'CostPlus', 'FixedPrice', 'UnitPrice'].map(t => ({ value: t, label: t }))} />
          <Input label="Project Manager" value={form.projectManager} onChange={e => setForm(f => ({ ...f, projectManager: e.target.value }))} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Contract Value" type="number" step="0.01" value={form.contractValue} onChange={e => setForm(f => ({ ...f, contractValue: e.target.value }))} />
          <Input label="Retainage %" type="number" step="0.01" value={form.retainagePercentage} onChange={e => setForm(f => ({ ...f, retainagePercentage: Number(e.target.value) }))} />
        </div>
        <Select label="Customer" value={form.customerId} onChange={e => setForm(f => ({ ...f, customerId: e.target.value }))}
          options={customers.map((c: any) => ({ value: c.id, label: `${c.customerId} - ${c.name}` }))} placeholder="Optional..." />
        <div className="grid grid-cols-2 gap-4">
          <Input label="Planned Start Date" type="date" value={form.plannedStartDate} onChange={e => setForm(f => ({ ...f, plannedStartDate: e.target.value }))} />
          <Input label="Planned End Date" type="date" value={form.plannedEndDate} onChange={e => setForm(f => ({ ...f, plannedEndDate: e.target.value }))} />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={() => onSubmit({
            companyId: currentCompanyId(),
            projectCode: form.projectCode,
            name: form.name,
            description: form.description || null,
            projectType: form.projectType,
            customerId: form.customerId || null,
            projectManager: form.projectManager || null,
            contractValue: form.contractValue ? Number(form.contractValue) : null,
            retainagePercentage: form.retainagePercentage || null,
            plannedStartDate: form.plannedStartDate || null,
            plannedEndDate: form.plannedEndDate || null,
          })}>Create</Button>
        </div>
      </div>
    </Modal>
  )
}

function EditProjectModal({ open, onClose, project, onSubmit, customers }: {
  open: boolean; onClose: () => void; project: ProjectSummary; onSubmit: (d: any) => void; customers: any[]
}) {
  const [form, setForm] = useState({
    name: project.name,
    description: project.description ?? '',
    projectType: project.projectType,
    customerId: project.customerId ?? '',
    projectManager: project.projectManager ?? '',
    contractValue: project.contractValue?.toString() ?? '',
    retainagePercentage: project.retainagePercentage,
    status: project.status,
    plannedStartDate: project.plannedStartDate?.slice(0, 10) ?? '',
    plannedEndDate: project.plannedEndDate?.slice(0, 10) ?? '',
  })
  if (!open) return null

  return (
    <Modal title="Edit Project" isOpen={open} onClose={onClose}>
      <div className="space-y-4">
        <Input label="Name" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
        <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
        <div className="grid grid-cols-2 gap-4">
          <Select label="Project Type" value={form.projectType} onChange={e => setForm(f => ({ ...f, projectType: e.target.value }))}
            options={['TimeAndMaterials', 'CostPlus', 'FixedPrice', 'UnitPrice'].map(t => ({ value: t, label: t }))} />
          <Select label="Status" value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value }))}
            options={['Planning', 'Active', 'OnHold', 'Completed', 'Closed'].map(s => ({ value: s, label: s }))} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Project Manager" value={form.projectManager} onChange={e => setForm(f => ({ ...f, projectManager: e.target.value }))} />
          <Select label="Customer" value={form.customerId} onChange={e => setForm(f => ({ ...f, customerId: e.target.value }))}
            options={[{ value: '', label: '— None —' }, ...customers.map((c: any) => ({ value: c.id, label: `${c.customerId} - ${c.name}` }))]} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Contract Value" type="number" step="0.01" value={form.contractValue} onChange={e => setForm(f => ({ ...f, contractValue: e.target.value }))} />
          <Input label="Retainage %" type="number" step="0.01" value={form.retainagePercentage} onChange={e => setForm(f => ({ ...f, retainagePercentage: Number(e.target.value) }))} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Planned Start Date" type="date" value={form.plannedStartDate} onChange={e => setForm(f => ({ ...f, plannedStartDate: e.target.value }))} />
          <Input label="Planned End Date" type="date" value={form.plannedEndDate} onChange={e => setForm(f => ({ ...f, plannedEndDate: e.target.value }))} />
        </div>
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={() => onSubmit({
            name: form.name,
            description: form.description || null,
            projectType: form.projectType,
            customerId: form.customerId || null,
            projectManager: form.projectManager || null,
            contractValue: form.contractValue ? Number(form.contractValue) : null,
            retainagePercentage: form.retainagePercentage || null,
            status: form.status,
            plannedStartDate: form.plannedStartDate || null,
            plannedEndDate: form.plannedEndDate || null,
          })}>Save</Button>
        </div>
      </div>
    </Modal>
  )
}
