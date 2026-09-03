import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle, Percent } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getTaxCodes, createTaxCode, updateTaxCode, deleteTaxCode, type TaxCode } from '@api/tax'

const schema = z.object({
  code: z.string().trim().min(1, 'Code is required').max(50),
  description: z.string().trim().min(1, 'Description is required').max(250),
  lhdntTaxType: z.string().min(1, 'LHDN Tax Type is required'),
})

type FormData = z.infer<typeof schema>

const LHDN_TYPES = [
  { value: '01', label: '01 — Supply of goods (SST)' },
  { value: '02', label: '02 — Supply of services (SST)' },
  { value: '03', label: '03 — Supply of goods (RLP)' },
  { value: '04', label: '04 — Supply of services (RLP)' },
  { value: 'E', label: 'E — Exempt supply' },
  { value: 'ZRL', label: 'ZRL — Zero-rated supply' },
]

const defaultValues: FormData = { code: '', description: '', lhdntTaxType: '01' }

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function TaxCodesPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editing, setEditing] = useState<TaxCode | null>(null)
  const [toDelete, setToDelete] = useState<TaxCode | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues,
  })

  const { data: codes = [], isLoading } = useQuery({
    queryKey: ['tax', 'codes'],
    queryFn: getTaxCodes,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['tax', 'codes'] })

  const createMutation = useMutation({
    mutationFn: createTaxCode,
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: FormData }) =>
      updateTaxCode(id, { ...data, id }),
    onSuccess: () => { invalidate(); closeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteTaxCode,
    onSuccess: () => { invalidate(); setToDelete(null) },
  })

  const openCreate = () => { setEditing(null); setFormError(null); reset(defaultValues); setIsModalOpen(true) }
  const openEdit = (c: TaxCode) => {
    setEditing(c); setFormError(null)
    reset({ code: c.code, description: c.description, lhdntTaxType: c.lhdntTaxType })
    setIsModalOpen(true)
  }
  const closeForm = () => { setIsModalOpen(false); setEditing(null); setFormError(null) }

  const onSubmit = (data: FormData) => {
    setFormError(null)
    if (editing) { updateMutation.mutate({ id: editing.id, data }); return }
    createMutation.mutate(data)
  }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return codes
    return codes.filter(c => c.code.toLowerCase().includes(q) || c.description.toLowerCase().includes(q))
  }, [codes, search])

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
          title="Tax Codes"
          description="LHDN e-Invoice tax type codes for SST compliance"
          action={
            <Button variant="primary" size="sm" onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
              New Tax Code
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by code or description..."
              leftIcon={<Search className="h-4 w-4" />} aria-label="Search tax codes" />
          </div>

          {isLoading ? <SkeletonTable columns={5} /> : filtered.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No tax codes match your search.' : 'No tax codes yet.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">LHDN Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map(code => (
                    <tr key={code.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono font-medium text-gray-900 dark:text-white">{code.code}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{code.description}</td>
                      <td className="px-3 py-3">
                        <Badge variant="info" size="sm">{code.lhdntTaxType}</Badge>
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={code.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {code.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton size="sm" variant="ghost" aria-label={`Edit ${code.code}`} onClick={() => openEdit(code)}>
                            <Pencil className="h-4 w-4" />
                          </IconButton>
                          <IconButton size="sm" variant="ghost" className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Delete ${code.code}`} onClick={() => setToDelete(code)}>
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

      <Modal isOpen={isModalOpen} onClose={closeForm} title={editing ? 'Edit Tax Code' : 'New Tax Code'} size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending || updateMutation.isPending}>Cancel</Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending || updateMutation.isPending}>
              {editing ? 'Save Changes' : 'Create Tax Code'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input {...register('code')} label="Code" placeholder="e.g., SST-SALES-10" {...fieldError(errors.code?.message)} required />
          <Input {...register('description')} label="Description" placeholder="e.g., Standard rated supply of goods (10%)" {...fieldError(errors.description?.message)} required />
          <Select {...register('lhdntTaxType')} label="LHDN Tax Type" options={LHDN_TYPES} {...fieldError(errors.lhdntTaxType?.message)} required />
        </form>
      </Modal>

      <ConfirmDialog isOpen={!!toDelete} onClose={() => setToDelete(null)}
        onConfirm={() => toDelete && deleteMutation.mutate(toDelete.id)}
        title="Delete Tax Code" confirmText="Delete" variant="danger" isLoading={deleteMutation.isPending}
        message={toDelete ? `Delete tax code "${toDelete.code}" (${toDelete.description})?` : ''}
      />
    </div>
  )
}
