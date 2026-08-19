import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getReorderSuggestions, approveReorderSuggestion, convertReorderSuggestion } from '@api/inventory'

export function ReorderSuggestionsPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'reorder-suggestions'], queryFn: () => getReorderSuggestions() })
  const approve = useMutation({ mutationFn: (id: string) => approveReorderSuggestion(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'reorder-suggestions'] }), onError: e => setErr(getErrorMessage(e)) })
  const convert = useMutation({ mutationFn: (id: string) => convertReorderSuggestion(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'reorder-suggestions'] }), onError: e => setErr(getErrorMessage(e)) })

  return (
    <div className="space-y-6">
      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}
      <Card>
        <CardHeader title="Reorder Suggestions" description={`${rows.length} suggestion(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No reorder suggestions.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Item</th><th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Suggested Qty</th><th className="px-3 py-2 font-medium text-gray-500">Status</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.itemId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.suggestedQuantity}</td>
                      <td className="px-3 py-3"><Badge variant={r.status === 'Approved' ? 'success' : r.status === 'Converted' ? 'neutral' : 'warning'} size="sm" dot>{r.status}</Badge></td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          {r.status === 'Pending' && <Button size="sm" variant="outline" disabled={approve.isPending} onClick={() => approve.mutate(r.id)}>Approve</Button>}
                          {r.status === 'Approved' && <Button size="sm" variant="primary" disabled={convert.isPending} onClick={() => convert.mutate(r.id)}>Convert to PO</Button>}
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
