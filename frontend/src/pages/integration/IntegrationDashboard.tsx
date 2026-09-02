import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  getIntegrationHealth,
  getTradingPartnerPerformance,
  getEdiTransactionLog,
  getWebhookDeliveryLogs,
} from '@/api/integration'
import { PdfPreviewModal } from '@/components/PdfPreviewModal'

function StatCard({ title, value, subtitle, color = 'text-gray-900' }: any) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="text-sm text-gray-500">{title}</div>
        <div className={`text-2xl font-bold ${color}`}>{value}</div>
        {subtitle && <div className="text-xs text-gray-400 mt-1">{subtitle}</div>}
      </CardContent>
    </Card>
  )
}

function StatusBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    Success: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800',
    Pending: 'bg-yellow-100 text-yellow-800',
    Received: 'bg-blue-100 text-blue-800',
    Processed: 'bg-green-100 text-green-800',
    Acknowledged: 'bg-green-100 text-green-800',
    PendingRetry: 'bg-orange-100 text-orange-800',
  }
  return <Badge className={colors[status] || 'bg-gray-100 text-gray-800'}>{status}</Badge>
}

export default function IntegrationDashboard() {
  const [health, setHealth] = useState<any>(null)
  const [partners, setPartners] = useState<any[]>([])
  const [ediLogs, setEdiLogs] = useState<any[]>([])
  const [webhookLogs, setWebhookLogs] = useState<any[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([
      getIntegrationHealth().catch(() => null),
      getTradingPartnerPerformance(undefined, 30).catch(() => []),
      getEdiTransactionLog({ pageSize: 10 }).catch(() => []),
      getWebhookDeliveryLogs({ pageSize: 10 }).catch(() => []),
    ]).then(([h, p, e, w]) => {
      setHealth(h)
      setPartners(p)
      setEdiLogs(e)
      setWebhookLogs(w)
      setLoading(false)
    })
  }, [])

  if (loading) return <div className="p-8 text-gray-500">Loading...</div>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Integration & EDI Dashboard</h1>
        <Button variant="outline" onClick={() => window.location.reload()}>Refresh</Button>
      </div>

      {/* Health Overview */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard title="Trading Partners" value={health?.activeTradingPartners || 0} subtitle={`${health?.totalTradingPartners || 0} total`} />
        <StatCard title="Active Webhooks" value={health?.activeWebhooks || 0} subtitle={`${health?.totalWebhooks || 0} total`} />
        <StatCard title="EDI (24h)" value={health?.ediProcessed24h || 0} subtitle={`${health?.ediFailed24h || 0} failed`} color={health?.ediFailed24h > 0 ? 'text-red-600' : 'text-green-600'} />
        <StatCard title="Webhook Deliveries (24h)" value={health?.webhookDeliveries24h || 0} subtitle={`${health?.webhookFailed24h || 0} failed`} color={health?.webhookFailed24h > 0 ? 'text-red-600' : 'text-green-600'} />
      </div>

      {/* Alerts */}
      {(health?.pendingAcks > 0 || health?.expiredCertificates > 0) && (
        <Card className="border-orange-200 bg-orange-50">
          <CardContent className="p-4">
            <div className="font-semibold text-orange-800">⚠ Active Alerts</div>
            <div className="text-sm text-orange-700 mt-1 space-y-1">
              {health?.pendingAcks > 0 && <div>• {health.pendingAcks} pending EDI acknowledgments (997/999)</div>}
              {health?.expiredCertificates > 0 && <div>• {health.expiredCertificates} trading partner certificates expired</div>}
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Partner Performance */}
        <Card>
          <CardHeader>
            <CardTitle>Trading Partner Performance (30 days)</CardTitle>
          </CardHeader>
          <CardContent>
            {partners.length === 0 ? (
              <p className="text-sm text-gray-500">No active trading partners.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b">
                      <th className="text-left py-2">Partner</th>
                      <th className="text-right py-2">Transactions</th>
                      <th className="text-right py-2">Error Rate</th>
                      <th className="text-right py-2">ACK Rate</th>
                    </tr>
                  </thead>
                  <tbody>
                    {partners.map((p: any) => (
                      <tr key={p.partnerId} className="border-b hover:bg-gray-50">
                        <td className="py-2 font-medium">{p.partnerName}</td>
                        <td className="py-2 text-right">{p.totalTransactions}</td>
                        <td className="py-2 text-right">
                          <span className={p.errorRatePercent > 5 ? 'text-red-600' : 'text-green-600'}>
                            {p.errorRatePercent}%
                          </span>
                        </td>
                        <td className="py-2 text-right">{p.acknowledgmentRatePercent}%</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Recent EDI Transactions */}
        <Card>
          <CardHeader>
            <CardTitle>Recent EDI Transactions</CardTitle>
          </CardHeader>
          <CardContent>
            {ediLogs.length === 0 ? (
              <p className="text-sm text-gray-500">No recent EDI transactions.</p>
            ) : (
              <div className="space-y-2 max-h-64 overflow-y-auto">
                {ediLogs.map((log: any) => (
                  <div key={log.id} className="flex items-center justify-between text-sm border-b pb-2">
                    <div>
                      <span className="font-mono font-medium">{log.documentType}</span>
                      <span className="text-gray-500 ml-2">{log.direction}</span>
                      <span className="text-gray-400 ml-2">→ {log.partnerCode}</span>
                    </div>
                    <StatusBadge status={log.status} />
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Recent Webhook Deliveries */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Webhook Deliveries</CardTitle>
        </CardHeader>
        <CardContent>
          {webhookLogs.length === 0 ? (
            <p className="text-sm text-gray-500">No recent webhook deliveries.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2">Event</th>
                    <th className="text-left py-2">Target</th>
                    <th className="text-center py-2">Attempt</th>
                    <th className="text-center py-2">Status</th>
                    <th className="text-right py-2">Duration</th>
                  </tr>
                </thead>
                <tbody>
                  {webhookLogs.map((log: any) => (
                    <tr key={log.id} className="border-b hover:bg-gray-50">
                      <td className="py-2 font-mono text-xs">{log.eventType}</td>
                      <td className="py-2 text-xs text-gray-500 truncate max-w-[200px]">{log.targetUrl}</td>
                      <td className="py-2 text-center">{log.attemptNumber}</td>
                      <td className="py-2 text-center"><StatusBadge status={log.status} /></td>
                      <td className="py-2 text-right text-gray-500">{log.durationMs ? `${log.durationMs}ms` : '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
