import { useState, useEffect, useMemo } from 'react'
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
  Inbox,
  PlayCircle,
  Filter,
  ListChecks,
} from 'lucide-react'
import { useAuthStore } from '@stores/authStore'
import { sqliteService, type DatabaseStats, type SalesCallRecord, type SalesCallItemRecord } from '@/utils/sqliteDb'
import {
  getOfflineSyncPackage,
  uploadOfflineSalesCalls,
  getParkedSalesCalls,
  processParkedOrders,
  type ParkedCallSummary,
  type ProcessCallResultItem,
} from '@/api/salesperson'

export function SalespersonSyncPage() {
  const currentCompany = useAuthStore((s) => s.currentCompany)
  const [stats, setStats] = useState<DatabaseStats | null>(null)
  const [salesCalls, setSalesCalls] = useState<(SalesCallRecord & { lines: SalesCallItemRecord[] })[]>([])
  const [isSyncing, setIsSyncing] = useState(false)
  const [isUploading, setIsUploading] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [parkedCalls, setParkedCalls] = useState<ParkedCallSummary[]>([])
  const [selectedCallIds, setSelectedCallIds] = useState<Set<string>>(new Set())
  const [isProcessing, setIsProcessing] = useState(false)
  const [processResults, setProcessResults] = useState<ProcessCallResultItem[] | null>(null)
  const [parkedError, setParkedError] = useState<string | null>(null)
  const [pendingCalls, setPendingCalls] = useState<(SalesCallRecord & { lines: SalesCallItemRecord[] })[]>([])
  const [selectedPendingIds, setSelectedPendingIds] = useState<Set<string>>(new Set())
  const [pendingDayFilter, setPendingDayFilter] = useState<string>('all')

  const selectedRepId = localStorage.getItem('nerp_selected_salesrep_id') || ''

  const reloadData = async () => {
    try {
      const s = await sqliteService.getDatabaseStats()
      setStats(s)
      const calls = await sqliteService.getAllSalesCalls()
      setSalesCalls(calls)
      const unsynced = await sqliteService.getUnsyncedSalesCalls()
      setPendingCalls(unsynced)
    } catch {
      // ignore
    }
  }

  // Distinct call dates present in the pending queue, newest first
  const pendingDays = useMemo(() => {
    const days = Array.from(new Set(pendingCalls.map((c) => c.callDate)))
    days.sort().reverse()
    return days
  }, [pendingCalls])

  // Pending calls narrowed by the selected day filter
  const filteredPending = useMemo(() => {
    if (pendingDayFilter === 'all') return pendingCalls
    return pendingCalls.filter((c) => c.callDate === pendingDayFilter)
  }, [pendingCalls, pendingDayFilter])

  const togglePendingSelection = (id: string) => {
    setSelectedPendingIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const toggleSelectAllPending = () => {
    const visibleIds = filteredPending.map((c) => c.id)
    const allSelected = visibleIds.length > 0 && visibleIds.every((id) => selectedPendingIds.has(id))
    setSelectedPendingIds((prev) => {
      const next = new Set(prev)
      if (allSelected) {
        visibleIds.forEach((id) => next.delete(id))
      } else {
        visibleIds.forEach((id) => next.add(id))
      }
      return next
    })
  }

  const loadParkedCalls = async () => {
    if (!currentCompany?.id) return
    try {
      const list = await getParkedSalesCalls(currentCompany.id, selectedRepId || undefined)
      setParkedCalls(list)
      setParkedError(null)
    } catch {
      setParkedCalls([])
      setParkedError('Server unreachable — parked orders are available when online.')
    }
  }

  useEffect(() => {
    reloadData()
    loadParkedCalls()
  }, [])

  const toggleCallSelection = (id: string) => {
    setSelectedCallIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const toggleSelectAllParked = () => {
    if (selectedCallIds.size === parkedCalls.length) {
      setSelectedCallIds(new Set())
    } else {
      setSelectedCallIds(new Set(parkedCalls.map((c) => c.id)))
    }
  }

  const handleProcessSelected = async () => {
    if (!currentCompany?.id || selectedCallIds.size === 0) return
    setIsProcessing(true)
    setError(null)
    try {
      const res = await processParkedOrders(Array.from(selectedCallIds), currentCompany.id)
      setProcessResults(res.items)
      setSelectedCallIds(new Set())
      await loadParkedCalls()
      const processed = res.totalProcessed
      setMessage(
        processed > 0
          ? `Processed ${processed} call(s) into Sales Orders.`
          : 'No orders were created — see the result table below for details.',
      )
      setTimeout(() => setMessage(null), 5000)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to process parked orders.')
    } finally {
      setIsProcessing(false)
    }
  }

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

  // Upload only the SELECTED offline calls (allows syncing a specific day / subset)
  const handleUploadCalls = async () => {
    if (!selectedRepId || !currentCompany?.id) return
    const selected = pendingCalls.filter((c) => selectedPendingIds.has(c.id))
    if (selected.length === 0) {
      setError('Select at least one pending call to sync.')
      setTimeout(() => setError(null), 3000)
      return
    }
    setIsUploading(true)
    setMessage(`Uploading ${selected.length} selected call(s) to server...`)
    setError(null)
    try {
      const payload = {
        companyId: currentCompany.id,
        salesRepId: selectedRepId,
        calls: selected.map((c) => ({
          clientCallId: c.id,
          customerId: c.customerId,
          routeId: c.routeId,
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
            unitOfMeasure: l.unitOfMeasure || 'EA',
            discountPercent: l.discountPercent,
            lineTotal: l.lineTotal,
          })),
        })),
      }

      const res = await uploadOfflineSalesCalls(payload)
      for (const item of res.items) {
        await sqliteService.markCallSynced(item.clientCallId, item.salesOrderId, item.salesOrderNumber)
      }

      setSelectedPendingIds(new Set())
      await reloadData()
      await loadParkedCalls()
      setMessage(`Uploaded ${res.totalProcessed} call(s) to the server. They are parked for review — process them into Sales Orders in the Parked Orders section below.`)
      setTimeout(() => setMessage(null), 5000)
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
                disabled={isUploading || selectedPendingIds.size === 0}
                className="px-3.5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-lg text-xs font-bold flex items-center gap-1.5 shadow-sm disabled:opacity-50 disabled:cursor-not-allowed animate-pulse"
                title="Upload only the calls selected in the Pending Sync list below"
              >
                <UploadCloud className={`w-4 h-4 ${isUploading ? 'animate-spin' : ''}`} />
                <span>
                  {isUploading
                    ? 'Uploading...'
                    : selectedPendingIds.size > 0
                    ? `Sync ${selectedPendingIds.size} Selected`
                    : 'Select Calls to Sync'}
                </span>
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

      {/* Pending Sync — choose which offline calls (by day / individually) to upload */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
        <div className="p-4 border-b border-slate-200 dark:border-slate-800 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <ListChecks className="w-4 h-4 text-amber-500" />
            <h2 className="text-sm font-bold text-slate-900 dark:text-white">
              Pending Sync (Offline Sales Calls)
            </h2>
            <span className="text-xs text-slate-500">
              {filteredPending.length} of {pendingCalls.length} pending
              {selectedPendingIds.size > 0 && (
                <span className="ml-1.5 font-bold text-emerald-600">· {selectedPendingIds.size} selected</span>
              )}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <Filter className="w-3.5 h-3.5 text-slate-400" />
            <select
              value={pendingDayFilter}
              onChange={(e) => setPendingDayFilter(e.target.value)}
              className="px-2.5 py-1.5 border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-200 rounded-lg text-xs font-semibold outline-none cursor-pointer"
              aria-label="Filter pending calls by day"
            >
              <option value="all">All days</option>
              {pendingDays.map((d) => (
                <option key={d} value={d}>
                  {new Date(d).toLocaleDateString()}
                </option>
              ))}
            </select>
            {filteredPending.length > 0 && (
              <button
                onClick={toggleSelectAllPending}
                className="px-2.5 py-1.5 border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 text-slate-600 dark:text-slate-300 rounded-lg text-[11px] font-bold"
              >
                {filteredPending.every((c) => selectedPendingIds.has(c.id)) ? 'Deselect All' : 'Select All'}
              </button>
            )}
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-slate-500 font-bold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-4 w-10">
                  <input
                    type="checkbox"
                    checked={filteredPending.length > 0 && filteredPending.every((c) => selectedPendingIds.has(c.id))}
                    onChange={toggleSelectAllPending}
                    className="rounded border-slate-300"
                    aria-label="Select all visible pending calls"
                  />
                </th>
                <th className="py-3 px-4">Call # / Time</th>
                <th className="py-3 px-4">Customer</th>
                <th className="py-3 px-4">Call Date</th>
                <th className="py-3 px-4 text-center">Items</th>
                <th className="py-3 px-4 text-right">Total</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {filteredPending.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-10 text-center text-slate-400">
                    {pendingCalls.length === 0
                      ? 'No pending calls — all offline calls are already synced.'
                      : 'No pending calls for the selected day.'}
                  </td>
                </tr>
              ) : (
                filteredPending.map((call) => (
                  <tr key={call.id} className="hover:bg-slate-50/80 dark:hover:bg-slate-800/50">
                    <td className="py-3 px-4">
                      <input
                        type="checkbox"
                        checked={selectedPendingIds.has(call.id)}
                        onChange={() => togglePendingSelection(call.id)}
                        className="rounded border-slate-300"
                        aria-label={`Select call for ${call.customerName}`}
                      />
                    </td>
                    <td className="py-3 px-4">
                      <div className="font-mono font-bold text-slate-900 dark:text-white">{call.id.slice(0, 8)}</div>
                      <div className="text-[11px] text-slate-400 font-mono">
                        {new Date(call.startTime).toLocaleTimeString()}
                      </div>
                    </td>
                    <td className="py-3 px-4 font-semibold text-slate-800 dark:text-slate-200">
                      {call.customerName}
                    </td>
                    <td className="py-3 px-4 text-slate-600 dark:text-slate-400">
                      {new Date(call.callDate).toLocaleDateString()}
                    </td>
                    <td className="py-3 px-4 text-center tabular-nums">
                      <span className="px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-bold">
                        {call.lines.length}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right tabular-nums font-bold text-emerald-600 dark:text-emerald-400">
                      ${call.grandTotal.toFixed(2)}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Parked Orders — synced calls awaiting processing into Sales Orders */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
        <div className="p-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Inbox className="w-4 h-4 text-amber-500" />
            <h2 className="text-sm font-bold text-slate-900 dark:text-white">
              Parked Orders (Awaiting Processing)
            </h2>
          </div>
          <div className="flex items-center gap-2.5">
            <span className="text-xs text-slate-500">
              {parkedCalls.length} parked call{parkedCalls.length === 1 ? '' : 's'}
            </span>
            <button
              onClick={loadParkedCalls}
              className="px-2.5 py-1.5 border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 hover:bg-slate-50 text-slate-600 dark:text-slate-300 rounded-lg text-[11px] font-bold flex items-center gap-1"
              title="Refresh parked list from server"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              <span>Refresh</span>
            </button>
            {parkedCalls.length > 0 && (
              <button
                onClick={handleProcessSelected}
                disabled={isProcessing || selectedCallIds.size === 0}
                className="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-[11px] font-bold flex items-center gap-1.5 shadow-sm disabled:opacity-50"
              >
                <PlayCircle className={`w-3.5 h-3.5 ${isProcessing ? 'animate-spin' : ''}`} />
                <span>
                  {isProcessing
                    ? 'Processing...'
                    : `Process Selected${selectedCallIds.size > 0 ? ` (${selectedCallIds.size})` : ''}`}
                </span>
              </button>
            )}
          </div>
        </div>

        {parkedError && (
          <div className="p-3 bg-amber-50 dark:bg-amber-950/40 border-b border-amber-200 dark:border-amber-800 text-amber-800 dark:text-amber-300 text-xs flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{parkedError}</span>
          </div>
        )}

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-slate-500 font-bold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-4 w-10">
                  <input
                    type="checkbox"
                    checked={parkedCalls.length > 0 && selectedCallIds.size === parkedCalls.length}
                    onChange={toggleSelectAllParked}
                    className="rounded border-slate-300"
                    aria-label="Select all parked calls"
                  />
                </th>
                <th className="py-3 px-4">Call #</th>
                <th className="py-3 px-4">Customer</th>
                <th className="py-3 px-4">Call Date</th>
                <th className="py-3 px-4 text-center">Lines</th>
                <th className="py-3 px-4 text-right">Total</th>
                <th className="py-3 px-4">Source</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {parkedCalls.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-10 text-center text-slate-400">
                    {parkedError
                      ? 'Unable to load parked orders from the server.'
                      : 'No parked orders. Synced calls appear here until you process them into Sales Orders.'}
                  </td>
                </tr>
              ) : (
                parkedCalls.map((call) => (
                  <tr key={call.id} className="hover:bg-slate-50/80 dark:hover:bg-slate-800/50">
                    <td className="py-3 px-4">
                      <input
                        type="checkbox"
                        checked={selectedCallIds.has(call.id)}
                        onChange={() => toggleCallSelection(call.id)}
                        className="rounded border-slate-300"
                        aria-label={`Select call ${call.callNumber}`}
                      />
                    </td>
                    <td className="py-3 px-4 font-mono font-bold text-slate-900 dark:text-white">
                      {call.callNumber}
                    </td>
                    <td className="py-3 px-4 font-semibold text-slate-800 dark:text-slate-200">
                      {call.customerName}
                      {call.customerCode && (
                        <span className="ml-1.5 text-[11px] text-slate-400 font-mono">({call.customerCode})</span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-slate-600 dark:text-slate-400">
                      {new Date(call.callDate).toLocaleDateString()}
                    </td>
                    <td className="py-3 px-4 text-center tabular-nums">
                      <span className="px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-bold">
                        {call.lineCount}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right tabular-nums font-bold text-emerald-600 dark:text-emerald-400">
                      ${call.totalAmount.toFixed(2)}
                    </td>
                    <td className="py-3 px-4">
                      {call.isOfflineCreated ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-indigo-50 text-indigo-700 dark:bg-indigo-950/50 dark:text-indigo-400 border border-indigo-200 dark:border-indigo-800">
                          Offline
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400 border border-slate-200 dark:border-slate-700">
                          Online
                        </span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Processing Results */}
        {processResults && processResults.length > 0 && (
          <div className="border-t border-slate-200 dark:border-slate-800 p-4">
            <h3 className="text-xs font-bold text-slate-900 dark:text-white mb-2">Processing Results</h3>
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="text-slate-500 font-bold uppercase tracking-wider text-[10px] border-b border-slate-200 dark:border-slate-800">
                  <th className="py-2 pr-3">Call #</th>
                  <th className="py-2 pr-3">Status</th>
                  <th className="py-2 pr-3">Sales Order</th>
                  <th className="py-2">Message</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
                {processResults.map((r) => (
                  <tr key={r.callId}>
                    <td className="py-2 pr-3 font-mono font-semibold">{r.callNumber || '—'}</td>
                    <td className="py-2 pr-3">
                      {r.status === 'Processed' ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800">
                          <CheckCircle2 className="w-3 h-3" /> Processed
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400 border border-slate-200 dark:border-slate-700">
                          {r.status}
                        </span>
                      )}
                    </td>
                    <td className="py-2 pr-3 font-mono font-semibold text-indigo-600 dark:text-indigo-400">
                      {r.salesOrderNumber || '—'}
                    </td>
                    <td className="py-2 text-slate-500 dark:text-slate-400">{r.message}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
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
