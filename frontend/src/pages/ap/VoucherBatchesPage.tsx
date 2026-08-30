import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { Plus, AlertCircle, ChevronRight, FileText } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import { getVoucherBatches, createVoucherBatch, getNextVoucherBatchNumber} from '@api/ap'
import { getFiscalPeriods } from '@api/platform'
import { voucherBatchStatusMap } from './statusMaps'

const batchSchema = z.object({
  batchNumber: z.string().trim().min(1, 'Batch number is required'),
  description: z.string().trim().min(1, 'Description is required'),
  postingDate: z.string().min(1, 'Posting date is required'),
  fiscalPeriodId: z.string().min(1, 'Fiscal period is required'),
})

type BatchForm = z.infer<typeof batchSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function VoucherBatchesPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<BatchForm>({
    resolver: zodResolver(batchSchema),
    defaultValues: {
      batchNumber: '',
      description: '',
      postingDate: new Date().toISOString().slice(0, 10),
      fiscalPeriodId: '',
    },
  })

  const { data: batches = [], isLoading } = useQuery({
    queryKey: ['ap', 'voucherBatches'],
    queryFn: () => getVoucherBatches(),
  })

  const { data: periods = [] } = useQuery({
    queryKey: ['platform', 'fiscalPeriods'],
    queryFn: () => getFiscalPeriods(),
  })

  const periodOptions = useMemo(
    () =>
      periods.map(p => ({
        value: p.id,
        label: `P${p.periodNumber} - ${p.description} (${formatDate(p.startDate)})`,
      })),
    [periods]
  )

  const createMutation = useMutation({
    mutationFn: createVoucherBatch,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ap', 'voucherBatches'] })
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = async () => {
    setFormError(null)
    try {
      const next = await getNextVoucherBatchNumber()
      reset({
        batchNumber: next,
        description: '',
        postingDate: new Date().toISOString().slice(0, 10),
        fiscalPeriodId: periods[0]?.id ?? '',
      })
    } catch {
      reset({
        batchNumber: '',
        description: '',
        postingDate: new Date().toISOString().slice(0, 10),
        fiscalPeriodId: periods[0]?.id ?? '',
      })
    }
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: BatchForm) => {
    setFormError(null)
    createMutation.mutate({
      companyId: currentCompanyId(),
      batchNumber: data.batchNumber,
      description: data.description,
      postingDate: new Date(data.postingDate).toISOString(),
      fiscalPeriodId: data.fiscalPeriodId,
    })
  }

  const totalAmount = useMemo(() => batches.reduce((sum, b) => sum + b.vouchers.reduce((s, v) => s + v.totalAmount, 0), 0), [batches])

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
          title="Voucher Batches"
          description={`${batches.length} batch(es) — total ${formatCurrency(totalAmount)}`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Batch
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : batches.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No voucher batches yet. Create a batch to enter vendor invoices.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Batch #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Posting Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Vouchers</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Total</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {batches.map(batch => (
                    <tr
                      key={batch.id}
                      onClick={() => navigate(`/ap/voucher-batches/${batch.id}`)}
                      className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors cursor-pointer"
                    >
                      <td className="px-3 py-3 font-medium text-primary-600 dark:text-primary-400">{batch.batchNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{batch.description}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(batch.postingDate)}</td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={batch.status} mapping={voucherBatchStatusMap} />
                      </td>
                      <td className="px-3 py-3 text-right tabular-nums text-gray-700 dark:text-gray-300">
                        {batch.vouchers.length}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(batch.vouchers.reduce((s, v) => s + v.totalAmount, 0))}
                      </td>
                      <td className="px-3 py-3 text-right">
                        <ChevronRight className="h-4 w-4 inline text-gray-400" aria-hidden="true" />
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
        title="New Voucher Batch"
        description="Create a draft batch and add vendor invoices. The batch must balance before it can be posted."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Create Batch
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input
            {...register('batchNumber')}
            label="Batch Number"
            {...fieldError(errors.batchNumber?.message)}
            required
          />
          <Input
            {...register('description')}
            label="Description"
            placeholder="e.g. July 2026 vendor invoices"
            {...fieldError(errors.description?.message)}
            required
          />
          <Input
            {...register('postingDate')}
            type="date"
            label="Posting Date"
            {...fieldError(errors.postingDate?.message)}
            required
          />
          <Select
            {...register('fiscalPeriodId')}
            label="Fiscal Period"
            placeholder="Select fiscal period..."
            options={periodOptions}
            {...fieldError(errors.fiscalPeriodId?.message)}
            required
          />
          <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
            <FileText className="h-4 w-4" aria-hidden="true" />
            Vouchers are added after the batch is created.
          </div>
        </form>
      </Modal>
    </div>
  )
}
