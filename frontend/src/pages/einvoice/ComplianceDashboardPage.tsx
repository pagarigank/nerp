import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Shield,
  ShieldCheck,
  ShieldOff,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Clock,
  FileText,
  Key,
  Calendar,
  TrendingUp,
  TrendingDown,
  ArrowRight,
  ExternalLink,
  RefreshCw,
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import {
  getDocuments,
  getCertificates,
  getCredentials,
  type EInvoiceDocument,
  type DigitalCertificate,
  type ApiCredential,
} from '@api/einvoice'

interface ComplianceMetrics {
  totalDocuments: number
  validCount: number
  invalidCount: number
  rejectedCount: number
  pendingCount: number
  cancelledCount: number
  successRate: number
  totalAmount: number
}

interface CertificateAlert {
  cert: DigitalCertificate
  daysUntilExpiry: number
  severity: 'expired' | 'critical' | 'warning' | 'ok'
}

function daysUntil(date: string): number {
  const diff = new Date(date).getTime() - Date.now()
  return Math.ceil(diff / (1000 * 60 * 60 * 24))
}

function getCertSeverity(notAfter: string): CertificateAlert['severity'] {
  const days = daysUntil(notAfter)
  if (days <= 0) return 'expired'
  if (days <= 7) return 'critical'
  if (days <= 30) return 'warning'
  return 'ok'
}

function getSeverityColor(severity: CertificateAlert['severity']) {
  switch (severity) {
    case 'expired':
      return 'bg-red-100 border-red-300 dark:bg-red-900/20 dark:border-red-800'
    case 'critical':
      return 'bg-orange-100 border-orange-300 dark:bg-orange-900/20 dark:border-orange-800'
    case 'warning':
      return 'bg-yellow-100 border-yellow-300 dark:bg-yellow-900/20 dark:border-yellow-800'
    default:
      return 'bg-green-50 border-green-200 dark:bg-green-900/10 dark:border-green-800'
  }
}

function getSeverityIcon(severity: CertificateAlert['severity']) {
  switch (severity) {
    case 'expired':
      return <ShieldOff className="h-5 w-5 text-red-600 dark:text-red-400" />
    case 'critical':
      return <AlertTriangle className="h-5 w-5 text-orange-600 dark:text-orange-400" />
    case 'warning':
      return <AlertTriangle className="h-5 w-5 text-yellow-600 dark:text-yellow-400" />
    default:
      return <ShieldCheck className="h-5 w-5 text-green-600 dark:text-green-400" />
  }
}

function StatCard({ label, value, icon: Icon, color, subtext }: {
  label: string
  value: string | number
  icon: any
  color: string
  subtext?: string
}) {
  return (
    <Card className="p-4">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-gray-500 dark:text-gray-400">{label}</p>
          <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1">{value}</p>
          {subtext && (
            <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">{subtext}</p>
          )}
        </div>
        <div className={`p-2 rounded-lg ${color}`}>
          <Icon className="h-5 w-5" />
        </div>
      </div>
    </Card>
  )
}

export function ComplianceDashboardPage() {
  // TODO: Replace with actual companyId from auth context
  const companyId = '00000000-0000-0000-0000-000000000001'

  const { data: documents = [], isLoading: docsLoading } = useQuery({
    queryKey: ['einvoice', 'documents', companyId],
    queryFn: () => getDocuments(companyId),
  })

  const { data: certificates = [], isLoading: certsLoading } = useQuery({
    queryKey: ['einvoice', 'certificates', companyId],
    queryFn: () => getCertificates(companyId),
  })

  const { data: credentials = [], isLoading: credsLoading } = useQuery({
    queryKey: ['einvoice', 'credentials', companyId],
    queryFn: () => getCredentials(companyId),
  })

  const metrics = useMemo<ComplianceMetrics>(() => {
    const total = documents.length
    const valid = documents.filter(d => d.status === 'Valid').length
    const invalid = documents.filter(d => d.status === 'Invalid').length
    const rejected = documents.filter(d => d.status === 'Rejected').length
    const pending = documents.filter(d =>
      ['Queued', 'Submitted', 'Validating'].includes(d.status)
    ).length
    const cancelled = documents.filter(d => d.status === 'Cancelled').length
    const totalAmount = documents.reduce((sum, d) => sum + d.totalAmount, 0)

    return {
      totalDocuments: total,
      validCount: valid,
      invalidCount: invalid,
      rejectedCount: rejected,
      pendingCount: pending,
      cancelledCount: cancelled,
      successRate: total > 0 ? Math.round((valid / total) * 100) : 0,
      totalAmount,
    }
  }, [documents])

  const certAlerts = useMemo<CertificateAlert[]>(() => {
    return certificates.map(cert => ({
      cert,
      daysUntilExpiry: daysUntil(cert.notAfter),
      severity: getCertSeverity(cert.notAfter),
    })).sort((a, b) => a.daysUntilExpiry - b.daysUntilExpiry)
  }, [certificates])

  const activeCreds = useMemo(() => {
    const active = credentials.filter(c => c.isActive)
    const expired = credentials.filter(c => !c.isActive)
    return { active, expired }
  }, [credentials])

  const isLoading = docsLoading || certsLoading || credsLoading

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Card key={i} className="p-4">
              <div className="animate-pulse space-y-2">
                <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/2" />
                <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded w-1/3" />
              </div>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            Compliance Dashboard
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            e-Invoice compliance overview for MyInvois
          </p>
        </div>
      </div>

      {/* Critical Alerts */}
      {certAlerts.some(a => a.severity === 'expired' || a.severity === 'critical') && (
        <div className="p-4 rounded-lg bg-red-50 border border-red-200 dark:bg-red-900/20 dark:border-red-800">
          <div className="flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-red-800 dark:text-red-200">
                Critical Certificate Alerts
              </p>
              <ul className="mt-2 space-y-1 text-sm text-red-700 dark:text-red-300">
                {certAlerts
                  .filter(a => a.severity === 'expired' || a.severity === 'critical')
                  .map(alert => (
                    <li key={alert.cert.id}>
                      {alert.severity === 'expired'
                        ? `EXPIRED: ${alert.cert.certificateName} — expired ${Math.abs(alert.daysUntilExpiry)} days ago`
                        : `CRITICAL: ${alert.cert.certificateName} — expires in ${alert.daysUntilExpiry} days`}
                    </li>
                  ))}
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Setup Incomplete Alerts */}
      {(!credentials.some(c => c.isActive) || !certificates.some(c => c.isActive)) && (
        <div className="p-4 rounded-lg bg-amber-50 border border-amber-200 dark:bg-amber-900/20 dark:border-amber-800">
          <div className="flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-400 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-amber-800 dark:text-amber-200">
                Setup Incomplete
              </p>
              <ul className="mt-2 space-y-1 text-sm text-amber-700 dark:text-amber-300">
                {!certificates.some(c => c.isActive) && (
                  <li>• No active digital certificate — documents cannot be signed</li>
                )}
                {!credentials.some(c => c.isActive) && (
                  <li>• No active API credentials — cannot authenticate with MyInvois</li>
                )}
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Metrics Grid */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard
          label="Total Documents"
          value={metrics.totalDocuments}
          icon={FileText}
          color="bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400"
          subtext={`${metrics.currencyCode || 'MYR'} ${metrics.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })} total`}
        />
        <StatCard
          label="Valid (Accepted)"
          value={metrics.validCount}
          icon={CheckCircle}
          color="bg-green-50 dark:bg-green-900/20 text-green-600 dark:text-green-400"
          subtext={`${metrics.successRate}% success rate`}
        />
        <StatCard
          label="Invalid + Rejected"
          value={metrics.invalidCount + metrics.rejectedCount}
          icon={XCircle}
          color="bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400"
          subtext={`${metrics.invalidCount} invalid, ${metrics.rejectedCount} rejected`}
        />
        <StatCard
          label="Pending"
          value={metrics.pendingCount}
          icon={Clock}
          color="bg-amber-50 dark:bg-amber-900/20 text-amber-600 dark:text-amber-400"
          subtext="Awaiting validation"
        />
      </div>

      {/* Second Row */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard
          label="Cancelled"
          value={metrics.cancelledCount}
          icon={AlertTriangle}
          color="bg-gray-50 dark:bg-gray-800 text-gray-600 dark:text-gray-400"
        />
        <StatCard
          label="Active Certificate"
          value={certificates.filter(c => c.isActive).length}
          icon={Shield}
          color="bg-emerald-50 dark:bg-emerald-900/20 text-emerald-600 dark:text-emerald-400"
          subtext={certAlerts.length > 0 && certAlerts[0].severity !== 'ok'
            ? `Nearest expiry: ${certAlerts[0].daysUntilExpiry}d`
            : undefined}
        />
        <StatCard
          label="API Credentials"
          value={activeCreds.active.length}
          icon={Key}
          color="bg-violet-50 dark:bg-violet-900/20 text-violet-600 dark:text-violet-400"
          subtext={`${activeCreds.expired.length} inactive`}
        />
        <StatCard
          label="Compliance Score"
          value={`${metrics.successRate}%`}
          icon={metrics.successRate >= 90 ? TrendingUp : TrendingDown}
          color={metrics.successRate >= 90
            ? 'bg-green-50 dark:bg-green-900/20 text-green-600 dark:text-green-400'
            : 'bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400'}
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Certificate Status */}
        <Card>
          <CardHeader
            title="Certificate Status"
            description={`${certificates.length} certificate(s) on file`}
          />
          <CardContent>
            {certAlerts.length === 0 ? (
              <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
                No certificates uploaded.
              </p>
            ) : (
              <div className="space-y-3">
                {certAlerts.map(({ cert, daysUntilExpiry, severity }) => (
                  <div
                    key={cert.id}
                    className={`p-3 rounded-lg border ${getSeverityColor(severity)}`}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        {getSeverityIcon(severity)}
                        <div>
                          <p className="text-sm font-medium text-gray-900 dark:text-white">
                            {cert.certificateName}
                          </p>
                          <p className="text-xs text-gray-500 dark:text-gray-400">
                            Issuer: {cert.issuer}
                          </p>
                        </div>
                      </div>
                      <div className="text-right">
                        {severity === 'expired' ? (
                          <Badge variant="error" size="sm">EXPIRED</Badge>
                        ) : severity === 'critical' ? (
                          <Badge variant="warning" size="sm">{daysUntilExpiry}d left</Badge>
                        ) : severity === 'warning' ? (
                          <Badge variant="info" size="sm">{daysUntilExpiry}d left</Badge>
                        ) : (
                          <Badge variant="success" size="sm">OK</Badge>
                        )}
                        <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
                          Expires: {new Date(cert.notAfter).toLocaleDateString()}
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Recent Invalid Documents */}
        <Card>
          <CardHeader
            title="Recent Errors"
            description={`${metrics.invalidCount + metrics.rejectedCount} document(s) with errors`}
          />
          <CardContent>
            {metrics.invalidCount + metrics.rejectedCount === 0 ? (
              <div className="text-center py-8">
                <CheckCircle className="h-10 w-10 text-green-500 mx-auto mb-3" />
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  No errors — all documents are valid!
                </p>
              </div>
            ) : (
              <div className="space-y-2 max-h-80 overflow-y-auto">
                {documents
                  .filter(d => d.status === 'Invalid' || d.status === 'Rejected')
                  .slice(0, 10)
                  .map(doc => (
                    <div
                      key={doc.id}
                      className="p-3 rounded-lg border border-red-200 dark:border-red-800 bg-red-50/50 dark:bg-red-900/10"
                    >
                      <div className="flex items-center justify-between">
                        <div>
                          <div className="flex items-center gap-2">
                            <Badge variant="error" size="sm">{doc.status}</Badge>
                            <span className="text-sm font-medium text-gray-900 dark:text-white">
                              {doc.documentType}
                            </span>
                          </div>
                          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                            {doc.currencyCode} {doc.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                            {doc.submittedAt && ` • ${new Date(doc.submittedAt).toLocaleDateString()}`}
                          </p>
                        </div>
                        {doc.validationLink && (
                          <a
                            href={doc.validationLink}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs text-emerald-600 hover:text-emerald-700 dark:text-emerald-400"
                          >
                            <ExternalLink className="h-3 w-3" />
                          </a>
                        )}
                      </div>
                      {doc.lhdnsErrorMessage && (
                        <p className="text-xs text-red-600 dark:text-red-400 mt-2 truncate" title={doc.lhdnsErrorMessage}>
                          {doc.lhdnsErrorMessage}
                        </p>
                      )}
                    </div>
                  ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* API Credential Status */}
      <Card>
        <CardHeader
          title="API Credential Status"
          description="MyInvois authentication status"
        />
        <CardContent>
          {credentials.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
              No API credentials configured.
            </p>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {credentials.map(cred => (
                <div
                  key={cred.id}
                  className={`p-3 rounded-lg border ${
                    cred.isActive
                      ? 'border-green-200 dark:border-green-800 bg-green-50/50 dark:bg-green-900/10'
                      : 'border-gray-200 dark:border-gray-700'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <Key className={`h-4 w-4 ${cred.isActive ? 'text-green-600 dark:text-green-400' : 'text-gray-400'}`} />
                      <div>
                        <p className="text-sm font-medium text-gray-900 dark:text-white">
                          {cred.clientId}
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-400">
                          {cred.environment}
                        </p>
                      </div>
                    </div>
                    <Badge variant={cred.isActive ? 'success' : 'neutral'} size="sm">
                      {cred.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Success Rate Bar */}
      <Card>
        <CardHeader title="Submission Success Rate" />
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center justify-between text-sm">
              <span className="text-gray-600 dark:text-gray-400">
                {metrics.validCount} of {metrics.totalDocuments} documents accepted
              </span>
              <span className="font-bold text-gray-900 dark:text-white">
                {metrics.successRate}%
              </span>
            </div>
            <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-3">
              <div
                className={`h-3 rounded-full transition-all ${
                  metrics.successRate >= 90
                    ? 'bg-green-500'
                    : metrics.successRate >= 70
                      ? 'bg-yellow-500'
                      : 'bg-red-500'
                }`}
                style={{ width: `${metrics.successRate}%` }}
              />
            </div>
            <div className="flex gap-4 text-xs text-gray-500 dark:text-gray-400">
              <span>✓ Valid: {metrics.validCount}</span>
              <span>✕ Invalid: {metrics.invalidCount}</span>
              <span>✕ Rejected: {metrics.rejectedCount}</span>
              <span>⊘ Cancelled: {metrics.cancelledCount}</span>
              <span>◷ Pending: {metrics.pendingCount}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
