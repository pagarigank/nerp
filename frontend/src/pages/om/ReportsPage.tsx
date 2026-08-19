// Sales Reports (Phase 8) — Open Orders, Backorders, Shipment Register, Sales
// Analysis, Credit Holds, Drop-Ship Status, Sales Tax. Reads from the OM reports API.

import { useEffect, useState } from 'react'
import {
  BarChart3,
  FileText,
  PackageX,
  Truck,
  TrendingUp,
  AlertTriangle,
  Send,
  Percent,
  LineChart,
  History,
  Route,
} from 'lucide-react'
import {
  getOpenOrdersReport,
  getBackordersReport,
  getShipmentRegisterReport,
  getSalesAnalysisReport,
  getCreditHoldsReport,
  getDropShipStatusReport,
  getSalesTaxReport,
  getSalesTrendReport,
  getShippingLog,
  getFreightAnalysis,
} from '@/api/orderManagement'
import type {
  OpenOrderRow,
  BackorderRow,
  ShipmentRegisterRow,
  SalesAnalysisRow,
  CreditHoldRow,
  DropShipStatusRow,
  SalesTaxRow,
  SalesTrendRow,
  ShippingLogRow,
  FreightAnalysisRow,
} from '@/types/orderManagement'

type ReportKey =
  | 'open-orders'
  | 'backorders'
  | 'shipment-register'
  | 'sales-analysis'
  | 'credit-holds'
  | 'drop-ship-status'
  | 'sales-tax'
  | 'sales-trend'
  | 'shipping-log'
  | 'freight-analysis'

const money = (n: number) => `$${n.toFixed(2)}`

export function ReportsPage() {
  const [active, setActive] = useState<ReportKey>('open-orders')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [openOrders, setOpenOrders] = useState<OpenOrderRow[]>([])
  const [backorders, setBackorders] = useState<BackorderRow[]>([])
  const [shipmentReg, setShipmentReg] = useState<ShipmentRegisterRow[]>([])
  const [analysis, setAnalysis] = useState<SalesAnalysisRow[]>([])
  const [holds, setHolds] = useState<CreditHoldRow[]>([])
  const [dropShips, setDropShips] = useState<DropShipStatusRow[]>([])
  const [tax, setTax] = useState<SalesTaxRow[]>([])
  const [trend, setTrend] = useState<SalesTrendRow[]>([])
  const [shippingLog, setShippingLog] = useState<ShippingLogRow[]>([])
  const [freight, setFreight] = useState<FreightAnalysisRow[]>([])

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        switch (active) {
          case 'open-orders':
            setOpenOrders(await getOpenOrdersReport())
            break
          case 'backorders':
            setBackorders(await getBackordersReport())
            break
          case 'shipment-register':
            setShipmentReg(await getShipmentRegisterReport())
            break
          case 'sales-analysis':
            setAnalysis(await getSalesAnalysisReport())
            break
          case 'credit-holds':
            setHolds(await getCreditHoldsReport())
            break
          case 'drop-ship-status':
            setDropShips(await getDropShipStatusReport())
            break
          case 'sales-tax':
            setTax(await getSalesTaxReport())
            break
          case 'sales-trend':
            setTrend(await getSalesTrendReport())
            break
          case 'shipping-log':
            setShippingLog(await getShippingLog())
            break
          case 'freight-analysis':
            setFreight(await getFreightAnalysis())
            break
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load report')
      } finally {
        setLoading(false)
      }
    }
    void load()
  }, [active])

  const tabs: { key: ReportKey; label: string; icon: typeof FileText }[] = [
    { key: 'open-orders', label: 'Open Orders', icon: FileText },
    { key: 'backorders', label: 'Backorders', icon: PackageX },
    { key: 'shipment-register', label: 'Shipment Register', icon: Truck },
    { key: 'sales-analysis', label: 'Sales Analysis', icon: TrendingUp },
    { key: 'credit-holds', label: 'Credit Holds', icon: AlertTriangle },
    { key: 'drop-ship-status', label: 'Drop-Ship Status', icon: Send },
    { key: 'sales-tax', label: 'Sales Tax', icon: Percent },
    { key: 'sales-trend', label: 'Sales Trend', icon: LineChart },
    { key: 'shipping-log', label: 'Shipping Log', icon: History },
    { key: 'freight-analysis', label: 'Freight Analysis', icon: Route },
  ]

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center gap-2">
        <BarChart3 className="h-6 w-6 text-blue-600" />
        <h1 className="text-2xl font-semibold">Sales Reports</h1>
      </div>

      <div className="mb-4 flex flex-wrap gap-2">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setActive(t.key)}
            className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium ${
              active === t.key
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-200'
            }`}
          >
            <t.icon className="h-4 w-4" />
            {t.label}
          </button>
        ))}
      </div>

      {loading && <p className="text-sm text-gray-500">Loading…</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}

      {!loading && !error && active === 'open-orders' && (
        <ReportTable
          headers={['Order', 'Customer', 'Date', 'Status', 'Ordered', 'Backorder', 'Value', 'Hold']}
          rows={openOrders.map((r) => [
            r.orderNumber,
            r.customerId ?? '—',
            new Date(r.orderDate).toLocaleDateString(),
            r.status,
            String(r.orderedQty),
            String(r.backorderedQty),
            money(r.orderValue),
            r.isOnCreditHold ? 'YES' : '—',
          ])}
        />
      )}

      {!loading && !error && active === 'backorders' && (
        <ReportTable
          headers={['Order', 'Customer', 'Item', 'Ordered', 'Shipped', 'Backorder', 'Unit Price']}
          rows={backorders.map((r) => [
            r.orderNumber,
            r.customerId ?? '—',
            r.itemId,
            String(r.orderedQty),
            String(r.shippedQty),
            String(r.backorderedQty),
            money(r.unitPrice),
          ])}
        />
      )}

      {!loading && !error && active === 'shipment-register' && (
        <ReportTable
          headers={['Shipment', 'Order', 'Customer', 'Date', 'Status', 'Freight', 'Value']}
          rows={shipmentReg.map((r) => [
            r.shipmentNumber,
            r.salesOrderId ?? '—',
            r.customerId ?? '—',
            new Date(r.shipDate).toLocaleDateString(),
            r.status,
            money(r.freightCost),
            money(r.shipmentValue),
          ])}
        />
      )}

      {!loading && !error && active === 'sales-analysis' && (
        <ReportTable
          headers={['Item', 'Customer', 'Qty', 'Shipped', 'Net Sales', 'Tax']}
          rows={analysis.map((r) => [
            r.itemId,
            r.customerId ?? '—',
            String(r.quantity),
            String(r.shippedQuantity),
            money(r.netSales),
            money(r.taxAmount),
          ])}
        />
      )}

      {!loading && !error && active === 'credit-holds' && (
        <ReportTable
          headers={['Order', 'Customer', 'Date', 'Status', 'Reason']}
          rows={holds.map((r) => [
            r.orderNumber,
            r.customerId ?? '—',
            new Date(r.orderDate).toLocaleDateString(),
            r.status,
            r.reason,
          ])}
        />
      )}

      {!loading && !error && active === 'drop-ship-status' && (
        <ReportTable
          headers={['Order', 'Customer', 'Item', 'Vendor', 'Ordered', 'Shipped', 'Backorder']}
          rows={dropShips.map((r) => [
            r.orderNumber,
            r.customerId ?? '—',
            r.itemId,
            r.dropShipVendorId ?? '—',
            String(r.orderedQty),
            String(r.shippedQty),
            String(r.backorderedQty),
          ])}
        />
      )}

      {!loading && !error && active === 'sales-tax' && (
        <ReportTable
          headers={['Tax %', 'Qty', 'Taxable', 'Tax']}
          rows={tax.map((r) => [
            r.taxPercent.toFixed(2),
            String(r.quantity),
            money(r.taxableAmount),
            money(r.taxAmount),
          ])}
        />
      )}

      {!loading && !error && active === 'sales-trend' && (
        <ReportTable
          headers={['Year', 'Month', 'Qty', 'Net Sales', 'Tax']}
          rows={trend.map((r) => [
            String(r.year),
            String(r.month),
            String(r.quantity),
            money(r.netSales),
            money(r.taxAmount),
          ])}
        />
      )}

      {!loading && !error && active === 'shipping-log' && (
        <ReportTable
          headers={['Shipment', 'Order', 'Customer', 'Date', 'Carrier', 'Tracking', 'Freight', 'Value']}
          rows={shippingLog.map((r) => [
            r.shipmentNumber,
            r.salesOrderId ?? '—',
            r.customerId ?? '—',
            new Date(r.shipDate).toLocaleDateString(),
            r.carrier,
            r.trackingNumber,
            money(r.freightCost),
            money(r.shipmentValue),
          ])}
        />
      )}

      {!loading && !error && active === 'freight-analysis' && (
        <ReportTable
          headers={['Carrier', 'Shipments', 'Freight Cost', 'Goods Value']}
          rows={freight.map((r) => [
            r.carrier,
            String(r.shipmentCount),
            money(r.freightCost),
            money(r.goodsValue),
          ])}
        />
      )}
    </div>
  )
}

function ReportTable({ headers, rows }: { headers: string[]; rows: string[][] }) {
  return (
    <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
      <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead className="bg-gray-50 dark:bg-gray-800">
          <tr>
            {headers.map((h) => (
              <th
                key={h}
                className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500"
              >
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
          {rows.length === 0 ? (
            <tr>
              <td colSpan={headers.length} className="px-4 py-6 text-center text-sm text-gray-400">
                No records found.
              </td>
            </tr>
          ) : (
            rows.map((row, i) => (
              <tr key={i}>
                {row.map((cell, j) => (
                  <td key={j} className="px-4 py-2 text-sm">
                    {cell}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}
