import { useEffect, useState } from 'react'
import { Card } from '@components/ui/Card'
import { DataTable } from '@components/ui/DataTable'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { formatDate } from '@utils/helpers'
import { getOrderStatusDashboard, getCreditHoldsReport, companyId } from '@api/orderManagement'
import type { OrderStatusRow, CreditHoldRow } from '@/types/orderManagement'

export function OrderStatusDashboardPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<OrderStatusRow[]>([])
  const [holds, setHolds] = useState<CreditHoldRow[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([
      getOrderStatusDashboard(companyId()),
      getCreditHoldsReport().catch(() => []),
    ])
      .then(([dash, holdRes]) => {
        setData((dash as { data: OrderStatusRow[] }).data ?? [])
        setHolds((holdRes as { data?: CreditHoldRow[] }).data ?? (holdRes as CreditHoldRow[]))
      })
      .catch((e) => setError(getErrorMessage(e)))
      .finally(() => setLoading(false))
  }, [])

  const columns = [
    { key: 'status', header: 'Status', render: (row: any) => <Badge variant={row.status === 'Shipped' ? 'success' : row.status === 'Cancelled' ? 'error' : 'info'}>{row.status}</Badge> },
    { key: 'orderCount', header: 'Orders' },
    { key: 'remainingToShip', header: 'Units Remaining to Ship' },
  ]

  const holdColumns = [
    { key: 'orderNumber', header: 'Order #' },
    { key: 'reason', header: 'Hold Reason' },
    { key: 'orderDate', header: 'Order Date', render: (row: any) => formatDate(row.orderDate) },
    { key: 'status', header: 'Status' },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Sales Order Status Dashboard</h1>
      <Card>
        <DataTable columns={columns} data={data} loading={loading} emptyMessage="No orders" />
      </Card>
      <Card>
        <h2 className="text-lg font-semibold px-4 pt-4">Orders on Credit Hold</h2>
        <DataTable
          columns={holdColumns}
          data={holds}
          loading={loading}
          emptyMessage="No orders on credit hold"
        />
      </Card>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
