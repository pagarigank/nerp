import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { UomSelect } from '@components/ui/UomSelect'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getTransactions, createReceipt, createIssue, createTransfer, createAdjustment, getItems, getWarehouses, getItemUomConversions } from '@api/inventory'
import type { UomConversionDto } from '@/types/inventory'

const schema = z.object({
  transactionType: z.enum(['receipt', 'issue', 'transfer', 'adjustment']),
  itemId: z.string().min(1, 'Item is required'),
  warehouseId: z.string().min(1, 'Warehouse is required'),
  toWarehouseId: z.string().optional(),
  quantity: z.coerce.number().positive('Quantity must be > 0'),
  unitCost: z.coerce.number().min(0).optional(),
  unitOfMeasure: z.string().optional(),
  reasonCode: z.string().optional(),
  referenceNumber: z.string().optional(),
  notes: z.string().optional(),
})
type Form = z.infer<typeof schema>
const defaults: Form = { transactionType: 'receipt', itemId: '', warehouseId: '', toWarehouseId: '', quantity: 1, unitCost: 0, unitOfMeasure: '', reasonCode: '', referenceNumber: '', notes: '' }
function fieldError(message?: string) { return message ? { error: message } : {} }

export function TransactionsPage() {
  const qc = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)
  const [uomOptions, setUomOptions] = useState<{ value: string; label: string }[]>([])
  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<Form>({ resolver: zodResolver(schema), defaultValues: defaults })

  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })
  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'transactions'], queryFn: () => getTransactions() })

  const itemOptions = useMemo(() => items.map(i => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])
  const whOptions = useMemo(() => warehouses.map(w => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` })), [warehouses])

  const mutation = useMutation({
    mutationFn: (d: Form) => {
      const base = {
        companyId: '',
        itemId: d.itemId,
        warehouseId: d.warehouseId,
        quantity: d.quantity,
        unitOfMeasure: d.unitOfMeasure || undefined,
        referenceNumber: d.referenceNumber || undefined,
        notes: d.notes || undefined,
      }
      if (d.transactionType === 'receipt') return createReceipt({ ...base, unitCost: d.unitCost ?? 0 })
      if (d.transactionType === 'issue') return createIssue(base)
      if (d.transactionType === 'adjustment') return createAdjustment({ ...base, quantityAdjustment: d.quantity, reasonCode: d.reasonCode || 'ADJ' })
      return createTransfer({ ...base, fromWarehouseId: d.warehouseId, toWarehouseId: d.toWarehouseId || d.warehouseId })
    },
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'transactions'] }); qc.invalidateQueries({ queryKey: ['inventory', 'item-stock'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const close = () => setFormError(null)
  const onSubmit = (d: Form) => { setFormError(null); mutation.mutate(d) }
  const selectedType = watch('transactionType')

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5" /> <span className="text-sm">{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader title="Enter Transaction" description="Receipt, Issue, Transfer, or Adjustment" />
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Select {...register('transactionType')} label="Type"
                options={[
                  { value: 'receipt', label: 'Receipt' },
                  { value: 'issue', label: 'Issue' },
                  { value: 'transfer', label: 'Transfer' },
                  { value: 'adjustment', label: 'Adjustment' },
                ]} required />
              <Select {...register('itemId')} label="Item" options={itemOptions} {...fieldError(errors.itemId?.message)} required onChange={(e) => { register('itemId').onChange(e); const itemId = e.target.value; if (itemId) { const item = items.find((i: any) => i.id === itemId); const baseUom = item?.baseUnitOfMeasure || 'EA'; setUomOptions([{ value: baseUom, label: baseUom + ' (base)' }]); void getItemUomConversions(itemId).then((convs: UomConversionDto[]) => { const opts = [{ value: baseUom, label: baseUom + ' (base)' }]; for (const c of convs) { if (c.fromUOM === baseUom) opts.push({ value: c.toUOM, label: `${c.toUOM} (${c.conversionFactor}x)` }); } setUomOptions(opts); }).catch(() => {}); } }} />
              <Select {...register('warehouseId')} label={selectedType === 'transfer' ? 'From Warehouse' : 'Warehouse'} options={whOptions} {...fieldError(errors.warehouseId?.message)} required />
              {selectedType === 'transfer' && (
                <Select {...register('toWarehouseId')} label="To Warehouse" options={whOptions} required />
              )}
              <Input {...register('quantity')} label="Quantity" type="number" step="0.01" min="0.01" {...fieldError(errors.quantity?.message)} required />
              {selectedType === 'receipt' && (
                <Input {...register('unitCost')} label="Unit Cost" type="number" step="0.01" min="0" />
              )}
              {selectedType === 'adjustment' && (
                <Input {...register('reasonCode')} label="Reason Code" placeholder="e.g. DAMAGE" />
              )}
              <UomSelect {...register('unitOfMeasure')} />
              <Input {...register('referenceNumber')} label="Reference #" />
              <Input {...register('notes')} label="Notes" />
            </div>
            <div className="flex justify-end">
              <Button variant="primary" type="submit" leftIcon={<Plus className="h-4 w-4" />} isLoading={mutation.isPending}>
                Post {selectedType}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Recent Transactions" description={`${rows.length} transaction(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No transactions yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Date</th><th className="px-3 py-2 font-medium text-gray-500">Type</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Item</th><th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Qty</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Unit Cost</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.slice(0, 50).map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.transactionDate).toLocaleDateString()}</td>
                      <td className="px-3 py-3"><Badge variant="neutral" size="sm">{r.transactionType}</Badge></td>
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.itemId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.quantity}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.unitCost.toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
