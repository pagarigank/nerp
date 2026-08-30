import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, Scale } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getBankAccounts,
  getPositivePayExceptions,
  createPositivePayException,
  decidePositivePay,
  } from '@api/cash'
import type { PositivePayException } from '@/types/cash'

const exceptionSchema = z.object({
  bankAccountId: z.string().min(1, 'Select a bank account'),
  checkNumber: z.string().min(1, 'Check number is required'),
  amount: z.coerce.number().positive('Amount must be greater than zero'),
  issueDate: z.string().min(1, 'Issue date is required'),
})

type ExceptionForm = z.infer<typeof exceptionSchema>

const decideSchema = z.object({
  decision: z.string().min(1, 'Select a decision'),
  decisionReason: z.string().optional(),
})

type DecideForm = z.infer<typeof decideSchema>

const decisionOptions = [
  { value: 'Pay', label: 'Pay' },
  { value: 'NoPay', label: 'No Pay' },
]

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function PositivePayPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [decideTarget, setDecideTarget] = useState<PositivePayException | null>(null)
  const [decideError, setDecideError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<ExceptionForm>({
    resolver: zodResolver(exceptionSchema),
    defaultValues: {
      bankAccountId: '',
      checkNumber: '',
      amount: 0,
      issueDate: new Date().toISOString().slice(0, 10),
    },
  })

  const {
    register: registerDecide,
    handleSubmit: handleSubmitDecide,
    reset: resetDecide,
    formState: { errors: decideErrors },
  } = useForm<DecideForm>({
    resolver: zodResolver(decideSchema),
    defaultValues: {
      decision: 'Pay',
      decisionReason: '',
    },
  })

  const { data: exceptions = [], isLoading } = useQuery({
    queryKey: ['cash', 'positive-pay-exceptions'],
    queryFn: () => getPositivePayExceptions(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountCode} - ${a.accountName}` })),
    [accounts]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'positive-pay-exceptions'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: ExceptionForm) =>
      createPositivePayException({
        companyId: currentCompanyId(),
        bankAccountId: data.bankAccountId,
        checkNumber: data.checkNumber,
        amount: data.amount,
        issueDate: new Date(data.issueDate).toISOString(),
        decisionReason: 'Bank returned unmatched item',
      }),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const decideMutation = useMutation({
    mutationFn: (data: DecideForm) =>
      decidePositivePay(decideTarget!.id, {
        decision: data.decision,
        decisionReason: data.decisionReason || '',
      }),
    onSuccess: () => {
      invalidate()
      setDecideTarget(null)
    },
    onError: err => setDecideError(getErrorMessage(err)),
  })

  const openForm = () => {
    setFormError(null)
    reset({
      bankAccountId: '',
      checkNumber: '',
      amount: 0,
      issueDate: new Date().toISOString().slice(0, 10),
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: ExceptionForm) => {
    setFormError(null)
    createMutation.mutate(data)
  }

  const onDecideSubmit = (data: DecideForm) => {
    setDecideError(null)
    decideMutation.mutate(data)
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
          title="Positive Pay Exceptions"
          description={`${exceptions.length} exception(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Scale className="h-4 w-4" />}>
              Log Exception
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : exceptions.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No positive pay exceptions. Exceptions are logged when the bank returns an unmatched check.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Check #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Issued</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Decision</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {exceptions.map((exc: PositivePayException) => (
                    <tr key={exc.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-gray-900 dark:text-white">
                        {exc.checkNumber}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(exc.amount)}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(exc.issueDate)}</td>
                      <td className="px-3 py-3">
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                            exc.decision === 'Pending'
                              ? 'bg-amber-100 text-amber-800 dark:bg-amber-900/20 dark:text-amber-300'
                              : exc.decision === 'Pay'
                                ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/20 dark:text-emerald-300'
                                : 'bg-red-100 text-red-800 dark:bg-red-900/20 dark:text-red-300'
                          }`}
                        >
                          {exc.decision}
                        </span>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end">
                          {exc.decision === 'Pending' && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => {
                                setDecideError(null)
                                resetDecide({ decision: 'Pay', decisionReason: '' })
                                setDecideTarget(exc)
                              }}
                            >
                              Decide
                            </Button>
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

      {/* Create Exception Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title="Log Positive Pay Exception"
        description="Record a check returned by the bank as a positive pay discrepancy."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Log Exception
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="sm:col-span-2">
              <Combobox
                label="Bank Account"
                placeholder="Select bank account..."
                options={accountOptions}
                value={watch('bankAccountId')}
                onChange={value => setValue('bankAccountId', value, { shouldValidate: true })}
                required
              />
            </div>
            <Input
              {...register('checkNumber')}
              label="Check Number"
              placeholder="Check number"
              {...fieldError(errors.checkNumber?.message)}
              required
            />
            <Input
              {...register('amount')}
              type="number"
              step="0.01"
              min="0.01"
              label="Amount"
              {...fieldError(errors.amount?.message)}
              required
            />
            <Input
              {...register('issueDate')}
              type="date"
              label="Issue Date"
              {...fieldError(errors.issueDate?.message)}
              required
            />
          </div>
        </form>
      </Modal>

      {/* Decide Modal */}
      <Modal
        isOpen={!!decideTarget}
        onClose={() => setDecideTarget(null)}
        title="Decide: Pay / No-Pay"
        description={`Review check #${decideTarget?.checkNumber} for ${decideTarget ? formatCurrency(decideTarget.amount) : ''} and decide whether to pay.`}
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={() => setDecideTarget(null)} disabled={decideMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmitDecide(onDecideSubmit)} isLoading={decideMutation.isPending}>
              Submit Decision
            </Button>
          </>
        }
      >
        {decideError && (
          <div
            className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm mb-4"
            role="alert"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" />
            <span>{decideError}</span>
          </div>
        )}
        <form onSubmit={handleSubmitDecide(onDecideSubmit)} className="space-y-4" noValidate>
          <Select
            {...registerDecide('decision')}
            label="Decision"
            options={decisionOptions}
            {...fieldError(decideErrors.decision?.message)}
          />
          <Input
            {...registerDecide('decisionReason')}
            label="Reason"
            placeholder="Optional reason for this decision"
          />
        </form>
      </Modal>
    </div>
  )
}
