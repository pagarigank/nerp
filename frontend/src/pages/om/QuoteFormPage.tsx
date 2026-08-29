// <copyright file="QuoteFormPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { ArrowLeft, Plus, Trash2, AlertCircle } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Combobox } from '@components/ui/Combobox'
import { getErrorMessage } from '@api/client'
import { createSalesOrder, configureQuote, DEMO_COMPANY_ID } from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import { getItems } from '@api/inventory'
import type { ArCustomer } from '@/types/ar'
import type { ItemSummary } from '@/types/inventory'

const lineSchema = z.object({
  lineNumber: z.number().optional(),
  itemId: z.string().min(1, 'Select an item'),
  description: z.string().optional(),
  quantity: z.coerce.number().positive('Quantity must be greater than zero'),
  unitPrice: z.coerce.number().min(0, 'Enter a unit price'),
  unitOfMeasure: z.string().optional(),
  discountPercent: z.coerce.number().min(0).max(100).optional(),
})

const quoteSchema = z.object({
  customerId: z.string().min(1, 'Select a customer'),
  orderDate: z.string().min(1, 'Order date is required'),
  expiryDate: z.string().optional(),
  lines: z.array(lineSchema).min(1, 'At least one line item is required'),
})

type QuoteForm = z.infer<typeof quoteSchema>

export function QuoteFormPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)
  const [savedId, setSavedId] = useState<string | null>(null)

  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: () => getCustomers(),
  })
  const { data: items = [] } = useQuery({
    queryKey: ['inventory', 'items'],
    queryFn: () => getItems(),
  })

  const customerOptions = useMemo(
    () => customers.map((c: ArCustomer) => ({ value: c.id, label: c.name })),
    [customers]
  )
  const itemOptions = useMemo(
    () => items.map((i: ItemSummary) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })),
    [items]
  )

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    control,
    formState: { errors },
  } = useForm<QuoteForm>({
    resolver: zodResolver(quoteSchema),
    defaultValues: {
      customerId: '',
      orderDate: new Date().toISOString().slice(0, 10),
      expiryDate: '',
      lines: [{ lineNumber: 1, itemId: '', description: '', quantity: 1, unitPrice: 0, unitOfMeasure: 'EA', discountPercent: 0 }],
    },
  })

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' })

  async function onSubmit(data: QuoteForm) {
    setFormError(null)
    try {
      const payload = {
        orderNumber: null,
        companyId: DEMO_COMPANY_ID,
        customerId: data.customerId,
        orderDate: new Date(data.orderDate).toISOString(),
        shipToAddress: null,
        billToAddress: null,
        paymentTermId: null,
        salesRepId: null,
        shippingMethod: null,
        customerPoNumber: null,
        salesOrderTypeId: null,
        taxCodeId: null,
        taxExemptionCertificateId: null,
        lines: data.lines
          .filter((l) => l.itemId && l.itemId.trim() !== '')
          .map((l, i) => ({
            lineNumber: i + 1,
            itemId: l.itemId,
            description: l.description || '',
            quantity: l.quantity ?? 0,
            unitPrice: l.unitPrice ?? 0,
            unitOfMeasure: l.unitOfMeasure || 'EA',
            discountPercent: l.discountPercent ?? 0,
            taxPercent: 0,
            warehouseId: null,
            projectId: null,
            accountId: null,
            itemCategoryId: null,
            isDropShip: false,
            dropShipVendorId: null,
          })),
      }
      const id = await createSalesOrder(payload)
      await configureQuote(id, data.expiryDate || undefined)
      setSavedId(id)
      queryClient.invalidateQueries({ queryKey: ['om', 'salesOrders'] })
      queryClient.invalidateQueries({ queryKey: ['om', 'quotes'] })
      navigate('/om/quotes')
    } catch (e) {
      setFormError(getErrorMessage(e))
    }
  }

  return (
    <div className="space-y-6">
      <Button variant="ghost" size="sm" onClick={() => navigate('/om/quotes')}>
        <ArrowLeft className="h-4 w-4" /> Back to Quotes
      </Button>

      <h2 className="text-2xl font-bold text-gray-900 dark:text-white">New Quote</h2>

      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
        <Card>
          <CardHeader title="Quote Information" description="Customer and quote details" />
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              <div className="sm:col-span-2 lg:col-span-1">
                <Combobox
                  label="Customer"
                  placeholder="Select customer..."
                  options={customerOptions}
                  value={watch('customerId')}
                  onChange={(v) => setValue('customerId', v, { shouldValidate: true })}
                  required
                />
              </div>
              <Input {...register('orderDate')} type="date" label="Quote Date" required {...{ error: errors.orderDate?.message }} />
              <Input {...register('expiryDate')} type="date" label="Expiry (optional)" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader
            title="Line Items"
            description="Items quoted"
            action={
              <Button
                variant="outline"
                size="sm"
                type="button"
                onClick={() => append({ lineNumber: fields.length + 1, itemId: '', description: '', quantity: 1, unitPrice: 0, unitOfMeasure: 'EA', discountPercent: 0 })}
                leftIcon={<Plus className="h-4 w-4" />}
              >
                Add Line
              </Button>
            }
          />
          <CardContent className="space-y-4">
            {fields.map((field, index) => (
              <div key={field.id} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-6 gap-4 items-end">
                <div className="lg:col-span-2">
                  <Combobox
                    label="Item"
                    placeholder="Select item..."
                    options={itemOptions}
                    value={watch(`lines.${index}.itemId`)}
                    onChange={(v) => setValue(`lines.${index}.itemId`, v, { shouldValidate: true })}
                    required
                  />
                </div>
                <Input
                  {...register(`lines.${index}.quantity`)}
                  type="number"
                  step="0.01"
                  min="0.01"
                  label="Qty"
                  required
                />
                <Input
                  {...register(`lines.${index}.unitPrice`)}
                  type="number"
                  step="0.01"
                  min="0"
                  label="Unit Price"
                  required
                />
                <Input {...register(`lines.${index}.unitOfMeasure`)} label="UOM" placeholder="EA" />
                <div className="flex items-end">
                  {fields.length > 1 && (
                    <Button variant="ghost" size="sm" type="button" onClick={() => remove(index)} leftIcon={<Trash2 className="h-4 w-4" />}>
                      Remove
                    </Button>
                  )}
                </div>
              </div>
            ))}
            {typeof errors.lines?.message === 'string' && (
              <p className="text-sm text-red-600 dark:text-red-400" role="alert">{errors.lines.message}</p>
            )}
          </CardContent>
        </Card>

        <div className="flex justify-end gap-2">
          <Button variant="secondary" type="button" onClick={() => navigate('/om/quotes')}>
            Cancel
          </Button>
          <Button variant="primary" type="submit">
            Create Quote
          </Button>
        </div>
      </form>
    </div>
  )
}
