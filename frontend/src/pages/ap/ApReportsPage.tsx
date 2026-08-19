import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AlertCircle, Play } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getAgingReport,
  getVendorTrialBalance,
  getBatchRegister,
  getCashRequirements,
  getForm1099Summary,
  getCheckRegister,
  getApAccountDistribution,
} from '@api/ap'
import type {
  ApAccountDistributionReport,
  ApAgingReport,
  ApBatchRegisterReport,
  CashRequirementsReport,
  CheckRegisterReport,
  Form1099SummaryResult,
  VendorTrialBalanceReport,
} from '@/types/ap'
import { paymentMethodMap, paymentStatusMap, voucherBatchStatusMap } from './statusMaps'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'

type ReportKey =
  | 'aging'
  | 'vendorTrialBalance'
  | 'batchRegister'
  | 'cashRequirements'
  | 'form1099'
  | 'checkRegister'
  | 'accountDistribution'

const reportOptions: { key: ReportKey; label: string; description: string; hasDaysAhead: boolean; hasTaxYear: boolean; hasDateRange: boolean }[] = [
  { key: 'aging', label: 'Aging', description: 'Unpaid vendor balances by age bucket', hasDaysAhead: false, hasTaxYear: false, hasDateRange: false },
  { key: 'vendorTrialBalance', label: 'Vendor Trial Balance', description: 'Vendor activity and ending balances', hasDaysAhead: false, hasTaxYear: false, hasDateRange: false },
  { key: 'batchRegister', label: 'Batch Register', description: 'Voucher batches and totals', hasDaysAhead: false, hasTaxYear: false, hasDateRange: false },
  { key: 'cashRequirements', label: 'Cash Requirements', description: 'Upcoming vendor payments', hasDaysAhead: true, hasTaxYear: false, hasDateRange: false },
  { key: 'form1099', label: '1099 Summary', description: 'Reportable payments by vendor for a tax year', hasDaysAhead: false, hasTaxYear: true, hasDateRange: false },
  { key: 'checkRegister', label: 'Check Register', description: 'Issued payments in a date range', hasDaysAhead: false, hasTaxYear: false, hasDateRange: true },
  { key: 'accountDistribution', label: 'Account Distribution', description: 'Posted AP distribution by account', hasDaysAhead: false, hasTaxYear: false, hasDateRange: true },
]

function summaryGrid(items: { label: string; value: string; highlight?: boolean }[]) {
  return (
    <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
      {items.map(item => (
        <div key={item.label} className="rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 p-3">
          <p className="text-xs text-gray-500 dark:text-gray-400">{item.label}</p>
          <p className={`mt-0.5 text-sm font-semibold font-tabular tabular-nums ${item.highlight ? 'text-primary-600 dark:text-primary-400' : 'text-gray-900 dark:text-white'}`}>
            {item.value}
          </p>
        </div>
      ))}
    </div>
  )
}

function dataTable(headers: string[], rows: (string | React.ReactNode)[][], align?: ('left' | 'right')[]) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
            {headers.map((h, i) => (
              <th key={h} className={`px-3 py-2 font-medium text-gray-500 dark:text-gray-400 ${align?.[i] === 'right' ? 'text-right' : ''}`}>
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
          {rows.map((row, ri) => (
            <tr key={ri} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
              {row.map((cell, ci) => (
                <td key={ci} className={`px-3 py-2.5 ${align?.[ci] === 'right' ? 'text-right font-tabular tabular-nums' : ''} ${typeof cell === 'string' && /^-?\d/.test(cell) ? 'font-tabular tabular-nums' : ''}`}>
                  {cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function money(value: number): string {
  return formatCurrency(value)
}

function vendorCell(code: string, name: string): React.ReactNode {
  return (
    <div>
      <span className="font-mono text-xs text-gray-500 dark:text-gray-400">{code}</span>
      <p className="text-gray-900 dark:text-white">{name}</p>
    </div>
  )
}

export function ApReportsPage() {
  const [activeReport, setActiveReport] = useState<ReportKey>('aging')
  const [daysAhead, setDaysAhead] = useState('30')
  const [taxYear, setTaxYear] = useState(String(new Date().getFullYear()))
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [runId, setRunId] = useState(0)
  const [error, setError] = useState<string | null>(null)

  const config = reportOptions.find(r => r.key === activeReport)!

  const run = () => {
    setError(null)
    if (config.hasTaxYear && !taxYear) {
      setError('Enter a tax year.')
      return
    }
    setRunId(id => id + 1)
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="AP Reports" description="Generate and view Accounts Payable reports for the current company." />
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {reportOptions.map(option => (
              <button
                key={option.key}
                type="button"
                onClick={() => {
                  setActiveReport(option.key)
                  setError(null)
                }}
                className={`px-3 py-1.5 text-sm font-medium rounded-lg border transition-colors duration-fast ${
                  activeReport === option.key
                    ? 'border-primary-600 bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300'
                    : 'border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:border-gray-400 dark:hover:border-gray-500'
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>

          <div className="mt-4 flex flex-wrap items-end gap-4">
            {config.hasDaysAhead && (
              <Input
                label="Days Ahead"
                type="number"
                step="1"
                min="1"
                value={daysAhead}
                onChange={e => setDaysAhead(e.target.value)}
                className="w-44"
              />
            )}
            {config.hasTaxYear && (
              <Input
                label="Tax Year"
                type="number"
                step="1"
                min="2000"
                max="2100"
                value={taxYear}
                onChange={e => setTaxYear(e.target.value)}
                className="w-44"
              />
            )}
            {config.hasDateRange && (
              <>
                <Input
                  label="From Date"
                  type="date"
                  value={fromDate}
                  onChange={e => setFromDate(e.target.value)}
                  className="w-44"
                />
                <Input
                  label="To Date"
                  type="date"
                  value={toDate}
                  onChange={e => setToDate(e.target.value)}
                  className="w-44"
                />
              </>
            )}
            <Button variant="primary" size="sm" onClick={run} leftIcon={<Play className="h-4 w-4" />}>
              Run Report
            </Button>
          </div>

          <p className="mt-3 text-sm text-gray-500 dark:text-gray-400">{config.description}</p>
        </CardContent>
      </Card>

      {error && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      {runId > 0 && (
        <ReportResult
          key={`${activeReport}-${runId}`}
          reportKey={activeReport}
          filters={{ daysAhead, taxYear, fromDate, toDate }}
        />
      )}
    </div>
  )
}

function ReportResult({ reportKey, filters }: { reportKey: ReportKey; filters: { daysAhead: string; taxYear: string; fromDate: string; toDate: string } }) {
  const query = useQuery({
    queryKey: ['ap', 'reports', reportKey, filters],
    queryFn: async () => {
      const from = filters.fromDate ? new Date(filters.fromDate).toISOString() : undefined
      const to = filters.toDate ? new Date(filters.toDate).toISOString() : undefined
      switch (reportKey) {
        case 'aging': {
          const report = await getAgingReport()
          return renderAging(report)
        }
        case 'vendorTrialBalance': {
          const report = await getVendorTrialBalance()
          return renderVendorTrialBalance(report)
        }
        case 'batchRegister': {
          const report = await getBatchRegister()
          return renderBatchRegister(report)
        }
        case 'cashRequirements': {
          const report = await getCashRequirements({ daysAhead: Number(filters.daysAhead) || 30 })
          return renderCashRequirements(report)
        }
        case 'form1099': {
          const report = await getForm1099Summary(Number(filters.taxYear) || new Date().getFullYear())
          return renderForm1099(report)
        }
        case 'checkRegister': {
          const report = await getCheckRegister({ ...(from ? { fromDate: from } : {}), ...(to ? { toDate: to } : {}) })
          return renderCheckRegister(report)
        }
        case 'accountDistribution': {
          const report = await getApAccountDistribution({ ...(from ? { fromDate: from } : {}), ...(to ? { toDate: to } : {}) })
          return renderAccountDistribution(report)
        }
      }
    },
  })

  if (query.isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (query.isError) {
    return (
      <Card>
        <CardContent className="py-10 text-center">
          <p className="text-sm text-red-600 dark:text-red-400">{getErrorMessage(query.error)}</p>
        </CardContent>
      </Card>
    )
  }

  if (!query.data) return null

  return (
    <Card>
      <CardHeader title={query.data.title} description={`Generated ${formatDate(new Date())}`} />
      <CardContent className="space-y-6">{query.data.body}</CardContent>
    </Card>
  )
}

function renderAging(report: ApAgingReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Aged Payables',
    body: (
      <>
        {summaryGrid([
          { label: 'As Of', value: formatDate(report.asOfDate) },
          { label: 'Total Current', value: money(report.totalCurrent) },
          { label: 'Total Due', value: money(report.totalDue), highlight: true },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No outstanding payables.</p>
        ) : (
          dataTable(
            ['Vendor', 'Current', '1-30', '31-60', '61-90', 'Over 90', 'Total Due'],
            report.lines.map(line => [
              vendorCell(line.vendorCode, line.vendorName),
              money(line.currentBalance),
              money(line.days1To30),
              money(line.days31To60),
              money(line.days61To90),
              money(line.over90Days),
              money(line.totalDue),
            ]),
            ['left', 'right', 'right', 'right', 'right', 'right', 'right']
          )
        )}
      </>
    ),
  }
}

function renderVendorTrialBalance(report: VendorTrialBalanceReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Vendor Trial Balance',
    body: (
      <>
        {summaryGrid([
          { label: 'As Of', value: formatDate(report.asOfDate) },
          { label: 'Total Beginning', value: money(report.totalBeginningBalance) },
          { label: 'Total Ending', value: money(report.totalEndingBalance), highlight: true },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No vendor activity.</p>
        ) : (
          dataTable(
            ['Vendor', 'Beginning', 'Debits', 'Credits', 'Ending Balance'],
            report.lines.map(line => [
              vendorCell(line.vendorCode, line.vendorName),
              money(line.beginningBalance),
              money(line.debits),
              money(line.credits),
              money(line.endingBalance),
            ]),
            ['left', 'right', 'right', 'right', 'right']
          )
        )}
      </>
    ),
  }
}

function renderBatchRegister(report: ApBatchRegisterReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Batch Register',
    body: (
      <>
        {summaryGrid([
          { label: 'Batches', value: String(report.totalBatches) },
          { label: 'Grand Total', value: money(report.grandTotal), highlight: true },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No voucher batches.</p>
        ) : (
          dataTable(
            ['Batch #', 'Description', 'Posting Date', 'Status', 'Vouchers', 'Total', 'Discount'],
            report.lines.map(line => [
              <span key={line.batchId} className="font-mono text-xs">{line.batchNumber}</span>,
              line.description,
              formatDate(line.postingDate),
              <MapStatusBadge key={`${line.batchId}-status`} value={line.status} mapping={voucherBatchStatusMap} />,
              String(line.voucherCount),
              money(line.totalAmount),
              line.totalDiscount > 0 ? money(line.totalDiscount) : '—',
            ]),
            ['left', 'left', 'left', 'left', 'right', 'right', 'right']
          )
        )}
      </>
    ),
  }
}

function renderCashRequirements(report: CashRequirementsReport): { title: string; body: React.ReactNode } {
  return {
    title: `Cash Requirements (${report.daysAhead} days)`,
    body: (
      <>
        {summaryGrid([
          { label: 'As Of', value: formatDate(report.asOfDate) },
          { label: 'Due (not past due)', value: money(report.totalDue) },
          { label: 'Past Due', value: money(report.totalPastDue) },
          { label: 'Grand Total', value: money(report.grandTotal), highlight: true },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No upcoming payments in this window.</p>
        ) : (
          dataTable(
            ['Vendor', 'Invoice', 'Due Date', 'Amount', 'Discount', 'Net Due', 'Past Due'],
            report.lines.map(line => [
              vendorCell(line.vendorCode, line.vendorName),
              <span key={line.voucherId} className="font-mono text-xs">{line.invoiceNumber}</span>,
              formatDate(line.dueDate),
              money(line.originalAmount),
              line.discountAmount > 0 ? money(line.discountAmount) : '—',
              money(line.netDue),
              line.pastDue ? 'Yes' : '—',
            ]),
            ['left', 'left', 'left', 'right', 'right', 'right', 'left']
          )
        )}
      </>
    ),
  }
}

function renderForm1099(report: Form1099SummaryResult): { title: string; body: React.ReactNode } {
  return {
    title: `1099 Summary — ${report.taxYear}`,
    body: (
      <>
        {summaryGrid([
          { label: 'Total Payments', value: money(report.totalPayments) },
          { label: 'Total Backup Withholding', value: money(report.totalBackupWithholding) },
        ])}
        {report.vendors.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No reportable payments for this tax year.</p>
        ) : (
          dataTable(
            ['Vendor', 'Category', 'Payments', 'Backup Withholding'],
            report.vendors.map(line => [
              vendorCell(line.vendorIdCode, line.name),
              line.category,
              money(line.totalPayments),
              line.backupWithholdingAmount > 0 ? money(line.backupWithholdingAmount) : '—',
            ]),
            ['left', 'left', 'right', 'right']
          )
        )}
      </>
    ),
  }
}

function renderCheckRegister(report: CheckRegisterReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Check Register',
    body: (
      <>
        {summaryGrid([
          { label: 'From', value: formatDate(report.fromDate) },
          { label: 'To', value: formatDate(report.toDate) },
          { label: 'Payments', value: String(report.totalChecks) },
          { label: 'Total', value: money(report.totalAmount), highlight: true },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No payments in this range.</p>
        ) : (
          dataTable(
            ['Reference', 'Vendor', 'Date', 'Method', 'Status', 'Amount'],
            report.lines.map(line => [
              <span key={line.paymentId} className="font-mono text-xs">{line.paymentReference}</span>,
              line.vendorName,
              formatDate(line.paymentDate),
              paymentMethodMap[String(line.paymentMethod)] ?? line.paymentMethod,
              <MapStatusBadge key={`${line.paymentId}-status`} value={line.status} mapping={paymentStatusMap} />,
              money(line.amount),
            ]),
            ['left', 'left', 'left', 'left', 'left', 'right']
          )
        )}
      </>
    ),
  }
}

function renderAccountDistribution(report: ApAccountDistributionReport): { title: string; body: React.ReactNode } {
  return {
    title: 'AP Account Distribution',
    body: (
      <>
        {summaryGrid([
          { label: 'Total Debits', value: money(report.totalDebit) },
          { label: 'Total Credits', value: money(report.totalCredit) },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No posted AP distributions in this range.</p>
        ) : (
          dataTable(
            ['Account', 'Description', 'Transactions', 'Debit', 'Credit'],
            report.lines.map(line => [
              <span key={line.accountId} className="font-mono text-xs">{line.accountNumber}</span>,
              line.accountDescription,
              String(line.transactionCount),
              money(line.debit),
              money(line.credit),
            ]),
            ['left', 'left', 'right', 'right', 'right']
          )
        )}
      </>
    ),
  }
}
