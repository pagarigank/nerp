import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Check, ChevronRight, Lock } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getFiscalPeriods,
  getPeriodClosePreview,
  closeFiscalPeriod,
} from '@api/platform'
import type { FiscalPeriod, PeriodClosePreview } from '@/types/platform'

const REASON_CODES = [
  'Month-end close',
  'Quarter-end close',
  'Year-end close',
  'Adjustment posting complete',
  'Audit sign-off',
]

export function PeriodCloseWizardPage() {
  const qc = useQueryClient()
  const [step, setStep] = useState(0)
  const [selectedPeriodId, setSelectedPeriodId] = useState<string>('')
  const [reason, setReason] = useState<string>(REASON_CODES[0] ?? 'Month-end close')
  const [error, setError] = useState<string | null>(null)
  const [closed, setClosed] = useState<PeriodClosePreview | null>(null)

  const { data: periods = [] } = useQuery({
    queryKey: ['platform', 'fiscal-periods'],
    queryFn: () => getFiscalPeriods(),
  })

  const preview = useQuery({
    queryKey: ['period-close-preview', selectedPeriodId],
    queryFn: () => getPeriodClosePreview(selectedPeriodId),
    enabled: step >= 1 && !!selectedPeriodId,
  })

  const closeMut = useMutation({
    mutationFn: () => closeFiscalPeriod(selectedPeriodId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['platform', 'fiscal-periods'] })
      setClosed(preview.data ?? null)
      setStep(3)
    },
    onError: (e) => setError(getErrorMessage(e)),
  })

  const selected = periods.find((p: FiscalPeriod) => p.id === selectedPeriodId)
  const openPeriods = periods.filter((p: FiscalPeriod) => p.status === 1 || (p as unknown as { status?: string }).status === 'Open')

  const reset = () => {
    setStep(0)
    setSelectedPeriodId('')
    setReason(REASON_CODES[0] ?? 'Month-end close')
    setError(null)
    setClosed(null)
  }

  return (
    <div className="space-y-6 max-w-3xl">
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{error}</span>
        </div>
      )}

      <Card>
        <CardHeader title="Period Close Wizard" description="Close a fiscal period after confirming it is ready" />
        <CardContent>
          {/* Stepper */}
          <ol className="flex items-center gap-2 mb-6 text-sm">
            {['Select period', 'Review readiness', 'Confirm & close', 'Done'].map((label, i) => (
              <li key={label} className="flex items-center gap-2">
                <span
                  className={`flex h-7 w-7 items-center justify-center rounded-full text-xs font-semibold ${
                    i < step ? 'bg-green-600 text-white' : i === step ? 'bg-blue-600 text-white' : 'bg-gray-200 text-gray-500 dark:bg-gray-700'
                  }`}
                >
                  {i < step ? <Check className="h-4 w-4" /> : i + 1}
                </span>
                <span className={i === step ? 'font-medium text-gray-900 dark:text-white' : 'text-gray-500'}>{label}</span>
                {i < 3 && <ChevronRight className="h-4 w-4 text-gray-300" />}
              </li>
            ))}
          </ol>

          {step === 0 && (
            <div className="space-y-4">
              <label className="block">
                <span className="block font-medium mb-1.5 text-sm text-gray-700 dark:text-gray-300">Fiscal period</span>
                <select
                  className="w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
                  value={selectedPeriodId}
                  onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setSelectedPeriodId(e.target.value)}
                >
                  <option value="">Choose a period…</option>
                  {openPeriods.map((p: FiscalPeriod) => (
                    <option key={p.id} value={p.id}>
                      {`P${p.periodNumber} · ${p.description} (${new Date(p.startDate).toLocaleDateString()} – ${new Date(p.endDate).toLocaleDateString()})`}
                    </option>
                  ))}
                </select>
              </label>
              <div className="flex justify-end">
                <Button variant="primary" disabled={!selectedPeriodId} onClick={() => setStep(1)}>
                  Next
                </Button>
              </div>
            </div>
          )}

          {step === 1 && (
            <div className="space-y-4">
              {preview.isLoading ? (
                <p className="text-sm text-gray-500">Checking readiness…</p>
              ) : preview.data ? (
                <div className="space-y-3">
                  <div className="rounded-lg border border-gray-200 dark:border-gray-700 p-4">
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      {preview.data.description}
                    </p>
                    <p className="text-xs text-gray-500">
                      {new Date(preview.data.startDate).toLocaleDateString()} – {new Date(preview.data.endDate).toLocaleDateString()}
                    </p>
                  </div>
                  <div className="flex items-center justify-between rounded-lg bg-gray-50 dark:bg-gray-800/50 p-3 text-sm">
                    <span className="text-gray-600 dark:text-gray-300">Pending GL batches</span>
                    <Badge variant={preview.data.pendingGlBatches === 0 ? 'success' : 'warning'}>
                      {preview.data.pendingGlBatches}
                    </Badge>
                  </div>
                  {preview.data.pendingGlBatches > 0 && (
                    <div className="flex items-center gap-2 text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg p-3">
                      <AlertCircle className="h-4 w-4" /> There are open sub-ledger batches. Close them before proceeding, or force-close anyway.
                    </div>
                  )}
                  {preview.data.warnings.map((w) => (
                    <p key={w} className="text-sm text-gray-500">• {w}</p>
                  ))}
                </div>
              ) : null}
              <div className="flex justify-between">
                <Button variant="secondary" onClick={() => setStep(0)}>
                  Back
                </Button>
                <Button variant="primary" disabled={preview.isLoading} onClick={() => setStep(2)}>
                  Next
                </Button>
              </div>
            </div>
          )}

          {step === 2 && (
            <div className="space-y-4">
              <label className="block">
                <span className="block font-medium mb-1.5 text-sm text-gray-700 dark:text-gray-300">Close reason</span>
                <select
                  className="w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
                  value={reason}
                  onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setReason(e.target.value)}
                >
                  {REASON_CODES.map((r) => (
                    <option key={r} value={r}>{r}</option>
                  ))}
                </select>
              </label>
              <div className="rounded-lg border border-gray-200 dark:border-gray-700 p-4 text-sm text-gray-600 dark:text-gray-300">
                You are about to close <span className="font-medium">{selected?.description}</span>. Closing prevents further posting to this
                period. A reversal entry is required to correct any post-close adjustment.
              </div>
              <div className="flex justify-between">
                <Button variant="secondary" onClick={() => setStep(1)} disabled={closeMut.isPending}>
                  Back
                </Button>
                <Button variant="primary" onClick={() => closeMut.mutate()} isLoading={closeMut.isPending} leftIcon={<Lock className="h-4 w-4" />}>
                  Close period
                </Button>
              </div>
            </div>
          )}

          {step === 3 && (
            <div className="space-y-4 text-center">
              <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-green-100 text-green-600">
                <Check className="h-6 w-6" />
              </div>
              <p className="text-sm text-gray-700 dark:text-gray-200">
                Period <span className="font-medium">{closed?.description ?? selected?.description}</span> has been closed.
              </p>
              <div className="flex justify-center">
                <Button variant="secondary" onClick={reset}>
                  Close another period
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
