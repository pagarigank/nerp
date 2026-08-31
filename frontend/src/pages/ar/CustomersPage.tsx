import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select, Checkbox, Textarea, SearchInput } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getCustomers, createCustomer, updateCustomer, deleteCustomer } from '@api/ar'
import { getPaymentTerms } from '@api/ap'
import { getSalesReps, getTaxCodes, getTaxExemptions } from '@api/orderManagement'
import type { ArCustomer, UpdateArCustomerRequest } from '@/types/ar'
import type { PaymentTerm } from '@/types/ap'
import type { SalesRepSummary, TaxCodeSummary, TaxExemptionCertificateSummary } from '@/types/orderManagement'

const customerSchema = z.object({
  customerId: z.string().trim().min(1, 'Customer code is required'),
  name: z.string().trim().min(1, 'Name is required'),
  legalName: z.string().optional(),
  taxId: z.string().optional(),
  creditLimit: z.coerce.number().min(0, 'Credit limit cannot be negative'),
  creditHoldDays: z.coerce.number().int().min(0, 'Credit hold days must be 0 or greater'),
  taxExempt: z.boolean(),
  taxExemptCertificate: z.string().optional(),
  currencyCode: z.string().min(1, 'Currency is required'),
  defaultPaymentTermId: z.string().optional(),
  salesRepId: z.string().optional(),
  taxCodeId: z.string().optional(),
  taxExemptionCertificateId: z.string().optional(),
  billingAddress: z.string().optional(),
  billingCity: z.string().optional(),
  billingState: z.string().optional(),
  billingZipCode: z.string().optional(),
  billingCountry: z.string().optional(),
  shippingAddress: z.string().optional(),
  shippingCity: z.string().optional(),
  shippingState: z.string().optional(),
  shippingZipCode: z.string().optional(),
  shippingCountry: z.string().optional(),
  address: z.string().optional(),
  city: z.string().optional(),
  state: z.string().optional(),
  zipCode: z.string().optional(),
  phone: z.string().optional(),
  email: z.string().email('Invalid email format').optional().or(z.literal('')),
  website: z.string().optional(),
  contactName: z.string().optional(),
  notes: z.string().optional(),
})

type CustomerForm = z.infer<typeof customerSchema>

const defaultValues: CustomerForm = {
  customerId: '',
  name: '',
  legalName: '',
  taxId: '',
  creditLimit: 0,
  creditHoldDays: 0,
  taxExempt: false,
  taxExemptCertificate: '',
  currencyCode: 'USD',
  defaultPaymentTermId: '',
  salesRepId: '',
  taxCodeId: '',
  taxExemptionCertificateId: '',
  billingAddress: '',
  billingCity: '',
  billingState: '',
  billingZipCode: '',
  billingCountry: 'US',
  shippingAddress: '',
  shippingCity: '',
  shippingState: '',
  shippingZipCode: '',
  shippingCountry: 'US',
  address: '',
  city: '',
  state: '',
  zipCode: '',
  phone: '',
  email: '',
  website: '',
  contactName: '',
  notes: '',
}

const currencyOptions = ['USD', 'CAD', 'EUR', 'GBP', 'MXN'].map(code => ({ value: code, label: code }))
const stateOptions = [
  'AL','AK','AZ','AR','CA','CO','CT','DE','FL','GA','HI','ID','IL','IN','IA','KS','KY','LA','ME','MD',
  'MA','MI','MN','MS','MO','MT','NE','NV','NH','NJ','NM','NY','NC','ND','OH','OK','OR','PA','RI','SC',
  'SD','TN','TX','UT','VT','VA','WA','WV','WI','WY'
].map(s => ({ value: s, label: s }))

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function CustomersPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingCustomer, setEditingCustomer] = useState<ArCustomer | null>(null)
  const [customerToDelete, setCustomerToDelete] = useState<ArCustomer | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<CustomerForm>({
    resolver: zodResolver(customerSchema),
    defaultValues,
  })

  const { data: customers = [], isLoading } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: getCustomers,
  })

  const { data: paymentTerms = [] } = useQuery({
    queryKey: ['ap', 'paymentTerms'],
    queryFn: () => getPaymentTerms(),
  })

  const paymentTermOptions = useMemo(
    () => paymentTerms.map((t: PaymentTerm) => ({
      value: t.id,
      label: `${t.name} (net ${t.dueDays}${t.discountPercent > 0 ? `, ${t.discountPercent}%/${t.discountDays}d` : ''})`,
    })),
    [paymentTerms]
  )

  const { data: salesReps = [] } = useQuery({ queryKey: ['om', 'salesReps'], queryFn: () => getSalesReps() })
  const { data: taxCodes = [] } = useQuery({ queryKey: ['om', 'taxCodes'], queryFn: () => getTaxCodes() })
  const { data: taxExemptions = [] } = useQuery({ queryKey: ['om', 'taxExemptions'], queryFn: () => getTaxExemptions() })

  const salesRepOptions = useMemo(
    () => salesReps.map((r: SalesRepSummary) => ({ value: r.id, label: `${r.code} - ${r.name}` })),
    [salesReps]
  )
  const taxCodeOptions = useMemo(
    () => taxCodes.map((t: TaxCodeSummary) => ({ value: t.id, label: `${t.code} - ${t.description} (${t.rate}%)` })),
    [taxCodes]
  )
  const taxExemptionOptions = useMemo(
    () => taxExemptions.filter((e: TaxExemptionCertificateSummary) => e.isActive).map((e: TaxExemptionCertificateSummary) => ({ value: e.id, label: `${e.certificateNumber} - ${e.jurisdiction}` })),
    [taxExemptions]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ar', 'customers'] })
  }

  const createMutation = useMutation({
    mutationFn: createCustomer,
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateArCustomerRequest }) => updateCustomer(id, data),
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteCustomer,
    onSuccess: () => { invalidate(); setCustomerToDelete(null) },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingCustomer(null)
    setFormError(null)
    reset(defaultValues)
    setIsModalOpen(true)
  }

  const openEditForm = (customer: ArCustomer) => {
    setEditingCustomer(customer)
    setFormError(null)
    reset({
      customerId: customer.customerId,
      name: customer.name,
      legalName: customer.legalName ?? '',
      taxId: customer.taxId ?? '',
      creditLimit: customer.creditLimit,
      creditHoldDays: customer.creditHoldDays,
      taxExempt: customer.taxExempt,
      taxExemptCertificate: customer.taxExemptCertificate ?? '',
      currencyCode: customer.currencyCode || 'USD',
      defaultPaymentTermId: customer.defaultPaymentTermId ?? '',
      salesRepId: customer.salesRepId ?? '',
      taxCodeId: customer.taxCodeId ?? '',
      taxExemptionCertificateId: customer.taxExemptionCertificateId ?? '',
      billingAddress: customer.billingAddress ?? '',
      billingCity: customer.billingCity ?? '',
      billingState: customer.billingState ?? '',
      billingZipCode: customer.billingZipCode ?? '',
      billingCountry: customer.billingCountry ?? 'US',
      shippingAddress: customer.shippingAddress ?? '',
      shippingCity: customer.shippingCity ?? '',
      shippingState: customer.shippingState ?? '',
      shippingZipCode: customer.shippingZipCode ?? '',
      shippingCountry: customer.shippingCountry ?? 'US',
      address: '',
      city: '',
      state: '',
      zipCode: '',
      phone: '',
      email: '',
      website: '',
      contactName: '',
      notes: '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingCustomer(null)
    setFormError(null)
  }

  const onSubmit = (data: CustomerForm) => {
    setFormError(null)
    if (editingCustomer) {
      updateMutation.mutate({
        id: editingCustomer.id,
        data: {
          name: data.name,
          legalName: data.legalName || null,
          taxId: data.taxId || null,
          creditLimit: data.creditLimit,
          creditHoldDays: data.creditHoldDays,
          defaultPaymentTermId: data.defaultPaymentTermId || null,
          taxExempt: data.taxExempt,
          taxExemptCertificate: data.taxExemptCertificate || null,
          currencyCode: data.currencyCode || null,
          salesRepId: data.salesRepId || null,
          taxCodeId: data.taxCodeId || null,
          taxExemptionCertificateId: data.taxExemptionCertificateId || null,
          billingAddress: data.billingAddress || null,
          billingCity: data.billingCity || null,
          billingState: data.billingState || null,
          billingZipCode: data.billingZipCode || null,
          billingCountry: data.billingCountry || null,
          shippingAddress: data.shippingAddress || null,
          shippingCity: data.shippingCity || null,
          shippingState: data.shippingState || null,
          shippingZipCode: data.shippingZipCode || null,
          shippingCountry: data.shippingCountry || null,
        },
      })
      return
    }
    createMutation.mutate({
      customerId: data.customerId,
      name: data.name,
      legalName: data.legalName || null,
      taxId: data.taxId || null,
      creditLimit: data.creditLimit,
      creditHoldDays: data.creditHoldDays,
      defaultPaymentTermId: data.defaultPaymentTermId || null,
      taxExempt: data.taxExempt,
      taxExemptCertificate: data.taxExemptCertificate || null,
      currencyCode: data.currencyCode || null,
      salesRepId: data.salesRepId || null,
      taxCodeId: data.taxCodeId || null,
      taxExemptionCertificateId: data.taxExemptionCertificateId || null,
      billingAddress: data.billingAddress || null,
      billingCity: data.billingCity || null,
      billingState: data.billingState || null,
      billingZipCode: data.billingZipCode || null,
      billingCountry: data.billingCountry || null,
      shippingAddress: data.shippingAddress || null,
      shippingCity: data.shippingCity || null,
      shippingState: data.shippingState || null,
      shippingZipCode: data.shippingZipCode || null,
      shippingCountry: data.shippingCountry || null,
    })
  }

  const filteredCustomers = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return customers
    return customers.filter(
      c => c.name.toLowerCase().includes(q) || c.customerId.toLowerCase().includes(q)
    )
  }, [customers, search])

  const taxExempt = watch('taxExempt')

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Customers"
          description={`${customers.length} customer(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Customer
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <SearchInput
              value={search}
              onChange={setSearch}
              placeholder="Search by name or code..."
              aria-label="Search customers"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : filteredCustomers.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No customers match your search.' : 'No customers yet. Create your first customer.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Contact</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Credit Limit</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Currency</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Tax</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredCustomers.map(customer => (
                    <tr key={customer.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{customer.customerId}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        <p className="font-medium text-gray-900 dark:text-white">{customer.name}</p>
                        {customer.legalName && customer.legalName !== customer.name && (
                          <p className="text-xs text-gray-500 dark:text-gray-400">{customer.legalName}</p>
                        )}
                      </td>
                      <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                        {customer.taxId && <p>Tax ID: {customer.taxId}</p>}
                        {!customer.taxId && <p className="italic">No tax ID</p>}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {new Intl.NumberFormat('en-US', {
                          style: 'currency',
                          currency: customer.currencyCode || 'USD',
                        }).format(customer.creditLimit)}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{customer.currencyCode || 'USD'}</td>
                      <td className="px-3 py-3">
                        {customer.taxExempt ? (
                          <Badge variant="info" size="sm">Exempt</Badge>
                        ) : (
                          <Badge variant="neutral" size="sm">Taxable</Badge>
                        )}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={customer.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {customer.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton size="sm" variant="ghost" aria-label={`Edit ${customer.name}`} onClick={() => openEditForm(customer)}>
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton size="sm" variant="ghost" className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20" aria-label={`Delete ${customer.name}`} onClick={() => setCustomerToDelete(customer)}>
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
        title={editingCustomer ? 'Edit Customer' : 'New Customer'}
        description={editingCustomer ? `Update ${editingCustomer.name}` : 'Add a new accounts receivable customer'}
        size="xl"
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
              {editingCustomer ? 'Save Changes' : 'Create Customer'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          {/* Basic Info */}
          <div>
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">Basic Information</h4>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                {...register('customerId')}
                label="Customer Code"
                placeholder="e.g. CUST-0001"
                {...fieldError(errors.customerId?.message)}
                disabled={!!editingCustomer}
                required
              />
              <Input
                {...register('name')}
                label="Name"
                placeholder="Customer name"
                {...fieldError(errors.name?.message)}
                required
              />
              <Input {...register('legalName')} label="Legal Name" placeholder="Legal entity name" />
              <Input {...register('taxId')} label="Tax ID" placeholder="EIN / VAT number" />
              <Select
                {...register('currencyCode')}
                label="Currency"
                options={currencyOptions}
                {...fieldError(errors.currencyCode?.message)}
              />
              <Select
                {...register('defaultPaymentTermId')}
                label="Default Payment Terms"
                placeholder="Select payment terms..."
                options={paymentTermOptions}
              />
            </div>
          </div>

          {/* Credit */}
          <div>
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">Credit & Tax</h4>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input
                {...register('creditLimit')}
                type="number"
                step="0.01"
                min="0"
                label="Credit Limit"
                {...fieldError(errors.creditLimit?.message)}
              />
              <Input
                {...register('creditHoldDays')}
                type="number"
                step="1"
                min="0"
                label="Credit Hold (days past due)"
                hint="Days past due before automatic credit hold"
                {...fieldError(errors.creditHoldDays?.message)}
              />
            </div>
            <div className="flex flex-col gap-3 mt-3">
              <Checkbox
                {...register('taxExempt')}
                label="Tax Exempt"
                description="Exempt this customer from sales tax"
              />
              {taxExempt && (
                <Input
                  {...register('taxExemptCertificate')}
                  label="Tax Exempt Certificate"
                  placeholder="Certificate number"
                />
              )}
              <Select
                {...register('salesRepId')}
                label="Sales Rep"
                placeholder="Assign a sales person..."
                options={salesRepOptions}
              />
              <Select
                {...register('taxCodeId')}
                label="Default Tax Code"
                placeholder="Select tax code..."
                options={taxCodeOptions}
              />
              <Select
                {...register('taxExemptionCertificateId')}
                label="Default Tax Exemption"
                placeholder="Select exemption certificate..."
                options={taxExemptionOptions}
              />
            </div>
          </div>

          {/* Contact Info */}
          <div>
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">Contact Information</h4>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Input {...register('contactName')} label="Primary Contact" placeholder="Contact name" />
              <Input {...register('phone')} label="Phone" placeholder="(555) 555-5555" />
              <Input {...register('email')} label="Email" placeholder="contact@company.com" type="email" {...fieldError(errors.email?.message)} />
              <Input {...register('website')} label="Website" placeholder="https://company.com" />
            </div>
          </div>

          {/* Billing Address */}
          <div>
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">Billing Address</h4>
            <div className="space-y-3">
              <Textarea {...register('billingAddress')} label="Street Address" placeholder="123 Main St, Suite 100" rows={2} />
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <Input {...register('billingCity')} label="City" placeholder="City" />
                <Select {...register('billingState')} label="State" placeholder="Select..." options={stateOptions} />
                <Input {...register('billingZipCode')} label="ZIP Code" placeholder="12345" />
              </div>
              <Input {...register('billingCountry')} label="Country" placeholder="US" />
            </div>
          </div>

          {/* Shipping Address */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <h4 className="text-sm font-medium text-gray-900 dark:text-white">Shipping Address</h4>
              <label className="flex items-center gap-2 text-sm cursor-pointer">
                <input type="checkbox" onChange={e => {
                  if (e.target.checked) {
                    const billingAddress = watch('billingAddress')
                    const billingCity = watch('billingCity')
                    const billingState = watch('billingState')
                    const billingZipCode = watch('billingZipCode')
                    const billingCountry = watch('billingCountry')
                    reset({ ...watch(), shippingAddress: billingAddress, shippingCity: billingCity, shippingState: billingState, shippingZipCode: billingZipCode, shippingCountry: billingCountry || 'US' })
                  }
                }} className="h-4 w-4 rounded border-gray-300" />
                Same as billing
              </label>
            </div>
            <div className="space-y-3">
              <Textarea {...register('shippingAddress')} label="Street Address" placeholder="123 Main St, Suite 100" rows={2} />
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <Input {...register('shippingCity')} label="City" placeholder="City" />
                <Select {...register('shippingState')} label="State" placeholder="Select..." options={stateOptions} />
                <Input {...register('shippingZipCode')} label="ZIP Code" placeholder="12345" />
              </div>
              <Input {...register('shippingCountry')} label="Country" placeholder="US" />
            </div>
          </div>

          {/* Notes */}
          <div>
            <Textarea {...register('notes')} label="Notes" placeholder="Internal notes about this customer..." rows={2} />
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!customerToDelete}
        onClose={() => setCustomerToDelete(null)}
        onConfirm={() => customerToDelete && deleteMutation.mutate(customerToDelete.id)}
        title="Delete Customer"
        message={
          customerToDelete
            ? `Are you sure you want to delete "${customerToDelete.name}"? This is a soft delete and can be reversed by an administrator.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
