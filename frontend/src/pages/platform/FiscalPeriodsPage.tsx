import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import {
  getFiscalPeriods,
  getFiscalYears,
  createFiscalPeriod,
  closeFiscalPeriod,
  openFiscalPeriod,
  companyId,
} from '@api/platform'
import type { FiscalPeriod } from '@/types/platform'
import { periodStatusMap } from './statusMaps'

const periodSchema = z.object({
  fiscalYearId: z.string().min(1, 'Fiscal year is required'),
  periodNumber: z.coerce.number().int().min(1, 'Period number must be 1 or greater').max(13, 'Max 13 periods'),
  description: z.string().trim().min(1, 'Description is required'),
  startDate: z.string().min(1, 'Start date is required'),
  endDate: z.string().min(1, 'End date is required'),
})

type PeriodForm = z.infer<typeof periodSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function FiscalPeriodsPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [periodAction, setPeriodAction] = useState<{ period: FiscalPeriod; action: 'close' | 'open' } | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PeriodForm>({
    resolver: zodResolver(periodSchema),
    defaultValues: {
      fiscalYearId: '',
      periodNumber: 1,
      description: '',
      startDate: '',
      endDate: '',
    },
  })

  const { data: periods = [], isLoading } = useQuery({
    queryKey: ['platform', 'fiscalPeriods'],
    queryFn: () => getFiscalPeriods(),
  })

  const { data: fiscalYears = [] } = useQuery({
    queryKey: ['platform', 'fiscalYears'],
    queryFn: () => getFiscalYears(),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform', 'fiscalPeriods'] })
  }

  const createMutation = useMutation({
    mutationFn: createFiscalPeriod,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const closeMutation = useMutation({
    mutationFn: closeFiscalPeriod,
    onSuccess: () => {
      invalidate()
      setPeriodAction(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openMutation = useMutation({
    mutationFn: openFiscalPeriod,
    onSuccess: () => {
      invalidate()
      setPeriodAction(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const fiscalYearOptions = useMemo(
    () =>
      fiscalYears.map(y => ({
        value: y.id,
        label: `${y.year} - ${y.description}`,
      })),
    [fiscalYears]
  )

  const openForm = () => {
    setFormError(null)
    reset({
      fiscalYearId: fiscalYearOptions[0]?.value ?? '',
      periodNumber: periods.length + 1,
      description: '',
      startDate: '',
      endDate: '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: PeriodForm) => {
    setFormError(null)
    createMutation.mutate({
      fiscalYearId: data.fiscalYearId,
      companyId: companyId(),
      periodNumber: data.periodNumber,
      description: data.description,
      startDate: new Date(data.startDate).toISOString(),
      endDate: new Date(data.endDate).toISOString(),
    })
  }

  const sortedPeriods = useMemo(
    () => [...periods].sort((a, b) => b.periodNumber - a.periodNumber),
    [periods]
  )

  const openCount = periods.filter(p => p.status === 0).length
  const closedCount = periods.filter(p => p.status === 1).length

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
          title="Fiscal Periods"
          description={`${periods.length} period(s) — ${openCount} open, ${closedCount} closed`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Period
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : sortedPeriods.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No fiscal periods yet. Create a period to begin posting transactions.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Period</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Start</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">End</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {sortedPeriods.map(period => (
                    <tr key={period.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white tabular-nums">
                        {period.periodNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{period.description}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {new Date(period.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {new Date(period.endDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={period.status} mapping={periodStatusMap} />
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-2">
                          {period.status === 0 && (
                            <Button
                              variant="secondary"
                              size="sm"
                              onClick={() => {
                                setFormError(null)
                                setPeriodAction({ period, action: 'close' })
                              }}
                            >
                              Close
                            </Button>
                          )}
                          {period.status === 1 && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => {
                                setFormError(null)
                                setPeriodAction({ period, action: 'open' })
                              }}
                            >
                              Reopen
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
        title="New Fiscal Period"
        description="Add a fiscal period to the selected company's fiscal year."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Create Period
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Select
            {...register('fiscalYearId')}
            label="Fiscal Year"
            options={fiscalYearOptions}
            placeholder="Select a fiscal year"
            {...fieldError(errors.fiscalYearId?.message)}
            required
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('periodNumber')}
              type="number"
              min="1"
              max="13"
              step="1"
              label="Period Number"
              {...fieldError(errors.periodNumber?.message)}
              required
            />
            <Input
              {...register('description')}
              label="Description"
              placeholder="e.g. July 2026"
              {...fieldError(errors.description?.message)}
              required
            />
            <Input
              {...register('startDate')}
              type="date"
              label="Start Date"
              {...fieldError(errors.startDate?.message)}
              required
            />
            <Input
              {...register('endDate')}
              type="date"
              label="End Date"
              {...fieldError(errors.endDate?.message)}
              required
            />
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!periodAction}
        onClose={() => setPeriodAction(null)}
        onConfirm={() =>
          periodAction?.action === 'close'
            ? closeMutation.mutate(periodAction.period.id)
            : openMutation.mutate(periodAction?.period.id ?? '')
        }
        title={periodAction?.action === 'close' ? 'Close Period' : 'Reopen Period'}
        message={
          periodAction?.action === 'close'
            ? `Close period ${periodAction.period.periodNumber}? Once closed, no further postings can be made to it.`
            : `Reopen period ${periodAction?.period.periodNumber}? This allows new postings to the period.`
        }
        confirmText={periodAction?.action === 'close' ? 'Close Period' : 'Reopen'}
        variant={periodAction?.action === 'close' ? 'danger' : 'primary'}
        isLoading={closeMutation.isPending || openMutation.isPending}
      />
    </div>
  )
}
