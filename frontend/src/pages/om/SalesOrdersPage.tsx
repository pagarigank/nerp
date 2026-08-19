import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Eye, CheckCircle } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { DataTable } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import { cancelSalesOrder, confirmSalesOrder, getSalesOrders } from '@api/orderManagement'
import type { SalesOrderSummary } from '@/types/orderManagement'

export function SalesOrdersPage() {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<SalesOrderSummary[]>([])
  const [error, setError] = useState<string | null>(null)
  const [actionId, setActionId] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      setData(await getSalesOrders())
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function handleConfirm(id: string) {
    setActionId(id)
    try {
      await confirmSalesOrder(id)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setActionId(null)
    }
  }

  async function handleCancel(id: string) {
    setActionId(id)
    try {
      await cancelSalesOrder(id)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setActionId(null)
    }
  }

  const columns = [
    { key: 'orderNumber', header: 'Order #', sortable: true },
    { key: 'customerId', header: 'Customer' },
    { key: 'orderDate', header: 'Order Date', sortable: true },
    { key: 'status', header: 'Status' },
    {
      key: 'totalAmount',
      header: 'Total',
      align: 'right' as const,
      render: (row: SalesOrderSummary) => `$${row.totalAmount.toFixed(2)}`,
    },
    {
      key: 'actions',
      header: 'Actions',
      render: (row: SalesOrderSummary) => (
        <div className="flex gap-2">
          <Button size="sm" variant="ghost" onClick={() => navigate(`/om/sales-orders/${row.id}`)}>
            <Eye className="h-4 w-4" /> View
          </Button>
          {row.status === 'Draft' && (
            <Button
              size="sm"
              variant="success"
              disabled={actionId === row.id}
              onClick={() => handleConfirm(row.id)}
            >
              <CheckCircle className="h-4 w-4" /> Confirm
            </Button>
          )}
          {row.status === 'Draft' && (
            <Button
              size="sm"
              variant="outline"
              disabled={actionId === row.id}
              onClick={() => handleCancel(row.id)}
            >
              Cancel
            </Button>
          )}
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Sales Orders</h2>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
            Create and confirm customer sales orders
          </p>
        </div>
        <Button className="gap-2" onClick={() => navigate('/om/sales-orders/new')}>
          <Plus className="h-4 w-4" /> New Order
        </Button>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">
          {error}
        </div>
      )}

      <DataTable
        columns={columns}
        data={data}
        loading={loading}
        emptyMessage="No sales orders found"
      />
    </div>
  )
}
