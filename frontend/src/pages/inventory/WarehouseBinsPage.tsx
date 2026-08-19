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
import { getWarehouseBins, createWarehouseBin, getWarehouses } from '@api/inventory'
import type { CreateWarehouseBinRequest } from '@/types/inventory'

const schema = z.object({
  warehouseId: z.string().min(1, 'Warehouse is required'),
  binCode: z.string().trim().min(1, 'Bin code is required'),
  aisle: z.string().optional(),
  rack: z.string().optional(),
  shelf: z.string().optional(),
})
type Form = z.infer<typeof schema>
const defaults: Form = { warehouseId: '', binCode: '', aisle: '', rack: '', shelf: '' }
function fieldError(message?: string) { return message ? { error: message } : {} }

export function WarehouseBinsPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { errors } } = useForm<Form>({ resolver: zodResolver(schema), defaultValues: defaults })

  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })
  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'warehouse-bins'], queryFn: () => getWarehouseBins() })

  const warehouseOptions = useMemo(() => warehouses.map(w => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` })), [warehouses])
  const whName = (id: string) => warehouses.find(w => w.id === id)?.warehouseCode ?? id

  const createMutation = useMutation({
    mutationFn: (d: CreateWarehouseBinRequest) => createWarehouseBin(d),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'warehouse-bins'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); reset(defaults); setOpen(true) }
  const onSubmit = (d: Form) => {
    setFormError(null)
    const body: CreateWarehouseBinRequest = { warehouseId: d.warehouseId, binCode: d.binCode }
    if (d.aisle) body.aisle = d.aisle
    if (d.rack) body.rack = d.rack
    if (d.shelf) body.shelf = d.shelf
    createMutation.mutate(body)
  }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter(r => r.binCode.toLowerCase().includes(q) || (r.aisle ?? '').toLowerCase().includes(q))
  }, [rows, search])

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5" /> <span className="text-sm">{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Warehouse Bins" description={`${rows.length} bin(s)`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New Bin</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search..." aria-label="Search bins" leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No bins yet.'}</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th><th className="px-3 py-2 font-medium text-gray-500">Bin</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Aisle</th><th className="px-3 py-2 font-medium text-gray-500">Rack</th><th className="px-3 py-2 font-medium text-gray-500">Shelf</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{whName(r.warehouseId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.binCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.aisle ?? '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.rack ?? '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.shelf ?? '—'}</td>
                      <td className="px-3 py-3"><Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
      <Modal isOpen={open} onClose={close} title="New Bin"
        footer={<><Button variant="secondary" onClick={close} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>Create</Button></>}>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Select {...register('warehouseId')} label="Warehouse" options={warehouseOptions} {...fieldError(errors.warehouseId?.message)} required />
          <Input {...register('binCode')} label="Bin Code" {...fieldError(errors.binCode?.message)} required />
          <div className="grid grid-cols-3 gap-3">
            <Input {...register('aisle')} label="Aisle" />
            <Input {...register('rack')} label="Rack" />
            <Input {...register('shelf')} label="Shelf" />
          </div>
        </form>
      </Modal>
    </div>
  )
}
