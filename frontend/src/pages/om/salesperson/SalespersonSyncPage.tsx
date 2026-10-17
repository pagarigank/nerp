import { useState, useEffect } from 'react'
import {
  Database,
  RefreshCw,
  UploadCloud,
  Download,
  CheckCircle2,
  AlertCircle,
  FileText,
  Clock,
  HardDrive,
  Layers,
  ArrowRight,
} from 'lucide-react'
import { useAuthStore } from '@stores/authStore'
import { sqliteService, type DatabaseStats, type SalesCallRecord, type SalesCallItemRecord } from '@/utils/sqliteDb'
import { getOfflineSyncPackage, uploadOfflineSalesCalls } from '@/api/salesperson'

export function SalespersonSyncPage() {
  const currentCompany = useAuthStore((s) => s.currentCompany)
  const [stats, setStats] = useState<DatabaseStats | null>(null)
  const [salesCalls, setSalesCalls] = useState<(SalesCallRecord & { lines: SalesCallItemRecord[] })[]>([])
  const [isSyncing, setIsSyncing] = useState(false)
  const [isUploading, setIsUploading] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const selectedRepId = localStorage.getItem('nerp_selected_salesrep_id') || ''

  const reloadData = async () => {
    try {
      const s = await sqliteService.getDatabaseStats()
      setStats(s)
      const calls = await sqliteService.getAllSalesCalls()
      setSalesCalls(calls)
    } catch {
      // ignore
    }
  }

  useEffect(() => {
    reloadData()
  }, [])

  // Download Package to SQLite
  const handleDownloadPackage = async () => {
    if (!selectedRepId || !currentCompany?.id) {
      setError('Please select an active Sales Rep on the portal before syncing.')
      return
    }
    setIsSyncing(true)
    setMessage('Connecting to live server and fetching catalog/routes...')
    setError(null)
    try {
      const pkg = await getOfflineSyncPackage(selectedRepId, currentCompany.id)
      await sqliteService.importSyncPackage({
        serverTime: pkg.serverTime,
        routes: pkg.routes,
        products: pkg.products,
        pricingRules: pkg.pricingRules,
      })
      await reloadData()
      setMessage(`Successfully downloaded ${pkg.routes.length} routes, ${pkg.products.length} products, and ${pkg.pricingRules.length} pricing rules into SQLite!`)
      setTimeout(() => setMessage(null), 4000)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to download sync package.')
    } finally {
      setIsSyncing(false)
    }
  }

  // Upload unsynced calls
  const handleUploadCalls = async () => {
    if (!selectedRepId || !currentCompany?.id) return
    setIsUploading(true)
    setMessage('Uploading pending sales calls to server...')
    setError(null)
    try {
      const unsynced = await sqliteService.getUnsyncedSalesCalls()
      if (unsynced.length === 0) {
        setMessage('All sales calls are already synced.')
        setTimeout(() => setMessage(null), 3000)
        setIsUploading(false)
        return
      }

      const payload = {
        companyId: currentCompany.id,
        salesRepId: selectedRepId,
        calls: unsynced.map((c) => ({
          clientCallId: c.id,
          customerId: c.customerId,
          callDate: c.callDate,
          startTime: c.startTime,
          endTime: c.endTime,
          notes: c.notes,
          lines: c.lines.map((l) => ({
            itemId: l.itemId,
            itemCode: l.itemCode,
            description: l.description,
            quantity: l.orderQty,
            sellingPrice: l.sellingPrice,
            unitOfMeasure: 'EA',
            discountPercent: l.discountPercent,
            lineTotal: l.lineTotal,
          })),
          createSalesOrder: true,
        })),
      }

      const res = await uploadOfflineSalesCalls(payload)
      for (const item of res.items) {
        await sqliteService.markCallSynced(item.clientCallId, item.salesOrderId, item.salesOrderNumber)
      }

      await reloadData()
      setMessage(`Successfully uploaded and created ${res.totalProcessed} sales orders on the live server!`)
      setTimeout(() => setMessage(null), 4000)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to upload calls.')
    } finally {
      setIsUploading(false)
    }
  }

  // Export SQLite binary as download
  const handleExportSqliteFile = async () => {
    try {
      const blob = await sqliteService.exportDatabaseBlob()
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `nerp_salesperson_${new Date().toISOString().slice(0, 10)}.sqlite3`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to export SQLite database.')
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 shadow-sm">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-slate-900 to-indigo-800 text-white flex items-center justify-center shadow-md">
              <Database className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900 dark:text-white">
                Client SQLite Database &amp; Offline Sync Center
              </h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                WebAssembly SQLite storage engine, offline catalogue caching, and live server synchronization
              </p>
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex flex-wrap items-center gap-2.5">
            <button
              onClick={handleExportSqliteFile}
              className="px-3.5 py-2 border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 text-slate-700 dark:text-slate-200 rounded-lg text-xs font-bold flex items-center gap-1.5 shadow-sm"
              title="Download local SQLite database file to disk"
            >
              <Download className="w-4 h-4" />
              <span>Export .sqlite3 File</span>
            </button>

            <button
              onClick={handleDownloadPackage}
              disabled={isSyncing}
              className="px-3.5 py-2 bg-slate-900 hover:bg-slate-800 dark:bg-slate-100 dark:hover:bg-white text-white dark:text-slate-900 rounded-lg text-xs font-bold flex items-center gap-1.5 shadow-sm disabled:opacity-50"
            >
              <RefreshCw className={`w-4 h-4 ${isSyncing ? 'animate-spin' : ''}`} />
              <span>{isSyncing ? 'Downloading...' : 'Download Live Package to SQLite'}</span>
            </button>

            {stats && stats.unsyncedCallsCount > 0 && (
              <button
                onClick={handleUploadCalls}
                disabled={isUploading}
                className="px-3.5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold flex items-center gap-1.5 shadow-sm disabled:opacity-50 animate-pulse"
              >
                <UploadCloud className={`w-4 h-4 ${isUploading ? 'animate-spin' : ''}`} />
                <span>Sync {stats.unsyncedCallsCount} Pending Call{stats.unsyncedCallsCount > 1 ? 's' : ''}</span>
              </button>
            )}
          </div>
        </div>

        {/* Notifications */}
        {message && (
          <div className="mt-4 p-3 bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-emerald-800 dark:text-emerald-300 text-xs rounded-lg flex items-center gap-2">
            <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600" />
            <span>{message}</span>
          </div>
        )}
        {error && (
          <div className="mt-4 p-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 text-red-800 dark:text-red-300 text-xs rounded-lg flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0 text-red-600" />
            <span>{error}</span>
          </div>
        )}
      </div>

      {/* Database Diagnostic Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 shadow-sm">
          <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 text-xs mb-1 font-semibold">
            <HardDrive className="w-4 h-4 text-indigo-500" />
            <span>SQLite Database Size</span>
          </div>
          <div className="text-2xl font-black text-slate-900 dark:text-white tabular-nums">
            {stats?.databaseSizeKb || 0} <span className="text-xs font-normal text-slate-400">KB</span>
          </div>
        </div>

        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 shadow-sm">
          <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 text-xs mb-1 font-semibold">
            <Layers className="w-4 h-4 text-emerald-500" />
            <span>Products in SQLite</span>
          </div>
          <div className="text-2xl font-black text-slate-900 dark:text-white tabular-nums">
            {stats?.productsCount || 0}
          </div>
        </div>

        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 shadow-sm">
          <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 text-xs mb-1 font-semibold">
            <FileText className="w-4 h-4 text-blue-500" />
            <span>Assigned Routes</span>
          </div>
          <div className="text-2xl font-black text-slate-900 dark:text-white tabular-nums">
            {stats?.routesCount || 0}
          </div>
        </div>

        <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-4 shadow-sm">
          <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400 text-xs mb-1 font-semibold">
            <UploadCloud className="w-4 h-4 text-amber-500" />
            <span>Calls Pending Sync</span>
          </div>
          <div className={`text-2xl font-black tabular-nums ${stats?.unsyncedCallsCount ? 'text-amber-600' : 'text-slate-900 dark:text-white'}`}>
            {stats?.unsyncedCallsCount || 0}
          </div>
        </div>
      </div>

      {/* Recorded Sales Calls Table */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
        <div className="p-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <h2 className="text-sm font-bold text-slate-900 dark:text-white">
            Recorded Sales Calls &amp; Sync Status
          </h2>
          <span className="text-xs text-slate-500">
            {salesCalls.length} Call{salesCalls.length === 1 ? '' : 's'} recorded locally
          </span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-slate-500 font-bold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-4">Call # / Time</th>
                <th className="py-3 px-4">Customer</th>
                <th className="py-3 px-4 text-center">Items Ordered</th>
                <th className="py-3 px-4 text-right">Order Grand Total</th>
                <th className="py-3 px-4 text-center">Sync Status</th>
                <th className="py-3 px-4">Server Sales Order</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {salesCalls.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-slate-400">
                    No sales calls recorded in the local SQLite database yet.
                  </td>
                </tr>
              ) : (
                salesCalls.map((call) => (
                  <tr key={call.id} className="hover:bg-slate-50/80 dark:hover:bg-slate-800/50">
                    <td className="py-3 px-4">
                      <div className="font-bold text-slate-900 dark:text-white">{call.id.slice(0, 8)}</div>
                      <div className="text-[11px] text-slate-400 font-mono">
                        {new Date(call.startTime).toLocaleTimeString()} ({call.callDate})
                      </div>
                    </td>
                    <td className="py-3 px-4 font-semibold text-slate-800 dark:text-slate-200">
                      {call.customerName}
                    </td>
                    <td className="py-3 px-4 text-center tabular-nums">
                      <span className="px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-bold">
                        {call.lines.length} items
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right tabular-nums font-bold text-emerald-600 dark:text-emerald-400">
                      ${call.grandTotal.toFixed(2)}
                    </td>
                    <td className="py-3 px-4 text-center">
                      {call.isSynced === 1 ? (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800">
                          <CheckCircle2 className="w-3 h-3" /> Synced
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-50 text-amber-700 dark:bg-amber-950/50 dark:text-amber-400 border border-amber-200 dark:border-amber-800">
                          <Clock className="w-3 h-3" /> Pending Sync
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4 font-mono font-semibold text-indigo-600 dark:text-indigo-400">
                      {call.serverOrderNumber || '—'}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
