import { useQuery } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { getGlTieOut } from '@api/inventory'

export function GlTieOutPage() {
  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'gl-tie-out'], queryFn: () => getGlTieOut() })

  const total = rows.filter(r => r.glAccountNumber === 'TOTAL').reduce((s, r) => s + r.subLedgerValue, 0)
  const byAccount = rows.filter(r => r.glAccountNumber !== 'TOTAL')

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Inventory GL Tie-Out" description="Perpetual sub-ledger value rolled up by GL inventory-asset account. Must equal the GL account balance at period close." />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            <div className="overflow-x-auto"><table className="w-full text-sm">
              <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                <th className="px-3 py-2 font-medium text-gray-500">GL Account</th>
                <th className="px-3 py-2 font-medium text-gray-500 text-right">Sub-ledger Value</th>
              </tr></thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {byAccount.map((r, i) => (
                  <tr key={i} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.glAccountNumber}</td>
                    <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.subLedgerValue.toFixed(2)}</td>
                  </tr>
                ))}
                <tr className="bg-gray-50 dark:bg-gray-800/60 font-semibold">
                  <td className="px-3 py-3">TOTAL</td>
                  <td className="px-3 py-3 text-right">{total.toFixed(2)}</td>
                </tr>
              </tbody>
            </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
