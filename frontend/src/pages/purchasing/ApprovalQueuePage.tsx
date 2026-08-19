import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Check } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { getErrorMessage } from '@api/client'
import { getPurchaseOrderApprovalQueue, approvePurchaseOrder } from '@api/purchasing'

export function ApprovalQueuePage() {
  const qc = useQueryClient()
  const [error, setError] = useState<string | null>(null)

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['purchasing', 'po-approval-queue'],
    queryFn: () => getPurchaseOrderApprovalQueue(),
  })

  const approveMutation = useMutation({
    mutationFn: (id: string) => approvePurchaseOrder(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['purchasing', 'po-approval-queue'] }),
    onError: (e) => setError(getErrorMessage(e)),
  })

  return (
    <div className="space-y-6">
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{error}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Purchase Order Approval Queue" description={`${rows.length} PO(s) pending approval`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No purchase orders pending approval.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">PO #</th><th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Order Date</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Total</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Released</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.poNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.vendorId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.orderDate).toLocaleDateString()}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.totalAmount.toFixed(2)}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.releasedAmount.toFixed(2)}</td>
                      <td className="px-3 py-3 text-right">
                        <Button size="sm" variant="primary" disabled={approveMutation.isPending} onClick={() => approveMutation.mutate(r.id)}><Check className="h-3.5 w-3.5" /> Approve</Button>
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
