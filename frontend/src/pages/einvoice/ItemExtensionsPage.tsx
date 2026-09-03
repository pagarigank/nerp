import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, Pencil, Trash2, AlertCircle, Package } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { get, post, del } from '@api/client'

interface ItemExtension {
  id: string
  itemId: string
  classificationCode?: string
  unitOfMeasureCode?: string
  productTariffCode?: string
  countryOfOrigin?: string
  isSubjectToEInvoice: boolean
  createdAt: string
}

function getItemExtensions(): Promise<ItemExtension[]> {
  return get('/einvoice/item-extensions')
}

function createItemExtension(data: Partial<ItemExtension>): Promise<ItemExtension> {
  return post('/einvoice/item-extensions', data)
}

function deleteItemExtension(id: string): Promise<void> {
  return del(`/einvoice/item-extensions/${id}`)
}

const CLASSIFICATION_CODES = [
  { value: '', label: '— None —' },
  { value: '022', label: '022 — Goods' },
  { value: '031', label: '031 — Services' },
  { value: '032', label: '032 — Construction services' },
  { value: '033', label: '033 — Utility services' },
  { value: '034', label: '034 — Telecom services' },
  { value: '035', label: '035 — Transportation services' },
  { value: '036', label: '036 — Real estate services' },
  { value: '037', label: '037 — Professional services' },
  { value: '038', label: '038 — Government services' },
  { value: '039', label: '039 — Other services' },
]

const UOM_CODES = [
  { value: '', label: '— None —' },
  { value: 'C62', label: 'C62 — Each (unit)' },
  { value: 'KGM', label: 'KGM — Kilogram' },
  { value: 'GRM', label: 'GRM — Gram' },
  { value: 'LTR', label: 'LTR — Litre' },
  { value: 'MLT', label: 'MLT — Millilitre' },
  { value: 'MTR', label: 'MTR — Metre' },
  { value: 'MTK', label: 'MTK — Square metre' },
  { value: 'MTQ', label: 'MTQ — Cubic metre' },
  { value: 'HUR', label: 'HUR — Hour' },
  { value: 'DAY', label: 'DAY — Day' },
  { value: 'MON', label: 'MON — Month' },
  { value: 'SET', label: 'SET — Set' },
  { value: 'BOX', label: 'BOX — Box' },
  { value: 'PAK', label: 'PAK — Package' },
  { value: 'BAG', label: 'BAG — Bag' },
]

const COUNTRIES = [
  { value: '', label: '— None —' },
  { value: 'MYS', label: 'MYS — Malaysia' },
  { value: 'SGP', label: 'SGP — Singapore' },
  { value: 'THA', label: 'THA — Thailand' },
  { value: 'IDN', label: 'IDN — Indonesia' },
  { value: 'PHL', label: 'PHL — Philippines' },
  { value: 'VNM', label: 'VNM — Vietnam' },
  { value: 'CHN', label: 'CHN — China' },
  { value: 'JPN', label: 'JPN — Japan' },
  { value: 'KOR', label: 'KOR — South Korea' },
  { value: 'USA', label: 'USA — United States' },
  { value: 'GBR', label: 'GBR — United Kingdom' },
  { value: 'DEU', label: 'DEU — Germany' },
]

export function ItemExtensionsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editing, setEditing] = useState<ItemExtension | null>(null)
  const [toDelete, setToDelete] = useState<ItemExtension | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const [itemId, setItemId] = useState('')
  const [classificationCode, setClassificationCode] = useState('')
  const [unitOfMeasureCode, setUnitOfMeasureCode] = useState('')
  const [productTariffCode, setProductTariffCode] = useState('')
  const [countryOfOrigin, setCountryOfOrigin] = useState('')

  const { data: items = [], isLoading } = useQuery({
    queryKey: ['einvoice', 'item-extensions'],
    queryFn: getItemExtensions,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['einvoice', 'item-extensions'] })

  const createMutation = useMutation({
    mutationFn: createItemExtension,
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteItemExtension,
    onSuccess: () => { invalidate(); setToDelete(null) },
  })

  const openCreate = () => {
    setEditing(null); setFormError(null)
    setItemId(''); setClassificationCode(''); setUnitOfMeasureCode('')
    setProductTariffCode(''); setCountryOfOrigin('')
    setIsModalOpen(true)
  }

  const openEdit = (item: ItemExtension) => {
    setEditing(item); setFormError(null)
    setItemId(item.itemId)
    setClassificationCode(item.classificationCode ?? '')
    setUnitOfMeasureCode(item.unitOfMeasureCode ?? '')
    setProductTariffCode(item.productTariffCode ?? '')
    setCountryOfOrigin(item.countryOfOrigin ?? '')
    setIsModalOpen(true)
  }

  const closeForm = () => { setIsModalOpen(false); setEditing(null); setFormError(null) }

  const onSubmit = () => {
    setFormError(null)
    if (!itemId.trim()) { setFormError('Item ID is required'); return }
    createMutation.mutate({
      itemId,
      classificationCode: classificationCode || undefined,
      unitOfMeasureCode: unitOfMeasureCode || undefined,
      productTariffCode: productTariffCode || undefined,
      countryOfOrigin: countryOfOrigin || undefined,
    })
  }

  const filtered = items.filter(i => {
    const q = search.trim().toLowerCase()
    if (!q) return true
    return i.itemId.toLowerCase().includes(q) ||
      i.classificationCode?.toLowerCase().includes(q) ||
      i.productTariffCode?.toLowerCase().includes(q)
  })

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Item / Service Extensions"
          description="Map ERP items to MyInvois classification codes, UOM, and tariff codes"
          action={
            <Button variant="primary" size="sm" onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
              Add Extension
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by item ID, classification, or tariff code..."
              leftIcon={<Search className="h-4 w-4" />} aria-label="Search item extensions" />
          </div>

          {isLoading ? <SkeletonTable columns={6} /> : filtered.length === 0 ? (
            <div className="text-center py-12">
              <Package className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                No item extensions configured yet.
              </p>
              <Button variant="primary" size="sm" onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
                Add First Extension
              </Button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Item ID</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Classification</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">UOM</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Tariff Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Country</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map(item => (
                    <tr key={item.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">{item.itemId.slice(0, 8)}...</td>
                      <td className="px-3 py-3">
                        {item.classificationCode ? (
                          <Badge variant="info" size="sm">{item.classificationCode}</Badge>
                        ) : (
                          <span className="text-gray-400">—</span>
                        )}
                      </td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">{item.unitOfMeasureCode ?? '—'}</td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">{item.productTariffCode ?? '—'}</td>
                      <td className="px-3 py-3 text-xs text-gray-700 dark:text-gray-300">{item.countryOfOrigin ?? '—'}</td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton size="sm" variant="ghost" aria-label="Edit" onClick={() => openEdit(item)}>
                            <Pencil className="h-4 w-4" />
                          </IconButton>
                          <IconButton size="sm" variant="ghost" className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label="Delete" onClick={() => setToDelete(item)}>
                            <Trash2 className="h-4 w-4" />
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

      {/* Create/Edit Modal */}
      <Modal isOpen={isModalOpen} onClose={closeForm} title={editing ? 'Edit Item Extension' : 'Add Item Extension'} size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>Cancel</Button>
            <Button variant="primary" onClick={onSubmit} isLoading={createMutation.isPending}>
              {editing ? 'Save Changes' : 'Add Extension'}
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input label="Item ID" value={itemId} onChange={e => setItemId(e.target.value)}
            placeholder="ERP Item ID (GUID)" required disabled={!!editing} />
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select label="Classification Code" value={classificationCode} onChange={e => setClassificationCode(e.target.value)}
              options={CLASSIFICATION_CODES} />
            <Select label="Unit of Measure" value={unitOfMeasureCode} onChange={e => setUnitOfMeasureCode(e.target.value)}
              options={UOM_CODES} />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input label="Product Tariff Code (HS)" value={productTariffCode} onChange={e => setProductTariffCode(e.target.value)}
              placeholder="e.g., 8471.30" />
            <Select label="Country of Origin" value={countryOfOrigin} onChange={e => setCountryOfOrigin(e.target.value)}
              options={COUNTRIES} />
          </div>
        </div>
      </Modal>

      <ConfirmDialog isOpen={!!toDelete} onClose={() => setToDelete(null)}
        onConfirm={() => toDelete && deleteMutation.mutate(toDelete.id)}
        title="Delete Item Extension" confirmText="Delete" variant="danger" isLoading={deleteMutation.isPending}
        message={toDelete ? `Delete extension for item ${toDelete.itemId.slice(0, 8)}...?` : ''}
      />
    </div>
  )
}
