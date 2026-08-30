import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle, ShieldCheck, Copy } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Textarea, Select } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getRoles,
  createRole,
  updateRole,
  deleteRole,
  getPermissionCatalog,
  getAllPermissions,
  getRoleMatrix,
  setRolePermissions,
  cloneRole,
  grantAllRolePermissions,
} from '@api/platform'
import { usePagePermission } from '@hooks/usePagePermission'
import type { Role, CatalogModule, PermissionRef } from '@/types/platform'

const roleSchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  description: z.string().trim().min(1, 'Description is required'),
})

type RoleForm = z.infer<typeof roleSchema>

const ACTIONS = ['view', 'create', 'edit', 'delete'] as const
type Action = (typeof ACTIONS)[number]
const ACTION_LABELS: Record<Action, string> = { view: 'View', create: 'Create', edit: 'Edit', delete: 'Delete' }

// Build the stable permission code the backend uses: "{module}.{page}.{action}".
function codeFor(module: string, page: string, action: Action): string {
  return `${module}.${page}.${action}`
}

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

  // Permission selection state for the matrix editor, keyed by code.
  const [selected, setSelected] = useState<Set<string>>(new Set())

  // Phase 4: module filter for the matrix editor.
  const [moduleFilter, setModuleFilter] = useState<string>('all')

  // Page-level RBAC for this screen (demonstrates usePagePermission in a real page).
  const { canCreate: canCreateRole, canEdit: canEditRole, canDelete: canDeleteRole, canView: canViewRoles } =
    usePagePermission('platform', 'roles')

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

  // Catalog (module -> pages -> actions) + the full permission id map (code -> id).
  const { data: catalog = [] } = useQuery({
    queryKey: ['platform', 'permission-catalog'],
    queryFn: getPermissionCatalog,
    enabled: isModalOpen,
  })
  const { data: allPermissions = [] } = useQuery({
    queryKey: ['platform', 'permissions'],
    queryFn: getAllPermissions,
    enabled: isModalOpen,
    staleTime: 5 * 60 * 1000,
  })

  const codeToId = useMemo(() => {
    const map = new Map<string, string>()
    for (const p of allPermissions as PermissionRef[]) map.set(p.code, p.id)
    return map
  }, [allPermissions])

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

  const savePermsMutation = useMutation({
    mutationFn: ({ id, ids }: { id: string; ids: string[] }) => setRolePermissions(id, ids),
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

  const cloneMutation = useMutation({
    mutationFn: ({ id, name }: { id: string; name?: string }) => cloneRole(id, name),
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const grantAllMutation = useMutation({
    mutationFn: (id: string) => grantAllRolePermissions(id),
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingRole(null)
    setFormError(null)
    setSelected(new Set())
    reset({ name: '', description: '' })
    setIsModalOpen(true)
  }

  const openEditForm = (role: Role) => {
    setEditingRole(role)
    setFormError(null)
    reset({ name: role.name, description: role.description })
    setIsModalOpen(true)
    // Load the role's current grants into the matrix.
    getRoleMatrix(role.id)
      .then(matrix => {
        const next = new Set<string>()
        for (const mod of matrix.modules) {
          for (const page of mod.pages) {
            if (page.view) next.add(codeFor(mod.module, page.page, 'view'))
            if (page.create) next.add(codeFor(mod.module, page.page, 'create'))
            if (page.edit) next.add(codeFor(mod.module, page.page, 'edit'))
            if (page.delete) next.add(codeFor(mod.module, page.page, 'delete'))
          }
        }
        setSelected(next)
      })
      .catch(() => setSelected(new Set()))
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingRole(null)
    setFormError(null)
    setSelected(new Set())
  }

  const toggle = (code: string) => {
    setSelected(prev => {
      const next = new Set(prev)
      if (next.has(code)) next.delete(code)
      else next.add(code)
      return next
    })
  }

  // Phase 4 bulk helpers operate on codes in the current selection.
  const selectModule = (module: string, value: boolean) => {
    setSelected(prev => {
      const next = new Set(prev)
      for (const mod of catalog as CatalogModule[]) {
        if (mod.module !== module) continue
        for (const page of mod.pages) {
          for (const a of ACTIONS) {
            const code = codeFor(mod.module, page.page, a)
            if (value) next.add(code)
            else next.delete(code)
          }
        }
      }
      return next
    })
  }

  const selectAll = (value: boolean) => {
    setSelected(prev => {
      const next = new Set(prev)
      for (const mod of catalog as CatalogModule[]) {
        for (const page of mod.pages) {
          for (const a of ACTIONS) next.add(codeFor(mod.module, page.page, a))
        }
      }
      if (!value) next.clear()
      return next
    })
  }

  const requestClone = (role: Role) => {
    cloneMutation.mutate({ id: role.id })
  }

  const onSubmit = (data: RoleForm) => {
    setFormError(null)
    const ids = Array.from(selected)
      .map(code => codeToId.get(code))
      .filter((id): id is string => !!id)

    const afterSaved = (roleId: string) => {
      savePermsMutation.mutate(
        { id: roleId, ids },
        {
          onSuccess: () => {
            invalidate()
            closeForm()
          },
        },
      )
    }

    if (editingRole) {
      updateMutation.mutate(
        { id: editingRole.id, data },
        { onSuccess: (r) => afterSaved(r.id) },
      )
      return
    }
    createMutation.mutate(data, { onSuccess: (r) => afterSaved(r.id) })
  }

  const filteredRoles = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return roles
    return roles.filter(
      r => r.name.toLowerCase().includes(q) || r.description.toLowerCase().includes(q),
    )
  }, [roles, search])

  const saving =
    createMutation.isPending ||
    updateMutation.isPending ||
    savePermsMutation.isPending ||
    cloneMutation.isPending ||
    grantAllMutation.isPending

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
            canCreateRole ? (
              <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
                New Role
              </Button>
            ) : null
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
                          {canEditRole && (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              aria-label={`Edit ${role.name}`}
                              onClick={() => openEditForm(role)}
                            >
                              <Pencil className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
                          )}
                          {canCreateRole && (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              aria-label={`Clone ${role.name}`}
                              onClick={() => requestClone(role)}
                              isLoading={cloneMutation.isPending && cloneMutation.variables?.id === role.id}
                            >
                              <Copy className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
                          )}
                          {canDeleteRole && (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                              aria-label={`Delete ${role.name}`}
                              onClick={() => setRoleToDelete(role)}
                            >
                              <Trash2 className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
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
        title={editingRole ? 'Edit Role' : 'New Role'}
        description={editingRole ? `Update ${editingRole.name}` : 'Add a new security role'}
        size="xl"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={saving}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={saving}>
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

          <div className="pt-2">
            <div className="flex items-center gap-2 mb-2">
              <ShieldCheck className="h-4 w-4 text-indigo-500" aria-hidden="true" />
              <h3 className="text-sm font-semibold text-gray-900 dark:text-white">
                Page Permissions
              </h3>
              <span className="text-xs text-gray-400">View · Create · Edit · Delete per page</span>
            </div>

            {catalog.length === 0 ? (
              <p className="text-sm text-gray-500">Loading pages…</p>
            ) : (
              <>
                {/* Phase 4: bulk helpers + module filter */}
                <div className="flex flex-wrap items-center gap-2 mb-3">
                  <Select
                    value={moduleFilter}
                    onChange={e => setModuleFilter(e.target.value)}
                    aria-label="Filter matrix by module"
                    className="w-44"
                  >
                    <option value="all">All modules</option>
                    {(catalog as CatalogModule[]).map(mod => (
                      <option key={mod.module} value={mod.module}>
                        {mod.label}
                      </option>
                    ))}
                  </Select>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => selectAll(true)}
                    disabled={saving}
                  >
                    Grant all
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => selectAll(false)}
                    disabled={saving}
                  >
                    Clear all
                  </Button>
                  {moduleFilter !== 'all' && (
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => selectModule(moduleFilter, true)}
                      disabled={saving}
                    >
                      Grant module
                    </Button>
                  )}
                  <span className="text-xs text-gray-400 ml-auto">
                    {selected.size} selected
                  </span>
                </div>

                <div className="max-h-[50vh] overflow-y-auto border border-gray-200 dark:border-gray-700 rounded-lg divide-y divide-gray-100 dark:divide-gray-700/60">
                  {(catalog as CatalogModule[])
                    .filter(mod => moduleFilter === 'all' || mod.module === moduleFilter)
                    .map(mod => (
                    <div key={mod.module} className="p-3">
                      <div className="flex items-center justify-between mb-2">
                        <div className="text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                          {mod.label}
                        </div>
                        {moduleFilter === 'all' && (
                          <div className="flex items-center gap-2">
                            <Button variant="ghost" size="sm" onClick={() => selectModule(mod.module, true)} disabled={saving}>
                              grant
                            </Button>
                            <Button variant="ghost" size="sm" onClick={() => selectModule(mod.module, false)} disabled={saving}>
                              none
                            </Button>
                          </div>
                        )}
                      </div>
                      <table className="w-full text-sm">
                        <thead>
                          <tr className="text-left text-gray-400">
                            <th className="py-1 font-medium">Page</th>
                            {ACTIONS.map(a => (
                              <th key={a} className="py-1 font-medium text-center w-16">{ACTION_LABELS[a]}</th>
                            ))}
                          </tr>
                        </thead>
                        <tbody>
                          {mod.pages.map(page => (
                            <tr key={page.page} className="border-t border-gray-100 dark:border-gray-700/40">
                              <td className="py-1 pr-2 text-gray-700 dark:text-gray-300">{page.label}</td>
                              {ACTIONS.map(a => {
                                const code = codeFor(mod.module, page.page, a)
                                return (
                                  <td key={a} className="py-1 text-center">
                                    <input
                                      type="checkbox"
                                      className="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                                      checked={selected.has(code)}
                                      onChange={() => toggle(code)}
                                      aria-label={`${page.label} - ${ACTION_LABELS[a]}`}
                                    />
                                  </td>
                                )
                              })}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
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
