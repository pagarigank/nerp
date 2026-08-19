import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Eye, FileText } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Drawer } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getStatements, generateStatements, getStatement } from '@api/ar'
import type { ArStatement } from '@/types/ar'
import { statementStatusMap, invoiceStatusMap } from './statusMaps'
import { ArStatusBadge } from './ArStatusBadge'

interface GenerateResult {
  count: number
  asOfDate: string
}

export function StatementsPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [generateResult, setGenerateResult] = useState<GenerateResult | null>(null)
  const [previewStatement, setPreviewStatement] = useState<ArStatement | null>(null)

  const { data: statements = [], isLoading } = useQuery({
    queryKey: ['ar', 'statements'],
    queryFn: () => getStatements(),
  })

  const generateMutation = useMutation({
    mutationFn: generateStatements,
    onSuccess: result => {
      setError(null)
      setGenerateResult(result)
      queryClient.invalidateQueries({ queryKey: ['ar', 'statements'] })
    },
    onError: err => setError(getErrorMessage(err)),
  })

  return (
    <div className="space-y-6">
      {error && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Generate Statements"
          description="Create a statement for every customer with an open balance as of today."
          action={
            <Button
              variant="primary"
              size="sm"
              leftIcon={<FileText className="h-4 w-4" />}
              onClick={() => generateMutation.mutate(undefined)}
              isLoading={generateMutation.isPending}
            >
              Generate Statements
            </Button>
          }
        />
        {generateResult && (
          <CardContent>
            <div className="rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-800 dark:bg-emerald-900/20 dark:border-emerald-800 dark:text-emerald-300 p-4 text-sm">
              Generated <strong>{generateResult.count}</strong> statement(s) as of{' '}
              <strong>{formatDate(generateResult.asOfDate)}</strong>.
            </div>
          </CardContent>
        )}
      </Card>

      <Card>
        <CardHeader title="Statements" description={`${statements.length} statement(s) on file`} />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : statements.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No statements generated yet. Click "Generate Statements" to create them.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Statement #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">As Of</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Total Due</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {statements.map(statement => (
                    <tr key={statement.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-primary-600 dark:text-primary-400">
                        {statement.statementNumber}
                      </td>
                      <td className="px-3 py-3">
                        <p className="font-medium text-gray-900 dark:text-white">{statement.customerName}</p>
                        <p className="text-xs text-gray-500 dark:text-gray-400">{statement.customerCode}</p>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(statement.asOfDate)}</td>
                      <td className="px-3 py-3">
                        <ArStatusBadge value={statement.status} mapping={statementStatusMap} />
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(statement.totalDue)}
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end">
                          <Button
                            variant="outline"
                            size="sm"
                            leftIcon={<Eye className="h-4 w-4" />}
                            onClick={() => setPreviewStatement(statement)}
                          >
                            Preview
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <StatementPreview
        statement={previewStatement}
        onClose={() => setPreviewStatement(null)}
      />
    </div>
  )
}

interface StatementPreviewProps {
  statement: ArStatement | null
  onClose: () => void
}

function StatementPreview({ statement, onClose }: StatementPreviewProps) {
  const { data: detail, isLoading } = useQuery({
    queryKey: ['ar', 'statement', statement?.id],
    queryFn: () => getStatement(statement?.id ?? ''),
    enabled: !!statement,
  })

  return (
    <Drawer isOpen={!!statement} onClose={onClose} title="Statement Preview" size="lg">
      {isLoading ? (
        <SkeletonTable columns={5} rows={4} />
      ) : !detail ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">Statement unavailable.</p>
      ) : (
        <div className="space-y-5">
          <div className="flex items-start justify-between">
            <div>
              <p className="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">
                Statement
              </p>
              <h3 className="text-xl font-bold text-gray-900 dark:text-white mt-1">{detail.statementNumber}</h3>
            </div>
            <ArStatusBadge value={detail.status} mapping={statementStatusMap} />
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <p className="text-gray-500 dark:text-gray-400">Customer</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5">{detail.customerName}</p>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{detail.customerCode}</p>
            </div>
            <div className="text-right">
              <p className="text-gray-500 dark:text-gray-400">As of</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5">{formatDate(detail.asOfDate)}</p>
            </div>
          </div>

          <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 text-left bg-gray-50 dark:bg-gray-900/50">
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Invoice #</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Due</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Total</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Balance Due</th>
                  <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {detail.invoices.map(invoice => (
                  <tr key={invoice.invoiceNumber}>
                    <td className="px-3 py-2.5 font-medium text-primary-600 dark:text-primary-400">
                      {invoice.invoiceNumber}
                    </td>
                    <td className="px-3 py-2.5 text-gray-700 dark:text-gray-300">{formatDate(invoice.invoiceDate)}</td>
                    <td className="px-3 py-2.5 text-gray-700 dark:text-gray-300">{formatDate(invoice.dueDate)}</td>
                    <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                      {formatCurrency(invoice.totalAmount)}
                    </td>
                    <td className="px-3 py-2.5 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                      {formatCurrency(invoice.balanceDue)}
                    </td>
                    <td className="px-3 py-2.5">
                      <ArStatusBadge value={invoice.status} mapping={invoiceStatusMap} />
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
                  <td colSpan={4} className="px-3 py-2.5 text-right font-medium text-gray-700 dark:text-gray-300">
                    Total Due
                  </td>
                  <td className="px-3 py-2.5 text-right font-tabular tabular-nums font-semibold text-gray-900 dark:text-white">
                    {formatCurrency(detail.totalDue)}
                  </td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      )}
    </Drawer>
  )
}
