import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getItemCategories, createItemCategory } from '@api/inventory'
import type { CreateItemCategoryRequest } from '@/types/inventory'

const schema = z.object({
  categoryCode: z.string().trim().min(1, 'Code is required'),
  description: z.string().trim().min(1, 'Description is required'),
})
type Form = z.infer<typeof schema>
const defaults: Form = { categoryCode: '', description: '' }
function fieldError(message?: string) { return message ? { error: message } : {} }

export function ItemCategoriesPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { errors } } = useForm<Form>({ resolver: zodResolver(schema), defaultValues: defaults })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'item-categories'], queryFn: () => getItemCategories() })
  const createMutation = useMutation({
    mutationFn: (d: CreateItemCategoryRequest) => createItemCategory(d),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'item-categories'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); reset(defaults); setOpen(true) }
  const onSubmit = (d: Form) => { setFormError(null); createMutation.mutate({ ...d, companyId: '' }) }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter(r => r.categoryCode.toLowerCase().includes(q) || r.description.toLowerCase().includes(q))
  }, [rows, search])

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5" /> <span className="text-sm">{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Item Categories" description={`${rows.length} categor(ies)`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New Category</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search..." aria-label="Search categories" leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No categories yet.'}</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Code</th><th className="px-3 py-2 font-medium text-gray-500">Description</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.categoryCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.description}</td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
      <Modal isOpen={open} onClose={close} title="New Category"
        footer={<><Button variant="secondary" onClick={close} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>Create</Button></>}>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input {...register('categoryCode')} label="Code" {...fieldError(errors.categoryCode?.message)} required />
          <Input {...register('description')} label="Description" {...fieldError(errors.description?.message)} required />
        </form>
      </Modal>
    </div>
  )
}
