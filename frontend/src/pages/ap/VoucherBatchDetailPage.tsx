import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { ArrowLeft, Plus, AlertCircle, X } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import {
  getVoucherBatch,
  addVoucherToBatch,
  releaseVoucherBatch,
  postVoucherBatch,
  reverseVoucherBatch,
  getVendors,
} from '@api/ap'
import { getAccounts } from '@api/platform'
import type { Voucher } from '@/types/ap'
import { voucherBatchStatusMap, voucherTypeMap } from './statusMaps'

interface DistributionDraft {
  accountId: string
  debit: string
  credit: string
}

interface AddVoucherModalProps {
  isOpen: boolean
  onClose: () => void
  onSave: (data: Parameters<typeof addVoucherToBatch>[1]) => void
  isSaving: boolean
  error: string | null
}

function AddVoucherModal({ isOpen, onClose, onSave, isSaving, error }: AddVoucherModalProps) {
  const [vendorId, setVendorId] = useState('')
  const [voucherType, setVoucherType] = useState('0')
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [invoiceDate, setInvoiceDate] = useState(new Date().toISOString().slice(0, 10))
  const [dueDate, setDueDate] = useState(new Date().toISOString().slice(0, 10))
  const [totalAmount, setTotalAmount] = useState('')
  const [discountAmount, setDiscountAmount] = useState('0')
  const [description, setDescription] = useState('')
  const [paymentTermId, setPaymentTermId] = useState('')
  const [form1099Amount, setForm1099Amount] = useState('0')
  const [backupWithholdingAmount, setBackupWithholdingAmount] = useState('0')
  const [distributions, setDistributions] = useState<DistributionDraft[]>([{ accountId: '', debit: '', credit: '' }])
  const [localError, setLocalError] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({
    queryKey: ['ap', 'vendors'],
    queryFn: () => getVendors(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const vendorOptions = useMemo(
    () =>
      vendors.map(v => ({
        value: v.id,
        label: `${v.vendorId} - ${v.name}`,
      })),
    [vendors]
  )

  const accountOptions = useMemo(
    () =>
      accounts.map(a => ({
        value: a.id,
        label: `${a.accountNumber} - ${a.description}`,
      })),
    [accounts]
  )

  const voucherTypeOptions = useMemo(
    () =>
      Object.entries(voucherTypeMap)
        .filter(([key]) => /^\d+$/.test(key))
        .sort((a, b) => Number(a[0]) - Number(b[0]))
        .map(([value, label]) => ({ value, label })),
    []
  )

  const resetState = () => {
    setVendorId('')
    setVoucherType('0')
    setInvoiceNumber('')
    setInvoiceDate(new Date().toISOString().slice(0, 10))
    setDueDate(new Date().toISOString().slice(0, 10))
    setTotalAmount('')
    setDiscountAmount('0')
    setDescription('')
    setPaymentTermId('')
    setForm1099Amount('0')
    setBackupWithholdingAmount('0')
    setDistributions([{ accountId: '', debit: '', credit: '' }])
    setLocalError(null)
  }

  const updateDistribution = (index: number, field: keyof DistributionDraft, value: string) => {
    setDistributions(prev => prev.map((d, i) => (i === index ? { ...d, [field]: value } : d)))
    setLocalError(null)
  }

  const handleSave = () => {
    if (!vendorId) {
      setLocalError('Select a vendor for the voucher.')
      return
    }
    if (!invoiceNumber.trim()) {
      setLocalError('Invoice number is required.')
      return
    }
    const total = Number(totalAmount) || 0
    if (total <= 0) {
      setLocalError('Total amount must be greater than zero.')
      return
    }
    if (distributions.length === 0) {
      setLocalError('Add at least one distribution line.')
      return
    }

    let totalDebits = 0
    let totalCredits = 0
    for (const d of distributions) {
      if (!d.accountId) {
        setLocalError('Every distribution line needs an account.')
        return
      }
      const debit = Number(d.debit) || 0
      const credit = Number(d.credit) || 0
      if (debit <= 0 && credit <= 0) {
        setLocalError('Each distribution line needs a debit or credit amount greater than zero.')
        return
      }
      if (debit > 0 && credit > 0) {
        setLocalError('A distribution line can have either a debit or a credit, not both.')
        return
      }
      totalDebits += debit
      totalCredits += credit
    }
    if (Math.abs(totalDebits - totalCredits) > 0.005) {
      setLocalError(`Distributions must balance. Debits ${formatCurrency(totalDebits)} vs credits ${formatCurrency(totalCredits)}.`)
      return
    }

    setLocalError(null)
    onSave({
      vendorId,
      voucherType: Number(voucherType),
      invoiceNumber: invoiceNumber.trim(),
      invoiceDate: new Date(invoiceDate).toISOString(),
      dueDate: new Date(dueDate).toISOString(),
      totalAmount: total,
      discountAmount: Number(discountAmount) || 0,
      description: description.trim() || null,
      paymentTermId: paymentTermId || null,
      purchaseOrderId: null,
      receiptLineId: null,
      form1099Amount: Number(form1099Amount) || 0,
      backupWithholdingAmount: Number(backupWithholdingAmount) || 0,
      distributions: distributions.map(d => ({
        accountId: d.accountId,
        debit: Number(d.debit) || 0,
        credit: Number(d.credit) || 0,
        projectId: null,
        taskId: null,
      })),
    })
  }

  const handleClose = () => {
    resetState()
    onClose()
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Add Voucher"
      description="Add a vendor invoice and its account distribution lines to this batch."
      size="lg"
      footer={
        <>
          <Button variant="secondary" onClick={handleClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleSave} isLoading={isSaving}>
            Add Voucher
          </Button>
        </>
      }
    >
      <div className="space-y-5">
        {(localError || error) && (
          <div
            className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm"
            role="alert"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
            <span>{localError || error}</span>
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Combobox
            label="Vendor"
            placeholder="Select vendor..."
            options={vendorOptions}
            value={vendorId}
            onChange={value => {
              setVendorId(value)
              setLocalError(null)
            }}
            required
          />
          <Select
            label="Voucher Type"
            options={voucherTypeOptions}
            value={voucherType}
            onChange={e => setVoucherType(e.target.value)}
            required
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="Invoice Number"
            placeholder="Vendor invoice number"
            value={invoiceNumber}
            onChange={e => setInvoiceNumber(e.target.value)}
            required
          />
          <Input
            label="Description"
            placeholder="Optional"
            value={description}
            onChange={e => setDescription(e.target.value)}
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="Invoice Date"
            type="date"
            value={invoiceDate}
            onChange={e => setInvoiceDate(e.target.value)}
            required
          />
          <Input
            label="Due Date"
            type="date"
            value={dueDate}
            onChange={e => setDueDate(e.target.value)}
            required
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <Input
            label="Total Amount"
            type="number"
            step="0.01"
            min="0"
            value={totalAmount}
            onChange={e => setTotalAmount(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
            required
          />
          <Input
            label="Discount Amount"
            type="number"
            step="0.01"
            min="0"
            value={discountAmount}
            onChange={e => setDiscountAmount(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
          />
          <Input
            label="Payment Term"
            placeholder="Optional term id"
            value={paymentTermId}
            onChange={e => setPaymentTermId(e.target.value)}
          />
        </div>

        <div className="space-y-3">
          <h3 className="text-sm font-medium text-gray-900 dark:text-white">Distribution Lines</h3>
          {distributions.map((dist, index) => (
            <div key={index} className="grid grid-cols-1 sm:grid-cols-[1fr_120px_120px_auto] gap-3 items-start">
              <Combobox
                {...(index === 0 ? { label: 'Account' } : {})}
                placeholder="Select account..."
                options={accountOptions}
                value={dist.accountId}
                onChange={value => updateDistribution(index, 'accountId', value)}
              />
              <Input
                {...(index === 0 ? { label: 'Debit' } : {})}
                type="number"
                step="0.01"
                min="0"
                value={dist.debit}
                onChange={e => updateDistribution(index, 'debit', e.target.value)}
                placeholder="0.00"
                className="text-right tabular-nums"
              />
              <Input
                {...(index === 0 ? { label: 'Credit' } : {})}
                type="number"
                step="0.01"
                min="0"
                value={dist.credit}
                onChange={e => updateDistribution(index, 'credit', e.target.value)}
                placeholder="0.00"
                className="text-right tabular-nums"
              />
              <div className="flex items-end gap-2">
                <IconButton
                  size="sm"
                  variant="ghost"
                  className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                  aria-label={`Remove distribution line ${index + 1}`}
                  onClick={() => setDistributions(prev => prev.filter((_, i) => i !== index))}
                >
                  <X className="h-4 w-4" aria-hidden="true" />
                </IconButton>
              </div>
            </div>
          ))}
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => setDistributions(prev => [...prev, { accountId: '', debit: '', credit: '' }])}
            leftIcon={<Plus className="h-4 w-4" />}
          >
            Add Distribution
          </Button>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="1099 Amount"
            type="number"
            step="0.01"
            min="0"
            value={form1099Amount}
            onChange={e => setForm1099Amount(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
          />
          <Input
            label="Backup Withholding Amount"
            type="number"
            step="0.01"
            min="0"
            value={backupWithholdingAmount}
            onChange={e => setBackupWithholdingAmount(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
          />
        </div>
      </div>
    </Modal>
  )
}

export function VoucherBatchDetailPage() {
  const { batchId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isAddOpen, setIsAddOpen] = useState(false)
  const [actionDialog, setActionDialog] = useState<'release' | 'post' | null>(null)
  const [isReverseOpen, setIsReverseOpen] = useState(false)
  const [reverseReason, setReverseReason] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)

  const { data: batch, isLoading, isError } = useQuery({
    queryKey: ['ap', 'voucherBatch', batchId],
    queryFn: () => getVoucherBatch(batchId),
    enabled: !!batchId,
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

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ap', 'voucherBatch', batchId] })
    queryClient.invalidateQueries({ queryKey: ['ap', 'voucherBatches'] })
  }

  const addVoucherMutation = useMutation({
    mutationFn: (data: Parameters<typeof addVoucherToBatch>[1]) => addVoucherToBatch(batchId, data),
    onSuccess: () => {
      invalidate()
      setIsAddOpen(false)
    },
    onError: err => setActionError(getErrorMessage(err)),
  })

  const releaseMutation = useMutation({
    mutationFn: () => releaseVoucherBatch(batchId),
    onSuccess: () => {
      invalidate()
      setActionDialog(null)
    },
    onError: err => {
      setActionDialog(null)
      setActionError(getErrorMessage(err))
    },
  })

  const postMutation = useMutation({
    mutationFn: () => postVoucherBatch(batchId),
    onSuccess: () => {
      invalidate()
      setActionDialog(null)
    },
    onError: err => {
      setActionDialog(null)
      setActionError(getErrorMessage(err))
    },
  })

  const reverseMutation = useMutation({
    mutationFn: () => reverseVoucherBatch(batchId, { reason: reverseReason }),
    onSuccess: reversed => {
      invalidate()
      setIsReverseOpen(false)
      setReverseReason('')
      navigate(`/ap/voucher-batches/${reversed.id}`)
    },
    onError: err => setActionError(getErrorMessage(err)),
  })

  const vouchers = batch?.vouchers ?? []
  const totalAmount = vouchers.reduce((sum, v) => sum + v.totalAmount, 0)

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (isError || !batch) {
    return (
      <Card>
        <CardContent className="py-10 text-center">
          <p className="text-sm text-gray-500 dark:text-gray-400">Voucher batch not found.</p>
          <Button variant="outline" size="sm" className="mt-4" asChild>
            <Link to="/ap/voucher-batches">Back to Batches</Link>
          </Button>
        </CardContent>
      </Card>
    )
  }

  const isDraft = batch.status === 0 || batch.status === 'Draft'
  const isBatched = batch.status === 1 || batch.status === 'Batched'
  const isPosted = batch.status === 2 || batch.status === 'Posted'
  const isEditable = isDraft

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/ap/voucher-batches">
            <ArrowLeft className="h-4 w-4" aria-hidden="true" />
            Back
          </Link>
        </Button>
      </div>

      {actionError && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{actionError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title={batch.batchNumber}
          description={batch.description}
          action={
            <div className="flex items-center gap-2">
              <MapStatusBadge value={batch.status} mapping={voucherBatchStatusMap} size="md" />
              {isDraft && (
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => {
                    setActionError(null)
                    setActionDialog('release')
                  }}
                >
                  Release
                </Button>
              )}
              {isBatched && (
                <Button
                  variant="success"
                  size="sm"
                  onClick={() => {
                    setActionError(null)
                    setActionDialog('post')
                  }}
                >
                  Post
                </Button>
              )}
              {isPosted && (
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => {
                    setActionError(null)
                    setIsReverseOpen(true)
                  }}
                >
                  Reverse
                </Button>
              )}
            </div>
          }
        />
        <CardContent>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
            <div>
              <p className="text-gray-500 dark:text-gray-400">Posting Date</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5">{formatDate(batch.postingDate)}</p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Vouchers</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5 tabular-nums">{vouchers.length}</p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Total Amount</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5 font-tabular tabular-nums">
                {formatCurrency(totalAmount)}
              </p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Fiscal Period</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5">{batch.fiscalPeriodId.slice(0, 8)}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader
          title="Vouchers"
          description="Vendor invoices in this batch"
          action={
            isEditable && (
              <Button
                variant="primary"
                size="sm"
                leftIcon={<Plus className="h-4 w-4" />}
                onClick={() => {
                  setActionError(null)
                  setIsAddOpen(true)
                }}
              >
                Add Voucher
              </Button>
            )
          }
        />
        <CardContent>
          {vouchers.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No vouchers in this batch yet. {isEditable ? 'Click "Add Voucher" to create one.' : ''}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Invoice #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Vendor</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Invoice Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Due Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Discount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">1099</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {vouchers.map((voucher: Voucher) => (
                    <tr key={voucher.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{voucher.invoiceNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{vendorNames.get(voucher.vendorId) ?? voucher.vendorId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{voucherTypeMap[String(voucher.voucherType)] ?? voucher.voucherType}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(voucher.invoiceDate)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(voucher.dueDate)}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(voucher.totalAmount)}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-700 dark:text-gray-300">
                        {voucher.discountAmount > 0 ? formatCurrency(voucher.discountAmount) : '—'}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {voucher.is1099Reportable ? 'Yes' : '—'}
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
                    <td colSpan={5} className="px-3 py-2 text-right font-medium text-gray-700 dark:text-gray-300">
                      Totals
                    </td>
                    <td className="px-3 py-2 text-right font-tabular tabular-nums font-semibold text-gray-900 dark:text-white">
                      {formatCurrency(totalAmount)}
                    </td>
                    <td className="px-3 py-2" colSpan={2} />
                  </tr>
                </tfoot>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <AddVoucherModal
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onSave={data => addVoucherMutation.mutate(data)}
        isSaving={addVoucherMutation.isPending}
        error={actionError}
      />

      <ConfirmDialog
        isOpen={actionDialog === 'release'}
        onClose={() => setActionDialog(null)}
        onConfirm={() => releaseMutation.mutate()}
        title="Release Batch"
        message="Release this batch to move it from Draft to Batched status. Vouchers can still be added until it is posted."
        confirmText="Release"
        isLoading={releaseMutation.isPending}
      />

      <ConfirmDialog
        isOpen={actionDialog === 'post'}
        onClose={() => setActionDialog(null)}
        onConfirm={() => postMutation.mutate()}
        title="Post Batch"
        message="Posting requires the batch to be in Batched status. Posted records are immutable and cannot be edited."
        confirmText="Post"
        variant="danger"
        isLoading={postMutation.isPending}
      />

      <Modal
        isOpen={isReverseOpen}
        onClose={() => {
          setIsReverseOpen(false)
          setReverseReason('')
        }}
        title="Reverse Batch"
        description="Reversing creates a new batch with inverted vouchers. The original batch becomes immutable."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={() => setIsReverseOpen(false)} disabled={reverseMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => reverseMutation.mutate()}
              isLoading={reverseMutation.isPending}
              disabled={!reverseReason.trim()}
            >
              Reverse Batch
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input
            label="Reason"
            placeholder="Required — why is this batch being reversed?"
            value={reverseReason}
            onChange={e => setReverseReason(e.target.value)}
            required
          />
        </div>
      </Modal>
    </div>
  )
}
