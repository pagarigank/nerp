import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  getWebhookSubscriptions,
  getWebhookEventTypes,
  createWebhookSubscription,
  testWebhook,
  deleteWebhookSubscription,
  getWebhookDeliveryLogs,
} from '@/api/integration'

function WebhookForm({ onSave, onCancel }: { onSave: (data: any) => void; onCancel: () => void }) {
  const [eventTypes, setEventTypes] = useState<string[]>([])
  const [form, setForm] = useState({
    companyId: '11111111-1111-1111-1111-111111111111',
    eventType: '',
    targetUrl: '',
    secret: '',
    payloadFormat: 'json',
    description: '',
    maxRetries: 3,
    retryDelaySeconds: 30,
  })

  useEffect(() => { getWebhookEventTypes().then(setEventTypes).catch(() => {}) }, [])

  return (
    <Card>
      <CardHeader><CardTitle>New Webhook Subscription</CardTitle></CardHeader>
      <CardContent className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="text-sm font-medium">Event Type *</label>
            <select className="w-full border rounded px-3 py-2 text-sm" value={form.eventType} onChange={e => setForm({ ...form, eventType: e.target.value })}>
              <option value="">Select event...</option>
              {eventTypes.map(et => <option key={et} value={et}>{et}</option>)}
            </select>
          </div>
          <div>
            <label className="text-sm font-medium">Target URL *</label>
            <input className="w-full border rounded px-3 py-2 text-sm" value={form.targetUrl} onChange={e => setForm({ ...form, targetUrl: e.target.value })} placeholder="https://..." />
          </div>
          <div>
            <label className="text-sm font-medium">Shared Secret (HMAC)</label>
            <input className="w-full border rounded px-3 py-2 text-sm" type="password" value={form.secret} onChange={e => setForm({ ...form, secret: e.target.value })} />
          </div>
          <div>
            <label className="text-sm font-medium">Payload Format</label>
            <select className="w-full border rounded px-3 py-2 text-sm" value={form.payloadFormat} onChange={e => setForm({ ...form, payloadFormat: e.target.value })}>
              <option value="json">JSON</option>
              <option value="xml">XML</option>
            </select>
          </div>
          <div className="col-span-2">
            <label className="text-sm font-medium">Description</label>
            <input className="w-full border rounded px-3 py-2 text-sm" value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} />
          </div>
        </div>
        <div className="flex gap-2 pt-2">
          <Button size="sm" onClick={() => onSave(form)} disabled={!form.eventType || !form.targetUrl}>Create</Button>
          <Button size="sm" variant="outline" onClick={onCancel}>Cancel</Button>
        </div>
      </CardContent>
    </Card>
  )
}

export default function WebhooksPage() {
  const [webhooks, setWebhooks] = useState<any[]>([])
  const [logs, setLogs] = useState<any[]>([])
  const [showForm, setShowForm] = useState(false)
  const [loading, setLoading] = useState(true)

  const load = () => {
    Promise.all([
      getWebhookSubscriptions().catch(() => []),
      getWebhookDeliveryLogs({ pageSize: 20 }).catch(() => []),
    ]).then(([w, l]) => { setWebhooks(w); setLogs(l); setLoading(false) })
  }

  useEffect(() => { load() }, [])

  const handleCreate = async (data: any) => {
    await createWebhookSubscription(data)
    setShowForm(false)
    load()
  }

  const handleTest = async (id: string) => {
    await testWebhook(id)
    alert('Test webhook queued for delivery.')
  }

  const handleDelete = async (id: string) => {
    if (confirm('Delete this webhook subscription?')) {
      await deleteWebhookSubscription(id)
      load()
    }
  }

  if (loading) return <div className="p-8 text-gray-500">Loading...</div>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Webhook Subscriptions</h1>
        <Button onClick={() => setShowForm(true)}>+ New Webhook</Button>
      </div>

      {showForm && <WebhookForm onSave={handleCreate} onCancel={() => setShowForm(false)} />}

      <Card>
        <CardContent className="p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="text-left p-3">Event</th>
                <th className="text-left p-3">Target URL</th>
                <th className="text-center p-3">Retries</th>
                <th className="text-center p-3">Status</th>
                <th className="text-right p-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {webhooks.map((w: any) => (
                <tr key={w.id} className="border-b hover:bg-gray-50">
                  <td className="p-3 font-mono text-xs">{w.eventType}</td>
                  <td className="p-3 text-xs text-gray-500 truncate max-w-[250px]">{w.targetUrl}</td>
                  <td className="p-3 text-center">{w.maxRetries}</td>
                  <td className="p-3 text-center">
                    <Badge className={w.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}>
                      {w.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </td>
                  <td className="p-3 text-right space-x-2">
                    <Button size="sm" variant="outline" onClick={() => handleTest(w.id)}>Test</Button>
                    <Button size="sm" variant="destructive" onClick={() => handleDelete(w.id)}>Delete</Button>
                  </td>
                </tr>
              ))}
              {webhooks.length === 0 && (
                <tr><td colSpan={5} className="p-8 text-center text-gray-500">No webhook subscriptions configured.</td></tr>
              )}
            </tbody>
          </table>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Recent Delivery Logs</CardTitle></CardHeader>
        <CardContent>
          {logs.length === 0 ? (
            <p className="text-sm text-gray-500">No delivery logs yet.</p>
          ) : (
            <div className="overflow-x-auto max-h-64 overflow-y-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left py-2">Event</th>
                    <th className="text-center py-2">Attempt</th>
                    <th className="text-center py-2">HTTP</th>
                    <th className="text-center py-2">Status</th>
                    <th className="text-right py-2">Duration</th>
                  </tr>
                </thead>
                <tbody>
                  {logs.map((l: any) => (
                    <tr key={l.id} className="border-b hover:bg-gray-50">
                      <td className="py-2 font-mono text-xs">{l.eventType}</td>
                      <td className="py-2 text-center">{l.attemptNumber}</td>
                      <td className="py-2 text-center">{l.httpStatusCode || '-'}</td>
                      <td className="py-2 text-center">
                        <Badge className={l.status === 'Success' ? 'bg-green-100 text-green-800' : l.status === 'Failed' ? 'bg-red-100 text-red-800' : 'bg-yellow-100 text-yellow-800'}>
                          {l.status}
                        </Badge>
                      </td>
                      <td className="py-2 text-right text-gray-500">{l.durationMs ? `${l.durationMs}ms` : '-'}</td>
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
