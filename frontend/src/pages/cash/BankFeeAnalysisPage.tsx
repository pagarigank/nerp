import { currentCompanyId } from '@/api/company'
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { BarChart3 } from 'lucide-react'
import { formatCurrency } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { getBankFeeAnalysis} from '@api/cash'
import type { BankFeeType } from '@/types/cash'

const feeTypeLabels: Record<BankFeeType, string> = {
  ServiceCharge: 'Service Charge',
  WireFee: 'Wire Fee',
  ACHFee: 'ACH Fee',
  OverdraftFee: 'Overdraft Fee',
  NsfFee: 'NSF Fee',
  CreditCardProcessing: 'Card Processing',
  Other: 'Other',
}

export function BankFeeAnalysisPage() {
  const [year, setYear] = useState(new Date().getFullYear())
  const [month, setMonth] = useState(new Date().getMonth() + 1)

  const { data, isLoading } = useQuery({
    queryKey: ['cash', 'fee-analysis', year, month, currentCompanyId()],
    queryFn: () => getBankFeeAnalysis(year, month),
  })

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold flex items-center gap-2">
        <BarChart3 className="h-5 w-5" /> Bank Fee Analysis
      </h1>

      <div className="flex gap-3">
        <Input label="Year" type="number" value={String(year)} onChange={(e) => setYear(Number(e.target.value))} className="w-28" />
        <Input label="Month" type="number" min={1} max={12} value={String(month)} onChange={(e) => setMonth(Number(e.target.value))} className="w-28" />
      </div>

      <Card>
        <CardHeader title={`Fee Totals (${year}-${String(month).padStart(2, '0')}) — Total ${data ? formatCurrency(data.totalFees) : '—'}`} />
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-gray-500">Loading…</p>
          ) : !data || data.lines.length === 0 ? (
            <p className="text-sm text-gray-500">No fees recorded for this period.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-gray-500">
                  <th className="py-2">Fee Type</th>
                  <th>Count</th>
                  <th className="text-right">Total</th>
                </tr>
              </thead>
              <tbody>
                {data.lines.map((l) => (
                  <tr key={l.id} className="border-b">
                    <td className="py-2">{feeTypeLabels[l.feeType as BankFeeType] ?? l.feeType}</td>
                    <td>{l.count}</td>
                    <td className="text-right">{formatCurrency(l.amount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
