import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, Send, CheckCircle2, Ban } from 'lucide-react'
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
  getBankTransfers,
  getBankAccounts,
  createBankTransfer,
  confirmBankTransfer,
  completeBankTransfer,
  voidBankTransfer,
  DEMO_COMPANY_ID,
} from '@api/cash'
import type { CashBankTransfer } from '@/types/cash'
import { transferStatusMap } from './statusMaps'

const transferSchema = z.object({
  fromBankAccountId: z.string().min(1, 'Select a source account'),
  toBankAccountId: z.string().min(1, 'Select a destination account'),
  transferNumber: z.string().trim().min(1, 'Transfer number is required'),
  amount: z.coerce.number().positive('Amount must be greater than zero'),
  transferDate: z.string().min(1, 'Transfer date is required'),
  reference: z.string().optional(),
})

type TransferForm = z.infer<typeof transferSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function BankTransfersPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [voidTarget, setVoidTarget] = useState<CashBankTransfer | null>(null)
  const [voidReason, setVoidReason] = useState('')

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<TransferForm>({
    resolver: zodResolver(transferSchema),
    defaultValues: {
      fromBankAccountId: '',
      toBankAccountId: '',
      transferNumber: '',
      amount: 0,
      transferDate: new Date().toISOString().slice(0, 10),
      reference: '',
    },
  })

  const { data: transfers = [], isLoading } = useQuery({
    queryKey: ['cash', 'transfers'],
    queryFn: () => getBankTransfers(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const activeAccountOptions = useMemo(
    () =>
      accounts
        .filter(a => a.status !== 'Closed')
        .map(a => ({ value: a.id, label: `${a.accountCode} - ${a.accountName}` })),
    [accounts]
  )

  const accountName = useMemo(() => {
    const map = new Map(accounts.map(a => [a.id, a.accountName]))
    return (id: string) => map.get(id) ?? '—'
  }, [accounts])

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'transfers'] })
    queryClient.invalidateQueries({ queryKey: ['cash', 'bankAccounts'] })
  }

  const createMutation = useMutation({
    mutationFn: createBankTransfer,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const confirmMutation = useMutation({
    mutationFn: confirmBankTransfer,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const completeMutation = useMutation({
    mutationFn: completeBankTransfer,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const voidMutation = useMutation({
    mutationFn: () => voidBankTransfer(voidTarget!.id, voidReason || undefined),
    onSuccess: () => {
      invalidate()
      setVoidTarget(null)
      setVoidReason('')
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = () => {
    setFormError(null)
    reset({
      fromBankAccountId: '',
      toBankAccountId: '',
      transferNumber: `TRF-2026-${String(transfers.length + 1).padStart(3, '0')}`,
      amount: 0,
      transferDate: new Date().toISOString().slice(0, 10),
      reference: '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: TransferForm) => {
    setFormError(null)
    if (data.fromBankAccountId === data.toBankAccountId) {
      setFormError('Source and destination accounts must differ.')
      return
    }
    createMutation.mutate({
      companyId: DEMO_COMPANY_ID,
      fromBankAccountId: data.fromBankAccountId,
      toBankAccountId: data.toBankAccountId,
      transferNumber: data.transferNumber,
      amount: data.amount,
      transferDate: new Date(data.transferDate).toISOString(),
      reference: data.reference || null,
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
          title="Bank Transfers"
          description={`${transfers.length} transfer(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Transfer
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : transfers.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No bank transfers yet. Record a transfer between bank accounts to track in-transit cash.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Transfer #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">From</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">To</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {transfers.map(transfer => (
                    <tr key={transfer.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                        {transfer.transferNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{accountName(transfer.fromBankAccountId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{accountName(transfer.toBankAccountId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(transfer.transferDate)}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(transfer.amount)}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={transfer.status} mapping={transferStatusMap} />
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          {transfer.status === 'Draft' && (
                            <Button
                              variant="outline"
                              size="sm"
                              leftIcon={<Send className="h-4 w-4" />}
                              onClick={() => confirmMutation.mutate(transfer.id)}
                              isLoading={confirmMutation.isPending && confirmMutation.variables === transfer.id}
                            >
                              Confirm
                            </Button>
                          )}
                          {transfer.status === 'InTransit' && (
                            <Button
                              variant="success"
                              size="sm"
                              leftIcon={<CheckCircle2 className="h-4 w-4" />}
                              onClick={() => completeMutation.mutate(transfer.id)}
                              isLoading={completeMutation.isPending && completeMutation.variables === transfer.id}
                            >
                              Complete
                            </Button>
                          )}
                          {transfer.status !== 'Voided' && transfer.status !== 'Completed' && (
                            <Button
                              variant="ghost"
                              size="sm"
                              className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                              leftIcon={<Ban className="h-4 w-4" />}
                              onClick={() => {
                                setVoidReason('')
                                setVoidTarget(transfer)
                              }}
                            >
                              Void
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

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title="New Bank Transfer"
        description="Record a transfer between bank accounts. Confirm to move funds, then complete when the bank settles."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Create Transfer
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Combobox
              label="From Account"
              placeholder="Select source account..."
              options={activeAccountOptions}
              value={watch('fromBankAccountId')}
              onChange={value => setValue('fromBankAccountId', value, { shouldValidate: true })}
              required
            />
            <Combobox
              label="To Account"
              placeholder="Select destination account..."
              options={activeAccountOptions}
              value={watch('toBankAccountId')}
              onChange={value => setValue('toBankAccountId', value, { shouldValidate: true })}
              required
            />
            <Input
              {...register('transferNumber')}
              label="Transfer Number"
              {...fieldError(errors.transferNumber?.message)}
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
              {...register('transferDate')}
              type="date"
              label="Transfer Date"
              {...fieldError(errors.transferDate?.message)}
              required
            />
            <Input {...register('reference')} label="Reference" placeholder="Optional memo" />
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={!!voidTarget}
        onClose={() => setVoidTarget(null)}
        title="Void Bank Transfer"
        description="A voided transfer cannot be undone. Completed transfers cannot be voided."
        size="sm"
        footer={
          <>
            <Button variant="secondary" onClick={() => setVoidTarget(null)} disabled={voidMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => voidMutation.mutate()}
              isLoading={voidMutation.isPending}
              disabled={!voidReason.trim()}
            >
              Void Transfer
            </Button>
          </>
        }
      >
        <Input
          value={voidReason}
          onChange={e => setVoidReason(e.target.value)}
          label="Reason"
          placeholder="Required reason for voiding"
          required
        />
      </Modal>
    </div>
  )
}
