// <copyright file="VendorW9Page.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { AlertCircle } from 'lucide-react'
import { getVendors, captureW9, getW9 } from '@api/ap'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { Vendor, VendorW9Dto } from '@/types/ap'

export function VendorW9Page() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [vendorId, setVendorId] = useState('')
  const [taxId, setTaxId] = useState('')
  const [legalName, setLegalName] = useState('')
  const [tinVerified, setTinVerified] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors(), enabled: !!companyId })
  const { data: records = [] } = useQuery({ queryKey: ['w9', vendorId], queryFn: () => getW9(vendorId), enabled: !!vendorId })

  const vendorOptions = useMemo(
    () => vendors.map((v: Vendor) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach((v: Vendor) => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const selectedVendor = vendors.find((v: Vendor) => v.id === vendorId)

  const mutate = useMutation({
    mutationFn: () => captureW9(vendorId, { taxId, legalName, tinVerified, tinMatchStatus: tinVerified ? 'Valid' : 'Pending' }),
    onSuccess: () => { setErr(null); setTaxId(''); setLegalName(''); setTinVerified(false); queryClient.invalidateQueries({ queryKey: ['w9'] }) },
    onError: (e) => setErr(getErrorMessage(e)),
  })

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="W-9 Capture & TIN Verification" description="Capture vendor W-9 information and verify TIN against IRS records" />
        <CardContent className="space-y-4">
          {err && <div className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-4 w-4" /> <span>{err}</span></div>}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={vendorId} onChange={e => setVendorId(e.target.value)} required />
            <Input placeholder="XX-XXXXXXX" value={taxId} onChange={e => setTaxId(e.target.value)} label="TIN (Tax ID)" required />
            <Input placeholder="Legal entity name" value={legalName} onChange={e => setLegalName(e.target.value)} label="Legal Name" />
            <div className="flex items-end gap-4">
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 pb-2">
                <input type="checkbox" checked={tinVerified} onChange={(e) => setTinVerified(e.target.checked)} className="h-4 w-4" /> TIN Verified
              </label>
              <Button variant="primary" disabled={!vendorId || !taxId || mutate.isPending} onClick={() => mutate.mutate()} isLoading={mutate.isPending}>
                Capture W-9
              </Button>
            </div>
          </div>
          {selectedVendor && (
            <div className="text-sm text-gray-600 dark:text-gray-400">
              Vendor: {selectedVendor.name} | Tax ID on file: {selectedVendor.taxId ?? '—'}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Captured W-9 Records" description={vendorId ? `Records for ${selectedVendor?.name ?? 'selected vendor'}` : 'Select a vendor to view records'} />
        <CardContent>
          {!vendorId ? (
            <p className="text-sm text-gray-500 py-8 text-center">Select a vendor to view W-9 records.</p>
          ) : records.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">No W-9 captured for this vendor.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">TIN</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Legal Name</th>
                  <th className="px-3 py-2 font-medium text-gray-500">TIN Status</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Captured</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {records.map((r: VendorW9Dto) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.taxId}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.legalName}</td>
                      <td className="px-3 py-3">{r.tinVerified ? <Badge variant="success" size="sm">{r.tinMatchStatus ?? 'Verified'}</Badge> : <Badge variant="warning" size="sm">Unverified</Badge>}</td>
                      <td className="px-3 py-3 text-gray-500 text-xs">{new Date(r.capturedOn).toLocaleString()}</td>
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
