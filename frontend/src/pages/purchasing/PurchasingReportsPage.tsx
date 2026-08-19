import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { FileText, TrendingUp, Package, Link2, BarChart3, Trophy, AlertTriangle, Table2, Clock } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge } from '@components/ui/Badge'
import {
  getOpenPOReport,
  getRequisitionStatusReport,
  getReceivingReport,
  getCommittedCostReport,
  getPOStatusReport,
  getVendorPerformanceReport,
  getPurchaseAnalysisReport,
  getPriceVarianceReport,
  getOverReceiptExceptionReport,
} from '@api/purchasing'

type ReportKey =
  | 'open-po' | 'requisition-status' | 'receiving-report' | 'committed-cost'
  | 'po-status' | 'vendor-performance' | 'purchase-analysis' | 'price-variance' | 'over-receipt'

const reports: { key: ReportKey; name: string; icon: typeof FileText; description: string }[] = [
  { key: 'open-po', name: 'Open PO Report', icon: Package, description: 'Open purchase orders by vendor with remaining amounts' },
  { key: 'requisition-status', name: 'Requisition Status', icon: FileText, description: 'Requisition conversion and approval status' },
  { key: 'receiving-report', name: 'Receiving Report', icon: TrendingUp, description: 'Goods receipts by vendor, item, and date' },
  { key: 'committed-cost', name: 'Committed Cost', icon: Link2, description: 'Open PO committed amounts vs received/remaining' },
  { key: 'po-status', name: 'PO Status', icon: Clock, description: 'Purchase orders grouped by status with amounts and time in status' },
  { key: 'vendor-performance', name: 'Vendor Performance', icon: Trophy, description: 'On-time delivery and spend by vendor' },
  { key: 'purchase-analysis', name: 'Purchase Analysis', icon: BarChart3, description: 'Spend cube by vendor and buyer' },
  { key: 'price-variance', name: 'Price Variance', icon: Table2, description: 'PO price vs vendor standard cost with variance %' },
  { key: 'over-receipt', name: 'Over-receipt Exceptions', icon: AlertTriangle, description: 'Receipts exceeding ordered quantities' },
]

function money(n: number) { return n.toFixed(2) }
function pct(n: number) { return n.toFixed(1) + '%' }

export function PurchasingReportsPage() {
  const [active, setActive] = useState<ReportKey>('open-po')

  const openPO = useQuery({ queryKey: ['purchasing', 'reports', 'open-po'], queryFn: () => getOpenPOReport(), enabled: active === 'open-po' })
  const reqStatus = useQuery({ queryKey: ['purchasing', 'reports', 'requisition-status'], queryFn: () => getRequisitionStatusReport(), enabled: active === 'requisition-status' })
  const receiving = useQuery({ queryKey: ['purchasing', 'reports', 'receiving-report'], queryFn: () => getReceivingReport(), enabled: active === 'receiving-report' })
  const committed = useQuery({ queryKey: ['purchasing', 'reports', 'committed-cost'], queryFn: () => getCommittedCostReport(), enabled: active === 'committed-cost' })
  const poStatus = useQuery({ queryKey: ['purchasing', 'reports', 'po-status'], queryFn: () => getPOStatusReport(), enabled: active === 'po-status' })
  const vendorPerf = useQuery({ queryKey: ['purchasing', 'reports', 'vendor-performance'], queryFn: () => getVendorPerformanceReport(), enabled: active === 'vendor-performance' })
  const purchaseAnalysis = useQuery({ queryKey: ['purchasing', 'reports', 'purchase-analysis'], queryFn: () => getPurchaseAnalysisReport(), enabled: active === 'purchase-analysis' })
  const priceVariance = useQuery({ queryKey: ['purchasing', 'reports', 'price-variance'], queryFn: () => getPriceVarianceReport(), enabled: active === 'price-variance' })
  const overReceipt = useQuery({ queryKey: ['purchasing', 'reports', 'over-receipt'], queryFn: () => getOverReceiptExceptionReport(), enabled: active === 'over-receipt' })

  const current = reports.find(r => r.key === active)!

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Purchasing Reports</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Generate purchasing reports and analytics</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {reports.map(r => (
          <button
            key={r.key}
            onClick={() => setActive(r.key)}
            className={`text-left rounded-lg border p-4 transition-colors ${
              active === r.key
                ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20'
                : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'
            }`}
          >
            <div className="flex items-center gap-2">
              <r.icon className="h-5 w-5 text-primary-600 dark:text-primary-400" />
              <span className="font-medium text-gray-900 dark:text-white">{r.name}</span>
            </div>
            <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{r.description}</p>
          </button>
        ))}
      </div>

      <Card>
        <CardHeader title={current.name} description={current.description} />
        <CardContent>
          {active === 'open-po' && (
            openPO.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (openPO.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No open POs.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">PO #</th><th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                <th className="px-3 py-2 font-medium text-gray-500">Order Date</th><th className="px-3 py-2 font-medium text-gray-500">Status</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Original</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Received</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Remaining</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {openPO.data!.map(r => (
                  <tr key={r.poId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.poNumber}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.vendorName}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.orderDate).toLocaleDateString()}</td>
                    <td className="px-3 py-3"><Badge variant="neutral" size="sm" dot>{r.status}</Badge></td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{money(r.originalAmount)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.receivedAmount)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.remainingAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}

          {active === 'requisition-status' && (
            reqStatus.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (reqStatus.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No requisitions.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">Requisition #</th><th className="px-3 py-2 font-medium text-gray-500">Status</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Total</th><th className="px-3 py-2 font-medium text-gray-500">Converted</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {reqStatus.data!.map(r => (
                  <tr key={r.requisitionId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.requisitionNumber}</td>
                    <td className="px-3 py-3"><Badge variant="neutral" size="sm" dot>{r.status}</Badge></td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{money(r.totalAmount)}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.convertedToPo ? 'Yes' : 'No'}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}

          {active === 'receiving-report' && (
            receiving.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (receiving.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No receipts.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">Receipt #</th><th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                <th className="px-3 py-2 font-medium text-gray-500">Received</th><th className="px-3 py-2 font-medium text-gray-500">Item</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Qty</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {receiving.data!.map(r => (
                  <tr key={r.receiptId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.receiptNumber}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.vendorName}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.receivedDate).toLocaleDateString()}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.itemDescription}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.quantityReceived}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}

          {active === 'committed-cost' && (
            committed.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (committed.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No committed cost.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">PO #</th><th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Committed</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Received</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Remaining</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {committed.data!.map(r => (
                  <tr key={r.poId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.poNumber}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.vendorName}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{money(r.committedAmount)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.receivedAmount)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.remainingAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}

          {active === 'po-status' && (
            poStatus.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (poStatus.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No purchase orders.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">Status</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Count</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Total</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Avg Days In Status</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {poStatus.data!.map(r => (
                  <tr key={r.status} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.status}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.count}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.totalAmount)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.averageDaysInStatus.toFixed(1)}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}

          {active === 'vendor-performance' && (
            vendorPerf.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (vendorPerf.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">Vendor</th><th className="px-3 py-2 font-medium text-gray-500 text-right">POs</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Total Spend</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">On-time</th><th className="px-3 py-2 font-medium text-gray-500 text-right">On-time %</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {vendorPerf.data!.map(r => (
                  <tr key={r.vendorId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.vendorId.slice(0, 8)}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.totalPOs}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.totalAmount)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.onTimePOs}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{pct(r.onTimePercentage)}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}

          {active === 'purchase-analysis' && (
            purchaseAnalysis.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (purchaseAnalysis.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">Vendor</th><th className="px-3 py-2 font-medium text-gray-500">Buyer</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">POs</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Spend</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Tax</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Freight</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Lines</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {purchaseAnalysis.data!.map((r, i) => (
                  <tr key={i} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.vendorId.slice(0, 8)}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.buyerId.slice(0, 8)}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.poCount}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.totalSpend)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.totalTax)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.totalFreight)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.lineCount}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}

          {active === 'price-variance' && (
            priceVariance.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (priceVariance.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No price variances beyond threshold.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">PO #</th><th className="px-3 py-2 font-medium text-gray-500">Item</th><th className="px-3 py-2 font-medium text-gray-500">Desc</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Std Cost</th><th className="px-3 py-2 font-medium text-gray-500 text-right">PO Price</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Var %</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Ext Var</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {priceVariance.data!.map((r, i) => (
                  <tr key={i} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.poNumber}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.itemId ?? '—'}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.description}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{money(r.vendorStandardCost)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.poUnitPrice)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{pct(r.variancePercent)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.extendedVariance)}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}

          {active === 'over-receipt' && (
            overReceipt.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            (overReceipt.data?.length ?? 0) === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No over-receipt exceptions.</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">Receipt</th><th className="px-3 py-2 font-medium text-gray-500">Received</th><th className="px-3 py-2 font-medium text-gray-500">Item</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Ordered</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Received</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Over %</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {overReceipt.data!.map((r, i) => (
                  <tr key={i} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.receiptNumber}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.receivedDate).toLocaleDateString()}</td>
                    <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.description}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.orderedQuantity}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.receivedQuantity}</td>
                    <td className="px-3 py-3 text-right text-red-600 font-medium">{pct(r.overReceiptPercent)}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
