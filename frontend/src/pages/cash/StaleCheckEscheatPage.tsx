import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, ShieldAlert } from 'lucide-react'
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
  getBankAccounts,
  getStaleCheckEscheatments,
  createStaleCheckEscheatment,
  escheatStaleCheck,
  reissueStaleCheck,
  } from '@api/cash'
import type { StaleCheckEscheatment } from '@/types/cash'

const staleSchema = z.object({
  bankAccountId: z.string().min(1, 'Select a bank account'),
  checkNumber: z.string().min(1, 'Check number is required'),
  amount: z.coerce.number().positive('Amount must be greater than zero'),
  payee: z.string().min(1, 'Payee is required'),
  state: z.string().min(1, 'State is required'),
  issueDate: z.string().min(1, 'Issue date is required'),
})

type StaleForm = z.infer<typeof staleSchema>

const staleStatusMap: Record<string, { variant: string; label: string }> = {
  Identified: { variant: 'warning', label: 'Identified' },
  Escheated: { variant: 'info', label: 'Escheated' },
  Reissued: { variant: 'success', label: 'Reissued' },
}

const stateOptions = [
  'AL','AK','AZ','AR','CA','CO','CT','DE','FL','GA','HI','ID','IL','IN','IA',
  'KS','KY','LA','ME','MD','MA','MI','MN','MS','MO','MT','NE','NV','NH','NJ',
  'NM','NY','NC','ND','OH','OK','OR','PA','RI','SC','SD','TN','TX','UT','VT',
  'VA','WA','WV','WI','WY',
].map(s => ({ value: s, label: s }))

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function StaleCheckEscheatPage() {
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
  } = useForm<StaleForm>({
    resolver: zodResolver(staleSchema),
    defaultValues: {
      bankAccountId: '',
      checkNumber: '',
      amount: 0,
      payee: '',
      state: '',
      issueDate: new Date().toISOString().slice(0, 10),
    },
  })

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['cash', 'stale-check-escheatments'],
    queryFn: () => getStaleCheckEscheatments(),
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
    queryClient.invalidateQueries({ queryKey: ['cash', 'stale-check-escheatments'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: StaleForm) =>
      createStaleCheckEscheatment({
        companyId: currentCompanyId(),
        bankAccountId: data.bankAccountId,
        checkNumber: data.checkNumber,
        amount: data.amount,
        issueDate: new Date(data.issueDate).toISOString(),
        payee: data.payee,
        state: data.state,
      }),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const escheatMutation = useMutation({
    mutationFn: escheatStaleCheck,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const reissueMutation = useMutation({
    mutationFn: reissueStaleCheck,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = () => {
    setFormError(null)
    reset({
      bankAccountId: '',
      checkNumber: '',
      amount: 0,
      payee: '',
      state: '',
      issueDate: new Date().toISOString().slice(0, 10),
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: StaleForm) => {
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
          title="Stale-Dated Checks & Escheatment"
          description={`${rows.length} stale check(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<ShieldAlert className="h-4 w-4" />}>
              Identify Stale Check
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : rows.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No stale checks identified. Identify stale-dated checks for escheatment or reissuance.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Check #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Payee</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">State</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Issued</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: StaleCheckEscheatment) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-gray-900 dark:text-white">
                        {r.checkNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.payee}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.state}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(r.amount)}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(r.issueDate)}</td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={r.status} mapping={staleStatusMap} />
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          {r.status === 'Identified' && (
                            <>
                              <Button
                                variant="outline"
                                size="sm"
                                disabled={escheatMutation.isPending}
                                onClick={() => escheatMutation.mutate(r.id)}
                                isLoading={escheatMutation.isPending && escheatMutation.variables === r.id}
                              >
                                Escheat
                              </Button>
                              <Button
                                variant="outline"
                                size="sm"
                                disabled={reissueMutation.isPending}
                                onClick={() => reissueMutation.mutate(r.id)}
                                isLoading={reissueMutation.isPending && reissueMutation.variables === r.id}
                              >
                                Reissue
                              </Button>
                            </>
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

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title="Identify Stale-Dated Check"
        description="Flag a check that has not been cashed within the state's dormancy period."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Identify Check
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
              {...register('payee')}
              label="Payee"
              placeholder="Payee name"
              {...fieldError(errors.payee?.message)}
              required
            />
            <Combobox
              label="State"
              placeholder="Select state..."
              options={stateOptions}
              value={watch('state')}
              onChange={value => setValue('state', value, { shouldValidate: true })}
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
    </div>
  )
}
