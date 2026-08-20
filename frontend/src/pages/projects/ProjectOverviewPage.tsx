import { FolderKanban } from 'lucide-react'
import { ProjectSectionPage } from './ProjectSectionPage'
import type { ProjectSummary } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')

export function ProjectOverviewPage() {
  return (
    <ProjectSectionPage title="Project Overview">
      {({ project }: { project: ProjectSummary }) => (
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
      )}
    </ProjectSectionPage>
  )
}
