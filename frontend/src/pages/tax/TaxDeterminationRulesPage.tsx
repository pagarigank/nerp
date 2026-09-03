import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle, ArrowUpDown, Play, HelpCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getTaxCodes,
  getTaxDeterminationRules,
  createTaxDeterminationRule,
  updateTaxDeterminationRule,
  deleteTaxDeterminationRule,
  type TaxCode,
  type TaxDeterminationRule,
} from '@api/tax'

const schema = z.object({
  itemCategory: z.string().trim().min(1),
  customerType: z.string().trim().min(1),
  placeOfSupply: z.string().trim().min(1),
  taxCodeId: z.string().min(1, 'Tax Code is required'),
  priority: z.coerce.number().int().min(1).max(9999),
  description: z.string().optional(),
  effectiveFrom: z.string().optional(),
  effectiveTo: z.string().optional(),
})

type FormData = z.infer<typeof schema>

const defaultValues: FormData = {
  itemCategory: '*',
  customerType: '*',
  placeOfSupply: '*',
  taxCodeId: '',
  priority: 100,
  description: '',
  effectiveFrom: '',
  effectiveTo: '',
}

const ITEM_CATEGORIES = [
  { value: '*', label: '* (Any category)' },
  { value: 'GOODS', label: 'GOODS — Physical goods' },
  { value: 'SERVICES', label: 'SERVICES — Services' },
  { value: 'FOOD', label: 'FOOD — Food & beverages' },
  { value: 'DIGITAL', label: 'DIGITAL — Digital products' },
  { value: 'CONSTRUCTION', label: 'CONSTRUCTION — Construction services' },
  { value: 'UTILITIES', label: 'UTILITIES — Utilities' },
]

const CUSTOMER_TYPES = [
  { value: '*', label: '* (Any customer)' },
  { value: 'BUSINESS', label: 'BUSINESS — B2B' },
  { value: 'INDIVIDUAL', label: 'INDIVIDUAL — B2C' },
  { value: 'GOVERNMENT', label: 'GOVERNMENT — Government entities' },
  { value: 'NON_RESIDENT', label: 'NON_RESIDENT — Foreign buyers' },
]

const PLACE_OF_SUPPLY = [
  { value: '*', label: '* (Any place)' },
  { value: 'DOMESTIC', label: 'DOMESTIC — Within Malaysia' },
  { value: 'EXPORT', label: 'EXPORT — Outside Malaysia' },
]

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

function getSpecificity(item: string, customer: string, place: string): number {
  let score = 0
  if (item !== '*') score += 4
  if (customer !== '*') score += 2
  if (place !== '*') score += 1
  return score
}

function getSpecificityLabel(score: number): string {
  if (score >= 6) return 'High'
  if (score >= 3) return 'Medium'
  return 'Low'
}

function getSpecificityColor(score: number): 'success' | 'warning' | 'neutral' {
  if (score >= 6) return 'success'
  if (score >= 3) return 'warning'
  return 'neutral'
}

export function TaxDeterminationRulesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editing, setEditing] = useState<TaxDeterminationRule | null>(null)
  const [toDelete, setToDelete] = useState<TaxDeterminationRule | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  // Dry-run preview
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewItem, setPreviewItem] = useState('*')
  const [previewCustomer, setPreviewCustomer] = useState('*')
  const [previewPlace, setPreviewPlace] = useState('*')
  const [previewResult, setPreviewResult] = useState<any>(null)

  const companyId = '00000000-0000-0000-0000-000000000001'

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues,
  })

  const watchedItem = watch('itemCategory')
  const watchedCustomer = watch('customerType')
  const watchedPlace = watch('placeOfSupply')

  const { data: taxCodes = [] } = useQuery({ queryKey: ['tax', 'codes'], queryFn: getTaxCodes })
  const { data: rules = [], isLoading } = useQuery({
    queryKey: ['tax', 'rules', companyId],
    queryFn: () => getTaxDeterminationRules(companyId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['tax', 'rules'] })

  const createMutation = useMutation({
    mutationFn: (data: FormData) => createTaxDeterminationRule({
      companyId,
      itemCategory: data.itemCategory,
      customerType: data.customerType,
      placeOfSupply: data.placeOfSupply,
      taxCodeId: data.taxCodeId,
      priority: data.priority,
      description: data.description || undefined,
      effectiveFrom: data.effectiveFrom || undefined,
      effectiveTo: data.effectiveTo || undefined,
    }),
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: FormData }) =>
      updateTaxDeterminationRule(id, {
        companyId,
        itemCategory: data.itemCategory,
        customerType: data.customerType,
        placeOfSupply: data.placeOfSupply,
        taxCodeId: data.taxCodeId,
        priority: data.priority,
        description: data.description || undefined,
        effectiveFrom: data.effectiveFrom || undefined,
        effectiveTo: data.effectiveTo || undefined,
      }),
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteTaxDeterminationRule,
    onSuccess: () => { invalidate(); setToDelete(null) },
  })

  const openCreate = () => { setEditing(null); setFormError(null); reset(defaultValues); setIsModalOpen(true) }
  const openEdit = (r: TaxDeterminationRule) => {
    setEditing(r); setFormError(null)
    reset({
      itemCategory: r.itemCategory,
      customerType: r.customerType,
      placeOfSupply: r.placeOfSupply,
      taxCodeId: r.taxCodeId,
      priority: r.priority,
      description: r.description ?? '',
      effectiveFrom: r.effectiveFrom ? r.effectiveFrom.split('T')[0] : '',
      effectiveTo: r.effectiveTo ? r.effectiveTo.split('T')[0] : '',
    })
    setIsModalOpen(true)
  }
  const closeForm = () => { setIsModalOpen(false); setEditing(null); setFormError(null) }

  const onSubmit = (data: FormData) => {
    setFormError(null)
    if (editing) { updateMutation.mutate({ id: editing.id, data }); return }
    createMutation.mutate(data)
  }

  const taxCodeMap = useMemo(() => {
    const map: Record<string, TaxCode> = {}
    taxCodes.forEach(c => { map[c.id] = c })
    return map
  }, [taxCodes])

  const sortedRules = useMemo(() => {
    const q = search.trim().toLowerCase()
    let filtered = rules
    if (q) {
      filtered = rules.filter(r => {
        const code = taxCodeMap[r.taxCodeId]
        return code?.code.toLowerCase().includes(q) ||
          r.itemCategory.toLowerCase().includes(q) ||
          r.customerType.toLowerCase().includes(q) ||
          r.placeOfSupply.toLowerCase().includes(q)
      })
    }
    return [...filtered].sort((a, b) => a.priority - b.priority)
  }, [rules, taxCodeMap, search])

  const taxCodeOptions = taxCodes.map(c => ({ value: c.id, label: `${c.code} — ${c.description}` }))

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
          title="Tax Determination Rules"
          description="Map (Item Category × Customer Type × Place of Supply) → Tax Code. Lower priority = evaluated first."
          action={
            <div className="flex gap-2">
              <Button variant="secondary" size="sm" onClick={() => setPreviewOpen(true)} leftIcon={<Play className="h-4 w-4" />}>
                Test Rule
              </Button>
              <Button variant="primary" size="sm" onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
                New Rule
              </Button>
            </div>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by category, customer, place, or tax code..."
              leftIcon={<Search className="h-4 w-4" />} aria-label="Search rules" />
          </div>

          {isLoading ? <SkeletonTable columns={7} /> : sortedRules.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No rules match your search.' : 'No tax determination rules configured.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 w-16">
                      <ArrowUpDown className="h-3 w-3 inline mr-1" />Priority
                    </th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Item Category</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Customer Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Place of Supply</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Tax Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Specificity</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {sortedRules.map(rule => {
                    const code = taxCodeMap[rule.taxCodeId]
                    const spec = getSpecificity(rule.itemCategory, rule.customerType, rule.placeOfSupply)
                    return (
                      <tr key={rule.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-3 text-center">
                          <span className="font-mono font-bold text-gray-900 dark:text-white">{rule.priority}</span>
                        </td>
                        <td className="px-3 py-3">
                          <Badge variant={rule.itemCategory === '*' ? 'neutral' : 'info'} size="sm">
                            {rule.itemCategory}
                          </Badge>
                        </td>
                        <td className="px-3 py-3">
                          <Badge variant={rule.customerType === '*' ? 'neutral' : 'info'} size="sm">
                            {rule.customerType}
                          </Badge>
                        </td>
                        <td className="px-3 py-3">
                          <Badge variant={rule.placeOfSupply === '*' ? 'neutral' : 'info'} size="sm">
                            {rule.placeOfSupply}
                          </Badge>
                        </td>
                        <td className="px-3 py-3 font-mono text-gray-900 dark:text-white">{code?.code ?? '—'}</td>
                        <td className="px-3 py-3">
                          <Badge variant={getSpecificityColor(spec)} size="sm">{getSpecificityLabel(spec)}</Badge>
                        </td>
                        <td className="px-3 py-3">
                          <div className="flex items-center justify-end gap-1">
                            <IconButton size="sm" variant="ghost" aria-label="Edit" onClick={() => openEdit(rule)}>
                              <Pencil className="h-4 w-4" />
                            </IconButton>
                            <IconButton size="sm" variant="ghost" className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                              aria-label="Delete" onClick={() => setToDelete(rule)}>
                              <Trash2 className="h-4 w-4" />
                            </IconButton>
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

      {/* Rule Priority Legend */}
      <Card className="p-4">
        <div className="flex items-start gap-3">
          <HelpCircle className="h-5 w-5 text-gray-400 mt-0.5" />
          <div className="text-sm text-gray-600 dark:text-gray-400">
            <p className="font-medium text-gray-900 dark:text-white mb-1">How Priority Works</p>
            <p className="mb-2">Rules are evaluated from lowest priority number to highest. The first matching rule determines the tax code.</p>
            <div className="flex gap-4">
              <span><Badge variant="info" size="sm">Specific</Badge> = exact match (e.g., "SERVICES")</span>
              <span><Badge variant="neutral" size="sm">*</Badge> = wildcard (matches anything)</span>
            </div>
            <p className="mt-2">Example: Priority 50 (SERVICES → DOMESTIC → SST-SERVICE-8) beats Priority 100 (* → BUSINESS → DOMESTIC → SST-SALES-10) because it's more specific.</p>
          </div>
        </div>
      </Card>

      {/* Create/Edit Modal */}
      <Modal isOpen={isModalOpen} onClose={closeForm} title={editing ? 'Edit Tax Rule' : 'New Tax Rule'} size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending || updateMutation.isPending}>Cancel</Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending || updateMutation.isPending}>
              {editing ? 'Save Changes' : 'Create Rule'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          {/* Matching Criteria */}
          <div className="border-b border-gray-200 dark:border-gray-700 pb-4">
            <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Matching Criteria</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Select {...register('itemCategory')} label="Item Category" options={ITEM_CATEGORIES} {...fieldError(errors.itemCategory?.message)} required />
              <Select {...register('customerType')} label="Customer Type" options={CUSTOMER_TYPES} {...fieldError(errors.customerType?.message)} required />
              <Select {...register('placeOfSupply')} label="Place of Supply" options={PLACE_OF_SUPPLY} {...fieldError(errors.placeOfSupply?.message)} required />
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
              Use * for wildcard. Current specificity: <Badge variant={getSpecificityColor(getSpecificity(watchedItem, watchedCustomer, watchedPlace))} size="sm">
                {getSpecificityLabel(getSpecificity(watchedItem, watchedCustomer, watchedPlace))}
              </Badge>
            </p>
          </div>

          {/* Action */}
          <div className="border-b border-gray-200 dark:border-gray-700 pb-4">
            <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Action</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Select {...register('taxCodeId')} label="Tax Code" options={taxCodeOptions} {...fieldError(errors.taxCodeId?.message)} required />
              <Input {...register('priority')} label="Priority (lower = first)" type="number" min="1" max="9999" {...fieldError(errors.priority?.message)} required />
            </div>
          </div>

          {/* Optional */}
          <div>
            <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Optional</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Input {...register('effectiveFrom')} label="Effective From" type="date" />
              <Input {...register('effectiveTo')} label="Effective To" type="date" />
            </div>
            <Input {...register('description')} label="Description" placeholder="e.g., Services supplied domestically" className="mt-4" />
          </div>
        </form>
      </Modal>

      {/* Dry-Run Preview Modal */}
      <Modal isOpen={previewOpen} onClose={() => { setPreviewOpen(false); setPreviewResult(null) }}
        title="Test Tax Determination" size="md"
        footer={
          <Button variant="secondary" onClick={() => { setPreviewOpen(false); setPreviewResult(null) }}>Close</Button>
        }
      >
        <div className="space-y-4">
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Preview which tax code would be determined for a given context.
          </p>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <Select label="Item Category" value={previewItem} onChange={e => setPreviewItem(e.target.value)} options={ITEM_CATEGORIES} />
            <Select label="Customer Type" value={previewCustomer} onChange={e => setPreviewCustomer(e.target.value)} options={CUSTOMER_TYPES} />
            <Select label="Place of Supply" value={previewPlace} onChange={e => setPreviewPlace(e.target.value)} options={PLACE_OF_SUPPLY} />
          </div>
          <Button variant="primary" size="sm" onClick={async () => {
            try {
              const params = new URLSearchParams({
                companyId,
                itemCategory: previewItem,
                customerType: previewCustomer,
                placeOfSupply: previewPlace,
              })
              const res = await fetch(`/api/v1.0/TaxDeterminationRule/determine?${params}`, {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken') || ''}` }
              })
              if (res.ok) { setPreviewResult(await res.json()) }
              else { setPreviewResult({ error: await res.text() }) }
            } catch (e: any) { setPreviewResult({ error: e.message }) }
          }}>
            Run Preview
          </Button>
          {previewResult && (
            <div className={`p-4 rounded-lg border ${previewResult.error ? 'bg-red-50 border-red-200 dark:bg-red-900/20 dark:border-red-800' : 'bg-green-50 border-green-200 dark:bg-green-900/20 dark:border-green-800'}`}>
              {previewResult.error ? (
                <p className="text-sm text-red-700 dark:text-red-300">{previewResult.error}</p>
              ) : (
                <div className="space-y-2 text-sm">
                  <p className="font-medium text-green-800 dark:text-green-200">Rule Matched!</p>
                  <p>Tax Code ID: <code className="font-mono">{taxCodeMap[previewResult.taxCodeId]?.code ?? previewResult.taxCodeId}</code></p>
                  <p>Priority: {previewResult.priority} | Specificity: {previewResult.specificityScore}</p>
                  <p>Total matching rules: {previewResult.totalMatchingRules}</p>
                </div>
              )}
            </div>
          )}
        </div>
      </Modal>

      <ConfirmDialog isOpen={!!toDelete} onClose={() => setToDelete(null)}
        onConfirm={() => toDelete && deleteMutation.mutate(toDelete.id)}
        title="Delete Tax Rule" confirmText="Delete" variant="danger" isLoading={deleteMutation.isPending}
        message={toDelete ? `Delete rule: ${toDelete.itemCategory} × ${toDelete.customerType} × ${toDelete.placeOfSupply} → Tax Code?` : ''}
      />
    </div>
  )
}
