import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { getWarehouses, getWarehouseBins } from '@api/inventory'
import { getPutAwayPickingRules, createPutAwayPickingRule, recommendPutAway } from '@api/inventory'
import type { PutAwayPickingRuleDto, PutAwayPickingRecommendationDto } from '@/types/inventory'

const POLICIES = ['FIFO', 'LIFO', 'FEFO', 'LowestBin', 'HighestBin']

export function PutAwayPickingPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [whId, setWhId] = useState('')
  const [binId, setBinId] = useState('')
  const [putAwayRank, setPutAwayRank] = useState('1')
  const [pickSequence, setPickSequence] = useState('1')
  const [policy, setPolicy] = useState('0')
  const [rec, setRec] = useState<PutAwayPickingRecommendationDto | null>(null)

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'putaway'], queryFn: () => getPutAwayPickingRules() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'wh-mini'], queryFn: () => getWarehouses() })
  const { data: bins = [] } = useQuery({ queryKey: ['inventory', 'bins-mini', whId], queryFn: () => getWarehouseBins(whId), enabled: !!whId })

  const create = useMutation({
    mutationFn: () => createPutAwayPickingRule({ companyId: '', warehouseId: whId, binId, putAwayRank: Number(putAwayRank), pickSequence: Number(pickSequence), pickingPolicy: Number(policy) }),
    onSuccess: () => { setBinId(''); qc.invalidateQueries({ queryKey: ['inventory', 'putaway'] }) },
    onError: e => setErr(getErrorMessage(e)),
  })
  const recommend = useMutation({
    mutationFn: ({ b, m }: { b: string; m: 'putaway' | 'pick' }) => recommendPutAway(b, m),
    onSuccess: d => setRec(d),
    onError: e => setErr(getErrorMessage(e)),
  })

  const whOptions = [{ value: '', label: 'Warehouse…' }, ...warehouses.map(w => ({ value: w.id, label: w.warehouseName ?? w.warehouseCode }))]
  const binOptions = [{ value: '', label: 'Bin…' }, ...bins.map(b => ({ value: b.id, label: b.binCode }))]

  return (
    <div className="space-y-6">
      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}

      <Card>
        <CardHeader title="New Put-away / Picking Rule" description="Rank bins for put-away and picking strategy" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-6 gap-3">
            <Select options={whOptions} value={whId} onChange={e => setWhId((e.target as HTMLSelectElement).value)} />
            <Select options={binOptions} value={binId} onChange={e => setBinId((e.target as HTMLSelectElement).value)} />
            <Input placeholder="Put-away rank" value={putAwayRank} onChange={e => setPutAwayRank(e.target.value)} />
            <Input placeholder="Pick sequence" value={pickSequence} onChange={e => setPickSequence(e.target.value)} />
            <Select options={POLICIES.map((p, i) => ({ value: String(i), label: p }))} value={policy} onChange={e => setPolicy((e.target as HTMLSelectElement).value)} />
            <Button onClick={() => create.mutate()} disabled={!whId || !binId || create.isPending}>{create.isPending ? 'Saving…' : 'Add'}</Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Bin Recommendation" description="Suggest the optimal bin for put-away or picking" />
        <CardContent>
          <div className="flex gap-2">
            <Button size="sm" disabled={!whId || recommend.isPending} onClick={() => recommend.mutate({ b: whId, m: 'putaway' })}>Recommend Put-away</Button>
            <Button size="sm" variant="outline" disabled={!whId || recommend.isPending} onClick={() => recommend.mutate({ b: whId, m: 'pick' })}>Recommend Pick</Button>
          </div>
          {rec && (
            <div className="mt-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/50 text-sm">
              <Badge variant="info" size="sm" dot>{rec.mode}</Badge>
              <span className="ml-2">Bin: <b>{rec.binId ? rec.binId.slice(0, 8) : '—'}</b></span>
              {rec.pickingPolicy && <span className="ml-3">Policy: <b>{rec.pickingPolicy}</b></span>}
              {rec.reason && <p className="mt-1 text-gray-600 dark:text-gray-400">{rec.reason}</p>}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Put-away / Picking Rules" description={`${rows.length} rule(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No rules.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th><th className="px-3 py-2 font-medium text-gray-500">Bin</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Put-away Rank</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Pick Seq</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Policy</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: PutAwayPickingRuleDto) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.warehouseId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.binId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.putAwayRank}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.pickSequence}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{POLICIES[r.pickingPolicy] ?? r.pickingPolicy}</td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
