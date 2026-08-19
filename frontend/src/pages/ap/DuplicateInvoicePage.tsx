// <copyright file="DuplicateInvoicePage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { AlertCircle, Search } from 'lucide-react'
import { getVendors, checkDuplicateInvoice, getDuplicateChecks } from '@api/ap'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { Vendor, DuplicateInvoiceCheckDto } from '@/types/ap'

export function DuplicateInvoicePage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [vendorId, setVendorId] = useState('')
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [amount, setAmount] = useState('')
  const [lookbackDays, setLookbackDays] = useState('90')
  const [err, setErr] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors(), enabled: !!companyId })
  const { data: checks = [], isLoading } = useQuery({ queryKey: ['dupchecks', companyId], queryFn: () => getDuplicateChecks(companyId), enabled: !!companyId })

  const vendorOptions = useMemo(
    () => vendors.map((v: Vendor) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach((v: Vendor) => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const mutate = useMutation({
    mutationFn: () => checkDuplicateInvoice({ companyId, vendorId, invoiceNumber, amount: Number(amount), lookbackDays: Number(lookbackDays) }),
    onSuccess: () => { setErr(null); queryClient.invalidateQueries({ queryKey: ['dupchecks'] }) },
    onError: (e) => setErr(getErrorMessage(e)),
  })

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Duplicate Invoice Detection" description="Check if an invoice has already been entered for this vendor" />
        <CardContent className="space-y-4">
          {err && <div className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-4 w-4" /> <span>{err}</span></div>}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={vendorId} onChange={e => setVendorId(e.target.value)} required />
            <Input placeholder="INV-2026-001" value={invoiceNumber} onChange={e => setInvoiceNumber(e.target.value)} label="Invoice Number" required />
            <Input type="number" step="0.01" min="0" placeholder="0.00" value={amount} onChange={e => setAmount(e.target.value)} label="Amount" />
            <Input type="number" min="1" placeholder="90" value={lookbackDays} onChange={e => setLookbackDays(e.target.value)} label="Lookback Days" />
            <div className="flex items-end">
              <Button variant="primary" disabled={!vendorId || !invoiceNumber || mutate.isPending} onClick={() => mutate.mutate()} isLoading={mutate.isPending} leftIcon={<Search className="h-4 w-4" />}>
                Check
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Recent Checks" description={`${checks.length} check(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> : (
            checks.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No duplicate checks yet.</p> : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Invoice</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Checked</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {checks.map((c: DuplicateInvoiceCheckDto) => (
                      <tr key={c.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{vendorNames.get(c.vendorId) ?? c.vendorId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{c.invoiceNumber}</td>
                        <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${c.amount.toLocaleString()}</td>
                        <td className="px-3 py-3">{c.isDuplicate ? <Badge variant="error" size="sm">Duplicate</Badge> : <Badge variant="success" size="sm">Unique</Badge>}</td>
                        <td className="px-3 py-3 text-gray-500 text-xs">{new Date(c.checkedOn).toLocaleString()}</td>
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
