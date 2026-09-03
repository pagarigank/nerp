import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Scale,
  CheckCircle,
  XCircle,
  Clock,
  AlertTriangle,
  TrendingUp,
  ExternalLink,
  RefreshCw,
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import {
  getReconciliationReport,
  type ReconciliationReport,
  type ReconciliationItem,
  type ReconciliationDiscrepancy,
} from '@api/einvoice'

function getSeverityBadge(severity: string) {
  switch (severity) {
    case 'High':
      return <Badge variant="error" size="sm">{severity}</Badge>
    case 'Medium':
      return <Badge variant="warning" size="sm">{severity}</Badge>
    default:
      return <Badge variant="neutral" size="sm">{severity}</Badge>
  }
}

function getStatusBadge(status: string) {
  switch (status) {
    case 'Valid':
      return <Badge variant="success" size="sm" dot>Valid</Badge>
    case 'Invalid':
    case 'Rejected':
      return <Badge variant="error" size="sm" dot>{status}</Badge>
    case 'Cancelled':
      return <Badge variant="neutral" size="sm" dot>Cancelled</Badge>
    case 'Submitted':
    case 'Validating':
      return <Badge variant="info" size="sm" dot>{status}</Badge>
    default:
      return <Badge variant="neutral" size="sm">{status}</Badge>
  }
}

export function ReconciliationReportPage() {
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date()
    d.setDate(d.getDate() - 30)
    return d.toISOString().split('T')[0]
  })
  const [toDate, setToDate] = useState(() => new Date().toISOString().split('T')[0])

  // TODO: Replace with actual companyId from auth context
  const companyId = '00000000-0000-0000-0000-000000000001'

  const { data: report, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['einvoice', 'reconciliation', companyId, fromDate, toDate],
    queryFn: () => getReconciliationReport(companyId, fromDate, toDate),
  })

  const [activeTab, setActiveTab] = useState<'overview' | 'discrepancies' | 'items'>('overview')

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            Reconciliation Report
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Compare ERP-posted documents against MyInvois-confirmed e-Invoices
          </p>
        </div>
      </div>

      {/* Date Range + Refresh */}
      <Card>
        <CardContent>
          <div className="flex flex-col sm:flex-row gap-3 items-end">
            <Input
              label="From Date"
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
            />
            <Input
              label="To Date"
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
            />
            <Button
              variant="primary"
              size="sm"
              onClick={() => refetch()}
              isLoading={isFetching}
              leftIcon={<RefreshCw className="h-4 w-4" />}
            >
              Generate Report
            </Button>
          </div>
        </CardContent>
      </Card>

      {isLoading ? (
        <SkeletonTable columns={4} />
      ) : report ? (
        <>
          {/* Summary Stats */}
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            <Card className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-blue-50 dark:bg-blue-900/20">
                  <Scale className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                </div>
                <div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">Total</p>
                  <p className="text-xl font-bold text-gray-900 dark:text-white">{report.summary.totalDocuments}</p>
                </div>
              </div>
            </Card>
            <Card className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-green-50 dark:bg-green-900/20">
                  <CheckCircle className="h-5 w-5 text-green-600 dark:text-green-400" />
                </div>
                <div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">Valid</p>
                  <p className="text-xl font-bold text-green-600 dark:text-green-400">{report.summary.validDocuments}</p>
                </div>
              </div>
            </Card>
            <Card className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-red-50 dark:bg-red-900/20">
                  <XCircle className="h-5 w-5 text-red-600 dark:text-red-400" />
                </div>
                <div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">Invalid</p>
                  <p className="text-xl font-bold text-red-600 dark:text-red-400">{report.summary.invalidDocuments}</p>
                </div>
              </div>
            </Card>
            <Card className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-amber-50 dark:bg-amber-900/20">
                  <Clock className="h-5 w-5 text-amber-600 dark:text-amber-400" />
                </div>
                <div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">Pending</p>
                  <p className="text-xl font-bold text-amber-600 dark:text-amber-400">{report.summary.pendingDocuments}</p>
                </div>
              </div>
            </Card>
            <Card className="p-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-emerald-50 dark:bg-emerald-900/20">
                  <TrendingUp className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
                </div>
                <div>
                  <p className="text-xs text-gray-500 dark:text-gray-400">Success Rate</p>
                  <p className="text-xl font-bold text-emerald-600 dark:text-emerald-400">{report.summary.successRate}%</p>
                </div>
              </div>
            </Card>
          </div>

          {/* Amount Summary */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Card className="p-4">
              <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">Total Amount</p>
              <p className="text-lg font-bold text-gray-900 dark:text-white">
                MYR {report.summary.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
              </p>
            </Card>
            <Card className="p-4">
              <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">Valid Amount</p>
              <p className="text-lg font-bold text-green-600 dark:text-green-400">
                MYR {report.summary.validAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
              </p>
            </Card>
            <Card className="p-4">
              <p className="text-xs text-gray-500 dark:text-gray-400 mb-1">Invalid Amount</p>
              <p className="text-lg font-bold text-red-600 dark:text-red-400">
                MYR {report.summary.invalidAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
              </p>
            </Card>
          </div>

          {/* Tabs */}
          <div className="border-b border-gray-200 dark:border-gray-700">
            <nav className="flex gap-6">
              {[
                { key: 'overview' as const, label: 'Overview' },
                { key: 'discrepancies' as const, label: `Discrepancies (${report.discrepancies.length})` },
                { key: 'items' as const, label: `All Documents (${report.items.length})` },
              ].map(tab => (
                <button
                  key={tab.key}
                  onClick={() => setActiveTab(tab.key)}
                  className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors ${
                    activeTab === tab.key
                      ? 'border-emerald-500 text-emerald-600 dark:text-emerald-400'
                      : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </nav>
          </div>

          {/* Tab Content */}
          {activeTab === 'overview' && (
            <Card>
              <CardHeader title="Reconciliation Overview" />
              <CardContent>
                <div className="space-y-4">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600 dark:text-gray-400">Success Rate</span>
                    <span className="font-bold text-gray-900 dark:text-white">{report.summary.successRate}%</span>
                  </div>
                  <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-3">
                    <div
                      className={`h-3 rounded-full transition-all ${
                        report.summary.successRate >= 90
                          ? 'bg-green-500'
                          : report.summary.successRate >= 70
                            ? 'bg-yellow-500'
                            : 'bg-red-500'
                      }`}
                      style={{ width: `${report.summary.successRate}%` }}
                    />
                  </div>

                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                    <div className="p-3 rounded-lg bg-gray-50 dark:bg-gray-800">
                      <p className="text-gray-500 dark:text-gray-400">Report Period</p>
                      <p className="font-medium text-gray-900 dark:text-white">
                        {new Date(report.fromDate).toLocaleDateString()} — {new Date(report.toDate).toLocaleDateString()}
                      </p>
                    </div>
                    <div className="p-3 rounded-lg bg-gray-50 dark:bg-gray-800">
                      <p className="text-gray-500 dark:text-gray-400">Generated</p>
                      <p className="font-medium text-gray-900 dark:text-white">
                        {new Date(report.generatedAt).toLocaleString()}
                      </p>
                    </div>
                    <div className="p-3 rounded-lg bg-gray-50 dark:bg-gray-800">
                      <p className="text-gray-500 dark:text-gray-400">Discrepancies</p>
                      <p className="font-medium text-gray-900 dark:text-white">{report.discrepancies.length}</p>
                    </div>
                    <div className="p-3 rounded-lg bg-gray-50 dark:bg-gray-800">
                      <p className="text-gray-500 dark:text-gray-400">Cancelled</p>
                      <p className="font-medium text-gray-900 dark:text-white">{report.summary.cancelledDocuments}</p>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {activeTab === 'discrepancies' && (
            <Card>
              <CardHeader
                title="Discrepancies"
                description={`${report.discrepancies.length} issue(s) found`}
              />
              <CardContent>
                {report.discrepancies.length === 0 ? (
                  <div className="text-center py-8">
                    <CheckCircle className="h-10 w-10 text-green-500 mx-auto mb-3" />
                    <p className="text-sm text-gray-500 dark:text-gray-400">
                      No discrepancies found — all documents reconciled!
                    </p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {report.discrepancies.map((disc, idx) => (
                      <div
                        key={`${disc.documentId}-${idx}`}
                        className={`p-4 rounded-lg border ${
                          disc.severity === 'High'
                            ? 'border-red-200 bg-red-50/50 dark:border-red-800 dark:bg-red-900/10'
                            : 'border-yellow-200 bg-yellow-50/50 dark:border-yellow-800 dark:bg-yellow-900/10'
                        }`}
                      >
                        <div className="flex items-start justify-between">
                          <div>
                            <div className="flex items-center gap-2 mb-1">
                              {getSeverityBadge(disc.severity)}
                              <Badge variant="neutral" size="sm">{disc.type}</Badge>
                            </div>
                            <p className="text-sm font-medium text-gray-900 dark:text-white">{disc.description}</p>
                            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">{disc.detail}</p>
                            {disc.errorCode && (
                              <p className="text-xs text-red-600 dark:text-red-400 mt-1">Error Code: {disc.errorCode}</p>
                            )}
                          </div>
                          <div className="text-right">
                            <p className="text-sm font-medium text-gray-900 dark:text-white">
                              MYR {disc.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                            </p>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {activeTab === 'items' && (
            <Card>
              <CardHeader title="All Documents" />
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                        <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                        <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                        <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Source</th>
                        <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                        <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">UUID</th>
                        <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Submitted</th>
                        <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Validated</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                      {report.items.map((item) => (
                        <tr key={item.documentId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                          <td className="px-3 py-3">{getStatusBadge(item.status)}</td>
                          <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{item.documentType}</td>
                          <td className="px-3 py-3 text-gray-500 dark:text-gray-400">{item.sourceDocumentType}</td>
                          <td className="px-3 py-3 text-right font-medium text-gray-900 dark:text-white">
                            {item.currencyCode} {item.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                          </td>
                          <td className="px-3 py-3 font-mono text-xs text-gray-500 dark:text-gray-400">
                            {item.myInvoisUuid ? item.myInvoisUuid.slice(0, 8) + '...' : '—'}
                          </td>
                          <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                            {item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}
                          </td>
                          <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                            {item.validatedAt ? new Date(item.validatedAt).toLocaleDateString() : '—'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}
        </>
      ) : null}
    </div>
  )
}
