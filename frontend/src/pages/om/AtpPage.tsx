import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { checkAtp } from '@api/orderManagement'
import { getItems, getWarehouses } from '@api/inventory'
import type { AtpResult } from '@/types/orderManagement'
import type { ItemSummary, WarehouseSummary } from '@/types/inventory'

export function AtpPage() {
  const [itemId, setItemId] = useState('')
  const [warehouseId, setWarehouseId] = useState('')
  const [quantity, setQuantity] = useState(1)
  const [itemSearch, setItemSearch] = useState('')
  const [whSearch, setWhSearch] = useState('')
  const [showItemDrop, setShowItemDrop] = useState(false)
  const [showWhDrop, setShowWhDrop] = useState(false)
  const [result, setResult] = useState<AtpResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })

  const filteredItems = useMemo(() => {
    const q = itemSearch.trim().toLowerCase()
    if (!q) return items.slice(0, 10)
    return items.filter((i: ItemSummary) => i.itemCode.toLowerCase().includes(q) || i.description.toLowerCase().includes(q)).slice(0, 10)
  }, [items, itemSearch])

  const filteredWhs = useMemo(() => {
    const q = whSearch.trim().toLowerCase()
    if (!q) return warehouses.slice(0, 10)
    return warehouses.filter((w: WarehouseSummary) => w.warehouseCode.toLowerCase().includes(q) || w.warehouseName.toLowerCase().includes(q)).slice(0, 10)
  }, [warehouses, whSearch])

  const selectedItem = useMemo(() => items.find((i: ItemSummary) => i.id === itemId), [items, itemId])
  const selectedWh = useMemo(() => warehouses.find((w: WarehouseSummary) => w.id === warehouseId), [warehouses, warehouseId])

  async function check() {
    if (!itemId || !warehouseId) return
    setLoading(true)
    setError(null)
    try {
      const res = await checkAtp(itemId, warehouseId, Number(quantity))
      setResult((res as { data: AtpResult }).data ?? null)
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Available-to-Promise (ATP)</h1>
      <Card className="p-4 grid grid-cols-2 md:grid-cols-4 gap-3 items-end">
        <div className="relative">
          <Input value={selectedItem ? `${selectedItem.itemCode} - ${selectedItem.description}` : itemSearch}
            onChange={e => { setItemSearch(e.target.value); setItemId(''); setShowItemDrop(true) }}
            onFocus={() => setShowItemDrop(true)} onBlur={() => setTimeout(() => setShowItemDrop(false), 200)}
            label="Item" placeholder="Search item..." />
          {showItemDrop && filteredItems.length > 0 && (
            <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
              {filteredItems.map((i: ItemSummary) => (
                <button key={i.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                  onMouseDown={() => { setItemId(i.id); setItemSearch(i.itemCode); setShowItemDrop(false) }}>
                  <span className="font-medium">{i.itemCode}</span> <span className="text-gray-500 text-xs">{i.description}</span>
                </button>
              ))}
            </div>
          )}
        </div>
        <div className="relative">
          <Input value={selectedWh ? `${selectedWh.warehouseCode} - ${selectedWh.warehouseName}` : whSearch}
            onChange={e => { setWhSearch(e.target.value); setWarehouseId(''); setShowWhDrop(true) }}
            onFocus={() => setShowWhDrop(true)} onBlur={() => setTimeout(() => setShowWhDrop(false), 200)}
            label="Warehouse" placeholder="Search warehouse..." />
          {showWhDrop && filteredWhs.length > 0 && (
            <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
              {filteredWhs.map((w: WarehouseSummary) => (
                <button key={w.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                  onMouseDown={() => { setWarehouseId(w.id); setWhSearch(w.warehouseCode); setShowWhDrop(false) }}>
                  <span className="font-medium">{w.warehouseCode}</span> <span className="text-gray-500 text-xs">{w.warehouseName}</span>
                </button>
              ))}
            </div>
          )}
        </div>
        <Input type="number" value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} label="Quantity" />
        <Button variant="primary" disabled={!itemId || !warehouseId || loading} onClick={check}>
          {loading ? 'Checking...' : 'Check ATP'}
        </Button>
      </Card>
      {result && (
        <Card className="p-4 space-y-2">
          <div className="flex items-center gap-3">
            <span className="text-sm">Requested: <strong className="tabular-nums">{result.requestedQuantity}</strong></span>
            <span className="text-sm">Available: <strong className="tabular-nums">{result.available}</strong></span>
            <Badge variant={result.isSufficient ? 'success' : 'error'}>{result.isSufficient ? 'In Stock' : 'Backorder'}</Badge>
          </div>
          <p className="text-sm text-gray-500">Promised date: {new Date(result.promisedDate).toLocaleDateString()}</p>
        </Card>
      )}
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
