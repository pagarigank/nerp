import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, CheckCircle, Pencil, Trash2 } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { getErrorMessage } from '@api/client'
import { deleteShipment, getShipment, getPackingSlip, confirmShipment } from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import type { ShipmentDetail, PackingSlip } from '@/types/orderManagement'
import type { ArCustomer } from '@/types/ar'

export function ShipmentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [shipment, setShipment] = useState<ShipmentDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [packingSlip, setPackingSlip] = useState<PackingSlip | null>(null)
  const [slipLoading, setSlipLoading] = useState(false)
  const [slipError, setSlipError] = useState<string | null>(null)
  const [customers, setCustomers] = useState<ArCustomer[]>([])
  const [confirming, setConfirming] = useState(false)
  const customerName = (id?: string | null) => id ? customers.find((c: ArCustomer) => c.id === id)?.name ?? id.slice(0, 8) : '—'

  useEffect(() => {
    if (!id) return
    void load()
  }, [id])

  async function load() {
    if (!id) return
    setLoading(true)
    setError(null)
    try {
      const [sh, custs] = await Promise.all([getShipment(id), getCustomers()])
      setShipment(sh)
      setCustomers(custs)
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  if (loading) return <p className="text-gray-500">Loading…</p>
  if (!shipment) return <p className="text-red-600">{error ?? 'Shipment not found.'}</p>

  async function handleConfirm() {
    if (!id) return
    setConfirming(true)
    try {
      await confirmShipment(id)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setConfirming(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={() => navigate('/om/shipments')}>
          <ArrowLeft className="h-4 w-4" /> Back
        </Button>
        <div className="flex items-center gap-2">
          {shipment.status === 'Draft' && (
            <>
              <Button variant="outline" onClick={() => navigate(`/om/shipments?edit=${shipment.id}`)}>
                <Pencil className="h-4 w-4" /> Edit
              </Button>
              <Button variant="outline" onClick={async () => { if (confirm(`Delete shipment ${shipment.shipmentNumber}?`)) { try { await deleteShipment(shipment.id); navigate('/om/shipments') } catch (e) { setError(getErrorMessage(e)) } } }}>
                <Trash2 className="h-4 w-4 text-red-500" /> Delete
              </Button>
              <Button variant="success" disabled={confirming} onClick={handleConfirm}>
                <CheckCircle className="h-4 w-4" /> Confirm Shipment
              </Button>
            </>
          )}
          <Button
          variant="outline"
          disabled={slipLoading}
          onClick={async () => {
            if (!id) return
            setSlipLoading(true)
            setSlipError(null)
            try {
              setPackingSlip(await getPackingSlip(id))
            } catch (e) {
              setSlipError(getErrorMessage(e))
            } finally {
              setSlipLoading(false)
            }
          }}
        >
          {slipLoading ? 'Loading…' : 'Packing Slip'}
        </Button>
        </div>
      </div>

      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">{shipment.shipmentNumber}</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Status: <span className="font-medium">{shipment.status}</span> · {shipment.shipmentDate}
          {shipment.carrier && <> · Carrier: {shipment.carrier}</>}
          {shipment.trackingNumber && <> · Tracking: {shipment.trackingNumber}</>}
        </p>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">
          {error}
        </div>
      )}

      <div className="grid grid-cols-2 gap-3 rounded-lg border border-gray-200 p-4 text-sm dark:border-gray-700 sm:grid-cols-4">
        <div><span className="text-gray-500">Customer:</span> <span className="font-medium">{customerName(shipment.customerId)}</span></div>
        <div><span className="text-gray-500">Freight:</span> <span className="font-medium">${shipment.freightCost.toFixed(2)}</span></div>
      </div>

      <div className="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">#</th>
              <th className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">Item</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Price</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {shipment.lines.map((l) => (
              <tr key={l.id}>
                <td className="px-4 py-2 text-sm">{l.lineNumber}</td>
                <td className="px-4 py-2 text-sm">{l.description}</td>
                <td className="px-4 py-2 text-right text-sm">{l.quantity}</td>
                <td className="px-4 py-2 text-right text-sm">${l.unitPrice.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {slipError && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{slipError}</div>
      )}
      {packingSlip && (
        <div className="rounded-lg border border-gray-200 p-4 dark:border-gray-700">
          <h3 className="mb-2 text-sm font-semibold text-gray-700 dark:text-gray-200">
            Packing Slip — {packingSlip.shipmentNumber}
            {packingSlip.carrier ? ` · ${packingSlip.carrier}` : ''}
            {packingSlip.trackingNumber ? ` · ${packingSlip.trackingNumber}` : ''}
          </h3>
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead>
              <tr className="text-left text-xs font-medium uppercase text-gray-500">
                <th className="px-2 py-1">Item</th>
                <th className="px-2 py-1">Description</th>
                <th className="px-2 py-1 text-right">Qty</th>
                <th className="px-2 py-1 text-right">UoM</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {packingSlip.lines.map((l, i) => (
                <tr key={i} className="text-sm">
                  <td className="px-2 py-1">{l.itemId}</td>
                  <td className="px-2 py-1">{l.description}</td>
                  <td className="px-2 py-1 text-right">{l.quantity}</td>
                  <td className="px-2 py-1 text-right">{l.unitOfMeasure}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
