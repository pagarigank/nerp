import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search, AlertCircle, ChevronDown, ChevronRight } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getAuditLogsByEntity, getAuditLogsByUser, getUsers } from '@api/platform'
import { cn } from '@utils/helpers'

type QueryMode = 'entity' | 'user'

const entityTypeOptions = [
  { value: 'Company', label: 'Company' },
  { value: 'FiscalYear', label: 'Fiscal Year' },
  { value: 'FiscalPeriod', label: 'Fiscal Period' },
  { value: 'Account', label: 'Account' },
  { value: 'SegmentType', label: 'Segment Type' },
  { value: 'SegmentValue', label: 'Segment Value' },
  { value: 'Currency', label: 'Currency' },
  { value: 'ExchangeRate', label: 'Exchange Rate' },
  { value: 'NumberSequence', label: 'Number Sequence' },
  { value: 'User', label: 'User' },
  { value: 'Role', label: 'Role' },
  { value: 'Permission', label: 'Permission' },
  { value: 'JournalBatch', label: 'Journal Batch' },
  { value: 'Vendor', label: 'Vendor' },
  { value: 'PaymentTerm', label: 'Payment Term' },
  { value: 'VoucherBatch', label: 'Voucher Batch' },
  { value: 'Voucher', label: 'Voucher' },
  { value: 'Payment', label: 'Payment' },
  { value: 'Customer', label: 'Customer' },
  { value: 'InvoiceBatch', label: 'Invoice Batch' },
  { value: 'Invoice', label: 'Invoice' },
  { value: 'CashReceipt', label: 'Cash Receipt' },
]

function formatJson(json: string | null | undefined): string {
  if (!json) return ''
  try {
    return JSON.stringify(JSON.parse(json), null, 2)
  } catch {
    return json
  }
}

function ChangesCell({ oldValues, newValues }: { oldValues?: string | null | undefined; newValues?: string | null | undefined }) {
  const [open, setOpen] = useState(false)

  if (!oldValues && !newValues) {
    return <span className="text-gray-400 dark:text-gray-600">—</span>
  }

  return (
    <div>
      <button
        type="button"
        onClick={() => setOpen(o => !o)}
        className="inline-flex items-center gap-1 text-primary-600 dark:text-primary-400 text-xs font-medium hover:underline"
        aria-expanded={open}
      >
        {open ? <ChevronDown className="h-3.5 w-3.5" aria-hidden="true" /> : <ChevronRight className="h-3.5 w-3.5" aria-hidden="true" />}
        {open ? 'Hide changes' : 'View changes'}
      </button>
      {open && (
        <pre className="mt-2 max-h-48 overflow-auto rounded-md bg-gray-50 dark:bg-gray-900 p-2 text-[11px] leading-relaxed text-gray-700 dark:text-gray-300 font-mono whitespace-pre-wrap">
          {oldValues && (
            <>
              <span className="font-semibold text-red-600 dark:text-red-400">Before:</span>{'\n'}
              {formatJson(oldValues)}
            </>
          )}
          {oldValues && newValues && '\n\n'}
          {newValues && (
            <>
              <span className="font-semibold text-green-600 dark:text-green-400">After:</span>{'\n'}
              {formatJson(newValues)}
            </>
          )}
        </pre>
      )}
    </div>
  )
}

export function AuditLogsPage() {
  const [mode, setMode] = useState<QueryMode>('entity')
  const [entityType, setEntityType] = useState('')
  const [entityId, setEntityId] = useState('')
  const [username, setUsername] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [submitted, setSubmitted] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const { data: users = [] } = useQuery({
    queryKey: ['platform', 'users'],
    queryFn: getUsers,
    enabled: mode === 'user',
  })

  const userOptions = useMemo(
    () => users.map(u => ({ value: u.username, label: `${u.username} (${u.displayName})` })),
    [users]
  )

  const canSearch =
    mode === 'entity'
      ? entityType.trim().length > 0 && entityId.trim().length > 0
      : username.trim().length > 0

  const queryKey: unknown[] =
    mode === 'entity'
      ? ['platform', 'auditLogs', 'entity', entityType.trim(), entityId.trim()]
      : ['platform', 'auditLogs', 'user', username.trim(), from || undefined, to || undefined]

  const { data: logs = [], isLoading, isFetching, error, refetch } = useQuery({
    queryKey,
    queryFn: () =>
      mode === 'entity'
        ? getAuditLogsByEntity(entityType.trim(), entityId.trim())
        : getAuditLogsByUser(username.trim(), { ...(from ? { from } : {}), ...(to ? { to } : {}) }),
    enabled: submitted && canSearch,
  })

  const runSearch = () => {
    setFormError(null)
    if (!canSearch) {
      setFormError(
        mode === 'entity' ? 'Enter both an entity type and an entity ID.' : 'Select a user to view their activity.'
      )
      return
    }
    setSubmitted(true)
    refetch()
  }

  return (
    <div className="space-y-6">
      {formError && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{formError}</span>
        </div>
      )}
      {error && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{getErrorMessage(error)}</span>
        </div>
      )}

      <Card>
        <CardHeader title="Audit Log" description="Immutable record of who changed what, and when" />
        <CardContent>
          <div className="mb-4 flex gap-1">
            {(
              [
                { value: 'entity', label: 'By Entity' },
                { value: 'user', label: 'By User' },
              ] as const
            ).map(option => (
              <button
                key={option.value}
                type="button"
                onClick={() => {
                  setMode(option.value)
                  setSubmitted(false)
                }}
                className={cn(
                  'px-4 py-2 rounded-lg text-sm font-medium transition-colors',
                  mode === option.value
                    ? 'bg-primary-600 text-white'
                    : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'
                )}
                aria-pressed={mode === option.value}
              >
                {option.label}
              </button>
            ))}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {mode === 'entity' ? (
              <>
                <Select
                  value={entityType}
                  onChange={e => setEntityType(e.target.value)}
                  options={entityTypeOptions}
                  placeholder="Select entity type"
                  aria-label="Entity type"
                />
                <Input
                  value={entityId}
                  onChange={e => setEntityId(e.target.value)}
                  placeholder="Entity ID (GUID)"
                  aria-label="Entity ID"
                />
              </>
            ) : (
              <>
                <Select
                  value={username}
                  onChange={e => setUsername(e.target.value)}
                  options={userOptions}
                  placeholder="Select user"
                  aria-label="User"
                />
                <Input
                  type="date"
                  value={from}
                  onChange={e => setFrom(e.target.value)}
                  aria-label="From date"
                />
                <Input
                  type="date"
                  value={to}
                  onChange={e => setTo(e.target.value)}
                  aria-label="To date"
                />
              </>
            )}
            <div className="flex items-end">
              <Button variant="primary" onClick={runSearch} isLoading={isFetching} leftIcon={<Search className="h-4 w-4" />}>
                Load
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader
          title="Entries"
          description={`${logs.length} audit entr${logs.length === 1 ? 'y' : 'ies'}`}
        />
        <CardContent>
          {isLoading || (isFetching && logs.length === 0) ? (
            <SkeletonTable columns={7} />
          ) : !submitted ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              Select search criteria above and click Load to view the audit trail.
            </p>
          ) : logs.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No audit entries found for the selected criteria.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Action</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Entity</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Performed By</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Performed On</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">IP</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Correlation ID</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Changes</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {logs.map(log => (
                    <tr key={log.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors align-top">
                      <td className="px-3 py-3">
                        <span className="inline-flex rounded-md bg-primary-50 dark:bg-primary-900/30 px-2 py-0.5 text-xs font-medium text-primary-700 dark:text-primary-300">
                          {log.action}
                        </span>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        <span className="block text-xs font-medium text-gray-900 dark:text-white">{log.entityType}</span>
                        <span className="font-mono text-[11px] text-gray-500 dark:text-gray-400">{log.entityId}</span>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{log.performedBy}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {new Date(log.performedOn).toLocaleString('en-US', {
                          month: 'short',
                          day: 'numeric',
                          year: 'numeric',
                          hour: 'numeric',
                          minute: '2-digit',
                        })}
                      </td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-500 dark:text-gray-400">{log.ipAddress ?? '—'}</td>
                      <td className="px-3 py-3 font-mono text-[11px] text-gray-500 dark:text-gray-400">
                        {log.correlationId ? log.correlationId.slice(0, 8) : '—'}
                      </td>
                      <td className="px-3 py-3">
                        <ChangesCell oldValues={log.oldValues} newValues={log.newValues} />
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
