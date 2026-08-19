import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2 } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getMemos, applyCreditMemo, getCustomers } from '@api/ar'
import type { CreditMemoApplyResult } from '@/types/ar'
import { memoStatusMap } from './statusMaps'
import { ArStatusBadge } from './ArStatusBadge'

export function CreditMemoApplyPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<CreditMemoApplyResult | null>(null)

  const { data: memos = [], isLoading } = useQuery({
    queryKey: ['ar', 'memos'],
    queryFn: () => getMemos(),
  })
  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: () => getCustomers(),
  })

  const creditMemos = memos.filter(m => m.memoType === 'CreditMemo' && m.status === 'Open')

  const applyMutation = useMutation({
    mutationFn: (memoId: string) => applyCreditMemo(memoId, { targetInvoiceIds: [] }),
    onSuccess: res => {
      setError(null)
      setResult(res)
      queryClient.invalidateQueries({ queryKey: ['ar', 'memos'] })
    },
    onError: err => setError(getErrorMessage(err)),
  })

  return (
    <div className="space-y-6">
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      {result && (
        <div className="rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-800 dark:bg-emerald-900/20 dark:border-emerald-800 dark:text-emerald-300 p-4 text-sm flex items-center gap-2">
          <CheckCircle2 className="h-5 w-5" />
          Applied {formatCurrency(result.appliedAmount)} across {result.appliedInvoiceIds.length} invoice(s).
        </div>
      )}

      <Card>
        <CardHeader title="Apply Credit Memos" description="Apply open credit memos to outstanding invoices (auto-matched by amount, oldest due date first)." />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={5} />
          ) : creditMemos.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No open credit memos to apply.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Memo #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {creditMemos.map(memo => (
                    <tr key={memo.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-primary-600 dark:text-primary-400">{memo.referenceNumber}</td>
                      <td className="px-3 py-3">{customers.find(c => c.id === memo.customerId)?.name ?? memo.customerId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(memo.memoDate)}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(memo.totalAmount)}</td>
                      <td className="px-3 py-3"><ArStatusBadge value={memo.status} mapping={memoStatusMap} /></td>
                      <td className="px-3 py-3">
                        <div className="flex justify-end">
                          <Button variant="outline" size="sm" leftIcon={<CheckCircle2 className="h-4 w-4" />} onClick={() => applyMutation.mutate(memo.id)} isLoading={applyMutation.isPending}>
                            Apply
                          </Button>
                        </div>
                      </td>
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
