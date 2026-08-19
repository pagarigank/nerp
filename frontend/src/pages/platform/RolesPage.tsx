import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Textarea } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getRoles, createRole, updateRole, deleteRole } from '@api/platform'
import type { Role } from '@/types/platform'

const roleSchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  description: z.string().trim().min(1, 'Description is required'),
})

type RoleForm = z.infer<typeof roleSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function RolesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingRole, setEditingRole] = useState<Role | null>(null)
  const [roleToDelete, setRoleToDelete] = useState<Role | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<RoleForm>({
    resolver: zodResolver(roleSchema),
    defaultValues: { name: '', description: '' },
  })

  const { data: roles = [], isLoading } = useQuery({
    queryKey: ['platform', 'roles'],
    queryFn: getRoles,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform', 'roles'] })
  }

  const createMutation = useMutation({
    mutationFn: createRole,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { name: string; description: string } }) =>
      updateRole(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteRole,
    onSuccess: () => {
      invalidate()
      setRoleToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingRole(null)
    setFormError(null)
    reset({ name: '', description: '' })
    setIsModalOpen(true)
  }

  const openEditForm = (role: Role) => {
    setEditingRole(role)
    setFormError(null)
    reset({ name: role.name, description: role.description })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingRole(null)
    setFormError(null)
  }

  const onSubmit = (data: RoleForm) => {
    setFormError(null)
    if (editingRole) {
      updateMutation.mutate({ id: editingRole.id, data })
      return
    }
    createMutation.mutate(data)
  }

  const filteredRoles = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return roles
    return roles.filter(
      r => r.name.toLowerCase().includes(q) || r.description.toLowerCase().includes(q)
    )
  }, [roles, search])

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
          title="Roles"
          description="Security roles controlling module and action access"
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Role
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by name or description..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search roles"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={4} />
          ) : filteredRoles.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No roles match your search.' : 'No roles yet. Create your first role.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredRoles.map(role => (
                    <tr key={role.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{role.name}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{role.description}</td>
                      <td className="px-3 py-3">
                        <Badge variant={role.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {role.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${role.name}`}
                            onClick={() => openEditForm(role)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Delete ${role.name}`}
                            onClick={() => setRoleToDelete(role)}
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
        title={editingRole ? 'Edit Role' : 'New Role'}
        description={editingRole ? `Update ${editingRole.name}` : 'Add a new security role'}
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
              {editingRole ? 'Save Changes' : 'Create Role'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input
            {...register('name')}
            label="Name"
            placeholder="e.g. AP Clerk"
            {...fieldError(errors.name?.message)}
            required
          />
          <Textarea
            {...register('description')}
            label="Description"
            placeholder="What this role is allowed to do"
            {...fieldError(errors.description?.message)}
            required
          />
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!roleToDelete}
        onClose={() => setRoleToDelete(null)}
        onConfirm={() => roleToDelete && deleteMutation.mutate(roleToDelete.id)}
        title="Delete Role"
        message={
          roleToDelete
            ? `Are you sure you want to delete role "${roleToDelete.name}"? This is a soft delete.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
