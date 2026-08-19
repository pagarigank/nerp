import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle, Power, XCircle, Ban, Users } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import { getAccounts } from '@api/platform'
import {
  getBankAccounts,
  createBankAccount,
  updateBankAccount,
  deleteBankAccount,
  activateBankAccount,
  deactivateBankAccount,
  closeBankAccount,
  getBankAccountDetail,
  addBankContact,
  deleteBankContact,
  DEMO_COMPANY_ID,
} from '@api/cash'
import type { CashBankAccount, CreateCashBankAccountRequest, UpdateCashBankAccountRequest } from '@/types/cash'
import { bankAccountStatusMap, bankAccountTypeMap, bankAccountTypeValue } from './statusMaps'

const accountSchema = z.object({
  accountCode: z.string().trim().min(1, 'Account code is required'),
  accountName: z.string().trim().min(1, 'Account name is required'),
  accountNumber: z.string().trim().min(1, 'Account number is required'),
  routingNumber: z.string().optional(),
  bankName: z.string().optional(),
  currencyCode: z.string().min(1, 'Currency is required'),
  accountType: z.string().min(1, 'Account type is required'),
  openingBalance: z.coerce.number().min(0, 'Opening balance cannot be negative'),
  glAccountId: z.string().optional(),
})

type AccountForm = z.infer<typeof accountSchema>

const accountTypeOptions = [
  { value: '0', label: 'Checking' },
  { value: '1', label: 'Savings' },
  { value: '2', label: 'Money Market' },
  { value: '3', label: 'Petty Cash' },
  { value: '4', label: 'Investment' },
]

const currencyOptions = ['USD', 'CAD', 'EUR', 'GBP', 'MXN'].map(code => ({ value: code, label: code }))

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function BankAccountsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingAccount, setEditingAccount] = useState<CashBankAccount | null>(null)
  const [accountToDelete, setAccountToDelete] = useState<CashBankAccount | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [contactsAccount, setContactsAccount] = useState<CashBankAccount | null>(null)
  const [contactName, setContactName] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [contactPhone, setContactPhone] = useState('')
  const [contactTitle, setContactTitle] = useState('')
  const [contactError, setContactError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<AccountForm>({
    resolver: zodResolver(accountSchema),
    defaultValues: {
      accountCode: '',
      accountName: '',
      accountNumber: '',
      routingNumber: '',
      bankName: '',
      currencyCode: 'USD',
      accountType: '0',
      openingBalance: 0,
      glAccountId: '',
    },
  })

  const { data: accounts = [], isLoading } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const { data: glAccounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const glAccountOptions = useMemo(
    () =>
      glAccounts
        .filter(a => a.isActive)
        .map(a => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [glAccounts]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'bankAccounts'] })
  }

  const createMutation = useMutation({
    mutationFn: createBankAccount,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateCashBankAccountRequest }) => updateBankAccount(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteBankAccount,
    onSuccess: () => {
      invalidate()
      setAccountToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, action }: { id: string; action: 'activate' | 'deactivate' | 'close' }) => {
      if (action === 'activate') return activateBankAccount(id)
      if (action === 'deactivate') return deactivateBankAccount(id)
      return closeBankAccount(id)
    },
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const {
    data: contactDetail,
    refetch: refetchContacts,
  } = useQuery({
    queryKey: ['cash', 'bankAccountDetail', contactsAccount?.id],
    queryFn: () => getBankAccountDetail(contactsAccount!.id),
    enabled: contactsAccount !== null,
  })

  const addContactMutation = useMutation({
    mutationFn: () => addBankContact(contactsAccount!.id, contactName, contactPhone || null, contactEmail || null, contactTitle || null),
    onSuccess: () => {
      refetchContacts()
      setContactName('')
      setContactEmail('')
      setContactPhone('')
      setContactTitle('')
      setContactError(null)
    },
    onError: err => setContactError(getErrorMessage(err)),
  })

  const deleteContactMutation = useMutation({
    mutationFn: (contactId: string) => deleteBankContact(contactsAccount!.id, contactId),
    onSuccess: () => refetchContacts(),
    onError: err => setContactError(getErrorMessage(err)),
  })

  const openContacts = (account: CashBankAccount) => {
    setContactsAccount(account)
    setContactError(null)
  }

  const openCreateForm = () => {
    setEditingAccount(null)
    setFormError(null)
    reset({
      accountCode: '',
      accountName: '',
      accountNumber: '',
      routingNumber: '',
      bankName: '',
      currencyCode: 'USD',
      accountType: '0',
      openingBalance: 0,
      glAccountId: '',
    })
    setIsModalOpen(true)
  }

  const openEditForm = (account: CashBankAccount) => {
    setEditingAccount(account)
    setFormError(null)
    reset({
      accountCode: account.accountCode,
      accountName: account.accountName,
      accountNumber: account.accountNumber,
      routingNumber: account.routingNumber ?? '',
      bankName: account.bankName ?? '',
      currencyCode: account.currencyCode || 'USD',
      accountType: String(bankAccountTypeValue[account.accountType] ?? 0),
      openingBalance: account.openingBalance,
      glAccountId: account.glAccountId ?? '',
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
    const common = {
      accountName: data.accountName,
      accountNumber: data.accountNumber,
      routingNumber: data.routingNumber || null,
      bankName: data.bankName || null,
      currencyCode: data.currencyCode || null,
      accountType: Number(data.accountType),
      glAccountId: data.glAccountId || null,
    }
    if (editingAccount) {
      updateMutation.mutate({ id: editingAccount.id, data: common })
      return
    }
    const payload: CreateCashBankAccountRequest = {
      companyId: DEMO_COMPANY_ID,
      accountCode: data.accountCode,
      openingBalance: data.openingBalance,
      ...common,
    }
    createMutation.mutate(payload)
  }

  const filteredAccounts = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return accounts
    return accounts.filter(
      a =>
        a.accountCode.toLowerCase().includes(q) ||
        a.accountName.toLowerCase().includes(q) ||
        a.bankName?.toLowerCase().includes(q) ||
        a.accountNumber.toLowerCase().includes(q)
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
          title="Bank Accounts"
          description={`${accounts.length} account(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Bank Account
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by code, name, or account number..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search bank accounts"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={8} />
          ) : filteredAccounts.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No accounts match your search.' : 'No bank accounts yet. Create your first account.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Balance</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Currency</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredAccounts.map(account => (
                    <tr key={account.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-gray-900 dark:text-white">
                        {account.accountCode}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        <p className="font-medium text-gray-900 dark:text-white">{account.accountName}</p>
                        <p className="text-xs text-gray-500 dark:text-gray-400">
                          {account.bankName ? `${account.bankName} · ` : ''}•••• {account.accountNumber.slice(-4)}
                        </p>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {bankAccountTypeMap[account.accountType] ?? account.accountType}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(account.currentBalance, account.currencyCode || 'USD')}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{account.currencyCode}</td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={account.status} mapping={bankAccountStatusMap} />
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          {account.status === 'Active' ? (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              aria-label={`Deactivate ${account.accountName}`}
                              title="Deactivate"
                              onClick={() => statusMutation.mutate({ id: account.id, action: 'deactivate' })}
                            >
                              <Ban className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
                          ) : account.status === 'Inactive' ? (
                            <>
                              <IconButton
                                size="sm"
                                variant="ghost"
                                className="text-emerald-600"
                                aria-label={`Activate ${account.accountName}`}
                                title="Activate"
                                onClick={() => statusMutation.mutate({ id: account.id, action: 'activate' })}
                              >
                                <Power className="h-4 w-4" aria-hidden="true" />
                              </IconButton>
                              <IconButton
                                size="sm"
                                variant="ghost"
                                className="text-red-600"
                                aria-label={`Close ${account.accountName}`}
                                title="Close account"
                                onClick={() => statusMutation.mutate({ id: account.id, action: 'close' })}
                              >
                                <XCircle className="h-4 w-4" aria-hidden="true" />
                              </IconButton>
                            </>
                          ) : null}
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${account.accountName}`}
                            onClick={() => openEditForm(account)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Delete ${account.accountName}`}
                            onClick={() => setAccountToDelete(account)}
                          >
                            <Trash2 className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Contacts ${account.accountName}`}
                            title="Contacts"
                            onClick={() => openContacts(account)}
                          >
                            <Users className="h-4 w-4" aria-hidden="true" />
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
        title={editingAccount ? 'Edit Bank Account' : 'New Bank Account'}
        description={editingAccount ? `Update ${editingAccount.accountName}` : 'Add a new bank account to track cash'}
        size="lg"
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
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('accountCode')}
              label="Account Code"
              placeholder="e.g. BNK-CHK-001"
              {...fieldError(errors.accountCode?.message)}
              disabled={!!editingAccount}
              required
            />
            <Input
              {...register('accountName')}
              label="Account Name"
              placeholder="e.g. Operating Checking"
              {...fieldError(errors.accountName?.message)}
              required
            />
            <Input
              {...register('accountNumber')}
              label="Account Number"
              placeholder="Bank account number"
              {...fieldError(errors.accountNumber?.message)}
              required
            />
            <Input {...register('routingNumber')} label="Routing Number" placeholder="ABA routing number" />
            <Input {...register('bankName')} label="Bank Name" placeholder="e.g. First National Bank" />
            <Select
              {...register('currencyCode')}
              label="Currency"
              options={currencyOptions}
              {...fieldError(errors.currencyCode?.message)}
            />
            <Select
              {...register('accountType')}
              label="Account Type"
              options={accountTypeOptions}
              {...fieldError(errors.accountType?.message)}
            />
            {!editingAccount && (
              <Input
                {...register('openingBalance')}
                type="number"
                step="0.01"
                min="0"
                label="Opening Balance"
                {...fieldError(errors.openingBalance?.message)}
              />
            )}
            <div className="sm:col-span-2">
              <Combobox
                label="GL Account (Cash)"
                placeholder="Select the cash GL account..."
                options={glAccountOptions}
                value={watch('glAccountId') ?? ''}
                onChange={value => setValue('glAccountId', value, { shouldValidate: true })}
                allowClear
              />
            </div>
          </div>
          {editingAccount && (
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Last reconciled balance: {formatCurrency(editingAccount.currentBalance, editingAccount.currencyCode || 'USD')} as of{' '}
              {formatDate(new Date())}
            </p>
          )}
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!accountToDelete}
        onClose={() => setAccountToDelete(null)}
        onConfirm={() => accountToDelete && deleteMutation.mutate(accountToDelete.id)}
        title="Delete Bank Account"
        message={
          accountToDelete
            ? `Are you sure you want to delete "${accountToDelete.accountName}"? This is a soft delete and can be reversed by an administrator.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        />

        <Modal
          isOpen={contactsAccount !== null}
          onClose={() => setContactsAccount(null)}
          title={`Contacts — ${contactsAccount?.accountName ?? ''}`}
        >
          <div className="space-y-3">
            {contactError && (
              <div className="flex items-center gap-2 text-sm text-red-600">
                <AlertCircle className="h-4 w-4" /> {contactError}
              </div>
            )}
            <div className="rounded border">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left text-gray-500">
                    <th className="px-3 py-2">Name</th>
                    <th>Title</th>
                    <th>Email</th>
                    <th>Phone</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {(contactDetail?.contacts ?? []).map(c => (
                    <tr key={c.id} className="border-b">
                      <td className="px-3 py-2">{c.name}</td>
                      <td>{c.title}</td>
                      <td>{c.email}</td>
                      <td>{c.phone}</td>
                      <td className="text-right">
                        <IconButton
                          size="sm"
                          variant="ghost"
                          className="text-red-600"
                          aria-label={`Delete contact ${c.name}`}
                          onClick={() => deleteContactMutation.mutate(c.id)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </IconButton>
                      </td>
                    </tr>
                  ))}
                  {(contactDetail?.contacts ?? []).length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-3 py-3 text-gray-500">No contacts.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <Input label="Name" value={contactName} onChange={(e) => setContactName(e.target.value)} />
              <Input label="Title" value={contactTitle} onChange={(e) => setContactTitle(e.target.value)} />
              <Input label="Email" value={contactEmail} onChange={(e) => setContactEmail(e.target.value)} />
              <Input label="Phone" value={contactPhone} onChange={(e) => setContactPhone(e.target.value)} />
            </div>
            <div className="flex justify-end">
              <Button disabled={!contactName || addContactMutation.isPending} onClick={() => addContactMutation.mutate()}>
                Add Contact
              </Button>
            </div>
          </div>
        </Modal>

        </div>
        )
        }
