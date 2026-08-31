import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getWarehouseBins,
  createWarehouseBin,
  updateWarehouseBinLocation,
  deactivateWarehouseBin,
  activateWarehouseBin,
  getWarehouses,
  companyId,
} from '@api/inventory'
import type { CreateWarehouseBinRequest, WarehouseBinSummary } from '@/types/inventory'

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
  const [editTarget, setEditTarget] = useState<WarehouseBinSummary | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const { register, handleSubmit, reset, formState: { errors } } = useForm<Form>({ resolver: zodResolver(schema), defaultValues: defaults })

  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })
  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'warehouse-bins'], queryFn: () => getWarehouseBins() })

  const warehouseOptions = useMemo(() => warehouses.map(w => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` })), [warehouses])
  const whName = (id: string) => warehouses.find(w => w.id === id)?.warehouseCode ?? id

  const openForm = () => {
    setEditTarget(null)
    setFormError(null)
    reset(defaults)
    setOpen(true)
  }
  const openEdit = (bin: WarehouseBinSummary) => {
    setEditTarget(bin)
    setFormError(null)
    reset({ warehouseId: bin.warehouseId, binCode: bin.binCode, aisle: bin.aisle ?? '', rack: bin.rack ?? '', shelf: bin.shelf ?? '' })
    setOpen(true)
  }
  const close = () => { setOpen(false); setEditTarget(null); setFormError(null) }

  const createMutation = useMutation({
    mutationFn: (d: CreateWarehouseBinRequest) => createWarehouseBin(d),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'warehouse-bins'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const updateMutation = useMutation({
    mutationFn: (d: Form) => updateWarehouseBinLocation(editTarget!.id, { aisle: d.aisle || null, rack: d.rack || null, shelf: d.shelf || null }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'warehouse-bins'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const onSubmit = (d: Form) => {
    setFormError(null)
    if (editTarget) {
      updateMutation.mutate(d)
      return
    }
    const body: CreateWarehouseBinRequest = { warehouseId: d.warehouseId, binCode: d.binCode, companyId: companyId() }
    if (d.aisle) body.aisle = d.aisle
    if (d.rack) body.rack = d.rack
    if (d.shelf) body.shelf = d.shelf
    createMutation.mutate(body)
  }

  const toggleMutation = useMutation({
    mutationFn: (bin: WarehouseBinSummary) => (bin.isActive ? deactivateWarehouseBin(bin.id) : activateWarehouseBin(bin.id)),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'warehouse-bins'] }) },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter(r => r.binCode.toLowerCase().includes(q) || (r.aisle ?? '').toLowerCase().includes(q))
  }, [rows, search])

  const isEdit = !!editTarget

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
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
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
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          <Button size="sm" variant="outline" onClick={() => openEdit(r)}><Pencil className="h-3.5 w-3.5" /> Edit</Button>
                          <Button size="sm" variant="outline" onClick={() => toggleMutation.mutate(r)} disabled={toggleMutation.isPending}>
                            {r.isActive ? <><Trash2 className="h-3.5 w-3.5" /> Delete</> : 'Restore'}
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
      <Modal isOpen={open} onClose={close} title={isEdit ? `Edit Bin: ${editTarget?.binCode}` : 'New Bin'}
        footer={<><Button variant="secondary" onClick={close} disabled={createMutation.isPending || updateMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending || updateMutation.isPending}>{isEdit ? 'Save' : 'Create'}</Button></>}>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Select {...register('warehouseId')} label="Warehouse" options={warehouseOptions} {...fieldError(errors.warehouseId?.message)} required disabled={isEdit} />
          <Input {...register('binCode')} label="Bin Code" {...fieldError(errors.binCode?.message)} required disabled={isEdit} />
          <div className="grid grid-cols-3 gap-3">
            <Input {...register('aisle')} label="Aisle" />
            <Input {...register('rack')} label="Rack" />
            <Input {...register('shelf')} label="Shelf" />
          </div>
          {isEdit && <p className="text-xs text-gray-500">Warehouse and Bin Code are the bin's identity and cannot be changed. Use Delete to deactivate this bin if needed.</p>}
        </form>
      </Modal>
    </div>
  )
}
