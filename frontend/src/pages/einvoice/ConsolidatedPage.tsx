import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Layers, Search, ChevronDown, ChevronRight, AlertCircle, CheckCircle2, Clock, Send, Package } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getConsolidatedDocuments, getConsolidationPreview, getConsolidatedSources } from '@api/einvoice'
import type { ConsolidatedDocument, ConsolidationPreview, ConsolidationLineItem } from '@api/einvoice'

const STATUS_VARIANT: Record<string, 'success' | 'warning' | 'info' | 'danger' | 'default'> = {
  Queued: 'default',
  Submitted: 'info',
  Valid: 'success',
  Invalid: 'danger',
  Rejected: 'danger',
}

const STATUS_ICON: Record<string, typeof Clock> = {
  Queued: Clock,
  Submitted: Send,
  Valid: CheckCircle2,
  Invalid: AlertCircle,
  Rejected: AlertCircle,
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount)
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-MY', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function formatDateShort(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-MY', { year: 'numeric', month: 'short', day: 'numeric' })
}

export function ConsolidatedPage() {
  const [search, setSearch] = useState('')
  const [expandedDoc, setExpandedDoc] = useState<string | null>(null)

  const { data: consolidatedDocs = [], isLoading: loadingConsolidated } = useQuery({
    queryKey: ['einvoice', 'consolidated-documents'],
    queryFn: getConsolidatedDocuments,
  })

  const { data: preview, isLoading: loadingPreview } = useQuery({
    queryKey: ['einvoice', 'consolidation-preview'],
    queryFn: getConsolidationPreview,
  })

  const filteredDocs = consolidatedDocs.filter(d => {
    const q = search.trim().toLowerCase()
    if (!q) return true
    return d.status.toLowerCase().includes(q) ||
      d.myInvoisUuid?.toLowerCase().includes(q)
  })

  const filteredEligible = (preview?.batches || []).filter(b => {
    const q = search.trim().toLowerCase()
    if (!q) return true
    return b.sourceDocumentId.toLowerCase().includes(q)
  })

  // Group eligible docs by batch number
  const batchGroups = filteredEligible.reduce((acc, item) => {
    const batch = item.batchNumber || 1
    if (!acc[batch]) acc[batch] = []
    acc[batch].push(item)
    return acc
  }, {} as Record<number, ConsolidationLineItem[]>)

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="pt-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">Consolidated Documents</p>
            <p className="text-2xl font-bold text-gray-900 dark:text-white">{consolidatedDocs.length}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">Eligible for Consolidation</p>
            <p className="text-2xl font-bold text-amber-600">{preview?.totalEligibleCount || 0}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">Total Eligible Amount</p>
            <p className="text-2xl font-bold text-emerald-600">{formatCurrency(preview?.totalEligibleAmount || 0)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">Max Amount per Line</p>
            <p className="text-2xl font-bold text-blue-600">{formatCurrency(preview?.maxAmountPerLine || 10000)}</p>
          </CardContent>
        </Card>
      </div>

      {/* Eligible Documents Preview */}
      <Card>
        <CardHeader
          title="Eligible Documents for Consolidation"
          description={`Documents under RM10,000 not yet consolidated. Max 100 lines per consolidated document.`}
        />
        <CardContent>
          {loadingPreview ? <SkeletonTable columns={5} /> : filteredEligible.length === 0 ? (
            <div className="text-center py-8">
              <Package className="h-10 w-10 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
              <p className="text-sm text-gray-500 dark:text-gray-400">
                No documents eligible for consolidation.
              </p>
              <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
                Documents must be Valid status and under RM10,000 to be eligible.
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {Object.entries(batchGroups).map(([batchNum, items]) => {
                const batchTotal = items.reduce((s, i) => s + i.totalAmount, 0)
                const batchTax = items.reduce((s, i) => s + i.taxAmount, 0)
                return (
                  <div key={batchNum} className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
                    <div className="px-4 py-2 bg-gray-50 dark:bg-gray-800/50 flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Layers className="h-4 w-4 text-gray-400" />
                        <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                          Batch {batchNum}
                        </span>
                        <Badge variant="info" size="sm">{items.length} documents</Badge>
                      </div>
                      <div className="flex items-center gap-4 text-xs">
                        <span className="text-gray-500">Tax: <span className="font-medium text-gray-700 dark:text-gray-300">{formatCurrency(batchTax)}</span></span>
                        <span className="font-medium text-gray-900 dark:text-white">Total: {formatCurrency(batchTotal)}</span>
                      </div>
                    </div>
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-gray-100 dark:border-gray-700/60 text-left text-xs">
                          <th className="px-4 py-2 font-medium text-gray-500 dark:text-gray-400">Source Document</th>
                          <th className="px-4 py-2 font-medium text-gray-500 dark:text-gray-400">Amount</th>
                          <th className="px-4 py-2 font-medium text-gray-500 dark:text-gray-400">Tax</th>
                          <th className="px-4 py-2 font-medium text-gray-500 dark:text-gray-400">UUID</th>
                          <th className="px-4 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-50 dark:divide-gray-700/30">
                        {items.map(item => (
                          <tr key={item.documentId} className="hover:bg-gray-50 dark:hover:bg-gray-800/30">
                            <td className="px-4 py-2 font-mono text-xs text-gray-700 dark:text-gray-300">
                              {item.sourceDocumentId.slice(0, 8)}...
                            </td>
                            <td className="px-4 py-2 font-mono text-xs text-gray-700 dark:text-gray-300">
                              {formatCurrency(item.totalAmount)}
                            </td>
                            <td className="px-4 py-2 font-mono text-xs text-gray-500">
                              {formatCurrency(item.taxAmount)}
                            </td>
                            <td className="px-4 py-2 font-mono text-xs text-gray-500">
                              {item.myInvoisUuid ? item.myInvoisUuid.slice(0, 12) + '...' : '—'}
                            </td>
                            <td className="px-4 py-2 text-xs text-gray-500">
                              {formatDateShort(item.createdAt)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )
              })}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Consolidated Documents */}
      <Card>
        <CardHeader
          title="Consolidated e-Invoices"
          description="Previously consolidated documents submitted to MyInvois"
          action={
            <div className="max-w-xs">
              <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search status or UUID..."
                leftIcon={<Search className="h-4 w-4" />} aria-label="Search consolidated documents" />
            </div>
          }
        />
        <CardContent>
          {loadingConsolidated ? <SkeletonTable columns={7} /> : filteredDocs.length === 0 ? (
            <div className="text-center py-8">
              <Layers className="h-10 w-10 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
              <p className="text-sm text-gray-500 dark:text-gray-400">No consolidated documents yet.</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Total Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Tax Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">MyInvois UUID</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Created</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Submitted</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredDocs.map(doc => {
                    const StatusIcon = STATUS_ICON[doc.status] || Clock
                    return (
                      <tr key={doc.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-3">
                          <span className="inline-flex items-center gap-1">
                            <StatusIcon className="h-3 w-3" />
                            <Badge variant={STATUS_VARIANT[doc.status] || 'default'} size="sm">{doc.status}</Badge>
                          </span>
                        </td>
                        <td className="px-3 py-3 font-mono text-xs font-semibold">{formatCurrency(doc.totalAmount)}</td>
                        <td className="px-3 py-3 font-mono text-xs text-gray-500">{formatCurrency(doc.taxAmount)}</td>
                        <td className="px-3 py-3 font-mono text-xs text-gray-500">
                          {doc.myInvoisUuid ? doc.myInvoisUuid.slice(0, 16) + '...' : '—'}
                        </td>
                        <td className="px-3 py-3 text-xs text-gray-500">{formatDateShort(doc.createdAt)}</td>
                        <td className="px-3 py-3 text-xs text-gray-500">
                          {doc.submittedAt ? formatDateShort(doc.submittedAt) : '—'}
                        </td>
                        <td className="px-3 py-3 text-right">
                          {doc.validationLink && (
                            <a href={doc.validationLink} target="_blank" rel="noopener noreferrer"
                              className="text-xs text-blue-600 hover:underline">View on MyInvois</a>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
