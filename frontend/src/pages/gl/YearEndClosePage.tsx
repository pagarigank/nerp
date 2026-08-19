import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { getAccounts, getFiscalYears } from '@api/platform'
import { closeYearEnd } from '@api/gl'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { AlertCircle } from 'lucide-react'
import { getErrorMessage } from '@api/client'
import { formatCurrency } from '@utils/helpers'
import type { YearEndCloseRunDto } from '@/types/gl'

export function YearEndClosePage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [fiscalYearId, setFiscalYearId] = useState('')
  const [retainedEarningsAccountId, setRetainedEarningsAccountId] = useState('')
  const [closedBy, setClosedBy] = useState(useAuthStore((s) => s.user?.fullName) ?? 'system')
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<YearEndCloseRunDto | null>(null)

  const { data: fiscalYears = [] } = useQuery({
    queryKey: ['fiscalYears', companyId],
    queryFn: () => getFiscalYears(companyId),
    enabled: !!companyId,
  })
  const { data: accounts = [] } = useQuery({
    queryKey: ['accounts', companyId],
    queryFn: () => getAccounts(companyId),
    enabled: !!companyId,
  })
  const equityAccounts = accounts.filter((a) => a.accountType === 2)

  const mutation = useMutation({
    mutationFn: () => closeYearEnd({ companyId, fiscalYearId, retainedEarningsAccountId, closedBy }),
    onSuccess: (run) => {
      setResult(run)
      queryClient.invalidateQueries({ queryKey: ['journalBatches'] })
    },
    onError: (e) => setError(getErrorMessage(e)),
  })

  const fiscalYearOptions = useMemo(
    () => fiscalYears.map(fy => ({ value: fy.id, label: `${fy.year} - ${fy.description}` })),
    [fiscalYears]
  )

  const equityAccountOptions = useMemo(
    () => equityAccounts.map(a => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [equityAccounts]
  )

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Year-End Close</h1>
      <p className="text-sm text-gray-500 dark:text-gray-400">
        Closes the income statement (revenue & expense accounts) to retained earnings and locks the fiscal year.
      </p>
      <Card>
        <CardHeader title="Run Year-End Close" description="This action creates a closing journal batch and locks the fiscal year." />
        <CardContent className="space-y-4">
          {error && (
            <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
              <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
              <span className="text-sm">{error}</span>
            </div>
          )}
          <Combobox
            label="Fiscal Year"
            placeholder="Select fiscal year..."
            options={fiscalYearOptions}
            value={fiscalYearId}
            onChange={setFiscalYearId}
            required
          />
          <Combobox
            label="Retained Earnings Account"
            placeholder="Select equity account..."
            options={equityAccountOptions}
            value={retainedEarningsAccountId}
            onChange={setRetainedEarningsAccountId}
            required
          />
          <Input label="Closed By" value={closedBy} onChange={(e) => setClosedBy(e.target.value)} />
          <Button
            variant="primary"
            disabled={!fiscalYearId || !retainedEarningsAccountId || mutation.isPending}
            onClick={() => mutation.mutate()}
            isLoading={mutation.isPending}
          >
            Close Year
          </Button>
          {result && (
            <div className="rounded-lg bg-emerald-50 border border-emerald-200 p-4 dark:bg-emerald-900/20 dark:border-emerald-800">
              <p className="text-sm font-medium text-emerald-800 dark:text-emerald-300">Year-end closed successfully.</p>
              <p className="mt-1 text-sm text-emerald-700 dark:text-emerald-400">
                Total revenue {formatCurrency(result.totalRevenue)} · Total expense {formatCurrency(result.totalExpense)} ·
                Retained earnings {formatCurrency(result.retainedEarningsAmount)}.
              </p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
