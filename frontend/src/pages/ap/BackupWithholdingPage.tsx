// <copyright file="BackupWithholdingPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Calculator, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { calculateBackupWithholding, getVendors } from '@api/ap'
import type { BackupWithholdingResult } from '@/types/ap'

export function BackupWithholdingPage() {
  const [formError, setFormError] = useState<string | null>(null)
  const [vendorId, setVendorId] = useState('')
  const [paymentAmount, setPaymentAmount] = useState('0')
  const [result, setResult] = useState<BackupWithholdingResult | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })

  const vendorOptions = useMemo(
    () => vendors.map(v => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  const mut = useMutation({
    mutationFn: () => calculateBackupWithholding({ vendorId, paymentAmount: Number(paymentAmount) }),
    onSuccess: (d) => { setResult(d); setFormError(null) },
    onError: (e) => { setResult(null); setFormError(getErrorMessage(e)) },
  })

  const canRun = useMemo(() => Boolean(vendorId) && Number(paymentAmount) > 0, [vendorId, paymentAmount])

  const selectedVendor = vendors.find(v => v.id === vendorId)

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Backup Withholding" description="Calculate backup withholding on a vendor payment" />
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select
              label="Vendor"
              placeholder="Select vendor..."
              options={vendorOptions}
              value={vendorId}
              onChange={e => setVendorId(e.target.value)}
              required
            />
            <Input
              type="number"
              step="0.01"
              min="0"
              value={paymentAmount}
              onChange={e => setPaymentAmount(e.target.value)}
              label="Payment Amount"
              required
            />
          </div>
          {selectedVendor && (
            <div className="flex gap-4 text-sm text-gray-600 dark:text-gray-400">
              <span>Withholding: {selectedVendor.backupWithholdingFlag ? <Badge variant="warning" size="sm">{(selectedVendor.backupWithholdingRate * 100).toFixed(0)}%</Badge> : <Badge variant="neutral" size="sm">None</Badge>}</span>
              <span>1099 Category: {selectedVendor.form1099Category ?? '—'}</span>
            </div>
          )}
          <Button variant="primary" onClick={() => mut.mutate()} disabled={!canRun || mut.isPending}>
            <Calculator className="h-4 w-4" /> Calculate
          </Button>
        </CardContent>
      </Card>

      {result && (
        <Card>
          <CardHeader title="Withholding Result" action={result.isSubjectToWithholding ? <Badge variant="warning" size="sm" dot>Subject</Badge> : <Badge variant="neutral" size="sm" dot>Not Subject</Badge>} />
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
              <div><p className="text-gray-500">Rate</p><p className="font-medium text-gray-900 dark:text-white">{(result.withholdingRate * 100).toFixed(2)}%</p></div>
              <div><p className="text-gray-500">Withholding</p><p className="font-medium text-gray-900 dark:text-white">${result.withholdingAmount.toFixed(2)}</p></div>
              <div><p className="text-gray-500">Net Payment</p><p className="font-medium text-gray-900 dark:text-white">${result.netPaymentAmount.toFixed(2)}</p></div>
              <div><p className="text-gray-500">Subject</p><p className="font-medium text-gray-900 dark:text-white">{result.isSubjectToWithholding ? 'Yes' : 'No'}</p></div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
