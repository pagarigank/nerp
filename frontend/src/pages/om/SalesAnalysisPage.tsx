import { useEffect, useMemo, useState } from 'react'
import { Button } from '@components/ui/Button'
import { Card, CardContent, CardHeader } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { LoadingSpinner } from '@components/ui/LoadingSpinner'
import { DataTable } from '@components/ui/DataTable'
import { Badge } from '@components/ui/Badge'
import { X } from 'lucide-react'
import { getErrorMessage } from '@api/client'
import {
  getSalesAnalysisReport,
  getSalesOrder,
  getSalesOrders,
} from '@api/orderManagement'
import type {
  SalesAnalysisRow,
  SalesOrderDetail,
  SalesOrderSummary,
} from '@/types/orderManagement'

type GroupBy = 'item' | 'customer'

interface DrillRow {
  orderId: string
  orderNumber: string
  customerId?: string | null
  orderDate: string
  status: string
  totalAmount: number
  matchedQty: number
  matchedValue: number
}

interface DrillState {
  key: string
  loading: boolean
  rows: DrillRow[]
}

// Item-level drill-down inspects order lines one order at a time, so bound the sweep.
const MAX_DETAIL_FETCHES = 30

function rangeBounds(from: string, to: string) {
  return {
    fromTs: from ? new Date(`${from}T00:00:00`).getTime() : null,
    toTs: to ? new Date(`${to}T23:59:59.999`).getTime() : null,
  }
}

export function SalesAnalysisPage() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [groupBy, setGroupBy] = useState<GroupBy>('item')
  const [rows, setRows] = useState<SalesAnalysisRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [cachedOrders, setCachedOrders] = useState<SalesOrderSummary[] | null>(null)
  const [drill, setDrill] = useState<DrillState | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    setCachedOrders(null)
    setDrill(null)
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
    const map = new Map<string, { key: string; customerId: string | null; quantity: number; shipped: number; net: number; tax: number }>()
    for (const r of rows) {
      const key = groupBy === 'item' ? r.itemId : (r.customerId ?? 'Unassigned')
      const cur = map.get(key) ?? { key, customerId: r.customerId, quantity: 0, shipped: 0, net: 0, tax: 0 }
      cur.quantity += r.quantity
      cur.shipped += r.shippedQuantity
      cur.net += r.netSales
      cur.tax += r.taxAmount
      map.set(key, cur)
    }
    return Array.from(map.values()).sort((a, b) => b.net - a.net)
  }, [rows, groupBy])

  async function openDrill(row: { key: string; customerId: string | null }) {
    setDrill({ key: row.key, loading: true, rows: [] })
    setError(null)
    try {
      let orders = cachedOrders
      if (orders === null) {
        orders = await getSalesOrders()
        setCachedOrders(orders)
      }

      const { fromTs, toTs } = rangeBounds(from, to)
      const inRange = orders.filter((o) => {
        const t = new Date(o.orderDate).getTime()
        return (fromTs === null || t >= fromTs) && (toTs === null || t <= toTs)
      })

      let drillRows: DrillRow[]
      if (groupBy === 'customer') {
        drillRows = inRange
          .filter((o) => o.customerId === row.key)
          .map((o) => ({
            orderId: o.id,
            orderNumber: o.orderNumber,
            customerId: o.customerId,
            orderDate: o.orderDate,
            status: o.status,
            totalAmount: o.totalAmount,
            matchedQty: 0,
            matchedValue: 0,
          }))
      } else {
        const candidates = inRange.slice(0, MAX_DETAIL_FETCHES)
        const details = await Promise.all(candidates.map((o) => getSalesOrder(o.id)))
        drillRows = candidates
          .map((o: SalesOrderSummary, i: number) => ({ order: o, detail: details[i] as SalesOrderDetail }))
          .filter((x) => x.detail.lines.some((l) => l.itemId === row.key))
          .map((x) => ({
            orderId: x.order.id,
            orderNumber: x.order.orderNumber,
            customerId: x.order.customerId,
            orderDate: x.order.orderDate,
            status: x.order.status,
            totalAmount: x.order.totalAmount,
            matchedQty: x.detail.lines.filter((l) => l.itemId === row.key).reduce((s, l) => s + l.quantity, 0),
            matchedValue: x.detail.lines.filter((l) => l.itemId === row.key).reduce((s, l) => s + l.lineTotal, 0),
          }))
      }

      setDrill({ key: row.key, loading: false, rows: drillRows })
    } catch (e) {
      setError(getErrorMessage(e))
      setDrill(null)
    }
  }

  const columns = [
    {
      key: 'key',
      header: groupBy === 'item' ? 'Item' : 'Customer',
      render: (row: { key: string; customerId: string | null }) => (
        <button
          type="button"
          className="text-indigo-600 hover:underline dark:text-indigo-400"
          title="Show contributing sales orders"
          onClick={() => void openDrill(row)}
        >
          {row.key.slice(0, 8)}
        </button>
      ),
    },
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

      {drill && (
        <Card>
          <CardHeader
            title={`Contributing sales orders — ${drill.key.slice(0, 8)}`}
            description={
              groupBy === 'customer'
                ? `${drill.rows.length} order(s) for this customer${from || to ? ' within the report date range' : ''}`
                : `${drill.rows.length} order(s) containing this item among the most recent ${MAX_DETAIL_FETCHES} in range`
            }
            action={
              <Button variant="ghost" size="sm" onClick={() => setDrill(null)}>
                <X className="h-4 w-4" />
              </Button>
            }
          />
          <CardContent>
            {drill.loading ? (
              <div className="flex justify-center py-6"><LoadingSpinner /></div>
            ) : drill.rows.length === 0 ? (
              <p className="py-6 text-center text-sm text-gray-500">No contributing sales orders found.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                  <thead>
                    <tr className="text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                      <th className="px-3 py-2">Order #</th>
                      <th className="px-3 py-2">Customer</th>
                      <th className="px-3 py-2">Date</th>
                      <th className="px-3 py-2">Status</th>
                      <th className="px-3 py-2 text-right">Order Total</th>
                      {groupBy === 'item' && <th className="px-3 py-2 text-right">Matched Qty</th>}
                      {groupBy === 'item' && <th className="px-3 py-2 text-right">Matched Value</th>}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {drill.rows.map((r) => (
                      <tr key={r.orderId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-2 font-medium text-gray-900 dark:text-white">{r.orderNumber}</td>
                        <td className="px-3 py-2 text-gray-600 dark:text-gray-300">{r.customerId?.slice(0, 8) ?? '—'}</td>
                        <td className="px-3 py-2 text-gray-600 dark:text-gray-300">{new Date(r.orderDate).toLocaleDateString()}</td>
                        <td className="px-3 py-2"><Badge size="sm">{r.status}</Badge></td>
                        <td className="px-3 py-2 text-right tabular-nums">${r.totalAmount.toFixed(2)}</td>
                        {groupBy === 'item' && <td className="px-3 py-2 text-right tabular-nums">{r.matchedQty}</td>}
                        {groupBy === 'item' && <td className="px-3 py-2 text-right tabular-nums">${r.matchedValue.toFixed(2)}</td>}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
