// <copyright file="VendorCrud.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle, Banknote, ShieldCheck, X } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select, Checkbox } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getVendors, createVendor, updateVendor, deactivateVendor, activateVendor, setVendorHold, getPaymentTerms } from '@api/ap'
import type { Vendor, UpdateVendorRequest } from '@/types/ap'
import { vendor1099CategoryMap } from '@/pages/ap/statusMaps'

const bankAccountSchema = z.object({
  bankName: z.string().trim().min(1, 'Bank name is required'),
  accountNumber: z.string().trim().min(1, 'Account number is required'),
  routingNumber: z.string().optional(),
  isDefault: z.boolean(),
})

const vendorSchema = z.object({
  vendorId: z.string().trim().min(1, 'Vendor code is required'),
  name: z.string().trim().min(1, 'Name is required'),
  legalName: z.string().optional(),
  taxId: z.string().optional(),
  form1099Category: z.string().optional(),
  defaultPaymentTermId: z.string().optional(),
  backupWithholdingFlag: z.boolean(),
  backupWithholdingRate: z.coerce.number().min(0, 'Rate cannot be negative').max(100, 'Rate cannot exceed 100'),
  insuranceCarrier: z.string().optional(),
  insurancePolicyNumber: z.string().optional(),
  insuranceExpiry: z.string().optional(),
  diversityClassification: z.string().optional(),
  bankAccounts: z.array(bankAccountSchema),
})

type VendorForm = z.infer<typeof vendorSchema>

const defaultValues: VendorForm = {
  vendorId: '',
  name: '',
  legalName: '',
  taxId: '',
  form1099Category: '0',
  defaultPaymentTermId: '',
  backupWithholdingFlag: false,
  backupWithholdingRate: 24,
  insuranceCarrier: '',
  insurancePolicyNumber: '',
  insuranceExpiry: '',
  diversityClassification: '',
  bankAccounts: [],
}

const categoryOptions = Object.entries(vendor1099CategoryMap)
  .filter(([key]) => /^\d+$/.test(key))
  .sort((a, b) => Number(a[0]) - Number(b[0]))
  .map(([value, label]) => ({ value, label }))

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

/**
 * Shared vendor master CRUD. Vendors are a single AP-owned master (no CompanyId),
 * reused by both the Accounts Payable and Purchasing modules. The purchasing
 * "supplier" CRUD is this same component rendered under the purchasing route.
 */
export function VendorCrud() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingVendor, setEditingVendor] = useState<Vendor | null>(null)
  const [vendorToDelete, setVendorToDelete] = useState<Vendor | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    control,
    formState: { errors },
  } = useForm<VendorForm>({
    resolver: zodResolver(vendorSchema),
    defaultValues,
  })

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'bankAccounts',
  })

  const { data: vendors = [], isLoading } = useQuery({
    queryKey: ['ap', 'vendors'],
    queryFn: () => getVendors(),
  })

  const { data: paymentTerms = [] } = useQuery({
    queryKey: ['ap', 'paymentTerms'],
    queryFn: () => getPaymentTerms(),
  })

  const paymentTermOptions = useMemo(
    () =>
      paymentTerms.map(t => ({
        value: t.id,
        label: `${t.name} (net ${t.dueDays}${t.discountPercent > 0 ? `, ${t.discountPercent}%/${t.discountDays}d` : ''})`,
      })),
    [paymentTerms]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ap', 'vendors'] })
  }

  const createMutation = useMutation({
    mutationFn: createVendor,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateVendorRequest }) => updateVendor(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateVendor(id),
    onSuccess: invalidate,
    onError: err => setFormError(getErrorMessage(err)),
  })

  const activateMutation = useMutation({
    mutationFn: (id: string) => activateVendor(id),
    onSuccess: invalidate,
    onError: err => setFormError(getErrorMessage(err)),
  })

  const setHoldMutation = useMutation({
    mutationFn: ({ id, onHold }: { id: string; onHold: boolean }) => setVendorHold(id, { onHold }),
    onSuccess: invalidate,
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingVendor(null)
    setFormError(null)
    reset(defaultValues)
    setIsModalOpen(true)
  }

  const openEditForm = (vendor: Vendor) => {
    setEditingVendor(vendor)
    setFormError(null)
    reset({
      vendorId: vendor.vendorId,
      name: vendor.name,
      legalName: vendor.legalName ?? '',
      taxId: vendor.taxId ?? '',
      form1099Category: String(vendor.form1099Category ?? 0),
      defaultPaymentTermId: vendor.defaultPaymentTermId ?? '',
      backupWithholdingFlag: vendor.backupWithholdingFlag,
      backupWithholdingRate: Math.round((vendor.backupWithholdingRate || 0) * 100),
      insuranceCarrier: vendor.insuranceCarrier ?? '',
      insurancePolicyNumber: vendor.insurancePolicyNumber ?? '',
      insuranceExpiry: vendor.insuranceExpiry ? vendor.insuranceExpiry.slice(0, 10) : '',
      diversityClassification: vendor.diversityClassification ?? '',
      bankAccounts:
        vendor.bankAccounts.map(a => ({
          bankName: a.bankName,
          accountNumber: a.accountNumber,
          routingNumber: a.routingNumber ?? '',
          isDefault: a.isDefault,
        })) ?? [],
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingVendor(null)
    setFormError(null)
  }

  const onSubmit = (data: VendorForm) => {
    setFormError(null)
    const common = {
      name: data.name,
      legalName: data.legalName || null,
      taxId: data.taxId || null,
      form1099Category: data.form1099Category ? Number(data.form1099Category) : null,
      defaultPaymentTermId: data.defaultPaymentTermId || null,
      backupWithholdingFlag: data.backupWithholdingFlag,
      backupWithholdingRate: data.backupWithholdingRate / 100,
      insuranceCarrier: data.insuranceCarrier || null,
      insurancePolicyNumber: data.insurancePolicyNumber || null,
      insuranceExpiry: data.insuranceExpiry || null,
      diversityClassification: data.diversityClassification || null,
    }
    const bankAccounts = data.bankAccounts.map(b => ({
      bankName: b.bankName,
      accountNumber: b.accountNumber,
      routingNumber: b.routingNumber || null,
      isDefault: b.isDefault,
    }))
    if (editingVendor) {
      updateMutation.mutate({ id: editingVendor.id, data: common })
      return
    }
    createMutation.mutate({
      ...common,
      vendorId: data.vendorId,
      isActive: true,
      bankAccounts,
    })
  }

  const filteredVendors = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return vendors
    return vendors.filter(v => v.name.toLowerCase().includes(q) || v.vendorId.toLowerCase().includes(q))
  }, [vendors, search])

  const backupWithholdingFlag = watch('backupWithholdingFlag')

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
          title="Vendors"
          description={`${vendors.length} vendor(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Vendor
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
              aria-label="Search vendors"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : filteredVendors.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No vendors match your search.' : 'No vendors yet. Create your first vendor.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">1099 Category</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Withholding</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredVendors.map(vendor => (
                    <tr key={vendor.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{vendor.vendorId}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        <p className="font-medium text-gray-900 dark:text-white">{vendor.name}</p>
                        {vendor.legalName && vendor.legalName !== vendor.name && (
                          <p className="text-xs text-gray-500 dark:text-gray-400">{vendor.legalName}</p>
                        )}
                        {vendor.taxId && <p className="text-xs text-gray-500 dark:text-gray-400">Tax ID: {vendor.taxId}</p>}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {vendor1099CategoryMap[String(vendor.form1099Category ?? 0)] ?? '—'}
                      </td>
                      <td className="px-3 py-3">
                        {vendor.backupWithholdingFlag ? (
                          <Badge variant="warning" size="sm">
                            {(vendor.backupWithholdingRate * 100).toFixed(0)}%
                          </Badge>
                        ) : (
                          <Badge variant="neutral" size="sm">
                            None
                          </Badge>
                        )}
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center gap-1">
                          <Badge variant={vendor.isActive ? 'success' : 'neutral'} size="sm" dot>
                            {vendor.isActive ? 'Active' : 'Inactive'}
                          </Badge>
                          {vendor.onHold && (
                            <Badge variant="warning" size="sm">
                              On Hold
                            </Badge>
                          )}
                        </div>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${vendor.name}`}
                            onClick={() => openEditForm(vendor)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          {vendor.onHold ? (
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => setHoldMutation.mutate({ id: vendor.id, onHold: false })}
                            >
                              Release
                            </Button>
                          ) : (
                            <Button
                              size="sm"
                              variant="ghost"
                              className="text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/20"
                              onClick={() => setHoldMutation.mutate({ id: vendor.id, onHold: true })}
                            >
                              Hold
                            </Button>
                          )}
                          {vendor.isActive ? (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/20"
                              aria-label={`Deactivate ${vendor.name}`}
                              onClick={() => deactivateMutation.mutate(vendor.id)}
                            >
                              <Trash2 className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
                          ) : (
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => activateMutation.mutate(vendor.id)}
                            >
                              Reactivate
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
        title={editingVendor ? 'Edit Vendor' : 'New Vendor'}
        description={editingVendor ? `Update ${editingVendor.name}` : 'Add a new vendor (shared supplier master across modules)'}
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
              {editingVendor ? 'Save Changes' : 'Create Vendor'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('vendorId')}
              label="Vendor Code"
              placeholder="e.g. VEND-0001"
              {...fieldError(errors.vendorId?.message)}
              disabled={!!editingVendor}
              required
            />
            <Input
              {...register('name')}
              label="Name"
              placeholder="Vendor name"
              {...fieldError(errors.name?.message)}
              required
            />
            <Input {...register('legalName')} label="Legal Name" placeholder="Legal entity name" />
            <Input {...register('taxId')} label="Tax ID" placeholder="EIN / VAT number" />
            <Select
              {...register('form1099Category')}
              label="1099 Category"
              placeholder="Select 1099 category..."
              options={categoryOptions}
              {...fieldError(errors.form1099Category?.message)}
            />
            <Select
              {...register('defaultPaymentTermId')}
              label="Default Payment Terms"
              placeholder="Select payment terms..."
              options={paymentTermOptions}
              {...fieldError(errors.defaultPaymentTermId?.message)}
            />
          </div>

          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Banknote className="h-4 w-4 text-gray-400" aria-hidden="true" />
              <h3 className="text-sm font-medium text-gray-900 dark:text-white">Bank Accounts</h3>
            </div>
            {fields.length === 0 && (
              <p className="text-sm text-gray-500 dark:text-gray-400">
                No bank accounts. Add one to enable ACH or check payments.
              </p>
            )}
            {fields.map((field, index) => (
              <div key={field.id} className="grid grid-cols-1 sm:grid-cols-[1fr_1fr_1fr_auto] gap-3 items-start">
                <Input
                  {...register(`bankAccounts.${index}.bankName`)}
                  {...(index === 0 ? { label: 'Bank Name' } : {})}
                  placeholder="Bank name"
                  aria-label={`Bank ${index + 1} name`}
                />
                <Input
                  {...register(`bankAccounts.${index}.accountNumber`)}
                  {...(index === 0 ? { label: 'Account Number' } : {})}
                  placeholder="Account number"
                  aria-label={`Bank ${index + 1} account number`}
                />
                <Input
                  {...register(`bankAccounts.${index}.routingNumber`)}
                  {...(index === 0 ? { label: 'Routing Number' } : {})}
                  placeholder="Routing"
                  aria-label={`Bank ${index + 1} routing number`}
                />
                <div className="flex items-center gap-2 pt-2 sm:pt-7">
                  <Checkbox
                    {...register(`bankAccounts.${index}.isDefault`)}
                    label="Default"
                  />
                  <IconButton
                    size="sm"
                    variant="ghost"
                    className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                    aria-label={`Remove bank account ${index + 1}`}
                    onClick={() => remove(index)}
                  >
                    <X className="h-4 w-4" aria-hidden="true" />
                  </IconButton>
                </div>
              </div>
            ))}
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => append({ bankName: '', accountNumber: '', routingNumber: '', isDefault: fields.length === 0 })}
              leftIcon={<Plus className="h-4 w-4" />}
            >
              Add Bank Account
            </Button>
          </div>

          <div className="flex flex-col gap-4">
            <Checkbox
              {...register('backupWithholdingFlag')}
              label="Subject to Backup Withholding"
              description="Withhold federal tax at the configured rate on payments to this vendor"
            />
            {backupWithholdingFlag && (
              <Input
                {...register('backupWithholdingRate')}
                type="number"
                step="0.01"
                min="0"
                max="100"
                label="Backup Withholding Rate (%)"
                hint="Default 24% per IRS backup withholding rules"
                {...fieldError(errors.backupWithholdingRate?.message)}
              />
            )}
          </div>

          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <ShieldCheck className="h-4 w-4 text-gray-400" aria-hidden="true" />
              <h3 className="text-sm font-medium text-gray-900 dark:text-white">Compliance</h3>
              {editingVendor?.onHold && (
                <Badge variant="warning" size="sm">
                  On Hold
                </Badge>
              )}
            </div>
            {editingVendor?.onHold && (
              <p className="text-xs text-gray-500 dark:text-gray-400">
                This vendor is on hold and is excluded from payment selection until released.
              </p>
            )}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input {...register('insuranceCarrier')} label="Insurance Carrier" placeholder="e.g. Travelers" />
              <Input {...register('insurancePolicyNumber')} label="Insurance Policy Number" placeholder="Policy number" />
              <Input {...register('insuranceExpiry')} type="date" label="Insurance Expiry" />
              <Input
                {...register('diversityClassification')}
                label="Diversity Classification"
                placeholder="e.g. MBE, WBE, DBE, SDVOSB"
                hint="Free-text diversity classification for reporting"
              />
            </div>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!vendorToDelete}
        onClose={() => setVendorToDelete(null)}
        onConfirm={() => vendorToDelete && deactivateMutation.mutate(vendorToDelete.id)}
        title="Deactivate Vendor"
        message={
          vendorToDelete
            ? `Are you sure you want to deactivate "${vendorToDelete.name}"? This is a soft delete and can be reversed by an administrator.`
            : ''
        }
        confirmText="Deactivate"
        variant="danger"
        isLoading={deactivateMutation.isPending}
      />
    </div>
  )
}
