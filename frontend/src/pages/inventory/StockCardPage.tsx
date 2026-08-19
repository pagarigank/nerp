import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Package } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { getItems, getStockCard } from '@api/inventory'
import type { ItemSummary, StockCardRow } from '@/types/inventory'

const money = (n: number) => `$${Number(n).toFixed(2)}`
const qty = (n: number) => Number(n).toFixed(2)

export function StockCardPage() {
  const [itemId, setItemId] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [data, setData] = useState<StockCardRow[]>([])

  const { data: items = [] } = useQuery({
    queryKey: ['inventory', 'items'],
    queryFn: () => getItems(),
  })

  const itemOptions = useMemo(
    () => items.map((i: ItemSummary) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })),
    [items]
  )

  const selectedItem = useMemo(
    () => items.find((i: ItemSummary) => i.id === itemId),
    [items, itemId]
  )

  useEffect(() => {
    if (!itemId) { setData([]); return }
    setLoading(true)
    setError(null)
    void getStockCard(itemId, undefined, from || undefined, to || undefined)
      .then(setData)
      .catch((e) => setError(getErrorMessage(e)))
      .finally(() => setLoading(false))
  }, [itemId, from, to])

  const summary = useMemo(() => {
    if (data.length === 0) return null
    const totalIn = data.reduce((s, r) => s + r.quantityIn, 0)
    const totalOut = data.reduce((s, r) => s + r.quantityOut, 0)
    const lastRow = data[data.length - 1]
    return { totalIn, totalOut, balance: lastRow.runningBalance, value: lastRow.runningValue }
  }, [data])

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
          <Package className="h-6 w-6" /> Stock Card
        </h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Item-centric transaction history with running balance
        </p>
      </div>

      <Card>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Select
              label="Item"
              placeholder="Select item..."
              options={itemOptions}
              value={itemId}
              onChange={(e) => setItemId(e.target.value)}
              required
            />
            <Input label="From" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
            <Input label="To" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>

          {selectedItem && (
            <div className="flex gap-6 text-sm text-gray-600 dark:text-gray-400">
              <span><strong>Code:</strong> {selectedItem.itemCode}</span>
              <span><strong>Type:</strong> {selectedItem.itemType}</span>
              <span><strong>UOM:</strong> {selectedItem.baseUnitOfMeasure}</span>
              <span><strong>Std Cost:</strong> {money(selectedItem.standardCost ?? 0)}</span>
            </div>
          )}
        </CardContent>
      </Card>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{error}</div>
      )}

      {summary && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <Card className="p-4">
            <p className="text-xs text-gray-500 uppercase">Total Received</p>
            <p className="text-lg font-bold text-green-600">{qty(summary.totalIn)}</p>
          </Card>
          <Card className="p-4">
            <p className="text-xs text-gray-500 uppercase">Total Issued</p>
            <p className="text-lg font-bold text-red-600">{qty(summary.totalOut)}</p>
          </Card>
          <Card className="p-4">
            <p className="text-xs text-gray-500 uppercase">Running Balance</p>
            <p className="text-lg font-bold text-gray-900 dark:text-white">{qty(summary.balance)}</p>
          </Card>
          <Card className="p-4">
            <p className="text-xs text-gray-500 uppercase">Running Value</p>
            <p className="text-lg font-bold text-gray-900 dark:text-white">{money(summary.value)}</p>
          </Card>
        </div>
      )}

      <Card>
        <CardHeader title="Transactions" description={`${data.length} movement(s)`} />
        <CardContent>
          {loading ? (
            <p className="text-sm text-gray-500 py-8 text-center">Loading...</p>
          ) : data.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">
              {itemId ? 'No transactions found for this item.' : 'Select an item to view its stock card.'}
            </p>
          ) : (
            <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
              <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead className="bg-gray-50 dark:bg-gray-800">
                  <tr>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Date</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Type</th>
                    <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Reference</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty In</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty Out</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Unit Cost</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Amount</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Balance</th>
                    <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Value</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                  {data.map((row) => (
                    <tr key={row.transactionId} className="bg-white dark:bg-gray-900 hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-2 text-sm whitespace-nowrap">{new Date(row.transactionDate).toLocaleDateString()}</td>
                      <td className="px-3 py-2 text-sm">
                        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                          row.transactionType === 'Receipt' ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                          : row.transactionType === 'Issue' ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
                          : 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-400'
                        }`}>
                          {row.transactionType}
                        </span>
                      </td>
                      <td className="px-3 py-2 text-sm text-gray-500 font-mono">{row.referenceNumber ?? '—'}</td>
                      <td className="px-3 py-2 text-sm text-right tabular-nums text-green-600">{row.quantityIn > 0 ? qty(row.quantityIn) : ''}</td>
                      <td className="px-3 py-2 text-sm text-right tabular-nums text-red-600">{row.quantityOut > 0 ? qty(row.quantityOut) : ''}</td>
                      <td className="px-3 py-2 text-sm text-right tabular-nums">{money(row.unitCost)}</td>
                      <td className="px-3 py-2 text-sm text-right tabular-nums">{money(row.extendedCost)}</td>
                      <td className="px-3 py-2 text-sm text-right tabular-nums font-medium">{qty(row.runningBalance)}</td>
                      <td className="px-3 py-2 text-sm text-right tabular-nums font-medium">{money(row.runningValue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
