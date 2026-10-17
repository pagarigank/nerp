import { useState, useEffect, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Calendar,
  Clock,
  MapPin,
  Phone,
  User,
  RefreshCw,
  UploadCloud,
  CheckCircle2,
  AlertCircle,
  Wifi,
  WifiOff,
  Database,
  ArrowRight,
  Route as RouteIcon,
} from 'lucide-react'
import { useAuthStore } from '@stores/authStore'
import {
  getSalespersonRoutes,
  getOfflineSyncPackage,
} from '@/api/salesperson'
import { getSalesReps } from '@/api/orderManagement'
import type { SalesRepSummary } from '@/types/orderManagement'
import { sqliteService, type RouteRecord, type DatabaseStats } from '@/utils/sqliteDb'

const DAYS_OF_WEEK = [
  { id: 1, name: 'Monday', short: 'Mon' },
  { id: 2, name: 'Tuesday', short: 'Tue' },
  { id: 3, name: 'Wednesday', short: 'Wed' },
  { id: 4, name: 'Thursday', short: 'Thu' },
  { id: 5, name: 'Friday', short: 'Fri' },
  { id: 6, name: 'Saturday', short: 'Sat' },
  { id: 0, name: 'Sunday', short: 'Sun' },
]

export function SalespersonPortalPage() {
  const navigate = useNavigate()
  const currentCompany = useAuthStore((s) => s.currentCompany)
  const currentUser = useAuthStore((s) => s.user)

  // Current day of week (0=Sunday, 1=Monday, etc.)
  const todayDayOfWeek = new Date().getDay()
  const [selectedDay, setSelectedDay] = useState<number>(todayDayOfWeek)
  const [selectedRepId, setSelectedRepId] = useState<string>(() => {
    return localStorage.getItem('nerp_selected_salesrep_id') || ''
  })
  const [isSimulatedOffline, setIsSimulatedOffline] = useState(false)
  const [isOnline, setIsOnline] = useState(navigator.onLine)
  const [dbStats, setDbStats] = useState<DatabaseStats | null>(null)
  const [syncStatusMsg, setSyncStatusMsg] = useState<string | null>(null)
  const [syncErrorMsg, setSyncErrorMsg] = useState<string | null>(null)
  const [isSyncing, setIsSyncing] = useState(false)
  const [offlineRoutes, setOfflineRoutes] = useState<RouteRecord[]>([])

  // Network listener
  useEffect(() => {
    const handleOnline = () => setIsOnline(true)
    const handleOffline = () => setIsOnline(false)
    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)
    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [])

  const effectiveOnline = isOnline && !isSimulatedOffline

  // Save rep selection
  const handleSelectRep = (repId: string) => {
    setSelectedRepId(repId)
    localStorage.setItem('nerp_selected_salesrep_id', repId)
  }

  // Refresh SQLite stats
  const refreshDbStats = async () => {
    try {
      const stats = await sqliteService.getDatabaseStats()
      setDbStats(stats)
      const local = await sqliteService.getRoutesByDay(selectedDay)
      setOfflineRoutes(local)
    } catch {
      // ignore
    }
  }

  useEffect(() => {
    refreshDbStats()
  }, [selectedDay, selectedRepId])

  // Fetch Sales Reps
  const { data: salesReps = [] } = useQuery<SalesRepSummary[]>({
    queryKey: ['om', 'salesReps'],
    queryFn: () => getSalesReps(),
    enabled: effectiveOnline,
  })

  // Auto-select sales rep: prefer match by auth user email, else first rep
  useEffect(() => {
    if (selectedRepId || salesReps.length === 0) return
    const userEmail = currentUser?.email?.toLowerCase()
    const matched = userEmail
      ? salesReps.find((r) => r.email?.toLowerCase() === userEmail)
      : undefined
    const target = matched ?? salesReps[0]
    if (target) handleSelectRep(target.id)
  }, [salesReps, selectedRepId, currentUser])

  // Live query for routes (when online)
  const { data: liveRoutes = [], isLoading: isLoadingRoutes } = useQuery({
    queryKey: ['om', 'salespersonRoutes', currentCompany?.id, selectedRepId, selectedDay],
    queryFn: () => getSalespersonRoutes(currentCompany?.id, selectedRepId, selectedDay),
    enabled: effectiveOnline && Boolean(selectedRepId),
  })

  // Selected Sales Rep details
  const currentRep = useMemo(() => {
    return salesReps.find((r) => r.id === selectedRepId)
  }, [salesReps, selectedRepId])

  // Display routes: prefer live if online, otherwise offline SQLite records
  const routesToDisplay = useMemo(() => {
    if (effectiveOnline && liveRoutes.length > 0) {
      return liveRoutes.map((r) => ({
        id: r.id,
        salesRepId: r.salesRepId,
        customerId: r.customerId,
        customerCode: r.customerCode,
        customerName: r.customerName,
        contactName: '',
        phone: r.customerPhone || '',
        address: r.customerAddress || '',
        dayOfWeek: r.dayOfWeek,
        sequenceOrder: r.sequenceOrder,
        estimatedTime: r.estimatedTime,
        notes: r.notes,
        creditLimit: r.creditLimit,
        currentBalance: r.currentBalance,
      }))
    }
    return offlineRoutes
  }, [effectiveOnline, liveRoutes, offlineRoutes])

  // Sync Live Data into SQLite
  const handleSyncToSqlite = async () => {
    if (!selectedRepId || !effectiveOnline) return
    setIsSyncing(true)
    setSyncStatusMsg('Downloading catalog and route assignments...')
    setSyncErrorMsg(null)
    try {
      const pkg = await getOfflineSyncPackage(selectedRepId, currentCompany?.id)
      await sqliteService.importSyncPackage({
        serverTime: pkg.serverTime,
        routes: pkg.routes,
        products: pkg.products,
        pricingRules: pkg.pricingRules,
      })
      await refreshDbStats()
      setSyncStatusMsg(`Successfully synced ${pkg.routes.length} routes and ${pkg.products.length} products to SQLite!`)
      setTimeout(() => setSyncStatusMsg(null), 4000)
    } catch (err: unknown) {
      setSyncErrorMsg(err instanceof Error ? err.message : 'Sync failed.')
    } finally {
      setIsSyncing(false)
    }
  }

  // Upload of offline calls is a selective action (day filter + per-call checkboxes),
  // which lives in the SQLite Sync Center rather than this one-tap portal.
  const handleGoToSyncCenter = () => navigate('/om/salesperson/sync')

  return (
    <div className="space-y-6">
      {/* Top Banner */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 shadow-sm">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-emerald-600 to-teal-500 flex items-center justify-center text-white shadow-md shadow-emerald-500/20">
              <User className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-xl font-bold text-slate-900 dark:text-white tracking-tight">
                  Salesperson Daily Visit Portal
                </h1>
                <span
                  className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                    effectiveOnline
                      ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800'
                      : 'bg-amber-50 text-amber-700 dark:bg-amber-950/50 dark:text-amber-400 border border-amber-200 dark:border-amber-800'
                  }`}
                >
                  {effectiveOnline ? (
                    <>
                      <Wifi className="w-3.5 h-3.5" /> Online Mode
                    </>
                  ) : (
                    <>
                      <WifiOff className="w-3.5 h-3.5" /> Offline SQLite
                    </>
                  )}
                </span>
              </div>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                Daily route execution, product catalogue pricing, and offline-first sales order calls
              </p>
            </div>
          </div>

          {/* Action buttons & SQLite state */}
          <div className="flex flex-wrap items-center gap-2.5">
            {/* Rep Selector */}
            <div className="flex items-center gap-1.5 bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-lg px-2.5 py-1.5">
              <User className="w-4 h-4 text-slate-400" />
              <select
                aria-label="Active Sales Rep"
                className="bg-transparent text-sm font-medium text-slate-800 dark:text-slate-200 outline-none cursor-pointer"
                value={selectedRepId}
                onChange={(e) => handleSelectRep(e.target.value)}
              >
                {salesReps.length > 0 ? (
                  salesReps.map((r) => (
                    <option key={r.id} value={r.id} className="dark:bg-slate-900">
                      {r.name} ({r.code})
                    </option>
                  ))
                ) : (
                  <option value={selectedRepId}>
                    {currentRep?.name || 'Local Sales Rep'}
                  </option>
                )}
              </select>
            </div>

            {/* Simulated Offline Toggle */}
            <button
              onClick={() => setIsSimulatedOffline(!isSimulatedOffline)}
              className={`px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors flex items-center gap-1.5 ${
                isSimulatedOffline
                  ? 'bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border-amber-300 dark:border-amber-700'
                  : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 border-slate-200 dark:border-slate-700 hover:bg-slate-200'
              }`}
              title="Toggle to test offline SQLite behavior"
            >
              {isSimulatedOffline ? <WifiOff className="w-3.5 h-3.5" /> : <Wifi className="w-3.5 h-3.5" />}
              {isSimulatedOffline ? 'Simulating Offline' : 'Simulate Offline'}
            </button>

            {/* Sync to SQLite Button */}
            <button
              onClick={handleSyncToSqlite}
              disabled={isSyncing || !effectiveOnline}
              className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-slate-900 hover:bg-slate-800 dark:bg-slate-100 dark:hover:bg-white text-white dark:text-slate-900 text-xs font-semibold rounded-lg shadow-sm transition-all disabled:opacity-50"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${isSyncing ? 'animate-spin' : ''}`} />
              {isSyncing ? 'Syncing...' : 'Sync to SQLite'}
            </button>

            {/* Upload Unsynced Calls → selective sync lives in the Sync Center */}
            {dbStats && dbStats.unsyncedCallsCount > 0 && (
              <button
                onClick={handleGoToSyncCenter}
                className="inline-flex items-center gap-1.5 px-3.5 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold rounded-lg shadow-sm transition-all animate-pulse"
                title="Select which offline calls to sync (by day) in the Sync Center"
              >
                <UploadCloud className="w-3.5 h-3.5" />
                Sync {dbStats.unsyncedCallsCount} Pending Order{dbStats.unsyncedCallsCount > 1 ? 's' : ''}
              </button>
            )}
          </div>
        </div>

        {/* Sync message banners */}
        {syncStatusMsg && (
          <div className="mt-4 p-3 bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 text-emerald-800 dark:text-emerald-300 text-xs rounded-lg flex items-center gap-2">
            <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-600" />
            <span>{syncStatusMsg}</span>
          </div>
        )}
        {syncErrorMsg && (
          <div className="mt-4 p-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-800 text-red-800 dark:text-red-300 text-xs rounded-lg flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0 text-red-600" />
            <span>{syncErrorMsg}</span>
          </div>
        )}

        {/* SQLite Diagnostic Strip */}
        <div className="mt-4 pt-3 border-t border-slate-100 dark:border-slate-800 flex flex-wrap items-center justify-between gap-4 text-xs text-slate-500 dark:text-slate-400">
          <div className="flex items-center gap-4">
            <span className="flex items-center gap-1">
              <Database className="w-3.5 h-3.5 text-slate-400" />
              SQLite DB: <strong className="text-slate-700 dark:text-slate-300">{dbStats?.databaseSizeKb || 0} KB</strong>
            </span>
            <span>
              Products:{' '}
              <strong className="text-slate-700 dark:text-slate-300">{dbStats?.productsCount || 0}</strong>
            </span>
            <span>
              Saved Routes:{' '}
              <strong className="text-slate-700 dark:text-slate-300">{dbStats?.routesCount || 0}</strong>
            </span>
            <span>
              Pending Sync:{' '}
              <strong className={dbStats?.unsyncedCallsCount ? 'text-amber-600 font-bold' : 'text-slate-700 dark:text-slate-300'}>
                {dbStats?.unsyncedCallsCount || 0}
              </strong>
            </span>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => navigate('/om/salesperson/sync')}
              className="text-indigo-600 dark:text-indigo-400 hover:underline font-medium"
            >
              SQLite Sync Center →
            </button>
            <span className="text-slate-300 dark:text-slate-700">|</span>
            <button
              onClick={() => navigate('/om/salesperson/routes')}
              className="text-indigo-600 dark:text-indigo-400 hover:underline font-medium"
            >
              Manage Weekly Routes →
            </button>
          </div>
        </div>
      </div>

      {/* Days of Week Navigation Bar */}
      <div className="flex overflow-x-auto gap-2 p-1.5 bg-slate-100 dark:bg-slate-800/80 rounded-xl">
        {DAYS_OF_WEEK.map((day) => {
          const isSelected = selectedDay === day.id
          const isToday = todayDayOfWeek === day.id

          return (
            <button
              key={day.id}
              onClick={() => setSelectedDay(day.id)}
              className={`flex-1 min-w-[100px] py-2.5 px-3 rounded-lg text-sm font-semibold transition-all flex flex-col items-center justify-center gap-0.5 ${
                isSelected
                  ? 'bg-white dark:bg-slate-900 text-indigo-600 dark:text-indigo-400 shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:bg-white/60 dark:hover:bg-slate-800 hover:text-slate-900'
              }`}
            >
              <div className="flex items-center gap-1.5">
                <span>{day.name}</span>
                {isToday && (
                  <span className="w-2 h-2 rounded-full bg-emerald-500 ring-2 ring-emerald-500/20" />
                )}
              </div>
              <span className="text-[10px] uppercase tracking-wider font-medium opacity-60">
                {isToday ? 'Today' : day.short}
              </span>
            </button>
          )
        })}
      </div>

      {/* Daily Visit Route Stops List */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <RouteIcon className="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
            <h2 className="text-base font-bold text-slate-900 dark:text-white">
              {DAYS_OF_WEEK.find((d) => d.id === selectedDay)?.name}&apos;s Scheduled Customer Visits
            </h2>
            <span className="px-2 py-0.5 rounded-full text-xs font-semibold bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400">
              {routesToDisplay.length} Customer{routesToDisplay.length === 1 ? '' : 's'}
            </span>
          </div>

          <button
            onClick={() => navigate('/om/salesperson/routes')}
            className="text-xs text-indigo-600 dark:text-indigo-400 font-medium hover:underline flex items-center gap-1"
          >
            + Assign Customer to Day
          </button>
        </div>

        {isLoadingRoutes && effectiveOnline ? (
          <div className="p-12 text-center bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
            <RefreshCw className="w-6 h-6 animate-spin mx-auto text-indigo-600 mb-2" />
            <p className="text-sm text-slate-500">Loading today&apos;s visit routes...</p>
          </div>
        ) : routesToDisplay.length === 0 ? (
          <div className="p-12 text-center bg-white dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-800">
            <Calendar className="w-10 h-10 mx-auto text-slate-300 dark:text-slate-600 mb-3" />
            <h3 className="text-base font-bold text-slate-800 dark:text-slate-200 mb-1">
              No Customers Assigned for {DAYS_OF_WEEK.find((d) => d.id === selectedDay)?.name}
            </h3>
            <p className="text-sm text-slate-500 dark:text-slate-400 max-w-md mx-auto mb-4">
              There are no customer visits scheduled for this day of the week. You can assign customers
              or sync your offline database.
            </p>
            <div className="flex justify-center gap-3">
              <button
                onClick={() => navigate('/om/salesperson/routes')}
                className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-semibold rounded-lg shadow-sm"
              >
                Assign Customer Routes
              </button>
              {effectiveOnline && (
                <button
                  onClick={handleSyncToSqlite}
                  className="px-4 py-2 border border-slate-300 dark:border-slate-700 text-slate-700 dark:text-slate-300 text-xs font-semibold rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800"
                >
                  Download SQLite Package
                </button>
              )}
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {routesToDisplay.map((stop, idx) => {
              const hasBalance = (stop.currentBalance || 0) > 0
              const isOverLimit = (stop.creditLimit || 0) > 0 && (stop.currentBalance || 0) >= (stop.creditLimit || 0)

              return (
                <div
                  key={stop.id || idx}
                  className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 shadow-sm hover:border-indigo-300 dark:hover:border-indigo-700 transition-all flex flex-col justify-between"
                >
                  <div>
                    {/* Stop Header */}
                    <div className="flex items-start justify-between gap-2 mb-3">
                      <div className="flex items-center gap-2">
                        <span className="w-7 h-7 rounded-lg bg-indigo-50 dark:bg-indigo-950/70 text-indigo-700 dark:text-indigo-300 font-bold text-xs flex items-center justify-center border border-indigo-200 dark:border-indigo-800">
                          #{stop.sequenceOrder || idx + 1}
                        </span>
                        <div>
                          <span className="text-[11px] font-mono text-slate-400 dark:text-slate-500">
                            {stop.customerCode}
                          </span>
                          <h3 className="text-base font-bold text-slate-900 dark:text-white leading-tight">
                            {stop.customerName}
                          </h3>
                        </div>
                      </div>

                      {stop.estimatedTime && (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 shrink-0">
                          <Clock className="w-3 h-3 text-slate-400" />
                          {stop.estimatedTime}
                        </span>
                      )}
                    </div>

                    {/* Address & Contact */}
                    <div className="space-y-1.5 text-xs text-slate-600 dark:text-slate-400 mb-4">
                      {stop.address && (
                        <div className="flex items-start gap-1.5">
                          <MapPin className="w-3.5 h-3.5 text-slate-400 shrink-0 mt-0.5" />
                          <span className="line-clamp-2">{stop.address}</span>
                        </div>
                      )}
                      {stop.phone && (
                        <div className="flex items-center gap-1.5">
                          <Phone className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                          <a
                            href={`tel:${stop.phone}`}
                            className="text-indigo-600 dark:text-indigo-400 hover:underline"
                          >
                            {stop.phone}
                          </a>
                        </div>
                      )}
                    </div>

                    {/* Route Special Instructions */}
                    {stop.notes && (
                      <div className="p-2.5 rounded-lg bg-slate-50 dark:bg-slate-800/50 border border-slate-100 dark:border-slate-800 text-xs text-slate-600 dark:text-slate-300 mb-4">
                        <span className="font-semibold text-slate-800 dark:text-slate-200">Notes: </span>
                        {stop.notes}
                      </div>
                    )}

                    {/* Credit Status */}
                    <div className="pt-3 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between text-xs mb-4">
                      <span className="text-slate-500">AR Balance / Limit:</span>
                      <div className="text-right">
                        <span
                          className={`font-semibold tabular-nums ${
                            isOverLimit
                              ? 'text-red-600 dark:text-red-400'
                              : hasBalance
                              ? 'text-amber-600 dark:text-amber-400'
                              : 'text-slate-700 dark:text-slate-300'
                          }`}
                        >
                          ${(stop.currentBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}
                        </span>
                        <span className="text-slate-400"> / ${(stop.creditLimit || 0).toLocaleString('en-US')}</span>
                      </div>
                    </div>
                  </div>

                  {/* Primary CTA: Start Sales Order Call */}
                  <button
                    onClick={() =>
                      navigate(`/om/salesperson/call/${stop.customerId}?repId=${selectedRepId}&routeId=${stop.id}`)
                    }
                    className="w-full py-2.5 px-4 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white rounded-lg font-semibold text-xs shadow-md shadow-emerald-600/20 flex items-center justify-center gap-2 transition-all hover:gap-3"
                  >
                    <span>Start Sales Order Call</span>
                    <ArrowRight className="w-4 h-4" />
                  </button>
                </div>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}
