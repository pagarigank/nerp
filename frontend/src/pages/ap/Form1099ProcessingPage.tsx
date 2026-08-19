// <copyright file="Form1099ProcessingPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { Download, AlertCircle } from 'lucide-react'
import { getVendors, getForm1099Summary, getForm1099Efile, classify1099, get1099Classifications } from '@api/ap'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { Vendor, Form1099VendorSummary, Ap1099ClassificationDto } from '@/types/ap'

const currentYear = new Date().getFullYear()

export function Form1099ProcessingPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [taxYear, setTaxYear] = useState(String(currentYear))
  const [vendorId, setVendorId] = useState('')
  const [formType, setFormType] = useState('0')
  const [msg, setMsg] = useState<string | null>(null)
  const [err, setErr] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors(), enabled: !!companyId })
  const { data: summary } = useQuery({ queryKey: ['1099summary', companyId, taxYear], queryFn: () => getForm1099Summary(Number(taxYear), companyId), enabled: !!companyId && !!taxYear })
  const { data: classifications = [] } = useQuery({ queryKey: ['1099class', vendorId, taxYear], queryFn: () => get1099Classifications(vendorId || undefined, Number(taxYear)), enabled: !!companyId && !!taxYear })

  const vendorOptions = useMemo(
    () => vendors.map((v: Vendor) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  const taxYearOptions = useMemo(
    () => [currentYear, currentYear - 1, currentYear - 2].map(y => ({ value: String(y), label: String(y) })),
    []
  )

  const classify = useMutation({
    mutationFn: () => classify1099({ vendorId, formType: Number(formType), taxYear: Number(taxYear) }),
    onSuccess: (r: Ap1099ClassificationDto) => { setMsg(`Classified ${r.formType} for tax year ${r.taxYear}`); setErr(null); queryClient.invalidateQueries({ queryKey: ['1099class'] }) },
    onError: (e) => { setErr(getErrorMessage(e)); setMsg(null) },
  })

  const exportEfile = async () => {
    try {
      const data = await getForm1099Efile(Number(taxYear), companyId)
      const blob = new Blob([data], { type: 'text/plain' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `1099_${taxYear}.txt`
      a.click()
      URL.revokeObjectURL(url)
      setMsg('e-File (IRIS-ready) downloaded.')
    } catch (e) {
      setErr(getErrorMessage(e))
    }
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader
          title="1099 Processing"
          description="Vendor 1099 summary, NEC/MISC classification, and e-file export"
          action={<Button variant="outline" size="sm" onClick={exportEfile} leftIcon={<Download className="h-4 w-4" />}>Export e-File</Button>}
        />
        <CardContent className="space-y-4">
          {msg && <div className="p-3 rounded-lg bg-green-50 border border-green-200 text-green-700 text-sm">{msg}</div>}
          {err && <div className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"><AlertCircle className="h-4 w-4" /> <span>{err}</span></div>}
          <div className="max-w-xs">
            <Select label="Tax Year" options={taxYearOptions} value={taxYear} onChange={e => setTaxYear(e.target.value)} />
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="1099 Summary" description={`${summary?.vendors.length ?? 0} vendor(s) with reportable payments`} />
        <CardContent>
          {summary ? (
            <div className="space-y-4">
              <div className="grid grid-cols-2 md:grid-cols-3 gap-4 text-sm">
                <div><p className="text-gray-500">Tax Year</p><p className="font-medium text-gray-900 dark:text-white">{summary.taxYear}</p></div>
                <div><p className="text-gray-500">Total Payments</p><p className="font-medium text-gray-900 dark:text-white tabular-nums">${summary.totalPayments.toLocaleString()}</p></div>
                <div><p className="text-gray-500">Backup Withholding</p><p className="font-medium text-gray-900 dark:text-white tabular-nums">${summary.totalBackupWithholding.toLocaleString()}</p></div>
              </div>
              {summary.vendors.length > 0 && (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                      <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                      <th className="px-3 py-2 font-medium text-gray-500">Tax ID</th>
                      <th className="px-3 py-2 font-medium text-gray-500">Category</th>
                      <th className="px-3 py-2 font-medium text-gray-500 text-right">Payments</th>
                      <th className="px-3 py-2 font-medium text-gray-500 text-right">Backup Wh.</th>
                    </tr></thead>
                    <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                      {summary.vendors.map((v: Form1099VendorSummary) => (
                        <tr key={v.vendorId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                          <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{v.name}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{v.taxId ?? '—'}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{v.category}</td>
                          <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${v.totalPayments.toLocaleString()}</td>
                          <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300 tabular-nums">{v.backupWithholdingAmount > 0 ? `$${v.backupWithholdingAmount.toLocaleString()}` : '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          ) : <p className="text-sm text-gray-500 py-8 text-center">No data.</p>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="NEC vs MISC Classification" description="Classify vendors for 1099-NEC or 1099-MISC reporting" />
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={vendorId} onChange={e => setVendorId(e.target.value)} required />
            <Select label="Form Type" options={[{ value: '0', label: 'Form 1099-NEC' }, { value: '1', label: 'Form 1099-MISC' }]} value={formType} onChange={e => setFormType(e.target.value)} />
            <div className="flex items-end">
              <Button variant="primary" disabled={!vendorId || classify.isPending} onClick={() => classify.mutate()} isLoading={classify.isPending}>
                Classify
              </Button>
            </div>
          </div>
          {classifications.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {classifications.map((c: Ap1099ClassificationDto) => (
                <Badge key={c.id} variant={c.formType === 'NEC' ? 'info' : 'warning'} size="sm">{c.formType} · {c.taxYear}</Badge>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
