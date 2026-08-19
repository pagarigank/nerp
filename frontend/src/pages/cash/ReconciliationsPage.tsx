import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, ArrowUpRight } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import {
  getReconciliations,
  getBankStatements,
  getBankAccounts,
  createReconciliationSession,
} from '@api/cash'
import { reconciliationStatusMap } from './statusMaps'

const sessionSchema = z.object({
  bankStatementId: z.string().min(1, 'Select a bank statement'),
  sessionNumber: z.string().trim().min(1, 'Session number is required'),
})

type SessionForm = z.infer<typeof sessionSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function ReconciliationsPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<SessionForm>({
    resolver: zodResolver(sessionSchema),
    defaultValues: {
      bankStatementId: '',
      sessionNumber: '',
    },
  })

  const { data: sessions = [], isLoading } = useQuery({
    queryKey: ['cash', 'reconciliations'],
    queryFn: () => getReconciliations(),
  })

  const { data: statements = [] } = useQuery({
    queryKey: ['cash', 'bankStatements'],
    queryFn: () => getBankStatements(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const accountName = useMemo(() => {
    const map = new Map(accounts.map(a => [a.id, a.accountName]))
    return (id: string) => map.get(id) ?? '—'
  }, [accounts])

  const statementOptions = useMemo(() => {
    const sessionStatementIds = new Set(sessions.map(s => s.bankStatementId))
    return statements
      .filter(s => s.status !== 'Locked' && !sessionStatementIds.has(s.id))
      .map(s => ({
        value: s.id,
        label: `${s.statementNumber} · ${formatDate(s.statementDate)} · ${accountName(s.bankAccountId)}`,
      }))
  }, [statements, sessions, accountName])

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'reconciliations'] })
    queryClient.invalidateQueries({ queryKey: ['cash', 'bankStatements'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: SessionForm) =>
      createReconciliationSession(data.bankStatementId, {
        sessionNumber: data.sessionNumber,
        createdBy: 'admin',
      }),
    onSuccess: result => {
      invalidate()
      setIsModalOpen(false)
      navigate(`/cash/reconciliations/${result.sessionId}`)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = () => {
    setFormError(null)
    reset({
      bankStatementId: '',
      sessionNumber: `RCON-2026-${String(sessions.length + 1).padStart(3, '0')}`,
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: SessionForm) => {
    setFormError(null)
    createMutation.mutate(data)
  }

  return (
    <div className="space-y-6">
      {formError && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Reconciliations"
          description={`${sessions.length} session(s) on file`}
          action={
            <Button
              variant="primary"
              size="sm"
              onClick={openForm}
              leftIcon={<Plus className="h-4 w-4" />}
            >
              New Session
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : sessions.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No reconciliation sessions yet. Validate a bank statement, then create a session to begin.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Session #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bank Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Statement Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Ending Balance</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Variance</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {sessions.map(session => (
                    <tr key={session.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                        {session.sessionNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{accountName(session.bankAccountId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(session.statementDate)}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(session.endingBalance)}
                      </td>
                      <td
                        className={`px-3 py-3 text-right font-tabular tabular-nums ${
                          session.variance != null && Math.abs(session.variance) > 0.005
                            ? 'text-red-600 dark:text-red-400'
                            : 'text-gray-900 dark:text-white'
                        }`}
                      >
                        {session.variance != null ? formatCurrency(session.variance) : '—'}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={session.status} mapping={reconciliationStatusMap} />
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end">
                          <Button
                            variant="outline"
                            size="sm"
                            leftIcon={<ArrowUpRight className="h-4 w-4" />}
                            onClick={() => navigate(`/cash/reconciliations/${session.id}`)}
                          >
                            Open
                          </Button>
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

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title="New Reconciliation Session"
        description="Create a session from a validated bank statement. Match statement lines to system transactions."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Create Session
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="sm:col-span-2">
              <Combobox
                label="Bank Statement"
                placeholder="Select statement..."
                options={statementOptions}
                value={watch('bankStatementId')}
                onChange={value => setValue('bankStatementId', value, { shouldValidate: true })}
                required
              />
              {statementOptions.length === 0 && (
                <p className="mt-1.5 text-xs text-gray-500 dark:text-gray-400">
                  No statements available. Import and validate a bank statement first.
                </p>
              )}
            </div>
            <Input
              {...register('sessionNumber')}
              label="Session Number"
              {...fieldError(errors.sessionNumber?.message)}
              required
            />
          </div>
        </form>
      </Modal>
    </div>
  )
}
