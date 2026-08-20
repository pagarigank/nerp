import { useState, useMemo } from 'react'
import { Settings2, Plus, Play, Package } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/orderManagement'
import { getItems, getWarehouses } from '@api/inventory'
import {
  getBuildOrders,
  getBuildOrder,
  createBuildOrder,
  releaseBuildOrder,
  completeBuildOrder,
  disassembleBuildOrder,
  getBomHeaders,
} from '@api/bom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { BuildOrderSummary } from '@/types/bom'
import type { ItemSummary, WarehouseSummary } from '@/types/inventory'
import type { BomHeaderSummary } from '@/types/bom'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toFixed(4)}` : '—')

export function BuildOrdersPage() {
  const queryClient = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [completeId, setCompleteId] = useState<string | null>(null)
  const [actualYield, setActualYield] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [statusFilter, setStatusFilter] = useState<string>('')

  const { data: orders = [], isLoading } = useQuery({
    queryKey: ['bom', 'buildOrders', statusFilter],
    queryFn: () => getBuildOrders(undefined, statusFilter || undefined),
  })

  const { data: items = [] } = useQuery({
    queryKey: ['inventory', 'items'],
    queryFn: () => getItems(),
  })

  const { data: warehouses = [] } = useQuery({
    queryKey: ['inventory', 'warehouses'],
    queryFn: () => getWarehouses(),
  })

  const { data: boms = [] } = useQuery({
    queryKey: ['bom', 'headers'],
    queryFn: () => getBomHeaders(),
  })

  const itemMap = useMemo(() => Object.fromEntries((items as ItemSummary[]).map(i => [i.id, i])), [items])
  const whMap = useMemo(() => Object.fromEntries((warehouses as WarehouseSummary[]).map(w => [w.id, w])), [warehouses])
  const bomMap = useMemo(() => Object.fromEntries((boms as BomHeaderSummary[]).map(b => [b.id, b])), [boms])

  const releaseMutation = useMutation({
    mutationFn: releaseBuildOrder,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bom', 'buildOrders'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const completeMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data?: any }) => completeBuildOrder(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bom', 'buildOrders'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const disassembleMutation = useMutation({
    mutationFn: ({ id }: { id: string }) => disassembleBuildOrder(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bom', 'buildOrders'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const columns: DataTableColumn<BuildOrderSummary>[] = [
    { key: 'buildNumber', header: 'Build #', sortable: true },
    { key: 'transactionType', header: 'Type' },
    {
      key: 'parentItemId',
      header: 'Parent Item',
      render: (r: BuildOrderSummary) => {
        const item = itemMap[r.parentItemId]
        return item ? `${item.itemCode}` : r.parentItemId.slice(0, 8)
      },
    },
    { key: 'quantityToBuild', header: 'Qty to Build', align: 'right' },
    {
      key: 'warehouseId',
      header: 'Warehouse',
      render: (r: BuildOrderSummary) => whMap[r.warehouseId]?.warehouseCode ?? '—',
    },
    { key: 'buildDate', header: 'Date', render: (r: BuildOrderSummary) => new Date(r.buildDate).toLocaleDateString() },
    {
      key: 'status',
      header: 'Status',
      render: (r: BuildOrderSummary) => (
        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
          r.status === 'Completed' ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
          : r.status === 'Released' ? 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400'
          : r.status === 'Cancelled' ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
          : 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300'
        }`}>{r.status}</span>
      ),
    },
    { key: 'totalCost', header: 'Total Cost', align: 'right', render: (r: BuildOrderSummary) => MONEY(r.totalCost) },
    { key: 'unitCost', header: 'Unit Cost', align: 'right', render: (r: BuildOrderSummary) => MONEY(r.unitCost) },
    {
      key: 'actions',
      header: 'Actions',
      render: (_: unknown, row: BuildOrderSummary) => (
        <div className="flex gap-1">
          {(row.status === 'Draft' || row.status === 'Planned') && (
            <Button size="sm" variant="outline" onClick={() => releaseMutation.mutate(row.id)}>
              <Play className="h-3.5 w-3.5 mr-1" /> Release
            </Button>
          )}
          {(row.status === 'Released' || row.status === 'InProgress') && (
            <Button size="sm" onClick={() => { setCompleteId(row.id); setActualYield('') }}>
              <Package className="h-3.5 w-3.5 mr-1" /> Complete
            </Button>
          )}
          {row.status === 'Completed' && row.transactionType === 'Assemble' && (
            <Button size="sm" variant="destructive" onClick={() => {
              if (confirm('Disassemble this build? Components will be restocked.')) {
                disassembleMutation.mutate({ id: row.id })
              }
            }}>Disassemble</Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <Package className="h-6 w-6" /> Build Orders
          </h1>
          <p className="mt-1 text-sm text-gray-500">Assemble or disassemble items using BOMs</p>
        </div>
        <Button onClick={() => setShowCreate(true)}><Plus className="h-4 w-4 mr-1" /> New Build Order</Button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{error}</div>
      )}

      <div className="flex gap-2">
        {['', 'Draft', 'Released', 'InProgress', 'Completed'].map(s => (
          <button
            key={s}
            onClick={() => setStatusFilter(s)}
            className={`px-3 py-1.5 text-sm rounded-md ${
              statusFilter === s ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-300'
            }`}
          >{s || 'All'}</button>
        ))}
      </div>

      <DataTable data={orders as BuildOrderSummary[]} columns={columns} isLoading={isLoading} emptyMessage="No build orders yet." />

      <CreateBuildOrderModal
        open={showCreate}
        onClose={() => setShowCreate(false)}
        onSubmit={(data) => {
          createBuildOrder(data)
            .then(() => {
              queryClient.invalidateQueries({ queryKey: ['bom', 'buildOrders'] })
              setShowCreate(false)
            })
            .catch((e: any) => setError(getErrorMessage(e)))
        }}
        boms={boms as BomHeaderSummary[]}
        itemMap={itemMap}
        warehouses={warehouses as WarehouseSummary[]}
      />

      {/* Complete Build Order Modal */}
      <Modal title="Complete Build Order" isOpen={!!completeId} onClose={() => setCompleteId(null)}>
        <div className="space-y-4">
          <Input
            label="Actual Yield (optional)"
            type="number"
            step="0.01"
            min="0"
            value={actualYield}
            onChange={(e) => setActualYield(e.target.value)}
            placeholder="Enter actual quantity produced..."
          />
          <p className="text-sm text-gray-500">Leave blank to use the planned quantity.</p>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => setCompleteId(null)}>Cancel</Button>
            <Button onClick={() => {
              if (!completeId) return
              const payload: any = {}
              if (actualYield) payload.actualYield = Number(actualYield)
              completeMutation.mutate({ id: completeId, data: payload })
              setCompleteId(null)
            }}>Complete</Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

function CreateBuildOrderModal({ open, onClose, onSubmit, boms, itemMap, warehouses }: {
  open: boolean
  onClose: () => void
  onSubmit: (data: any) => void
  boms: BomHeaderSummary[]
  itemMap: Record<string, ItemSummary>
  warehouses: WarehouseSummary[]
}) {
  const [bomId, setBomId] = useState('')
  const [buildNumber, setBuildNumber] = useState('')
  const [txnType, setTxnType] = useState('Assemble')
  const [qty, setQty] = useState(1)
  const [whId, setWhId] = useState('')
  const [date, setDate] = useState(new Date().toISOString().split('T')[0])
  const [notes, setNotes] = useState('')

  const selectedBom = boms.find(b => b.id === bomId)

  if (!open) return null

  return (
    <Modal title="Create Build Order" isOpen={open} onClose={onClose}>
      <div className="space-y-4">
        <Select
          value={bomId}
          onChange={(e) => setBomId(e.target.value)}
          label="BOM"
          placeholder="Select BOM..."
          options={boms.filter(b => b.status === 'Active').map(b => ({
            value: b.id,
            label: `${itemMap[b.parentItemId]?.itemCode ?? '?'} Rev ${b.revision} (${b.componentCount} components)`,
          }))}
        />
        <Input label="Build Number" value={buildNumber} onChange={(e) => setBuildNumber(e.target.value)} />
        <Select
          value={txnType}
          onChange={(e) => setTxnType(e.target.value)}
          label="Transaction Type"
          options={[
            { value: 'Assemble', label: 'Assemble' },
            { value: 'Disassemble', label: 'Disassemble' },
          ]}
        />
        <div className="grid grid-cols-2 gap-4">
          <Input label="Quantity to Build" type="number" value={qty} onChange={(e) => setQty(Number(e.target.value))} />
          <Input label="Build Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </div>
        <Select
          value={whId}
          onChange={(e) => setWhId(e.target.value)}
          label="Warehouse"
          placeholder="Select warehouse..."
          options={warehouses.map(w => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` }))}
        />
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Optional notes..." />
        {selectedBom && (
          <p className="text-sm text-gray-500">
            BOM: {itemMap[selectedBom.parentItemId]?.itemCode} Rev {selectedBom.revision} — {selectedBom.componentCount} components
          </p>
        )}
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={() => {
            if (!selectedBom) return
            onSubmit({
              companyId: currentCompanyId(),
              buildNumber,
              transactionType: txnType,
              bomHeaderId: bomId,
              parentItemId: selectedBom.parentItemId,
              quantityToBuild: qty,
              unitOfMeasure: 'EA',
              warehouseId: whId,
              buildDate: date,
              notes: notes || null,
            })
          }}>Create</Button>
        </div>
      </div>
    </Modal>
  )
}
