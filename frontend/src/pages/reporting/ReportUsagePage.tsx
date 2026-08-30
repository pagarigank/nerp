import { currentCompanyId } from '@/api/company'
import { useState, useEffect } from 'react'
import { BarChart3, TrendingUp, Clock, AlertTriangle } from 'lucide-react'

interface UsageSummary {
  totalRuns: number
  avgExecutionTimeMs: number
  failedRuns: number
  successRate: number
}

interface TopReport {
  reportDefinitionId: string
  runCount: number
  avgExecutionTime: number
  lastRun: string
}

interface ModuleUsage {
  module: string
  runCount: number
  avgExecutionTime: number
}

interface DailyTrend {
  date: string
  runCount: number
  failedCount: number
}

export function ReportUsagePage() {
  const [summary, setSummary] = useState<UsageSummary | null>(null)
  const [mostRun, setMostRun] = useState<TopReport[]>([])
  const [slowest, setSlowest] = useState<TopReport[]>([])
  const [byModule, setByModule] = useState<ModuleUsage[]>([])
  const [dailyTrend, setDailyTrend] = useState<DailyTrend[]>([])
  const [loading, setLoading] = useState(true)
  const [companyId] = useState(currentCompanyId())

  useEffect(() => {
    const fetchUsage = async () => {
      setLoading(true)
      try {
        const response = await fetch(`/api/v1/reporting/catalog/usage-report?companyId=${companyId}`)
        const data = await response.json()
        const result = data.data
        setSummary(result.summary)
        setMostRun(result.mostRunReports || [])
        setSlowest(result.slowestReports || [])
        setByModule(result.usageByModule || [])
        setDailyTrend(result.dailyTrend || [])
      } catch (err) {
        console.error('Failed to fetch usage report:', err)
      } finally {
        setLoading(false)
      }
    }
    fetchUsage()
  }, [companyId])

  if (loading) return <div className="text-center py-12 text-gray-500">Loading usage analytics...</div>

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Report Usage Analytics</h1>
        <p className="text-gray-500 mt-1">Track report usage, performance, and adoption</p>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-4 gap-4">
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          <div className="flex items-center gap-2 text-gray-500 text-sm mb-1">
            <BarChart3 size={16} /> Total Runs (30d)
          </div>
          <div className="text-2xl font-bold">{summary?.totalRuns || 0}</div>
        </div>
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          <div className="flex items-center gap-2 text-gray-500 text-sm mb-1">
            <TrendingUp size={16} /> Success Rate
          </div>
          <div className="text-2xl font-bold text-green-600">{summary?.successRate || 0}%</div>
        </div>
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          <div className="flex items-center gap-2 text-gray-500 text-sm mb-1">
            <Clock size={16} /> Avg Execution Time
          </div>
          <div className="text-2xl font-bold">{summary?.avgExecutionTimeMs || 0}ms</div>
        </div>
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          <div className="flex items-center gap-2 text-gray-500 text-sm mb-1">
            <AlertTriangle size={16} /> Failed Runs
          </div>
          <div className="text-2xl font-bold text-red-600">{summary?.failedRuns || 0}</div>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Most Run Reports */}
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          <h3 className="font-semibold mb-3">Most Run Reports</h3>
          {mostRun.length === 0 ? (
            <p className="text-gray-400 text-sm">No data yet</p>
          ) : (
            <div className="space-y-2">
              {mostRun.map((r, i) => (
                <div key={r.reportDefinitionId} className="flex items-center gap-3 text-sm">
                  <span className="w-6 text-gray-400">{i + 1}.</span>
                  <span className="flex-1 font-mono text-xs truncate">{r.reportDefinitionId.substring(0, 8)}...</span>
                  <span className="text-blue-600 font-medium">{r.runCount}x</span>
                  <span className="text-gray-400">{r.avgExecutionTime}ms</span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Slowest Reports */}
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          <h3 className="font-semibold mb-3">Slowest Reports</h3>
          {slowest.length === 0 ? (
            <p className="text-gray-400 text-sm">No data yet</p>
          ) : (
            <div className="space-y-2">
              {slowest.map((r, i) => (
                <div key={r.reportDefinitionId} className="flex items-center gap-3 text-sm">
                  <span className="w-6 text-gray-400">{i + 1}.</span>
                  <span className="flex-1 font-mono text-xs truncate">{r.reportDefinitionId.substring(0, 8)}...</span>
                  <span className="text-red-600 font-medium">{r.avgExecutionTime}ms</span>
                  <span className="text-gray-400">{r.runCount}x</span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Usage by Module */}
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          <h3 className="font-semibold mb-3">Usage by Module</h3>
          {byModule.length === 0 ? (
            <p className="text-gray-400 text-sm">No data yet</p>
          ) : (
            <div className="space-y-2">
              {byModule.map(m => (
                <div key={m.module} className="flex items-center gap-3 text-sm">
                  <span className="flex-1">{m.module}</span>
                  <span className="font-medium">{m.runCount} runs</span>
                  <span className="text-gray-400">{m.avgExecutionTime}ms avg</span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Daily Trend */}
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4">
          <h3 className="font-semibold mb-3">Daily Trend (Last 30 Days)</h3>
          {dailyTrend.length === 0 ? (
            <p className="text-gray-400 text-sm">No data yet</p>
          ) : (
            <div className="space-y-1 max-h-60 overflow-y-auto">
              {dailyTrend.map(d => (
                <div key={d.date} className="flex items-center gap-3 text-sm py-1">
                  <span className="w-24 text-gray-500">{new Date(d.date).toLocaleDateString()}</span>
                  <div className="flex-1 bg-gray-100 rounded-full h-4 overflow-hidden">
                    <div className="bg-blue-500 h-full rounded-full" style={{ width: `${Math.min(100, (d.runCount / Math.max(...dailyTrend.map(x => x.runCount), 1)) * 100)}%` }} />
                  </div>
                  <span className="w-12 text-right">{d.runCount}</span>
                  {d.failedCount > 0 && <span className="text-red-500 text-xs">({d.failedCount} fail)</span>}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
