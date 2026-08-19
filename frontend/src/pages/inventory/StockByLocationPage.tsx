import { useQuery } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge } from '@components/ui/Badge'
import { Select } from '@components/ui/Input'
import { useState } from 'react'
import { getItems, getWarehouses, getItemStockByLocation } from '@api/inventory'

export function StockByLocationPage() {
  const [whId, setWhId] = useState('')
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items-mini'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'wh-mini'], queryFn: () => getWarehouses() })
  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['inventory', 'stock-by-location', whId],
    queryFn: () => getItemStockByLocation(whId || undefined),
  })

  const name = (id: string) => items.find(i => i.id === id)?.itemCode ?? id.slice(0, 8)
  const whOptions = [{ value: '', label: 'All warehouses' }, ...warehouses.map(w => ({ value: w.id, label: w.warehouseName ?? w.warehouseCode }))]

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Stock by Location / Lot" description="On-hand, allocated and available quantity per warehouse, bin and lot." />
        <CardContent>
          <div className="mb-3 max-w-xs">
            <Select options={whOptions} value={whId} onChange={e => setWhId((e.target as HTMLSelectElement).value)} />
          </div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No stock rows.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Item</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Bin</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Lot</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">On Hand</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Allocated</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Available</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{name(r.itemId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.binId ? r.binId.slice(0, 8) : '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.lotId ? r.lotId.slice(0, 8) : '—'}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.onHandQuantity}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.allocatedQuantity}</td>
                      <td className="px-3 py-3 text-right"><Badge variant={r.availableQuantity < 0 ? 'error' : 'success'} size="sm" dot>{r.availableQuantity}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
