import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getWarehouses, createWarehouse } from '@api/inventory'
import type { CreateWarehouseRequest } from '@/types/inventory'

const warehouseTypeOptions = [
  { value: 'Distribution', label: 'Distribution' },
  { value: 'Manufacturing', label: 'Manufacturing' },
  { value: 'Service', label: 'Service' },
  { value: 'Transit', label: 'Transit' },
]

const schema = z.object({
  warehouseCode: z.string().trim().min(1, 'Code is required'),
  warehouseName: z.string().trim().min(1, 'Name is required'),
  warehouseType: z.string().min(1, 'Type is required'),
  address: z.string().nullable(),
})

type Form = z.infer<typeof schema>

const defaults: Form = { warehouseCode: '', warehouseName: '', warehouseType: 'Distribution', address: null }

function fieldError(message?: string) {
  return message ? { error: message } : {}
}

export function WarehousesPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const { register, handleSubmit, reset, formState: { errors } } = useForm<Form>({ resolver: zodResolver(schema), defaultValues: defaults })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })

  const createMutation = useMutation({
    mutationFn: (d: CreateWarehouseRequest) => createWarehouse(d),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'warehouses'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); reset(defaults); setOpen(true) }
  const onSubmit = (d: Form) => {
    setFormError(null)
    createMutation.mutate({ ...d, companyId: '' })
  }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter(r => r.warehouseCode.toLowerCase().includes(q) || r.warehouseName.toLowerCase().includes(q))
  }, [rows, search])

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5" /> <span className="text-sm">{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Warehouses" description={`${rows.length} warehouse(s)`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New Warehouse</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search..." aria-label="Search warehouses" leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No warehouses yet.'}</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Code</th><th className="px-3 py-2 font-medium text-gray-500">Name</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Type</th><th className="px-3 py-2 font-medium text-gray-500">Address</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.warehouseCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseName}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseType}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.address ?? '—'}</td>
                      <td className="px-3 py-3"><Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Modal isOpen={open} onClose={close} title="New Warehouse"
        footer={<><Button variant="secondary" onClick={close} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>Create</Button></>}>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input {...register('warehouseCode')} label="Code" {...fieldError(errors.warehouseCode?.message)} required />
            <Input {...register('warehouseName')} label="Name" {...fieldError(errors.warehouseName?.message)} required />
            <Select {...register('warehouseType')} label="Type" options={warehouseTypeOptions} {...fieldError(errors.warehouseType?.message)} required />
            <Input {...register('address')} label="Address" placeholder="Street, city..." />
          </div>
        </form>
      </Modal>
    </div>
  )
}
