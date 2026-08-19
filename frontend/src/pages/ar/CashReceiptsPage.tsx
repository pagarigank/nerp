import { useEffect, useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, Wand2, ArrowRightLeft, X } from 'lucide-react'
import { formatCurrency } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, Drawer } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getCashReceipts,
  createCashReceipt,
  applyCash,
  autoApplyCash,
  getInvoices,
  getCustomers,
  DEMO_COMPANY_ID,
} from '@api/ar'
import type { ArCashReceipt } from '@/types/ar'
import { receiptStatusMap } from './statusMaps'
import { ArStatusBadge } from './ArStatusBadge'

const receiptSchema = z.object({
  customerId: z.string().min(1, 'Select a customer'),
  receiptReference: z.string().trim().min(1, 'Receipt reference is required'),
  totalAmount: z.coerce.number().positive('Amount must be greater than zero'),
  receiptDate: z.string().min(1, 'Receipt date is required'),
  paymentMethod: z.string().min(1, 'Payment method is required'),
  currencyCode: z.string(),
  referenceNumber: z.string().optional(),
})

type ReceiptForm = z.infer<typeof receiptSchema>

const paymentMethodOptions = ['Check', 'ACH', 'Wire', 'Cash', 'CreditCard', 'Other'].map(value => ({
  value,
  label: value,
}))

const currencyOptions = ['USD', 'CAD', 'EUR', 'GBP', 'MXN'].map(code => ({ value: code, label: code }))

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function CashReceiptsPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [applyReceipt, setApplyReceipt] = useState<ArCashReceipt | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<ReceiptForm>({
    resolver: zodResolver(receiptSchema),
    defaultValues: {
      customerId: '',
      receiptReference: '',
      totalAmount: 0,
      receiptDate: new Date().toISOString().slice(0, 10),
      paymentMethod: 'Check',
      currencyCode: 'USD',
      referenceNumber: '',
    },
  })

  const { data: receipts = [], isLoading } = useQuery({
    queryKey: ['ar', 'cashReceipts'],
    queryFn: () => getCashReceipts(),
  })

  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: getCustomers,
  })

  const customerOptions = useMemo(
    () => customers.map(c => ({ value: c.id, label: `${c.customerId} - ${c.name}` })),
    [customers]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ar', 'cashReceipts'] })
    queryClient.invalidateQueries({ queryKey: ['ar', 'invoices'] })
  }

  const selectedCustomerName = useMemo(
    () => customers.find(c => c.id === watch('customerId'))?.name ?? '',
    [customers, watch]
  )

  const createMutation = useMutation({
    mutationFn: createCashReceipt,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = () => {
    setFormError(null)
    reset({
      customerId: '',
      receiptReference: `CR-2026-${String(receipts.length + 1).padStart(3, '0')}`,
      totalAmount: 0,
      receiptDate: new Date().toISOString().slice(0, 10),
      paymentMethod: 'Check',
      currencyCode: 'USD',
      referenceNumber: '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: ReceiptForm) => {
    setFormError(null)
    createMutation.mutate({
      companyId: DEMO_COMPANY_ID,
      customerId: data.customerId,
      receiptReference: data.receiptReference,
      totalAmount: data.totalAmount,
      receiptDate: new Date(data.receiptDate).toISOString(),
      paymentMethod: data.paymentMethod,
      currencyCode: data.currencyCode,
      referenceNumber: data.referenceNumber || null,
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
          title="Cash Receipts"
          description={`${receipts.length} receipt(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Receipt
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : receipts.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No cash receipts yet. Create a receipt to record customer payments.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Reference</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Applied</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Unapplied</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {receipts.map(receipt => {
                    const isApplied = receipt.status === 'FullyApplied' || receipt.status === 'Refunded'
                    return (
                      <tr key={receipt.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-3 font-medium text-primary-600 dark:text-primary-400">
                          {receipt.receiptReference}
                        </td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                          {customers.find(c => c.id === receipt.customerId)?.name ?? receipt.customerId}
                        </td>
                        <td className="px-3 py-3">
                          <ArStatusBadge value={receipt.status} mapping={receiptStatusMap} />
                        </td>
                        <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                          {formatCurrency(receipt.totalAmount)}
                        </td>
                        <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                          {formatCurrency(receipt.appliedAmount)}
                        </td>
                        <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                          {formatCurrency(receipt.unappliedAmount)}
                        </td>
                        <td className="px-3 py-3">
                          <div className="flex items-center justify-end">
                            <Button
                              variant="outline"
                              size="sm"
                              disabled={isApplied}
                              leftIcon={<ArrowRightLeft className="h-4 w-4" />}
                              onClick={() => {
                                setFormError(null)
                                setApplyReceipt(receipt)
                              }}
                            >
                              Apply
                            </Button>
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title="New Cash Receipt"
        description="Record a customer payment. Apply it to open invoices after creation."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Create Receipt
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="sm:col-span-2">
              <Combobox
                label="Customer"
                placeholder="Select customer..."
                options={customerOptions}
                value={watch('customerId')}
                onChange={value => setValue('customerId', value, { shouldValidate: true })}
                required
              />
              {selectedCustomerName && (
                <p className="mt-1.5 text-xs text-gray-500 dark:text-gray-400">Paying: {selectedCustomerName}</p>
              )}
            </div>
            <Input
              {...register('receiptReference')}
              label="Receipt Reference"
              {...fieldError(errors.receiptReference?.message)}
              required
            />
            <Input
              {...register('totalAmount')}
              type="number"
              step="0.01"
              min="0.01"
              label="Amount"
              {...fieldError(errors.totalAmount?.message)}
              required
            />
            <Input
              {...register('receiptDate')}
              type="date"
              label="Receipt Date"
              {...fieldError(errors.receiptDate?.message)}
              required
            />
            <Select
              {...register('paymentMethod')}
              label="Payment Method"
              options={paymentMethodOptions}
              {...fieldError(errors.paymentMethod?.message)}
            />
            <Select
              {...register('currencyCode')}
              label="Currency"
              options={currencyOptions}
              {...fieldError(errors.currencyCode?.message)}
            />
            <Input {...register('referenceNumber')} label="Bank Reference #" placeholder="Optional check/ACH number" />
          </div>
        </form>
      </Modal>

      <ApplyDrawer
        receipt={applyReceipt}
        onClose={() => setApplyReceipt(null)}
        onApplied={invalidate}
      />
    </div>
  )
}

interface ApplyDrawerProps {
  receipt: ArCashReceipt | null
  onClose: () => void
  onApplied: () => void
}

function ApplyDrawer({ receipt, onClose, onApplied }: ApplyDrawerProps) {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [amounts, setAmounts] = useState<Record<string, string>>({})

  useEffect(() => {
    setError(null)
    setAmounts({})
  }, [receipt?.id])

  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: getCustomers,
  })

  const { data: invoices = [], isLoading } = useQuery({
    queryKey: ['ar', 'invoices', receipt?.customerId],
    queryFn: () => getInvoices({ customerId: receipt?.customerId ?? '' }),
    enabled: !!receipt,
  })

  const openInvoices = useMemo(
    () => invoices.filter(i => i.balanceDue > 0 && i.status !== 'Voided'),
    [invoices]
  )

  const customer = receipt ? customers.find(c => c.id === receipt.customerId) : undefined

  const applyMutation = useMutation({
    mutationFn: ({ invoiceId, amount }: { invoiceId: string; amount: number }) =>
      applyCash(receipt!.id, { invoiceId, amount }),
    onSuccess: () => {
      setError(null)
      queryClient.invalidateQueries({ queryKey: ['ar', 'invoices'] })
      queryClient.invalidateQueries({ queryKey: ['ar', 'cashReceipts'] })
      onApplied()
    },
    onError: err => setError(getErrorMessage(err)),
  })

  const autoApplyMutation = useMutation({
    mutationFn: () => autoApplyCash(receipt!.id),
    onSuccess: () => {
      setError(null)
      queryClient.invalidateQueries({ queryKey: ['ar', 'invoices'] })
      queryClient.invalidateQueries({ queryKey: ['ar', 'cashReceipts'] })
      onApplied()
    },
    onError: err => setError(getErrorMessage(err)),
  })

  if (!receipt) return null

  return (
    <Drawer isOpen={!!receipt} onClose={onClose} title={`Apply ${receipt.receiptReference}`} size="lg">
      <div className="space-y-5">
        <div className="rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 p-4 space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-500 dark:text-gray-400">Customer</span>
            <span className="font-medium text-gray-900 dark:text-white">{customer?.name ?? receipt.customerId}</span>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-500 dark:text-gray-400">Receipt Amount</span>
            <span className="font-medium font-tabular tabular-nums text-gray-900 dark:text-white">
              {formatCurrency(receipt.totalAmount)}
            </span>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-500 dark:text-gray-400">Applied</span>
            <span className="font-medium font-tabular tabular-nums text-gray-900 dark:text-white">
              {formatCurrency(receipt.appliedAmount)}
            </span>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="text-gray-500 dark:text-gray-400">Unapplied</span>
            <span className="font-semibold font-tabular tabular-nums text-emerald-600 dark:text-emerald-400">
              {formatCurrency(receipt.unappliedAmount)}
            </span>
          </div>
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

        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-sm font-medium text-gray-900 dark:text-white">Open Invoices</h3>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {openInvoices.length} invoice(s) available to apply
            </p>
          </div>
          <Button
            variant="outline"
            size="sm"
            leftIcon={<Wand2 className="h-4 w-4" />}
            onClick={() => autoApplyMutation.mutate()}
            isLoading={autoApplyMutation.isPending}
            disabled={receipt.unappliedAmount <= 0 || openInvoices.length === 0}
          >
            Auto Apply
          </Button>
        </div>

        {isLoading ? (
          <SkeletonTable columns={4} rows={3} />
        ) : openInvoices.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-6 text-center">
            No open invoices for this customer.
          </p>
        ) : (
          <div className="overflow-x-auto border border-gray-200 dark:border-gray-700 rounded-lg">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 text-left bg-gray-50 dark:bg-gray-900/50">
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Invoice #</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Balance Due</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                  <th className="px-3 py-2 w-24" />
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {openInvoices.map(invoice => (
                  <tr key={invoice.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                    <td className="px-3 py-2.5 font-medium text-primary-600 dark:text-primary-400">
                      {invoice.invoiceNumber}
                    </td>
                    <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                      {formatCurrency(invoice.balanceDue)}
                    </td>
                    <td className="px-3 py-2.5">
                      <Input
                        type="number"
                        step="0.01"
                        min="0"
                        max={invoice.balanceDue}
                        value={amounts[invoice.id] ?? ''}
                        placeholder={String(invoice.balanceDue)}
                        onChange={e =>
                          setAmounts(prev => ({ ...prev, [invoice.id]: e.target.value }))
                        }
                        className="text-right tabular-nums w-32 ml-auto"
                        aria-label={`Apply amount to ${invoice.invoiceNumber}`}
                      />
                    </td>
                    <td className="px-3 py-2.5">
                      <Button
                        variant="secondary"
                        size="sm"
                        disabled={receipt.unappliedAmount <= 0}
                        isLoading={applyMutation.isPending && applyMutation.variables?.invoiceId === invoice.id}
                        onClick={() => {
                          const amount = Number(amounts[invoice.id] || invoice.balanceDue)
                          if (amount <= 0) return
                          applyMutation.mutate({ invoiceId: invoice.id, amount })
                        }}
                      >
                        Apply
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
          <X className="h-4 w-4" aria-hidden="true" />
          Enter an amount (defaults to full balance) and click Apply to allocate the receipt.
        </div>
      </div>
    </Drawer>
  )
}
