import { Fragment, useEffect, useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, Link } from 'react-router-dom'
import { ArrowLeft, Plus, AlertCircle, ChevronDown, ChevronRight, Trash2, Wand2 } from 'lucide-react'
import { formatCurrency, formatDate, generateGuid } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getInvoiceBatch, addInvoiceBatchLines, releaseInvoiceBatch, postInvoiceBatch, getCustomers } from '@api/ar'
import { getAccounts } from '@api/platform'
import type { ArCustomer, ArInvoiceBatchLineItem } from '@/types/ar'
import type { Account } from '@/types/platform'
import { batchStatusMap, invoiceStatusMap } from './statusMaps'
import { ArStatusBadge } from './ArStatusBadge'

interface LineRow {
  accountId: string
  description: string
  quantity: string
  unitPrice: string
  taxAmount: string
  discountAmount: string
}

function blankLine(): LineRow {
  return { accountId: '', description: '', quantity: '1', unitPrice: '', taxAmount: '0', discountAmount: '0' }
}

function lineTotal(row: LineRow): number {
  const qty = Number(row.quantity) || 0
  const price = Number(row.unitPrice) || 0
  const tax = Number(row.taxAmount) || 0
  const discount = Number(row.discountAmount) || 0
  return qty * price + tax - discount
}

interface AddInvoiceModalProps {
  isOpen: boolean
  onClose: () => void
  customers: ArCustomer[]
  onSave: (invoice: ArInvoiceBatchLineItem) => void
  isSaving: boolean
  error: string | null
}

function AddInvoiceModal({ isOpen, onClose, customers, onSave, isSaving, error }: AddInvoiceModalProps) {
  const [customerId, setCustomerId] = useState('')
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [invoiceDate, setInvoiceDate] = useState(new Date().toISOString().slice(0, 10))
  const [dueDate, setDueDate] = useState(
    new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
  )
  const [description, setDescription] = useState('')
  const [rows, setRows] = useState<LineRow[]>([blankLine()])
  const [localError, setLocalError] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen) {
      setCustomerId('')
      setInvoiceNumber('')
      setInvoiceDate(new Date().toISOString().slice(0, 10))
      setDueDate(new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10))
      setDescription('')
      setRows([blankLine()])
      setLocalError(null)
    }
  }, [isOpen])

  const customerOptions = useMemo(
    () =>
      customers.map(c => ({
        value: c.id,
        label: `${c.customerId} - ${c.name}`,
      })),
    [customers]
  )

  const total = useMemo(() => rows.reduce((sum, row) => sum + lineTotal(row), 0), [rows])

  const updateRow = (index: number, patch: Partial<LineRow>) => {
    setRows(prev => prev.map((row, i) => (i === index ? { ...row, ...patch } : row)))
  }

  const addRow = () => setRows(prev => [...prev, blankLine()])

  const removeRow = (index: number) => {
    setRows(prev => (prev.length === 1 ? prev : prev.filter((_, i) => i !== index)))
  }

  const fillRandomAccount = (index: number) => updateRow(index, { accountId: generateGuid() })

  const handleSave = () => {
    if (!customerId) {
      setLocalError('Select a customer for the invoice.')
      return
    }
    if (!invoiceNumber.trim()) {
      setLocalError('Invoice number is required.')
      return
    }
    if (!invoiceDate) {
      setLocalError('Invoice date is required.')
      return
    }
    if (!dueDate) {
      setLocalError('Due date is required.')
      return
    }

    const validLines = rows.filter(
      r => r.accountId.trim() && r.description.trim() && Number(r.quantity) > 0 && Number(r.unitPrice) >= 0
    )
    if (validLines.length === 0) {
      setLocalError('Add at least one line with account, description, quantity, and unit price.')
      return
    }

    const invoice: ArInvoiceBatchLineItem = {
      customerId,
      invoiceNumber: invoiceNumber.trim(),
      invoiceDate: new Date(invoiceDate).toISOString(),
      dueDate: new Date(dueDate).toISOString(),
      lines: validLines.map(r => ({
        accountId: r.accountId.trim(),
        description: r.description.trim(),
        quantity: Number(r.quantity),
        unitPrice: Number(r.unitPrice),
        taxAmount: Number(r.taxAmount) || 0,
        discountAmount: Number(r.discountAmount) || 0,
      })),
    }
    if (description.trim()) invoice.description = description.trim()

    setLocalError(null)
    onSave(invoice)
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Add Invoice"
      description="Add an invoice with line items to this batch."
      size="xl"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleSave} isLoading={isSaving}>
            Add Invoice
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
            label="Customer"
            placeholder="Select customer..."
            options={customerOptions}
            value={customerId}
            onChange={setCustomerId}
            required
          />
          <Input
            label="Invoice Number"
            placeholder="e.g. INV-2026-0001"
            value={invoiceNumber}
            onChange={e => setInvoiceNumber(e.target.value)}
            required
          />
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
          <div className="sm:col-span-2">
            <Input
              label="Description"
              placeholder="Optional invoice description"
              value={description}
              onChange={e => setDescription(e.target.value)}
            />
          </div>
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">Line Items</p>
            <Button variant="outline" size="sm" onClick={addRow} leftIcon={<Plus className="h-4 w-4" />}>
              Add Line
            </Button>
          </div>
          <div className="overflow-x-auto border border-gray-200 dark:border-gray-700 rounded-lg">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 text-left bg-gray-50 dark:bg-gray-900/50">
                  <th className="px-2 py-2 font-medium text-gray-500 dark:text-gray-400 min-w-[220px]">Account</th>
                  <th className="px-2 py-2 font-medium text-gray-500 dark:text-gray-400 min-w-[180px]">Description</th>
                  <th className="px-2 py-2 font-medium text-gray-500 dark:text-gray-400 w-20 text-right">Qty</th>
                  <th className="px-2 py-2 font-medium text-gray-500 dark:text-gray-400 w-28 text-right">Unit Price</th>
                  <th className="px-2 py-2 font-medium text-gray-500 dark:text-gray-400 w-24 text-right">Tax</th>
                  <th className="px-2 py-2 font-medium text-gray-500 dark:text-gray-400 w-24 text-right">Discount</th>
                  <th className="px-2 py-2 font-medium text-gray-500 dark:text-gray-400 w-32 text-right">Line Total</th>
                  <th className="px-2 py-2 w-10" />
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {rows.map((row, index) => (
                  <tr key={index}>
                    <td className="px-2 py-1.5">
                      <select
                        value={row.accountId}
                        onChange={e => updateRow(index, { accountId: e.target.value })}
                        className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1"
                        aria-label={`Line ${index + 1} account`}
                      >
                        <option value="">Select account...</option>
                        {accountOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                      </select>
                    </td>
                    <td className="px-2 py-1.5">
                      <Input
                        value={row.description}
                        onChange={e => updateRow(index, { description: e.target.value })}
                        placeholder="Description"
                        aria-label={`Line ${index + 1} description`}
                      />
                    </td>
                    <td className="px-2 py-1.5">
                      <Input
                        value={row.quantity}
                        type="number"
                        min="0"
                        step="any"
                        onChange={e => updateRow(index, { quantity: e.target.value })}
                        className="text-right tabular-nums"
                        aria-label={`Line ${index + 1} quantity`}
                      />
                    </td>
                    <td className="px-2 py-1.5">
                      <Input
                        value={row.unitPrice}
                        type="number"
                        min="0"
                        step="0.01"
                        onChange={e => updateRow(index, { unitPrice: e.target.value })}
                        className="text-right tabular-nums"
                        aria-label={`Line ${index + 1} unit price`}
                      />
                    </td>
                    <td className="px-2 py-1.5">
                      <Input
                        value={row.taxAmount}
                        type="number"
                        min="0"
                        step="0.01"
                        onChange={e => updateRow(index, { taxAmount: e.target.value })}
                        className="text-right tabular-nums"
                        aria-label={`Line ${index + 1} tax`}
                      />
                    </td>
                    <td className="px-2 py-1.5">
                      <Input
                        value={row.discountAmount}
                        type="number"
                        min="0"
                        step="0.01"
                        onChange={e => updateRow(index, { discountAmount: e.target.value })}
                        className="text-right tabular-nums"
                        aria-label={`Line ${index + 1} discount`}
                      />
                    </td>
                    <td className="px-2 py-1.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                      {formatCurrency(lineTotal(row))}
                    </td>
                    <td className="px-2 py-1.5 text-center">
                      <IconButton
                        size="sm"
                        variant="ghost"
                        className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                        aria-label={`Remove line ${index + 1}`}
                        onClick={() => removeRow(index)}
                        disabled={rows.length === 1}
                      >
                        <Trash2 className="h-4 w-4" aria-hidden="true" />
                      </IconButton>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
                  <td colSpan={6} className="px-3 py-2 text-right font-medium text-gray-700 dark:text-gray-300">
                    Invoice Total
                  </td>
                  <td className="px-3 py-2 text-right font-tabular tabular-nums font-semibold text-gray-900 dark:text-white">
                    {formatCurrency(total)}
                  </td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      </div>
    </Modal>
  )
}

export function InvoiceBatchDetailPage() {
  const { batchId = '' } = useParams()
  const queryClient = useQueryClient()
  const [isAddOpen, setIsAddOpen] = useState(false)
  const [actionDialog, setActionDialog] = useState<'release' | 'post' | null>(null)
  const [expandedInvoice, setExpandedInvoice] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const { data: batch, isLoading, isError } = useQuery({
    queryKey: ['ar', 'invoiceBatch', batchId],
    queryFn: () => getInvoiceBatch(batchId),
    enabled: !!batchId,
  })

  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: getCustomers,
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const accountOptions = useMemo(
    () => accounts.map((a: Account) => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [accounts]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ar', 'invoiceBatch', batchId] })
    queryClient.invalidateQueries({ queryKey: ['ar', 'invoiceBatches'] })
    queryClient.invalidateQueries({ queryKey: ['ar', 'invoices'] })
  }

  const addLinesMutation = useMutation({
    mutationFn: (invoice: ArInvoiceBatchLineItem) => addInvoiceBatchLines(batchId, [invoice]),
    onSuccess: () => {
      invalidate()
      setIsAddOpen(false)
    },
    onError: err => setActionError(getErrorMessage(err)),
  })

  const releaseMutation = useMutation({
    mutationFn: () => releaseInvoiceBatch(batchId),
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
    mutationFn: () => postInvoiceBatch(batchId),
    onSuccess: () => {
      invalidate()
      setActionDialog(null)
    },
    onError: err => {
      setActionDialog(null)
      setActionError(getErrorMessage(err))
    },
  })

  const total = useMemo(() => batch?.invoices.reduce((sum, i) => sum + i.totalAmount, 0) ?? 0, [batch])
  const balanceDue = useMemo(() => batch?.invoices.reduce((sum, i) => sum + i.balanceDue, 0) ?? 0, [batch])

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
          <p className="text-sm text-gray-500 dark:text-gray-400">Invoice batch not found.</p>
          <Button variant="outline" size="sm" className="mt-4" asChild>
            <Link to="/ar/invoice-batches">Back to Batches</Link>
          </Button>
        </CardContent>
      </Card>
    )
  }

  const isDraft = batch.status === 'Draft'
  const isBatched = batch.status === 'Batched'

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/ar/invoice-batches">
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
              <ArStatusBadge value={batch.status} mapping={batchStatusMap} size="md" />
              {(isDraft || isBatched) && (
                <>
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
                </>
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
              <p className="text-gray-500 dark:text-gray-400">Invoices</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5 tabular-nums">{batch.invoices.length}</p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Total</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5 font-tabular tabular-nums">
                {formatCurrency(total)}
              </p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Balance Due</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5 font-tabular tabular-nums">
                {formatCurrency(balanceDue)}
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader
          title="Invoices"
          description="Invoices in this batch"
          action={
            isDraft && (
              <Button
                variant="primary"
                size="sm"
                leftIcon={<Plus className="h-4 w-4" />}
                onClick={() => {
                  setActionError(null)
                  setIsAddOpen(true)
                }}
              >
                Add Invoice
              </Button>
            )
          }
        />
        <CardContent>
          {batch.invoices.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No invoices in this batch yet. {isDraft ? 'Click "Add Invoice" to create one.' : ''}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 w-8" />
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Invoice #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Due</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Total</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Balance Due</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {batch.invoices.map(invoice => {
                    const isExpanded = expandedInvoice === invoice.id
                    return (
                      <Fragment key={invoice.id}>
                        <tr
                          key={invoice.id}
                          onClick={() => setExpandedInvoice(isExpanded ? null : invoice.id)}
                          className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors cursor-pointer"
                        >
                          <td className="px-3 py-3">
                            {isExpanded ? (
                              <ChevronDown className="h-4 w-4 text-gray-400" aria-hidden="true" />
                            ) : (
                              <ChevronRight className="h-4 w-4 text-gray-400" aria-hidden="true" />
                            )}
                          </td>
                          <td className="px-3 py-3 font-medium text-primary-600 dark:text-primary-400">
                            {invoice.invoiceNumber}
                          </td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{invoice.customerName}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(invoice.invoiceDate)}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(invoice.dueDate)}</td>
                          <td className="px-3 py-3">
                            <ArStatusBadge value={invoice.status} mapping={invoiceStatusMap} />
                          </td>
                          <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                            {formatCurrency(invoice.totalAmount)}
                          </td>
                          <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                            {formatCurrency(invoice.balanceDue)}
                          </td>
                        </tr>
                        {isExpanded && (
                          <tr key={`${invoice.id}-lines`}>
                            <td colSpan={8} className="px-3 pb-4 pt-1 bg-gray-50/50 dark:bg-gray-900/30">
                              <div className="ml-8 overflow-x-auto">
                                <table className="w-full text-sm">
                                  <thead>
                                    <tr className="text-left">
                                      <th className="px-3 py-1.5 font-medium text-gray-500 dark:text-gray-400">Account</th>
                                      <th className="px-3 py-1.5 font-medium text-gray-500 dark:text-gray-400">Description</th>
                                      <th className="px-3 py-1.5 font-medium text-gray-500 dark:text-gray-400 text-right">Qty</th>
                                      <th className="px-3 py-1.5 font-medium text-gray-500 dark:text-gray-400 text-right">Unit Price</th>
                                      <th className="px-3 py-1.5 font-medium text-gray-500 dark:text-gray-400 text-right">Tax</th>
                                      <th className="px-3 py-1.5 font-medium text-gray-500 dark:text-gray-400 text-right">Total</th>
                                    </tr>
                                  </thead>
                                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/40">
                                    {invoice.lines.map((line, index) => (
                                      <tr key={index}>
                                        <td className="px-3 py-1.5 font-mono text-xs text-gray-600 dark:text-gray-400">
                                          {line.accountId}
                                        </td>
                                        <td className="px-3 py-1.5 text-gray-700 dark:text-gray-300">{line.description}</td>
                                        <td className="px-3 py-1.5 text-right tabular-nums text-gray-700 dark:text-gray-300">
                                          {line.quantity}
                                        </td>
                                        <td className="px-3 py-1.5 text-right font-tabular tabular-nums text-gray-700 dark:text-gray-300">
                                          {formatCurrency(line.unitPrice)}
                                        </td>
                                        <td className="px-3 py-1.5 text-right font-tabular tabular-nums text-gray-700 dark:text-gray-300">
                                          {formatCurrency(line.taxAmount)}
                                        </td>
                                        <td className="px-3 py-1.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                                          {formatCurrency(line.totalAmount)}
                                        </td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              </div>
                            </td>
                          </tr>
                        )}
                      </Fragment>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <AddInvoiceModal
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        customers={customers}
        onSave={invoice => addLinesMutation.mutate(invoice)}
        isSaving={addLinesMutation.isPending}
        error={actionError}
      />

      <ConfirmDialog
        isOpen={actionDialog === 'release'}
        onClose={() => setActionDialog(null)}
        onConfirm={() => releaseMutation.mutate()}
        title="Release Batch"
        message="Release this batch to move it from Draft to Batched status. Invoices can still be added until it is posted."
        confirmText="Release"
        isLoading={releaseMutation.isPending}
      />

      <ConfirmDialog
        isOpen={actionDialog === 'post'}
        onClose={() => setActionDialog(null)}
        onConfirm={() => postMutation.mutate()}
        title="Post Batch"
        message="Posting makes these invoices effective. Posted records are immutable and cannot be edited."
        confirmText="Post"
        variant="danger"
        isLoading={postMutation.isPending}
      />
    </div>
  )
}
