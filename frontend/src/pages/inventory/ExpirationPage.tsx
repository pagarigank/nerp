import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { getExpirations, getExpiringSoon, getExpired } from '@api/inventory'
import type { ExpirationSummary } from '@/types/inventory'

function fmt(d: string) { return new Date(d).toLocaleDateString() }

export function ExpirationPage() {
  const [view, setView] = useState<'all' | 'soon' | 'expired'>('all')
  const { data: all = [], isLoading } = useQuery({ queryKey: ['inventory', 'expirations'], queryFn: () => getExpirations() })
  const { data: soon = [] } = useQuery({ queryKey: ['inventory', 'expirations', 'soon'], queryFn: () => getExpiringSoon() })
  const { data: expired = [] } = useQuery({ queryKey: ['inventory', 'expirations', 'expired'], queryFn: () => getExpired() })

  const rows: ExpirationSummary[] = view === 'soon' ? soon : view === 'expired' ? expired : all

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Item Expiration" description={`${all.length} record(s) — ${soon.length} expiring soon, ${expired.length} expired`}
          action={
            <div className="flex gap-1">
              {(['all', 'soon', 'expired'] as const).map(v => (
                <Button key={v} size="sm" variant={view === v ? 'primary' : 'outline'} onClick={() => setView(v)}>
                  {v === 'all' ? 'All' : v === 'soon' ? 'Expiring Soon' : 'Expired'}
                </Button>
              ))}
            </div>
          } />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No records.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Item</th><th className="px-3 py-2 font-medium text-gray-500">Expiration</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Qty</th><th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.itemId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{fmt(r.expirationDate)}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.quantity}</td>
                      <td className="px-3 py-3"><Badge variant={r.status === 'Expired' ? 'error' : 'warning'} size="sm" dot>{r.status}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
