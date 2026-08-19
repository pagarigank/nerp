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
import { getCompanies, createCompany, updateCompany, deleteCompany } from '@api/platform'
import type { Company } from '@/types/platform'

const companySchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  legalName: z.string().trim().min(1, 'Legal name is required'),
  baseCurrency: z.string().trim().min(1, 'Base currency is required'),
  taxId: z.string().optional(),
  address: z.string().optional(),
})

type CompanyForm = z.infer<typeof companySchema>

const defaultValues: CompanyForm = {
  name: '',
  legalName: '',
  baseCurrency: 'USD',
  taxId: '',
  address: '',
}

const currencyOptions = ['USD', 'CAD', 'EUR', 'GBP', 'MXN'].map(code => ({ value: code, label: code }))

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function CompaniesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingCompany, setEditingCompany] = useState<Company | null>(null)
  const [companyToDelete, setCompanyToDelete] = useState<Company | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CompanyForm>({
    resolver: zodResolver(companySchema),
    defaultValues,
  })

  const { data: companies = [], isLoading } = useQuery({
    queryKey: ['platform', 'companies'],
    queryFn: getCompanies,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['platform', 'companies'] })
  }

  const createMutation = useMutation({
    mutationFn: createCompany,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { name: string; legalName: string; baseCurrency: string; taxId?: string | null; address?: string | null } }) =>
      updateCompany(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteCompany,
    onSuccess: () => {
      invalidate()
      setCompanyToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingCompany(null)
    setFormError(null)
    reset(defaultValues)
    setIsModalOpen(true)
  }

  const openEditForm = (company: Company) => {
    setEditingCompany(company)
    setFormError(null)
    reset({
      name: company.name,
      legalName: company.legalName,
      baseCurrency: company.baseCurrency,
      taxId: company.taxId ?? '',
      address: company.address ?? '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingCompany(null)
    setFormError(null)
  }

  const onSubmit = (data: CompanyForm) => {
    setFormError(null)
    const payload = {
      name: data.name,
      legalName: data.legalName,
      baseCurrency: data.baseCurrency,
      taxId: data.taxId || null,
      address: data.address || null,
    }
    if (editingCompany) {
      updateMutation.mutate({ id: editingCompany.id, data: payload })
      return
    }
    createMutation.mutate(payload)
  }

  const filteredCompanies = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return companies
    return companies.filter(
      c => c.name.toLowerCase().includes(q) || c.legalName.toLowerCase().includes(q)
    )
  }, [companies, search])

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
          title="Companies"
          description={`${companies.length} company(ies) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Company
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by name..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search companies"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : filteredCompanies.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No companies match your search.' : 'No companies yet. Create your first company.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Legal Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Currency</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Tax ID</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredCompanies.map(company => (
                    <tr key={company.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{company.name}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{company.legalName}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{company.baseCurrency}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{company.taxId ?? '—'}</td>
                      <td className="px-3 py-3">
                        <Badge variant={company.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {company.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${company.name}`}
                            onClick={() => openEditForm(company)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Delete ${company.name}`}
                            onClick={() => setCompanyToDelete(company)}
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
        title={editingCompany ? 'Edit Company' : 'New Company'}
        description={editingCompany ? `Update ${editingCompany.name}` : 'Add a new company to the ERP system'}
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
              {editingCompany ? 'Save Changes' : 'Create Company'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('name')}
              label="Name"
              placeholder="Company name"
              {...fieldError(errors.name?.message)}
              required
            />
            <Input
              {...register('legalName')}
              label="Legal Name"
              placeholder="Legal entity name"
              {...fieldError(errors.legalName?.message)}
              required
            />
            <Select
              {...register('baseCurrency')}
              label="Base Currency"
              options={currencyOptions}
              {...fieldError(errors.baseCurrency?.message)}
              required
            />
            <Input {...register('taxId')} label="Tax ID" placeholder="EIN / VAT number" />
          </div>
          <Input
            {...register('address')}
            label="Address"
            placeholder="Registered office address"
          />
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!companyToDelete}
        onClose={() => setCompanyToDelete(null)}
        onConfirm={() => companyToDelete && deleteMutation.mutate(companyToDelete.id)}
        title="Delete Company"
        message={
          companyToDelete
            ? `Are you sure you want to delete "${companyToDelete.name}"? This is a soft delete and can be reversed by an administrator.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
