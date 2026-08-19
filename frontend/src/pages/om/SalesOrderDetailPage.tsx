import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { CheckCircle, ArrowLeft, Lock, Unlock } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { getErrorMessage } from '@api/client'
import { confirmSalesOrder, getSalesOrder, placeCreditHold, releaseCreditHold, getPickList, approveDiscount } from '@api/orderManagement'
import type { SalesOrderDetail, PickList } from '@/types/orderManagement'

export function SalesOrderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [order, setOrder] = useState<SalesOrderDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actioning, setActioning] = useState(false)
  const [pickList, setPickList] = useState<PickList | null>(null)
  const [pickLoading, setPickLoading] = useState(false)
  const [pickError, setPickError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    void load()
  }, [id])

  async function load() {
    if (!id) return
    setLoading(true)
    setError(null)
    try {
      setOrder(await getSalesOrder(id))
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  async function handleConfirm() {
    if (!id) return
    setActioning(true)
    try {
      await confirmSalesOrder(id)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setActioning(false)
    }
  }

  async function handleHold() {
    if (!id) return
    setActioning(true)
    try {
      await placeCreditHold(id, 'Manual credit hold')
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setActioning(false)
    }
  }

  async function handleRelease() {
    if (!id) return
    setActioning(true)
    try {
      await releaseCreditHold(id)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setActioning(false)
    }
  }

  async function handleApproveDiscount() {
    if (!id) return
    setActioning(true)
    try {
      await approveDiscount(id, 'Manager')
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setActioning(false)
    }
  }

  if (loading) return <p className="text-gray-500">Loading…</p>
  if (!order) return <p className="text-red-600">{error ?? 'Order not found.'}</p>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={() => navigate('/om/sales-orders')}>
          <ArrowLeft className="h-4 w-4" /> Back
        </Button>
        <div className="flex items-center gap-2">
          {order.isOnCreditHold ? (
            <Button variant="outline" disabled={actioning} onClick={handleRelease}>
              <Unlock className="h-4 w-4" /> Release Hold
            </Button>
          ) : (
            <Button variant="outline" disabled={actioning} onClick={handleHold}>
              <Lock className="h-4 w-4" /> Place Credit Hold
            </Button>
          )}
          {order.status === 'Draft' && (
            <Button variant="success" disabled={actioning} onClick={handleConfirm}>
              <CheckCircle className="h-4 w-4" /> Confirm Order
            </Button>
          )}
          {order.requiresDiscountApproval && !order.discountApproved && order.status === 'Draft' && (
            <Button variant="outline" disabled={actioning} onClick={handleApproveDiscount}>
              <CheckCircle className="h-4 w-4" /> Approve Discount
            </Button>
          )}
          {order.discountApproved && (
            <span className="inline-flex items-center gap-1 rounded-md bg-green-100 px-2 py-1 text-xs font-medium text-green-700 dark:bg-green-900/30 dark:text-green-300">
              <CheckCircle className="h-3 w-3" /> Discount Approved
            </span>
          )}
          <Button
            variant="outline"
            disabled={pickLoading}
            onClick={async () => {
              if (!id) return
              setPickLoading(true)
              setPickError(null)
              try {
                setPickList(await getPickList(id))
              } catch (e) {
                setPickError(getErrorMessage(e))
              } finally {
                setPickLoading(false)
              }
            }}
          >
            {pickLoading ? 'Loading…' : 'Pick List'}
          </Button>
        </div>
      </div>

      {pickError && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{pickError}</div>
      )}
      {pickList && (
        <div className="rounded-lg border border-gray-200 p-4 dark:border-gray-700">
          <h3 className="mb-2 text-sm font-semibold text-gray-700 dark:text-gray-200">
            Pick List — {pickList.orderNumber} ({pickList.status})
          </h3>
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead>
              <tr className="text-left text-xs font-medium uppercase text-gray-500">
                <th className="px-2 py-1">Item</th>
                <th className="px-2 py-1">Description</th>
                <th className="px-2 py-1 text-right">Qty</th>
                <th className="px-2 py-1 text-right">UoM</th>
                <th className="px-2 py-1">Warehouse</th>
                <th className="px-2 py-1 text-right">Remaining</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {pickList.lines.map((l, i) => (
                <tr key={i} className="text-sm">
                  <td className="px-2 py-1">{l.itemId}</td>
                  <td className="px-2 py-1">{l.description}</td>
                  <td className="px-2 py-1 text-right">{l.quantity}</td>
                  <td className="px-2 py-1 text-right">{l.unitOfMeasure}</td>
                  <td className="px-2 py-1">{l.warehouseId ?? '—'}</td>
                  <td className="px-2 py-1 text-right">{l.remainingToPick}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">{order.orderNumber}</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Status: <span className="font-medium">{order.status}</span> · {order.orderDate}
          {order.isOnCreditHold && (
            <span className="ml-2 rounded bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800 dark:bg-amber-900/30 dark:text-amber-300">
              ON CREDIT HOLD
            </span>
          )}
        </p>
      </div>

      <div className="grid grid-cols-2 gap-3 rounded-lg border border-gray-200 p-4 text-sm dark:border-gray-700 sm:grid-cols-4">
        <div>
          <span className="text-gray-500">Order Type:</span>{' '}
          <span className="font-medium">{order.salesOrderTypeId ?? '—'}</span>
        </div>
        <div>
          <span className="text-gray-500">Tax Code:</span>{' '}
          <span className="font-medium">{order.taxCodeId ?? '—'}</span>
        </div>
        <div>
          <span className="text-gray-500">Tax Exemption:</span>{' '}
          <span className="font-medium">{order.taxExemptionCertificateId ?? '—'}</span>
        </div>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">
          {error}
        </div>
      )}

      <div className="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">#</th>
              <th className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">Item</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Price</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Line Total</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Shipped</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Backorder</th>
              <th className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">Drop-ship</th>
              </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {order.lines.map((l) => (
              <tr key={l.id}>
              <td className="px-4 py-2 text-sm">{l.lineNumber}</td>
              <td className="px-4 py-2 text-sm">{l.description}</td>
              <td className="px-4 py-2 text-right text-sm">{l.quantity}</td>
              <td className="px-4 py-2 text-right text-sm">${l.unitPrice.toFixed(2)}</td>
              <td className="px-4 py-2 text-right text-sm">${l.lineTotal.toFixed(2)}</td>
              <td className="px-4 py-2 text-right text-sm">{l.shippedQuantity}</td>
              <td className="px-4 py-2 text-right text-sm">{Math.max(0, l.quantity - l.shippedQuantity)}</td>
              <td className="px-4 py-2 text-sm">{l.isDropShip ? 'Yes' : 'No'}</td>
              </tr>
              ))}
              </tbody>
        </table>
      </div>
    </div>
  )
}
