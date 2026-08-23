import { useEffect, useState } from 'react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { DataTable } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import { formatCurrency, formatDate } from '@utils/helpers'
import { getCommissionRuns } from '@api/orderManagement'
import type { CommissionRunSummary } from '@/types/orderManagement'

export function CommissionRunsPage() {
  const [rows, setRows] = useState<CommissionRunSummary[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getCommissionRuns()
      .then((res) => setRows((res as { data?: CommissionRunSummary[] }).data ?? (res as CommissionRunSummary[])))
      .catch((e) => setError(getErrorMessage(e)))
      .finally(() => setLoading(false))
  }, [])

  const columns = [
    { key: 'runNumber', header: 'Run #' },
    { key: 'periodStart', header: 'Period Start', render: (v: string) => formatDate(v) },
    { key: 'periodEnd', header: 'Period End', render: (v: string) => formatDate(v) },
    { key: 'repCount', header: 'Reps' },
    { key: 'totalRevenue', header: 'Revenue Base', align: 'right' as const, render: (v: number) => formatCurrency(v) },
    { key: 'totalCommission', header: 'Commission', align: 'right' as const, render: (v: number) => formatCurrency(v) },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Commission Runs</h1>
      <Card>
        <CardHeader title="Weekly Commission Runs" description={`${rows.length} run(s)`} />
        <CardContent>
          <DataTable
            columns={columns}
            data={rows}
            loading={loading}
            emptyMessage="No commission runs yet — the weekly job runs Mondays at 01:00 UTC"
          />
        </CardContent>
      </Card>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
