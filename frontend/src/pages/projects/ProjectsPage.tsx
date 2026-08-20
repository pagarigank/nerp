import { useState, useMemo, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { FolderKanban, Plus, Pencil, Trash2, DollarSign } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import {
  getProjects,
  createProject,
  updateProject,
  updateProjectStatus,
  getProjectTasks,
  addProjectTask,
  deleteProjectTask,
  getBudgetLines,
  addBudgetLine,
  deleteBudgetLine,
  getCostTransactions,
  postCost,
  getCostSummary,
  getWipSchedule,
  getChangeOrders,
  createChangeOrder,
  approveChangeOrder,
  rejectChangeOrder,
  executeChangeOrder,
  getContractLines,
  addContractLine,
  generateInvoice,
  getWipScheduleAnalysis,
  getForecast,
  getProfitability,
  getBudgetVsActual,
  getUnbilled,
  getChangeOrderSummary,
} from '@api/projectAccounting'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { ProjectSummary, ProjectTask, BudgetLine, CostTransaction, ChangeOrder, ContractLine, WipSchedule, CostSummary } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')
const PCT = (v: number) => `${v.toFixed(1)}%`

export function ProjectsPage() {
  const queryClient = useQueryClient()
  const [searchParams] = useSearchParams()
  const sectionParam = searchParams.get('section') as 'overview' | 'tasks' | 'budget' | 'costs' | 'billing' | 'change-orders' | 'analysis' | null
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [tab, setTab] = useState<'overview' | 'tasks' | 'budget' | 'costs' | 'billing' | 'change-orders' | 'analysis'>(sectionParam ?? 'overview')
  const [showCreate, setShowCreate] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('')

  const { data: projects = [], isLoading } = useQuery({
    queryKey: ['projects', statusFilter],
    queryFn: () => getProjects(undefined, statusFilter || undefined),
  })

  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: () => getCustomers(),
  })

  const customerMap = useMemo(() => Object.fromEntries((customers as any[]).map((c: any) => [c.id, c])), [customers])

  const selectedProject = (projects as ProjectSummary[]).find((p: ProjectSummary) => p.id === selectedId)

  // Deep-linking: when a sub-menu item sets ?section=, switch the active detail tab to it,
  // and if no project is selected yet, open the first project so the section actually renders.
  useEffect(() => {
    if (sectionParam) {
      setTab(sectionParam)
      if (!selectedId && (projects as ProjectSummary[]).length > 0) {
        setSelectedId((projects as ProjectSummary[])[0].id)
      }
    }
  }, [sectionParam, projects, selectedId])

  const createMutation = useMutation({
    mutationFn: createProject,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      setShowCreate(false)
    },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const statusColumns: DataTableColumn<ProjectSummary>[] = [
    { key: 'projectCode', header: 'Code', sortable: true },
    { key: 'name', header: 'Name' },
    { key: 'projectType', header: 'Type' },
    { key: 'projectManager', header: 'PM', render: (r: ProjectSummary) => r.projectManager ?? '—' },
    { key: 'contractValue', header: 'Contract', align: 'right', render: (r: ProjectSummary) => MONEY(r.contractValue) },
    { key: 'costsToDate', header: 'Costs', align: 'right', render: (r: ProjectSummary) => MONEY(r.costsToDate) },
    { key: 'revenueToDate', header: 'Revenue', align: 'right', render: (r: ProjectSummary) => MONEY(r.revenueToDate) },
    { key: 'percentComplete', header: '% Complete', align: 'right', render: (r: ProjectSummary) => PCT(r.percentComplete) },
    { key: 'profitMargin', header: 'Margin', align: 'right', render: (r: ProjectSummary) => r.profitMargin != null ? PCT(r.profitMargin) : '—' },
    { key: 'status', header: 'Status', render: (r: ProjectSummary) => (
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

      {selectedProject ? (
        <ProjectDetail project={selectedProject} tab={tab} setTab={setTab} onBack={() => setSelectedId(null)} setError={setError} queryClient={queryClient} />
      ) : (
        <DataTable data={projects as ProjectSummary[]} columns={statusColumns} isLoading={isLoading}
          onRowClick={(row) => setSelectedId(row.id)} emptyMessage="No projects yet." />
      )}

      <CreateProjectModal open={showCreate} onClose={() => setShowCreate(false)} onSubmit={(d) => createMutation.mutate(d)} customers={customers as any[]} />
    </div>
  )
}

// --- Project Detail ---
function ProjectDetail({ project, tab, setTab, onBack, setError, queryClient }: {
  project: ProjectSummary; tab: string; setTab: (t: any) => void; onBack: () => void; setError: (e: string | null) => void; queryClient: any
}) {
  const tabs = [
    { key: 'overview', label: 'Overview' },
    { key: 'tasks', label: 'Tasks' },
    { key: 'budget', label: 'Budget' },
    { key: 'costs', label: 'Costs' },
    { key: 'billing', label: 'Billing' },
    { key: 'change-orders', label: 'Change Orders' },
    { key: 'analysis', label: 'Analysis' },
  ]

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-4">
        <Button variant="outline" onClick={onBack}>← Back</Button>
        <div>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white">{project.projectCode} — {project.name}</h2>
          <p className="text-sm text-gray-500">Type: {project.projectType} | PM: {project.projectManager ?? '—'} | {project.status}</p>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-6 gap-4">
        {[
          { label: 'Contract', value: MONEY(project.contractValue) },
          { label: 'Budget', value: MONEY(project.revisedBudget) },
          { label: 'Costs', value: MONEY(project.costsToDate) },
          { label: 'Revenue', value: MONEY(project.revenueToDate) },
          { label: '% Complete', value: PCT(project.percentComplete) },
          { label: 'Margin', value: project.profitMargin != null ? PCT(project.profitMargin) : '—' },
        ].map((kpi, i) => (
          <div key={i} className="rounded-lg border border-gray-200 bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-xs text-gray-500">{kpi.label}</p>
            <p className="text-lg font-semibold text-gray-900 dark:text-white">{kpi.value}</p>
          </div>
        ))}
      </div>

      <div className="flex gap-2 border-b border-gray-200 dark:border-gray-700">
        {tabs.map(t => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`px-3 py-2 text-sm font-medium ${tab === t.key ? 'border-b-2 border-blue-600 text-blue-600' : 'text-gray-600 hover:text-gray-900 dark:text-gray-400'}`}
          >{t.label}</button>
        ))}
      </div>

      {tab === 'overview' && <OverviewTab project={project} />}
      {tab === 'tasks' && <TasksTab project={project} setError={setError} queryClient={queryClient} />}
      {tab === 'budget' && <BudgetTab project={project} setError={setError} queryClient={queryClient} />}
      {tab === 'costs' && <CostsTab project={project} setError={setError} queryClient={queryClient} />}
      {tab === 'billing' && <BillingTab project={project} setError={setError} queryClient={queryClient} />}
      {tab === 'change-orders' && <ChangeOrdersTab project={project} setError={setError} queryClient={queryClient} />}
      {tab === 'analysis' && <AnalysisTab project={project} />}
    </div>
  )
}

function OverviewTab({ project }: { project: ProjectSummary }) {
  return (
    <div className="grid grid-cols-2 gap-6">
      <div className="space-y-3 rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
        <h3 className="font-semibold text-gray-900 dark:text-white">Project Info</h3>
        <div className="text-sm space-y-1">
          <p><span className="text-gray-500">Code:</span> {project.projectCode}</p>
          <p><span className="text-gray-500">Name:</span> {project.name}</p>
          <p><span className="text-gray-500">Type:</span> {project.projectType}</p>
          <p><span className="text-gray-500">Manager:</span> {project.projectManager ?? '—'}</p>
          <p><span className="text-gray-500">Customer:</span> {project.customerId ? 'Assigned' : '—'}</p>
          <p><span className="text-gray-500">Start:</span> {project.plannedStartDate ?? '—'}</p>
          <p><span className="text-gray-500">End:</span> {project.plannedEndDate ?? '—'}</p>
        </div>
      </div>
      <div className="space-y-3 rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
        <h3 className="font-semibold text-gray-900 dark:text-white">Financial Summary</h3>
        <div className="text-sm space-y-1">
          <p><span className="text-gray-500">Contract Value:</span> {MONEY(project.contractValue)}</p>
          <p><span className="text-gray-500">Original Budget:</span> {MONEY(project.originalBudget)}</p>
          <p><span className="text-gray-500">Revised Budget:</span> {MONEY(project.revisedBudget)}</p>
          <p><span className="text-gray-500">Costs to Date:</span> {MONEY(project.costsToDate)}</p>
          <p><span className="text-gray-500">Revenue to Date:</span> {MONEY(project.revenueToDate)}</p>
          <p><span className="text-gray-500">Retainage %:</span> {project.retainagePercentage}%</p>
          <p><span className="text-gray-500">Retainage Held:</span> {MONEY(project.retainageHeld)}</p>
        </div>
      </div>
    </div>
  )
}

function TasksTab({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ taskCode: '', description: '', budgetedHours: 0, budgetedCost: 0 })

  const { data: tasks = [] } = useQuery({
    queryKey: ['projects', project.id, 'tasks'],
    queryFn: () => getProjectTasks(project.id),
  })

  const addMutation = useMutation({
    mutationFn: (data: any) => addProjectTask(project.id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'tasks'] }); setShowAdd(false); setForm({ taskCode: '', description: '', budgetedHours: 0, budgetedCost: 0 }) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const deleteMutation = useMutation({
    mutationFn: (taskId: string) => deleteProjectTask(project.id, taskId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'tasks'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const columns: DataTableColumn<ProjectTask>[] = [
    { key: 'taskCode', header: 'Code', sortable: true },
    { key: 'description', header: 'Description' },
    { key: 'budgetedHours', header: 'Budget Hrs', align: 'right' },
    { key: 'budgetedCost', header: 'Budget Cost', align: 'right', render: (r: ProjectTask) => MONEY(r.budgetedCost) },
    { key: 'actualHours', header: 'Actual Hrs', align: 'right' },
    { key: 'actualCost', header: 'Actual Cost', align: 'right', render: (r: ProjectTask) => MONEY(r.actualCost) },
    { key: 'percentComplete', header: '% Done', align: 'right', render: (r: ProjectTask) => PCT(r.percentComplete) },
    { key: 'actions', header: '', render: (_: unknown, r: ProjectTask) => (
      <Button size="sm" variant="destructive" onClick={() => deleteMutation.mutate(r.id)}><Trash2 className="h-3.5 w-3.5" /></Button>
    )},
  ]

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button onClick={() => setShowAdd(true)}><Plus className="h-4 w-4 mr-1" /> Add Task</Button>
      </div>
      <DataTable data={tasks as ProjectTask[]} columns={columns} emptyMessage="No tasks defined." />
      {showAdd && (
        <Modal title="Add Task" isOpen={showAdd} onClose={() => setShowAdd(false)}>
          <div className="space-y-4">
            <Input label="Task Code" value={form.taskCode} onChange={e => setForm(f => ({ ...f, taskCode: e.target.value }))} />
            <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Budgeted Hours" type="number" value={form.budgetedHours} onChange={e => setForm(f => ({ ...f, budgetedHours: Number(e.target.value) }))} />
              <Input label="Budgeted Cost" type="number" step="0.01" value={form.budgetedCost} onChange={e => setForm(f => ({ ...f, budgetedCost: Number(e.target.value) }))} />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => setShowAdd(false)}>Cancel</Button>
              <Button onClick={() => addMutation.mutate(form)}>Add</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

function BudgetTab({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ taskId: '', category: 'Labor', budgetAmount: 0, budgetedHours: 0, description: '' })
  const { data: tasks = [] } = useQuery({ queryKey: ['projects', project.id, 'tasks'], queryFn: () => getProjectTasks(project.id) })
  const { data: budgetLines = [] } = useQuery({ queryKey: ['projects', project.id, 'budget'], queryFn: () => getBudgetLines(project.id) })

  const addMutation = useMutation({
    mutationFn: (data: any) => addBudgetLine(project.id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'budget'] }); setShowAdd(false) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const deleteMutation = useMutation({
    mutationFn: (lineId: string) => deleteBudgetLine(project.id, lineId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'budget'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const taskMap = Object.fromEntries((tasks as ProjectTask[]).map(t => [t.id, t]))

  const columns: DataTableColumn<BudgetLine>[] = [
    { key: 'category', header: 'Category', sortable: true },
    { key: 'budgetAmount', header: 'Budget', align: 'right', render: (r: BudgetLine) => MONEY(r.budgetAmount) },
    { key: 'budgetedHours', header: 'Hours', align: 'right' },
    { key: 'actualAmount', header: 'Actual', align: 'right', render: (r: BudgetLine) => MONEY(r.actualAmount) },
    { key: 'committedAmount', header: 'Committed', align: 'right', render: (r: BudgetLine) => MONEY(r.committedAmount) },
    { key: 'variance', header: 'Variance', align: 'right', render: (r: BudgetLine) => <span className={r.variance < 0 ? 'text-red-600' : 'text-green-600'}>{MONEY(r.variance)}</span> },
    { key: 'description', header: 'Description' },
    { key: 'actions', header: '', render: (_: unknown, r: BudgetLine) => (
      <Button size="sm" variant="destructive" onClick={() => deleteMutation.mutate(r.id)}><Trash2 className="h-3.5 w-3.5" /></Button>
    )},
  ]

  return (
    <div className="space-y-4">
      <div className="flex justify-between">
        <p className="text-sm text-gray-500">Total Budget: {MONEY(project.revisedBudget)} | Costs: {MONEY(project.costsToDate)}</p>
        <Button onClick={() => setShowAdd(true)}><Plus className="h-4 w-4 mr-1" /> Add Budget Line</Button>
      </div>
      <DataTable data={budgetLines as BudgetLine[]} columns={columns} emptyMessage="No budget lines." />
      {showAdd && (
        <Modal title="Add Budget Line" isOpen={showAdd} onClose={() => setShowAdd(false)}>
          <div className="space-y-4">
            <Select label="Task" value={form.taskId} onChange={e => setForm(f => ({ ...f, taskId: e.target.value }))} options={(tasks as ProjectTask[]).map(t => ({ value: t.id, label: `${t.taskCode} - ${t.description}` }))} placeholder="Select task..." />
            <Select label="Category" value={form.category} onChange={e => setForm(f => ({ ...f, category: e.target.value }))}
              options={['Labor', 'Materials', 'Subcontract', 'Equipment', 'Overhead', 'Other'].map(c => ({ value: c, label: c }))} />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Budget Amount" type="number" step="0.01" value={form.budgetAmount} onChange={e => setForm(f => ({ ...f, budgetAmount: Number(e.target.value) }))} />
              <Input label="Budgeted Hours" type="number" value={form.budgetedHours} onChange={e => setForm(f => ({ ...f, budgetedHours: Number(e.target.value) }))} />
            </div>
            <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => setShowAdd(false)}>Cancel</Button>
              <Button onClick={() => addMutation.mutate({ ...form, taskId: form.taskId || '00000000-0000-0000-0000-000000000000' })}>Add</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

function CostsTab({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ taskId: '', category: 'Labor', transactionType: 'ManualAdjustment', amount: 0, hours: 0, description: '' })
  const { data: tasks = [] } = useQuery({ queryKey: ['projects', project.id, 'tasks'], queryFn: () => getProjectTasks(project.id) })
  const { data: costs = [] } = useQuery({ queryKey: ['projects', project.id, 'costs'], queryFn: () => getCostTransactions(project.id) })
  const { data: summary } = useQuery({ queryKey: ['projects', project.id, 'cost-summary'], queryFn: () => getCostSummary(project.id) })

  const addMutation = useMutation({
    mutationFn: (data: any) => postCost(project.id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'costs'] }); setShowAdd(false) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const columns: DataTableColumn<CostTransaction>[] = [
    { key: 'transactionDate', header: 'Date', render: (r: CostTransaction) => new Date(r.transactionDate).toLocaleDateString() },
    { key: 'category', header: 'Category' },
    { key: 'transactionType', header: 'Type' },
    { key: 'amount', header: 'Amount', align: 'right', render: (r: CostTransaction) => MONEY(r.amount) },
    { key: 'hours', header: 'Hours', align: 'right' },
    { key: 'billableAmount', header: 'Billable', align: 'right', render: (r: CostTransaction) => MONEY(r.billableAmount) },
    { key: 'description', header: 'Description' },
    { key: 'status', header: 'Status' },
  ]

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button onClick={() => setShowAdd(true)}><Plus className="h-4 w-4 mr-1" /> Post Cost</Button>
      </div>

      {summary && (
        <div className="grid grid-cols-4 gap-4 text-sm">
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-gray-500">Total Costs</p><p className="font-semibold">{MONEY(summary.totalCosts)}</p>
          </div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-gray-500">Budget</p><p className="font-semibold">{MONEY(summary.totalBudget)}</p>
          </div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-gray-500">Remaining</p><p className="font-semibold">{MONEY(summary.remaining)}</p>
          </div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-gray-500">% Complete</p><p className="font-semibold">{PCT(summary.percentComplete)}</p>
          </div>
        </div>
      )}

      <DataTable data={costs as CostTransaction[]} columns={columns} emptyMessage="No cost transactions." />

      {showAdd && (
        <Modal title="Post Cost" isOpen={showAdd} onClose={() => setShowAdd(false)}>
          <div className="space-y-4">
            <Select label="Task" value={form.taskId} onChange={e => setForm(f => ({ ...f, taskId: e.target.value }))} options={(tasks as ProjectTask[]).map(t => ({ value: t.id, label: `${t.taskCode} - ${t.description}` }))} placeholder="Select task..." />
            <div className="grid grid-cols-2 gap-4">
              <Select label="Category" value={form.category} onChange={e => setForm(f => ({ ...f, category: e.target.value }))}
                options={['Labor', 'Materials', 'Subcontract', 'Equipment', 'Overhead', 'Other'].map(c => ({ value: c, label: c }))} />
              <Select label="Type" value={form.transactionType} onChange={e => setForm(f => ({ ...f, transactionType: e.target.value }))}
                options={['ApVoucher', 'PayrollLabor', 'InventoryIssue', 'SubcontractInvoice', 'ManualAdjustment'].map(c => ({ value: c, label: c }))} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <Input label="Amount" type="number" step="0.01" value={form.amount} onChange={e => setForm(f => ({ ...f, amount: Number(e.target.value) }))} />
              <Input label="Hours" type="number" value={form.hours} onChange={e => setForm(f => ({ ...f, hours: Number(e.target.value) }))} />
            </div>
            <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => setShowAdd(false)}>Cancel</Button>
              <Button onClick={() => addMutation.mutate({ ...form, taskId: form.taskId || '00000000-0000-0000-0000-000000000000' })}>Post</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

function ChangeOrdersTab({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ description: '', amount: 0, category: 'Materials', reason: '' })

  const { data: cos = [] } = useQuery({ queryKey: ['projects', project.id, 'change-orders'], queryFn: () => getChangeOrders(project.id) })

  const submitMutation = useMutation({
    mutationFn: (coId: string) => submitChangeOrder(project.id, coId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'change-orders'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })
  const approveMutation = useMutation({
    mutationFn: (coId: string) => approveChangeOrder(project.id, coId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'change-orders'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })
  const executeMutation = useMutation({
    mutationFn: (coId: string) => executeChangeOrder(project.id, coId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'change-orders'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const addMutation = useMutation({
    mutationFn: (data: any) => createChangeOrder(project.id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'change-orders'] }); setShowAdd(false) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const columns: DataTableColumn<ChangeOrder>[] = [
    { key: 'description', header: 'Description' },
    { key: 'amount', header: 'Amount', align: 'right', render: (r: ChangeOrder) => MONEY(r.amount) },
    { key: 'category', header: 'Category' },
    { key: 'reason', header: 'Reason' },
    { key: 'status', header: 'Status', render: (r: ChangeOrder) => (
      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
        r.status === 'Approved' ? 'bg-green-100 text-green-800'
        : r.status === 'Rejected' ? 'bg-red-100 text-red-800'
        : r.status === 'Submitted' ? 'bg-yellow-100 text-yellow-800'
        : 'bg-gray-100 text-gray-800'
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

function BillingTab({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const { data: contracts = [] } = useQuery({ queryKey: ['projects', project.id, 'contracts'], queryFn: () => getContractLines(project.id) })
  const { data: wip } = useQuery({ queryKey: ['projects', project.id, 'wip'], queryFn: () => getWipSchedule(project.id) })

  const generateMutation = useMutation({
    mutationFn: () => generateInvoice(project.id),
    onSuccess: (data: any) => { queryClient.invalidateQueries({ queryKey: ['projects', project.id] }); setError(null); alert(`Invoice generated: $${data.invoiceAmount}`) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const contractColumns: DataTableColumn<ContractLine>[] = [
    { key: 'description', header: 'Description' },
    { key: 'billingMethod', header: 'Method' },
    { key: 'contractAmount', header: 'Contract', align: 'right', render: (r: ContractLine) => MONEY(r.contractAmount) },
    { key: 'billedAmount', header: 'Billed', align: 'right', render: (r: ContractLine) => MONEY(r.billedAmount) },
    { key: 'remaining', header: 'Remaining', align: 'right', render: (r: ContractLine) => MONEY(r.remaining) },
    { key: 'notToExceed', header: 'NTE', align: 'right', render: (r: ContractLine) => MONEY(r.notToExceed) },
    { key: 'percentComplete', header: '% Complete', align: 'right', render: (r: ContractLine) => PCT(r.percentComplete) },
  ]

  return (
    <div className="space-y-6">
      {wip && (
        <div className="grid grid-cols-4 gap-4 text-sm">
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-gray-500">Contract Value</p><p className="font-semibold">{MONEY(wip.contractValue)}</p>
          </div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-gray-500">Costs to Date</p><p className="font-semibold">{MONEY(wip.costsToDate)}</p>
          </div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-gray-500">Earned Revenue</p><p className="font-semibold">{MONEY(wip.earnedRevenue)}</p>
          </div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
            <p className="text-gray-500">Over/Under Billing</p>
            <p className={`font-semibold ${wip.overUnderBilling >= 0 ? 'text-green-600' : 'text-red-600'}`}>{MONEY(wip.overUnderBilling)}</p>
          </div>
        </div>
      )}

      <div className="flex justify-between items-center">
        <h3 className="font-semibold text-gray-900 dark:text-white">Contract Lines</h3>
        <Button onClick={() => generateMutation.mutate()}><DollarSign className="h-4 w-4 mr-1" /> Generate Invoice</Button>
      </div>
      <DataTable data={contracts as ContractLine[]} columns={contractColumns} emptyMessage="No contract lines defined." />
    </div>
  )
}

// --- Analysis Tab (WIP / Forecast / Profitability / Budget vs Actual / Unbilled / Change Orders) ---
function AnalysisTab({ project }: { project: ProjectSummary }) {
  const { data: wip } = useQuery({ queryKey: ['projects', project.id, 'analysis', 'wip'], queryFn: () => getWipScheduleAnalysis(project.id) })
  const { data: forecast } = useQuery({ queryKey: ['projects', project.id, 'analysis', 'forecast'], queryFn: () => getForecast(project.id) })
  const { data: profitability } = useQuery({ queryKey: ['projects', project.id, 'analysis', 'profitability'], queryFn: () => getProfitability(project.id) })
  const { data: changeSummary } = useQuery({ queryKey: ['projects', project.id, 'analysis', 'co'], queryFn: () => getChangeOrderSummary(project.id) })
  const { data: unbilled } = useQuery({ queryKey: ['projects', project.id, 'analysis', 'unbilled'], queryFn: () => getUnbilled(project.id) })
  const { data: budgetVsActual = [] } = useQuery({ queryKey: ['projects', project.id, 'analysis', 'bva'], queryFn: () => getBudgetVsActual(project.id) })

  const Kpi = ({ label, value, tone }: { label: string; value: string; tone?: 'green' | 'red' }) => (
    <div className="rounded-lg border border-gray-200 bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
      <p className="text-xs text-gray-500">{label}</p>
      <p className={`text-lg font-semibold ${tone === 'green' ? 'text-green-600' : tone === 'red' ? 'text-red-600' : 'text-gray-900 dark:text-white'}`}>{value}</p>
    </div>
  )

  return (
    <div className="space-y-6">
      <div>
        <h3 className="mb-2 font-semibold text-gray-900 dark:text-white">WIP & Forecast</h3>
        <div className="grid grid-cols-2 md:grid-cols-6 gap-4">
          <Kpi label="Earned Revenue" value={MONEY(wip?.earnedRevenue ?? 0)} />
          <Kpi label="Over/Under Billing" value={MONEY(wip?.overUnderBilling ?? 0)} tone={(wip?.overUnderBilling ?? 0) >= 0 ? 'green' : 'red'} />
          <Kpi label="EAC" value={MONEY(forecast?.estimateAtCompletion ?? 0)} />
          <Kpi label="ETC" value={MONEY(forecast?.estimateToComplete ?? 0)} />
          <Kpi label="CPI" value={forecast?.costPerformanceIndex?.toFixed(2) ?? '—'} />
          <Kpi label="SPI" value={forecast?.schedulePerformanceIndex?.toFixed(2) ?? '—'} />
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
          <h4 className="font-semibold text-gray-900 dark:text-white">Profitability</h4>
          <div className="mt-2 space-y-1 text-sm">
            <p><span className="text-gray-500">Revenue:</span> {MONEY(profitability?.revenue ?? 0)}</p>
            <p><span className="text-gray-500">Costs:</span> {MONEY(profitability?.costs ?? 0)}</p>
            <p><span className="text-gray-500">Margin:</span> {MONEY(profitability?.margin ?? 0)} ({profitability?.marginPercent?.toFixed(1) ?? '0.0'}%)</p>
            <p><span className="text-gray-500">Retainage Held:</span> {MONEY(profitability?.retainageHeld ?? 0)}</p>
          </div>
        </div>
        <div className="rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
          <h4 className="font-semibold text-gray-900 dark:text-white">Unbilled AR</h4>
          <div className="mt-2 space-y-1 text-sm">
            <p><span className="text-gray-500">Earned:</span> {MONEY(unbilled?.earnedRevenue ?? 0)}</p>
            <p><span className="text-gray-500">Billed:</span> {MONEY(unbilled?.billedRevenue ?? 0)}</p>
            <p><span className="text-gray-500">Unbilled:</span> {MONEY(unbilled?.unbilledAmount ?? 0)}</p>
          </div>
        </div>
        <div className="rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
          <h4 className="font-semibold text-gray-900 dark:text-white">Change Orders</h4>
          <div className="mt-2 space-y-1 text-sm">
            <p><span className="text-gray-500">Original Budget:</span> {MONEY(changeSummary?.originalBudget ?? 0)}</p>
            <p><span className="text-gray-500">Approved COs:</span> {MONEY(changeSummary?.approvedChangeOrders ?? 0)}</p>
            <p><span className="text-gray-500">Pending COs:</span> {MONEY(changeSummary?.pendingChangeOrders ?? 0)}</p>
            <p><span className="text-gray-500">Revised Budget:</span> {MONEY(changeSummary?.revisedBudget ?? 0)}</p>
          </div>
        </div>
      </div>

      <div>
        <h3 className="mb-2 font-semibold text-gray-900 dark:text-white">Budget vs. Actual</h3>
        <DataTable data={budgetVsActual as any[]} columns={[
          { key: 'category', header: 'Category' },
          { key: 'budgetAmount', header: 'Budget', align: 'right', render: (r: any) => MONEY(r.budgetAmount) },
          { key: 'actualAmount', header: 'Actual', align: 'right', render: (r: any) => MONEY(r.actualAmount) },
          { key: 'committedAmount', header: 'Committed', align: 'right', render: (r: any) => MONEY(r.committedAmount) },
          { key: 'variance', header: 'Variance', align: 'right', render: (r: any) => <span className={r.variance < 0 ? 'text-red-600' : 'text-green-600'}>{MONEY(r.variance)}</span> },
          { key: 'variancePercent', header: '% Var', align: 'right', render: (r: any) => `${r.variancePercent?.toFixed(1)}%` },
        ]} emptyMessage="No budget lines." />
      </div>
    </div>
  )
}

// --- Create Project Modal ---
function CreateProjectModal({ open, onClose, onSubmit, customers }: { open: boolean; onClose: () => void; onSubmit: (d: any) => void; customers: any[] }) {
  const [form, setForm] = useState({
    projectCode: '', name: '', description: '', projectType: 'TimeAndMaterials',
    customerId: '', projectManager: '', contractValue: '', retainagePercentage: 0,
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
          })}>Create</Button>
        </div>
      </div>
    </Modal>
  )
}
