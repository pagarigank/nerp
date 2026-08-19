import { useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { getForm1099Summary, getForm1099Efile } from '@api/ap'
import type { Form1099SummaryResult } from '@/types/ap'

export function Form1099Page() {
  const [formError, setFormError] = useState<string | null>(null)
  const [taxYear, setTaxYear] = useState(new Date().getFullYear())

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['ap', '1099', taxYear],
    queryFn: async () => getForm1099Summary(taxYear),
  })
  useEffect(() => { if (isError) setFormError(getErrorMessage(error)) }, [isError, error])

  const downloadEfile = async () => {
    setFormError(null)
    try {
      const content = await getForm1099Efile(taxYear)
      const blob = new Blob([content], { type: 'text/csv' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `1099-${taxYear}.csv`
      a.click()
      URL.revokeObjectURL(url)
    } catch (e) {
      setFormError(getErrorMessage(e))
    }
  }

  const summary = data as Form1099SummaryResult | undefined

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Form 1099 Processing" description="Vendor 1099 summary and e-file export" action={<Button variant="outline" size="sm" onClick={downloadEfile} leftIcon={<Download className="h-4 w-4" />}>Export e-File</Button>} />
        <CardContent>
          <div className="max-w-xs mb-4"><Input type="number" min="2000" max="2100" value={String(taxYear)} onChange={e => setTaxYear(Number(e.target.value))} label="Tax Year" /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !summary ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4 mb-4">
                  <div><p className="text-xs text-gray-500">Tax Year</p><p className="font-medium text-gray-900 dark:text-white">{summary.taxYear}</p></div>
                  <div><p className="text-xs text-gray-500">Total Payments</p><p className="font-medium text-gray-900 dark:text-white">{summary.totalPayments.toFixed(2)}</p></div>
                  <div><p className="text-xs text-gray-500">Backup Wh.</p><p className="font-medium text-gray-900 dark:text-white">{summary.totalBackupWithholding.toFixed(2)}</p></div>
                </div>
                <div className="overflow-x-auto"><table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Vendor</th><th className="px-3 py-2 font-medium text-gray-500">Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Category</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Payments</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Backup Wh.</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {summary.vendors.map(v => (
                      <tr key={v.vendorId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{v.name}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{v.vendorIdCode}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{v.category}</td>
                        <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{v.totalPayments.toFixed(2)}</td>
                        <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{v.backupWithholdingAmount.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table></div>
              </>
            )}
        </CardContent>
      </Card>
    </div>
  )
}
