import { useState, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle, Pencil, ArrowLeftRight } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getItems,
  getWarehouses,
  getWarehouseBins,
  getItemStockByLocation,
  createReceipt,
  createAdjustment,
  createTransfer,
  companyId,
} from '@api/inventory'
import type { ItemStockSummary } from '@/types/inventory'

const assignSchema = z.object({
  itemId: z.string().min(1, 'Item is required'),
  warehouseId: z.string().min(1, 'Warehouse is required'),
  binId: z.string().optional(),
  quantity: z.coerce.number().positive('Quantity must be > 0'),
  unitCost: z.coerce.number().min(0).optional(),
  referenceNumber: z.string().optional(),
  notes: z.string().optional(),
})
type AssignForm = z.infer<typeof assignSchema>
const assignDefaults: AssignForm = {
  itemId: '',
  warehouseId: '',
  binId: '',
  quantity: 1,
  unitCost: 0,
  referenceNumber: '',
  notes: '',
}

const adjustSchema = z.object({
  binId: z.string().optional(),
  quantityAdjustment: z.coerce.number({ invalid_type_error: 'Enter a number' }).refine((n) => n !== 0, 'Use a non-zero adjustment'),
  reasonCode: z.string().optional(),
  notes: z.string().optional(),
})
type AdjustForm = z.infer<typeof adjustSchema>

export function StockByLocationPage() {
  const qc = useQueryClient()
  const [whId, setWhId] = useState('')
  const [search, setSearch] = useState('')
  const [assignOpen, setAssignOpen] = useState(false)
  const [adjustTarget, setAdjustTarget] = useState<ItemStockSummary | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items-mini'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'wh-mini'], queryFn: () => getWarehouses() })
  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['inventory', 'stock-by-location', whId],
    queryFn: () => getItemStockByLocation(whId || undefined),
  })

  const whOptions = [{ value: '', label: 'All warehouses' }, ...warehouses.map((w) => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` }))]
  const itemOptions = useMemo(() => items.map((i) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])
  const name = (id?: string) => (id ? items.find((i) => i.id === id)?.itemCode ?? id.slice(0, 8) : '—')

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter((r) => name(r.itemId).toLowerCase().includes(q) || (r.warehouseCode ?? '').toLowerCase().includes(q))
  }, [rows, search])

  const assignForm = useForm<AssignForm>({ resolver: zodResolver(assignSchema), defaultValues: assignDefaults })
  const assignWh = assignForm.watch('warehouseId')
  const { data: assignBins = [] } = useQuery({
    queryKey: ['inventory', 'bins', assignWh],
    queryFn: () => getWarehouseBins(assignWh || undefined),
    enabled: !!assignWh,
  })
  const assignBinOptions = useMemo(
    () => [{ value: '', label: 'No bin (default)' }, ...assignBins.map((b: any) => ({ value: b.id, label: [b.binCode, b.aisle && `Aisle ${b.aisle}`, b.rack && `Rack ${b.rack}`, b.shelf && `Shelf ${b.shelf}`].filter(Boolean).join(' / ') }))],
    [assignBins],
  )

  const assignMut = useMutation({
    mutationFn: (d: AssignForm) =>
      createReceipt({
        companyId: companyId(),
        itemId: d.itemId,
        warehouseId: d.warehouseId,
        binId: d.binId || null,
        quantity: d.quantity,
        unitCost: d.unitCost ?? 0,
        referenceNumber: d.referenceNumber || undefined,
        notes: d.notes || undefined,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['inventory', 'stock-by-location'] })
      qc.invalidateQueries({ queryKey: ['inventory', 'item-stock'] })
      setAssignOpen(false)
      assignForm.reset(assignDefaults)
    },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  // Adjust: re-qty an existing location row (posts an adjustment to that bin)
  const adjustForm = useForm<AdjustForm>({ defaultValues: { binId: '', quantityAdjustment: 0, reasonCode: '', notes: '' } })
  const adjustMut = useMutation({
    mutationFn: (d: AdjustForm) => {
      if (!adjustTarget) return Promise.reject(new Error('No target row'))
      return createAdjustment({
        companyId: companyId(),
        itemId: adjustTarget.itemId,
        warehouseId: adjustTarget.warehouseId,
        binId: adjustTarget.binId ?? null,
        quantityAdjustment: d.quantityAdjustment,
        reasonCode: d.reasonCode || 'ADJ',
        notes: d.notes || undefined,
      })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['inventory', 'stock-by-location'] })
      qc.invalidateQueries({ queryKey: ['inventory', 'item-stock'] })
      setAdjustTarget(null)
    },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  // Move (re-bin): transfer an existing location row to another bin in the same warehouse
  const moveMut = useMutation({
    mutationFn: (toBinId: string) => {
      if (!adjustTarget) return Promise.reject(new Error('No target row'))
      return createTransfer({
        companyId: companyId(),
        itemId: adjustTarget.itemId,
        fromWarehouseId: adjustTarget.warehouseId,
        toWarehouseId: adjustTarget.warehouseId,
        fromBinId: adjustTarget.binId ?? null,
        toBinId: toBinId || null,
        quantity: adjustTarget.quantityOnHand,
      })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['inventory', 'stock-by-location'] })
      qc.invalidateQueries({ queryKey: ['inventory', 'item-stock'] })
      setAdjustTarget(null)
    },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const adjustWhBins = useQuery({
    queryKey: ['inventory', 'bins', adjustTarget?.warehouseId],
    queryFn: () => getWarehouseBins(adjustTarget!.warehouseId),
    enabled: !!adjustTarget,
  })
  const moveBinOptions = useMemo(
    () => [{ value: '', label: 'No bin (default)' }, ...(adjustWhBins.data ?? []).map((b: any) => ({ value: b.id, label: [b.binCode, b.aisle && `Aisle ${b.aisle}`, b.rack && `Rack ${b.rack}`, b.shelf && `Shelf ${b.shelf}`].filter(Boolean).join(' / ') }))],
    [adjustWhBins.data],
  )

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5" /> <span className="text-sm">{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Stock by Location"
          description="On-hand, allocated and available quantity per warehouse, bin and lot. Assign items to bins or adjust/bin-move existing locations."
          action={<Button variant="primary" size="sm" onClick={() => { setFormError(null); setAssignOpen(true) }} leftIcon={<Plus className="h-4 w-4" />}>Assign to Location</Button>}
        />
        <CardContent>
          <div className="mb-3 flex gap-3 max-w-2xl">
            <Input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search by item or warehouse..." aria-label="Search stock" leftIcon={<Search className="h-4 w-4" />} />
            <Select options={whOptions} value={whId} onChange={(e) => setWhId((e.target as HTMLSelectElement).value)} aria-label="Filter warehouse" />
          </div>
          {isLoading ? (
            <p className="text-sm text-gray-500 py-8 text-center">Loading…</p>
          ) : filtered.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No stock rows. Use "Assign to Location" to place stock into a bin.'}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Item</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Bin</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Lot</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">On Hand</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Allocated</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Available</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map((r) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{name(r.itemId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.binId ? r.binId.slice(0, 8) : '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.lotId ? r.lotId.slice(0, 8) : '—'}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.onHandQuantity}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.allocatedQuantity}</td>
                      <td className="px-3 py-3 text-right">
                        <Badge variant={r.availableQuantity < 0 ? 'error' : 'success'} size="sm" dot>
                          {r.availableQuantity}
                        </Badge>
                      </td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          <Button size="sm" variant="outline" onClick={() => { setFormError(null); setAdjustTarget(r) }}>
                            <Pencil className="h-3.5 w-3.5" /> Adjust
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => { setFormError(null); setAdjustTarget(r) }}>
                            <ArrowLeftRight className="h-3.5 w-3.5" /> Move
                          </Button>
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

      {/* Assign to Location modal */}
      <Modal
        isOpen={assignOpen}
        onClose={() => setAssignOpen(false)}
        title="Assign Item to Location"
        description="Receive stock into a warehouse bin (posts a receipt)."
        footer={
          <>
            <Button variant="secondary" onClick={() => setAssignOpen(false)} disabled={assignMut.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={assignForm.handleSubmit((d) => assignMut.mutate(d))} isLoading={assignMut.isPending}>
              Receive to Bin
            </Button>
          </>
        }
      >
        <form className="space-y-4" noValidate>
          <Select {...assignForm.register('itemId')} label="Item" options={itemOptions} required />
          <Select {...assignForm.register('warehouseId')} label="Warehouse" options={whOptions.filter((o) => o.value !== '')} required />
          <Select {...assignForm.register('binId')} label="Bin" options={assignBinOptions} />
          <Input {...assignForm.register('quantity')} label="Quantity" type="number" step="0.01" min="0.01" required />
          <Input {...assignForm.register('unitCost')} label="Unit Cost" type="number" step="0.01" min="0" />
          <Input {...assignForm.register('referenceNumber')} label="Reference #" />
          <Input {...assignForm.register('notes')} label="Notes" />
        </form>
      </Modal>

      {/* Adjust / Move modal */}
      <Modal
        isOpen={!!adjustTarget}
        onClose={() => setAdjustTarget(null)}
        title={adjustTarget ? `Location: ${name(adjustTarget.itemId)} @ ${adjustTarget.warehouseCode}${adjustTarget.binId ? ' / ' + adjustTarget.binId.slice(0, 8) : ''}` : ''}
        description="Adjust quantity (posts an inventory adjustment) or move the whole location to another bin (posts a transfer)."
        footer={
          <>
            <Button variant="secondary" onClick={() => setAdjustTarget(null)} disabled={adjustMut.isPending || moveMut.isPending}>
              Cancel
            </Button>
            <Button variant="outline" onClick={adjustForm.handleSubmit((d) => adjustMut.mutate(d))} isLoading={adjustMut.isPending}>
              Apply Adjustment
            </Button>
            <Button
              variant="primary"
              onClick={() => {
                const to = (document.getElementById('moveToBin') as HTMLSelectElement | null)?.value ?? ''
                moveMut.mutate(to)
              }}
              isLoading={moveMut.isPending}
            >
              Move to Bin
            </Button>
          </>
        }
      >
        {adjustTarget && (
          <div className="space-y-4">
            <div className="rounded-lg bg-gray-50 dark:bg-gray-800/60 p-3 text-sm text-gray-700 dark:text-gray-300">
              Current on hand: <span className="font-semibold">{adjustTarget.onHandQuantity}</span>
            </div>
            <div>
              <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Adjust quantity</p>
              <Input {...adjustForm.register('quantityAdjustment')} label="Quantity Adjustment (+/-)" type="number" step="0.01" placeholder="e.g. 5 or -3" />
              <Input {...adjustForm.register('reasonCode')} label="Reason Code" placeholder="e.g. DAMAGE" />
            </div>
            <div>
              <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Move to another bin (same warehouse)</p>
              <Select id="moveToBin" label="To Bin" options={moveBinOptions} />
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
