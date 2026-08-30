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
import { Input } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import { getCashReceipts } from '@api/ar'
import { getNsfRecords, getBankAccounts, processNsf} from '@api/cash'
import type { ArCashReceipt } from '@/types/ar'
import { nsfStatusMap } from './statusMaps'

const nsfSchema = z.object({
  bankAccountId: z.string().min(1, 'Select a bank account'),
  cashReceiptId: z.string().min(1, 'Select a cash receipt'),
  nsfNumber: z.string().trim().min(1, 'NSF number is required'),
  amount: z.coerce.number().positive('Amount must be greater than zero'),
  returnedDate: z.string().min(1, 'Returned date is required'),
  bankReference: z.string().optional(),
  reason: z.string().optional(),
  nsfFeeAmount: z.coerce.number().min(0).optional(),
})

type NsfForm = z.infer<typeof nsfSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function NsfPage() {
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
  } = useForm<NsfForm>({
    resolver: zodResolver(nsfSchema),
    defaultValues: {
      bankAccountId: '',
      cashReceiptId: '',
      nsfNumber: '',
      amount: 0,
      returnedDate: new Date().toISOString().slice(0, 10),
      bankReference: '',
      reason: '',
      nsfFeeAmount: 0,
    },
  })

  const { data: records = [], isLoading } = useQuery({
    queryKey: ['cash', 'nsf'],
    queryFn: () => getNsfRecords(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const { data: receipts = [] } = useQuery({
    queryKey: ['ar', 'cashReceipts'],
    queryFn: () => getCashReceipts(),
  })

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountCode} - ${a.accountName}` })),
    [accounts]
  )

  const receiptOptions = useMemo(
    () =>
      receipts
        .filter(r => r.status !== 'Refunded' && r.status !== 'Voided')
        .map(r => ({ value: r.id, label: `${r.receiptReference} · ${formatCurrency(r.totalAmount)}` })),
    [receipts]
  )

  const selectedReceipt = useMemo<ArCashReceipt | undefined>(
    () => receipts.find(r => r.id === watch('cashReceiptId')),
    [receipts, watch]
  )

  const accountName = useMemo(() => {
    const map = new Map(accounts.map(a => [a.id, a.accountName]))
    return (id: string) => map.get(id) ?? '—'
  }, [accounts])

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'nsf'] })
    queryClient.invalidateQueries({ queryKey: ['ar', 'cashReceipts'] })
  }

  const createMutation = useMutation({
    mutationFn: processNsf,
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
      cashReceiptId: '',
      nsfNumber: `NSF-2026-${String(records.length + 1).padStart(3, '0')}`,
      amount: 0,
      returnedDate: new Date().toISOString().slice(0, 10),
      bankReference: '',
      reason: '',
      nsfFeeAmount: 0,
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: NsfForm) => {
    setFormError(null)
    createMutation.mutate({
      companyId: currentCompanyId(),
      bankAccountId: data.bankAccountId,
      cashReceiptId: data.cashReceiptId,
      nsfNumber: data.nsfNumber,
      amount: data.amount,
      returnedDate: new Date(data.returnedDate).toISOString(),
      bankReference: data.bankReference || null,
      reason: data.reason || null,
      nsfFeeAmount: data.nsfFeeAmount && data.nsfFeeAmount > 0 ? data.nsfFeeAmount : null,
      processedBy: 'admin',
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
          title="NSF Handling"
          description={`${records.length} NSF record(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>
              Process NSF
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : records.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No NSF records. When a deposit bounces, process an NSF to reverse the receipt and reopen the invoice.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">NSF #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bank Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Receipt</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Returned</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {records.map(record => (
                    <tr key={record.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                        {record.nsfNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{accountName(record.bankAccountId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {receipts.find(r => r.id === record.cashReceiptId)?.receiptReference ?? '—'}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(record.returnedDate)}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-red-600 dark:text-red-400">
                        {formatCurrency(record.amount)}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={record.status} mapping={nsfStatusMap} />
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
        title="Process NSF"
        description="Reverse a bounced cash receipt. The AR invoice is reopened and the customer balance restored."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Process NSF
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Combobox
              label="Bank Account"
              placeholder="Select bank account..."
              options={accountOptions}
              value={watch('bankAccountId')}
              onChange={value => setValue('bankAccountId', value, { shouldValidate: true })}
              required
            />
            <Combobox
              label="Cash Receipt"
              placeholder="Select receipt..."
              options={receiptOptions}
              value={watch('cashReceiptId')}
              onChange={value => {
                setValue('cashReceiptId', value, { shouldValidate: true })
                const receipt = receipts.find(r => r.id === value)
                if (receipt) setValue('amount', receipt.totalAmount)
              }}
              required
            />
            <Input
              {...register('nsfNumber')}
              label="NSF Number"
              {...fieldError(errors.nsfNumber?.message)}
              required
            />
            <Input
              {...register('amount')}
              type="number"
              step="0.01"
              min="0.01"
              label="Amount"
              {...(selectedReceipt ? { hint: `Receipt total is ${formatCurrency(selectedReceipt.totalAmount)}` } : {})}
              {...fieldError(errors.amount?.message)}
              required
            />
            <Input
              {...register('returnedDate')}
              type="date"
              label="Returned Date"
              {...fieldError(errors.returnedDate?.message)}
              required
            />
            <Input {...register('bankReference')} label="Bank Reference" placeholder="Optional bank reference #" />
            <Input
              {...register('nsfFeeAmount')}
              type="number"
              step="0.01"
              min="0"
              label="NSF Fee"
              hint="Optional fee charged by the bank"
            />
            <Input {...register('reason')} label="Reason" placeholder="Optional reason code" />
          </div>
        </form>
      </Modal>
    </div>
  )
}
