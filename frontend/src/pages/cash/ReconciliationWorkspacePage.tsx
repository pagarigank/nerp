import { useMemo, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Wand2, AlertCircle, Lock, ArrowLeft, Link2, CheckCircle2, Unlink, ShieldCheck } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import { getAccounts } from '@api/platform'
import { getPayments } from '@api/ap'
import { getCashReceipts } from '@api/ar'
import {
  getReconciliation,
  getReconciliationLines,
  getBankAccounts,
  getDeposits,
  getBankTransfers,
  runAutoMatch,
  markLineMatched,
  markLineCleared,
  markLineUnmatched,
  lockReconciliation,
} from '@api/cash'
import type { CashBankStatementLine } from '@/types/cash'
import { lineStatusMap, matchSourceMap, matchSourceValue, reconciliationStatusMap } from './statusMaps'

const lockSchema = z.object({
  varianceGlAccountId: z.string().min(1, 'Select a variance GL account'),
  tolerance: z.coerce.number().min(0, 'Tolerance must be zero or greater'),
})

type LockForm = z.infer<typeof lockSchema>

const matchSourceOptions = [
  { value: 'ApPayment', label: 'AP Payment' },
  { value: 'ArCashReceipt', label: 'AR Receipt' },
  { value: 'Deposit', label: 'Deposit' },
  { value: 'BankTransfer', label: 'Bank Transfer' },
]

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function ReconciliationWorkspacePage() {
  const { sessionId } = useParams<{ sessionId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [info, setInfo] = useState<string | null>(null)
  const [matchLine, setMatchLine] = useState<CashBankStatementLine | null>(null)
  const [isLockOpen, setIsLockOpen] = useState(false)
  const [lockError, setLockError] = useState<string | null>(null)
  const [autoMatchSummary, setAutoMatchSummary] = useState<string | null>(null)

  const { data: session, isLoading: sessionLoading } = useQuery({
    queryKey: ['cash', 'reconciliations', sessionId],
    queryFn: () => getReconciliation(sessionId!),
    enabled: !!sessionId,
  })

  const { data: lines = [], isLoading: linesLoading } = useQuery({
    queryKey: ['cash', 'reconciliations', sessionId, 'lines'],
    queryFn: () => getReconciliationLines(sessionId!),
    enabled: !!sessionId,
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const accountName = useMemo(
    () => accounts.find(a => a.id === session?.bankAccountId)?.accountName ?? '—',
    [accounts, session]
  )

  const isLocked = session?.status === 'Locked'

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'reconciliations', sessionId] })
    queryClient.invalidateQueries({ queryKey: ['cash', 'reconciliations', sessionId, 'lines'] })
    queryClient.invalidateQueries({ queryKey: ['cash', 'reconciliations'] })
  }

  const autoMatchMutation = useMutation({
    mutationFn: () => runAutoMatch(sessionId!),
    onSuccess: results => {
      const matched = results.filter(r => r.confidence !== 'None' && r.candidate).length
      const exact = results.filter(r => r.confidence === 'Exact').length
      const probable = results.filter(r => r.confidence === 'Probable').length
      setAutoMatchSummary(
        `${matched} line(s) auto-matched (${exact} exact, ${probable} probable). Review the remaining lines and match or clear them manually.`
      )
      setError(null)
      invalidate()
    },
    onError: err => setError(getErrorMessage(err)),
  })

  const clearMutation = useMutation({
    mutationFn: (statementLineId: string) => markLineCleared(sessionId!, { statementLineId, clearedBy: 'admin' }),
    onSuccess: () => {
      setError(null)
      invalidate()
    },
    onError: err => setError(getErrorMessage(err)),
  })

  const unmatchMutation = useMutation({
    mutationFn: (statementLineId: string) => markLineUnmatched(sessionId!, { statementLineId, clearedBy: 'admin' }),
    onSuccess: () => {
      setError(null)
      invalidate()
    },
    onError: err => setError(getErrorMessage(err)),
  })

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<LockForm>({
    resolver: zodResolver(lockSchema),
    defaultValues: {
      varianceGlAccountId: '',
      tolerance: 10,
    },
  })

  const openLock = () => {
    setLockError(null)
    reset({ varianceGlAccountId: '', tolerance: 10 })
    setIsLockOpen(true)
  }

  const lockMutation = useMutation({
    mutationFn: (data: LockForm) =>
      lockReconciliation(sessionId!, {
        varianceGlAccountId: data.varianceGlAccountId,
        tolerance: data.tolerance,
        lockedBy: 'admin',
      }),
    onSuccess: () => {
      setLockError(null)
      setIsLockOpen(false)
      setInfo('Reconciliation locked. Statement lines are now read-only.')
      invalidate()
    },
    onError: err => setLockError(getErrorMessage(err)),
  })

  const onLockSubmit = (data: LockForm) => {
    setLockError(null)
    lockMutation.mutate(data)
  }

  const unresolvedCount = useMemo(() => lines.filter(l => l.status === 'Unreconciled').length, [lines])

  const clearedAmount = useMemo(
    () => lines.filter(l => l.status !== 'Unreconciled').reduce((sum, l) => sum + Math.abs(l.amount), 0),
    [lines]
  )

  if (sessionLoading) {
    return (
      <div className="space-y-6">
        <Card>
          <CardContent>
            <SkeletonTable columns={4} />
          </CardContent>
        </Card>
      </div>
    )
  }

  if (!session) {
    return (
      <div className="p-8 text-center">
        <h2 className="text-xl font-bold text-gray-900 dark:text-white">Session not found</h2>
        <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">This reconciliation session no longer exists.</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate('/cash/reconciliations')}
          leftIcon={<ArrowLeft className="h-4 w-4" />}
        >
          Back to Reconciliations
        </Button>
      </div>

      {error && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      {info && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-700 dark:bg-emerald-900/20 dark:border-emerald-800 dark:text-emerald-300"
          role="status"
        >
          <CheckCircle2 className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{info}</span>
        </div>
      )}

      {autoMatchSummary && (
        <div
          className="flex items-start gap-2 p-4 rounded-lg bg-blue-50 border border-blue-200 text-blue-700 dark:bg-blue-900/20 dark:border-blue-800 dark:text-blue-300"
          role="status"
        >
          <Wand2 className="h-5 w-5 flex-shrink-0 mt-0.5" aria-hidden="true" />
          <span className="text-sm">{autoMatchSummary}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title={`Session ${session.sessionNumber}`}
          description={`${accountName} · Statement ${formatDate(session.statementDate)}`}
          action={
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                leftIcon={<Wand2 className="h-4 w-4" />}
                onClick={() => autoMatchMutation.mutate()}
                isLoading={autoMatchMutation.isPending}
                disabled={isLocked || unresolvedCount === 0}
              >
                Auto-Match
              </Button>
              <Button
                variant="success"
                size="sm"
                leftIcon={isLocked ? <ShieldCheck className="h-4 w-4" /> : <Lock className="h-4 w-4" />}
                onClick={openLock}
                disabled={isLocked}
              >
                {isLocked ? 'Locked' : 'Lock Session'}
              </Button>
            </div>
          }
        />
        <CardContent>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 p-4">
              <p className="text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">
                Beginning Balance
              </p>
              <p className="mt-1 text-lg font-semibold font-tabular tabular-nums text-gray-900 dark:text-white">
                {formatCurrency(session.beginningBalance)}
              </p>
            </div>
            <div className="rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 p-4">
              <p className="text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">
                Ending Balance
              </p>
              <p className="mt-1 text-lg font-semibold font-tabular tabular-nums text-gray-900 dark:text-white">
                {formatCurrency(session.endingBalance)}
              </p>
            </div>
            <div
              className={`rounded-lg border p-4 ${
                session.variance != null && Math.abs(session.variance) > 0.005
                  ? 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800'
                  : 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-800'
              }`}
            >
              <p className="text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400">Variance</p>
              <p
                className={`mt-1 text-lg font-semibold font-tabular tabular-nums ${
                  session.variance != null && Math.abs(session.variance) > 0.005
                    ? 'text-red-700 dark:text-red-400'
                    : 'text-emerald-700 dark:text-emerald-400'
                }`}
              >
                {session.variance != null ? formatCurrency(session.variance) : '—'}
              </p>
            </div>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-x-6 gap-y-2 text-sm text-gray-600 dark:text-gray-300">
            <span>
              <MapStatusBadge value={session.status} mapping={reconciliationStatusMap} />
            </span>
            <span>
              <span className="text-gray-500 dark:text-gray-400">{unresolvedCount}</span> unresolved line(s)
            </span>
            <span>
              Cleared <span className="font-tabular tabular-nums">{formatCurrency(clearedAmount)}</span>
            </span>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader
          title="Statement Lines"
          description={
            isLocked
              ? 'This reconciliation is locked. Statement lines are read-only.'
              : 'Match lines to system transactions, or clear lines that agree with the bank.'
          }
        />
        <CardContent>
          {linesLoading ? (
            <SkeletonTable columns={6} />
          ) : lines.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              This statement has no lines to reconcile.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Reference</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Match</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {lines.map(line => (
                    <tr key={line.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(line.transactionDate)}</td>
                      <td className="px-3 py-3 text-gray-900 dark:text-white">{line.description}</td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-600 dark:text-gray-400">
                        {line.checkNumber ?? line.referenceNumber ?? '—'}
                      </td>
                      <td
                        className={`px-3 py-3 text-right font-tabular tabular-nums ${
                          line.amount < 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-900 dark:text-white'
                        }`}
                      >
                        {formatCurrency(line.amount)}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={line.status} mapping={lineStatusMap} />
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {line.matchedSource ? (matchSourceMap[line.matchedSource] ?? line.matchedSource) : '—'}
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          {line.status === 'Unreconciled' && (
                            <>
                              <Button
                                variant="outline"
                                size="sm"
                                leftIcon={<Link2 className="h-4 w-4" />}
                                onClick={() => setMatchLine(line)}
                                disabled={isLocked}
                              >
                                Match
                              </Button>
                              <Button
                                variant="outline"
                                size="sm"
                                leftIcon={<CheckCircle2 className="h-4 w-4" />}
                                onClick={() => clearMutation.mutate(line.id)}
                                isLoading={clearMutation.isPending && clearMutation.variables === line.id}
                                disabled={isLocked}
                              >
                                Clear
                              </Button>
                            </>
                          )}
                          {line.status !== 'Unreconciled' && line.status !== 'Locked' && (
                            <Button
                              variant="ghost"
                              size="sm"
                              leftIcon={<Unlink className="h-4 w-4" />}
                              onClick={() => unmatchMutation.mutate(line.id)}
                              isLoading={unmatchMutation.isPending && unmatchMutation.variables === line.id}
                              disabled={isLocked}
                            >
                              Unmatch
                            </Button>
                          )}
                          {line.status === 'Locked' && (
                            <span className="text-xs text-gray-400 dark:text-gray-500 flex items-center gap-1">
                              <Lock className="h-3.5 w-3.5" aria-hidden="true" /> Locked
                            </span>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <MatchModal
        line={matchLine}
        sessionId={session.id}
        bankAccountId={session.bankAccountId}
        onClose={() => setMatchLine(null)}
        onMatched={() => {
          setMatchLine(null)
          invalidate()
        }}
      />

      <Modal
        isOpen={isLockOpen}
        onClose={() => setIsLockOpen(false)}
        title="Lock Reconciliation"
        description="Locking computes the variance to the bank statement. A nonzero variance is posted to the variance GL account within the tolerance."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={() => setIsLockOpen(false)} disabled={lockMutation.isPending}>
              Cancel
            </Button>
            <Button variant="success" onClick={handleSubmit(onLockSubmit)} isLoading={lockMutation.isPending}>
              Lock Session
            </Button>
          </>
        }
      >
        {lockError && (
          <div
            className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm mb-4"
            role="alert"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
            <span>{lockError}</span>
          </div>
        )}
        <form onSubmit={handleSubmit(onLockSubmit)} className="space-y-5" noValidate>
          <LockGlAccountPicker
            value={watch('varianceGlAccountId')}
            onChange={value => setValue('varianceGlAccountId', value, { shouldValidate: true })}
            {...(errors.varianceGlAccountId?.message ? { error: errors.varianceGlAccountId.message } : {})}
          />
          <Input
            {...register('tolerance')}
            type="number"
            step="0.01"
            min="0"
            label="Variance Tolerance"
            hint="Session locks only if |variance| is within tolerance."
            {...fieldError(errors.tolerance?.message)}
            required
          />
        </form>
      </Modal>
    </div>
  )
}

function LockGlAccountPicker({
  value,
  onChange,
  error,
}: {
  value: string
  onChange: (value: string) => void
  error?: string
}) {
  const { data: accounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const options = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [accounts]
  )

  return (
    <Combobox
      label="Variance GL Account"
      placeholder="Select account..."
      options={options}
      value={value}
      onChange={onChange}
      {...(error ? { error } : {})}
      required
    />
  )
}

interface MatchModalProps {
  line: CashBankStatementLine | null
  sessionId: string
  bankAccountId: string
  onClose: () => void
  onMatched: () => void
}

function MatchModal({ line, sessionId, bankAccountId, onClose, onMatched }: MatchModalProps) {
  const queryClient = useQueryClient()
  const [source, setSource] = useState('ApPayment')
  const [candidateId, setCandidateId] = useState('')
  const [error, setError] = useState<string | null>(null)

  const enabled = !!line

  const { data: payments = [] } = useQuery({
    queryKey: ['ap', 'payments'],
    queryFn: () => getPayments(),
    enabled,
  })

  const { data: receipts = [] } = useQuery({
    queryKey: ['ar', 'cashReceipts'],
    queryFn: () => getCashReceipts(),
    enabled,
  })

  const { data: deposits = [] } = useQuery({
    queryKey: ['cash', 'deposits'],
    queryFn: () => getDeposits(),
    enabled,
  })

  const { data: transfers = [] } = useQuery({
    queryKey: ['cash', 'transfers'],
    queryFn: () => getBankTransfers(),
    enabled,
  })

  const candidateOptions = useMemo(() => {
    if (source === 'ApPayment') {
      return payments
        .filter(p => p.bankAccountId === bankAccountId && String(p.status) !== '3' && String(p.status) !== 'Voided')
        .map(p => ({ value: p.id, label: `${p.paymentReference} · ${formatCurrency(p.totalAmount)}` }))
    }
    if (source === 'ArCashReceipt') {
      return receipts
        .filter(r => r.status !== 'Refunded' && r.status !== 'Voided')
        .map(r => ({ value: r.id, label: `${r.receiptReference} · ${formatCurrency(r.totalAmount)}` }))
    }
    if (source === 'Deposit') {
      return deposits
        .filter(d => d.bankAccountId === bankAccountId && d.status !== 'Voided')
        .map(d => ({ value: d.id, label: `${d.depositNumber} · ${formatCurrency(d.totalAmount)}` }))
    }
    return transfers
      .filter(
        t =>
          (t.fromBankAccountId === bankAccountId || t.toBankAccountId === bankAccountId) &&
          t.status !== 'Voided'
      )
      .map(t => ({ value: t.id, label: `${t.transferNumber} · ${formatCurrency(t.amount)}` }))
  }, [source, bankAccountId, payments, receipts, deposits, transfers])

  const matchMutation = useMutation({
    mutationFn: () =>
      markLineMatched(sessionId, {
        statementLineId: line!.id,
        transactionId: candidateId,
        source: matchSourceValue[source] ?? 0,
        clearedBy: 'admin',
      }),
    onSuccess: () => {
      setError(null)
      setCandidateId('')
      onMatched()
      queryClient.invalidateQueries({ queryKey: ['ap', 'payments'] })
      queryClient.invalidateQueries({ queryKey: ['ar', 'cashReceipts'] })
      queryClient.invalidateQueries({ queryKey: ['cash', 'deposits'] })
      queryClient.invalidateQueries({ queryKey: ['cash', 'transfers'] })
    },
    onError: err => setError(getErrorMessage(err)),
  })

  if (!line) return null

  return (
    <Modal
      isOpen={!!line}
      onClose={onClose}
      title="Match Statement Line"
      description="Link this bank line to a system transaction. Amounts should agree with the bank."
      size="md"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={matchMutation.isPending}>
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={() => matchMutation.mutate()}
            isLoading={matchMutation.isPending}
            disabled={!candidateId}
          >
            Match Line
          </Button>
        </>
      }
    >
      <div className="space-y-5">
        <div className="rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 p-4 space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-500 dark:text-gray-400">Date</span>
            <span className="font-medium text-gray-900 dark:text-white">{formatDate(line.transactionDate)}</span>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-500 dark:text-gray-400">Description</span>
            <span className="font-medium text-gray-900 dark:text-white">{line.description}</span>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-500 dark:text-gray-400">Amount</span>
            <span
              className={`font-semibold font-tabular tabular-nums ${
                line.amount < 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-900 dark:text-white'
              }`}
            >
              {formatCurrency(line.amount)}
            </span>
          </div>
          {(line.checkNumber ?? line.referenceNumber) && (
            <div className="flex items-center justify-between text-sm">
              <span className="text-gray-500 dark:text-gray-400">Reference</span>
              <span className="font-mono text-xs text-gray-900 dark:text-white">
                {line.checkNumber ?? line.referenceNumber}
              </span>
            </div>
          )}
        </div>

        {error && (
          <div
            className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm"
            role="alert"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
            <span>{error}</span>
          </div>
        )}

        <Select
          label="Source"
          options={matchSourceOptions}
          value={source}
          onChange={e => {
            setSource(e.target.value)
            setCandidateId('')
          }}
          required
        />

        <Combobox
          label="Transaction"
          placeholder="Select transaction..."
          options={candidateOptions}
          value={candidateId}
          onChange={setCandidateId}
          required
        />
        {candidateOptions.length === 0 && (
          <p className="text-xs text-gray-500 dark:text-gray-400">
            No candidate transactions found for this source. Try a different source type.
          </p>
        )}
      </div>
    </Modal>
  )
}
