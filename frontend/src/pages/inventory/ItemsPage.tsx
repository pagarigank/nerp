import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle, Pencil } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getItems, createItem, updateItem, getItemCategories, getItemVendors, createItemVendor, getItemGlAccounts, upsertItemGlAccounts, getItemUomConversions, createItemUomConversion, deleteItemUomConversion } from '@api/inventory'
import { getVendors } from '@api/ap'
import { getAccounts } from '@api/platform'
import type { ItemSummary, CreateItemRequest, UpdateItemRequest, ItemCategorySummary, ItemVendorAssignmentDto, ItemGlAccountDefaultsDto, UomConversionDto } from '@/types/inventory'

const itemTypeOptions = [
  { value: '1', label: 'Inventory' },
  { value: '2', label: 'Non-Inventory' },
  { value: '3', label: 'Service' },
]

const itemTypeLabels: Record<string, string> = {
  'Inventory': 'Inventory',
  'Non-Inventory': 'Non-Inventory',
  'Service': 'Service',
  '1': 'Inventory',
  '2': 'Non-Inventory',
  '3': 'Service',
}

const costingMethodOptions = [
  { value: '1', label: 'FIFO' },
  { value: '2', label: 'LIFO' },
  { value: '3', label: 'Average' },
  { value: '4', label: 'Standard' },
  { value: '5', label: 'Lot Specific' },
]

const costingMethodLabels: Record<string, string> = {
  'FIFO': 'FIFO',
  'LIFO': 'LIFO',
  'Average': 'Average',
  'Standard': 'Standard',
  'Lot Specific': 'Lot Specific',
  '1': 'FIFO',
  '2': 'LIFO',
  '3': 'Average',
  '4': 'Standard',
  '5': 'Lot Specific',
}

const statusOptions = [
  { value: '1', label: 'Active' },
  { value: '2', label: 'Inactive' },
  { value: '3', label: 'Discontinued' },
]

const itemSchema = z.object({
  itemCode: z.string().trim().min(1, 'Item code is required'),
  description: z.string().trim().min(1, 'Description is required'),
  longDescription: z.string().optional(),
  itemType: z.string().min(1, 'Item type is required'),
  baseUnitOfMeasure: z.string().trim().min(1, 'Unit of measure is required'),
  costingMethod: z.string().min(1, 'Costing method is required'),
  itemCategoryId: z.string().min(1, 'Category is required'),
  standardCost: z.coerce.number().min(0).optional(),
  reorderPoint: z.coerce.number().min(0).optional(),
  reorderQuantity: z.coerce.number().min(0).optional(),
  safetyStock: z.coerce.number().min(0).optional(),
  leadTimeDays: z.coerce.number().int().min(0).optional(),
  weight: z.coerce.number().min(0).optional(),
  length: z.coerce.number().min(0).optional(),
  width: z.coerce.number().min(0).optional(),
  height: z.coerce.number().min(0).optional(),
  weightUnit: z.string().optional(),
  isHazardousMaterial: z.boolean().optional(),
  hazardClass: z.string().optional(),
  countryOfOrigin: z.string().optional(),
  hsCode: z.string().optional(),
  storageCondition: z.string().optional(),
  isKit: z.boolean().optional(),
  isLotControlled: z.boolean().optional(),
  isSerialControlled: z.boolean().optional(),
})

type ItemForm = z.infer<typeof itemSchema>

const defaultValues: ItemForm = {
  itemCode: '',
  description: '',
  longDescription: '',
  itemType: '1',
  baseUnitOfMeasure: 'EA',
  costingMethod: '4',
  itemCategoryId: '',
  standardCost: undefined,
  reorderPoint: undefined,
  reorderQuantity: undefined,
  safetyStock: undefined,
  leadTimeDays: undefined,
  weight: undefined,
  length: undefined,
  width: undefined,
  height: undefined,
  weightUnit: '',
  isHazardousMaterial: false,
  hazardClass: '',
  countryOfOrigin: '',
  hsCode: '',
  storageCondition: '',
  isKit: false,
  isLotControlled: false,
  isSerialControlled: false,
}

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function ItemsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [selected, setSelected] = useState<ItemSummary | null>(null)
  const [editItem, setEditItem] = useState<ItemSummary | null>(null)
  const [detailTab, setDetailTab] = useState<'vendors' | 'gl' | 'uom'>('vendors')
  const [detailError, setDetailError] = useState<string | null>(null)
  const [vendorForm, setVendorForm] = useState({ vendorId: '', vendorItemCode: '', vendorCost: 0, leadTimeDays: 0, isPrimaryVendor: false })
  const [glForm, setGlForm] = useState<Record<string, string>>({})
  const [uomConversions, setUomConversions] = useState<UomConversionDto[]>([])
  const [uomForm, setUomForm] = useState({ fromUOM: '', toUOM: '', conversionFactor: 1 })
  const { data: glAccounts = [] } = useQuery({ queryKey: ['platform', 'accounts'], queryFn: () => getAccounts() })
  const glAccountOptions = useMemo(() => glAccounts.map((a) => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })), [glAccounts])

  async function loadUomConversions(itemId: string) {
    try {
      const data = await getItemUomConversions(itemId)
      setUomConversions(data)
    } catch {
      setUomConversions([])
    }
  }

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<ItemForm>({
    resolver: zodResolver(itemSchema),
    defaultValues,
  })

  const { data: items = [], isLoading } = useQuery({
    queryKey: ['inventory', 'items'],
    queryFn: () => getItems(),
  })

  const { data: categories = [] } = useQuery({
    queryKey: ['inventory', 'item-categories'],
    queryFn: () => getItemCategories(),
  })

  const categoryOptions = useMemo(
    () =>
      categories.map((c: ItemCategorySummary) => ({
        value: c.id,
        label: `${c.categoryCode} - ${c.description}`,
      })),
    [categories],
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['inventory', 'items'] })
  }

  const { data: vendors = [], refetch: refetchVendors } = useQuery({
    queryKey: ['inventory', 'item-vendors', selected?.id],
    queryFn: () => getItemVendors(selected!.id),
    enabled: selected !== null,
  })
  const { data: gl, refetch: refetchGl } = useQuery({
    queryKey: ['inventory', 'item-gl', selected?.id],
    queryFn: () => getItemGlAccounts(selected!.id),
    enabled: selected !== null,
  })
  const { data: vendorList = [] } = useQuery({ queryKey: ['ap', 'vendors-mini'], queryFn: () => getVendors() })
  const vendorSelectOptions = vendorList.map((v: any) => ({ value: v.id, label: `${v.vendorCode ?? v.name} - ${v.name}` }))

  const addVendorMut = useMutation({
    mutationFn: () => createItemVendor(selected!.id, { vendorId: vendorForm.vendorId, vendorItemCode: vendorForm.vendorItemCode || null, vendorCost: vendorForm.vendorCost, leadTimeDays: vendorForm.leadTimeDays, isPrimaryVendor: vendorForm.isPrimaryVendor }),
    onSuccess: () => { refetchVendors(); setVendorForm({ vendorId: '', vendorItemCode: '', vendorCost: 0, leadTimeDays: 0, isPrimaryVendor: false }) },
    onError: (e) => setDetailError(getErrorMessage(e)),
  })
  const saveGlMut = useMutation({
    mutationFn: () => upsertItemGlAccounts(selected!.id, glForm),
    onSuccess: () => { refetchGl(); setDetailError(null) },
    onError: (e) => setDetailError(getErrorMessage(e)),
  })

  const openDetail = (item: ItemSummary) => {
    setSelected(item)
    setDetailTab('vendors')
    setDetailError(null)
    setGlForm({})
  }

  const openEdit = (item: ItemSummary) => {
    setEditItem(item)
    setFormError(null)
    reset({
      itemCode: item.itemCode,
      description: item.description,
      longDescription: item.longDescription ?? '',
      itemType: itemTypeLabels[item.itemType] ? String(Object.entries(itemTypeLabels).find(([k, v]) => v === item.itemType)?.[0] ?? '1') : '1',
      baseUnitOfMeasure: item.baseUnitOfMeasure,
      costingMethod: item.costingMethod ? (costingMethodLabels[item.costingMethod] ? String(Object.entries(costingMethodLabels).find(([k, v]) => v === item.costingMethod)?.[0] ?? '4') : '4') : '4',
      itemCategoryId: '',
      standardCost: item.standardCost ?? undefined,
      reorderPoint: item.reorderPoint ?? undefined,
      reorderQuantity: item.reorderQuantity ?? undefined,
      safetyStock: item.safetyStock ?? undefined,
      leadTimeDays: item.leadTimeDays ?? undefined,
      weight: undefined,
      length: undefined,
      width: undefined,
      height: undefined,
      weightUnit: '',
      isHazardousMaterial: false,
      hazardClass: '',
      countryOfOrigin: '',
      hsCode: '',
      storageCondition: '',
      isKit: item.isKit ?? false,
      isLotControlled: item.isLotControlled ?? false,
      isSerialControlled: item.isSerialControlled ?? false,
    })
    setIsModalOpen(true)
  }

  const createMutation = useMutation({
    mutationFn: (data: CreateItemRequest) => createItem(data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: (err) => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateItemRequest }) => updateItem(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: (err) => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setFormError(null)
    setEditItem(null)
    reset(defaultValues)
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditItem(null)
    setFormError(null)
  }

  const onSubmit = (data: ItemForm) => {
    setFormError(null)
    if (editItem) {
      const payload: UpdateItemRequest = {
        description: data.description,
        longDescription: data.longDescription || null,
        standardCost: data.standardCost ?? null,
        reorderPoint: data.reorderPoint ?? null,
        reorderQuantity: data.reorderQuantity ?? null,
        safetyStock: data.safetyStock ?? null,
        leadTimeDays: data.leadTimeDays ?? null,
        weight: data.weight ?? null,
        length: data.length ?? null,
        width: data.width ?? null,
        height: data.height ?? null,
        weightUnit: data.weightUnit || null,
        isHazardousMaterial: data.isHazardousMaterial ?? false,
        hazardClass: data.hazardClass || null,
        countryOfOrigin: data.countryOfOrigin || null,
        hsCode: data.hsCode || null,
        storageCondition: data.storageCondition || null,
        isKit: data.isKit ?? false,
        isLotControlled: data.isLotControlled ?? false,
        isSerialControlled: data.isSerialControlled ?? false,
      }
      updateMutation.mutate({ id: editItem.id, data: payload })
    } else {
      const payload: CreateItemRequest = {
        itemCode: data.itemCode,
        description: data.description,
        longDescription: data.longDescription || null,
        companyId: '',
        itemType: Number(data.itemType),
        baseUnitOfMeasure: data.baseUnitOfMeasure,
        costingMethod: Number(data.costingMethod),
        itemCategoryId: data.itemCategoryId,
        standardCost: data.standardCost ?? null,
        reorderPoint: data.reorderPoint ?? null,
        reorderQuantity: data.reorderQuantity ?? null,
        safetyStock: data.safetyStock ?? null,
        leadTimeDays: data.leadTimeDays ?? null,
        weight: data.weight ?? null,
        length: data.length ?? null,
        width: data.width ?? null,
        height: data.height ?? null,
        weightUnit: data.weightUnit || null,
        isHazardousMaterial: data.isHazardousMaterial ?? false,
        hazardClass: data.hazardClass || null,
        countryOfOrigin: data.countryOfOrigin || null,
        hsCode: data.hsCode || null,
        storageCondition: data.storageCondition || null,
        isKit: data.isKit ?? false,
        isLotControlled: data.isLotControlled ?? false,
        isSerialControlled: data.isSerialControlled ?? false,
      }
      createMutation.mutate(payload)
    }
  }

  const filteredItems = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return items
    return items.filter(
      (i: ItemSummary) =>
        i.itemCode.toLowerCase().includes(q) || i.description.toLowerCase().includes(q),
    )
  }, [items, search])

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
          title="Inventory Items"
          description={`${items.length} item(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Item
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by code or description..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search items"
            />
          </div>

          {isLoading ? (
            <p className="text-sm text-gray-500 py-8 text-center">Loading items…</p>
          ) : filteredItems.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No items match your search.' : 'No items yet. Create your first item.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">UOM</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Costing</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Std Cost</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Lot/Serial</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredItems.map((item: ItemSummary) => (
                    <tr key={item.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{item.itemCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        <p className="font-medium text-gray-900 dark:text-white">{item.description}</p>
                        {item.longDescription && (
                          <p className="text-xs text-gray-500 dark:text-gray-400">{item.longDescription}</p>
                        )}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{itemTypeLabels[item.itemType] ?? item.itemType}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{item.baseUnitOfMeasure}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{costingMethodLabels[item.costingMethod ?? ''] ?? item.costingMethod ?? '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300 text-right">
                        {item.standardCost != null ? `$${item.standardCost.toFixed(2)}` : '—'}
                      </td>
                      <td className="px-3 py-3 text-gray-500 dark:text-gray-400 text-xs">
                        {item.isLotControlled && <Badge variant="info" size="sm">LOT</Badge>}
                        {item.isSerialControlled && <Badge variant="warning" size="sm">SN</Badge>}
                        {!item.isLotControlled && !item.isSerialControlled && '—'}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={item.status === 'Active' ? 'success' : 'neutral'} size="sm" dot>
                          {item.status}
                        </Badge>
                      </td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          <Button size="sm" variant="outline" onClick={() => openDetail(item)}>View</Button>
                          <Button size="sm" variant="outline" onClick={() => openEdit(item)}><Pencil className="h-3.5 w-3.5" /> Edit</Button>
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

      {/* Create / Edit Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title={editItem ? `Edit Item: ${editItem.itemCode}` : 'New Item'}
        description={editItem ? 'Update inventory item details' : 'Add a new inventory item master'}
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
              {editItem ? 'Update Item' : 'Create Item'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('itemCode')}
              label="Item Code"
              placeholder="e.g. ITEM-0001"
              {...fieldError(errors.itemCode?.message)}
              required
              disabled={!!editItem}
            />
            <Input
              {...register('description')}
              label="Description"
              placeholder="Item description"
              {...fieldError(errors.description?.message)}
              required
            />
            <Input {...register('longDescription')} label="Long Description" placeholder="Extended description" />
            <Select
              {...register('itemType')}
              label="Item Type"
              placeholder="Select type..."
              options={itemTypeOptions}
              {...fieldError(errors.itemType?.message)}
              required
              disabled={!!editItem}
            />
            <Input
              {...register('baseUnitOfMeasure')}
              label="Base Unit of Measure"
              placeholder="e.g. EA, CS, PL"
              {...fieldError(errors.baseUnitOfMeasure?.message)}
              required
            />
            <Select
              {...register('costingMethod')}
              label="Costing Method"
              placeholder="Select method..."
              options={costingMethodOptions}
              {...fieldError(errors.costingMethod?.message)}
              required
              disabled={!!editItem}
            />
            {!editItem && (
              <Select
                {...register('itemCategoryId')}
                label="Item Category"
                placeholder="Select category..."
                options={categoryOptions}
                {...fieldError(errors.itemCategoryId?.message)}
                required
              />
            )}
            <Input
              {...register('standardCost')}
              label="Standard Cost"
              type="number"
              step="0.01"
              min="0"
              placeholder="0.00"
              {...fieldError(errors.standardCost?.message)}
            />
            <Input
              {...register('reorderPoint')}
              label="Reorder Point"
              type="number"
              step="0.01"
              min="0"
              placeholder="0"
              {...fieldError(errors.reorderPoint?.message)}
            />
            <Input
              {...register('reorderQuantity')}
              label="Reorder Quantity"
              type="number"
              step="0.01"
              min="0"
              placeholder="0"
              {...fieldError(errors.reorderQuantity?.message)}
            />
            <Input
              {...register('safetyStock')}
              label="Safety Stock"
              type="number"
              step="0.01"
              min="0"
              placeholder="0"
              {...fieldError(errors.safetyStock?.message)}
            />
            <Input
              {...register('leadTimeDays')}
              label="Lead Time (days)"
              type="number"
              step="1"
              min="0"
              placeholder="0"
              {...fieldError(errors.leadTimeDays?.message)}
            />
          </div>

          {/* Control Flags */}
          <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">Control Flags</h4>
            <div className="flex flex-wrap gap-4">
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                <input type="checkbox" {...register('isLotControlled')} className="h-4 w-4" /> Lot Controlled
              </label>
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                <input type="checkbox" {...register('isSerialControlled')} className="h-4 w-4" /> Serial Controlled
              </label>
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                <input type="checkbox" {...register('isKit')} className="h-4 w-4" /> Kit / Bundled Item
              </label>
            </div>
          </div>

          {/* Physical Attributes */}
          <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
            <h4 className="text-sm font-medium text-gray-900 dark:text-white mb-3">Physical Attributes</h4>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <Input {...register('weight')} label="Weight" type="number" step="0.01" min="0" placeholder="0" />
              <Input {...register('length')} label="Length" type="number" step="0.01" min="0" placeholder="0" />
              <Input {...register('width')} label="Width" type="number" step="0.01" min="0" placeholder="0" />
              <Input {...register('height')} label="Height" type="number" step="0.01" min="0" placeholder="0" />
              <Input {...register('weightUnit')} label="Weight Unit" placeholder="LB, KG, OZ" />
              <Input {...register('countryOfOrigin')} label="Country of Origin" placeholder="US, CN, etc." />
              <Input {...register('hsCode')} label="HS Code / Customs" placeholder="Harmonized code" />
              <Input {...register('storageCondition')} label="Storage Condition" placeholder="e.g. Climate controlled" />
            </div>
            <div className="mt-3">
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                <input type="checkbox" {...register('isHazardousMaterial')} className="h-4 w-4" /> Hazardous Material
              </label>
              {watch('isHazardousMaterial') && (
                <div className="mt-2">
                  <Input {...register('hazardClass')} label="Hazard Class" placeholder="e.g. Class 3 Flammable" />
                </div>
              )}
            </div>
          </div>
        </form>
      </Modal>

      {/* Detail Modal */}
      <Modal
        isOpen={selected !== null}
        onClose={() => setSelected(null)}
        title={selected ? `Item: ${selected.itemCode}` : 'Item'}
        description="Vendor assignments & GL account defaults"
        size="lg"
        footer={<Button variant="secondary" onClick={() => setSelected(null)}>Close</Button>}
      >
        {detailError && (
          <div className="flex items-center gap-2 p-3 mb-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
            <AlertCircle className="h-4 w-4" /> <span>{detailError}</span>
          </div>
        )}
        <div className="flex gap-2 mb-4">
          <Button size="sm" variant={detailTab === 'vendors' ? 'primary' : 'outline'} onClick={() => setDetailTab('vendors')}>Vendor Assignments</Button>
          <Button size="sm" variant={detailTab === 'gl' ? 'primary' : 'outline'} onClick={() => setDetailTab('gl')}>GL Account Defaults</Button>
          <Button size="sm" variant={detailTab === 'uom' ? 'primary' : 'outline'} onClick={() => { setDetailTab('uom'); if (selected) void loadUomConversions(selected.id) }}>UOM Conversions</Button>
        </div>

        {detailTab === 'vendors' && (
          <div className="space-y-4">
            <div className="border rounded-lg divide-y divide-gray-100 dark:divide-gray-700/60">
              {vendors.length === 0 ? <p className="p-4 text-sm text-gray-500">No vendor assignments.</p> :
                vendors.map((v: ItemVendorAssignmentDto) => (
                  <div key={v.id} className="p-3 flex justify-between text-sm">
                    <span className="font-medium text-gray-900 dark:text-white">{v.vendorItemCode ?? v.vendorId.slice(0, 8)}</span>
                    <span className="text-gray-500">{v.isPrimaryVendor ? 'Primary' : 'Alt'} • {(v.vendorCost ?? 0).toFixed(2)}{v.leadTimeDays != null ? ` • ${v.leadTimeDays}d` : ''}</span>
                  </div>
                ))}
            </div>
            <div className="grid grid-cols-2 gap-2 items-end">
              <Select value={vendorForm.vendorId} onChange={e => setVendorForm(f => ({ ...f, vendorId: e.target.value }))} options={[{ value: '', label: 'Select vendor...' }, ...vendorSelectOptions]} label="Vendor" />
              <Input value={vendorForm.vendorItemCode} onChange={e => setVendorForm(f => ({ ...f, vendorItemCode: e.target.value }))} label="Vendor Item Code" />
              <Input type="number" step="0.01" min="0" value={String(vendorForm.vendorCost)} onChange={e => setVendorForm(f => ({ ...f, vendorCost: Number(e.target.value) }))} label="Vendor Cost" />
              <Input type="number" min="0" value={String(vendorForm.leadTimeDays)} onChange={e => setVendorForm(f => ({ ...f, leadTimeDays: Number(e.target.value) }))} label="Lead Time (days)" />
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 col-span-2">
                <input type="checkbox" checked={vendorForm.isPrimaryVendor} onChange={e => setVendorForm(f => ({ ...f, isPrimaryVendor: e.target.checked }))} /> Primary vendor
              </label>
            </div>
            <Button size="sm" variant="primary" onClick={() => addVendorMut.mutate()} disabled={addVendorMut.isPending || !vendorForm.vendorId}>Add Vendor</Button>
          </div>
        )}

        {detailTab === 'gl' && (
          <div className="space-y-3">
            {([
              ['inventoryAssetAccountId', 'Inventory Asset'],
              ['cogsAccountId', 'COGS'],
              ['varianceAccountId', 'Variance'],
              ['purchasePriceVarianceAccountId', 'Purchase Price Variance'],
              ['salesRevenueAccountId', 'Sales Revenue'],
              ['inventoryAdjustmentAccountId', 'Inventory Adjustment'],
              ['landedCostClearingAccountId', 'Landed Cost Clearing'],
            ] as [keyof ItemGlAccountDefaultsDto, string][]).map(([k, label]) => (
              <Select
                key={k}
                value={(gl?.[k] as string) ?? glForm[k as string] ?? ''}
                onChange={e => setGlForm(f => ({ ...f, [k]: e.target.value }))}
                label={label}
                placeholder="Select account..."
                options={[{ value: '', label: '— None —' }, ...glAccountOptions]}
              />
            ))}
            <Button size="sm" variant="primary" onClick={() => saveGlMut.mutate()} disabled={saveGlMut.isPending}>Save GL Defaults</Button>
          </div>
        )}

        {detailTab === 'uom' && selected && (
          <div className="space-y-4">
            <p className="text-xs text-gray-500">
              Base UOM: <strong>{selected.baseUnitOfMeasure}</strong> — Define conversions so quantities are automatically converted when ordering/receiving in different UOMs.
            </p>
            <div className="border rounded-lg divide-y divide-gray-100 dark:divide-gray-700/60">
              {uomConversions.length === 0 ? (
                <p className="p-4 text-sm text-gray-500">No UOM conversions defined.</p>
              ) : (
                uomConversions.map((c: UomConversionDto) => (
                  <div key={c.id} className="p-3 flex justify-between items-center text-sm">
                    <span className="font-medium text-gray-900 dark:text-white">
                      1 {c.fromUOM} = {c.conversionFactor} {c.toUOM}
                    </span>
                    <button
                      onClick={() => void deleteItemUomConversion(selected.id, c.id).then(() => loadUomConversions(selected.id))}
                      className="text-red-500 hover:text-red-700 text-xs"
                    >Delete</button>
                  </div>
                ))
              )}
            </div>
            <div className="grid grid-cols-3 gap-2 items-end">
              <Input value={uomForm.fromUOM} onChange={e => setUomForm(f => ({ ...f, fromUOM: e.target.value.toUpperCase() }))} label="From UOM" placeholder="e.g. CS" />
              <Input value={uomForm.toUOM} onChange={e => setUomForm(f => ({ ...f, toUOM: e.target.value.toUpperCase() }))} label="To UOM" placeholder="e.g. EA" />
              <Input type="number" step="0.0001" min="0.0001" value={String(uomForm.conversionFactor)} onChange={e => setUomForm(f => ({ ...f, conversionFactor: Number(e.target.value) }))} label="Factor" />
            </div>
            <Button
              size="sm" variant="primary"
              onClick={() => {
                if (!selected || !uomForm.fromUOM || !uomForm.toUOM) return
                createItemUomConversion(selected.id, uomForm).then(() => {
                  setUomForm({ fromUOM: '', toUOM: '', conversionFactor: 1 })
                  void loadUomConversions(selected.id)
                })
              }}
              disabled={!uomForm.fromUOM || !uomForm.toUOM || uomForm.conversionFactor <= 0}
            >Add Conversion</Button>
          </div>
        )}
      </Modal>
    </div>
  )
}
