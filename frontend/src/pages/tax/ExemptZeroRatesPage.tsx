import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { ShieldCheck, Plus, Pencil, Trash2, Search, ExternalLink, X, Check } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge } from '@components/ui/Badge'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import {
  getExemptSupplyCategories,
  createExemptSupplyCategory,
  updateExemptSupplyCategory,
  deleteExemptSupplyCategory,
  getTaxCodes,
  type ExemptSupplyCategory,
  type TaxCode,
} from '@api/tax'
import { useAuth } from '@stores/authStore'

const SUPPLY_TYPES = [
  { value: 'Exempt', label: 'Exempt (E)', color: 'warning' as const },
  { value: 'ZeroRated', label: 'Zero-Rated (ZRL)', color: 'success' as const },
  { value: 'DeemedSupply', label: 'Deemed Supply (DS)', color: 'info' as const },
]

interface FormData {
  companyId: string
  categoryCode: string
  description: string
  supplyType: string
  taxCodeId: string
  lhdnReference: string
  effectiveFrom: string
  effectiveTo: string
  notes: string
}

const emptyForm: FormData = {
  companyId: '',
  categoryCode: '',
  description: '',
  supplyType: 'Exempt',
  taxCodeId: '',
  lhdnReference: '',
  effectiveFrom: '',
  effectiveTo: '',
  notes: '',
}

export function ExemptZeroRatesPage() {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const companyId = user?.companyId || ''

  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<FormData>({ ...emptyForm, companyId })
  const [search, setSearch] = useState('')
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const { data: categories = [], isLoading } = useQuery({
    queryKey: ['exempt-supply-categories', companyId],
    queryFn: () => getExemptSupplyCategories(companyId),
    enabled: !!companyId,
  })

  const { data: taxCodes = [] } = useQuery({
    queryKey: ['tax-codes'],
    queryFn: getTaxCodes,
  })

  const createMutation = useMutation({
    mutationFn: createExemptSupplyCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exempt-supply-categories'] })
      setShowForm(false)
      setForm({ ...emptyForm, companyId })
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<ExemptSupplyCategory> }) =>
      updateExemptSupplyCategory(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exempt-supply-categories'] })
      setShowForm(false)
      setEditingId(null)
      setForm({ ...emptyForm, companyId })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: deleteExemptSupplyCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exempt-supply-categories'] })
      setDeleteConfirmId(null)
    },
  })

  const filtered = categories.filter(
    (c) =>
      c.categoryCode.toLowerCase().includes(search.toLowerCase()) ||
      (c.description && c.description.toLowerCase().includes(search.toLowerCase())),
  )

  const getTaxCodeDisplay = (taxCodeId: string) => {
    const tc = taxCodes.find((t) => t.id === taxCodeId)
    return tc ? tc.code : taxCodeId.slice(0, 8)
  }

  const getSupplyTypeInfo = (type: string) =>
    SUPPLY_TYPES.find((st) => st.value === type) || SUPPLY_TYPES[0]

  const handleEdit = (cat: ExemptSupplyCategory) => {
    setEditingId(cat.id)
    setForm({
      companyId: cat.companyId,
      categoryCode: cat.categoryCode,
      description: cat.description || '',
      supplyType: cat.supplyType,
      taxCodeId: cat.taxCodeId,
      lhdnReference: cat.lhdnReference || '',
      effectiveFrom: cat.effectiveFrom ? cat.effectiveFrom.split('T')[0] : '',
      effectiveTo: cat.effectiveTo ? cat.effectiveTo.split('T')[0] : '',
      notes: cat.notes || '',
    })
    setShowForm(true)
  }

  const handleSubmit = () => {
    const payload: Partial<ExemptSupplyCategory> = {
      ...form,
      companyId,
      effectiveFrom: form.effectiveFrom || undefined,
      effectiveTo: form.effectiveTo || undefined,
    }

    if (editingId) {
      updateMutation.mutate({ id: editingId, data: payload })
    } else {
      createMutation.mutate(payload)
    }
  }

  const handleCancel = () => {
    setShowForm(false)
    setEditingId(null)
    setForm({ ...emptyForm, companyId })
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader
          title="Exempt & Zero-Rated Supplies"
          description="Configure which product/service categories are exempt or zero-rated from SST"
          action={
            <div className="flex gap-2">
              <a
                href="/tax/tax-codes"
                className="inline-flex items-center gap-1 text-sm text-emerald-600 hover:underline"
              >
                Tax Codes <ExternalLink className="h-3 w-3" />
              </a>
              <Button
                size="sm"
                onClick={() => {
                  setShowForm(true)
                  setEditingId(null)
                  setForm({ ...emptyForm, companyId })
                }}
              >
                <Plus className="h-4 w-4 mr-1" />
                Add Category
              </Button>
            </div>
          }
        />
        <CardContent>
          {/* Summary badges */}
          <div className="flex gap-2 mb-4">
            {SUPPLY_TYPES.map((st) => {
              const count = categories.filter((c) => c.supplyType === st.value).length
              return (
                <Badge key={st.value} variant={st.color} size="sm">
                  {st.label}: {count}
                </Badge>
              )
            })}
          </div>

          {/* Search */}
          <div className="relative mb-4">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
            <Input
              placeholder="Search by category code or description..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-10"
            />
          </div>

          {/* Table */}
          {isLoading ? (
            <div className="text-center py-8 text-gray-500">Loading...</div>
          ) : filtered.length === 0 ? (
            <div className="text-center py-12">
              <ShieldCheck className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {categories.length === 0
                  ? 'No exempt supply categories configured yet.'
                  : 'No categories match your search.'}
              </p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700">
                    <th className="text-left py-2 px-3 font-medium text-gray-500">Category</th>
                    <th className="text-left py-2 px-3 font-medium text-gray-500">Description</th>
                    <th className="text-left py-2 px-3 font-medium text-gray-500">Type</th>
                    <th className="text-left py-2 px-3 font-medium text-gray-500">Tax Code</th>
                    <th className="text-left py-2 px-3 font-medium text-gray-500">LHDN Ref</th>
                    <th className="text-left py-2 px-3 font-medium text-gray-500">Effective</th>
                    <th className="text-right py-2 px-3 font-medium text-gray-500">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((cat) => {
                    const typeInfo = getSupplyTypeInfo(cat.supplyType)
                    return (
                      <tr
                        key={cat.id}
                        className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50"
                      >
                        <td className="py-2 px-3 font-mono text-xs font-medium">{cat.categoryCode}</td>
                        <td className="py-2 px-3 text-gray-600 dark:text-gray-400 max-w-[200px] truncate">
                          {cat.description || '—'}
                        </td>
                        <td className="py-2 px-3">
                          <Badge variant={typeInfo.color} size="sm">
                            {typeInfo.label}
                          </Badge>
                        </td>
                        <td className="py-2 px-3 font-mono text-xs">{getTaxCodeDisplay(cat.taxCodeId)}</td>
                        <td className="py-2 px-3 text-xs text-gray-500">{cat.lhdnReference || '—'}</td>
                        <td className="py-2 px-3 text-xs text-gray-500">
                          {cat.effectiveFrom
                            ? new Date(cat.effectiveFrom).toLocaleDateString()
                            : 'Always'}
                          {cat.effectiveTo ? ` → ${new Date(cat.effectiveTo).toLocaleDateString()}` : ''}
                        </td>
                        <td className="py-2 px-3 text-right">
                          <div className="flex items-center justify-end gap-1">
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleEdit(cat)}
                              className="h-7 w-7 p-0"
                            >
                              <Pencil className="h-3.5 w-3.5" />
                            </Button>
                            {deleteConfirmId === cat.id ? (
                              <div className="flex items-center gap-1">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => deleteMutation.mutate(cat.id)}
                                  className="h-7 w-7 p-0 text-red-600"
                                >
                                  <Check className="h-3.5 w-3.5" />
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => setDeleteConfirmId(null)}
                                  className="h-7 w-7 p-0"
                                >
                                  <X className="h-3.5 w-3.5" />
                                </Button>
                              </div>
                            ) : (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => setDeleteConfirmId(cat.id)}
                                className="h-7 w-7 p-0 text-red-500"
                              >
                                <Trash2 className="h-3.5 w-3.5" />
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Form Modal */}
      {showForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-lg mx-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
              <h3 className="text-lg font-semibold">
                {editingId ? 'Edit Exempt Supply Category' : 'Add Exempt Supply Category'}
              </h3>
              <Button variant="ghost" size="sm" onClick={handleCancel}>
                <X className="h-4 w-4" />
              </Button>
            </div>
            <div className="p-4 space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">Category Code *</label>
                <Input
                  value={form.categoryCode}
                  onChange={(e) => setForm({ ...form, categoryCode: e.target.value })}
                  placeholder="e.g., FOOD, MEDICAL, EDUCATION"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Description</label>
                <Input
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  placeholder="e.g., Basic food items exempt from sales tax"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Supply Type *</label>
                  <select
                    value={form.supplyType}
                    onChange={(e) => setForm({ ...form, supplyType: e.target.value })}
                    className="w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
                  >
                    {SUPPLY_TYPES.map((st) => (
                      <option key={st.value} value={st.value}>
                        {st.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Tax Code *</label>
                  <select
                    value={form.taxCodeId}
                    onChange={(e) => setForm({ ...form, taxCodeId: e.target.value })}
                    className="w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
                  >
                    <option value="">Select tax code</option>
                    {taxCodes
                      .filter((tc) => tc.code.includes('EXEMPT') || tc.code.includes('ZRL') || tc.code.includes('DS'))
                      .map((tc) => (
                        <option key={tc.id} value={tc.id}>
                          {tc.code} — {tc.description}
                        </option>
                      ))}
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">LHDN Reference</label>
                <Input
                  value={form.lhdnReference}
                  onChange={(e) => setForm({ ...form, lhdnReference: e.target.value })}
                  placeholder="e.g., Schedule A, Section 21"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Effective From</label>
                  <Input
                    type="date"
                    value={form.effectiveFrom}
                    onChange={(e) => setForm({ ...form, effectiveFrom: e.target.value })}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1">Effective To</label>
                  <Input
                    type="date"
                    value={form.effectiveTo}
                    onChange={(e) => setForm({ ...form, effectiveTo: e.target.value })}
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">Notes</label>
                <textarea
                  value={form.notes}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })}
                  rows={3}
                  className="w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
                  placeholder="Additional notes about this exemption..."
                />
              </div>
            </div>
            <div className="flex justify-end gap-2 p-4 border-t border-gray-200 dark:border-gray-700">
              <Button variant="secondary" onClick={handleCancel}>
                Cancel
              </Button>
              <Button
                onClick={handleSubmit}
                disabled={!form.categoryCode || !form.taxCodeId || createMutation.isPending || updateMutation.isPending}
              >
                {editingId ? 'Update' : 'Create'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
