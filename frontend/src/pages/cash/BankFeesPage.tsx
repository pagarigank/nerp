import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle } from 'lucide-react'
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
import { getBankFees, getBankAccounts, recordBankFee} from '@api/cash'
import { bankFeeStatusMap, bankFeeTypeMap } from './statusMaps'

const feeSchema = z.object({
  bankAccountId: z.string().min(1, 'Select a bank account'),
  feeNumber: z.string().trim().min(1, 'Fee number is required'),
  feeType: z.string().min(1, 'Select a fee type'),
  amount: z.coerce.number().positive('Fee amount must be greater than zero'),
  feeDate: z.string().min(1, 'Fee date is required'),
  description: z.string().optional(),
  expenseGlAccountId: z.string().min(1, 'Select an expense GL account'),
})

type FeeForm = z.infer<typeof feeSchema>

const feeTypeOptions = [
  { value: '0', label: 'Service Charge' },
  { value: '1', label: 'Wire Fee' },
  { value: '2', label: 'ACH Fee' },
  { value: '3', label: 'Overdraft Fee' },
  { value: '4', label: 'NSF Fee' },
  { value: '5', label: 'Card Processing' },
  { value: '6', label: 'Other' },
]

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function BankFeesPage() {
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
  } = useForm<FeeForm>({
    resolver: zodResolver(feeSchema),
    defaultValues: {
      bankAccountId: '',
      feeNumber: '',
      feeType: '0',
      amount: 0,
      feeDate: new Date().toISOString().slice(0, 10),
      description: '',
      expenseGlAccountId: '',
    },
  })

  const { data: fees = [], isLoading } = useQuery({
    queryKey: ['cash', 'bankFees'],
    queryFn: () => getBankFees(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const { data: glAccounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountCode} - ${a.accountName}` })),
    [accounts]
  )

  const glAccountOptions = useMemo(
    () => glAccounts.map(a => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [glAccounts]
  )

  const accountName = useMemo(() => {
    const map = new Map(accounts.map(a => [a.id, a.accountName]))
    return (id: string) => map.get(id) ?? '—'
  }, [accounts])

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'bankFees'] })
  }

  const createMutation = useMutation({
    mutationFn: recordBankFee,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = () => {
    setFormError(null)
    reset({
      bankAccountId: '',
      feeNumber: `FEE-2026-${String(fees.length + 1).padStart(3, '0')}`,
      feeType: '0',
      amount: 0,
      feeDate: new Date().toISOString().slice(0, 10),
      description: '',
      expenseGlAccountId: '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: FeeForm) => {
    setFormError(null)
    createMutation.mutate({
      companyId: currentCompanyId(),
      bankAccountId: data.bankAccountId,
      feeNumber: data.feeNumber,
      feeType: Number(data.feeType),
      amount: data.amount,
      feeDate: new Date(data.feeDate).toISOString(),
      description: data.description || null,
      expenseGlAccountId: data.expenseGlAccountId,
      postedBy: 'admin',
    })
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
          title="Bank Fees"
          description={`${fees.length} fee(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>
              Record Fee
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : fees.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No bank fees recorded. Record service charges, wire fees, and other bank charges.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Fee #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bank Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {fees.map(fee => (
                    <tr key={fee.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                        {fee.feeNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{accountName(fee.bankAccountId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {bankFeeTypeMap[fee.feeType] ?? fee.feeType}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(fee.feeDate)}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(fee.amount)}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={fee.status} mapping={bankFeeStatusMap} />
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
        title="Record Bank Fee"
        description="Record a bank service charge, wire fee, or other charge. Posting to GL happens automatically."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Record Fee
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
              {...register('feeNumber')}
              label="Fee Number"
              {...fieldError(errors.feeNumber?.message)}
              required
            />
            <Select
              {...register('feeType')}
              label="Fee Type"
              options={feeTypeOptions}
              {...fieldError(errors.feeType?.message)}
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
              {...register('feeDate')}
              type="date"
              label="Fee Date"
              {...fieldError(errors.feeDate?.message)}
              required
            />
            <div className="sm:col-span-2">
              <Combobox
                label="Expense GL Account"
                placeholder="Select expense account..."
                options={glAccountOptions}
                value={watch('expenseGlAccountId')}
                onChange={value => setValue('expenseGlAccountId', value, { shouldValidate: true })}
                required
              />
            </div>
            <div className="sm:col-span-2">
              <Input {...register('description')} label="Description" placeholder="Optional fee description" />
            </div>
          </div>
        </form>
      </Modal>
    </div>
  )
}
