import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select, Checkbox } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import { getAccounts, createAccount, updateAccount, deleteAccount, companyId } from '@api/platform'
import type { Account } from '@/types/platform'
import { accountTypeMap } from './statusMaps'

const accountSchema = z.object({
  accountNumber: z.string().trim().min(1, 'Account number is required'),
  description: z.string().trim().min(1, 'Description is required'),
  accountType: z.string().min(1, 'Account type is required'),
  normalBalance: z.string().min(1, 'Normal balance is required'),
  isActive: z.boolean(),
})

type AccountForm = z.infer<typeof accountSchema>

const accountTypeOptions = [
  { value: '0', label: 'Asset' },
  { value: '1', label: 'Liability' },
  { value: '2', label: 'Equity' },
  { value: '3', label: 'Revenue' },
  { value: '4', label: 'Expense' },
]

const normalBalanceOptions = [
  { value: '0', label: 'Debit' },
  { value: '1', label: 'Credit' },
]

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function AccountsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingAccount, setEditingAccount] = useState<Account | null>(null)
  const [accountToDelete, setAccountToDelete] = useState<Account | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<AccountForm>({
    resolver: zodResolver(accountSchema),
    defaultValues: {
      accountNumber: '',
      description: '',
      accountType: '0',
      normalBalance: '0',
      isActive: true,
    },
  })

  const { data: accounts = [], isLoading } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform', 'accounts'] })
  }

  const createMutation = useMutation({
    mutationFn: createAccount,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { description: string; accountType: number; normalBalance: number; isActive: boolean } }) =>
      updateAccount(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteAccount,
    onSuccess: () => {
      invalidate()
      setAccountToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingAccount(null)
    setFormError(null)
    reset({
      accountNumber: '',
      description: '',
      accountType: '0',
      normalBalance: '0',
      isActive: true,
    })
    setIsModalOpen(true)
  }

  const openEditForm = (account: Account) => {
    setEditingAccount(account)
    setFormError(null)
    reset({
      accountNumber: account.accountNumber,
      description: account.description,
      accountType: String(account.accountType),
      normalBalance: String(account.normalBalance),
      isActive: account.isActive,
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingAccount(null)
    setFormError(null)
  }

  const onSubmit = (data: AccountForm) => {
    setFormError(null)
    if (editingAccount) {
      updateMutation.mutate({
        id: editingAccount.id,
        data: {
          description: data.description,
          accountType: Number(data.accountType),
          normalBalance: Number(data.normalBalance),
          isActive: data.isActive,
        },
      })
      return
    }
    createMutation.mutate({
      companyId: companyId(),
      accountNumber: data.accountNumber,
      description: data.description,
      accountType: Number(data.accountType),
      normalBalance: Number(data.normalBalance),
      isActive: data.isActive,
    })
  }

  const filteredAccounts = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return accounts
    return accounts.filter(
      a => a.accountNumber.toLowerCase().includes(q) || a.description.toLowerCase().includes(q)
    )
  }, [accounts, search])

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
          title="Chart of Accounts"
          description={`${accounts.length} account(s) for the selected company`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Account
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by number or description..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search accounts"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : filteredAccounts.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No accounts match your search.' : 'No accounts yet. Create your first account.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Account #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Normal Balance</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredAccounts.map(account => (
                    <tr key={account.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-primary-600 dark:text-primary-400">
                        {account.accountNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-900 dark:text-white">{account.description}</td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={account.accountType} mapping={accountTypeMap} />
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {account.normalBalance === 0 ? 'Debit' : 'Credit'}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={account.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {account.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${account.accountNumber}`}
                            onClick={() => openEditForm(account)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Delete ${account.accountNumber}`}
                            onClick={() => setAccountToDelete(account)}
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
        title={editingAccount ? 'Edit Account' : 'New Account'}
        description={editingAccount ? `Update ${editingAccount.accountNumber}` : 'Add a new general ledger account'}
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
              {editingAccount ? 'Save Changes' : 'Create Account'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input
            {...register('accountNumber')}
            label="Account Number"
            placeholder="e.g. 1000"
            {...fieldError(errors.accountNumber?.message)}
            disabled={!!editingAccount}
            required
          />
          <Input
            {...register('description')}
            label="Description"
            placeholder="Account description"
            {...fieldError(errors.description?.message)}
            required
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Select
              {...register('accountType')}
              label="Account Type"
              options={accountTypeOptions}
              {...fieldError(errors.accountType?.message)}
              required
            />
            <Select
              {...register('normalBalance')}
              label="Normal Balance"
              options={normalBalanceOptions}
              {...fieldError(errors.normalBalance?.message)}
              required
            />
          </div>
          <Checkbox
            {...register('isActive')}
            label="Active"
            description="Accounts must be active to be used in transactions"
          />
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!accountToDelete}
        onClose={() => setAccountToDelete(null)}
        onConfirm={() => accountToDelete && deleteMutation.mutate(accountToDelete.id)}
        title="Delete Account"
        message={
          accountToDelete
            ? `Are you sure you want to delete account "${accountToDelete.accountNumber} - ${accountToDelete.description}"? This is a soft delete.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
