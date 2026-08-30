import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, Inbox, Trash2 } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import {
  getLockboxBatches,
  createLockboxBatch,
  postLockboxBatch,
  getBankAccounts,
  } from '@api/cash'
import type { LockboxBatch } from '@/types/cash'

const batchSchema = z.object({
  batchNumber: z.string().min(1, 'Batch number is required'),
  items: z.array(z.object({
    referenceNumber: z.string().min(1, 'Reference is required'),
    customerName: z.string().optional(),
    amount: z.coerce.number().positive('Amount must be greater than zero'),
    invoiceNumber: z.string().optional(),
  })).min(1, 'At least one item is required'),
})

type BatchForm = z.infer<typeof batchSchema>

const lockboxStatusMap: Record<string, { variant: string; label: string }> = {
  Imported: { variant: 'neutral', label: 'Imported' },
  Posted: { variant: 'success', label: 'Posted' },
}

export function LockboxPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors },
  } = useForm<BatchForm>({
    resolver: zodResolver(batchSchema),
    defaultValues: {
      batchNumber: `LBX-${new Date().toISOString().slice(0, 10)}`,
      items: [{ referenceNumber: '', customerName: '', amount: 0, invoiceNumber: '' }],
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'items' })

  const { data: batches = [], isLoading } = useQuery({
    queryKey: ['cash', 'lockbox-batches'],
    queryFn: () => getLockboxBatches(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'lockbox-batches'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: BatchForm) =>
      createLockboxBatch({
        companyId: currentCompanyId(),
        batchNumber: data.batchNumber,
        fileName: `${data.batchNumber}.csv`,
        format: 'CSV',
        items: data.items.map(i => ({
          referenceNumber: i.referenceNumber,
          customerName: i.customerName || '',
          amount: i.amount,
          invoiceNumber: i.invoiceNumber || '',
        })),
      }),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const postMutation = useMutation({
    mutationFn: postLockboxBatch,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = () => {
    setFormError(null)
    reset({
      batchNumber: `LBX-${new Date().toISOString().slice(0, 10)}`,
      items: [{ referenceNumber: '', customerName: '', amount: 0, invoiceNumber: '' }],
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: BatchForm) => {
    setFormError(null)
    createMutation.mutate(data)
  }

  const lineTotal = useMemo(
    () => (fields.reduce((sum, _f, i) => sum + (Number(0) || 0), 0)),
    [fields]
  )

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
          title="Lockbox / Remote Deposit Capture"
          description={`${batches.length} batch(es) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Inbox className="h-4 w-4" />}>
              Import Batch
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : batches.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No lockbox batches imported yet. Import a batch to process remote deposits.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Batch</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Items</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Total</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Imported</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {batches.map((b: LockboxBatch) => (
                    <tr key={b.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                        {b.batchNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{b.totalItems}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(b.totalAmount)}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={b.status} mapping={lockboxStatusMap} />
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(b.importedOn)}</td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end">
                          <Button
                            variant="outline"
                            size="sm"
                            disabled={b.status === 'Posted' || postMutation.isPending}
                            onClick={() => postMutation.mutate(b.id)}
                            isLoading={postMutation.isPending && postMutation.variables === b.id}
                          >
                            Post
                          </Button>
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
        title="Import Lockbox Batch"
        description="Enter lockbox items from a remote deposit capture file."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Import
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          {formError && (
            <div
              className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm"
              role="alert"
            >
              <AlertCircle className="h-4 w-4 flex-shrink-0" />
              <span>{formError}</span>
            </div>
          )}

          <Input
            {...register('batchNumber')}
            label="Batch Number"
            placeholder="e.g. LBX-2026-001"
            required
          />

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-medium text-gray-900 dark:text-white">Lockbox Items</h4>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => append({ referenceNumber: '', customerName: '', amount: 0, invoiceNumber: '' })}
                leftIcon={<Plus className="h-4 w-4" />}
              >
                Add Item
              </Button>
            </div>
            {fields.map((field, index) => (
              <div key={field.id} className="flex flex-col sm:flex-row gap-3 items-end">
                <Input
                  {...register(`items.${index}.referenceNumber`)}
                  className="sm:w-36"
                  placeholder="Reference #"
                  required
                />
                <Input
                  {...register(`items.${index}.customerName`)}
                  className="sm:flex-1"
                  placeholder="Customer name"
                />
                <Input
                  {...register(`items.${index}.amount`)}
                  className="sm:w-32"
                  type="number"
                  step="0.01"
                  min="0.01"
                  placeholder="Amount"
                  required
                />
                <Input
                  {...register(`items.${index}.invoiceNumber`)}
                  className="sm:w-32"
                  placeholder="Invoice #"
                />
                <IconButton
                  type="button"
                  size="sm"
                  variant="ghost"
                  className="text-red-600"
                  aria-label="Remove item"
                  onClick={() => fields.length > 1 && remove(index)}
                  disabled={fields.length <= 1}
                >
                  <Trash2 className="h-4 w-4" />
                </IconButton>
              </div>
            ))}
          </div>
        </form>
      </Modal>
    </div>
  )
}
