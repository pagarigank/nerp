import { useState } from 'react'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getProjectTasks, addProjectTask, deleteProjectTask, updateProjectTask } from '@api/projectAccounting'
import { useQuery, useMutation } from '@tanstack/react-query'
import { ProjectSectionPage } from './ProjectSectionPage'
import type { ProjectSummary, ProjectTask } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')
const PCT = (v?: number | null) => (v != null && !Number.isNaN(v) ? `${v.toFixed(1)}%` : '—')

export function ProjectTasksPage() {
  return (
    <ProjectSectionPage title="Project Tasks">
      {({ project, setError, queryClient }) => <TasksContent project={project} setError={setError} queryClient={queryClient} />}
    </ProjectSectionPage>
  )
}

function TasksContent({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const [showAdd, setShowAdd] = useState(false)
  const [editTask, setEditTask] = useState<ProjectTask | null>(null)
  const [form, setForm] = useState({ taskCode: '', description: '', budgetedHours: 0, budgetedCost: 0 })

  const { data: tasks = [] } = useQuery({ queryKey: ['projects', project.id, 'tasks'], queryFn: () => getProjectTasks(project.id) })

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
  const updateMutation = useMutation({
    mutationFn: ({ taskId, data }: { taskId: string; data: any }) => updateProjectTask(project.id, taskId, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'tasks'] }); setEditTask(null) },
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
      <div className="flex gap-1">
        <Button size="sm" variant="outline" onClick={() => { setEditTask(r); setForm({ taskCode: r.taskCode, description: r.description, budgetedHours: r.budgetedHours, budgetedCost: r.budgetedCost }) }}><Pencil className="h-3.5 w-3.5" /></Button>
        <Button size="sm" variant="destructive" onClick={() => deleteMutation.mutate(r.id)}><Trash2 className="h-3.5 w-3.5" /></Button>
      </div>
    )},
  ]

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button onClick={() => setShowAdd(true)}><Plus className="h-4 w-4 mr-1" /> Add Task</Button>
      </div>
      <DataTable data={tasks as ProjectTask[]} columns={columns} emptyMessage="No tasks defined." />
      {(showAdd || editTask) && (
        <Modal title={editTask ? 'Edit Task' : 'Add Task'} isOpen={showAdd || !!editTask} onClose={() => { setShowAdd(false); setEditTask(null) }}>
          <div className="space-y-4">
            <Input label="Task Code" value={form.taskCode} onChange={e => setForm(f => ({ ...f, taskCode: e.target.value }))} disabled={!!editTask} />
            <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Budgeted Hours" type="number" value={form.budgetedHours} onChange={e => setForm(f => ({ ...f, budgetedHours: Number(e.target.value) }))} />
              <Input label="Budgeted Cost" type="number" step="0.01" value={form.budgetedCost} onChange={e => setForm(f => ({ ...f, budgetedCost: Number(e.target.value) }))} />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => { setShowAdd(false); setEditTask(null) }}>Cancel</Button>
              <Button onClick={() => {
                if (editTask) { updateMutation.mutate({ taskId: editTask.id, data: { description: form.description, budgetedHours: form.budgetedHours, budgetedCost: form.budgetedCost } }) }
                else { addMutation.mutate(form) }
              }}>{editTask ? 'Save' : 'Add'}</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
