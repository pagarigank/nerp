import { BarChart3, ExternalLink } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'

export function TaxReportsPage() {
  return (
    <div className="space-y-6">
      <Card>
        <CardHeader
          title="Tax Reports"
          description="SST-related reports and analytics"
        />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <a href="/tax/sst-returns" className="block p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
              <div className="flex items-center gap-3">
                <BarChart3 className="h-5 w-5 text-emerald-500" />
                <div>
                  <p className="text-sm font-medium text-gray-900 dark:text-white">SST Returns (SST-02/SST-02A)</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">View and file bi-monthly SST returns</p>
                </div>
                <ExternalLink className="h-3.5 w-3.5 text-gray-400 ml-auto" />
              </div>
            </a>
            <a href="/tax/tax-codes" className="block p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
              <div className="flex items-center gap-3">
                <BarChart3 className="h-5 w-5 text-blue-500" />
                <div>
                  <p className="text-sm font-medium text-gray-900 dark:text-white">Tax Code Usage</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">View active tax codes and rates</p>
                </div>
                <ExternalLink className="h-3.5 w-3.5 text-gray-400 ml-auto" />
              </div>
            </a>
            <a href="/tax/tax-rates" className="block p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
              <div className="flex items-center gap-3">
                <BarChart3 className="h-5 w-5 text-amber-500" />
                <div>
                  <p className="text-sm font-medium text-gray-900 dark:text-white">Tax Rate History</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">Effective-dating audit trail for rate changes</p>
                </div>
                <ExternalLink className="h-3.5 w-3.5 text-gray-400 ml-auto" />
              </div>
            </a>
            <a href="/tax/local-tables" className="block p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
              <div className="flex items-center gap-3">
                <BarChart3 className="h-5 w-5 text-violet-500" />
                <div>
                  <p className="text-sm font-medium text-gray-900 dark:text-white">LHDN Code Tables</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400">Reference codes for MyInvois compliance</p>
                </div>
                <ExternalLink className="h-3.5 w-3.5 text-gray-400 ml-auto" />
              </div>
            </a>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
