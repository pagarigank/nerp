import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Calculator, Play } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { previewRevaluation, executeRevaluation } from '@api/gl'
import { getFiscalPeriods, companyId } from '@api/platform'
import type { RevaluationPreview, RevaluationResult } from '@/types/gl'

function todayIso(): string { return new Date().toISOString().split('T')[0] ?? '' }

export function RevaluationPage() {
  const qc = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)
  const [fiscalPeriodId, setFiscalPeriodId] = useState('')
  const [revaluationDate, setRevaluationDate] = useState(todayIso())
  const [reason, setReason] = useState('')
  const [preview, setPreview] = useState<RevaluationPreview | null>(null)
  const [result, setResult] = useState<RevaluationResult | null>(null)

  const { data: periods = [] } = useQuery({ queryKey: ['platform', 'fiscal-periods'], queryFn: () => getFiscalPeriods() })

  const previewMut = useMutation({
    mutationFn: () => previewRevaluation({ companyId: companyId(), fiscalPeriodId, revaluationDate }),
    onSuccess: (d) => { setPreview(d); setResult(null); setFormError(null) },
    onError: (e) => { setPreview(null); setFormError(getErrorMessage(e)) },
  })
  const executeMut = useMutation({
    mutationFn: () => executeRevaluation({ companyId: companyId(), fiscalPeriodId, revaluationDate, revaluationReason: reason || 'Period-end revaluation' }),
    onSuccess: (d) => { setResult(d); setPreview(null); qc.invalidateQueries(); setFormError(null) },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const periodOptions = periods.map(p => ({ value: p.id, label: `Period ${p.periodNumber} (${new Date(p.startDate).toLocaleDateString()}–${new Date(p.endDate).toLocaleDateString()})` }))
  const canRun = Boolean(fiscalPeriodId)

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader title="Multi-Currency Revaluation" description="Compute and post realized/unrealized FX gain-loss for open balances" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Select label="Fiscal Period" value={fiscalPeriodId} onChange={e => setFiscalPeriodId(e.target.value)} options={periodOptions} required />
            <Input type="date" label="Revaluation Date" value={revaluationDate} onChange={e => setRevaluationDate(e.target.value)} required />
            <Input label="Reason" value={reason} onChange={e => setReason(e.target.value)} placeholder="Period-end revaluation" />
          </div>
          <div className="mt-4 flex gap-2">
            <Button variant="outline" onClick={() => previewMut.mutate()} disabled={!canRun || previewMut.isPending}>
              <Calculator className="h-4 w-4" /> Preview
            </Button>
            <Button variant="primary" onClick={() => executeMut.mutate()} disabled={!canRun || executeMut.isPending || preview == null}>
              <Play className="h-4 w-4" /> Execute Revaluation
            </Button>
          </div>
        </CardContent>
      </Card>

      {preview && (
        <Card>
          <CardHeader title="Revaluation Preview" description={`${preview.linesToRevalue} line(s) • Estimated gain/loss ${preview.estimatedGainLoss.toFixed(2)}`} />
          <CardContent>
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">Account</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Orig Debit</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Orig Credit</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Revalued Debit</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Revalued Credit</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Gain/Loss</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {preview.lines.map(l => (
                  <tr key={l.accountId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{l.accountNumber}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{l.originalDebit.toFixed(2)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{l.originalCredit.toFixed(2)}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{l.revaluedDebit.toFixed(2)}</td>
                    <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{l.revaluedCredit.toFixed(2)}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{l.gainLoss.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table></div>
          </CardContent>
        </Card>
      )}

      {result && (
        <Card>
          <CardHeader title="Revaluation Posted" description={`Batch ${result.batchNumber} • ${result.linesRevalued} line(s) revalued`} />
          <CardContent>
            <div className="flex items-center gap-3">
              <Badge variant="success" size="sm" dot>Posted</Badge>
              <span className="text-sm text-gray-700 dark:text-gray-300">Total gain/loss: <strong>{result.totalGainLoss.toFixed(2)}</strong></span>
            </div>
            <p className="mt-2 text-xs text-gray-500">Batch ID: {result.batchId}</p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
