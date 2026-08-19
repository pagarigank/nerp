import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getSegmentTypes,
  getSegmentValues,
  createSegmentValue,
  updateSegmentValue,
  deleteSegmentValue,
  companyId,
} from '@api/platform'
import type { SegmentType, SegmentValue } from '@/types/platform'

const segmentValueSchema = z.object({
  segmentTypeId: z.string().min(1, 'Segment type is required'),
  value: z.string().trim().min(1, 'Value is required'),
  description: z.string().trim().min(1, 'Description is required'),
  displayOrder: z.coerce.number().int().min(0, 'Display order must be 0 or greater'),
})

type SegmentValueForm = z.infer<typeof segmentValueSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function SegmentValuesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingValue, setEditingValue] = useState<SegmentValue | null>(null)
  const [valueToDelete, setValueToDelete] = useState<SegmentValue | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SegmentValueForm>({
    resolver: zodResolver(segmentValueSchema),
    defaultValues: {
      segmentTypeId: '',
      value: '',
      description: '',
      displayOrder: 0,
    },
  })

  const { data: segmentTypes = [] } = useQuery({
    queryKey: ['platform', 'segmentTypes'],
    queryFn: () => getSegmentTypes(),
  })

  const { data: segmentValues = [], isLoading } = useQuery({
    queryKey: ['platform', 'segmentValues'],
    queryFn: () => getSegmentValues(),
  })

  const typeById = useMemo(() => {
    const map = new Map<string, SegmentType>()
    segmentTypes.forEach(t => map.set(t.id, t))
    return map
  }, [segmentTypes])

  const typeOptions = useMemo(
    () => segmentTypes.map(t => ({ value: t.id, label: `${t.code} - ${t.name}` })),
    [segmentTypes]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform', 'segmentValues'] })
  }

  const createMutation = useMutation({
    mutationFn: createSegmentValue,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { value: string; description: string; displayOrder: number } }) =>
      updateSegmentValue(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteSegmentValue,
    onSuccess: () => {
      invalidate()
      setValueToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingValue(null)
    setFormError(null)
    reset({
      segmentTypeId: typeFilter || (typeOptions[0]?.value ?? ''),
      value: '',
      description: '',
      displayOrder: 0,
    })
    setIsModalOpen(true)
  }

  const openEditForm = (value: SegmentValue) => {
    setEditingValue(value)
    setFormError(null)
    reset({
      segmentTypeId: value.segmentTypeId,
      value: value.value,
      description: value.description,
      displayOrder: value.displayOrder,
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingValue(null)
    setFormError(null)
  }

  const onSubmit = (data: SegmentValueForm) => {
    setFormError(null)
    if (editingValue) {
      updateMutation.mutate({
        id: editingValue.id,
        data: {
          value: data.value,
          description: data.description,
          displayOrder: data.displayOrder,
        },
      })
      return
    }
    createMutation.mutate({
      segmentTypeId: data.segmentTypeId,
      companyId: companyId(),
      value: data.value,
      description: data.description,
      displayOrder: data.displayOrder,
    })
  }

  const filteredValues = useMemo(() => {
    const q = search.trim().toLowerCase()
    return segmentValues.filter(v => {
      if (typeFilter && v.segmentTypeId !== typeFilter) return false
      if (q && !(v.value.toLowerCase().includes(q) || v.description.toLowerCase().includes(q))) return false
      return true
    })
  }, [segmentValues, search, typeFilter])

  const groupedValues = useMemo(() => {
    const sorted = [...filteredValues].sort((a, b) => {
      const ta = typeById.get(a.segmentTypeId)?.displayOrder ?? 0
      const tb = typeById.get(b.segmentTypeId)?.displayOrder ?? 0
      if (ta !== tb) return ta - tb
      return a.displayOrder - b.displayOrder
    })
    const groups: { type: SegmentType; values: SegmentValue[] }[] = []
    for (const value of sorted) {
      const type = typeById.get(value.segmentTypeId)
      if (!type) continue
      const last = groups[groups.length - 1]
      if (last && last.type.id === type.id) {
        last.values.push(value)
      } else {
        groups.push({ type, values: [value] })
      }
    }
    return groups
  }, [filteredValues, typeById])

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
          title="Segment Values"
          description="Valid values that may be used within each chart of accounts segment"
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Segment Value
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 grid grid-cols-1 sm:grid-cols-2 gap-4 max-w-2xl">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by value or description..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search segment values"
            />
            <Select
              value={typeFilter}
              onChange={e => setTypeFilter(e.target.value)}
              options={[{ value: '', label: 'All segment types' }, ...typeOptions]}
              aria-label="Filter by segment type"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : groupedValues.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search || typeFilter
                ? 'No segment values match your filters.'
                : 'No segment values yet. Create your first segment value.'}
            </p>
          ) : (
            <div className="space-y-6">
              {groupedValues.map(group => (
                <div key={group.type.id}>
                  <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-2">
                    {group.type.code} <span className="text-gray-500 dark:text-gray-400">— {group.type.name}</span>
                  </h3>
                  <div className="overflow-x-auto border border-gray-200 dark:border-gray-700 rounded-lg">
                    <table className="w-full text-sm">
                      <thead className="bg-gray-50 dark:bg-gray-800/50">
                        <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                          <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Value</th>
                          <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                          <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Display Order</th>
                          <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                          <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                        {group.values.map(value => (
                          <tr key={value.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                            <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                              {value.value}
                            </td>
                            <td className="px-3 py-3 text-gray-900 dark:text-white">{value.description}</td>
                            <td className="px-3 py-3 text-gray-700 dark:text-gray-300 tabular-nums">{value.displayOrder}</td>
                            <td className="px-3 py-3">
                              <Badge variant={value.isActive ? 'success' : 'neutral'} size="sm" dot>
                                {value.isActive ? 'Active' : 'Inactive'}
                              </Badge>
                            </td>
                            <td className="px-3 py-3">
                              <div className="flex items-center justify-end gap-1">
                                <IconButton
                                  size="sm"
                                  variant="ghost"
                                  aria-label={`Edit ${value.value}`}
                                  onClick={() => openEditForm(value)}
                                >
                                  <Pencil className="h-4 w-4" aria-hidden="true" />
                                </IconButton>
                                <IconButton
                                  size="sm"
                                  variant="ghost"
                                  className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                                  aria-label={`Delete ${value.value}`}
                                  onClick={() => setValueToDelete(value)}
                                >
                                  <Trash2 className="h-4 w-4" aria-hidden="true" />
                                </IconButton>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title={editingValue ? 'Edit Segment Value' : 'New Segment Value'}
        description={editingValue ? `Update ${editingValue.value}` : 'Add a valid value for a chart of accounts segment'}
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending || updateMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={handleSubmit(onSubmit)}
              isLoading={createMutation.isPending || updateMutation.isPending}
            >
              {editingValue ? 'Save Changes' : 'Create Segment Value'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Select
            {...register('segmentTypeId')}
            label="Segment Type"
            options={typeOptions}
            placeholder="Select a segment type"
            {...fieldError(errors.segmentTypeId?.message)}
            disabled={!!editingValue}
            required
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('value')}
              label="Value"
              placeholder="e.g. 10"
              {...fieldError(errors.value?.message)}
              disabled={!!editingValue}
              required
            />
            <Input
              {...register('displayOrder')}
              type="number"
              min="0"
              step="1"
              label="Display Order"
              {...fieldError(errors.displayOrder?.message)}
              required
            />
          </div>
          <Input
            {...register('description')}
            label="Description"
            placeholder="e.g. Engineering"
            {...fieldError(errors.description?.message)}
            required
          />
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!valueToDelete}
        onClose={() => setValueToDelete(null)}
        onConfirm={() => valueToDelete && deleteMutation.mutate(valueToDelete.id)}
        title="Delete Segment Value"
        message={
          valueToDelete
            ? `Are you sure you want to delete segment value "${valueToDelete.value}"? This is a soft delete.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
