// Project Accounting Reports (Phase 10) — portfolio analysis reports wired to the analysis API.
import { useEffect, useState } from 'react'
import {
  ArrowRightLeft,
  BadgeDollarSign,
  CalendarClock,
  DollarSign,
  FileCheck,
  FileText,
  GitBranch,
  Layers,
  LineChart,
  Scale,
  TrendingDown,
  TrendingUp,
  UserCheck,
  Users,
} from 'lucide-react'
import { getErrorMessage } from '@api/client'
import {
  getCertifiedPayrollReport,
  getContractAssetLiabilityReport,
  getContractValueAnalysisReport,
  getEacTrendPortfolio,
  getEarnedValueReport,
  getEmployeeProfitabilityReport,
  getEmployeeUtilizationReport,
  getLienWaiverRegisterReport,
  getPendingCoImpactReport,
  getPmPerformanceReport,
  getPortfolioDashboard,
  getProjectAgingReport,
  getSubcontractCommitmentReport,
  getSubcontractStatusReport,
} from '@api/projectAccounting'

type ReportKey =
  | 'employee-utilization'
  | 'employee-profitability'
  | 'subcontract-status'
  | 'subcontract-commitment'
  | 'certified-payroll'
  | 'portfolio-dashboard'
  | 'project-aging'
  | 'contract-value-analysis'
  | 'pm-performance'
  | 'earned-value'
  | 'pending-co-impact'
  | 'lien-waiver-register'
  | 'contract-asset-liability'
  | 'eac-trend'

interface ColumnDef {
  header: string
  key: string
  format?: 'money' | 'pct' | 'date' | 'bool' | 'number'
}

interface ReportDef {
  label: string
  icon: typeof Users
  load: () => Promise<unknown[]>
  columns: ColumnDef[]
}

const periodFrom = new Date(Date.now() - 30 * 24 * 3600 * 1000).toISOString()
const periodTo = new Date().toISOString()

const reports: Record<ReportKey, ReportDef> = {
  'employee-utilization': {
    label: 'Employee Utilization',
    icon: Users,
    load: () => getEmployeeUtilizationReport({ from: periodFrom, to: periodTo, capacityHours: 160 }),
    columns: [
      { header: 'Employee', key: 'employeeId' },
      { header: 'Total Hours', key: 'totalHours', format: 'number' },
      { header: 'Billable Hours', key: 'billableHours', format: 'number' },
      { header: 'Billable %', key: 'billablePercent', format: 'pct' },
      { header: 'Capacity', key: 'capacityHours', format: 'number' },
      { header: 'Utilization %', key: 'utilizationPercent', format: 'pct' },
      { header: 'Labor Cost', key: 'laborCost', format: 'money' },
    ],
  },
  'employee-profitability': {
    label: 'Employee Profitability',
    icon: DollarSign,
    load: () => getEmployeeProfitabilityReport({ from: periodFrom, to: periodTo }),
    columns: [
      { header: 'Employee', key: 'employeeId' },
      { header: 'Billed', key: 'billedAmount', format: 'money' },
      { header: 'Unbilled Billable', key: 'unbilledBillableAmount', format: 'money' },
      { header: 'Cost', key: 'costAmount', format: 'money' },
      { header: 'Margin', key: 'margin', format: 'money' },
      { header: 'Margin %', key: 'marginPercent', format: 'pct' },
    ],
  },
  'subcontract-status': {
    label: 'Subcontract Status',
    icon: FileText,
    load: () => getSubcontractStatusReport(),
    columns: [
      { header: 'Number', key: 'subcontractNumber' },
      { header: 'Project', key: 'projectCode' },
      { header: 'Status', key: 'status' },
      { header: 'Contract', key: 'contractAmount', format: 'money' },
      { header: 'Approved COs', key: 'approvedChangeOrders', format: 'money' },
      { header: 'Revised', key: 'revisedAmount', format: 'money' },
      { header: 'Invoiced to Date', key: 'invoicedToDate', format: 'money' },
      { header: 'Retainage Held', key: 'retainageHeld', format: 'money' },
      { header: 'Remaining', key: 'remaining', format: 'money' },
    ],
  },
  'subcontract-commitment': {
    label: 'Subcontract Commitment',
    icon: Layers,
    load: () => getSubcontractCommitmentReport(),
    columns: [
      { header: 'Project', key: 'projectCode' },
      { header: 'Open Subs', key: 'openSubcontractCount', format: 'number' },
      { header: 'Committed', key: 'committedTotal', format: 'money' },
      { header: 'Invoiced', key: 'invoicedAgainstCommitted', format: 'money' },
      { header: 'Remaining Commitment', key: 'remainingCommitment', format: 'money' },
      { header: 'Budget Remaining', key: 'projectBudgetRemaining', format: 'money' },
    ],
  },
  'certified-payroll': {
    label: 'Certified Payroll',
    icon: BadgeDollarSign,
    load: () => getCertifiedPayrollReport({ from: periodFrom, to: periodTo }),
    columns: [
      { header: 'Employee', key: 'employeeId' },
      { header: 'Project', key: 'projectCode' },
      { header: 'Classification', key: 'classification' },
      { header: 'Hours', key: 'hours', format: 'number' },
      { header: 'Wages', key: 'wageAmount', format: 'money' },
      { header: 'Rate', key: 'hourlyRate', format: 'money' },
    ],
  },
  'portfolio-dashboard': {
    label: 'Portfolio Dashboard',
    icon: LineChart,
    load: () => getPortfolioDashboard(),
    columns: [
      { header: 'Code', key: 'projectCode' },
      { header: 'Name', key: 'name' },
      { header: 'PM', key: 'projectManager' },
      { header: 'Contract Value', key: 'contractValue', format: 'money' },
      { header: 'Revenue', key: 'revenue', format: 'money' },
      { header: 'Costs', key: 'costs', format: 'money' },
      { header: 'Margin %', key: 'marginPercent', format: 'pct' },
      { header: '% Complete', key: 'percentComplete', format: 'pct' },
      { header: 'EAC', key: 'estimateAtCompletion', format: 'money' },
      { header: 'Risk', key: 'riskStatus' },
    ],
  },
  'project-aging': {
    label: 'Project Aging',
    icon: CalendarClock,
    load: () => getProjectAgingReport(),
    columns: [
      { header: 'Code', key: 'projectCode' },
      { header: 'Name', key: 'name' },
      { header: 'Group', key: 'statusGroup' },
      { header: 'Age Days', key: 'ageDays', format: 'number' },
      { header: 'Over 1 Year', key: 'overOneYear', format: 'bool' },
      { header: 'Over Budget', key: 'overBudget', format: 'bool' },
      { header: 'Neg Margin', key: 'negativeMargin', format: 'bool' },
      { header: 'Actionable', key: 'actionable', format: 'bool' },
    ],
  },
  'contract-value-analysis': {
    label: 'Contract Value Analysis',
    icon: Scale,
    load: () => getContractValueAnalysisReport(),
    columns: [
      { header: 'Contract Type', key: 'contractType' },
      { header: 'Projects', key: 'projectCount', format: 'number' },
      { header: 'Total Value', key: 'totalContractValue', format: 'money' },
      { header: 'Avg Margin %', key: 'averageMarginPercent', format: 'pct' },
    ],
  },
  'pm-performance': {
    label: 'PM Performance',
    icon: UserCheck,
    load: () => getPmPerformanceReport(),
    columns: [
      { header: 'Manager', key: 'projectManager' },
      { header: 'Projects', key: 'projectCount', format: 'number' },
      { header: 'Active', key: 'activeCount', format: 'number' },
      { header: 'On Time', key: 'completedOnTimeCount', format: 'number' },
      { header: 'With Schedule', key: 'completedWithScheduleCount', format: 'number' },
      { header: 'Avg Margin %', key: 'averageMarginPercent', format: 'pct' },
      { header: 'On Budget %', key: 'onBudgetPercent', format: 'pct' },
    ],
  },
  'earned-value': {
    label: 'Earned Value',
    icon: TrendingUp,
    load: () => getEarnedValueReport(),
    columns: [
      { header: 'Code', key: 'projectCode' },
      { header: 'BAC', key: 'bac', format: 'money' },
      { header: 'BCWS', key: 'bcws', format: 'money' },
      { header: 'BCWP', key: 'bcwp', format: 'money' },
      { header: 'ACWP', key: 'acwp', format: 'money' },
      { header: 'SV', key: 'sv', format: 'money' },
      { header: 'CV', key: 'cv', format: 'money' },
      { header: 'SPI', key: 'spi', format: 'number' },
      { header: 'CPI', key: 'cpi', format: 'number' },
      { header: 'EAC', key: 'eac', format: 'money' },
    ],
  },
  'pending-co-impact': {
    label: 'Pending CO Impact',
    icon: GitBranch,
    load: () => getPendingCoImpactReport(),
    columns: [
      { header: 'Code', key: 'projectCode' },
      { header: 'Contract Excl.', key: 'contractValueExcludingPending', format: 'money' },
      { header: 'Approved COs', key: 'approvedChangeOrders', format: 'money' },
      { header: 'Pending COs', key: 'pendingChangeOrders', format: 'money' },
      { header: 'Contract Incl.', key: 'contractValueIncludingPending', format: 'money' },
      { header: 'EAC', key: 'estimateAtCompletion', format: 'money' },
      { header: 'Proj. Revenue', key: 'projectedRevenueIncludingPending', format: 'money' },
      { header: 'Proj. Margin', key: 'projectedMargin', format: 'money' },
      { header: 'Proj. Margin %', key: 'projectedMarginPercent', format: 'pct' },
    ],
  },
  'lien-waiver-register': {
    label: 'Lien Waiver Register',
    icon: FileCheck,
    load: () => getLienWaiverRegisterReport(),
    columns: [
      { header: 'Type', key: 'waiverType' },
      { header: 'Final', key: 'isFinal', format: 'bool' },
      { header: 'Effective', key: 'effectiveDate', format: 'date' },
      { header: 'Amount', key: 'amount', format: 'money' },
      { header: 'Subcontract', key: 'subcontractNumber' },
      { header: 'Project', key: 'projectCode' },
      { header: 'Notes', key: 'description' },
    ],
  },
  'contract-asset-liability': {
    label: 'Contract Asset/Liability',
    icon: ArrowRightLeft,
    load: () => getContractAssetLiabilityReport(),
    columns: [
      { header: 'Code', key: 'projectCode' },
      { header: 'Name', key: 'name' },
      { header: 'Earned', key: 'earnedRevenue', format: 'money' },
      { header: 'Billed', key: 'billedRevenue', format: 'money' },
      { header: 'Asset', key: 'contractAsset', format: 'money' },
      { header: 'Liability', key: 'contractLiability', format: 'money' },
      { header: 'Classification', key: 'classification' },
    ],
  },
  'eac-trend': {
    label: 'EAC Trend',
    icon: TrendingDown,
    load: () => getEacTrendPortfolio(),
    columns: [
      { header: 'Capture Date', key: 'captureDate', format: 'date' },
      { header: 'Projects', key: 'projectCount', format: 'number' },
      { header: 'Avg Est. Margin %', key: 'averageEstimatedMarginPct', format: 'pct' },
      { header: 'Avg EAC', key: 'averageEstimateAtCompletion', format: 'money' },
    ],
  },
}

const money = (value: unknown): string =>
  `$${Number(value ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`

function renderCell(col: ColumnDef, row: Record<string, unknown>) {
  const value = row[col.key]
  if (col.format === 'bool') return value ? 'Yes' : 'No'
  if (value === null || value === undefined || value === '') return '\u2014'
  if (col.format === 'money') return money(value)
  if (col.format === 'pct') return `${Number(value).toFixed(1)}%`
  if (col.format === 'number') return Number(value).toLocaleString()
  if (col.format === 'date') return new Date(String(value)).toLocaleDateString()
  if (col.key === 'riskStatus' && value === 'Negative Margin') {
    return <span className="font-medium text-red-600">{String(value)}</span>
  }
  if (col.key === 'riskStatus' && value === 'Over Budget Risk') {
    return <span className="font-medium text-amber-600">{String(value)}</span>
  }
  return String(value)
}

export function ProjectsReportsPage() {
  const [active, setActive] = useState<ReportKey>('portfolio-dashboard')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [rows, setRows] = useState<Record<string, unknown>[]>([])

  const report = reports[active]

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        const data = await report.load()
        setRows(data as Record<string, unknown>[])
      } catch (e) {
        setError(getErrorMessage(e))
        setRows([])
      } finally {
        setLoading(false)
      }
    }
    void load()
  }, [report])

  const tabs = Object.entries(reports) as [ReportKey, ReportDef][]

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Project Reports</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Portfolio analysis: utilization, subcontracts, certified payroll, earned value and more
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        {tabs.map(([key, def]) => (
          <button
            key={key}
            onClick={() => setActive(key)}
            className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium ${
              active === key
                ? 'bg-primary-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-200'
            }`}
          >
            <def.icon className="h-4 w-4" />
            {def.label}
          </button>
        ))}
      </div>

      {loading && <p className="text-sm text-gray-500">Loading…</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}

      {!loading && !error && rows.length === 0 && (
        <p className="text-sm text-gray-400">No records found.</p>
      )}

      {!loading && !error && rows.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-800">
              <tr>
                {report.columns.map((col) => (
                  <th key={col.key} className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">
                    {col.header}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {rows.map((row, i) => (
                <tr key={i}>
                  {report.columns.map((col) => (
                    <td key={col.key} className="px-4 py-2 text-sm">
                      {renderCell(col, row)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
