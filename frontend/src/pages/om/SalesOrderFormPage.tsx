import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useForm, useFieldArray, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { ArrowLeft, Plus, Trash2, AlertCircle, Calculator } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { getErrorMessage } from '@api/client'
import { createSalesOrder, getShippingMethods, getSalesOrderTypes, evaluatePrice, getSalesReps, getTaxCodes, getTaxExemptions } from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import { getItems, getWarehouses, getItemUomConversions } from '@api/inventory'
import { getAccounts } from '@api/platform'
import { UomSelect } from '@components/ui/UomSelect'
import { getPaymentTerms } from '@api/ap'
import type { ArCustomer } from '@/types/ar'
import type { ItemSummary, UomConversionDto } from '@/types/inventory'
import type { PaymentTerm } from '@/types/ap'
import type { PricingResult, SalesRepSummary, TaxCodeSummary, TaxExemptionCertificateSummary } from '@/types/orderManagement'

// Lines are validated leniently here; requiredness (item/description/qty/uom) is
// enforced only for NON-blank lines in the orderSchema superRefine below. A blank
// line (no itemId) is allowed and pruned on submit, so an accidental empty line
// added via "Add Line" never blocks saving the order.
const lineSchema = z.object({
  lineNumber: z.number().optional(),
  itemId: z.string().optional(),
  description: z.string().optional(),
  quantity: z.coerce.number().min(0).optional(),
  unitPrice: z.coerce.number().min(0).optional(),
  unitOfMeasure: z.string().optional(),
  discountPercent: z.coerce.number().min(0).max(100).optional(),
  taxPercent: z.coerce.number().min(0).max(100).optional(),
  warehouseId: z.string().optional(),
  projectId: z.string().optional(),
  accountId: z.string().optional(),
  itemCategoryId: z.string().nullable().optional(),
  isDropShip: z.boolean().optional(),
})

const orderSchema = z.object({
  orderNumber: z.string().trim().optional(),
  customerId: z.string().min(1, 'Customer is required'),
  orderDate: z.string().min(1, 'Order date is required'),
  shipToAddress: z.string().optional(),
  billToAddress: z.string().optional(),
  paymentTermId: z.string().optional(),
  salesRepId: z.string().optional(),
  shippingMethod: z.string().optional(),
  customerPoNumber: z.string().optional(),
  salesOrderTypeId: z.string().optional(),
  taxCodeId: z.string().optional(),
  taxExemptionCertificateId: z.string().optional(),
  lines: z.array(lineSchema),
}).superRefine((data, ctx) => {
  const realLines = (data.lines ?? []).filter((l) => l.itemId && l.itemId.trim() !== '')
  if (realLines.length === 0) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['lines'], message: 'At least one line item with an item is required' })
  }
  data.lines?.forEach((line, i) => {
    if (!line.itemId || !line.itemId.trim()) return // blank line — pruned on submit
    if (!line.description || !line.description.trim()) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['lines', i, 'description'], message: 'Description is required' })
    }
    if (!line.quantity || line.quantity <= 0) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['lines', i, 'quantity'], message: 'Quantity must be greater than 0' })
    }
    if (!line.unitOfMeasure || !line.unitOfMeasure.trim()) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['lines', i, 'unitOfMeasure'], message: 'UOM is required' })
    }
  })
})

type OrderForm = z.infer<typeof orderSchema>
type LineForm = z.infer<typeof lineSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount)
}

export function SalesOrderFormPage() {
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const [customerSearch, setCustomerSearch] = useState('')
  const [showCustomerDropdown, setShowCustomerDropdown] = useState(false)
  const [lineUomOptions, setLineUomOptions] = useState<Record<number, { value: string; label: string }[]>>({})
  const [lineItemSearch, setLineItemSearch] = useState<Record<number, string>>({})
  const [showItemDropdown, setShowItemDropdown] = useState<Record<number, boolean>>({})

  const {
    register,
    handleSubmit,
    control,
    watch,
    setValue,
    formState: { errors },
  } = useForm<OrderForm>({
    resolver: zodResolver(orderSchema),
    defaultValues: {
      orderNumber: '',
      customerId: '',
      orderDate: new Date().toISOString().slice(0, 10),
      shipToAddress: '',
      billToAddress: '',
      paymentTermId: '',
      salesRepId: '',
      shippingMethod: '',
      customerPoNumber: '',
      salesOrderTypeId: '',
      taxCodeId: '',
      taxExemptionCertificateId: '',
      lines: [
        {
          lineNumber: 1,
          itemId: '',
          description: '',
          quantity: 1,
          unitPrice: 0,
          unitOfMeasure: 'EA',
          discountPercent: 0,
          taxPercent: 0,
        },
      ],
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })
  // useWatch (not watch) so deep setValue updates to array items re-render totals.
  const watchedLines = useWatch({ control, name: 'lines' })

  // Lookups
  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: getCustomers,
  })

  const { data: items = [] } = useQuery({
    queryKey: ['inventory', 'items'],
    queryFn: () => getItems(),
  })

  const { data: warehouses = [] } = useQuery({
    queryKey: ['inventory', 'warehouses'],
    queryFn: () => getWarehouses(),
  })

  const { data: glAccounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const { data: shippingMethods = [] } = useQuery({
    queryKey: ['om', 'shipping-methods'],
    queryFn: () => getShippingMethods(),
  })

  const { data: salesOrderTypes = [] } = useQuery({
    queryKey: ['om', 'sales-order-types'],
    queryFn: () => getSalesOrderTypes(),
  })

  const { data: paymentTerms = [] } = useQuery({
    queryKey: ['ap', 'paymentTerms'],
    queryFn: () => getPaymentTerms(true),
  })

  const { data: salesReps = [] } = useQuery({
    queryKey: ['om', 'salesReps'],
    queryFn: () => getSalesReps(),
  })

  const { data: taxCodes = [] } = useQuery({
    queryKey: ['om', 'taxCodes'],
    queryFn: () => getTaxCodes(),
  })

  const { data: taxExemptions = [] } = useQuery({
    queryKey: ['om', 'taxExemptions'],
    queryFn: () => getTaxExemptions(),
  })

  const filteredCustomers = useMemo(() => {
    const q = customerSearch.trim().toLowerCase()
    if (!q) return customers.slice(0, 10)
    return customers.filter(
      c => c.name.toLowerCase().includes(q) || c.customerId.toLowerCase().includes(q)
    ).slice(0, 10)
  }, [customers, customerSearch])

  const selectedCustomer = useMemo(
    () => customers.find(c => c.id === watch('customerId')),
    [customers, watch('customerId')]
  )

  // Running totals
  const totals = useMemo(() => {
    let subtotal = 0
    let totalDiscount = 0
    let totalTax = 0
    for (const line of watchedLines ?? []) {
      const lineTotal = (line.quantity || 0) * (line.unitPrice || 0)
      const discount = lineTotal * ((line.discountPercent || 0) / 100)
      const taxable = lineTotal - discount
      const tax = taxable * ((line.taxPercent || 0) / 100)
      subtotal += lineTotal
      totalDiscount += discount
      totalTax += tax
    }
    return { subtotal, totalDiscount, totalTax, grandTotal: subtotal - totalDiscount + totalTax }
  }, [watchedLines])

  // Fuzzy filter (matches item code and description, case-insensitive, gaps allowed)
  function fuzzyMatch(q: string, label: string): boolean {
    const needle = q.toLowerCase()
    if (!needle) return true
    const hay = label.toLowerCase()
    let i = 0
    for (const ch of hay) { if (ch === needle[i]) i++; if (i === needle.length) return true }
    return false
  }
  function filterItems(query: string): { value: string; label: string; item: ItemSummary }[] {
    return items
      .filter(i => fuzzyMatch(query, `${i.itemCode} - ${i.description}`))
      .map(i => ({ value: i.id, label: `${i.itemCode} - ${i.description}`, item: i }))
      .slice(0, 12)
  }

  const warehouseOptions = useMemo(
    () => warehouses.map((w: any) => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` })),
    [warehouses]
  )

  const accountOptions = useMemo(
    () => glAccounts.map((a: any) => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [glAccounts]
  )

  const shippingOptions = useMemo(
    () => shippingMethods.map((m) => ({ value: m.code, label: `${m.code} - ${m.description}` })),
    [shippingMethods]
  )

  const salesOrderTypeOptions = useMemo(
    () => salesOrderTypes.map((t) => ({ value: t.id, label: `${t.code} - ${t.description}` })),
    [salesOrderTypes]
  )

  const paymentTermOptions = useMemo(
    () => paymentTerms.map((t: PaymentTerm) => ({ value: t.id, label: `${t.name} (${t.dueDays}d, ${t.discountPercent}% disc/${t.discountDays}d)` })),
    [paymentTerms]
  )

  const salesRepOptions = useMemo(
    () => salesReps.map((r: SalesRepSummary) => ({ value: r.id, label: `${r.code} - ${r.name}` })),
    [salesReps]
  )

  const taxCodeOptions = useMemo(
    () => taxCodes.map((t: TaxCodeSummary) => ({ value: t.id, label: `${t.code} - ${t.description} (${t.rate}%)` })),
    [taxCodes]
  )

  const taxExemptionOptions = useMemo(
    () => taxExemptions
      .filter((e: TaxExemptionCertificateSummary) => e.isActive)
      .map((e: TaxExemptionCertificateSummary) => ({ value: e.id, label: `${e.certificateNumber} - ${e.jurisdiction}` })),
    [taxExemptions]
  )

  function selectCustomer(customer: ArCustomer) {
    setValue('customerId', customer.id, { shouldValidate: true })
    setCustomerSearch(customer.name)
    setShowCustomerDropdown(false)
    const formatAddress = (addr?: string | null, city?: string | null, state?: string | null, zip?: string | null, country?: string | null) => {
      const line1 = addr || ''
      const line2 = [city, state].filter(Boolean).join(', ')
      const line3 = [zip, country].filter(Boolean).join(' ')
      return [line1, line2, line3].filter(Boolean).join('\n')
    }
    const billing = formatAddress(customer.billingAddress, customer.billingCity, customer.billingState, customer.billingZipCode, customer.billingCountry)
    const shipping = formatAddress(customer.shippingAddress, customer.shippingCity, customer.shippingState, customer.shippingZipCode, customer.shippingCountry)
    if (billing) setValue('billToAddress', billing)
    if (shipping) setValue('shipToAddress', shipping)
    if (customer.defaultPaymentTermId) setValue('paymentTermId', customer.defaultPaymentTermId)
    if (customer.salesRepId) setValue('salesRepId', customer.salesRepId)
    if (customer.taxCodeId) setValue('taxCodeId', customer.taxCodeId)
    if (customer.taxExemptionCertificateId) setValue('taxExemptionCertificateId', customer.taxExemptionCertificateId)
  }

  function selectItem(index: number, selectedItemId: string) {
    const item = items.find((i: ItemSummary) => i.id === selectedItemId)
    if (!item) return
    setValue(`lines.${index}.itemId`, selectedItemId, { shouldValidate: true })
    setValue(`lines.${index}.description`, item.description)
    setValue(`lines.${index}.itemCategoryId`, item.itemCategoryId || null, { shouldValidate: false })
    const defaultUom = (item as any).defaultUnitOfMeasure || item.baseUnitOfMeasure || 'EA'
    setValue(`lines.${index}.unitOfMeasure`, defaultUom, { shouldValidate: true })
    const defaultWh = (item as any).defaultWarehouseId
    if (defaultWh) setValue(`lines.${index}.warehouseId`, defaultWh)
    const arAccount = (glAccounts as any[]).find((a: any) => a.accountNumber === '1200' || a.description?.toLowerCase().includes('receivable'))
    if (arAccount) setValue(`lines.${index}.accountId`, arAccount.id)
    // Load UOM conversions and build options for this line
    const baseUom = item.baseUnitOfMeasure || 'EA'
    setLineUomOptions(prev => ({ ...prev, [index]: [{ value: baseUom, label: baseUom + ' (base)' }] }))
    void getItemUomConversions(selectedItemId).then((convs: UomConversionDto[]) => {
      const opts = [{ value: baseUom, label: baseUom + ' (base)' }]
      for (const c of convs) {
        if (c.fromUOM === baseUom) opts.push({ value: c.toUOM, label: `${c.toUOM} (${c.conversionFactor}x)` })
      }
      setLineUomOptions(prev => ({ ...prev, [index]: opts }))
    }).catch(() => {
      setLineUomOptions(prev => ({ ...prev, [index]: [{ value: baseUom, label: baseUom + ' (base)' }] }))
    })
    // Auto-apply pricing rules for the selected item / customer / quantity.
    void autoApplyPricing(index, {
      itemId: item.id,
      itemCategoryId: item.itemCategoryId || null,
      quantity: Number(watch(`lines.${index}.quantity`)) || 1,
      baseUnitPrice: item.standardCost ?? 0,
    })
  }

  async function autoApplyPricing(
    index: number,
    ctx: { itemId: string; itemCategoryId: string; quantity: number; baseUnitPrice: number }
  ) {
    // Always surface a price immediately (item's standard cost) so the line
    // total is correct even before a customer is chosen. Customer-specific
    // rules will override this below when a customer is set.
    setValue(`lines.${index}.unitPrice`, ctx.baseUnitPrice, { shouldValidate: false })
    const customerId = watch('customerId')
    const orderDate = watch('orderDate')
    if (!customerId) return // customer must be chosen first for rules-based pricing
    try {
      const res = await evaluatePrice({
        companyId: currentCompanyId(),
        baseUnitPrice: ctx.baseUnitPrice,
        customerId,
        itemId: ctx.itemId,
        itemCategoryId: ctx.itemCategoryId || null,
        quantity: ctx.quantity,
        asOf: orderDate || new Date().toISOString(),
      })
      const result: PricingResult = res
      if (result) {
        setValue(`lines.${index}.unitPrice`, result.unitPrice, { shouldValidate: false })
        setValue(`lines.${index}.discountPercent`, result.discountPercent, { shouldValidate: false })
      }
    } catch {
      // leave manual entry as-is if pricing service unavailable
    }
  }

  function addLine() {
    const nextNum = (watchedLines?.length ?? 0) + 1
    append({
      lineNumber: nextNum,
      itemId: '',
      description: '',
      quantity: 1,
      unitPrice: 0,
      unitOfMeasure: 'EA',
      discountPercent: 0,
      taxPercent: 0,
      itemCategoryId: null,
    })
  }

  async function onSubmit(data: OrderForm) {
    setFormError(null)
    try {
      const payload = {
        orderNumber: data.orderNumber?.trim() || null,
        companyId: currentCompanyId(),
        customerId: data.customerId,
        orderDate: new Date(data.orderDate).toISOString(),
        shipToAddress: data.shipToAddress || null,
        billToAddress: data.billToAddress || null,
        paymentTermId: data.paymentTermId || null,
        salesRepId: data.salesRepId || null,
        shippingMethod: data.shippingMethod || null,
        customerPoNumber: data.customerPoNumber || null,
        salesOrderTypeId: data.salesOrderTypeId || null,
        taxCodeId: data.taxCodeId || null,
        taxExemptionCertificateId: data.taxExemptionCertificateId || null,
        lines: data.lines
          .filter((l) => l.itemId && l.itemId.trim() !== '')
          .map((l, i) => ({
            ...l,
            lineNumber: i + 1,
            quantity: l.quantity ?? 0,
            unitPrice: l.unitPrice ?? 0,
            discountPercent: l.discountPercent ?? 0,
            taxPercent: l.taxPercent ?? 0,
            warehouseId: l.warehouseId || null,
            projectId: l.projectId || null,
            accountId: l.accountId || null,
            itemCategoryId: l.itemCategoryId || null,
            isDropShip: l.isDropShip ?? false,
            dropShipVendorId: null,
          })),
      }
      const id = await createSalesOrder(payload)
      navigate(`/om/sales-orders/${id}`)
    } catch (e) {
      setFormError(getErrorMessage(e))
    }
  }

  return (
    <div className="space-y-6">
      <Button variant="ghost" size="sm" onClick={() => navigate('/om/sales-orders')}>
        <ArrowLeft className="h-4 w-4" /> Back to Sales Orders
      </Button>

      <h2 className="text-2xl font-bold text-gray-900 dark:text-white">New Sales Order</h2>

      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit, () => setFormError('Please correct the highlighted fields and try again.'))} className="space-y-6" noValidate>
        {/* Header Section */}
        <Card>
          <CardHeader title="Order Information" description="Basic order details" />
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <Input
                {...register('orderNumber')}
                label="Order Number"
                placeholder="Auto-generated"
                hint="Leave blank to auto-generate"
                {...fieldError(errors.orderNumber?.message)}
              />
              <input type="hidden" {...register('customerId')} />
              <div className="relative">
                <Input
                  value={selectedCustomer?.name ?? customerSearch}
                  onChange={(e) => {
                    setCustomerSearch(e.target.value)
                    setShowCustomerDropdown(true)
                    setValue('customerId', '', { shouldValidate: true })
                  }}
                  onFocus={() => setShowCustomerDropdown(true)}
                  onBlur={() => setTimeout(() => setShowCustomerDropdown(false), 200)}
                  label="Customer"
                  placeholder="Search customer..."
                  {...fieldError(errors.customerId?.message)}
                  required
                />
                {showCustomerDropdown && filteredCustomers.length > 0 && (
                  <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg max-h-60 overflow-auto">
                    {filteredCustomers.map(c => (
                      <button
                        key={c.id}
                        type="button"
                        className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                        onMouseDown={() => selectCustomer(c)}
                      >
                        <span className="font-medium">{c.name}</span>
                        <span className="ml-2 text-gray-500 text-xs">{c.customerId}</span>
                      </button>
                    ))}
                  </div>
                )}
              </div>
              <Input
                {...register('orderDate')}
                type="date"
                label="Order Date"
                {...fieldError(errors.orderDate?.message)}
                required
              />
              <Input
                {...register('customerPoNumber')}
                label="Customer PO #"
                placeholder="Customer's PO reference"
              />
            </div>
          </CardContent>
        </Card>

        {/* Shipping & Billing */}
        <Card>
          <CardHeader title="Shipping & Billing" description="Addresses, terms, and shipping" />
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Textarea
                {...register('shipToAddress')}
                label="Ship To Address"
                placeholder="Shipping address..."
                rows={3}
              />
              <Textarea
                {...register('billToAddress')}
                label="Bill To Address"
                placeholder="Billing address..."
                rows={3}
              />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <Select
                {...register('shippingMethod')}
                label="Shipping Method"
                placeholder="Select shipping method..."
                options={shippingOptions}
              />
              <Select
                {...register('salesOrderTypeId')}
                label="Order Type"
                placeholder="Select order type..."
                options={salesOrderTypeOptions}
              />
              <Select
                {...register('paymentTermId')}
                label="Payment Terms"
                placeholder="Select payment terms..."
                options={paymentTermOptions}
              />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <Select
                {...register('salesRepId')}
                label="Sales Rep"
                placeholder="Select sales rep..."
                options={salesRepOptions}
              />
              <Select
                {...register('taxCodeId')}
                label="Tax Code"
                placeholder="Select tax code..."
                options={taxCodeOptions}
              />
              <Select
                {...register('taxExemptionCertificateId')}
                label="Tax Exemption"
                placeholder="Select exemption certificate..."
                options={taxExemptionOptions}
              />
            </div>
          </CardContent>
        </Card>

        {/* Line Items */}
        <Card>
          <CardHeader
            title="Line Items"
            description={`${fields.length} line(s)`}
            action={
              <Button type="button" variant="outline" size="sm" onClick={addLine} leftIcon={<Plus className="h-4 w-4" />}>
                Add Line
              </Button>
            }
          />
          <CardContent>
            {errors.lines?.message && (
              <p className="text-sm text-red-600 dark:text-red-400 mb-2">{errors.lines.message}</p>
            )}
            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700 min-h-[200px] overflow-y-visible">
              <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-800">
                  <tr>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500 w-8">#</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500 min-w-[220px]">Item</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Description</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Price</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">UOM</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Warehouse</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Account</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Disc%</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Tax%</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Drop-Ship</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Line Total</th>
                    <th className="px-3 py-2 w-10" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {fields.map((field, index) => {
                    const line = watchedLines?.[index]
                    const lineTotal = ((line?.quantity ?? 0) * (line?.unitPrice ?? 0)) * (1 - (line?.discountPercent ?? 0) / 100)
                    return (
                      <tr key={field.id} className="bg-white dark:bg-gray-900">
                        <td className="px-3 py-2 text-sm text-gray-500">{index + 1}</td>
                        <td className="px-3 py-2 relative">
                          <input
                            type="text"
                            value={lineItemSearch[index] || ''}
                            onChange={(e) => setLineItemSearch(prev => ({ ...prev, [index]: e.target.value }))}
                            onFocus={() => setShowItemDropdown(idx => ({ ...idx, [index]: true }))}
                            onBlur={() => setTimeout(() => setShowItemDropdown(idx => ({ ...idx, [index]: false })), 200)}
                            placeholder="Start typing item code or name..."
                            className="w-full min-w-[180px] text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5"
                          />
                          {(showItemDropdown[index] && filterItems(lineItemSearch[index] || '').length > 0) && (
                            <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg max-h-60 overflow-auto">
                              {filterItems(lineItemSearch[index] || '').map(opt => (
                                <button
                                  key={opt.value}
                                  type="button"
                                  className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                                  onMouseDown={(e) => {
                                    e.preventDefault()
                                    selectItem(index, opt.value)
                                    setLineItemSearch(prev => ({ ...prev, [index]: opt.label }))
                                    setShowItemDropdown(d => ({ ...d, [index]: false }))
                                  }}
                                >
                                  <span className="font-medium">{opt.item.itemCode}</span>
                                  <span className="ml-2 text-gray-500 text-xs">{opt.item.description}</span>
                                </button>
                              ))}
                            </div>
                          )}
                          {showItemDropdown[index] && lineItemSearch[index] && filterItems(lineItemSearch[index] || '').length === 0 && (
                            <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg max-h-60 overflow-auto">
                              <div className="px-3 py-2 text-sm text-gray-500 dark:text-gray-400">No items found</div>
                            </div>
                          )}
                          <input type="hidden" {...register(`lines.${index}.itemId`)} />
                        </td>
                        <td className="px-3 py-2">
                          <input
                            {...register(`lines.${index}.description`)}
                            className="w-full text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5"
                          />
                        </td>
                        <td className="px-3 py-2">
                          <input
                            type="number"
                            step="0.01"
                            {...register(`lines.${index}.quantity`)}
                            onChange={(e) => {
                              register(`lines.${index}.quantity`).onChange(e)
                              const item = items.find((i: ItemSummary) => i.id === line?.itemId)
                              if (item) {
                                void autoApplyPricing(index, {
                                  itemId: item.id,
                                  itemCategoryId: item.itemCategoryId || null,
                                  quantity: Number(e.target.value) || 1,
                                  baseUnitPrice: item.standardCost ?? 0,
                                })
                              }
                            }}
                            className="w-20 text-sm text-right rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5 tabular-nums"
                          />
                        </td>
                        <td className="px-3 py-2">
                          <input
                            type="number"
                            step="0.01"
                            {...register(`lines.${index}.unitPrice`)}
                            className="w-24 text-sm text-right rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5 tabular-nums"
                          />
                        </td>
                        <td className="px-3 py-2">
                          <UomSelect
                            {...register(`lines.${index}.unitOfMeasure`)}
                            className="w-20 text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-1 py-1.5"
                          />
                        </td>
                        <td className="px-3 py-2">
                          <select
                            {...register(`lines.${index}.warehouseId`)}
                            className="w-32 text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5"
                          >
                            <option value="">None</option>
                            {warehouseOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                          </select>
                        </td>
                        <td className="px-3 py-2">
                          <select
                            {...register(`lines.${index}.accountId`)}
                            className="w-32 text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5"
                          >
                            <option value="">None</option>
                            {accountOptions.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                          </select>
                        </td>
                        <td className="px-3 py-2">
                          <input
                            type="number"
                            step="0.01"
                            {...register(`lines.${index}.discountPercent`)}
                            className="w-16 text-sm text-right rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5 tabular-nums"
                          />
                        </td>
                        <td className="px-3 py-2">
                          <input
                            type="number"
                            step="0.01"
                            {...register(`lines.${index}.taxPercent`)}
                            className="w-16 text-sm text-right rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5 tabular-nums"
                          />
                        </td>
                        <td className="px-3 py-2">
                          <label className="flex items-center gap-1 text-sm">
                            <input type="checkbox" {...register(`lines.${index}.isDropShip`)} className="h-3.5 w-3.5" />
                            <span className="text-gray-500">Drop</span>
                          </label>
                        </td>
                        <td className="px-3 py-2 text-right text-sm font-medium tabular-nums text-gray-900 dark:text-white">
                          {formatCurrency(lineTotal)}
                        </td>
                        <td className="px-3 py-2">
                          {fields.length > 1 && (
                            <button
                              type="button"
                              onClick={() => remove(index)}
                              className="text-red-500 hover:text-red-700 dark:hover:text-red-400"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>

        {/* Totals */}
        <Card>
          <CardContent>
            <div className="flex justify-end">
              <div className="w-80 space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Subtotal:</span>
                  <span className="font-medium tabular-nums">{formatCurrency(totals.subtotal)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Discount:</span>
                  <span className="font-medium tabular-nums text-red-600">-{formatCurrency(totals.totalDiscount)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">Tax:</span>
                  <span className="font-medium tabular-nums">{formatCurrency(totals.totalTax)}</span>
                </div>
                <div className="border-t border-gray-200 dark:border-gray-700 pt-2 flex justify-between">
                  <span className="text-base font-bold text-gray-900 dark:text-white">Grand Total:</span>
                  <span className="text-base font-bold tabular-nums text-gray-900 dark:text-white">{formatCurrency(totals.grandTotal)}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Actions */}
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={() => navigate('/om/sales-orders')}>
            Cancel
          </Button>
          <Button type="submit" variant="primary" leftIcon={<Calculator className="h-4 w-4" />}>
            Create Sales Order
          </Button>
        </div>
      </form>
    </div>
  )
}
