import { useState, useMemo } from 'react'
import { BarChart3 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import { getBomListing, getBuildHistory, getBomAccuracy } from '@api/bom'
import { getItems } from '@api/inventory'
import { useQuery } from '@tanstack/react-query'
import type { BomListingItem, BuildHistoryEntry, BomAccuracyItem } from '@/types/bom'
import type { ItemSummary } from '@/types/inventory'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toFixed(4)}` : '—')

export function BomReportsPage() {
  const [tab, setTab] = useState<'listing' | 'history' | 'accuracy'>('listing')

  const { data: items = [] } = useQuery({
    queryKey: ['inventory', 'items'],
    queryFn: () => getItems(),
  })

  const itemMap = useMemo(() => Object.fromEntries((items as ItemSummary[]).map(i => [i.id, i])), [items])

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
          <BarChart3 className="h-6 w-6" /> BOM Reports
        </h1>
      </div>

      <div className="flex gap-2 border-b border-gray-200 dark:border-gray-700">
        {[
          { key: 'listing' as const, label: 'BOM Listing' },
          { key: 'history' as const, label: 'Build History' },
          { key: 'accuracy' as const, label: 'Accuracy Report' },
        ].map(t => (
          <button key={t.key} onClick={() => setTab(t.key)}
            className={`px-3 py-2 text-sm font-medium ${
              tab === t.key ? 'border-b-2 border-blue-600 text-blue-600' : 'text-gray-600 hover:text-gray-900 dark:text-gray-400'
            }`}>{t.label}</button>
        ))}
      </div>

      {tab === 'listing' && <ListingReport itemMap={itemMap} />}
      {tab === 'history' && <HistoryReport itemMap={itemMap} />}
      {tab === 'accuracy' && <AccuracyReport itemMap={itemMap} />}
    </div>
  )
}

function ListingReport({ itemMap }: { itemMap: Record<string, ItemSummary> }) {
  const { data: listings = [], isLoading } = useQuery({
    queryKey: ['bom', 'listing'],
    queryFn: () => getBomListing(),
  })

  const columns: DataTableColumn<BomListingItem>[] = [
    {
      key: 'parentItemId', header: 'Parent Item',
      render: (r: BomListingItem) => itemMap[r.parentItemId]?.itemCode ?? r.parentItemId.slice(0, 8),
    },
    { key: 'revision', header: 'Rev' },
    { key: 'status', header: 'Status' },
    { key: 'componentCount', header: 'Components', align: 'right', render: (r: BomListingItem) => r.components.length },
    { key: 'yieldPercentage', header: 'Yield %', align: 'right', render: (r: BomListingItem) => `${r.yieldPercentage}%` },
  ]

  return <DataTable data={listings as BomListingItem[]} columns={columns} isLoading={isLoading} emptyMessage="No BOM listings." />
}

function HistoryReport({ itemMap }: { itemMap: Record<string, ItemSummary> }) {
  const { data: history = [], isLoading } = useQuery({
    queryKey: ['bom', 'buildHistory'],
    queryFn: () => getBuildHistory(),
  })

  const columns: DataTableColumn<BuildHistoryEntry>[] = [
    { key: 'buildNumber', header: 'Build #', sortable: true },
    { key: 'transactionType', header: 'Type' },
    {
      key: 'parentItemId', header: 'Parent Item',
      render: (r: BuildHistoryEntry) => itemMap[r.parentItemId]?.itemCode ?? r.parentItemId.slice(0, 8),
    },
    { key: 'quantityBuilt', header: 'Qty Built', align: 'right' },
    { key: 'actualYield', header: 'Actual Yield', align: 'right', render: (r: BuildHistoryEntry) => r.actualYield?.toFixed(0) ?? '—' },
    { key: 'yieldPercentage', header: 'Yield %', align: 'right', render: (r: BuildHistoryEntry) => r.yieldPercentage ? `${r.yieldPercentage.toFixed(1)}%` : '—' },
    { key: 'totalCost', header: 'Total Cost', align: 'right', render: (r: BuildHistoryEntry) => MONEY(r.totalCost) },
    { key: 'unitCost', header: 'Unit Cost', align: 'right', render: (r: BuildHistoryEntry) => MONEY(r.unitCost) },
    { key: 'componentCount', header: 'Components', align: 'right' },
    { key: 'totalScrapCost', header: 'Scrap Cost', align: 'right', render: (r: BuildHistoryEntry) => MONEY(r.totalScrapCost || null) },
    { key: 'status', header: 'Status' },
    { key: 'buildDate', header: 'Date', render: (r: BuildHistoryEntry) => new Date(r.buildDate).toLocaleDateString() },
  ]

  return <DataTable data={history as BuildHistoryEntry[]} columns={columns} isLoading={isLoading} emptyMessage="No build history." />
}

function AccuracyReport({ itemMap }: { itemMap: Record<string, ItemSummary> }) {
  const { data: issues = [], isLoading } = useQuery({
    queryKey: ['bom', 'accuracy'],
    queryFn: () => getBomAccuracy(),
  })

  const columns: DataTableColumn<BomAccuracyItem>[] = [
    {
      key: 'parentItemId', header: 'Parent Item',
      render: (r: BomAccuracyItem) => itemMap[r.parentItemId]?.itemCode ?? r.parentItemId.slice(0, 8),
    },
    { key: 'revision', header: 'Rev' },
    { key: 'status', header: 'Status' },
    { key: 'issueCount', header: 'Issues', align: 'right' },
    {
      key: 'issues', header: 'Details',
      render: (r: BomAccuracyItem) => (
        <ul className="list-disc list-inside text-xs text-red-600 dark:text-red-400">
          {r.issues.map((issue, i) => <li key={i}>{issue}</li>)}
        </ul>
      ),
    },
  ]

  return <DataTable data={issues as BomAccuracyItem[]} columns={columns} isLoading={isLoading} emptyMessage="No BOM accuracy issues found. All BOMs look healthy!" />
}
