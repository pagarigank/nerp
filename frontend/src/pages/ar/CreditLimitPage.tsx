// <copyright file="CreditLimitPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Search, AlertCircle, CheckCircle2, XCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { checkCreditLimit, getCustomers } from '@api/ar'
import type { CreditLimitCheckResult } from '@/types/ar'

export function CreditLimitPage() {
  const [formError, setFormError] = useState<string | null>(null)
  const [customerId, setCustomerId] = useState('')
  const [amount, setAmount] = useState('0')
  const [result, setResult] = useState<CreditLimitCheckResult | null>(null)

  const { data: customers = [] } = useQuery({ queryKey: ['ar', 'customers'], queryFn: getCustomers })

  const customerOptions = useMemo(() => customers.map((c: any) => ({ value: c.id, label: `${c.customerId} - ${c.name}` })), [customers])
  const selectedCustomer = useMemo(() => customers.find((c: any) => c.id === customerId), [customers, customerId])

  const mut = useMutation({
    mutationFn: () => checkCreditLimit(customerId, Number(amount)),
    onSuccess: (d) => { setResult(d); setFormError(null) },
    onError: (e) => { setResult(null); setFormError(getErrorMessage(e)) },
  })

  const canRun = Boolean(customerId)

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Credit Limit Check" description="Check a customer's available credit for a proposed amount" />
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Select label="Customer" placeholder="Select customer..." options={customerOptions} value={customerId} onChange={e => setCustomerId(e.target.value)} required />
            <Input type="number" step="0.01" min="0" value={amount} onChange={e => setAmount(e.target.value)} label="Proposed Amount" required />
          </div>
          {selectedCustomer && (
            <div className="flex gap-4 text-sm text-gray-600 dark:text-gray-400">
              <span>Credit Limit: <span className="font-medium">${selectedCustomer.creditLimit?.toFixed(2) ?? '0.00'}</span></span>
              <span>Tax Exempt: {selectedCustomer.taxExempt ? 'Yes' : 'No'}</span>
              <span>Currency: {selectedCustomer.currencyCode ?? 'USD'}</span>
            </div>
          )}
          <Button variant="primary" onClick={() => mut.mutate()} disabled={!canRun || mut.isPending} leftIcon={<Search className="h-4 w-4" />} isLoading={mut.isPending}>
            Check Credit
          </Button>
        </CardContent>
      </Card>

      {result && (
        <Card>
          <CardHeader
            title="Credit Check Result"
            action={result.isApproved
              ? <Badge variant="success" size="sm" dot>Approved</Badge>
              : <Badge variant="error" size="sm" dot>Exceeds Limit</Badge>}
          />
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
              <div><p className="text-gray-500">Credit Limit</p><p className="font-medium text-gray-900 dark:text-white tabular-nums">${result.creditLimit.toFixed(2)}</p></div>
              <div><p className="text-gray-500">Current Balance</p><p className="font-medium text-gray-900 dark:text-white tabular-nums">${result.currentBalance.toFixed(2)}</p></div>
              <div><p className="text-gray-500">Available Credit</p><p className="font-medium text-gray-900 dark:text-white tabular-nums">${result.availableCredit.toFixed(2)}</p></div>
              <div className="flex items-center">
                {result.isApproved
                  ? <span className="flex items-center gap-1 text-green-700 dark:text-green-400"><CheckCircle2 className="h-4 w-4" /> Approved</span>
                  : <span className="flex items-center gap-1 text-red-700 dark:text-red-400"><XCircle className="h-4 w-4" /> Exceeds Limit</span>}
              </div>
            </div>
            {result.message && <p className="mt-3 text-sm text-gray-600 dark:text-gray-400">{result.message}</p>}
          </CardContent>
        </Card>
      )}
    </div>
  )
}
