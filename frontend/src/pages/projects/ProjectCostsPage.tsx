import { useState } from 'react'
import { Plus } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getProjectTasks, getCostTransactions, postCost, getCostSummary } from '@api/projectAccounting'
import { useQuery, useMutation } from '@tanstack/react-query'
import { ProjectSectionPage } from './ProjectSectionPage'
import type { ProjectSummary, ProjectTask, CostTransaction } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')
const PCT = (v?: number | null) => (v != null && !Number.isNaN(v) ? `${v.toFixed(1)}%` : '—')

export function ProjectCostsPage() {
  return (
    <ProjectSectionPage title="Project Costs">
      {({ project, setError, queryClient }) => <CostsContent project={project} setError={setError} queryClient={queryClient} />}
    </ProjectSectionPage>
  )
}

function CostsContent({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
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
        <div className="space-y-4">
          <div className="grid grid-cols-4 gap-4 text-sm">
            <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800"><p className="text-gray-500">Total Costs</p><p className="font-semibold">{MONEY(summary.totalCosts)}</p></div>
            <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800"><p className="text-gray-500">Budget</p><p className="font-semibold">{MONEY(summary.totalBudget)}</p></div>
            <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800"><p className="text-gray-500">Remaining</p><p className="font-semibold">{MONEY(summary.remaining)}</p></div>
            <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800"><p className="text-gray-500">% Complete</p><p className="font-semibold">{PCT(summary.percentComplete)}</p></div>
          </div>
          {summary.byCategory && Object.keys(summary.byCategory).length > 0 && (
            <div className="rounded-lg border bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
              <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-2">Costs by Category</h4>
              <table className="w-full text-sm">
                <thead><tr className="border-b dark:border-gray-700">
                  <th className="px-2 py-1 text-left text-gray-500">Category</th>
                  <th className="px-2 py-1 text-right text-gray-500">Actual</th>
                  <th className="px-2 py-1 text-right text-gray-500">Budget</th>
                  <th className="px-2 py-1 text-right text-gray-500">Hours</th>
                  <th className="px-2 py-1 text-right text-gray-500">Variance</th>
                </tr></thead>
                <tbody>
                  {Object.entries(summary.byCategory).map(([cat, data]) => (
                    <tr key={cat} className="border-b dark:border-gray-700/50">
                      <td className="px-2 py-1 font-medium">{cat}</td>
                      <td className="px-2 py-1 text-right">{MONEY(data.actual)}</td>
                      <td className="px-2 py-1 text-right">{MONEY(data.budget)}</td>
                      <td className="px-2 py-1 text-right">{data.hours}</td>
                      <td className={`px-2 py-1 text-right font-medium ${data.variance < 0 ? 'text-red-600' : 'text-green-600'}`}>{MONEY(data.variance)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
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
