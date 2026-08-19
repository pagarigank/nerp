// <copyright file="CashDiscountPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { AlertCircle } from 'lucide-react'
import { getVendors, captureCashDiscount, getCashDiscounts, getLostDiscountSummary, getVoucherBatches } from '@api/ap'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { Vendor, CashDiscountCaptureDto, LostDiscountSummaryDto, VoucherBatch, Voucher } from '@/types/ap'

export function CashDiscountPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [vendorId, setVendorId] = useState('')
  const [voucherId, setVoucherId] = useState('')
  const [invoiceAmount, setInvoiceAmount] = useState('')
  const [discountAvailable, setDiscountAvailable] = useState('')
  const [discountTaken, setDiscountTaken] = useState('')
  const [err, setErr] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors(), enabled: !!companyId })
  const { data: batches = [] } = useQuery({ queryKey: ['ap', 'voucherBatches'], queryFn: () => getVoucherBatches(companyId), enabled: !!companyId })
  const { data: discounts = [], isLoading } = useQuery({ queryKey: ['cashdisc', companyId], queryFn: () => getCashDiscounts(companyId), enabled: !!companyId })
  const { data: summary } = useQuery({ queryKey: ['cashdiscsum', companyId], queryFn: () => getLostDiscountSummary(companyId), enabled: !!companyId })

  const vendorOptions = useMemo(
    () => vendors.map((v: Vendor) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  // Get vouchers for the selected vendor from posted batches
  const vendorVouchers = useMemo(() => {
    if (!vendorId) return []
    const result: Voucher[] = []
    for (const batch of batches) {
      const isPosted = batch.status === 2 || batch.status === 'Posted'
      if (!isPosted) continue
      for (const v of batch.vouchers) {
        if (v.vendorId === vendorId) {
          result.push(v)
        }
      }
    }
    return result
  }, [batches, vendorId])

  const voucherOptions = useMemo(
    () => vendorVouchers.map(v => ({ value: v.id, label: `${v.invoiceNumber} - $${v.totalAmount.toFixed(2)}` })),
    [vendorVouchers]
  )

  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach((v: Vendor) => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const mutate = useMutation({
    mutationFn: () => captureCashDiscount({
      voucherId, vendorId,
      invoiceAmount: Number(invoiceAmount),
      discountAvailable: Number(discountAvailable),
      discountTaken: Number(discountTaken),
      discountLost: Number(discountTaken) < Number(discountAvailable),
    }),
    onSuccess: () => {
      setErr(null); setVendorId(''); setVoucherId(''); setInvoiceAmount(''); setDiscountAvailable(''); setDiscountTaken('')
      queryClient.invalidateQueries({ queryKey: ['cashdisc'] })
      queryClient.invalidateQueries({ queryKey: ['cashdiscsum'] })
    },
    onError: (e) => setErr(getErrorMessage(e)),
  })

  const sum: LostDiscountSummaryDto | undefined = summary

  // Auto-fill invoice amount when voucher is selected
  const handleVoucherChange = (vid: string) => {
    setVoucherId(vid)
    const v = vendorVouchers.find(v => v.id === vid)
    if (v) {
      setInvoiceAmount(String(v.totalAmount))
      // Calculate discount available if payment terms exist
      if (v.discountAmount > 0) {
        setDiscountAvailable(String(v.discountAmount))
      }
    }
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Cash Discount Capture" description="Track early-payment discounts (e.g. 2/10 net 30)" />
        <CardContent className="space-y-4">
          {err && <div className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-4 w-4" /> <span>{err}</span></div>}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={vendorId} onChange={e => { setVendorId(e.target.value); setVoucherId('') }} required />
            <Select label="Voucher" placeholder="Select voucher..." options={voucherOptions} value={voucherId} onChange={e => handleVoucherChange(e.target.value)} />
            <Input type="number" step="0.01" min="0" placeholder="0.00" value={invoiceAmount} onChange={e => setInvoiceAmount(e.target.value)} label="Invoice Amount" />
            <Input type="number" step="0.01" min="0" placeholder="0.00" value={discountAvailable} onChange={e => setDiscountAvailable(e.target.value)} label="Discount Available" />
            <Input type="number" step="0.01" min="0" placeholder="0.00" value={discountTaken} onChange={e => setDiscountTaken(e.target.value)} label="Discount Taken" />
          </div>
          {Number(discountAvailable) > 0 && Number(discountTaken) < Number(discountAvailable) && (
            <div className="text-sm text-amber-600">⚠ Discount partially taken — {((1 - Number(discountTaken) / Number(discountAvailable)) * 100).toFixed(0)}% lost</div>
          )}
          <Button variant="primary" disabled={!vendorId || mutate.isPending} onClick={() => mutate.mutate()} isLoading={mutate.isPending}>
            Capture Discount
          </Button>
        </CardContent>
      </Card>

      {sum && (
        <Card>
          <CardHeader title="Lost Discount Summary" />
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
              <div><p className="text-gray-500">Available</p><p className="font-medium text-gray-900 dark:text-white tabular-nums">${sum.totalAvailable.toLocaleString()}</p></div>
              <div><p className="text-gray-500">Taken</p><p className="font-medium text-gray-900 dark:text-white tabular-nums">${sum.totalTaken.toLocaleString()}</p></div>
              <div><p className="text-gray-500">Lost</p><p className="font-medium text-red-600 dark:text-red-400 tabular-nums">${sum.totalLost.toLocaleString()}</p></div>
              <div><p className="text-gray-500">Lost Count</p><p className="font-medium text-gray-900 dark:text-white">{sum.lostCount}</p></div>
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader title="Captured Discounts" description={`${discounts.length} capture(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> : (
            discounts.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No discounts captured yet.</p> : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Invoice</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Available</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Taken</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Lost</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {discounts.map((d: CashDiscountCaptureDto) => (
                      <tr key={d.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{vendorNames.get(d.vendorId) ?? d.vendorId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300 tabular-nums">${d.invoiceAmount.toLocaleString()}</td>
                        <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${d.discountAvailable.toLocaleString()}</td>
                        <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${d.discountTaken.toLocaleString()}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{d.discountLost ? <span className="text-red-600">${d.discountLostAmount.toLocaleString()}</span> : '—'}</td>
                        <td className="px-3 py-3">{d.discountLost ? <Badge variant="error" size="sm">Lost</Badge> : <Badge variant="success" size="sm">Captured</Badge>}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
          )}
        </CardContent>
      </Card>
    </div>
  )
}
