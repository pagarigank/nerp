import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Search,
  CheckCircle,
  XCircle,
  Clock,
  AlertTriangle,
  RefreshCw,
  ExternalLink,
  Copy,
  ArrowRight,
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getDocuments, type EInvoiceDocument } from '@api/einvoice'

const STATUS_TIMELINE = ['Queued', 'Submitted', 'Validating', 'Valid'] as const

function getStatusStep(status: string): number {
  const idx = STATUS_TIMELINE.indexOf(status as any)
  return idx >= 0 ? idx : -1
}

function getStatusColor(status: string) {
  switch (status) {
    case 'Valid':
      return 'text-green-600 dark:text-green-400'
    case 'Invalid':
    case 'Rejected':
      return 'text-red-600 dark:text-red-400'
    case 'Cancelled':
      return 'text-gray-500 dark:text-gray-400'
    case 'Submitted':
    case 'Validating':
      return 'text-blue-600 dark:text-blue-400'
    default:
      return 'text-gray-500 dark:text-gray-400'
  }
}

function getStatusBg(status: string) {
  switch (status) {
    case 'Valid':
      return 'bg-green-100 dark:bg-green-900/20'
    case 'Invalid':
    case 'Rejected':
      return 'bg-red-100 dark:bg-red-900/20'
    case 'Cancelled':
      return 'bg-gray-100 dark:bg-gray-800'
    case 'Submitted':
    case 'Validating':
      return 'bg-blue-100 dark:bg-blue-900/20'
    default:
      return 'bg-gray-100 dark:bg-gray-800'
  }
}

function TimelineStep({ label, isComplete, isCurrent, isFailed }: {
  label: string
  isComplete: boolean
  isCurrent: boolean
  isFailed: boolean
}) {
  return (
    <div className="flex items-center gap-2">
      <div
        className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-medium ${
          isComplete
            ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
            : isCurrent
              ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400 ring-2 ring-blue-300 dark:ring-blue-700'
              : isFailed
                ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
                : 'bg-gray-100 text-gray-400 dark:bg-gray-800 dark:text-gray-500'
        }`}
      >
        {isComplete ? '✓' : isFailed ? '✕' : isCurrent ? '●' : '○'}
      </div>
      <span
        className={`text-xs font-medium ${
          isComplete || isCurrent
            ? 'text-gray-900 dark:text-white'
            : 'text-gray-400 dark:text-gray-500'
        }`}
      >
        {label}
      </span>
    </div>
  )
}

export function SubmissionStatusPage() {
  const [search, setSearch] = useState('')

  // TODO: Replace with actual companyId from auth context
  const companyId = '00000000-0000-0000-0000-000000000001'

  const {
    data: documents = [],
    isLoading,
    refetch,
    isFetching,
  } = useQuery({
    queryKey: ['einvoice', 'documents', companyId],
    queryFn: () => getDocuments(companyId),
  })

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return documents
    return documents.filter(
      (d) =>
        d.myInvoisUuid?.toLowerCase().includes(q) ||
        d.documentType.toLowerCase().includes(q) ||
        d.sourceDocumentType.toLowerCase().includes(q)
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
      {/* Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
        {STATUS_TIMELINE.map((status) => (
          <Card key={status} className="p-3">
            <p className="text-xs text-gray-500 dark:text-gray-400">{status}</p>
            <p className={`text-xl font-semibold ${getStatusColor(status)}`}>
              {stats[status] || 0}
            </p>
          </Card>
        ))}
        <Card className="p-3">
          <p className="text-xs text-gray-500 dark:text-gray-400">Invalid/Rejected</p>
          <p className="text-xl font-semibold text-red-600 dark:text-red-400">
            {(stats['Invalid'] || 0) + (stats['Rejected'] || 0)}
          </p>
        </Card>
      </div>

      {/* Documents with Timeline */}
      <Card>
        <CardHeader
          title="Submission Status"
          description="Track the lifecycle of each e-Invoice submission"
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
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search by UUID or type..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search submissions"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={5} />
          ) : filtered.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No submissions found.
            </p>
          ) : (
            <div className="space-y-4">
              {filtered.map((doc) => {
                const currentStep = getStatusStep(doc.status)
                const isTerminal = ['Valid', 'Invalid', 'Rejected', 'Cancelled'].includes(doc.status)
                const isFailed = ['Invalid', 'Rejected'].includes(doc.status)

                return (
                  <div
                    key={doc.id}
                    className={`p-4 rounded-lg border transition-colors ${
                      isFailed
                        ? 'border-red-200 dark:border-red-800'
                        : doc.status === 'Valid'
                          ? 'border-green-200 dark:border-green-800'
                          : 'border-gray-200 dark:border-gray-700'
                    }`}
                  >
                    <div className="flex items-start justify-between mb-3">
                      <div className="flex items-center gap-3">
                        <Badge
                          variant={
                            doc.status === 'Valid'
                              ? 'success'
                              : isFailed
                                ? 'error'
                                : doc.status === 'Cancelled'
                                  ? 'neutral'
                                  : 'info'
                          }
                          size="sm"
                        >
                          {doc.status}
                        </Badge>
                        <span className="text-sm font-medium text-gray-900 dark:text-white">
                          {doc.documentType}
                        </span>
                        <span className="text-xs text-gray-500 dark:text-gray-400">
                          {doc.sourceDocumentType}
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        {doc.validationLink && (
                          <a
                            href={doc.validationLink}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1 text-xs text-emerald-600 hover:text-emerald-700 dark:text-emerald-400"
                          >
                            View on LHDN <ExternalLink className="h-3 w-3" />
                          </a>
                        )}
                      </div>
                    </div>

                    {/* Timeline */}
                    <div className="flex items-center gap-3 mb-3">
                      {STATUS_TIMELINE.map((step, idx) => (
                        <div key={step} className="flex items-center gap-3">
                          <TimelineStep
                            label={step}
                            isComplete={currentStep > idx}
                            isCurrent={currentStep === idx}
                            isFailed={isFailed && currentStep === idx}
                          />
                          {idx < STATUS_TIMELINE.length - 1 && (
                            <ArrowRight className="h-3 w-3 text-gray-300 dark:text-gray-600" />
                          )}
                        </div>
                      ))}
                      {isFailed && (
                        <>
                          <ArrowRight className="h-3 w-3 text-gray-300 dark:text-gray-600" />
                          <TimelineStep label={doc.status} isComplete={false} isCurrent={false} isFailed />
                        </>
                      )}
                    </div>

                    {/* Details */}
                    <div className="flex flex-wrap gap-4 text-xs text-gray-500 dark:text-gray-400">
                      {doc.myInvoisUuid && (
                        <div className="flex items-center gap-1">
                          <span className="font-medium">UUID:</span>
                          <code className="font-mono bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 rounded">
                            {doc.myInvoisUuid.slice(0, 12)}...
                          </code>
                          <button
                            onClick={() => navigator.clipboard.writeText(doc.myInvoisUuid!)}
                            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                          >
                            <Copy className="h-3 w-3" />
                          </button>
                        </div>
                      )}
                      <div>
                        <span className="font-medium">Amount:</span>{' '}
                        {doc.currencyCode} {doc.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </div>
                      {doc.submittedAt && (
                        <div>
                          <span className="font-medium">Submitted:</span>{' '}
                          {new Date(doc.submittedAt).toLocaleString()}
                        </div>
                      )}
                      {doc.validatedAt && (
                        <div>
                          <span className="font-medium">Validated:</span>{' '}
                          {new Date(doc.validatedAt).toLocaleString()}
                        </div>
                      )}
                    </div>

                    {/* Error Message */}
                    {doc.lhdnsErrorMessage && (
                      <div className="mt-3 p-2 rounded bg-red-50 dark:bg-red-900/10 border border-red-100 dark:border-red-900/30">
                        <div className="flex items-start gap-2">
                          <AlertTriangle className="h-3.5 w-3.5 text-red-500 mt-0.5 flex-shrink-0" />
                          <p className="text-xs text-red-700 dark:text-red-300">{doc.lhdnsErrorMessage}</p>
                        </div>
                      </div>
                    )}
                  </div>
                )
              })}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
