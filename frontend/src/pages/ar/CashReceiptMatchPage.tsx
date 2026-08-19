import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, Link2 } from 'lucide-react'
import { formatCurrency } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getCashReceipts, matchCashReceiptByReference, getCustomers } from '@api/ar'
import type { CashReceiptReferenceMatch } from '@/types/ar'
import { receiptStatusMap } from './statusMaps'
import { ArStatusBadge } from './ArStatusBadge'

export function CashReceiptMatchPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<CashReceiptReferenceMatch | null>(null)

  const { data: receipts = [], isLoading } = useQuery({
    queryKey: ['ar', 'cash-receipts'],
    queryFn: () => getCashReceipts(),
  })
  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: () => getCustomers(),
  })

  const matchMutation = useMutation({
    mutationFn: (receiptId: string) => matchCashReceiptByReference(receiptId),
    onSuccess: res => {
      setError(null)
      setResult(res)
      queryClient.invalidateQueries({ queryKey: ['ar', 'cash-receipts'] })
    },
    onError: err => setError(getErrorMessage(err)),
  })

  const customerName = (id: string) => customers.find(c => c.id === id)?.name ?? id.slice(0, 8)

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
          Matched reference <strong>{result.referenceNumber}</strong>: applied {formatCurrency(result.appliedAmount)} to {result.matchedInvoiceIds.length} invoice(s); {formatCurrency(result.remainingAmount)} remaining.
        </div>
      )}

      <Card>
        <CardHeader title="Cash Receipt Matching by Reference" description="Auto-apply unapplied receipts to invoices using the receipt reference number." />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={5} />
          ) : receipts.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No cash receipts.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Receipt #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Reference</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Total</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Unapplied</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {receipts.map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-primary-600 dark:text-primary-400">{r.receiptReference}</td>
                      <td className="px-3 py-3">{customerName(r.customerId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{(r as unknown as { referenceNumber?: string }).referenceNumber ?? '—'}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(r.totalAmount)}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(r.unappliedAmount)}</td>
                      <td className="px-3 py-3"><ArStatusBadge value={r.status} mapping={receiptStatusMap} /></td>
                      <td className="px-3 py-3">
                        <div className="flex justify-end">
                          <Button
                            variant="outline"
                            size="sm"
                            leftIcon={<Link2 className="h-4 w-4" />}
                            onClick={() => matchMutation.mutate(r.id)}
                            isLoading={matchMutation.isPending}
                            disabled={r.unappliedAmount <= 0}
                          >
                            Match
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
