import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Power, PowerOff, AlertCircle, ShieldCheck } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getUsers,
  createUser,
  updateUser,
  activateUser,
  deactivateUser,
  getRoles,
  getCompanies,
  assignUserRole,
  removeUserRole,
} from '@api/platform'
import type { PlatformUser, Role, Company, UpdatePlatformUserRequest } from '@/types/platform'

const userSchema = z.object({
  username: z.string().trim().min(1, 'Username is required'),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address'),
  displayName: z.string().trim().min(1, 'Display name is required'),
  phoneNumber: z.string().optional(),
  password: z.string().optional()
    .refine(v => !v || v.length >= 8, 'Password must be at least 8 characters'),
})

type UserForm = z.infer<typeof userSchema>

const defaultValues: UserForm = {
  username: '',
  email: '',
  displayName: '',
  phoneNumber: '',
  password: '',
}

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function UsersPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<PlatformUser | null>(null)
  const [statusAction, setStatusAction] = useState<{ user: PlatformUser; action: 'activate' | 'deactivate' } | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UserForm>({
    resolver: zodResolver(userSchema),
    defaultValues,
  })

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['platform', 'users'],
    queryFn: getUsers,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform', 'users'] })
  }

  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePlatformUserRequest }) =>
      updateUser(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const activateMutation = useMutation({
    mutationFn: activateUser,
    onSuccess: () => {
      invalidate()
      setStatusAction(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deactivateMutation = useMutation({
    mutationFn: deactivateUser,
    onSuccess: () => {
      invalidate()
      setStatusAction(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  // --- Role management ---
  const [roleTarget, setRoleTarget] = useState<PlatformUser | null>(null)
  const { data: roles = [] } = useQuery({ queryKey: ['platform', 'roles'], queryFn: getRoles })
  const { data: companies = [] } = useQuery({ queryKey: ['platform', 'companies'], queryFn: getCompanies })
  const [roleFormError, setRoleFormError] = useState<string | null>(null)

  const assignRoleMutation = useMutation({
    mutationFn: (p: { userId: string; roleId: string; companyId: string | null }) =>
      assignUserRole(p.userId, p.roleId, p.companyId),
    onSuccess: () => {
      invalidate()
      setRoleTarget(null)
      setRoleFormError(null)
    },
    onError: err => setRoleFormError(getErrorMessage(err)),
  })
  const removeRoleMutation = useMutation({
    mutationFn: (p: { userId: string; roleId: string; companyId: string | null }) =>
      removeUserRole(p.userId, p.roleId, p.companyId),
    onSuccess: () => {
      invalidate()
      setRoleTarget(null)
      setRoleFormError(null)
    },
    onError: err => setRoleFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingUser(null)
    setFormError(null)
    reset(defaultValues)
    setIsModalOpen(true)
  }

  const openEditForm = (user: PlatformUser) => {
    setEditingUser(user)
    setFormError(null)
    reset({
      username: user.username,
      email: user.email,
      displayName: user.displayName,
      phoneNumber: user.phoneNumber ?? '',
      password: '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingUser(null)
    setFormError(null)
  }

  const onSubmit = (data: UserForm) => {
    setFormError(null)
    if (editingUser) {
      updateMutation.mutate({
        id: editingUser.id,
        data: {
          email: data.email,
          displayName: data.displayName,
          phoneNumber: data.phoneNumber || null,
          password: data.password ? data.password : null,
        },
      })
      return
    }
    if (!data.password) {
      setFormError('Password is required to create a user')
      return
    }
    createMutation.mutate({
      username: data.username,
      email: data.email,
      displayName: data.displayName,
      phoneNumber: data.phoneNumber || null,
      password: data.password,
    })
  }

  const filteredUsers = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return users
    return users.filter(
      u => u.username.toLowerCase().includes(q) || u.displayName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)
    )
  }, [users, search])

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
          title="Users"
          description={`${users.length} user account(s)`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New User
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by username, name, or email..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search users"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : filteredUsers.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No users match your search.' : 'No users yet. Create your first user.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Username</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Email</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Phone</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Roles</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Last Login</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredUsers.map(user => (
                    <tr key={user.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                        {user.username}
                      </td>
                      <td className="px-3 py-3 text-gray-900 dark:text-white">{user.displayName}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{user.email}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{user.phoneNumber ?? '—'}</td>
                      <td className="px-3 py-3">
                        <div className="flex flex-wrap gap-1">
                          {user.roles.length === 0 ? (
                            <span className="text-xs text-gray-400">No roles</span>
                          ) : (
                            user.roles.map(r => (
                              <Badge
                                key={`${r.roleId}-${r.companyId ?? 'global'}`}
                                variant={r.isGlobal ? 'success' : 'info'}
                                size="sm"
                              >
                                {r.isGlobal ? `${r.roleName} · Super Admin` : `${r.roleName} · ${r.companyName ?? 'Company'}`}
                              </Badge>
                            ))
                          )}
                        </div>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {user.lastLoginAt
                          ? new Date(user.lastLoginAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
                          : '—'}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={user.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {user.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Manage roles for ${user.username}`}
                            onClick={() => setRoleTarget(user)}
                          >
                            <ShieldCheck className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${user.username}`}
                            onClick={() => openEditForm(user)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          {user.isActive ? (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                              aria-label={`Deactivate ${user.username}`}
                              onClick={() => setStatusAction({ user, action: 'deactivate' })}
                            >
                              <PowerOff className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
                          ) : (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20"
                              aria-label={`Activate ${user.username}`}
                              onClick={() => setStatusAction({ user, action: 'activate' })}
                            >
                              <Power className="h-4 w-4" aria-hidden="true" />
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
        title={editingUser ? 'Edit User' : 'New User'}
        description={editingUser ? `Update ${editingUser.username}` : 'Provision a new user account'}
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
              {editingUser ? 'Save Changes' : 'Create User'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input
            {...register('username')}
            label="Username"
            placeholder="e.g. jsmith"
            {...fieldError(errors.username?.message)}
            disabled={!!editingUser}
            required
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('displayName')}
              label="Display Name"
              placeholder="e.g. Jane Smith"
              {...fieldError(errors.displayName?.message)}
              required
            />
            <Input
              {...register('phoneNumber')}
              label="Phone Number"
              placeholder="e.g. +1 555 000 0000"
              {...fieldError(errors.phoneNumber?.message)}
            />
          </div>
          <Input
            {...register('email')}
            type="email"
            label="Email"
            placeholder="e.g. jsmith@company.com"
            {...fieldError(errors.email?.message)}
            required
          />
          <Input
            {...register('password')}
            type="password"
            label="Password"
            placeholder={editingUser ? 'Leave blank to keep current' : 'At least 8 characters'}
            {...fieldError(errors.password?.message)}
            required={!editingUser}
            hint={editingUser ? 'Leave blank to keep the current password.' : 'Required. Minimum 8 characters.'}
          />
        </form>
      </Modal>

      <RoleManagerModal
        user={roleTarget}
        roles={roles}
        companies={companies}
        formError={roleFormError}
        assigning={assignRoleMutation.isPending}
        removing={removeRoleMutation.isPending}
        onClose={() => { setRoleTarget(null); setRoleFormError(null) }}
        onAssign={(roleId, companyId) =>
          roleTarget && assignRoleMutation.mutate({ userId: roleTarget.id, roleId, companyId })}
        onRemove={(roleId, companyId) =>
          roleTarget && removeRoleMutation.mutate({ userId: roleTarget.id, roleId, companyId })}
      />

      <ConfirmDialog
        isOpen={!!statusAction}
        onClose={() => setStatusAction(null)}
        onConfirm={() =>
          statusAction?.action === 'activate'
            ? activateMutation.mutate(statusAction.user.id)
            : deactivateMutation.mutate(statusAction?.user.id ?? '')
        }
        title={statusAction?.action === 'activate' ? 'Activate User' : 'Deactivate User'}
        message={
          statusAction?.action === 'activate'
            ? `Activate ${statusAction?.user.username}? They will be able to sign in again.`
            : `Deactivate ${statusAction?.user.username}? They will no longer be able to sign in.`
        }
        confirmText={statusAction?.action === 'activate' ? 'Activate' : 'Deactivate'}
        variant={statusAction?.action === 'activate' ? 'primary' : 'danger'}
        isLoading={activateMutation.isPending || deactivateMutation.isPending}
      />
    </div>
  )
}

interface RoleManagerModalProps {
  user: PlatformUser | null
  roles: Role[]
  companies: Company[]
  formError: string | null
  assigning: boolean
  removing: boolean
  onClose: () => void
  onAssign: (roleId: string, companyId: string | null) => void
  onRemove: (roleId: string, companyId: string | null) => void
}

function RoleManagerModal({
  user,
  roles,
  companies,
  formError,
  assigning,
  removing,
  onClose,
  onAssign,
  onRemove,
}: RoleManagerModalProps) {
  const [selectedRole, setSelectedRole] = useState('')
  const [scopeCompany, setScopeCompany] = useState<string>('') // '' = global (super admin)

  if (!user) return null

  return (
    <Modal
      isOpen={!!user}
      onClose={onClose}
      title={`Manage Roles — ${user.username}`}
      description="Assign a role globally (Super Admin) or scoped to a single company (Company Admin)"
      size="lg"
      footer={
        <Button variant="secondary" onClick={onClose}>
          Close
        </Button>
      }
    >
      {formError && (
        <div className="flex items-center gap-2 p-3 mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-4 w-4" /> <span>{formError}</span>
        </div>
      )}

      <div className="space-y-4">
        <div className="rounded-lg border border-gray-200 dark:border-gray-700 p-3">
          <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Current assignments</p>
          {user.roles.length === 0 ? (
            <p className="text-sm text-gray-400">This user has no roles yet.</p>
          ) : (
            <ul className="space-y-2">
              {user.roles.map(r => (
                <li key={`${r.roleId}-${r.companyId ?? 'global'}`} className="flex items-center justify-between gap-2">
                  <span className="text-sm">
                    <Badge variant={r.isGlobal ? 'success' : 'info'} size="sm">{r.roleName}</Badge>{' '}
                    <span className="text-gray-500">
                      {r.isGlobal ? '· Super Admin (all companies)' : `· ${r.companyName ?? 'Company'}`}
                    </span>
                  </span>
                  <Button
                    size="sm"
                    variant="ghost"
                    className="text-red-600"
                    disabled={removing}
                    onClick={() => onRemove(r.roleId, r.companyId ?? null)}
                  >
                    Remove
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
          <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Add assignment</p>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <select
              className="rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
              value={selectedRole}
              onChange={(e) => setSelectedRole(e.target.value)}
            >
              <option value="">Select role…</option>
              {roles.map(r => (
                <option key={r.id} value={r.id}>{r.name}</option>
              ))}
            </select>
            <select
              className="rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
              value={scopeCompany}
              onChange={(e) => setScopeCompany(e.target.value)}
            >
              <option value="">All companies (Super Admin)</option>
              {companies.map(c => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
            <Button
              variant="primary"
              disabled={!selectedRole || assigning}
              isLoading={assigning}
              onClick={() => onAssign(selectedRole, scopeCompany || null)}
            >
              Assign
            </Button>
          </div>
          <p className="text-xs text-gray-500 mt-2">
            Leave the company blank to grant the role across every company (Super Admin). Pick a company to scope the
            user to that company only (Company Admin).
          </p>
        </div>
      </div>
    </Modal>
  )
}
