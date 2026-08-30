import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { DataTable } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import { getSalesOrders, configureQuote, sendQuote, acceptQuote, rejectQuote, reviseQuote, convertQuote } from '@api/orderManagement'
import type { SalesOrderSummary } from '@/types/orderManagement'

export function QuotesPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<SalesOrderSummary[]>([])
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState<string | null>(null)
  const [exp, setExp] = useState('')

  const navigate = useNavigate()

  async function load() {
    setLoading(true)
    setError(null)
    try {
      setData(await getSalesOrders())
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function run(action: (id: string) => Promise<unknown>, id: string, okMsg: string) {
    setBusy(id + okMsg)
    try {
      await action(id)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setBusy(null)
    }
  }

  const columns = [
    { key: 'orderNumber', header: 'Order #' },
    { key: 'status', header: 'Status' },
    { key: 'orderDate', header: 'Date' },
    {
      key: 'actions',
      header: 'Quote Actions',
      render: (_: unknown, row: SalesOrderSummary) => (
        <div className="flex flex-wrap gap-2">
          <Button size="sm" variant="outline" disabled={busy !== null} onClick={() => run((id) => configureQuote(id, exp || undefined), row.id, 'cfg')}>Configure Quote</Button>
          <Button size="sm" variant="outline" disabled={busy !== null} onClick={() => run(sendQuote, row.id, 'send')}>Send</Button>
          <Button size="sm" variant="success" disabled={busy !== null} onClick={() => run(acceptQuote, row.id, 'accept')}>Accept</Button>
          <Button size="sm" variant="destructive" disabled={busy !== null} onClick={() => run(rejectQuote, row.id, 'reject')}>Reject</Button>
          <Button size="sm" variant="outline" disabled={busy !== null} onClick={() => run(reviseQuote, row.id, 'revise')}>Revise</Button>
          <Button size="sm" variant="primary" disabled={busy !== null} onClick={() => run((id) => convertQuote(id, `O${Date.now().toString().slice(-6)}`), row.id, 'conv')}>Convert</Button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Quotes &amp; Quote-to-Order (582)</h1>
        <div className="flex items-center gap-2">
          <Button size="sm" variant="primary" disabled={busy !== null} onClick={() => navigate('/om/quotes/new')} leftIcon={<Plus className="h-4 w-4" />}>New Quote</Button>
          <Input type="date" value={exp} onChange={(e) => setExp(e.target.value)} className="w-44" placeholder="Expiry (optional)" />
        </div>
      </div>
      <Card>
        <DataTable columns={columns} data={data} loading={loading} emptyMessage="No sales orders" />
      </Card>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
