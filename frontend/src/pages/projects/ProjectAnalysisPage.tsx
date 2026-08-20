import { DataTable } from '@components/ui/DataTable'
import { getWipScheduleAnalysis, getForecast, getProfitability, getBudgetVsActual, getUnbilled, getChangeOrderSummary } from '@api/projectAccounting'
import { useQuery } from '@tanstack/react-query'
import { ProjectSectionPage } from './ProjectSectionPage'
import type { ProjectSummary } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')

export function ProjectAnalysisPage() {
  return (
    <ProjectSectionPage title="WIP / Analysis">
      {({ project }: { project: ProjectSummary }) => <AnalysisContent project={project} />}
    </ProjectSectionPage>
  )
}

function AnalysisContent({ project }: { project: ProjectSummary }) {
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
