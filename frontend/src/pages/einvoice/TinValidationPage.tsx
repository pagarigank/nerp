import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Search, CheckCircle2, XCircle, AlertCircle, Database, Trash2, Layers, ClipboardCheck } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { get, post } from '@api/client'
import type { TinValidationResult } from '@api/einvoice'

interface TinCacheStatus {
  cachedCount: number
  tins: string[]
}

function validateTinApi(tin: string): Promise<TinValidationResult> {
  return get(`/einvoice/taxpayer-profiles/tin-validate/${encodeURIComponent(tin)}`)
}

function validateTinBatchApi(tins: string[]): Promise<TinValidationResult[]> {
  return post('/einvoice/taxpayer-profiles/tin-validate-batch', { tins })
}

function getTinCacheStatus(): Promise<TinCacheStatus> {
  return get('/einvoice/taxpayer-profiles/tin-cache-status')
}

function clearTinCache(): Promise<{ message: string }> {
  return post('/einvoice/taxpayer-profiles/tin-cache-clear', {})
}

function formatDate(dateStr?: string): string {
  if (!dateStr) return '—'
  return new Date(dateStr).toLocaleString('en-MY', {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

export function TinValidationPage() {
  const queryClient = useQueryClient()
  const [singleTin, setSingleTin] = useState('')
  const [batchInput, setBatchInput] = useState('')
  const [singleResult, setSingleResult] = useState<TinValidationResult | null>(null)
  const [batchResults, setBatchResults] = useState<TinValidationResult[]>([])
  const [formError, setFormError] = useState<string | null>(null)

  const { data: cacheStatus, isLoading: loadingCache } = useQuery({
    queryKey: ['einvoice', 'tin-cache-status'],
    queryFn: getTinCacheStatus,
  })

  const singleMutation = useMutation({
    mutationFn: validateTinApi,
    onSuccess: (result) => {
      setSingleResult(result)
      setFormError(null)
    },
    onError: (err) => {
      setFormError(getErrorMessage(err))
      setSingleResult(null)
    },
  })

  const batchMutation = useMutation({
    mutationFn: validateTinBatchApi,
    onSuccess: (results) => {
      setBatchResults(results)
      setFormError(null)
    },
    onError: (err) => {
      setFormError(getErrorMessage(err))
      setBatchResults([])
    },
  })

  const clearCacheMutation = useMutation({
    mutationFn: clearTinCache,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['einvoice', 'tin-cache-status'] })
    },
  })

  const handleSingleValidate = () => {
    const tin = singleTin.trim()
    if (!tin) {
      setFormError('Please enter a TIN to validate.')
      return
    }
    singleMutation.mutate(tin)
  }

  const handleBatchValidate = () => {
    const tins = batchInput
      .split('\n')
      .map(l => l.trim())
      .filter(l => l.length > 0)

    if (tins.length === 0) {
      setFormError('Please enter at least one TIN.')
      return
    }

    if (tins.length > 50) {
      setFormError('Maximum 50 TINs per batch. Please split into smaller batches.')
      return
    }

    batchMutation.mutate(tins)
  }

  const validCount = batchResults.filter(r => r.isValid).length
  const invalidCount = batchResults.filter(r => r.success && !r.isValid).length
  const errorCount = batchResults.filter(r => !r.success).length

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      {/* Cache Status Card */}
      <Card>
        <CardContent className="pt-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Database className="h-5 w-5 text-gray-400" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">
                  TIN Validation Cache
                </p>
                <p className="text-xs text-gray-500 dark:text-gray-400">
                  {loadingCache ? 'Loading...' : `${cacheStatus?.cachedCount || 0} TINs cached (24h TTL)`}
                </p>
              </div>
            </div>
            <Button variant="ghost" size="sm" onClick={() => clearCacheMutation.mutate()}
              disabled={clearCacheMutation.isPending} leftIcon={<Trash2 className="h-3.5 w-3.5" />}>
              Clear Cache
            </Button>
          </div>
          {cacheStatus && cacheStatus.tins.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-1">
              {cacheStatus.tins.slice(0, 10).map(tin => (
                <Badge key={tin} variant="info" size="sm">{tin}</Badge>
              ))}
              {cacheStatus.tins.length > 10 && (
                <Badge variant="default" size="sm">+{cacheStatus.tins.length - 10} more</Badge>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Single TIN Validation */}
      <Card>
        <CardHeader
          title="Validate TIN"
          description="Look up a single Tax Identification Number against the MyInvois API"
        />
        <CardContent>
          <div className="flex gap-3 max-w-lg">
            <Input
              value={singleTin}
              onChange={e => setSingleTin(e.target.value)}
              placeholder="Enter TIN (e.g., C1234567890)"
              leftIcon={<Search className="h-4 w-4" />}
              onKeyDown={e => e.key === 'Enter' && handleSingleValidate()}
              aria-label="TIN to validate"
            />
            <Button variant="primary" onClick={handleSingleValidate}
              isLoading={singleMutation.isPending}
              leftIcon={<ClipboardCheck className="h-4 w-4" />}>
              Validate
            </Button>
          </div>

          {singleResult && (
            <div className={`mt-4 p-4 rounded-lg border ${
              singleResult.isValid
                ? 'bg-emerald-50 border-emerald-200 dark:bg-emerald-900/20 dark:border-emerald-800'
                : 'bg-red-50 border-red-200 dark:bg-red-900/20 dark:border-red-800'
            }`}>
              <div className="flex items-start gap-3">
                {singleResult.isValid ? (
                  <CheckCircle2 className="h-5 w-5 text-emerald-500 mt-0.5" />
                ) : (
                  <XCircle className="h-5 w-5 text-red-500 mt-0.5" />
                )}
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="text-sm font-semibold text-gray-900 dark:text-white">
                      TIN: {singleResult.tin}
                    </span>
                    <Badge variant={singleResult.isValid ? 'success' : 'danger'} size="sm">
                      {singleResult.isValid ? 'Valid' : 'Invalid'}
                    </Badge>
                  </div>
                  {singleResult.entityName && (
                    <p className="text-sm text-gray-700 dark:text-gray-300">
                      <span className="font-medium">Entity:</span> {singleResult.entityName}
                    </p>
                  )}
                  {singleResult.businessDescription && (
                    <p className="text-sm text-gray-700 dark:text-gray-300">
                      <span className="font-medium">Business:</span> {singleResult.businessDescription}
                    </p>
                  )}
                  {singleResult.errorMessage && (
                    <p className="text-sm text-red-600 dark:text-red-400 mt-1">
                      {singleResult.errorMessage}
                    </p>
                  )}
                  <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
                    Validated: {formatDate(singleResult.validationDate)}
                  </p>
                </div>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Batch TIN Validation */}
      <Card>
        <CardHeader
          title="Batch TIN Validation"
          description="Validate multiple TINs at once (max 50). Enter one TIN per line."
        />
        <CardContent>
          <div className="max-w-lg">
            <textarea
              value={batchInput}
              onChange={e => setBatchInput(e.target.value)}
              placeholder={"C1234567890\nD9876543210\nE1112223333"}
              rows={6}
              className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg
                bg-white dark:bg-gray-800 text-gray-900 dark:text-white
                placeholder-gray-400 dark:placeholder-gray-500
                focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500
                font-mono"
              aria-label="Batch TINs"
            />
            <div className="flex items-center justify-between mt-3">
              <span className="text-xs text-gray-500 dark:text-gray-400">
                {batchInput.split('\n').filter(l => l.trim()).length} TINs entered
              </span>
              <Button variant="primary" onClick={handleBatchValidate}
                isLoading={batchMutation.isPending}
                leftIcon={<Layers className="h-4 w-4" />}>
                Validate Batch
              </Button>
            </div>
          </div>

          {batchResults.length > 0 && (
            <div className="mt-6">
              {/* Summary */}
              <div className="flex gap-4 mb-4">
                <div className="flex items-center gap-1.5">
                  <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                  <span className="text-sm font-medium text-emerald-700 dark:text-emerald-400">
                    {validCount} Valid
                  </span>
                </div>
                <div className="flex items-center gap-1.5">
                  <XCircle className="h-4 w-4 text-red-500" />
                  <span className="text-sm font-medium text-red-700 dark:text-red-400">
                    {invalidCount} Invalid
                  </span>
                </div>
                {errorCount > 0 && (
                  <div className="flex items-center gap-1.5">
                    <AlertCircle className="h-4 w-4 text-amber-500" />
                    <span className="text-sm font-medium text-amber-700 dark:text-amber-400">
                      {errorCount} Errors
                    </span>
                  </div>
                )}
              </div>

              {/* Results Table */}
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                      <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                      <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">TIN</th>
                      <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Entity Name</th>
                      <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Business</th>
                      <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Error</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {batchResults.map((result, idx) => (
                      <tr key={idx} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-2">
                          {!result.success ? (
                            <Badge variant="warning" size="sm">Error</Badge>
                          ) : result.isValid ? (
                            <Badge variant="success" size="sm">Valid</Badge>
                          ) : (
                            <Badge variant="danger" size="sm">Invalid</Badge>
                          )}
                        </td>
                        <td className="px-3 py-2 font-mono text-xs text-gray-700 dark:text-gray-300">
                          {result.tin}
                        </td>
                        <td className="px-3 py-2 text-gray-700 dark:text-gray-300">
                          {result.entityName || '—'}
                        </td>
                        <td className="px-3 py-2 text-gray-700 dark:text-gray-300">
                          {result.businessDescription || '—'}
                        </td>
                        <td className="px-3 py-2 text-xs text-red-600 dark:text-red-400">
                          {result.errorMessage || '—'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
