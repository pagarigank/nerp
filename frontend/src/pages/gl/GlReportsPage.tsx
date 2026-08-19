import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AlertCircle, Play } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getTrialBalance,
  getGeneralLedgerDetail,
  getUnpostedTransactions,
  getBalanceSheet,
  getIncomeStatement,
  getCashFlow,
  getBudgetVsActual,
  getAccountDistribution,
  getConsolidatedTrialBalance,
  getIntercompanyBalance,
  getMultiCurrencyRevaluation,
  getBudgets,
} from '@api/gl'
import { getFiscalPeriods, getCompanies } from '@api/platform'
import type {
  AccountDistributionReport,
  BudgetVsActualReport,
  CashFlowReport,
  ConsolidatedTrialBalanceReport,
  FinancialStatementReport,
  GeneralLedgerDetailReport,
  IntercompanyBalanceReport,
  MultiCurrencyRevaluationReport,
  TrialBalanceReport,
  UnpostedTransactionsReport
} from '@/types/gl'
import { accountTypeMap } from '@pages/platform/statusMaps'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'

type ReportKey =
  | 'trialBalance'
  | 'generalLedger'
  | 'unposted'
  | 'balanceSheet'
  | 'incomeStatement'
  | 'cashFlow'
  | 'budgetVsActual'
  | 'accountDistribution'
  | 'consolidatedTrialBalance'
  | 'intercompanyBalance'
  | 'multiCurrencyRevaluation'

const reportOptions: { key: ReportKey; label: string; description: string; requiresBudget: boolean; hasDateRange: boolean; requiresParentCompany: boolean; requiresRevaluationDate: boolean }[] = [
  { key: 'trialBalance', label: 'Trial Balance', description: 'Account balances by period', requiresBudget: false, hasDateRange: false, requiresParentCompany: false, requiresRevaluationDate: false },
  { key: 'generalLedger', label: 'GL Detail', description: 'Posted journal lines', requiresBudget: false, hasDateRange: true, requiresParentCompany: false, requiresRevaluationDate: false },
  { key: 'unposted', label: 'Unposted Transactions', description: 'Batches not yet posted', requiresBudget: false, hasDateRange: false, requiresParentCompany: false, requiresRevaluationDate: false },
  { key: 'balanceSheet', label: 'Balance Sheet', description: 'Assets, liabilities, equity', requiresBudget: false, hasDateRange: false, requiresParentCompany: false, requiresRevaluationDate: false },
  { key: 'incomeStatement', label: 'Income Statement', description: 'Revenue and expenses', requiresBudget: false, hasDateRange: false, requiresParentCompany: false, requiresRevaluationDate: false },
  { key: 'cashFlow', label: 'Cash Flow', description: 'Operating, investing, financing', requiresBudget: false, hasDateRange: false, requiresParentCompany: false, requiresRevaluationDate: false },
  { key: 'budgetVsActual', label: 'Budget vs Actual', description: 'Compare budget to posted activity', requiresBudget: true, hasDateRange: false, requiresParentCompany: false, requiresRevaluationDate: false },
  { key: 'accountDistribution', label: 'Account Distribution', description: 'Posting volume by account', requiresBudget: false, hasDateRange: true, requiresParentCompany: false, requiresRevaluationDate: false },
  { key: 'consolidatedTrialBalance', label: 'Consolidated Trial Balance', description: 'Combined trial balance across parent and child companies', requiresBudget: false, hasDateRange: false, requiresParentCompany: true, requiresRevaluationDate: false },
  { key: 'intercompanyBalance', label: 'Intercompany Balance', description: 'Due-to/due-from balances between companies', requiresBudget: false, hasDateRange: false, requiresParentCompany: true, requiresRevaluationDate: false },
  { key: 'multiCurrencyRevaluation', label: 'Multi-Currency Revaluation', description: 'Gain/loss from foreign currency revaluation', requiresBudget: false, hasDateRange: false, requiresParentCompany: false, requiresRevaluationDate: true },
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

export function GlReportsPage() {
  const [activeReport, setActiveReport] = useState<ReportKey>('trialBalance')
  const [fiscalPeriodId, setFiscalPeriodId] = useState('')
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [budgetId, setBudgetId] = useState('')
  const [runId, setRunId] = useState(0)
  const [error, setError] = useState<string | null>(null)

  const config = reportOptions.find(r => r.key === activeReport)!

  const { data: periods = [] } = useQuery({
    queryKey: ['platform', 'fiscalPeriods'],
    queryFn: () => getFiscalPeriods(),
  })

  const { data: budgets = [] } = useQuery({
    queryKey: ['gl', 'budgets'],
    queryFn: () => getBudgets(),
  })

  const { data: companies = [] } = useQuery({
    queryKey: ['platform', 'companies'],
    queryFn: () => getCompanies(),
  })

  const parentCompanies = companies.filter(c => !c.parentCompanyId)

  const periodOptions = useMemo(
    () => periods.map(p => ({ value: p.id, label: `P${p.periodNumber} - ${p.description}` })),
    [periods]
  )

  const budgetOptions = useMemo(
    () => budgets.map(b => ({ value: b.id, label: `${b.name} (${formatCurrency(b.totalAmount)})` })),
    [budgets]
  )

  const parentCompanyOptions = useMemo(
    () => parentCompanies.map(c => ({ value: c.id, label: c.name })),
    [parentCompanies]
  )

  const canRun =
    (config.requiresBudget ? !!budgetId : true) &&
    (config.requiresParentCompany ? !!parentCompanyOptions.length : true)

  const reportResult = useReport(activeReport, {
    fiscalPeriodId,
    ...(fromDate ? { fromDate: new Date(fromDate).toISOString() } : {}),
    ...(toDate ? { toDate: new Date(toDate).toISOString() } : {}),
    budgetId,
  }, runId)

  const run = () => {
    setError(null)
    if (!canRun) {
      if (config.requiresBudget && !budgetId) {
        setError('Select a budget to run this report.')
      } else if (config.requiresParentCompany && parentCompanies.length === 0) {
        setError('No parent companies available. Create a parent company first.')
      }
      return
    }
    setRunId(id => id + 1)
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Financial Reports" description="Generate and view GL reports for the current company." />
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
            {!config.requiresBudget && (
              <Select
                label="Fiscal Period"
                placeholder="All periods"
                options={periodOptions}
                value={fiscalPeriodId}
                onChange={e => setFiscalPeriodId(e.target.value)}
                className="w-64"
              />
            )}
            {config.requiresBudget && (
              <Select
                label="Budget"
                placeholder="Select budget..."
                options={budgetOptions}
                value={budgetId}
                onChange={e => setBudgetId(e.target.value)}
                className="w-64"
                required
              />
            )}
            {config.requiresParentCompany && (
              <Select
                label="Parent Company"
                placeholder="Select parent company..."
                options={parentCompanyOptions}
                value={fiscalPeriodId}
                onChange={e => setFiscalPeriodId(e.target.value)}
                className="w-64"
                required
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
            {config.requiresRevaluationDate && (
              <Input
                label="Revaluation Date"
                type="date"
                value={fromDate}
                onChange={e => setFromDate(e.target.value)}
                className="w-44"
              />
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
        reportResult.isLoading ? (
          <div className="space-y-4">
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-64 w-full" />
          </div>
        ) : reportResult.error ? (
          <Card>
            <CardContent className="py-10 text-center">
              <p className="text-sm text-red-600 dark:text-red-400">{reportResult.errorMessage}</p>
            </CardContent>
          </Card>
        ) : reportResult.data ? (
          <Card>
            <CardHeader title={reportResult.data.title} description={`Generated ${formatDate(new Date())}`} />
            <CardContent className="space-y-6">
              {reportResult.data.body}
            </CardContent>
          </Card>
        ) : null
      )}
    </div>
  )
}

function useReport(
  key: ReportKey,
  filters: { fiscalPeriodId: string; fromDate?: string; toDate?: string; budgetId: string },
  runId: number
): { isLoading: boolean; error: boolean; errorMessage: string; data: { title: string; body: React.ReactNode } | null } {
  const query = useQuery({
    queryKey: ['gl', 'reports', key, filters, runId],
    queryFn: async () => {
      switch (key) {
        case 'trialBalance': {
          const report = await getTrialBalance({ fiscalPeriodId: filters.fiscalPeriodId || undefined })
          return renderTrialBalance(report)
        }
        case 'generalLedger': {
          const report = await getGeneralLedgerDetail({
            fiscalPeriodId: filters.fiscalPeriodId || undefined,
            fromDate: filters.fromDate,
            toDate: filters.toDate,
          })
          return renderGeneralLedger(report)
        }
        case 'unposted': {
          const report = await getUnpostedTransactions()
          return renderUnposted(report)
        }
        case 'balanceSheet': {
          const report = await getBalanceSheet({ fiscalPeriodId: filters.fiscalPeriodId || undefined })
          return renderFinancialStatement(report)
        }
        case 'incomeStatement': {
          const report = await getIncomeStatement({ fiscalPeriodId: filters.fiscalPeriodId || undefined })
          return renderFinancialStatement(report)
        }
        case 'cashFlow': {
          const report = await getCashFlow({ fiscalPeriodId: filters.fiscalPeriodId || undefined })
          return renderCashFlow(report)
        }
        case 'budgetVsActual': {
          const report = await getBudgetVsActual({
            budgetId: filters.budgetId,
            fiscalPeriodId: filters.fiscalPeriodId || undefined,
          })
          return renderBudgetVsActual(report)
        }
        case 'accountDistribution': {
          const report = await getAccountDistribution({
            fiscalPeriodId: filters.fiscalPeriodId || undefined,
            fromDate: filters.fromDate,
            toDate: filters.toDate,
          })
          return renderAccountDistribution(report)
        }
        case 'consolidatedTrialBalance': {
          const report = await getConsolidatedTrialBalance({ fiscalPeriodId: filters.fiscalPeriodId || undefined })
          return renderConsolidatedTrialBalance(report)
        }
        case 'intercompanyBalance': {
          const report = await getIntercompanyBalance({ fiscalPeriodId: filters.fiscalPeriodId || undefined })
          return renderIntercompanyBalance(report)
        }
        case 'multiCurrencyRevaluation': {
          const report = await getMultiCurrencyRevaluation({ fiscalPeriodId: filters.fiscalPeriodId || undefined, revaluationDate: filters.fromDate })
          return renderMultiCurrencyRevaluation(report)
        }
      }
    },
    enabled: runId > 0,
  })

  return {
    isLoading: query.isLoading,
    error: query.isError,
    errorMessage: query.isError ? getErrorMessage(query.error) : '',
    data: query.data ?? null,
  }
}

function renderTrialBalance(report: TrialBalanceReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Trial Balance',
    body: (
      <>
        {summaryGrid([
          { label: 'Company', value: report.companyName },
          { label: 'Total Debits', value: money(report.totalDebit) },
          { label: 'Total Credits', value: money(report.totalCredit) },
          { label: 'Balance', value: money(report.totalDebit - report.totalCredit), highlight: report.totalDebit === report.totalCredit },
        ])}
        {dataTable(
          ['Account', 'Description', 'Type', 'Debit', 'Credit', 'Ending Balance'],
          report.lines.map(line => [
            <span key={line.accountId} className="font-mono text-xs">{line.accountNumber}</span>,
            line.accountDescription,
            <MapStatusBadge key={`${line.accountId}-type`} value={line.accountType} mapping={accountTypeMap} />,
            money(line.debit),
            money(line.credit),
            money(line.endingBalance),
          ]),
          ['left', 'left', 'left', 'right', 'right', 'right']
        )}
      </>
    ),
  }
}

function renderGeneralLedger(report: GeneralLedgerDetailReport): { title: string; body: React.ReactNode } {
  return {
    title: 'General Ledger Detail',
    body: (
      <>
        {summaryGrid([
          { label: 'Company', value: report.companyName },
          { label: 'From', value: report.fromDate ? formatDate(report.fromDate) : '—' },
          { label: 'To', value: report.toDate ? formatDate(report.toDate) : '—' },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No posted transactions match the filters.</p>
        ) : (
          dataTable(
            ['Date', 'Batch', 'Account', 'Reference', 'Debit', 'Credit'],
            report.lines.map((line, i) => [
              formatDate(line.postingDate),
              <span key={`${i}-b`} className="font-mono text-xs">{line.batchNumber}</span>,
              <span key={`${i}-a`} className="font-mono text-xs">{line.accountNumber} - {line.accountDescription}</span>,
              line.reference ?? '—',
              line.debit > 0 ? money(line.debit) : '—',
              line.credit > 0 ? money(line.credit) : '—',
            ]),
            ['left', 'left', 'left', 'left', 'right', 'right']
          )
        )}
      </>
    ),
  }
}

function renderUnposted(report: UnpostedTransactionsReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Unposted Transactions',
    body: (
      <>
        {summaryGrid([
          { label: 'Company', value: report.companyName },
          { label: 'Batches', value: String(report.batches.length) },
        ])}
        {report.batches.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">All journal batches are posted.</p>
        ) : (
          dataTable(
            ['Batch #', 'Description', 'Posting Date', 'Status', 'Lines', 'Debits', 'Credits'],
            report.batches.map(batch => [
              <span key={batch.batchId} className="font-mono text-xs">{batch.batchNumber}</span>,
              batch.description,
              formatDate(batch.postingDate),
              batch.status,
              String(batch.lineCount),
              money(batch.totalDebits),
              money(batch.totalCredits),
            ]),
            ['left', 'left', 'left', 'left', 'right', 'right', 'right']
          )
        )}
      </>
    ),
  }
}

function renderFinancialStatement(report: FinancialStatementReport): { title: string; body: React.ReactNode } {
  return {
    title: report.statementType === 'BalanceSheet' ? 'Balance Sheet' : 'Income Statement',
    body: (
      <>
        {summaryGrid([
          { label: 'Company', value: report.companyName },
          { label: 'Total', value: money(report.totalAmount), highlight: true },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No activity for this statement.</p>
        ) : (
          dataTable(
            ['Account', 'Description', 'Balance'],
            report.lines.map(line => [
              <span key={line.accountId} className="font-mono text-xs">{line.accountNumber}</span>,
              line.accountDescription,
              money(line.balance),
            ]),
            ['left', 'left', 'right']
          )
        )}
      </>
    ),
  }
}

function renderCashFlow(report: CashFlowReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Cash Flow',
    body: (
      <>
        {summaryGrid([
          { label: 'Operating', value: money(report.netCashOperating) },
          { label: 'Investing', value: money(report.netCashInvesting) },
          { label: 'Financing', value: money(report.netCashFinancing) },
          { label: 'Net Change', value: money(report.netCashChange), highlight: true },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No cash flow activity.</p>
        ) : (
          dataTable(
            ['Category', 'Account', 'Description', 'Amount'],
            report.lines.map((line, i) => [
              line.category,
              <span key={`${i}-a`} className="font-mono text-xs">{line.accountNumber}</span>,
              line.accountDescription,
              money(line.amount),
            ]),
            ['left', 'left', 'left', 'right']
          )
        )}
      </>
    ),
  }
}

function renderBudgetVsActual(report: BudgetVsActualReport): { title: string; body: React.ReactNode } {
  return {
    title: `Budget vs Actual — ${report.budgetName}`,
    body: (
      <>
        {summaryGrid([
          { label: 'Budget', value: money(report.totalBudget) },
          { label: 'Actual', value: money(report.totalActual) },
          { label: 'Variance', value: money(report.totalVariance) },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No budget lines match the filters.</p>
        ) : (
          dataTable(
            ['Account', 'Description', 'Budget', 'Actual', 'Variance', 'Var %'],
            report.lines.map(line => [
              <span key={line.accountId} className="font-mono text-xs">{line.accountNumber}</span>,
              line.accountDescription,
              money(line.budgetAmount),
              money(line.actualAmount),
              money(line.variance),
              `${line.variancePercent.toFixed(2)}%`,
            ]),
            ['left', 'left', 'right', 'right', 'right', 'right']
          )
        )}
      </>
    ),
  }
}

function renderAccountDistribution(report: AccountDistributionReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Account Distribution',
    body: (
      <>
        {summaryGrid([
          { label: 'Total Debits', value: money(report.totalDebit) },
          { label: 'Total Credits', value: money(report.totalCredit) },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No posted activity matches the filters.</p>
        ) : (
          dataTable(
            ['Account', 'Description', 'Transactions', 'Debit', 'Credit', 'Net Change'],
            report.lines.map(line => [
              <span key={line.accountId} className="font-mono text-xs">{line.accountNumber}</span>,
              line.accountDescription,
              String(line.transactionCount),
              money(line.debit),
              money(line.credit),
              money(line.netChange),
            ]),
            ['left', 'left', 'right', 'right', 'right', 'right']
          )
        )}
      </>
    ),
  }
}

function renderConsolidatedTrialBalance(report: ConsolidatedTrialBalanceReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Consolidated Trial Balance',
    body: (
      <>
        {summaryGrid([
          { label: 'Parent Company', value: report.parentCompanyName },
          { label: 'Total Debits', value: money(report.totalDebit) },
          { label: 'Total Credits', value: money(report.totalCredit) },
          { label: 'Balance', value: money(report.totalDebit - report.totalCredit), highlight: report.totalDebit === report.totalCredit },
        ])}
        {dataTable(
          ['Company', 'Account', 'Description', 'Type', 'Debit', 'Credit', 'Ending Balance'],
          report.lines.map(line => [
            line.companyName,
            <span key={line.accountId} className="font-mono text-xs">{line.accountNumber}</span>,
            line.accountDescription,
            <MapStatusBadge key={`${line.accountId}-type`} value={line.accountType} mapping={accountTypeMap} />,
            money(line.debit),
            money(line.credit),
            money(line.endingBalance),
          ]),
          ['left', 'left', 'left', 'left', 'right', 'right', 'right']
        )}
      </>
    ),
  }
}

function renderIntercompanyBalance(report: IntercompanyBalanceReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Intercompany Balance Report',
    body: (
      <>
        {summaryGrid([
          { label: 'Parent Company', value: report.parentCompanyName },
          { label: 'Mappings', value: String(report.lines.length) },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No intercompany balances found.</p>
        ) : (
          dataTable(
            ['From Company', 'From Account', 'To Company', 'To Account', 'Balance'],
            report.lines.map(line => [
              line.fromCompanyName,
              <span key={`${line.fromCompanyId}-${line.fromAccountNumber}`} className="font-mono text-xs">{line.fromAccountNumber}</span>,
              line.toCompanyName,
              <span key={`${line.toCompanyId}-${line.toAccountNumber}`} className="font-mono text-xs">{line.toAccountNumber}</span>,
              money(line.balance),
            ]),
            ['left', 'left', 'left', 'left', 'right']
          )
        )}
      </>
    ),
  }
}

function renderMultiCurrencyRevaluation(report: MultiCurrencyRevaluationReport): { title: string; body: React.ReactNode } {
  return {
    title: 'Multi-Currency Revaluation Report',
    body: (
      <>
        {summaryGrid([
          { label: 'Company', value: report.companyName },
          { label: 'Revaluation Date', value: formatDate(report.revaluationDate) },
          { label: 'Total Gain/Loss', value: money(report.totalGainLoss), highlight: true },
        ])}
        {report.lines.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No revaluation entries found.</p>
        ) : (
          dataTable(
            ['Account', 'Description', 'Currency', 'Original Balance', 'Revalued Balance', 'Gain/Loss'],
            report.lines.map(line => [
              <span key={line.accountId} className="font-mono text-xs">{line.accountNumber}</span>,
              line.accountDescription,
              line.currency,
              money(line.originalBalance),
              money(line.revaluedBalance),
              <span className={line.gainLoss >= 0 ? 'text-green-600' : 'text-red-600'}>{money(line.gainLoss)}</span>,
            ]),
            ['left', 'left', 'left', 'right', 'right', 'right']
          )
        )}
      </>
    ),
  }
}
