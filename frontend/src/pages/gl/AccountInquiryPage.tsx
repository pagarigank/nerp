import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { getAccounts, getFiscalPeriods } from '@api/platform'
import { getAccountInquiry } from '@api/gl'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge } from '@components/ui/Badge'
import { Combobox } from '@components/ui/Combobox'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { formatCurrency, formatDate } from '@utils/helpers'

export function AccountInquiryPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const [accountId, setAccountId] = useState('')
  const [fiscalPeriodId, setFiscalPeriodId] = useState('')

  const { data: accounts = [] } = useQuery({
    queryKey: ['accounts', companyId],
    queryFn: () => getAccounts(companyId),
    enabled: !!companyId,
  })
  const { data: periods = [] } = useQuery({
    queryKey: ['fiscalPeriods', companyId],
    queryFn: () => getFiscalPeriods(companyId),
    enabled: !!companyId,
  })

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [accounts]
  )

  const periodOptions = useMemo(
    () => [{ value: '', label: 'All periods' }, ...periods.map(p => ({ value: p.id, label: `P${p.periodNumber} - ${p.description}` }))],
    [periods]
  )

  const { data, isLoading, error } = useQuery({
    queryKey: ['accountInquiry', accountId, fiscalPeriodId],
    queryFn: () => getAccountInquiry(accountId, { fiscalPeriodId: fiscalPeriodId || undefined }),
    enabled: !!accountId,
  })

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Account Inquiry (Drill-Back)</h1>
      <div className="flex flex-wrap items-end gap-3">
        <div className="w-80">
          <Combobox
            label="Account"
            placeholder="Select account..."
            options={accountOptions}
            value={accountId}
            onChange={setAccountId}
            required
          />
        </div>
        <div className="w-64">
          <Combobox
            label="Fiscal Period (optional)"
            placeholder="All periods"
            options={periodOptions}
            value={fiscalPeriodId}
            onChange={setFiscalPeriodId}
            allowClear
          />
        </div>
      </div>
      {error && <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-300">{getErrorMessage(error)}</div>}
      {isLoading && <Skeleton className="h-24 w-full" />}
      {data && (
        <>
          <Card>
            <CardHeader title={`${data.accountNumber} — ${data.description}`} />
            <CardContent className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
              <div>
                <p className="text-gray-500 dark:text-gray-400">Type</p>
                <p className="font-medium text-gray-900 dark:text-white mt-0.5">{data.accountType}</p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Normal Balance</p>
                <p className="font-medium text-gray-900 dark:text-white mt-0.5">{data.normalBalance}</p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Status</p>
                <p className="mt-0.5">{data.isActive ? <Badge variant="success">Active</Badge> : <Badge variant="neutral">Inactive</Badge>}</p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Net Balance</p>
                <p className="font-semibold font-tabular tabular-nums text-gray-900 dark:text-white mt-0.5">{formatCurrency(data.netBalance)}</p>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader title="Period Balances" />
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Period</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Debit</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Credit</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Net</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {data.periodBalances.map((pb) => (
                      <tr key={pb.fiscalPeriodId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-2.5 text-gray-700 dark:text-gray-300">P{pb.periodNumber} — {pb.periodName}</td>
                        <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(pb.debit)}</td>
                        <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(pb.credit)}</td>
                        <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(pb.net)}</td>
                      </tr>
                    ))}
                    {data.periodBalances.length === 0 && <tr><td colSpan={4} className="px-3 py-6 text-center text-gray-500 dark:text-gray-400">No activity.</td></tr>}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader title="Journal Detail (Source Drill-Back)" />
            <CardContent>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Batch</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Source</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Debit</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Credit</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Reference</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {data.lines.map((l, i) => (
                      <tr key={i} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-2.5 font-mono text-xs text-gray-700 dark:text-gray-300">{l.batchNumber}</td>
                        <td className="px-3 py-2.5 text-gray-700 dark:text-gray-300">{formatDate(l.postingDate)}</td>
                        <td className="px-3 py-2.5"><Badge variant="info" size="sm">{l.sourceDocument}</Badge></td>
                        <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(l.debit)}</td>
                        <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(l.credit)}</td>
                        <td className="px-3 py-2.5 text-gray-700 dark:text-gray-300">{l.reference}</td>
                      </tr>
                    ))}
                    {data.lines.length === 0 && <tr><td colSpan={6} className="px-3 py-6 text-center text-gray-500 dark:text-gray-400">No journal activity.</td></tr>}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}
