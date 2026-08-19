import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { getWarehouses } from '@api/inventory'
import { getCycleCounts, scheduleCycleCount } from '@api/inventory'
import type { CycleCountSummary } from '@/types/inventory'

export function CycleCountSchedulePage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [whId, setWhId] = useState('')
  const [freq, setFreq] = useState('1')
  const [abc, setAbc] = useState('U')

  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'wh-mini'], queryFn: () => getWarehouses() })
  const whOptions = [{ value: '', label: 'All warehouses' }, ...warehouses.map(w => ({ value: w.id, label: w.warehouseName ?? w.warehouseCode }))]

  const schedule = useMutation({
    mutationFn: () => scheduleCycleCount({ companyId: '', warehouseId: whId || null, frequencyMonths: Number(freq), abcClass: abc, countDate: null }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'cycle-counts'] }); },
    onError: e => setErr(getErrorMessage(e)),
  })

  // Refresh the list of sheets after scheduling
  const { data: sheets = [], isLoading } = useQuery({ queryKey: ['inventory', 'cycle-counts'], queryFn: () => getCycleCounts() })

  const recent = sheets.slice(0, 10)

  return (
    <div className="space-y-6">
      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}

      <Card>
        <CardHeader title="Schedule Cycle Counts by ABC" description="A-items monthly, B-items quarterly, C-items annually. Generates draft count sheets pre-filled with system quantities." />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
            <Select options={whOptions} value={whId} onChange={e => setWhId((e.target as HTMLSelectElement).value)} />
            <Select options={[{ value: '1', label: 'Monthly (A)' }, { value: '3', label: 'Quarterly (B)' }, { value: '12', label: 'Annual (C)' }]} value={freq} onChange={e => setFreq((e.target as HTMLSelectElement).value)} />
            <Select options={[{ value: 'U', label: 'Unclassified' }, { value: 'A', label: 'A' }, { value: 'B', label: 'B' }, { value: 'C', label: 'C' }]} value={abc} onChange={e => setAbc((e.target as HTMLSelectElement).value)} />
            <Button onClick={() => schedule.mutate()} disabled={schedule.isPending}>{schedule.isPending ? 'Scheduling…' : 'Schedule'}</Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Recent Cycle Count Sheets" description={`${recent.length} sheet(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            recent.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No cycle count sheets.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Count #</th><th className="px-3 py-2 font-medium text-gray-500">Date</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {recent.map((s: CycleCountSummary) => (
                    <tr key={s.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{s.countNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{s.countDate?.slice(0, 10)}</td>
                      <td className="px-3 py-3"><Badge variant={s.status === 'Draft' ? 'warning' : 'neutral'} size="sm" dot>{s.status}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
