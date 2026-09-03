import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, FileText, Send, RotateCcw, AlertCircle, CheckCircle2, Clock, Ban } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { get, post } from '@api/client'

interface SstReturn {
  id: string
  companyId: string
  periodStart: string
  periodEnd: string
  returnType: string
  status: string
  totalTaxableAmount: number
  totalTaxCollected: number
  totalTaxPaid: number
  netTaxPayable: number
  exemptAmount: number
  lineItemsJson?: string
  createdAt: string
  updatedAt: string
  filedAt?: string
  lhdnReference?: string
}

function getSstReturns(): Promise<SstReturn[]> {
  return get('/tax/sst-returns')
}

function computeReturn(data: { periodStart: string; returnType: string }): Promise<SstReturn> {
  return post('/tax/sst-returns/compute', data)
}

function fileReturn(id: string, lhdnReference: string): Promise<SstReturn> {
  return post(`/tax/sst-returns/${id}/file`, { lhdnReference })
}

function amendReturn(id: string): Promise<SstReturn> {
  return post(`/tax/sst-returns/${id}/amend`)
}

const RETURN_TYPES = [
  { value: 'SST-02', label: 'SST-02 — Sales Tax' },
  { value: 'SST-02A', label: 'SST-02A — Service Tax' },
]

const STATUS_VARIANT: Record<string, 'success' | 'warning' | 'info' | 'danger' | 'default'> = {
  Draft: 'default',
  Prepared: 'info',
  Filed: 'success',
  Amended: 'warning',
}

const STATUS_ICON: Record<string, typeof Clock> = {
  Draft: Clock,
  Prepared: FileText,
  Filed: CheckCircle2,
  Amended: RotateCcw,
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-MY', { style: 'currency', currency: 'MYR' }).format(amount)
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-MY', { year: 'numeric', month: 'short', day: 'numeric' })
}

export function SstReturnsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isComputeOpen, setIsComputeOpen] = useState(false)
  const [isFileOpen, setIsFileOpen] = useState(false)
  const [selectedReturn, setSelectedReturn] = useState<SstReturn | null>(null)
  const [periodStart, setPeriodStart] = useState('')
  const [returnType, setReturnType] = useState('SST-02')
  const [lhdnReference, setLhdnReference] = useState('')
  const [formError, setFormError] = useState<string | null>(null)

  const { data: returns = [], isLoading } = useQuery({
    queryKey: ['sst-returns'],
    queryFn: getSstReturns,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['sst-returns'] })

  const computeMutation = useMutation({
    mutationFn: computeReturn,
    onSuccess: () => { invalidate(); setIsComputeOpen(false); resetComputeForm() },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const fileMutation = useMutation({
    mutationFn: ({ id, ref }: { id: string; ref: string }) => fileReturn(id, ref),
    onSuccess: () => { invalidate(); setIsFileOpen(false); setLhdnReference(''); setSelectedReturn(null) },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const amendMutation = useMutation({
    mutationFn: amendReturn,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const resetComputeForm = () => { setPeriodStart(''); setReturnType('SST-02'); setFormError(null) }

  const filtered = returns.filter(r => {
    const q = search.trim().toLowerCase()
    if (!q) return true
    return r.returnType.toLowerCase().includes(q) ||
      r.status.toLowerCase().includes(q) ||
      r.lhdnReference?.toLowerCase().includes(q)
  })

  const totalCollected = filtered.reduce((s, r) => s + r.totalTaxCollected, 0)
  const totalFiled = filtered.filter(r => r.status === 'Filed').length
  const totalPending = filtered.filter(r => r.status !== 'Filed' && r.status !== 'Amended').length

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="pt-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">Total Returns</p>
            <p className="text-2xl font-bold text-gray-900 dark:text-white">{returns.length}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">Total Collected</p>
            <p className="text-2xl font-bold text-emerald-600">{formatCurrency(totalCollected)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">Filed</p>
            <p className="text-2xl font-bold text-blue-600">{totalFiled}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">Pending</p>
            <p className="text-2xl font-bold text-amber-600">{totalPending}</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader
          title="SST Returns (SST-02 / SST-02A)"
          description="Bi-monthly sales and service tax returns for LHDN filing"
          action={
            <Button variant="primary" size="sm" onClick={() => { resetComputeForm(); setIsComputeOpen(true) }} leftIcon={<Plus className="h-4 w-4" />}>
              Compute Return
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by type, status, or reference..."
              leftIcon={<Search className="h-4 w-4" />} aria-label="Search returns" />
          </div>

          {isLoading ? <SkeletonTable columns={8} /> : filtered.length === 0 ? (
            <div className="text-center py-12">
              <FileText className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">No SST returns computed yet.</p>
              <Button variant="primary" size="sm" onClick={() => { resetComputeForm(); setIsComputeOpen(true) }} leftIcon={<Plus className="h-4 w-4" />}>
                Compute First Return
              </Button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Period</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Taxable</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Tax Collected</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Exempt</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Net Payable</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map(ret => {
                    const StatusIcon = STATUS_ICON[ret.status] || Clock
                    return (
                      <tr key={ret.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-3">
                          <span className="text-xs font-medium text-gray-900 dark:text-white">
                            {formatDate(ret.periodStart)} — {formatDate(ret.periodEnd)}
                          </span>
                        </td>
                        <td className="px-3 py-3">
                          <Badge variant={ret.returnType === 'SST-02' ? 'info' : 'success'} size="sm">{ret.returnType}</Badge>
                        </td>
                        <td className="px-3 py-3">
                          <span className={`inline-flex items-center gap-1 text-xs font-medium`}>
                            <StatusIcon className="h-3 w-3" />
                            <Badge variant={STATUS_VARIANT[ret.status] || 'default'} size="sm">{ret.status}</Badge>
                          </span>
                        </td>
                        <td className="px-3 py-3 text-right font-mono text-xs">{formatCurrency(ret.totalTaxableAmount)}</td>
                        <td className="px-3 py-3 text-right font-mono text-xs text-emerald-600">{formatCurrency(ret.totalTaxCollected)}</td>
                        <td className="px-3 py-3 text-right font-mono text-xs text-gray-500">{formatCurrency(ret.exemptAmount)}</td>
                        <td className="px-3 py-3 text-right font-mono text-xs font-semibold">{formatCurrency(ret.netTaxPayable)}</td>
                        <td className="px-3 py-3">
                          <div className="flex items-center justify-end gap-1">
                            {ret.status === 'Prepared' && (
                              <Button variant="ghost" size="sm" onClick={() => { setSelectedReturn(ret); setLhdnReference(''); setIsFileOpen(true) }}>
                                <Send className="h-3 w-3 mr-1" /> File
                              </Button>
                            )}
                            {ret.status === 'Filed' && (
                              <Button variant="ghost" size="sm" onClick={() => amendMutation.mutate(ret.id)} disabled={amendMutation.isPending}>
                                <RotateCcw className="h-3 w-3 mr-1" /> Amend
                              </Button>
                            )}
                          </div>
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

      {/* Compute Return Modal */}
      <Modal isOpen={isComputeOpen} onClose={() => setIsComputeOpen(false)} title="Compute SST Return" size="md"
        footer={
          <>
            <Button variant="secondary" onClick={() => setIsComputeOpen(false)} disabled={computeMutation.isPending}>Cancel</Button>
            <Button variant="primary" onClick={() => computeMutation.mutate({ periodStart, returnType })}
              isLoading={computeMutation.isPending}>Compute</Button>
          </>
        }
      >
        <div className="space-y-4">
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Select the filing period and return type. The system will aggregate all qualifying posted transactions for the period.
          </p>
          <Input label="Period Start Date" type="date" value={periodStart} onChange={e => setPeriodStart(e.target.value)} required />
          <Select label="Return Type" value={returnType} onChange={e => setReturnType(e.target.value)} options={RETURN_TYPES} />
          <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg text-xs text-blue-700 dark:text-blue-300">
            <strong>SST-02:</strong> Sales Tax (LHDN tax types 01, 02, 03)<br />
            <strong>SST-02A:</strong> Service Tax (LHDN tax type 04)
          </div>
        </div>
      </Modal>

      {/* File Return Modal */}
      <Modal isOpen={isFileOpen} onClose={() => { setIsFileOpen(false); setSelectedReturn(null) }} title="File SST Return" size="md"
        footer={
          <>
            <Button variant="secondary" onClick={() => { setIsFileOpen(false); setSelectedReturn(null) }} disabled={fileMutation.isPending}>Cancel</Button>
            <Button variant="primary" onClick={() => selectedReturn && fileMutation.mutate({ id: selectedReturn.id, ref: lhdnReference })}
              isLoading={fileMutation.isPending}>File Return</Button>
          </>
        }
      >
        <div className="space-y-4">
          {selectedReturn && (
            <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg text-sm">
              <p><span className="font-medium">Period:</span> {formatDate(selectedReturn.periodStart)} — {formatDate(selectedReturn.periodEnd)}</p>
              <p><span className="font-medium">Type:</span> {selectedReturn.returnType}</p>
              <p><span className="font-medium">Net Payable:</span> {formatCurrency(selectedReturn.netTaxPayable)}</p>
            </div>
          )}
          <Input label="LHDN Reference Number" value={lhdnReference} onChange={e => setLhdnReference(e.target.value)}
            placeholder="e.g., SST-2024-001234" required />
          <p className="text-xs text-amber-600 dark:text-amber-400">
            Once filed, corrections must go through the Amend flow which creates a new return.
          </p>
        </div>
      </Modal>
    </div>
  )
}
