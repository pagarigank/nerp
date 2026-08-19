import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { CalendarClock, FileClock } from 'lucide-react'
import { formatCurrency } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getArAgingByBasis } from '@api/ar'

export function AgingByBasisPage() {
  const [basis, setBasis] = useState<'DueDate' | 'InvoiceDate'>('DueDate')

  const { data: report, isLoading } = useQuery({
    queryKey: ['ar', 'aging-by-basis', basis],
    queryFn: () => getArAgingByBasis(basis),
  })

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader
          title="AR Aging — By Basis"
          description="Compare aging by due date vs invoice date."
          action={
            <div className="flex gap-2">
              <Button
                variant={basis === 'DueDate' ? 'primary' : 'outline'}
                size="sm"
                leftIcon={<CalendarClock className="h-4 w-4" />}
                onClick={() => setBasis('DueDate')}
              >
                By Due Date
              </Button>
              <Button
                variant={basis === 'InvoiceDate' ? 'primary' : 'outline'}
                size="sm"
                leftIcon={<FileClock className="h-4 w-4" />}
                onClick={() => setBasis('InvoiceDate')}
              >
                By Invoice Date
              </Button>
            </div>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={2} />
          ) : !report ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No data.</p>
          ) : (
            <div className="space-y-4">
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
                {report.agingBreakdown.map(b => (
                  <div key={b.bucket} className="rounded-lg border border-gray-200 dark:border-gray-700 p-3">
                    <p className="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">{b.bucket}</p>
                    <p className="mt-1 text-lg font-bold tabular-nums text-gray-900 dark:text-white">{formatCurrency(b.outstanding)}</p>
                  </div>
                ))}
                <div className="rounded-lg border border-primary-300 dark:border-primary-700 bg-primary-50 dark:bg-primary-900/20 p-3">
                  <p className="text-xs font-medium uppercase tracking-wider text-primary-600 dark:text-primary-400">Total</p>
                  <p className="mt-1 text-lg font-bold tabular-nums text-primary-700 dark:text-primary-300">{formatCurrency(report.totalOutstanding)}</p>
                </div>
              </div>
              <p className="text-xs text-gray-500 dark:text-gray-400">Basis: <strong>{basis}</strong> · As of {new Date(report.asOfDate).toLocaleDateString()}</p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
