import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Input, Select } from '@components/ui/Input'
import { getItemStock, getWarehouses } from '@api/inventory'

function money(n: number) { return n.toFixed(2) }

export function StockPage() {
  const [search, setSearch] = useState('')
  const [warehouseId, setWarehouseId] = useState<string>('')

  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })
  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['inventory', 'item-stock', warehouseId],
    queryFn: () => getItemStock(warehouseId || undefined),
  })

  const warehouseOptions = useMemo(() => [{ value: '', label: 'All warehouses' }, ...warehouses.map(w => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` }))], [warehouses])

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter(r => r.itemCode.toLowerCase().includes(q) || r.itemDescription.toLowerCase().includes(q))
  }, [rows, search])

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Item Stock" description={`${rows.length} stock record(s)`} />
        <CardContent>
          <div className="mb-4 flex gap-3 max-w-2xl">
            <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by item code or description..." aria-label="Search stock" leftIcon={<Search className="h-4 w-4" />} />
            <Select value={warehouseId} onChange={e => setWarehouseId(e.target.value)} options={warehouseOptions} aria-label="Filter warehouse" />
          </div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No stock records.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Item</th><th className="px-3 py-2 font-medium text-gray-500">Description</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th><th className="px-3 py-2 font-medium text-gray-500 text-right">On Hand</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Allocated</th><th className="px-3 py-2 font-medium text-gray-500 text-right">On Order</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Available</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.itemCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.itemDescription}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseCode}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{money(r.quantityOnHand)}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.quantityAllocated)}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.quantityOnOrder)}</td>
                      <td className="px-3 py-3 text-right font-medium text-gray-900 dark:text-white">{money(r.quantityAvailable)}</td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
