import { useEffect, useState, useCallback } from 'react'
import { Settings2, Plus, Trash2, Package } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Select } from '@components/ui/Select'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/orderManagement'
import {
  getBomHeaders,
  createBomHeader,
  updateBomHeader,
  deleteBomHeader,
  getBomComponents,
  addBomComponent,
  deleteBomComponent,
  getCostRollup,
} from '@api/bom'
import { getItems } from '@api/inventory'
import { getWorkCenters } from '@api/bom'
import type { BomHeaderSummary, BomComponentLine, BomExplosionLine } from '@/types/bom'
import type { ItemSummary } from '@/types/inventory'
import type { WorkCenterSummary } from '@/types/bom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

const YESNO = (v: boolean) => (v ? 'Yes' : 'No')
const MONEY = (v: number | null) => (v != null ? `$${Number(v).toFixed(4)}` : '—')

export function BomsPage() {
  const queryClient = useQueryClient()
  const [selectedBomId, setSelectedBomId] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [showComponentModal, setShowComponentModal] = useState(false)
  const [showCostModal, setShowCostModal] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Data queries
  const { data: boms = [], isLoading } = useQuery({
    queryKey: ['bom', 'headers'],
    queryFn: () => getBomHeaders(),
  })

  const { data: items = [] } = useQuery({
    queryKey: ['inventory', 'items'],
    queryFn: () => getItems(),
  })

  const { data: workCenters = [] } = useQuery({
    queryKey: ['bom', 'workCenters'],
    queryFn: () => getWorkCenters(),
  })

  const itemMap = Object.fromEntries((items as ItemSummary[]).map(i => [i.id, i]))
  const wcMap = Object.fromEntries((workCenters as WorkCenterSummary[]).map(w => [w.id, w]))

  const itemOptions = (items as ItemSummary[]).map(i => ({ value: i.id, label: `${i.itemCode} - ${i.description}` }))
  const wcOptions = (workCenters as WorkCenterSummary[]).map(w => ({ value: w.id, label: `${w.code} - ${w.name}` }))

  // Components for selected BOM
  const { data: components = [] } = useQuery({
    queryKey: ['bom', 'components', selectedBomId],
    queryFn: () => getBomComponents(selectedBomId!),
    enabled: !!selectedBomId,
  })

  // Cost rollup
  const { data: costRollup } = useQuery({
    queryKey: ['bom', 'costRollup', selectedBomId],
    queryFn: () => getCostRollup(selectedBomId!),
    enabled: !!selectedBomId && showCostModal,
  })

  // Mutations
  const createMutation = useMutation({
    mutationFn: createBomHeader,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bom', 'headers'] })
      setShowCreate(false)
    },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteBomHeader,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bom', 'headers'] })
      if (selectedBomId) setSelectedBomId(null)
    },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const addComponentMutation = useMutation({
    mutationFn: ({ bomHeaderId, data }: { bomHeaderId: string; data: any }) =>
      addBomComponent(bomHeaderId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bom', 'components'] })
      setShowComponentModal(false)
    },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const deleteComponentMutation = useMutation({
    mutationFn: ({ bomHeaderId, lineId }: { bomHeaderId: string; lineId: string }) =>
      deleteBomComponent(bomHeaderId, lineId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['bom', 'components'] })
    },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  // BOM columns
  const bomColumns: DataTableColumn<BomHeaderSummary>[] = [
    { key: 'revision', header: 'Revision', sortable: true },
    { key: 'bomType', header: 'Type', render: (r: BomHeaderSummary) => r.bomType },
    {
      key: 'parentItemId',
      header: 'Parent Item',
      render: (r: BomHeaderSummary) => {
        const item = itemMap[r.parentItemId]
        return item ? `${item.itemCode} - ${item.description}` : r.parentItemId.slice(0, 8)
      },
    },
    { key: 'componentCount', header: 'Components', align: 'right' },
    { key: 'yieldPercentage', header: 'Yield %', align: 'right', render: (r: BomHeaderSummary) => `${r.yieldPercentage}%` },
    { key: 'estimatedMaterialCost', header: 'Mat. Cost', align: 'right', render: (r: BomHeaderSummary) => MONEY(r.estimatedMaterialCost) },
    { key: 'status', header: 'Status', render: (r: BomHeaderSummary) => (
      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
        r.status === 'Active' ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
        : r.status === 'Obsolete' ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
        : 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300'
      }`}>{r.status}</span>
    )},
  ]

  // Component columns
  const compColumns: DataTableColumn<BomComponentLine>[] = [
    { key: 'operationSequence', header: 'Op #', align: 'right' },
    {
      key: 'componentItemId',
      header: 'Component',
      render: (r: BomComponentLine) => {
        const item = itemMap[r.componentItemId]
        return item ? `${item.itemCode} - ${item.description}` : r.componentItemId.slice(0, 8)
      },
    },
    { key: 'quantityPerParent', header: 'Qty/Parent', align: 'right' },
    { key: 'effectiveQuantity', header: 'Eff. Qty', align: 'right' },
    { key: 'unitOfMeasure', header: 'UOM' },
    { key: 'scrapFactor', header: 'Scrap %', align: 'right', render: (r: BomComponentLine) => `${r.scrapFactor}%` },
    { key: 'isPhantom', header: 'Phantom', render: (r: BomComponentLine) => YESNO(r.isPhantom) },
    { key: 'isCritical', header: 'Critical', render: (r: BomComponentLine) => YESNO(r.isCritical) },
    {
      key: 'workCenterId',
      header: 'Work Center',
      render: (r: BomComponentLine) => {
        if (!r.workCenterId) return '—'
        const wc = wcMap[r.workCenterId]
        return wc ? `${wc.code}` : r.workCenterId.slice(0, 8)
      },
    },
    {
      key: 'actions',
      header: '',
      render: (_: unknown, row: BomComponentLine) => (
        <Button size="sm" variant="destructive" onClick={() => {
          if (selectedBomId) deleteComponentMutation.mutate({ bomHeaderId: selectedBomId, lineId: row.id })
        }}>
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
      ),
    },
  ]

  const selectedBom = (boms as BomHeaderSummary[]).find((b: BomHeaderSummary) => b.id === selectedBomId)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <Settings2 className="h-6 w-6" /> Bill of Materials
          </h1>
          <p className="mt-1 text-sm text-gray-500">Define product structures and manage assemblies</p>
        </div>
        <Button onClick={() => setShowCreate(true)}><Plus className="h-4 w-4 mr-1" /> New BOM</Button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{error}</div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* BOM List */}
        <div>
          <DataTable
            data={boms as BomHeaderSummary[]}
            columns={bomColumns}
            isLoading={isLoading}
            onRowClick={(row) => setSelectedBomId(row.id)}
            selectedRowId={selectedBomId}
            emptyMessage="No BOMs defined yet."
          />
        </div>

        {/* Component detail panel */}
        <div className="space-y-4">
          {selectedBom ? (
            <>
              <div className="rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="font-semibold text-gray-900 dark:text-white">
                      {itemMap[selectedBom.parentItemId]?.itemCode ?? 'Unknown'} — Revision {selectedBom.revision}
                    </h3>
                    <p className="text-sm text-gray-500">{itemMap[selectedBom.parentItemId]?.description}</p>
                    <p className="text-xs text-gray-400 mt-1">
                      Status: {selectedBom.status} | Type: {selectedBom.bomType} | Yield: {selectedBom.yieldPercentage}%
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline" onClick={() => setShowComponentModal(true)}>
                      <Plus className="h-3.5 w-3.5 mr-1" /> Add Component
                    </Button>
                    <Button size="sm" variant="outline" onClick={() => setShowCostModal(true)}>
                      <Package className="h-3.5 w-3.5 mr-1" /> Cost Rollup
                    </Button>
                    <Button size="sm" variant="destructive" onClick={() => {
                      if (confirm('Delete this BOM?')) deleteMutation.mutate(selectedBom.id)
                    }}>
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </div>
              </div>

              <DataTable
                data={components as BomComponentLine[]}
                columns={compColumns}
                emptyMessage="No components defined. Click 'Add Component' to start."
              />
            </>
          ) : (
            <div className="flex h-64 items-center justify-center rounded-lg border border-dashed border-gray-300 dark:border-gray-600">
              <p className="text-sm text-gray-500">Select a BOM to view components</p>
            </div>
          )}
        </div>
      </div>

      {/* Create BOM Modal */}
      <CreateBomModal
        open={showCreate}
        onClose={() => setShowCreate(false)}
        onSubmit={(data) => createMutation.mutate(data)}
        itemOptions={itemOptions}
      />

      {/* Add Component Modal */}
      <AddComponentModal
        open={showComponentModal}
        onClose={() => setShowComponentModal(false)}
        onSubmit={(data) => {
          if (selectedBomId) addComponentMutation.mutate({ bomHeaderId: selectedBomId, data })
        }}
        itemOptions={itemOptions}
        wcOptions={wcOptions}
      />

      {/* Cost Rollup Modal */}
      {showCostModal && selectedBomId && (
        <Modal
          title="BOM Cost Rollup"
          isOpen={showCostModal}
          onClose={() => setShowCostModal(false)}
        >
          {costRollup && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div><span className="text-gray-500">Total Material Cost:</span> <span className="font-medium">${costRollup.totalMaterialCost.toFixed(4)}</span></div>
                <div><span className="text-gray-500">Total Cost:</span> <span className="font-medium">${costRollup.totalCost.toFixed(4)}</span></div>
                <div><span className="text-gray-500">Yield:</span> <span className="font-medium">{costRollup.yieldPercentage}%</span></div>
              </div>
              <table className="w-full text-sm">
                <thead className="bg-gray-50 dark:bg-gray-800">
                  <tr>
                    <th className="px-3 py-2 text-left">Component</th>
                    <th className="px-3 py-2 text-right">Qty</th>
                    <th className="px-3 py-2 text-right">Unit Cost</th>
                    <th className="px-3 py-2 text-right">Ext. Cost</th>
                  </tr>
                </thead>
                <tbody>
                  {costRollup.components.map((c: any, i: number) => (
                    <tr key={i} className="border-t dark:border-gray-700">
                      <td className="px-3 py-2">{itemMap[c.componentItemId]?.itemCode ?? c.componentItemId.slice(0, 8)}</td>
                      <td className="px-3 py-2 text-right">{c.effectiveQuantity}</td>
                      <td className="px-3 py-2 text-right">${c.unitCost.toFixed(4)}</td>
                      <td className="px-3 py-2 text-right">${c.extendedCost.toFixed(4)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Modal>
      )}
    </div>
  )
}

// --- Create BOM Modal ---
function CreateBomModal({ open, onClose, onSubmit, itemOptions }: {
  open: boolean
  onClose: () => void
  onSubmit: (data: any) => void
  itemOptions: { value: string; label: string }[]
}) {
  const [parentId, setParentId] = useState('')
  const [revision, setRevision] = useState('A')
  const [bomType, setBomType] = useState('Standard')
  const [description, setDescription] = useState('')
  const [yieldPct, setYieldPct] = useState(100)

  if (!open) return null

  return (
    <Modal title="Create BOM" isOpen={open} onClose={onClose}>
      <div className="space-y-4">
        <Select
          value={parentId}
          onChange={(e) => setParentId(e.target.value)}
          label="Parent Item"
          placeholder="Select item..."
          options={itemOptions}
        />
        <Input label="Revision" value={revision} onChange={(e) => setRevision(e.target.value)} />
        <Select
          value={bomType}
          onChange={(e) => setBomType(e.target.value)}
          label="BOM Type"
          options={[
            { value: 'Standard', label: 'Standard' },
            { value: 'Phantom', label: 'Phantom' },
            { value: 'Alternate', label: 'Alternate' },
          ]}
        />
        <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
        <Input label="Yield %" type="number" value={yieldPct} onChange={(e) => setYieldPct(Number(e.target.value))} />
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={() => onSubmit({
            companyId: currentCompanyId(),
            parentItemId: parentId,
            revision,
            bomType,
            description: description || null,
            yieldPercentage: yieldPct,
          })}>Create</Button>
        </div>
      </div>
    </Modal>
  )
}

// --- Add Component Modal ---
function AddComponentModal({ open, onClose, onSubmit, itemOptions, wcOptions }: {
  open: boolean
  onClose: () => void
  onSubmit: (data: any) => void
  itemOptions: { value: string; label: string }[]
  wcOptions: { value: string; label: string }[]
}) {
  const [compItemId, setCompItemId] = useState('')
  const [qty, setQty] = useState(1)
  const [uom, setUom] = useState('EA')
  const [scrap, setScrap] = useState(0)
  const [opSeq, setOpSeq] = useState(10)
  const [workCenterId, setWorkCenterId] = useState('')
  const [isPhantom, setIsPhantom] = useState(false)
  const [isCritical, setIsCritical] = useState(false)
  const [notes, setNotes] = useState('')

  if (!open) return null

  return (
    <Modal title="Add Component" isOpen={open} onClose={onClose}>
      <div className="space-y-4">
        <Select
          value={compItemId}
          onChange={(e) => setCompItemId(e.target.value)}
          label="Component Item"
          placeholder="Select item..."
          options={itemOptions}
        />
        <div className="grid grid-cols-2 gap-4">
          <Input label="Quantity per Parent" type="number" value={qty} onChange={(e) => setQty(Number(e.target.value))} />
          <Input label="UOM" value={uom} onChange={(e) => setUom(e.target.value)} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <Input label="Scrap Factor %" type="number" value={scrap} onChange={(e) => setScrap(Number(e.target.value))} />
          <Input label="Operation Sequence" type="number" value={opSeq} onChange={(e) => setOpSeq(Number(e.target.value))} />
        </div>
        <Select
          value={workCenterId}
          onChange={(e) => setWorkCenterId(e.target.value)}
          label="Work Center"
          placeholder="Optional..."
          options={wcOptions}
        />
        <div className="flex gap-4">
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={isPhantom} onChange={(e) => setIsPhantom(e.target.checked)} className="rounded" />
            Phantom
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={isCritical} onChange={(e) => setIsCritical(e.target.checked)} className="rounded" />
            Critical
          </label>
        </div>
        <Input label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} />
        <div className="flex justify-end gap-2 pt-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={() => onSubmit({
            componentItemId: compItemId,
            quantityPerParent: qty,
            unitOfMeasure: uom,
            scrapFactor: scrap,
            operationSequence: opSeq,
            workCenterId: workCenterId || null,
            isPhantom,
            isCritical,
            notes: notes || null,
          })}>Add Component</Button>
        </div>
      </div>
    </Modal>
  )
}
