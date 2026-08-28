import { useState, useEffect } from 'react'
import { Database, RefreshCw, AlertTriangle, CheckCircle, Clock } from 'lucide-react'

interface SyncTable {
  sourceTable: string
  stagingTable: string
  lastSyncOn: string | null
  totalRowsSynced: number
}

interface SyncError {
  sourceTable: string
  errorCount: number
  lastError: string | null
  lastErrorOn: string
}

interface SourceModule {
  module: string
  tableCount: number
  lastSynced: string | null
  totalRowsSynced: number
}

export function DataMartStatusPage() {
  const [tables, setTables] = useState<SyncTable[]>([])
  const [errors, setErrors] = useState<SyncError[]>([])
  const [modules, setModules] = useState<SourceModule[]>([])
  const [avgSyncMs, setAvgSyncMs] = useState(0)
  const [loading, setLoading] = useState(true)
  const [syncing, setSyncing] = useState(false)

  const fetchStatus = async () => {
    setLoading(true)
    try {
      const response = await fetch('/api/v1/reporting/catalog/sync-status')
      const data = await response.json()
      const result = data.data
      setTables(result.tables || [])
      setErrors(result.syncErrors || [])
      setModules(result.sourceModules || [])
      setAvgSyncMs(result.avgSyncDurationMs || 0)
    } catch (err) {
      console.error('Failed to fetch sync status:', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchStatus() }, [])

  const triggerSync = async (module?: string) => {
    setSyncing(true)
    try {
      const url = module ? `/api/v1/reporting/cdc/sync?module=${module}` : '/api/v1/reporting/cdc/sync'
      await fetch(url, { method: 'POST' })
      setTimeout(fetchStatus, 2000)
    } catch (err) {
      console.error('Failed to trigger sync:', err)
    } finally {
      setSyncing(false)
    }
  }

  const formatTime = (iso: string | null) => {
    if (!iso) return 'Never'
    return new Date(iso).toLocaleString()
  }

  const getStaleness = (lastSyncOn: string | null) => {
    if (!lastSyncOn) return { label: 'Never synced', color: 'text-red-500' }
    const hours = (Date.now() - new Date(lastSyncOn).getTime()) / 3600000
    if (hours < 1) return { label: `${Math.round(hours * 60)}m ago`, color: 'text-green-500' }
    if (hours < 4) return { label: `${Math.round(hours)}h ago`, color: 'text-amber-500' }
    return { label: `${Math.round(hours)}h ago`, color: 'text-red-500' }
  }

  if (loading) return <div className="text-center py-12 text-gray-500">Loading sync status...</div>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Data Mart Sync Status</h1>
          <p className="text-gray-500 mt-1">Monitor CDC/ETL pipeline health and table freshness</p>
        </div>
        <button
          onClick={() => triggerSync()}
          disabled={syncing}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 flex items-center gap-2 disabled:opacity-50"
        >
          <RefreshCw size={16} className={syncing ? 'animate-spin' : ''} />
          {syncing ? 'Syncing...' : 'Trigger Full Sync'}
        </button>
      </div>

      {/* Module Overview */}
      <div className="grid grid-cols-4 gap-4">
        {modules.map(m => {
          const staleness = getStaleness(m.lastSynced)
          return (
            <div key={m.module} className="bg-white dark:bg-gray-900 rounded-xl border p-4">
              <div className="flex items-center gap-2 mb-2">
                <Database size={16} className="text-blue-500" />
                <span className="font-semibold text-sm uppercase">{m.module}</span>
              </div>
              <div className="text-xs text-gray-500 space-y-1">
                <p>{m.tableCount} tables</p>
                <p>{m.totalRowsSynced.toLocaleString()} rows</p>
                <p className={`flex items-center gap-1 ${staleness.color}`}>
                  <Clock size={12} /> {staleness.label}
                </p>
              </div>
              <button
                onClick={() => triggerSync(m.module)}
                disabled={syncing}
                className="mt-2 px-2 py-1 bg-gray-100 hover:bg-gray-200 rounded text-xs w-full"
              >
                Sync {m.module}
              </button>
            </div>
          )
        })}
      </div>

      {/* Average Sync Duration */}
      <div className="bg-white dark:bg-gray-900 rounded-xl border p-4 flex items-center gap-4">
        <Clock size={20} className="text-blue-500" />
        <span className="text-sm">Average Sync Duration: <strong>{Math.round(avgSyncMs)}ms</strong></span>
        <span className="text-sm text-gray-400">•</span>
        <span className="text-sm text-gray-500">{tables.length} tables tracked</span>
      </div>

      {/* Table Details */}
      <div className="bg-white dark:bg-gray-900 rounded-xl border">
        <div className="p-4 border-b">
          <h3 className="font-semibold">All Tables</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th className="text-left px-4 py-2 font-medium">Source Table</th>
                <th className="text-left px-4 py-2 font-medium">Staging Table</th>
                <th className="text-left px-4 py-2 font-medium">Last Synced</th>
                <th className="text-right px-4 py-2 font-medium">Rows Synced</th>
                <th className="text-left px-4 py-2 font-medium">Freshness</th>
              </tr>
            </thead>
            <tbody>
              {tables.map(t => {
                const staleness = getStaleness(t.lastSyncOn)
                return (
                  <tr key={t.sourceTable} className="border-t hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-4 py-2 font-mono text-xs">{t.sourceTable}</td>
                    <td className="px-4 py-2 font-mono text-xs">{t.stagingTable}</td>
                    <td className="px-4 py-2 text-gray-500">{formatTime(t.lastSyncOn)}</td>
                    <td className="px-4 py-2 text-right">{t.totalRowsSynced.toLocaleString()}</td>
                    <td className={`px-4 py-2 font-medium ${staleness.color}`}>{staleness.label}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Errors */}
      {errors.length > 0 && (
        <div className="bg-red-50 dark:bg-red-900/10 rounded-xl border border-red-200 p-4">
          <h3 className="font-semibold text-red-700 dark:text-red-400 flex items-center gap-2 mb-3">
            <AlertTriangle size={16} /> Sync Errors ({errors.length})
          </h3>
          <div className="space-y-2">
            {errors.map(e => (
              <div key={e.sourceTable} className="flex items-start gap-3 text-sm">
                <span className="font-mono text-xs text-red-600">{e.sourceTable}</span>
                <span className="text-red-500">{e.errorCount} errors</span>
                <span className="text-gray-500 text-xs">{e.lastError?.substring(0, 80)}...</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
