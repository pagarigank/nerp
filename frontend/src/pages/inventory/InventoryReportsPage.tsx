// Inventory Reports (Phase 7) — all 12 reports wired to the Inventory report API.
import { useEffect, useMemo, useState } from 'react'
import {
  Package,
  DollarSign,
  FileText,
  TrendingUp,
  BarChart3,
  Barcode,
  Hash,
  RefreshCw,
  AlertTriangle,
  Layers,
} from 'lucide-react'
import { getErrorMessage } from '@api/client'
import {
  getValuationReport,
  getReorderReport,
  getTransactionHistory,
  getStockOutReport,
  getNegativeReport,
  getSlowMovingReport,
  getAbcAnalysis,
  getLotTraceability,
  getSerialTraceability,
  getInventoryTurnover,
  getCycleCountVariance,
  getCycleCountSummary,
} from '@api/inventory'

type ReportKey =
  | 'valuation'
  | 'reorder'
  | 'transactions'
  | 'stock-out'
  | 'negative'
  | 'slow-moving'
  | 'abc-analysis'
  | 'lot-traceability'
  | 'serial-traceability'
  | 'inventory-turnover'
  | 'cycle-count-variance'
  | 'cycle-count-summary'

const money = (n: number) => `$${Number(n).toFixed(2)}`
const pct = (n: number) => `${Number(n).toFixed(1)}%`

const tabs: { key: ReportKey; label: string; icon: typeof Package }[] = [
  { key: 'valuation', label: 'Valuation', icon: DollarSign },
  { key: 'reorder', label: 'Reorder', icon: AlertTriangle },
  { key: 'transactions', label: 'Transactions', icon: FileText },
  { key: 'stock-out', label: 'Stock-Out', icon: Package },
  { key: 'negative', label: 'Negative', icon: TrendingUp },
  { key: 'slow-moving', label: 'Slow-Moving', icon: Layers },
  { key: 'abc-analysis', label: 'ABC Analysis', icon: BarChart3 },
  { key: 'lot-traceability', label: 'Lot Traceability', icon: Barcode },
  { key: 'serial-traceability', label: 'Serial Traceability', icon: Hash },
  { key: 'inventory-turnover', label: 'Turnover', icon: RefreshCw },
  { key: 'cycle-count-variance', label: 'Count Variance', icon: Layers },
  { key: 'cycle-count-summary', label: 'Count Summary', icon: BarChart3 },
]

export function InventoryReportsPage() {
  const [active, setActive] = useState<ReportKey>('valuation')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [data, setData] = useState<Record<string, unknown>[]>([])

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        let rows: unknown[] = []
        switch (active) {
          case 'valuation': rows = await getValuationReport(); break
          case 'reorder': rows = await getReorderReport(); break
          case 'transactions': rows = await getTransactionHistory(); break
          case 'stock-out': rows = await getStockOutReport(); break
          case 'negative': rows = await getNegativeReport(); break
          case 'slow-moving': rows = await getSlowMovingReport(); break
          case 'abc-analysis': rows = await getAbcAnalysis(); break
          case 'lot-traceability': rows = await getLotTraceability(); break
          case 'serial-traceability': rows = await getSerialTraceability(); break
          case 'inventory-turnover': {
            const to = new Date().toISOString()
            const from = new Date(Date.now() - 365 * 24 * 3600 * 1000).toISOString()
            rows = await getInventoryTurnover(from, to); break
          }
          case 'cycle-count-variance': rows = await getCycleCountVariance(); break
          case 'cycle-count-summary': rows = await getCycleCountSummary(); break
        }
        setData(rows as Record<string, unknown>[])
      } catch (e) {
        setError(getErrorMessage(e))
      } finally {
        setLoading(false)
      }
    }
    void load()
  }, [active])

  const columns = useMemo(() => reportColumns(active), [active])

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Inventory Reports</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Generate inventory reports and analytics
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setActive(t.key)}
            className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium ${
              active === t.key
                ? 'bg-primary-600 text-white'
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

      {!loading && !error && data.length === 0 && (
        <p className="text-sm text-gray-400">No records found.</p>
      )}

      {!loading && !error && data.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-800">
              <tr>
                {columns.map((h) => (
                  <th key={h} className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {data.map((row, i) => (
                <tr key={i}>
                  {columns.map((c) => (
                    <td key={c} className="px-4 py-2 text-sm">
                      {formatCell(c, (row as Record<string, unknown>)[camel(c)])}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function camel(header: string): string {
  return header.replace(/[^a-zA-Z0-9]+(.)/g, (_, chr) => chr.toUpperCase())
}

function formatCell(header: string, value: unknown): string {
  if (value === null || value === undefined) return '—'
  if (typeof value === 'boolean') return value ? 'Yes' : 'No'
  if (/value|cost|cogs|variance|extended|turnover/i.test(header) && typeof value === 'number') return money(value)
  if (/percent/i.test(header) && typeof value === 'number') return pct(value)
  if (/date|receivedDate|expirationDate|installationDate|transactionDate/i.test(header) && typeof value === 'string') {
    return new Date(value).toLocaleDateString()
  }
  return String(value)
}

function reportColumns(key: ReportKey): string[] {
  const map: Record<ReportKey, string[]> = {
    valuation: ['Item', 'Description', 'Warehouse', 'On Hand', 'Unit Cost', 'Extended', 'ABC'],
    reorder: ['Item', 'Description', 'Warehouse', 'Available', 'Reorder Pt', 'Suggested', 'Lead Time'],
    transactions: ['Type', 'Item', 'Warehouse', 'Qty', 'Cost', 'Extended', 'Date', 'Lot', 'Serial'],
    'stock-out': ['Item', 'Description', 'Warehouse', 'On Hand', 'Allocated', 'Reorder Pt'],
    negative: ['Item', 'Description', 'Warehouse', 'On Hand', 'Allocated'],
    'slow-moving': ['Item', 'Description', 'Warehouse', 'On Hand', 'Unit Cost', 'On Hand Value', 'Last Movement'],
    'abc-analysis': ['Item', 'Description', 'Usage Value', '% of Total', 'Cumulative %', 'Class'],
    'lot-traceability': ['Lot', 'Item', 'Warehouse', 'Received', 'Expiration', 'Status', 'Received Qty', 'Issued Qty', 'Remaining'],
    'serial-traceability': ['Serial', 'Item', 'Warehouse', 'Received', 'Status', 'Customer', 'Installed'],
    'inventory-turnover': ['Item', 'Description', 'COGS', 'Avg Inventory', 'Turnover'],
    'cycle-count-variance': ['Count', 'Item', 'Warehouse', 'System', 'Counted', 'Variance', 'Variance $', '%', 'Notes'],
    'cycle-count-summary': ['Warehouse', 'Lines', 'System', 'Counted', 'Variance', 'Variance $'],
  }
  return map[key]
}
