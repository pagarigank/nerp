import { useEffect, useState } from 'react'
import { Card } from '@components/ui/Card'
import { DataTable } from '@components/ui/DataTable'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getOrderStatusDashboard, companyId } from '@api/orderManagement'
import type { OrderStatusRow } from '@/types/orderManagement'

export function OrderStatusDashboardPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<OrderStatusRow[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    getOrderStatusDashboard(companyId())
      .then((res) => setData((res as { data: OrderStatusRow[] }).data ?? []))
      .catch((e) => setError(getErrorMessage(e)))
      .finally(() => setLoading(false))
  }, [])

  const columns = [
    { key: 'status', header: 'Status', render: (v: string) => <Badge variant={v === 'Shipped' ? 'success' : v === 'Cancelled' ? 'error' : 'info'}>{v}</Badge> },
    { key: 'orderCount', header: 'Orders' },
    { key: 'remainingToShip', header: 'Units Remaining to Ship' },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Sales Order Status Dashboard (587)</h1>
      <Card>
        <DataTable columns={columns} data={data} loading={loading} emptyMessage="No orders" />
      </Card>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
