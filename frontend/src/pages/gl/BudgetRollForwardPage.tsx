import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { getAccounts, getFiscalYears } from '@api/platform'
import { getBudgets, rollForwardBudget, transferBudget } from '@api/gl'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { AlertCircle } from 'lucide-react'
import { getErrorMessage } from '@api/client'

export function BudgetRollForwardPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [budgetId, setBudgetId] = useState('')
  const [targetFiscalYearId, setTargetFiscalYearId] = useState('')
  const [accountId, setAccountId] = useState('')
  const [fromPeriod, setFromPeriod] = useState('')
  const [toPeriod, setToPeriod] = useState('')
  const [amount, setAmount] = useState('')
  const [reason, setReason] = useState('')
  const [msg, setMsg] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: budgets = [] } = useQuery({ queryKey: ['budgets', companyId], queryFn: () => getBudgets(), enabled: !!companyId })
  const { data: fiscalYears = [] } = useQuery({ queryKey: ['fiscalYears', companyId], queryFn: () => getFiscalYears(companyId), enabled: !!companyId })
  const { data: accounts = [] } = useQuery({ queryKey: ['accounts', companyId], queryFn: () => getAccounts(companyId), enabled: !!companyId })

  const rollFwd = useMutation({
    mutationFn: () => rollForwardBudget(budgetId, { targetFiscalYearId }),
    onSuccess: () => { setMsg('Budget rolled forward.'); queryClient.invalidateQueries({ queryKey: ['budgets'] }) },
    onError: (e) => setError(getErrorMessage(e)),
  })
  const transfer = useMutation({
    mutationFn: () => transferBudget(budgetId, {
      accountId, fromPeriodNumber: Number(fromPeriod), toPeriodNumber: Number(toPeriod),
      amount: Number(amount), reason,
    }),
    onSuccess: () => { setMsg('Budget transferred between periods.'); queryClient.invalidateQueries({ queryKey: ['budgets'] }) },
    onError: (e) => setError(getErrorMessage(e)),
  })

  const budgetOptions = useMemo(
    () => budgets.map(b => ({ value: b.id, label: `${b.name} (${b.isActive ? 'Active' : 'Inactive'})` })),
    [budgets]
  )

  const fiscalYearOptions = useMemo(
    () => fiscalYears.map(fy => ({ value: fy.id, label: `${fy.year} - ${fy.description}` })),
    [fiscalYears]
  )

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [accounts]
  )

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Budget Roll-Forward &amp; Transfer</h1>

      {msg && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-700 dark:bg-emerald-900/20 dark:border-emerald-800 dark:text-emerald-300">
          <span className="text-sm">{msg}</span>
        </div>
      )}
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      <Card>
        <CardHeader title="Select Budget" />
        <CardContent>
          <Combobox
            label="Budget"
            placeholder="Select budget..."
            options={budgetOptions}
            value={budgetId}
            onChange={setBudgetId}
            required
          />
        </CardContent>
      </Card>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader title="Roll Forward to Fiscal Year" description="Copy budget amounts to a new fiscal year" />
          <CardContent className="space-y-4">
            <Combobox
              label="Target Fiscal Year"
              placeholder="Select target fiscal year..."
              options={fiscalYearOptions}
              value={targetFiscalYearId}
              onChange={setTargetFiscalYearId}
              required
            />
            <Button
              variant="primary"
              disabled={!budgetId || !targetFiscalYearId || rollFwd.isPending}
              onClick={() => rollFwd.mutate()}
              isLoading={rollFwd.isPending}
            >
              Roll Forward
            </Button>
          </CardContent>
        </Card>
        <Card>
          <CardHeader title="Transfer Between Periods" description="Move budget from one period to another within the same budget" />
          <CardContent className="space-y-4">
            <Combobox
              label="Account"
              placeholder="Select account..."
              options={accountOptions}
              value={accountId}
              onChange={setAccountId}
              required
            />
            <div className="grid grid-cols-2 gap-4">
              <Input label="From Period" type="number" min="1" max="13" value={fromPeriod} onChange={(e) => setFromPeriod(e.target.value)} placeholder="1-13" />
              <Input label="To Period" type="number" min="1" max="13" value={toPeriod} onChange={(e) => setToPeriod(e.target.value)} placeholder="1-13" />
            </div>
            <Input label="Amount" type="number" step="0.01" min="0" value={amount} onChange={(e) => setAmount(e.target.value)} placeholder="0.00" />
            <Input label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Reason for transfer" />
            <Button
              variant="primary"
              disabled={!budgetId || !accountId || !fromPeriod || !toPeriod || !amount || transfer.isPending}
              onClick={() => transfer.mutate()}
              isLoading={transfer.isPending}
            >
              Transfer
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
