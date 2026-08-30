import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AlertCircle, Download, PiggyBank, TrendingDown, TrendingUp, Scale } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Combobox } from '@components/ui/Combobox'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import {
  getCashPosition,
  getCashForecast,
  getOutstandingChecks,
  getReconciliationSummary,
  getPositivePay,
  getBankAccounts,
  } from '@api/cash'
import { reconciliationStatusMap } from './statusMaps'

export function CashReportsPage() {
  const [error, setError] = useState<string | null>(null)
  const [agingAccountId, setAgingAccountId] = useState('')
  const [positivePayAccountId, setPositivePayAccountId] = useState('')
  const [isDownloading, setIsDownloading] = useState(false)

  const { data: positions = [], isLoading: positionLoading } = useQuery({
    queryKey: ['cash', 'reports', 'cashPosition'],
    queryFn: () => getCashPosition(),
  })

  const { data: forecast } = useQuery({
    queryKey: ['cash', 'reports', 'cashForecast'],
    queryFn: () => getCashForecast(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountCode} - ${a.accountName}` })),
    [accounts]
  )

  const { data: aging, isLoading: agingLoading } = useQuery({
    queryKey: ['cash', 'reports', 'outstandingChecks', agingAccountId],
    queryFn: () => getOutstandingChecks(currentCompanyId(), agingAccountId),
    enabled: !!agingAccountId,
  })

  const { data: summaries = [], isLoading: summaryLoading } = useQuery({
    queryKey: ['cash', 'reports', 'reconciliationSummary'],
    queryFn: () => getReconciliationSummary(),
  })

  const totalCash = useMemo(
    () => positions.reduce((sum, p) => sum + p.currentBalance, 0),
    [positions]
  )

  const downloadPositivePay = async () => {
    if (!positivePayAccountId) return
    setError(null)
    setIsDownloading(true)
    try {
      const csv = await getPositivePay(currentCompanyId(), positivePayAccountId)
      const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' })
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `positive-pay-${positivePayAccountId}.csv`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(url)
    } catch (err) {
      setError(getErrorMessage(err))
    } finally {
      setIsDownloading(false)
    }
  }

  return (
    <div className="space-y-6">
      {error && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-5">
          <div className="flex items-center gap-2 text-gray-500 dark:text-gray-400">
            <PiggyBank className="h-4 w-4" aria-hidden="true" />
            <span className="text-xs font-medium uppercase tracking-wide">Current Cash</span>
          </div>
          <p className="mt-2 text-2xl font-bold font-tabular tabular-nums text-gray-900 dark:text-white">
            {forecast ? formatCurrency(forecast.currentCash) : '—'}
          </p>
        </div>
        <div className="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-5">
          <div className="flex items-center gap-2 text-gray-500 dark:text-gray-400">
            <TrendingDown className="h-4 w-4" aria-hidden="true" />
            <span className="text-xs font-medium uppercase tracking-wide">Open Payables</span>
          </div>
          <p className="mt-2 text-2xl font-bold font-tabular tabular-nums text-red-600 dark:text-red-400">
            {forecast ? formatCurrency(forecast.openPayables) : '—'}
          </p>
        </div>
        <div className="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-5">
          <div className="flex items-center gap-2 text-gray-500 dark:text-gray-400">
            <TrendingUp className="h-4 w-4" aria-hidden="true" />
            <span className="text-xs font-medium uppercase tracking-wide">Open Receivables</span>
          </div>
          <p className="mt-2 text-2xl font-bold font-tabular tabular-nums text-emerald-600 dark:text-emerald-400">
            {forecast ? formatCurrency(forecast.openReceivables) : '—'}
          </p>
        </div>
        <div className="rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-5">
          <div className="flex items-center gap-2 text-gray-500 dark:text-gray-400">
            <Scale className="h-4 w-4" aria-hidden="true" />
            <span className="text-xs font-medium uppercase tracking-wide">After Payables + AR</span>
          </div>
          <p className="mt-2 text-2xl font-bold font-tabular tabular-nums text-gray-900 dark:text-white">
            {forecast ? formatCurrency(forecast.projectedCashAfterCollections) : '—'}
          </p>
        </div>
      </div>

      <Card>
        <CardHeader
          title="Cash Position"
          description={`Total cash on hand: ${formatCurrency(totalCash)}`}
        />
        <CardContent>
          {positionLoading ? (
            <SkeletonTable columns={5} />
          ) : positions.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No bank accounts configured. Create a bank account to see cash position.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Number</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Balance</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Out. Checks</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Out. Deposits</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {positions.map(position => (
                    <tr key={position.bankAccountId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 text-gray-900 dark:text-white">
                        {position.accountCode} - {position.accountName}
                      </td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-600 dark:text-gray-400">
                        {position.accountNumber}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(position.currentBalance)}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-red-600 dark:text-red-400">
                        {formatCurrency(position.outstandingChecks)}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-emerald-600 dark:text-emerald-400">
                        {formatCurrency(position.outstandingDeposits)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader
            title="Outstanding Checks"
            description="Aging of issued checks not yet cleared by the bank"
          />
          <CardContent>
            <div className="mb-4">
              <Combobox
                label="Bank Account"
                placeholder="Select bank account..."
                options={accountOptions}
                value={agingAccountId}
                onChange={setAgingAccountId}
              />
            </div>
            {agingLoading ? (
              <SkeletonTable columns={3} />
            ) : !aging ? (
              <p className="text-sm text-gray-500 dark:text-gray-400 py-6 text-center">
                Select a bank account to view outstanding check aging.
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                      <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bucket</th>
                      <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Checks</th>
                      <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {aging.buckets.map(bucket => (
                      <tr key={bucket.bucket} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-2.5 text-gray-700 dark:text-gray-300">{bucket.bucket}</td>
                        <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                          {bucket.checkCount}
                        </td>
                        <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-red-600 dark:text-red-400">
                          {formatCurrency(bucket.amount)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader
            title="Positive Pay Export"
            description="Download the issued-check file for fraud protection"
          />
          <CardContent>
            <div className="mb-4">
              <Combobox
                label="Bank Account"
                placeholder="Select bank account..."
                options={accountOptions}
                value={positivePayAccountId}
                onChange={setPositivePayAccountId}
              />
            </div>
            <Button
              variant="outline"
              leftIcon={<Download className="h-4 w-4" />}
              onClick={downloadPositivePay}
              isLoading={isDownloading}
              disabled={!positivePayAccountId}
            >
              Download CSV
            </Button>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader
          title="Reconciliation Summary"
          description="Most recent reconciliation per bank account"
        />
        <CardContent>
          {summaryLoading ? (
            <SkeletonTable columns={8} />
          ) : summaries.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No reconciliations recorded yet.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bank Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Statement</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Beginning</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Cleared Dep.</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Cleared With.</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Out. Checks</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Variance</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {summaries.map(summary => (
                    <tr key={summary.bankAccountId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 text-gray-900 dark:text-white">{summary.accountName}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(summary.statementDate)}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(summary.beginningBalance)}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-emerald-600 dark:text-emerald-400">
                        {formatCurrency(summary.clearedDeposits)}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-red-600 dark:text-red-400">
                        {formatCurrency(summary.clearedWithdrawals)}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(summary.outstandingChecks)}
                      </td>
                      <td
                        className={`px-3 py-3 text-right font-tabular tabular-nums ${
                          Math.abs(summary.variance) > 0.005 ? 'text-red-600 dark:text-red-400' : 'text-gray-900 dark:text-white'
                        }`}
                      >
                        {formatCurrency(summary.variance)}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={summary.status} mapping={reconciliationStatusMap} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
