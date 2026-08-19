import { useEffect, useState } from 'react'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Badge } from '@components/ui/Badge'
import { DataTable } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import { getSalesOrders, getPickList, getPackingSlip, createShipment, confirmShipment, companyId } from '@api/orderManagement'
import type { SalesOrderSummary, PickList, PackingSlip } from '@/types/orderManagement'

type Stage = 'pick' | 'pack' | 'ship'

export function PickPackShipPage() {
  const [orders, setOrders] = useState<SalesOrderSummary[]>([])
  const [soId, setSoId] = useState('')
  const [stage, setStage] = useState<Stage>('pick')
  const [pickList, setPickList] = useState<PickList | null>(null)
  const [packing, setPacking] = useState<PackingSlip | null>(null)
  const [shipmentId, setShipmentId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    getSalesOrders()
      .then((d) => setOrders(d as SalesOrderSummary[]))
      .catch((e) => setError(getErrorMessage(e)))
  }, [])

  async function loadPick() {
    if (!soId) return
    setLoading(true)
    setError(null)
    setMsg(null)
    try {
      const res = await getPickList(soId)
      setPickList(res as PickList)
      setStage('pick')
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  async function doPack() {
    if (!soId) return
    setLoading(true)
    setError(null)
    try {
      const shipmentLines = (pickList?.lines ?? []).map((pl, i) => ({
        lineNumber: i + 1,
        itemId: pl.itemId,
        description: pl.description,
        quantity: pl.remainingToPick > 0 ? pl.remainingToPick : pl.quantity,
        unitPrice: 0,
        unitOfMeasure: pl.unitOfMeasure,
        warehouseId: pl.warehouseId ?? null,
        salesOrderLineId: null,
        projectId: null,
        accountId: null,
        discountPercent: 0,
        taxPercent: 0,
      }))
      const id = await createShipment({
        shipmentNumber: `SHP${Date.now().toString().slice(-6)}`,
        companyId: companyId(),
        customerId: pickList?.customerId ?? orders.find((o) => o.id === soId)?.customerId ?? '',
        salesOrderId: soId,
        shipmentDate: new Date().toISOString().slice(0, 10),
        freightCost: 0,
        lines: shipmentLines,
      })
      setShipmentId((id as unknown as string) ?? '')
      const res = await getPackingSlip((id as unknown as string) ?? '')
      setPacking(res as PackingSlip)
      setStage('pack')
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  async function doShip() {
    if (!shipmentId) return
    setLoading(true)
    setError(null)
    try {
      await confirmShipment(shipmentId)
      const list = await getPackingSlip(shipmentId)
      setPacking(list as PackingSlip)
      setStage('ship')
      setMsg('Shipment confirmed.')
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  const pickCols = [
    { key: 'lineNumber', header: '#' },
    { key: 'itemId', header: 'Item', render: (v: string) => v.slice(0, 8) },
    { key: 'description', header: 'Desc' },
    { key: 'quantity', header: 'Qty' },
    { key: 'unitOfMeasure', header: 'UoM' },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Pick / Pack / Ship Workspace (644)</h1>
      <Card className="p-4 flex items-end gap-3">
        <div className="flex-1">
          <label className="block text-sm font-medium mb-1.5">Sales Order</label>
          <select
            className="block w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 text-sm"
            value={soId}
            onChange={(e) => setSoId(e.target.value)}
          >
            <option value="">Select order…</option>
            {orders.map((o) => (
              <option key={o.id} value={o.id}>{o.orderNumber} ({o.status})</option>
            ))}
          </select>
        </div>
        <Button variant="primary" disabled={!soId || loading} onClick={loadPick}>Load Pick List</Button>
      </Card>

      <div className="flex gap-2">
        {(['pick', 'pack', 'ship'] as Stage[]).map((s) => (
          <Badge key={s} variant={stage === s ? 'info' : 'neutral'}>{s.toUpperCase()}</Badge>
        ))}
      </div>

      {pickList && stage === 'pick' && (
        <Card className="p-4 space-y-3">
          <h2 className="font-semibold">Pick List — {pickList.orderNumber}</h2>
          <DataTable columns={pickCols} data={pickList.lines} loading={false} emptyMessage="No lines" />
          <Button variant="primary" onClick={doPack}>Generate Packing Slip (Pack)</Button>
        </Card>
      )}

      {packing && (stage === 'pack' || stage === 'ship') && (
        <Card className="p-4 space-y-3">
          <h2 className="font-semibold">Packing Slip — {packing.shipmentNumber}</h2>
          <DataTable
            columns={[
              { key: 'itemId', header: 'Item', render: (v: string) => v.slice(0, 8) },
              { key: 'description', header: 'Desc' },
              { key: 'quantity', header: 'Qty' },
              { key: 'unitOfMeasure', header: 'UoM' },
            ]}
            data={packing.lines}
            loading={false}
            emptyMessage="No lines"
          />
          {stage === 'pack' && <Button variant="success" onClick={doShip}>Confirm Shipment</Button>}
        </Card>
      )}

      {msg && <p className="text-sm text-green-600">{msg}</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
