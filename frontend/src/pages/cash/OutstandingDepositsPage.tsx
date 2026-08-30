import { currentCompanyId } from '@/api/company'
import { useQuery } from '@tanstack/react-query'
import { Layers } from 'lucide-react'
import { formatCurrency } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { getOutstandingDeposits} from '@api/cash'

export function OutstandingDepositsPage() {
  const { data, isLoading } = useQuery({
    queryKey: ['cash', 'outstanding-deposits', currentCompanyId()],
    queryFn: () => getOutstandingDeposits(),
  })

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold flex items-center gap-2">
        <Layers className="h-5 w-5" /> Outstanding Deposits
      </h1>

      <Card>
        <CardHeader title={`Total Outstanding: ${data ? formatCurrency(data.totalOutstandingDeposits) : '—'}`} />
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-gray-500">Loading…</p>
          ) : !data || data.accounts.length === 0 ? (
            <p className="text-sm text-gray-500">No outstanding deposits.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-gray-500">
                  <th className="py-2">Account</th>
                  <th>Deposits</th>
                  <th className="text-right">Outstanding</th>
                </tr>
              </thead>
              <tbody>
                {data.accounts.map((a) => (
                  <tr key={a.bankAccountId} className="border-b">
                    <td className="py-2">{a.accountName}</td>
                    <td>{a.depositCount}</td>
                    <td className="text-right">{formatCurrency(a.outstandingDepositAmount)}</td>
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
