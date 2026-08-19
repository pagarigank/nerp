import { useEffect, useState, useMemo } from 'react'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { DataTable } from '@components/ui/DataTable'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getSalesAnalysisReport } from '@api/orderManagement'
import type { SalesAnalysisRow } from '@/types/orderManagement'

type GroupBy = 'item' | 'customer'

export function SalesAnalysisPage() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [groupBy, setGroupBy] = useState<GroupBy>('item')
  const [rows, setRows] = useState<SalesAnalysisRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      setRows(await getSalesAnalysisReport(from || undefined, to || undefined))
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const grouped = useMemo(() => {
    const map = new Map<string, { key: string; quantity: number; shipped: number; net: number; tax: number }>()
    for (const r of rows) {
      const key = groupBy === 'item' ? r.itemId : (r.customerId ?? 'Unassigned')
      const cur = map.get(key) ?? { key, quantity: 0, shipped: 0, net: 0, tax: 0 }
      cur.quantity += r.quantity
      cur.shipped += r.shippedQuantity
      cur.net += r.netSales
      cur.tax += r.taxAmount
      map.set(key, cur)
    }
    return Array.from(map.values()).sort((a, b) => b.net - a.net)
  }, [rows, groupBy])

  const columns = [
    { key: 'key', header: groupBy === 'item' ? 'Item' : 'Customer', render: (v: string) => v.slice(0, 8) },
    { key: 'quantity', header: 'Ordered Qty' },
    { key: 'shipped', header: 'Shipped Qty' },
    { key: 'net', header: 'Net Sales' },
    { key: 'tax', header: 'Tax' },
    { key: 'fill', header: 'Fill %', render: (_: unknown, row: { shipped: number; quantity: number }) => (
      <Badge variant={row.quantity > 0 && row.shipped >= row.quantity ? 'success' : 'warning'}>
        {row.quantity > 0 ? Math.round((row.shipped / row.quantity) * 100) : 0}%
      </Badge>
    ) },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Sales Analysis Drill-down (590)</h1>
      <Card className="p-4 flex flex-wrap items-end gap-3">
        <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} placeholder="From" className="w-40" />
        <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} placeholder="To" className="w-40" />
        <select
          className="block rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm"
          value={groupBy}
          onChange={(e) => setGroupBy(e.target.value as GroupBy)}
        >
          <option value="item">Group by Item</option>
          <option value="customer">Group by Customer</option>
        </select>
        <Button variant="primary" onClick={load}>Run</Button>
      </Card>
      <Card>
        <DataTable columns={columns} data={grouped} loading={loading} emptyMessage="No analysis data" />
      </Card>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
