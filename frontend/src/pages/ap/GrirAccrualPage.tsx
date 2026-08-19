// <copyright file="GrirAccrualPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle } from 'lucide-react'
import { useAuthStore } from '@stores/authStore'
import { getVendors, createGrirAccrual, reverseGrirAccrual, getGrirAccruals } from '@api/ap'
import { getFiscalPeriods } from '@api/platform'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { Vendor, GrirAccrualDto } from '@/types/ap'

export function GrirAccrualPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [vendorId, setVendorId] = useState('')
  const [accrualAmount, setAccrualAmount] = useState('')
  const [fiscalPeriodId, setFiscalPeriodId] = useState('')
  const [err, setErr] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors(), enabled: !!companyId })
  const { data: periods = [] } = useQuery({ queryKey: ['platform', 'fiscalPeriods'], queryFn: () => getFiscalPeriods() })
  const { data: accruals = [], isLoading } = useQuery({ queryKey: ['grir', companyId], queryFn: () => getGrirAccruals(companyId), enabled: !!companyId })

  const vendorOptions = useMemo(
    () => vendors.map((v: Vendor) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  const periodOptions = useMemo(
    () => periods.map((p: any) => ({ value: p.id, label: `P${p.periodNumber} - ${p.description}` })),
    [periods]
  )

  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach((v: Vendor) => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const create = useMutation({
    mutationFn: () => createGrirAccrual({ companyId, vendorId, accrualAmount: Number(accrualAmount), fiscalPeriodId }),
    onSuccess: () => { setErr(null); setVendorId(''); setAccrualAmount(''); setFiscalPeriodId(''); queryClient.invalidateQueries({ queryKey: ['grir'] }) },
    onError: (e) => setErr(getErrorMessage(e)),
  })

  const reverse = useMutation({
    mutationFn: ({ id, fp }: { id: string; fp: string }) => reverseGrirAccrual(id, { fiscalPeriodId: fp }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['grir'] }),
    onError: (e) => setErr(getErrorMessage(e)),
  })

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{err}</span></div>}

      <Card>
        <CardHeader title="Create GR/IR Accrual" description="Record an accrual for goods received but not yet invoiced" />
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={vendorId} onChange={e => setVendorId(e.target.value)} required />
            <Input type="number" step="0.01" min="0" placeholder="0.00" value={accrualAmount} onChange={e => setAccrualAmount(e.target.value)} label="Accrual Amount" required />
            <Select label="Fiscal Period" placeholder="Select period..." options={periodOptions} value={fiscalPeriodId} onChange={e => setFiscalPeriodId(e.target.value)} required />
          </div>
          <Button variant="primary" disabled={!vendorId || !accrualAmount || !fiscalPeriodId || create.isPending} onClick={() => create.mutate()} isLoading={create.isPending}>
            Create Accrual
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Accruals" description={`${accruals.length} accrual(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> : (
            accruals.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No accruals.</p> : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Period</th>
                    <th className="px-3 py-2 font-medium text-gray-500">PO</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Receipt</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Action</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {accruals.map((a: GrirAccrualDto) => (
                      <tr key={a.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{vendorNames.get(a.vendorId) ?? a.vendorId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${a.accrualAmount.toLocaleString()}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{a.fiscalPeriodId.slice(0, 8)}…</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{a.purchaseOrderId ? a.purchaseOrderId.slice(0, 8) + '…' : '—'}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{a.receiptId ? a.receiptId.slice(0, 8) + '…' : '—'}</td>
                        <td className="px-3 py-3">
                          {a.reversedByAccrualId ? <Badge variant="neutral" size="sm">Reversed</Badge> : <Badge variant="success" size="sm" dot>Active</Badge>}
                        </td>
                        <td className="px-3 py-3 text-right">
                          {!a.reversedByAccrualId && (
                            <Button variant="outline" size="sm" disabled={reverse.isPending} onClick={() => reverse.mutate({ id: a.id, fp: a.fiscalPeriodId })}>
                              Reverse
                            </Button>
                          )}
                        </td>
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


