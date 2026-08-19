import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { getFiscalPeriods } from '@api/platform'
import { getPeriodEndChecklist } from '@api/gl'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge } from '@components/ui/Badge'
import { Combobox } from '@components/ui/Combobox'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { CheckCircle2, XCircle } from 'lucide-react'
import type { PeriodEndChecklistItem } from '@/types/gl'

export function PeriodEndChecklistPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const [fiscalPeriodId, setFiscalPeriodId] = useState('')
  const { data: periods = [] } = useQuery({
    queryKey: ['fiscalPeriods', companyId],
    queryFn: () => getFiscalPeriods(companyId),
    enabled: !!companyId,
  })
  const { data: items = [], isLoading } = useQuery({
    queryKey: ['checklist', companyId, fiscalPeriodId],
    queryFn: () => getPeriodEndChecklist(companyId, fiscalPeriodId),
    enabled: !!companyId && !!fiscalPeriodId,
  })

  const allPassed = (items as PeriodEndChecklistItem[]).every((i) => i.passed)

  const periodOptions = useMemo(
    () => periods.map(p => ({ value: p.id, label: `P${p.periodNumber} - ${p.description}` })),
    [periods]
  )

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Period-End Close Checklist</h1>
      <p className="text-sm text-gray-500 dark:text-gray-400">Tie-out across GL and sub-ledgers before closing the period.</p>
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
        <CardHeader title="Checklist" />
        <CardContent>
          {isLoading && <SkeletonTable columns={3} />}
          <ul className="divide-y">
            {items.map((it) => (
              <li key={it.name} className="flex items-start gap-3 py-3">
                {it.passed ? <CheckCircle2 className="mt-0.5 h-5 w-5 text-green-600" /> : <XCircle className="mt-0.5 h-5 w-5 text-red-600" />}
                <div className="flex-1">
                  <div className="font-medium">{it.name} <Badge variant={it.passed ? 'success' : 'error'}>{it.passed ? 'Pass' : 'Fail'}</Badge></div>
                  <div className="text-sm text-gray-500">{it.detail}</div>
                </div>
              </li>
            ))}
            {items.length === 0 && <li className="py-3 text-center text-gray-500">Select a period to run the checklist.</li>}
          </ul>
          {items.length > 0 && (
            <div className={`mt-3 rounded-md p-3 text-sm ${allPassed ? 'bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-300' : 'bg-amber-50 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300'}`}>
              {allPassed ? 'All checks passed — period is ready to close.' : 'One or more checks failed — resolve before closing.'}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
