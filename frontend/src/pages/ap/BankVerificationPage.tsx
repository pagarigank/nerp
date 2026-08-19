// <copyright file="BankVerificationPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { AlertCircle } from 'lucide-react'
import { getVendors, verifyBankAccount, approveBankVerification, rejectBankVerification, getBankVerifications } from '@api/ap'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { Vendor, VendorBankVerificationDto } from '@/types/ap'

export function BankVerificationPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [vendorId, setVendorId] = useState('')
  const [routing, setRouting] = useState('')
  const [account, setAccount] = useState('')
  const [err, setErr] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors(), enabled: !!companyId })
  const { data: verifications = [], isLoading } = useQuery({ queryKey: ['bankverif', companyId], queryFn: () => getBankVerifications(), enabled: !!companyId })

  const vendorOptions = useMemo(
    () => vendors.map((v: Vendor) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  const selectedVendor = vendors.find((v: Vendor) => v.id === vendorId)

  const verify = useMutation({
    mutationFn: () => verifyBankAccount({ vendorBankAccountId: vendorId, routingNumber: routing, accountNumber: account }),
    onSuccess: () => { setErr(null); setRouting(''); setAccount(''); queryClient.invalidateQueries({ queryKey: ['bankverif'] }) },
    onError: (e) => setErr(getErrorMessage(e)),
  })

  const decide = useMutation({
    mutationFn: ({ id, action }: { id: string; action: 'approve' | 'reject' }) =>
      action === 'approve' ? approveBankVerification(id, { notes: 'Pre-note approved' }) : rejectBankVerification(id, { notes: 'Pre-note rejected' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['bankverif'] }),
    onError: (e) => setErr(getErrorMessage(e)),
  })

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Vendor Bank Account Verification (Pre-Note / ACH)" description="Submit and approve bank account pre-notes before first ACH payment" />
        <CardContent className="space-y-4">
          {err && <div className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-4 w-4" /> <span>{err}</span></div>}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={vendorId} onChange={e => setVendorId(e.target.value)} required />
            <Input placeholder="9-digit routing number" value={routing} onChange={e => setRouting(e.target.value)} label="Routing Number" required />
            <Input placeholder="Account number" value={account} onChange={e => setAccount(e.target.value)} label="Account Number" required />
          </div>
          {selectedVendor && selectedVendor.bankAccounts.length > 0 && (
            <div className="text-sm text-gray-600 dark:text-gray-400">
              Existing bank accounts: {selectedVendor.bankAccounts.map(ba => `${ba.bankName} (${ba.accountNumber})`).join(', ')}
            </div>
          )}
          <Button variant="primary" disabled={!vendorId || !routing || !account || verify.isPending} onClick={() => verify.mutate()} isLoading={verify.isPending}>
            Submit Pre-Note
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Verification Queue" description={`${verifications.length} verification(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> : (
            verifications.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No verifications.</p> : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Bank Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Routing</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Notes</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {verifications.map((v: VendorBankVerificationDto) => (
                      <tr key={v.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{v.vendorBankAccountId.slice(0, 8)}…</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{v.routingNumber}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{v.accountNumber}</td>
                        <td className="px-3 py-3"><Badge variant={v.status === 'Approved' ? 'success' : v.status === 'Rejected' ? 'error' : 'warning'} size="sm" dot>{v.status}</Badge></td>
                        <td className="px-3 py-3 text-gray-500 text-xs">{v.notes ?? '—'}</td>
                        <td className="px-3 py-3 text-right">
                          {v.status === 'Pending' && (
                            <div className="flex justify-end gap-1">
                              <Button size="sm" variant="outline" disabled={decide.isPending} onClick={() => decide.mutate({ id: v.id, action: 'approve' })}>Approve</Button>
                              <Button size="sm" variant="ghost" className="text-red-600" disabled={decide.isPending} onClick={() => decide.mutate({ id: v.id, action: 'reject' })}>Reject</Button>
                            </div>
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
