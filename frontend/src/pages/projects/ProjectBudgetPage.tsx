import { useState } from 'react'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getProjectTasks, getBudgetLines, addBudgetLine, deleteBudgetLine, updateBudgetLine } from '@api/projectAccounting'
import { useQuery, useMutation } from '@tanstack/react-query'
import { ProjectSectionPage } from './ProjectSectionPage'
import type { ProjectSummary, ProjectTask, BudgetLine } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')

export function ProjectBudgetPage() {
  return (
    <ProjectSectionPage title="Project Budget">
      {({ project, setError, queryClient }) => <BudgetContent project={project} setError={setError} queryClient={queryClient} />}
    </ProjectSectionPage>
  )
}

function BudgetContent({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const [showAdd, setShowAdd] = useState(false)
  const [editLine, setEditLine] = useState<BudgetLine | null>(null)
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
  const updateMutation = useMutation({
    mutationFn: ({ lineId, data }: { lineId: string; data: any }) => updateBudgetLine(project.id, lineId, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'budget'] }); setEditLine(null) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const columns: DataTableColumn<BudgetLine>[] = [
    { key: 'category', header: 'Category', sortable: true },
    { key: 'budgetAmount', header: 'Budget', align: 'right', render: (r: BudgetLine) => MONEY(r.budgetAmount) },
    { key: 'budgetedHours', header: 'Hours', align: 'right' },
    { key: 'actualAmount', header: 'Actual', align: 'right', render: (r: BudgetLine) => MONEY(r.actualAmount) },
    { key: 'committedAmount', header: 'Committed', align: 'right', render: (r: BudgetLine) => MONEY(r.committedAmount) },
    { key: 'variance', header: 'Variance', align: 'right', render: (r: BudgetLine) => <span className={r.variance < 0 ? 'text-red-600' : 'text-green-600'}>{MONEY(r.variance)}</span> },
    { key: 'description', header: 'Description' },
    { key: 'actions', header: '', render: (_: unknown, r: BudgetLine) => (
      <div className="flex gap-1">
        <Button size="sm" variant="outline" onClick={() => { setEditLine(r); setForm({ taskId: r.taskId, category: r.category, budgetAmount: r.budgetAmount, budgetedHours: r.budgetedHours, description: r.description ?? '' }) }}><Pencil className="h-3.5 w-3.5" /></Button>
        <Button size="sm" variant="destructive" onClick={() => deleteMutation.mutate(r.id)}><Trash2 className="h-3.5 w-3.5" /></Button>
      </div>
    )},
  ]

  return (
    <div className="space-y-4">
      <div className="flex justify-between">
        <p className="text-sm text-gray-500">Total Budget: {MONEY(project.revisedBudget)} | Costs: {MONEY(project.costsToDate)}</p>
        <Button onClick={() => setShowAdd(true)}><Plus className="h-4 w-4 mr-1" /> Add Budget Line</Button>
      </div>
      <DataTable data={budgetLines as BudgetLine[]} columns={columns} emptyMessage="No budget lines." />
      {(showAdd || editLine) && (
        <Modal title={editLine ? 'Edit Budget Line' : 'Add Budget Line'} isOpen={showAdd || !!editLine} onClose={() => { setShowAdd(false); setEditLine(null) }}>
          <div className="space-y-4">
            <Select label="Task" value={form.taskId} onChange={e => setForm(f => ({ ...f, taskId: e.target.value }))} options={(tasks as ProjectTask[]).map(t => ({ value: t.id, label: `${t.taskCode} - ${t.description}` }))} placeholder="Select task..." disabled={!!editLine} />
            <Select label="Category" value={form.category} onChange={e => setForm(f => ({ ...f, category: e.target.value }))}
              options={['Labor', 'Materials', 'Subcontract', 'Equipment', 'Overhead', 'Other'].map(c => ({ value: c, label: c }))} disabled={!!editLine} />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Budget Amount" type="number" step="0.01" value={form.budgetAmount} onChange={e => setForm(f => ({ ...f, budgetAmount: Number(e.target.value) }))} />
              <Input label="Budgeted Hours" type="number" value={form.budgetedHours} onChange={e => setForm(f => ({ ...f, budgetedHours: Number(e.target.value) }))} />
            </div>
            <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => { setShowAdd(false); setEditLine(null) }}>Cancel</Button>
              <Button onClick={() => {
                if (editLine) { updateMutation.mutate({ lineId: editLine.id, data: { budgetAmount: form.budgetAmount, budgetedHours: form.budgetedHours, description: form.description || null } }) }
                else { addMutation.mutate({ ...form, taskId: form.taskId || '00000000-0000-0000-0000-000000000000' }) }
              }}>{editLine ? 'Save' : 'Add'}</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
