import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { ArrowLeft, Plus, Trash2, AlertCircle, Calculator } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { getErrorMessage } from '@api/client'
import { createSalesOrder, DEMO_COMPANY_ID, getShippingMethods, getSalesReps } from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import { getItems } from '@api/inventory'
import type { ArCustomer } from '@/types/ar'
import type { ItemSummary } from '@/types/inventory'

const lineSchema = z.object({
  lineNumber: z.number(),
  itemId: z.string().min(1, 'Item is required'),
  description: z.string().min(1, 'Description is required'),
  quantity: z.coerce.number().positive('Qty must be > 0'),
  unitPrice: z.coerce.number().min(0, 'Price cannot be negative'),
  unitOfMeasure: z.string().min(1, 'UOM is required'),
  discountPercent: z.coerce.number().min(0).max(100),
  taxPercent: z.coerce.number().min(0).max(100),
  warehouseId: z.string().optional(),
  projectId: z.string().optional(),
  accountId: z.string().optional(),
})

const orderSchema = z.object({
  orderNumber: z.string().trim().min(1, 'Order number is required'),
  customerId: z.string().min(1, 'Customer is required'),
  orderDate: z.string().min(1, 'Order date is required'),
  shipToAddress: z.string().optional(),
  billToAddress: z.string().optional(),
  paymentTermId: z.string().optional(),
  salesRepId: z.string().optional(),
  shippingMethod: z.string().optional(),
  customerPoNumber: z.string().optional(),
  lines: z.array(lineSchema).min(1, 'At least one line is required'),
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
  const watchedLines = watch('lines')

  // Lookups
  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: getCustomers,
  })

  const { data: items = [] } = useQuery({
    queryKey: ['inventory', 'items'],
    queryFn: getItems,
  })

  const { data: shippingMethods = [] } = useQuery({
    queryKey: ['om', 'shipping-methods'],
    queryFn: () => getShippingMethods(),
  })

  const { data: salesReps = [] } = useQuery({
    queryKey: ['om', 'sales-reps'],
    queryFn: () => getSalesReps(),
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

  const itemOptions = useMemo(
    () => items.map((i: ItemSummary) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })),
    [items]
  )

  const shippingOptions = useMemo(
    () => shippingMethods.map((m) => ({ value: m.code, label: `${m.code} - ${m.description}` })),
    [shippingMethods]
  )

  const salesRepOptions = useMemo(
    () => salesReps.map((r) => ({ value: r.id, label: `${r.code} - ${r.name}` })),
    [salesReps]
  )

  function selectCustomer(customer: ArCustomer) {
    setValue('customerId', customer.id, { shouldValidate: true })
    setCustomerSearch(customer.name)
    setShowCustomerDropdown(false)
  }

  function selectItem(index: number, itemId: string) {
    const item = items.find((i: ItemSummary) => i.id === itemId)
    if (item) {
      setValue(`lines.${index}.itemId`, itemId, { shouldValidate: true })
      setValue(`lines.${index}.description`, item.description)
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
    })
  }

  async function onSubmit(data: OrderForm) {
    setFormError(null)
    try {
      const payload = {
        orderNumber: data.orderNumber,
        companyId: DEMO_COMPANY_ID,
        customerId: data.customerId,
        orderDate: new Date(data.orderDate).toISOString(),
        shipToAddress: data.shipToAddress || null,
        billToAddress: data.billToAddress || null,
        paymentTermId: data.paymentTermId || null,
        salesRepId: data.salesRepId || null,
        shippingMethod: data.shippingMethod || null,
        customerPoNumber: data.customerPoNumber || null,
        lines: data.lines.map((l, i) => ({
          ...l,
          lineNumber: i + 1,
          warehouseId: l.warehouseId || null,
          projectId: l.projectId || null,
          accountId: l.accountId || null,
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

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
        {/* Header Section */}
        <Card>
          <CardHeader title="Order Information" description="Basic order details" />
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <Input
                {...register('orderNumber')}
                label="Order Number"
                placeholder="SO-1001"
                {...fieldError(errors.orderNumber?.message)}
                required
              />
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
                {...register('salesRepId')}
                label="Sales Rep"
                placeholder="Select sales rep..."
                options={salesRepOptions}
              />
              <Select
                {...register('shippingMethod')}
                label="Shipping Method"
                placeholder="Select shipping method..."
                options={shippingOptions}
              />
              <Input
                {...register('paymentTermId')}
                label="Payment Terms"
                placeholder="Payment terms ID"
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
            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
              <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-800">
                  <tr>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500 w-8">#</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Item</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Description</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Price</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">UOM</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Disc%</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Tax%</th>
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
                        <td className="px-3 py-2">
                          <select
                            {...register(`lines.${index}.itemId`)}
                            onChange={(e) => {
                              register(`lines.${index}.itemId`).onChange(e)
                              selectItem(index, e.target.value)
                            }}
                            className="w-full text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5"
                          >
                            <option value="">Select item...</option>
                            {itemOptions.map(opt => (
                              <option key={opt.value} value={opt.value}>{opt.label}</option>
                            ))}
                          </select>
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
                          <input
                            {...register(`lines.${index}.unitOfMeasure`)}
                            className="w-16 text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1.5"
                          />
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
