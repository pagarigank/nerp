import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { getVendors, getVoucherBatches, validateThreeWayMatch } from '@api/ap'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { Vendor, VoucherBatch, ThreeWayMatchRequest, ThreeWayMatchResult } from '@/types/ap'

export function MatchExceptionPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [vendorId, setVendorId] = useState('')
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [result, setResult] = useState<ThreeWayMatchResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['vendors', companyId], queryFn: () => getVendors(), enabled: !!companyId })
  const { data: batches = [] } = useQuery({ queryKey: ['voucherBatches', companyId], queryFn: () => getVoucherBatches(companyId), enabled: !!companyId })

  const mutation = useMutation({
    mutationFn: (req: ThreeWayMatchRequest) => validateThreeWayMatch(req),
    onSuccess: (r) => { setResult(r); queryClient.invalidateQueries({ queryKey: ['voucherBatches'] }) },
    onError: (e) => setError(getErrorMessage(e)),
  })

  const findBatch = () => batches.find((b: VoucherBatch) => b.vouchers.some((v) => v.vendorId === vendorId))

  const runCheck = () => {
    const batch = findBatch()
    if (!batch) {
      setError('Select a vendor with posted vouchers to validate.')
      return
    }
    const lines = batch.vouchers.flatMap((v) =>
      v.distributions.map((d) => ({
        itemCode: d.accountId.slice(0, 8),
        description: v.invoiceNumber,
        orderedQuantity: 1,
        receivedQuantity: 1,
        invoicedQuantity: d.debit > 0 ? 1 : 1,
        unitPrice: d.debit || d.credit,
        extendedAmount: d.debit || d.credit,
      })),
    )
    mutation.mutate({
      companyId,
      vendorId,
      invoiceNumber: invoiceNumber || batch.vouchers[0]?.invoiceNumber || 'INV',
      lines,
      invoiceTotal: batch.vouchers.reduce((s, v) => s + v.totalAmount, 0),
    })
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">3-Way Match Exception UI</h1>
      <p className="text-sm text-gray-500 dark:text-gray-400">Flag PO ↔ Receipt ↔ Invoice mismatches before payment.</p>
      <Card>
        <CardHeader title="Run Match Validation" />
        <CardContent className="space-y-3">
          {error && <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-300">{error}</div>}
          <select className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-800" value={vendorId} onChange={(e) => setVendorId(e.target.value)}>
            <option value="">Select vendor…</option>
            {vendors.map((v: Vendor) => (<option key={v.id} value={v.id}>{v.vendorId} — {v.name}</option>))}
          </select>
          <input className="w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-800" placeholder="Invoice number (optional)" value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} />
          <Button disabled={!vendorId || mutation.isPending} onClick={runCheck}>{mutation.isPending ? 'Validating…' : 'Validate Match'}</Button>
          {result && (
            <div className={`rounded-md p-3 text-sm ${result.isValid ? 'bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-300' : 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300'}`}>
              <div className="font-semibold">{result.isValid ? 'Match OK' : 'Exception(s) found'}</div>
              <div>Qty variance: <Badge variant={result.hasQuantityVariance ? 'error' : 'success'}>{result.hasQuantityVariance ? 'Yes' : 'No'}</Badge></div>
              <div>Price variance: <Badge variant={result.hasPriceVariance ? 'error' : 'success'}>{result.hasPriceVariance ? 'Yes' : 'No'}</Badge></div>
              <div>Total variance: {result.totalVarianceAmount.toLocaleString()}</div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
