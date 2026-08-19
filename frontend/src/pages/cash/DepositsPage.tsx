import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Trash2, AlertCircle, CheckCircle2, Ban, Layers, X } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import { getCashReceipts } from '@api/ar'
import {
  getDeposits,
  getBankAccounts,
  createDeposit,
  createDepositFromAr,
  confirmDeposit,
  clearDeposit,
  deleteDeposit,
  DEMO_COMPANY_ID,
} from '@api/cash'
import type { CashDeposit, CreateCashDepositRequest, DepositLineItem } from '@/types/cash'
import { depositStatusMap } from './statusMaps'

const sourceOptions = [
  { value: '0', label: 'Manual' },
  { value: '1', label: 'AR Cash Receipt' },
]

const depositSchema = z.object({
  bankAccountId: z.string().min(1, 'Select a bank account'),
  depositNumber: z.string().trim().min(1, 'Deposit number is required'),
  depositDate: z.string().min(1, 'Deposit date is required'),
  reference: z.string().optional(),
  fromArReceiptId: z.string().optional(),
})

type DepositForm = z.infer<typeof depositSchema>

interface LineDraft {
  id: string
  source: string
  sourceReferenceId: string
  amount: string
  description: string
}

let lineCounter = 0
function newLine(): LineDraft {
  lineCounter += 1
  return { id: `line-${lineCounter}`, source: '0', sourceReferenceId: '', amount: '', description: '' }
}

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function DepositsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [mode, setMode] = useState<'manual' | 'fromAr'>('manual')
  const [lines, setLines] = useState<LineDraft[]>([])
  const [formError, setFormError] = useState<string | null>(null)
  const [depositToDelete, setDepositToDelete] = useState<CashDeposit | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<DepositForm>({
    resolver: zodResolver(depositSchema),
    defaultValues: {
      bankAccountId: '',
      depositNumber: '',
      depositDate: new Date().toISOString().slice(0, 10),
      reference: '',
      fromArReceiptId: '',
    },
  })

  const { data: deposits = [], isLoading } = useQuery({
    queryKey: ['cash', 'deposits'],
    queryFn: () => getDeposits(),
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
        .filter(r => r.status !== 'Refunded' && r.totalAmount > 0)
        .map(r => ({ value: r.id, label: `${r.receiptReference} - ${formatCurrency(r.totalAmount)}` })),
    [receipts]
  )

  const accountName = useMemo(
    () => accounts.find(a => a.id === watch('bankAccountId'))?.accountName ?? '',
    [accounts, watch]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'deposits'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: CreateCashDepositRequest) => createDeposit(data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const createFromArMutation = useMutation({
    mutationFn: (data: { bankAccountId: string; depositNumber: string; depositDate: string; cashReceiptId: string }) =>
      createDepositFromAr({
        companyId: DEMO_COMPANY_ID,
        ...data,
      }),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const confirmMutation = useMutation({
    mutationFn: confirmDeposit,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const clearMutation = useMutation({
    mutationFn: clearDeposit,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteDeposit,
    onSuccess: () => {
      invalidate()
      setDepositToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = (nextMode: 'manual' | 'fromAr' = 'manual') => {
    setMode(nextMode)
    setFormError(null)
    setLines([newLine()])
    reset({
      bankAccountId: '',
      depositNumber: `DEP-2026-${String(deposits.length + 1).padStart(3, '0')}`,
      depositDate: new Date().toISOString().slice(0, 10),
      reference: '',
      fromArReceiptId: '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
    setLines([])
  }

  const onSubmit = (data: DepositForm) => {
    setFormError(null)
    if (mode === 'fromAr') {
      const receiptId = watch('fromArReceiptId')
      if (!receiptId) {
        setFormError('Select an AR cash receipt to deposit.')
        return
      }
      createFromArMutation.mutate({
        bankAccountId: data.bankAccountId,
        depositNumber: data.depositNumber,
        depositDate: new Date(data.depositDate).toISOString(),
        cashReceiptId: receiptId,
      })
      return
    }

    const lineItems: DepositLineItem[] = lines
      .filter(l => Number(l.amount) > 0)
      .map(l => ({
        source: Number(l.source),
        sourceReferenceId: l.sourceReferenceId || null,
        amount: Number(l.amount),
        description: l.description || null,
      }))

    if (lineItems.length === 0) {
      setFormError('Add at least one deposit line with an amount greater than zero.')
      return
    }

    createMutation.mutate({
      companyId: DEMO_COMPANY_ID,
      bankAccountId: data.bankAccountId,
      depositNumber: data.depositNumber,
      depositDate: new Date(data.depositDate).toISOString(),
      reference: data.reference || null,
      lines: lineItems,
    })
  }

  const lineTotal = useMemo(
    () => lines.reduce((sum, l) => sum + (Number(l.amount) || 0), 0),
    [lines]
  )

  const updateLine = (id: string, patch: Partial<LineDraft>) => {
    setLines(prev => prev.map(l => (l.id === id ? { ...l, ...patch } : l)))
  }

  const removeLine = (id: string) => {
    setLines(prev => (prev.length === 1 ? prev : prev.filter(l => l.id !== id)))
  }

  const filteredDeposits = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return deposits
    return deposits.filter(
      d => d.depositNumber.toLowerCase().includes(q) || d.reference?.toLowerCase().includes(q)
    )
  }, [deposits, search])

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
          title="Deposits"
          description={`${deposits.length} deposit(s) on file`}
          action={
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => openForm('fromAr')}
                leftIcon={<Layers className="h-4 w-4" />}
              >
                From AR Receipt
              </Button>
              <Button variant="primary" size="sm" onClick={() => openForm('manual')} leftIcon={<Plus className="h-4 w-4" />}>
                New Deposit
              </Button>
            </div>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by deposit number or reference..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search deposits"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : filteredDeposits.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No deposits match your search.' : 'No deposits yet. Record your first deposit.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Deposit #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bank Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Reference</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredDeposits.map(deposit => (
                    <tr key={deposit.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-gray-900 dark:text-white">
                        {deposit.depositNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(deposit.depositDate)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {accounts.find(a => a.id === deposit.bankAccountId)?.accountName ?? '—'}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{deposit.reference ?? '—'}</td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={deposit.status} mapping={depositStatusMap} />
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(deposit.totalAmount)}
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          {deposit.status === 'Draft' && (
                            <Button
                              variant="success"
                              size="sm"
                              leftIcon={<CheckCircle2 className="h-4 w-4" />}
                              onClick={() => confirmMutation.mutate(deposit.id)}
                              isLoading={confirmMutation.isPending && confirmMutation.variables === deposit.id}
                            >
                              Confirm
                            </Button>
                          )}
                          {deposit.status === 'Confirmed' && (
                            <Button
                              variant="outline"
                              size="sm"
                              leftIcon={<Ban className="h-4 w-4" />}
                              onClick={() => clearMutation.mutate(deposit.id)}
                              isLoading={clearMutation.isPending && clearMutation.variables === deposit.id}
                            >
                              Clear
                            </Button>
                          )}
                          {deposit.status === 'Draft' && (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-red-600"
                              aria-label={`Delete ${deposit.depositNumber}`}
                              onClick={() => setDepositToDelete(deposit)}
                            >
                              <Trash2 className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
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
        title={mode === 'fromAr' ? 'Deposit from AR Receipt' : 'New Deposit'}
        description={
          mode === 'fromAr'
            ? 'Create a deposit from an existing AR cash receipt'
            : 'Record money deposited into a bank account'
        }
        size="lg"
        footer={
          <>
            <Button
              variant="secondary"
              onClick={closeForm}
              disabled={createMutation.isPending || createFromArMutation.isPending}
            >
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={handleSubmit(onSubmit)}
              isLoading={createMutation.isPending || createFromArMutation.isPending}
            >
              Create Deposit
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
              {accountName && (
                <p className="mt-1.5 text-xs text-gray-500 dark:text-gray-400">Depositing into: {accountName}</p>
              )}
            </div>
            <Input
              {...register('depositNumber')}
              label="Deposit Number"
              {...fieldError(errors.depositNumber?.message)}
              required
            />
            <Input
              {...register('depositDate')}
              type="date"
              label="Deposit Date"
              {...fieldError(errors.depositDate?.message)}
              required
            />
            {mode === 'manual' && <Input {...register('reference')} label="Reference" placeholder="Optional note" />}
          </div>

          {mode === 'fromAr' ? (
            <div>
              <Combobox
                label="AR Cash Receipt"
                placeholder="Select a cash receipt..."
                options={receiptOptions}
                value={watch('fromArReceiptId') ?? ''}
                onChange={value => setValue('fromArReceiptId', value, { shouldValidate: true })}
                required
              />
              <p className="mt-1.5 text-xs text-gray-500 dark:text-gray-400">
                A single line will be added referencing the selected receipt.
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <h4 className="text-sm font-medium text-gray-900 dark:text-white">Deposit Lines</h4>
                <Button variant="outline" size="sm" onClick={() => setLines(prev => [...prev, newLine()])}>
                  Add Line
                </Button>
              </div>
              <div className="space-y-3">
                {lines.map(line => (
                  <div key={line.id} className="flex flex-col sm:flex-row gap-3 items-end">
                    <Select
                      className="sm:w-44"
                      aria-label="Source"
                      options={sourceOptions}
                      value={line.source}
                      onChange={e => updateLine(line.id, { source: e.target.value })}
                    />
                    {line.source === '1' ? (
                      <Combobox
                        className="sm:flex-1"
                        placeholder="Select AR receipt..."
                        options={receiptOptions}
                        value={line.sourceReferenceId}
                        onChange={value => updateLine(line.id, { sourceReferenceId: value })}
                        allowClear
                      />
                    ) : (
                      <Input
                        className="sm:flex-1"
                        placeholder="Description"
                        value={line.description}
                        onChange={e => updateLine(line.id, { description: e.target.value })}
                      />
                    )}
                    <Input
                      className="sm:w-40"
                      type="number"
                      step="0.01"
                      min="0"
                      placeholder="0.00"
                      aria-label="Amount"
                      value={line.amount}
                      onChange={e => updateLine(line.id, { amount: e.target.value })}
                    />
                    <IconButton
                      size="sm"
                      variant="ghost"
                      className="text-red-600"
                      aria-label="Remove line"
                      onClick={() => removeLine(line.id)}
                    >
                      <X className="h-4 w-4" aria-hidden="true" />
                    </IconButton>
                  </div>
                ))}
              </div>
              <div className="flex items-center justify-between text-sm">
                <span className="text-gray-500 dark:text-gray-400">Total</span>
                <span className="font-semibold font-tabular tabular-nums text-gray-900 dark:text-white">
                  {formatCurrency(lineTotal)}
                </span>
              </div>
            </div>
          )}
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!depositToDelete}
        onClose={() => setDepositToDelete(null)}
        onConfirm={() => depositToDelete && deleteMutation.mutate(depositToDelete.id)}
        title="Delete Deposit"
        message={
          depositToDelete
            ? `Are you sure you want to delete deposit "${depositToDelete.depositNumber}"? This is a soft delete and can be reversed by an administrator.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
