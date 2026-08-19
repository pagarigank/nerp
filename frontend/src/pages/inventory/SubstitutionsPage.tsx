import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { getItems } from '@api/inventory'
import {
  getItemSubstitutions,
  createItemSubstitution,
  approveItemSubstitution,
  rejectItemSubstitution,
} from '@api/inventory'
import type { ItemSubstitutionDto } from '@/types/inventory'

export function SubstitutionsPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [itemId, setItemId] = useState('')
  const [subItemId, setSubItemId] = useState('')
  const [reason, setReason] = useState('')

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['inventory', 'substitutions'],
    queryFn: () => getItemSubstitutions(),
  })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items-mini'], queryFn: () => getItems() })

  const create = useMutation({
    mutationFn: () =>
      createItemSubstitution({
        companyId: '',
        itemId,
        substituteItemId: subItemId,
        direction: 1,
        reason,
        requiresApproval: true,
      }),
    onSuccess: () => { setItemId(''); setSubItemId(''); setReason(''); qc.invalidateQueries({ queryKey: ['inventory', 'substitutions'] }) },
    onError: e => setErr(getErrorMessage(e)),
  })
  const approve = useMutation({
    mutationFn: (id: string) => approveItemSubstitution(id, 'current-user'),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'substitutions'] }),
    onError: e => setErr(getErrorMessage(e)),
  })
  const reject = useMutation({
    mutationFn: (id: string) => rejectItemSubstitution(id, 'current-user', 'not approved'),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'substitutions'] }),
    onError: e => setErr(getErrorMessage(e)),
  })

  const name = (id: string) => items.find(i => i.id === id)?.itemCode ?? id.slice(0, 8)
  const itemOptions = [{ value: '', label: 'Item…' }, ...items.map(i => ({ value: i.id, label: i.itemCode }))]

  return (
    <div className="space-y-6">
      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}

      <Card>
        <CardHeader title="New Item Substitution" description="Link a substitute item (approval required)" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
            <Select options={itemOptions} value={itemId} onChange={e => setItemId((e.target as HTMLSelectElement).value)} />
            <Select options={itemOptions} value={subItemId} onChange={e => setSubItemId((e.target as HTMLSelectElement).value)} />
            <Input placeholder="Reason" value={reason} onChange={e => setReason(e.target.value)} />
            <Button onClick={() => create.mutate()} disabled={!itemId || !subItemId || create.isPending}>
              {create.isPending ? 'Saving…' : 'Add'}
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Item Substitutions" description={`${rows.length} substitution(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No substitutions.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Item</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Substitute</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Reason</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: ItemSubstitutionDto) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{name(r.itemId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{name(r.substituteItemId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.reason ?? '—'}</td>
                      <td className="px-3 py-3">
                        <Badge variant={r.status === 'Approved' ? 'success' : r.status === 'Rejected' ? 'error' : 'warning'} size="sm" dot>{r.status}</Badge>
                      </td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          <Button size="sm" variant="outline" disabled={r.status !== 'Pending' || approve.isPending} onClick={() => approve.mutate(r.id)}>Approve</Button>
                          <Button size="sm" variant="ghost" className="text-red-600" disabled={r.status !== 'Pending' || reject.isPending} onClick={() => reject.mutate(r.id)}>Reject</Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
