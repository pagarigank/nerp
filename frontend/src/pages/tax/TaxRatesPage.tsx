import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle, Calendar } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getTaxCodes, getTaxRates, createTaxRate, updateTaxRate, deleteTaxRate, type TaxCode, type TaxRate } from '@api/tax'

const schema = z.object({
  taxCodeId: z.string().min(1, 'Tax Code is required'),
  ratePercent: z.coerce.number().min(0, 'Rate must be ≥ 0').max(100, 'Rate must be ≤ 100'),
  effectiveFrom: z.string().min(1, 'Effective From is required'),
  effectiveTo: z.string().optional(),
})

type FormData = z.infer<typeof schema>

const defaultValues: FormData = { taxCodeId: '', ratePercent: 10, effectiveFrom: '', effectiveTo: '' }

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

function isCurrentlyEffective(rate: TaxRate): boolean {
  const now = new Date()
  const from = new Date(rate.effectiveFrom)
  const to = rate.effectiveTo ? new Date(rate.effectiveTo) : null
  return rate.isActive && from <= now && (!to || to >= now)
}

export function TaxRatesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editing, setEditing] = useState<TaxRate | null>(null)
  const [toDelete, setToDelete] = useState<TaxRate | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues,
  })

  const { data: taxCodes = [] } = useQuery({ queryKey: ['tax', 'codes'], queryFn: getTaxCodes })
  const { data: rates = [], isLoading } = useQuery({ queryKey: ['tax', 'rates'], queryFn: getTaxRates })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['tax', 'rates'] })

  const createMutation = useMutation({
    mutationFn: createTaxRate,
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: FormData }) =>
      updateTaxRate(id, { ...data, id }),
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteTaxRate,
    onSuccess: () => { invalidate(); setToDelete(null) },
  })

  const openCreate = () => { setEditing(null); setFormError(null); reset(defaultValues); setIsModalOpen(true) }
  const openEdit = (r: TaxRate) => {
    setEditing(r); setFormError(null)
    reset({
      taxCodeId: r.taxCodeId,
      ratePercent: r.ratePercent,
      effectiveFrom: r.effectiveFrom.split('T')[0],
      effectiveTo: r.effectiveTo ? r.effectiveTo.split('T')[0] : '',
    })
    setIsModalOpen(true)
  }
  const closeForm = () => { setIsModalOpen(false); setEditing(null); setFormError(null) }

  const onSubmit = (data: FormData) => {
    setFormError(null)
    const payload = { ...data, effectiveTo: data.effectiveTo || undefined }
    if (editing) { updateMutation.mutate({ id: editing.id, data: payload }); return }
    createMutation.mutate(payload)
  }

  const taxCodeMap = useMemo(() => {
    const map: Record<string, TaxCode> = {}
    taxCodes.forEach(c => { map[c.id] = c })
    return map
  }, [taxCodes])

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rates
    return rates.filter(r => {
      const code = taxCodeMap[r.taxCodeId]
      return code?.code.toLowerCase().includes(q) || code?.description.toLowerCase().includes(q)
    })
  }, [rates, taxCodeMap, search])

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
          title="Tax Rates"
          description="SST rates with effective-dating for historical accuracy"
          action={
            <Button variant="primary" size="sm" onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
              New Tax Rate
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by tax code..."
              leftIcon={<Search className="h-4 w-4" />} aria-label="Search tax rates" />
          </div>

          {isLoading ? <SkeletonTable columns={6} /> : filtered.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No tax rates match your search.' : 'No tax rates yet.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Tax Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Rate</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Effective From</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Effective To</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map(rate => {
                    const code = taxCodeMap[rate.taxCodeId]
                    const effective = isCurrentlyEffective(rate)
                    return (
                      <tr key={rate.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-3">
                          <span className="font-mono font-medium text-gray-900 dark:text-white">{code?.code ?? '—'}</span>
                          <span className="text-gray-500 dark:text-gray-400 ml-2 text-xs">{code?.description}</span>
                        </td>
                        <td className="px-3 py-3 text-right font-bold text-gray-900 dark:text-white">{rate.ratePercent}%</td>
                        <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                          {new Date(rate.effectiveFrom).toLocaleDateString()}
                        </td>
                        <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                          {rate.effectiveTo ? new Date(rate.effectiveTo).toLocaleDateString() : '—'}
                        </td>
                        <td className="px-3 py-3">
                          {effective ? (
                            <Badge variant="success" size="sm" dot>Current</Badge>
                          ) : (
                            <Badge variant="neutral" size="sm">Expired</Badge>
                          )}
                        </td>
                        <td className="px-3 py-3">
                          <div className="flex items-center justify-end gap-1">
                            <IconButton size="sm" variant="ghost" aria-label="Edit" onClick={() => openEdit(rate)}>
                              <Pencil className="h-4 w-4" />
                            </IconButton>
                            <IconButton size="sm" variant="ghost" className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                              aria-label="Delete" onClick={() => setToDelete(rate)}>
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

      <Modal isOpen={isModalOpen} onClose={closeForm} title={editing ? 'Edit Tax Rate' : 'New Tax Rate'} size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending || updateMutation.isPending}>Cancel</Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending || updateMutation.isPending}>
              {editing ? 'Save Changes' : 'Create Tax Rate'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Select {...register('taxCodeId')} label="Tax Code" options={taxCodeOptions} {...fieldError(errors.taxCodeId?.message)} required />
          <Input {...register('ratePercent')} label="Rate (%)" type="number" step="0.01" {...fieldError(errors.ratePercent?.message)} required />
          <Input {...register('effectiveFrom')} label="Effective From" type="date" {...fieldError(errors.effectiveFrom?.message)} required />
          <Input {...register('effectiveTo')} label="Effective To (optional)" type="date" />
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Leave "Effective To" blank for an open-ended rate. The rate becomes effective on the "Effective From" date.
          </p>
        </form>
      </Modal>

      <ConfirmDialog isOpen={!!toDelete} onClose={() => setToDelete(null)}
        onConfirm={() => toDelete && deleteMutation.mutate(toDelete.id)}
        title="Delete Tax Rate" confirmText="Delete" variant="danger" isLoading={deleteMutation.isPending}
        message={toDelete ? `Delete the ${toDelete.ratePercent}% rate (effective ${new Date(toDelete.effectiveFrom).toLocaleDateString()})?` : ''}
      />
    </div>
  )
}
