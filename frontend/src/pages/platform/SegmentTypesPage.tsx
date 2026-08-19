import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Checkbox } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getSegmentTypes, createSegmentType, updateSegmentType, deleteSegmentType, companyId } from '@api/platform'
import type { SegmentType } from '@/types/platform'

const segmentTypeSchema = z.object({
  code: z.string().trim().min(1, 'Code is required'),
  name: z.string().trim().min(1, 'Name is required'),
  displayOrder: z.coerce.number().int().min(0, 'Display order must be 0 or greater'),
  isRequired: z.boolean(),
})

type SegmentTypeForm = z.infer<typeof segmentTypeSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function SegmentTypesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingType, setEditingType] = useState<SegmentType | null>(null)
  const [typeToDelete, setTypeToDelete] = useState<SegmentType | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SegmentTypeForm>({
    resolver: zodResolver(segmentTypeSchema),
    defaultValues: {
      code: '',
      name: '',
      displayOrder: 0,
      isRequired: false,
    },
  })

  const { data: segmentTypes = [], isLoading } = useQuery({
    queryKey: ['platform', 'segmentTypes'],
    queryFn: () => getSegmentTypes(),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform', 'segmentTypes'] })
  }

  const createMutation = useMutation({
    mutationFn: createSegmentType,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { name: string; code: string; displayOrder: number; isRequired: boolean } }) =>
      updateSegmentType(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteSegmentType,
    onSuccess: () => {
      invalidate()
      setTypeToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingType(null)
    setFormError(null)
    reset({ code: '', name: '', displayOrder: segmentTypes.length, isRequired: false })
    setIsModalOpen(true)
  }

  const openEditForm = (type: SegmentType) => {
    setEditingType(type)
    setFormError(null)
    reset({
      code: type.code,
      name: type.name,
      displayOrder: type.displayOrder,
      isRequired: type.isRequired,
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingType(null)
    setFormError(null)
  }

  const onSubmit = (data: SegmentTypeForm) => {
    setFormError(null)
    if (editingType) {
      updateMutation.mutate({
        id: editingType.id,
        data: {
          name: data.name,
          code: data.code,
          displayOrder: data.displayOrder,
          isRequired: data.isRequired,
        },
      })
      return
    }
    createMutation.mutate({
      companyId: companyId(),
      name: data.name,
      code: data.code,
      displayOrder: data.displayOrder,
      isRequired: data.isRequired,
    })
  }

  const filteredTypes = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return segmentTypes
    return segmentTypes.filter(
      t => t.name.toLowerCase().includes(q) || t.code.toLowerCase().includes(q)
    )
  }, [segmentTypes, search])

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
          title="Segment Types"
          description="Chart of accounts segment structure (Account, Department, Project, Cost Center, ...)"
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Segment Type
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by name or code..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search segment types"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : filteredTypes.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No segment types match your search.' : 'No segment types yet. Create your first segment type.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Display Order</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Required</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredTypes.map(type => (
                    <tr key={type.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                        {type.code}
                      </td>
                      <td className="px-3 py-3 text-gray-900 dark:text-white">{type.name}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300 tabular-nums">{type.displayOrder}</td>
                      <td className="px-3 py-3">
                        <Badge variant={type.isRequired ? 'info' : 'neutral'} size="sm">
                          {type.isRequired ? 'Required' : 'Optional'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={type.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {type.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${type.name}`}
                            onClick={() => openEditForm(type)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Delete ${type.name}`}
                            onClick={() => setTypeToDelete(type)}
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
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title={editingType ? 'Edit Segment Type' : 'New Segment Type'}
        description={editingType ? `Update ${editingType.name}` : 'Add a new chart of accounts segment type'}
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
              {editingType ? 'Save Changes' : 'Create Segment Type'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('code')}
              label="Code"
              placeholder="e.g. DEPT"
              {...fieldError(errors.code?.message)}
              disabled={!!editingType}
              required
            />
            <Input
              {...register('name')}
              label="Name"
              placeholder="e.g. Department"
              {...fieldError(errors.name?.message)}
              required
            />
          </div>
          <Input
            {...register('displayOrder')}
            type="number"
            min="0"
            step="1"
            label="Display Order"
            hint="Segments are ordered low to high when building account strings"
            {...fieldError(errors.displayOrder?.message)}
            required
          />
          <Checkbox
            {...register('isRequired')}
            label="Required segment"
            description="Every account string must include a value for this segment"
          />
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!typeToDelete}
        onClose={() => setTypeToDelete(null)}
        onConfirm={() => typeToDelete && deleteMutation.mutate(typeToDelete.id)}
        title="Delete Segment Type"
        message={
          typeToDelete
            ? `Are you sure you want to delete segment type "${typeToDelete.name}"? This is a soft delete.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
