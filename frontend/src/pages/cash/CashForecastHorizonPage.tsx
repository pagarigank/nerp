import { currentCompanyId } from '@/api/company'
import { useQuery } from '@tanstack/react-query'
import { LineChart } from 'lucide-react'
import { formatCurrency } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { getCashForecastHorizon} from '@api/cash'

export function CashForecastHorizonPage() {
  const { data, isLoading } = useQuery({
    queryKey: ['cash', 'forecast-horizon', currentCompanyId()],
    queryFn: () => getCashForecastHorizon(),
  })

  const rows = data
    ? [
        { label: 'Today (reconciled cash)', value: data.todayCash },
        { label: '7-day projected (after payables)', value: data.next7DayCash },
        { label: '30-day projected (after collections)', value: data.next30DayCash },
        { label: 'Open payables', value: data.openPayablesNext30 * -1 },
        { label: 'Open receivables', value: data.openReceivablesNext30 },
      ]
    : []

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold flex items-center gap-2">
        <LineChart className="h-5 w-5" /> Cash Position by Forecast Horizon
      </h1>

      <Card>
        <CardHeader title="Cash Forecast" />
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-gray-500">Loading…</p>
          ) : (
            <div className="space-y-2">
              {rows.map((r) => (
                <div key={r.label} className="flex items-center justify-between border-b py-2 text-sm">
                  <span className="text-gray-600">{r.label}</span>
                  <span className="font-medium">{formatCurrency(r.value)}</span>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
