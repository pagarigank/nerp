import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { flagStaleChecks, getEscheatment, reportEscheatment } from '@api/ap'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { StaleCheckEscheatmentDto } from '@/types/ap'

export function EscheatmentPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [statutoryDays, setStatutoryDays] = useState('180')
  const [err, setErr] = useState<string | null>(null)
  const [msg, setMsg] = useState<string | null>(null)

  const { data: items = [], isLoading } = useQuery({ queryKey: ['escheat', companyId], queryFn: () => getEscheatment(companyId), enabled: !!companyId })

  const flag = useMutation({
    mutationFn: () => flagStaleChecks({ companyId, statutoryDays: Number(statutoryDays) }),
    onSuccess: (r) => { setMsg(`Flagged ${r.length} stale check(s).`); setErr(null); queryClient.invalidateQueries({ queryKey: ['escheat'] }) },
    onError: (e) => { setErr(getErrorMessage(e)); setMsg(null) },
  })

  const report = useMutation({
    mutationFn: (id: string) => reportEscheatment(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['escheat'] }),
    onError: (e) => setErr(getErrorMessage(e)),
  })

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Unclaimed Property / Stale-Check Escheatment</h1>
      <Card>
        <CardHeader title="Run Stale-Check Scan" />
        <CardContent className="space-y-3">
          {err && <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-300">{err}</div>}
          {msg && <div className="rounded-md bg-green-50 p-3 text-sm text-green-700 dark:bg-green-900/30 dark:text-green-300">{msg}</div>}
          <input className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-800" placeholder="Statutory days (e.g. 180)" value={statutoryDays} onChange={(e) => setStatutoryDays(e.target.value)} />
          <Button disabled={flag.isPending} onClick={() => flag.mutate()}>{flag.isPending ? 'Scanning…' : 'Flag Stale Checks'}</Button>
        </CardContent>
      </Card>
      <Card>
        <CardHeader title="Escheatment Register" />
        <CardContent>
          {isLoading ? <div className="text-sm text-gray-500">Loading…</div> : (
            <table className="w-full text-left text-sm">
              <thead><tr><th>Payment</th><th>Amount</th><th>Issued</th><th>Status</th><th>Action</th></tr></thead>
              <tbody>
                {items.map((e: StaleCheckEscheatmentDto) => (
                  <tr key={e.id} className="border-t border-gray-200 dark:border-gray-700">
                    <td>{e.paymentId.slice(0, 8)}</td><td>{e.amount.toLocaleString()}</td><td>{new Date(e.issuedDate).toLocaleDateString()}</td>
                    <td><Badge variant={e.status === 'Reported' ? 'info' : 'warning'}>{e.status}</Badge></td>
                    <td>{e.status === 'Flagged' && <Button variant="outline" disabled={report.isPending} onClick={() => report.mutate(e.id)}>Report to State</Button>}</td>
                  </tr>
                ))}
                {items.length === 0 && <tr><td colSpan={5} className="text-gray-500">No escheatment items.</td></tr>}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
