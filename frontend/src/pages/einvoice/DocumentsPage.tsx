import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Search,
  FileText,
  ExternalLink,
  CheckCircle,
  XCircle,
  Clock,
  AlertTriangle,
  RefreshCw,
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getDocuments, type EInvoiceDocument } from '@api/einvoice'

const STATUS_OPTIONS = [
  { value: '', label: 'All Statuses' },
  { value: 'Queued', label: 'Queued' },
  { value: 'Submitted', label: 'Submitted' },
  { value: 'Validating', label: 'Validating' },
  { value: 'Valid', label: 'Valid' },
  { value: 'Invalid', label: 'Invalid' },
  { value: 'Cancelled', label: 'Cancelled' },
  { value: 'Rejected', label: 'Rejected' },
]

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
    case 'Queued':
      return <Badge variant="warning" size="sm" dot>Queued</Badge>
    default:
      return <Badge variant="neutral" size="sm">{status}</Badge>
  }
}

function getStatusIcon(status: string) {
  switch (status) {
    case 'Valid':
      return <CheckCircle className="h-4 w-4 text-green-500" />
    case 'Invalid':
    case 'Rejected':
      return <XCircle className="h-4 w-4 text-red-500" />
    case 'Cancelled':
      return <AlertTriangle className="h-4 w-4 text-gray-400" />
    case 'Submitted':
    case 'Validating':
      return <Clock className="h-4 w-4 text-blue-500" />
    default:
      return <FileText className="h-4 w-4 text-gray-400" />
  }
}

export function DocumentsPage() {
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  // TODO: Replace with actual companyId from auth context
  const companyId = '00000000-0000-0000-0000-000000000001'

  const {
    data: documents = [],
    isLoading,
    refetch,
    isFetching,
  } = useQuery({
    queryKey: ['einvoice', 'documents', companyId, statusFilter],
    queryFn: () => getDocuments(companyId, statusFilter || undefined),
  })

  const filteredDocuments = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return documents
    return documents.filter(
      (d) =>
        d.myInvoisUuid?.toLowerCase().includes(q) ||
        d.sourceDocumentType.toLowerCase().includes(q) ||
        d.documentType.toLowerCase().includes(q) ||
        d.lhdnsErrorMessage?.toLowerCase().includes(q)
    )
  }, [documents, search])

  const stats = useMemo(() => {
    const counts: Record<string, number> = {}
    documents.forEach((d) => {
      counts[d.status] = (counts[d.status] || 0) + 1
    })
    return counts
  }, [documents])

  return (
    <div className="space-y-6">
      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-blue-50 dark:bg-blue-900/20">
              <FileText className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            </div>
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Total</p>
              <p className="text-2xl font-semibold text-gray-900 dark:text-white">{documents.length}</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-green-50 dark:bg-green-900/20">
              <CheckCircle className="h-5 w-5 text-green-600 dark:text-green-400" />
            </div>
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Valid</p>
              <p className="text-2xl font-semibold text-green-600 dark:text-green-400">{stats['Valid'] || 0}</p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-red-50 dark:bg-red-900/20">
              <XCircle className="h-5 w-5 text-red-600 dark:text-red-400" />
            </div>
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Invalid</p>
              <p className="text-2xl font-semibold text-red-600 dark:text-red-400">
                {(stats['Invalid'] || 0) + (stats['Rejected'] || 0)}
              </p>
            </div>
          </div>
        </Card>
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-amber-50 dark:bg-amber-900/20">
              <Clock className="h-5 w-5 text-amber-600 dark:text-amber-400" />
            </div>
            <div>
              <p className="text-sm text-gray-500 dark:text-gray-400">Pending</p>
              <p className="text-2xl font-semibold text-amber-600 dark:text-amber-400">
                {(stats['Queued'] || 0) + (stats['Submitted'] || 0) + (stats['Validating'] || 0)}
              </p>
            </div>
          </div>
        </Card>
      </div>

      {/* Documents Table */}
      <Card>
        <CardHeader
          title="e-Invoice Documents"
          description={`${documents.length} document(s) on file`}
          action={
            <Button
              variant="secondary"
              size="sm"
              onClick={() => refetch()}
              isLoading={isFetching}
              leftIcon={<RefreshCw className="h-4 w-4" />}
            >
              Refresh
            </Button>
          }
        />
        <CardContent>
          <div className="flex flex-col sm:flex-row gap-3 mb-4">
            <div className="flex-1 max-w-md">
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Search by UUID, type, or error..."
                leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
                aria-label="Search documents"
              />
            </div>
            <div className="flex gap-2 flex-wrap">
              {STATUS_OPTIONS.map((opt) => (
                <button
                  key={opt.value}
                  onClick={() => setStatusFilter(opt.value)}
                  className={`px-3 py-1.5 text-xs font-medium rounded-full transition-colors ${
                    statusFilter === opt.value
                      ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300'
                      : 'bg-gray-100 text-gray-600 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-400 dark:hover:bg-gray-700'
                  }`}
                >
                  {opt.label}
                </button>
              ))}
            </div>
          </div>

          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : filteredDocuments.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search || statusFilter
                ? 'No documents match your filters.'
                : 'No e-Invoice documents yet. Submit your first document from the Sales Invoice screen.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">MyInvois UUID</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Submitted</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Error</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredDocuments.map((doc) => (
                    <tr
                      key={doc.id}
                      className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                    >
                      <td className="px-3 py-3">
                        <div className="flex items-center gap-2">
                          {getStatusIcon(doc.status)}
                          {getStatusBadge(doc.status)}
                        </div>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{doc.documentType}</td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-500 dark:text-gray-400">
                        {doc.myInvoisUuid ? (
                          <span title={doc.myInvoisUuid}>{doc.myInvoisUuid.slice(0, 8)}...</span>
                        ) : (
                          '—'
                        )}
                      </td>
                      <td className="px-3 py-3 text-right font-medium text-gray-900 dark:text-white">
                        {doc.currencyCode} {doc.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </td>
                      <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                        {doc.submittedAt
                          ? new Date(doc.submittedAt).toLocaleString()
                          : '—'}
                      </td>
                      <td className="px-3 py-3 max-w-[200px]">
                        {doc.lhdnsErrorMessage && (
                          <p
                            className="text-xs text-red-600 dark:text-red-400 truncate"
                            title={doc.lhdnsErrorMessage}
                          >
                            {doc.lhdnsErrorMessage}
                          </p>
                        )}
                      </td>
                      <td className="px-3 py-3 text-right">
                        {doc.validationLink && (
                          <a
                            href={doc.validationLink}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1 text-xs text-emerald-600 hover:text-emerald-700 dark:text-emerald-400"
                          >
                            View <ExternalLink className="h-3 w-3" />
                          </a>
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
