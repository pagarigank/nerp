import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { getFiscalPeriods } from '@api/platform'
import { getPrePostingList } from '@api/gl'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge } from '@components/ui/Badge'
import { Combobox } from '@components/ui/Combobox'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { formatCurrency } from '@utils/helpers'
import type { PrePostingEditLine } from '@/types/gl'

export function PrePostingReviewPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const [fiscalPeriodId, setFiscalPeriodId] = useState('')
  const { data: periods = [] } = useQuery({
    queryKey: ['fiscalPeriods', companyId],
    queryFn: () => getFiscalPeriods(companyId),
    enabled: !!companyId,
  })
  const { data: lines = [], isLoading } = useQuery({
    queryKey: ['prePosting', companyId, fiscalPeriodId],
    queryFn: () => getPrePostingList(companyId, fiscalPeriodId),
    enabled: !!companyId && !!fiscalPeriodId,
  })

  const totalDebit = (lines as PrePostingEditLine[]).reduce((s, l) => s + l.debit, 0)
  const totalCredit = (lines as PrePostingEditLine[]).reduce((s, l) => s + l.credit, 0)

  const periodOptions = useMemo(
    () => periods.map(p => ({ value: p.id, label: `P${p.periodNumber} - ${p.description}` })),
    [periods]
  )

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Posting Edit List / Pre-Posting Review</h1>
      <p className="text-sm text-gray-500 dark:text-gray-400">Review all unposted (draft/balanced) journal batches for the selected period before posting.</p>
      <div className="w-80">
        <Combobox
          label="Fiscal Period"
          placeholder="Select period..."
          options={periodOptions}
          value={fiscalPeriodId}
          onChange={setFiscalPeriodId}
          required
        />
      </div>
      <Card>
        <CardHeader title={`Edit List (${lines.length} line(s))`} />
        <CardContent>
          {isLoading && <SkeletonTable columns={7} />}
          <table className="w-full text-sm">
            <thead><tr className="border-b text-left text-gray-500"><th className="py-2">Batch</th><th>Account</th><th className="text-right">Debit</th><th className="text-right">Credit</th><th>Reference</th><th>Segments</th><th>Status</th></tr></thead>
            <tbody>
              {lines.map((l, i) => (
                <tr key={i} className="border-b">
                  <td className="py-2">{l.batchNumber}</td>
                  <td>{l.accountNumber}</td>
                  <td className="text-right">{formatCurrency(l.debit)}</td>
                  <td className="text-right">{formatCurrency(l.credit)}</td>
                  <td>{l.reference}</td>
                  <td className="max-w-xs truncate">{l.segmentsJson}</td>
                  <td><Badge variant="warning">{l.status}</Badge></td>
                </tr>
              ))}
              {lines.length === 0 && <tr><td colSpan={7} className="py-3 text-center text-gray-500">No unposted lines.</td></tr>}
            </tbody>
            {lines.length > 0 && (
              <tfoot>
                <tr className="border-t font-semibold">
                  <td className="py-2" colSpan={2}>Totals</td>
                  <td className="text-right">{formatCurrency(totalDebit)}</td>
                  <td className="text-right">{formatCurrency(totalCredit)}</td>
                  <td colSpan={3}></td>
                </tr>
              </tfoot>
            )}
          </table>
        </CardContent>
      </Card>
    </div>
  )
}
