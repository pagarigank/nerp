import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, CheckSquare } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select, Checkbox } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import {
  getPayments,
  createPayment,
  selectVouchersForPayment,
  issuePayment,
  voidPayment,
  getVendors,
  getVoucherBatches,
  DEMO_COMPANY_ID,
} from '@api/ap'
import type { Payment, Voucher } from '@/types/ap'
import { paymentMethodMap, paymentStatusMap } from './statusMaps'

const paymentSchema = z.object({
  vendorId: z.string().min(1, 'Vendor is required'),
  paymentReference: z.string().trim().min(1, 'Payment reference is required'),
  paymentDate: z.string().min(1, 'Payment date is required'),
  paymentMethod: z.string().min(1, 'Payment method is required'),
  currencyCode: z.string().trim().min(1, 'Currency is required'),
})

type PaymentForm = z.infer<typeof paymentSchema>

const paymentMethodOptions = Object.entries(paymentMethodMap)
  .filter(([key]) => /^\d+$/.test(key))
  .sort((a, b) => Number(a[0]) - Number(b[0]))
  .map(([value, label]) => ({ value, label }))

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

interface SelectVouchersModalProps {
  payment: Payment | null
  onClose: () => void
  onSave: (voucherIds: string[]) => void
  isSaving: boolean
  error: string | null
}

function SelectVouchersModal({ payment, onClose, onSave, isSaving, error }: SelectVouchersModalProps) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [localError, setLocalError] = useState<string | null>(null)

  const { data: batches = [] } = useQuery({
    queryKey: ['ap', 'voucherBatches'],
    queryFn: () => getVoucherBatches(),
    enabled: !!payment,
  })

  const availableVouchers = useMemo(() => {
    if (!payment) return []
    const result: Voucher[] = []
    for (const batch of batches) {
      const isPosted = batch.status === 2 || batch.status === 'Posted'
      if (!isPosted) continue
      for (const v of batch.vouchers) {
        if (v.vendorId === payment.vendorId && !v.selectedForPayment) {
          result.push(v)
        }
      }
    }
    return result
  }, [batches, payment])

  const selectedAmount = useMemo(
    () => availableVouchers.filter(v => selectedIds.has(v.id)).reduce((sum, v) => sum + v.totalAmount, 0),
    [availableVouchers, selectedIds]
  )

  const handleClose = () => {
    setSelectedIds(new Set())
    setLocalError(null)
    onClose()
  }

  const handleSave = () => {
    if (selectedIds.size === 0) {
      setLocalError('Select at least one voucher to apply to this payment.')
      return
    }
    setLocalError(null)
    onSave(Array.from(selectedIds))
  }

  return (
    <Modal
      isOpen={!!payment}
      onClose={handleClose}
      title="Select Vouchers"
      description={
        payment ? `Apply unposted vouchers to payment ${payment.paymentReference}` : 'Select vouchers to apply'
      }
      size="lg"
      footer={
        <>
          <Button variant="secondary" onClick={handleClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleSave} isLoading={isSaving}>
            Apply {selectedIds.size > 0 ? `(${formatCurrency(selectedAmount)})` : ''}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        {(localError || error) && (
          <div
            className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm"
            role="alert"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
            <span>{localError || error}</span>
          </div>
        )}

        {availableVouchers.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
            No available vouchers for this vendor. Vouchers must be in a posted batch and not already selected for another payment.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Invoice</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Due Date</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Discount</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {availableVouchers.map(voucher => (
                  <tr key={voucher.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                    <td className="px-3 py-2">
                      <Checkbox
                        checked={selectedIds.has(voucher.id)}
                        onChange={e => {
                          setLocalError(null)
                          const next = new Set(selectedIds)
                          if (e.target.checked) {
                            next.add(voucher.id)
                          } else {
                            next.delete(voucher.id)
                          }
                          setSelectedIds(next)
                        }}
                        label={voucher.invoiceNumber}
                      />
                    </td>
                    <td className="px-3 py-2 text-gray-700 dark:text-gray-300">{formatDate(voucher.dueDate)}</td>
                    <td className="px-3 py-2 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                      {formatCurrency(voucher.totalAmount)}
                    </td>
                    <td className="px-3 py-2 text-right font-tabular tabular-nums text-gray-700 dark:text-gray-300">
                      {voucher.discountAmount > 0 ? formatCurrency(voucher.discountAmount) : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </Modal>
  )
}

export function PaymentsPage() {
  const queryClient = useQueryClient()
  const [isCreateOpen, setIsCreateOpen] = useState(false)
  const [selectPayment, setSelectPayment] = useState<Payment | null>(null)
  const [paymentToVoid, setPaymentToVoid] = useState<Payment | null>(null)
  const [voidReason, setVoidReason] = useState('')
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<PaymentForm>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      vendorId: '',
      paymentReference: '',
      paymentDate: new Date().toISOString().slice(0, 10),
      paymentMethod: '0',
      currencyCode: 'USD',
    },
  })

  const selectedVendorId = watch('vendorId')

  const { data: payments = [], isLoading } = useQuery({
    queryKey: ['ap', 'payments'],
    queryFn: () => getPayments(),
  })

  const { data: vendors = [] } = useQuery({
    queryKey: ['ap', 'vendors'],
    queryFn: () => getVendors(),
  })

  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach(v => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const vendorOptions = useMemo(
    () =>
      vendors.map(v => ({
        value: v.id,
        label: `${v.vendorId} - ${v.name}`,
      })),
    [vendors]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ap', 'payments'] })
  }

  const createMutation = useMutation({
    mutationFn: createPayment,
    onSuccess: () => {
      invalidate()
      closeCreate()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const selectMutation = useMutation({
    mutationFn: ({ id, voucherIds }: { id: string; voucherIds: string[] }) =>
      selectVouchersForPayment(id, { voucherIds }),
    onSuccess: () => {
      invalidate()
      setSelectPayment(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const issueMutation = useMutation({
    mutationFn: (id: string) => issuePayment(id),
    onSuccess: invalidate,
    onError: err => setFormError(getErrorMessage(err)),
  })

  const voidMutation = useMutation({
    mutationFn: (id: string) => voidPayment(id, { reason: voidReason }),
    onSuccess: () => {
      invalidate()
      setPaymentToVoid(null)
      setVoidReason('')
    },
    onError: err => {
      setFormError(getErrorMessage(err))
      setPaymentToVoid(null)
      setVoidReason('')
    },
  })

  const openCreate = () => {
    setFormError(null)
    reset({
      vendorId: '',
      paymentReference: `PMT-${String(payments.length + 1).padStart(4, '0')}`,
      paymentDate: new Date().toISOString().slice(0, 10),
      paymentMethod: '0',
      currencyCode: 'USD',
    })
    setIsCreateOpen(true)
  }

  const closeCreate = () => {
    setIsCreateOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: PaymentForm) => {
    setFormError(null)
    createMutation.mutate({
      companyId: DEMO_COMPANY_ID,
      vendorId: data.vendorId,
      paymentReference: data.paymentReference,
      paymentDate: new Date(data.paymentDate).toISOString(),
      paymentMethod: Number(data.paymentMethod),
      currencyCode: data.currencyCode,
      bankAccountId: null,
    })
  }

  const totalIssued = payments.filter(p => p.status === 1 || p.status === 'Issued').reduce((s, p) => s + p.totalAmount, 0)

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
          title="Payments"
          description={`${payments.length} payment(s) — issued total ${formatCurrency(totalIssued)}`}
          action={
            <Button variant="primary" size="sm" onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
              New Payment
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : payments.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No payments yet. Create a payment and select vouchers to apply.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Payment #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Vendor</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Method</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Total</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {payments.map(payment => {
                    const isSelected = payment.status === 0 || payment.status === 'Selected'
                    const isIssued = payment.status === 1 || payment.status === 'Issued'
                    return (
                      <tr key={payment.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{payment.paymentReference}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{vendorNames.get(payment.vendorId) ?? payment.vendorId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(payment.paymentDate)}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{paymentMethodMap[String(payment.paymentMethod)] ?? payment.paymentMethod}</td>
                        <td className="px-3 py-3">
                          <MapStatusBadge value={payment.status} mapping={paymentStatusMap} />
                        </td>
                        <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                          {formatCurrency(payment.totalAmount)}
                        </td>
                        <td className="px-3 py-3">
                          <div className="flex items-center justify-end gap-1">
                            {isSelected && (
                              <>
                                <Button
                                  size="sm"
                                  variant="outline"
                                  onClick={() => setSelectPayment(payment)}
                                  leftIcon={<CheckSquare className="h-4 w-4" />}
                                >
                                  Vouchers
                                </Button>
                                <Button
                                  size="sm"
                                  variant="success"
                                  onClick={() => issueMutation.mutate(payment.id)}
                                  disabled={payment.lines.length === 0}
                                >
                                  Issue
                                </Button>
                              </>
                            )}
                            {isIssued && (
                              <Button
                                size="sm"
                                variant="destructive"
                                onClick={() => setPaymentToVoid(payment)}
                              >
                                Void
                              </Button>
                            )}
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
        isOpen={isCreateOpen}
        onClose={closeCreate}
        title="New Payment"
        description="Create a payment for a vendor, then select vouchers to apply."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeCreate} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Create Payment
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Combobox
            label="Vendor"
            placeholder="Select vendor..."
            options={vendorOptions}
            value={selectedVendorId}
            onChange={value => {
              setValue('vendorId', value, { shouldValidate: true })
            }}
            {...(errors.vendorId?.message ? { error: errors.vendorId.message } : {})}
            required
          />
          <Input
            {...register('paymentReference')}
            label="Payment Reference"
            {...fieldError(errors.paymentReference?.message)}
            required
          />
          <Input
            {...register('paymentDate')}
            type="date"
            label="Payment Date"
            {...fieldError(errors.paymentDate?.message)}
            required
          />
          <Select
            {...register('paymentMethod')}
            label="Payment Method"
            options={paymentMethodOptions}
            {...fieldError(errors.paymentMethod?.message)}
            required
          />
          <Input
            {...register('currencyCode')}
            label="Currency"
            placeholder="USD"
            {...fieldError(errors.currencyCode?.message)}
            required
          />
        </form>
      </Modal>

      <SelectVouchersModal
        payment={selectPayment}
        onClose={() => setSelectPayment(null)}
        onSave={voucherIds => selectMutation.mutate({ id: selectPayment!.id, voucherIds })}
        isSaving={selectMutation.isPending}
        error={formError}
      />

      <Modal
        isOpen={!!paymentToVoid}
        onClose={() => {
          setPaymentToVoid(null)
          setVoidReason('')
        }}
        title="Void Payment"
        description={
          paymentToVoid
            ? `Void payment ${paymentToVoid.paymentReference} (${formatCurrency(paymentToVoid.totalAmount)})`
            : 'Void payment'
        }
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={() => setPaymentToVoid(null)} disabled={voidMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => paymentToVoid && voidMutation.mutate(paymentToVoid.id)}
              isLoading={voidMutation.isPending}
              disabled={!voidReason.trim()}
            >
              Void Payment
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input
            label="Reason"
            placeholder="Required — why is this payment being voided?"
            value={voidReason}
            onChange={e => setVoidReason(e.target.value)}
            required
          />
        </div>
      </Modal>
    </div>
  )
}
