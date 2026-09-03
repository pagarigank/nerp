import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  FileText,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Clock,
  ExternalLink,
  Download,
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getDocuments, type EInvoiceDocument } from '@api/einvoice'

type ReportType = 'submission-status' | 'invalid-rejected'

function getStatusBadge(status: string) {
  switch (status) {
    case 'Valid': return <Badge variant="success" size="sm" dot>Valid</Badge>
    case 'Invalid': return <Badge variant="error" size="sm" dot>Invalid</Badge>
    case 'Rejected': return <Badge variant="error" size="sm" dot>Rejected</Badge>
    case 'Cancelled': return <Badge variant="neutral" size="sm" dot>Cancelled</Badge>
    case 'Submitted': return <Badge variant="info" size="sm" dot>Submitted</Badge>
    case 'Validating': return <Badge variant="info" size="sm" dot>Validating</Badge>
    default: return <Badge variant="neutral" size="sm">{status}</Badge>
  }
}

function exportToCSV(items: EInvoiceDocument[], filename: string) {
  const headers = ['Status', 'Type', 'Source Type', 'Amount', 'Currency', 'UUID', 'Submitted', 'Validated', 'Error']
  const rows = items.map(d => [
    d.status,
    d.documentType,
    d.sourceDocumentType,
    d.totalAmount.toFixed(2),
    d.currencyCode,
    d.myInvoisUuid ?? '',
    d.submittedAt ? new Date(d.submittedAt).toISOString() : '',
    d.validatedAt ? new Date(d.validatedAt).toISOString() : '',
    d.lhdnsErrorMessage ?? '',
  ])
  const csv = [headers.join(','), ...rows.map(r => r.map(c => `"${c}"`).join(','))].join('\n')
  const blob = new Blob([csv], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

export function ReportsPage() {
  const [reportType, setReportType] = useState<ReportType>('submission-status')
  const [fromDate, setFromDate] = useState(() => {
    const d = new Date(); d.setDate(d.getDate() - 30); return d.toISOString().split('T')[0]
  })
  const [toDate, setToDate] = useState(() => new Date().toISOString().split('T')[0])

  const companyId = '00000000-0000-0000-0000-000000000001'

  const { data: allDocs = [], isLoading } = useQuery({
    queryKey: ['einvoice', 'documents', companyId],
    queryFn: () => getDocuments(companyId),
  })

  const docs = allDocs.filter(d => {
    const created = new Date(d.createdAt)
    return created >= new Date(fromDate) && created <= new Date(toDate + 'T23:59:59')
  })

  const filteredDocs = reportType === 'invalid-rejected'
    ? docs.filter(d => d.status === 'Invalid' || d.status === 'Rejected')
    : docs

  const stats = {
    total: docs.length,
    valid: docs.filter(d => d.status === 'Valid').length,
    invalid: docs.filter(d => d.status === 'Invalid').length,
    rejected: docs.filter(d => d.status === 'Rejected').length,
    pending: docs.filter(d => ['Submitted', 'Validating', 'Queued'].includes(d.status)).length,
    cancelled: docs.filter(d => d.status === 'Cancelled').length,
    totalAmount: docs.reduce((s, d) => s + d.totalAmount, 0),
    validAmount: docs.filter(d => d.status === 'Valid').reduce((s, d) => s + d.totalAmount, 0),
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">e-Invoice Reports</h1>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Operational and compliance reports for MyInvois</p>
      </div>

      {/* Report Type Tabs */}
      <div className="border-b border-gray-200 dark:border-gray-700">
        <nav className="flex gap-6">
          <button onClick={() => setReportType('submission-status')}
            className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors ${
              reportType === 'submission-status'
                ? 'border-emerald-500 text-emerald-600 dark:text-emerald-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            }`}>
            <FileText className="h-4 w-4 inline mr-1" /> Submission Status
          </button>
          <button onClick={() => setReportType('invalid-rejected')}
            className={`py-3 px-1 text-sm font-medium border-b-2 transition-colors ${
              reportType === 'invalid-rejected'
                ? 'border-emerald-500 text-emerald-600 dark:text-emerald-400'
                : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400'
            }`}>
            <AlertTriangle className="h-4 w-4 inline mr-1" /> Invalid / Rejected
          </button>
        </nav>
      </div>

      {/* Filters */}
      <Card>
        <CardContent>
          <div className="flex flex-col sm:flex-row gap-3 items-end">
            <Input label="From Date" type="date" value={fromDate} onChange={e => setFromDate(e.target.value)} />
            <Input label="To Date" type="date" value={toDate} onChange={e => setToDate(e.target.value)} />
            <Button variant="secondary" size="sm" onClick={() => exportToCSV(filteredDocs, `${reportType}-report.csv`)}
              leftIcon={<Download className="h-4 w-4" />}>
              Export CSV
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card className="p-4">
          <p className="text-xs text-gray-500 dark:text-gray-400">Total</p>
          <p className="text-xl font-bold text-gray-900 dark:text-white">{stats.total}</p>
          <p className="text-xs text-gray-400 dark:text-gray-500">MYR {stats.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}</p>
        </Card>
        <Card className="p-4">
          <p className="text-xs text-gray-500 dark:text-gray-400">Valid</p>
          <p className="text-xl font-bold text-green-600 dark:text-green-400">{stats.valid}</p>
          <p className="text-xs text-gray-400 dark:text-gray-500">MYR {stats.validAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}</p>
        </Card>
        <Card className="p-4">
          <p className="text-xs text-gray-500 dark:text-gray-400">Invalid + Rejected</p>
          <p className="text-xl font-bold text-red-600 dark:text-red-400">{stats.invalid + stats.rejected}</p>
        </Card>
        <Card className="p-4">
          <p className="text-xs text-gray-500 dark:text-gray-400">Pending</p>
          <p className="text-xl font-bold text-amber-600 dark:text-amber-400">{stats.pending}</p>
        </Card>
      </div>

      {/* Results Table */}
      <Card>
        <CardHeader
          title={reportType === 'submission-status' ? 'Submission Status Report' : 'Invalid / Rejected Report'}
          description={`${filteredDocs.length} document(s) in selected period`}
        />
        <CardContent>
          {isLoading ? <SkeletonTable columns={7} /> : filteredDocs.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No documents found for the selected period.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">UUID</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Submitted</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Validated</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Error</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredDocs.map(doc => (
                    <tr key={doc.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3">{getStatusBadge(doc.status)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{doc.documentType}</td>
                      <td className="px-3 py-3 text-right font-medium text-gray-900 dark:text-white">
                        {doc.currencyCode} {doc.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-500 dark:text-gray-400">
                        {doc.myInvoisUuid ? doc.myInvoisUuid.slice(0, 8) + '...' : '—'}
                      </td>
                      <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                        {doc.submittedAt ? new Date(doc.submittedAt).toLocaleString() : '—'}
                      </td>
                      <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                        {doc.validatedAt ? new Date(doc.validatedAt).toLocaleString() : '—'}
                      </td>
                      <td className="px-3 py-3 max-w-[200px]">
                        {doc.lhdnsErrorMessage && (
                          <p className="text-xs text-red-600 dark:text-red-400 truncate" title={doc.lhdnsErrorMessage}>
                            {doc.lhdnsErrorMessage}
                          </p>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
