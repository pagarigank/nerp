// Shared wrapper for project section pages — provides project selector + KPI header.
import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { FolderKanban, Pencil } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { Select } from '@components/ui/Input'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { getProjects, updateProject } from '@api/projectAccounting'
import { getCustomers } from '@api/ar'
import type { ProjectSummary } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')
const PCT = (v?: number | null) => (v != null && !Number.isNaN(v) ? `${v.toFixed(1)}%` : '—')

export function ProjectSectionPage({ children, title }: { children: (props: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) => React.ReactNode; title: string }) {
  const queryClient = useQueryClient()
  const [searchParams] = useSearchParams()
  const projectId = searchParams.get('projectId')
  const [selectedId, setSelectedId] = useState<string | null>(projectId)
  const [error, setError] = useState<string | null>(null)
  const [showEdit, setShowEdit] = useState(false)

  useEffect(() => {
    if (projectId) setSelectedId(projectId)
  }, [projectId])

  const { data: projects = [] } = useQuery({ queryKey: ['projects'], queryFn: () => getProjects() })
  const { data: customers = [] } = useQuery({ queryKey: ['ar', 'customers'], queryFn: () => getCustomers() })

  const project = (projects as ProjectSummary[]).find((p: ProjectSummary) => p.id === selectedId) ?? (projects as ProjectSummary[])[0]

  // Auto-select first project if none selected
  useEffect(() => {
    if (!selectedId && (projects as ProjectSummary[]).length > 0) {
      setSelectedId((projects as ProjectSummary[])[0].id)
    }
  }, [projects, selectedId])

  if (!project) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
          <FolderKanban className="h-6 w-6" /> {title}
        </h1>
        <p className="text-sm text-gray-500">No projects found. Create a project first.</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <FolderKanban className="h-6 w-6 text-indigo-600" />
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">{title}</h1>
        </div>
        <Button variant="outline" onClick={() => setShowEdit(true)}><Pencil className="h-4 w-4 mr-1" /> Edit</Button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{error}</div>
      )}

      <div className="flex items-center gap-3">
        <Select
          label="Project"
          value={selectedId ?? ''}
          onChange={(e: any) => setSelectedId(e.target.value)}
          options={(projects as ProjectSummary[]).map((p: ProjectSummary) => ({ value: p.id, label: `${p.projectCode} — ${p.name}` }))}
        />
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

      {children({ project, setError, queryClient })}
    </div>
  )
}
