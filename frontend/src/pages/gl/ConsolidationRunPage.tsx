import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Play, Trash2, Edit, Eye, AlertCircle, CheckCircle, XCircle, Loader2 } from 'lucide-react'
import { formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import {
  createConsolidationRun,
  getConsolidationRuns,
  executeConsolidationRun,
  createIntercompanyMapping,
  getIntercompanyMappings,
  updateIntercompanyMapping,
  deleteIntercompanyMapping,
} from '@api/gl'
import type {
  CreateConsolidationRunRequest,
  CreateIntercompanyMappingRequest,
  UpdateIntercompanyMappingRequest,
  IntercompanyMappingDto,
} from '@/types/gl'
import { getCompanies } from '@api/platform'

const statusMap: Record<number, { label: string; className: string; icon: React.ReactNode }> = {
  0: { label: 'Draft', className: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300', icon: <XCircle className="h-4 w-4" /> },
  1: { label: 'Processing', className: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300', icon: <Loader2 className="h-4 w-4 animate-spin" /> },
  2: { label: 'Completed', className: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300', icon: <CheckCircle className="h-4 w-4" /> },
  3: { label: 'Failed', className: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300', icon: <AlertCircle className="h-4 w-4" /> },
}

function statusBadge(status: number) {
  const s = statusMap[status] || { label: 'Unknown', className: 'bg-gray-100 text-gray-700', icon: null }
  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${s.className}`}>
      {s.icon}
      {s.label}
    </span>
  )
}

function dataTable(headers: string[], rows: (string | React.ReactNode)[][], align?: ('left' | 'right')[]) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
            {headers.map((h, i) => (
              <th key={h} className={`px-3 py-2 font-medium text-gray-500 dark:text-gray-400 ${align?.[i] === 'right' ? 'text-right' : ''}`}>
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
          {rows.map((row, ri) => (
            <tr key={ri} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
              {row.map((cell, ci) => (
                <td key={ci} className={`px-3 py-2.5 ${align?.[ci] === 'right' ? 'text-right font-tabular tabular-nums' : ''}`}>
                  {cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function ConsolidationRunPage() {
  const queryClient = useQueryClient()
  const [activeTab, setActiveTab] = useState<'runs' | 'mappings'>('runs')

  const { data: companies = [] } = useQuery({
    queryKey: ['platform', 'companies'],
    queryFn: () => getCompanies(),
  })

  const parentCompanies = useMemo(
    () => companies.filter(c => !c.parentCompanyId),
    [companies]
  )

  const runsQuery = useQuery({
    queryKey: ['gl', 'consolidation', 'runs'],
    queryFn: () => {
      const parentId = parentCompanies[0]?.id
      if (!parentId) return []
      return getConsolidationRuns(parentId)
    },
    enabled: parentCompanies.length > 0,
  })

  const createRunMutation = useMutation({
    mutationFn: (data: CreateConsolidationRunRequest) => createConsolidationRun(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gl', 'consolidation', 'runs'] })
    },
  })

  const executeRunMutation = useMutation({
    mutationFn: (id: string) => executeConsolidationRun(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gl', 'consolidation', 'runs'] })
    },
  })

  const mappingsQuery = useQuery({
    queryKey: ['gl', 'consolidation', 'mappings'],
    queryFn: () => getIntercompanyMappings(),
    enabled: activeTab === 'mappings',
  })

  const createMappingMutation = useMutation({
    mutationFn: (data: CreateIntercompanyMappingRequest) => createIntercompanyMapping(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gl', 'consolidation', 'mappings'] })
    },
  })

  const updateMappingMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateIntercompanyMappingRequest }) => updateIntercompanyMapping(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gl', 'consolidation', 'mappings'] })
    },
  })

  const deleteMappingMutation = useMutation({
    mutationFn: (id: string) => deleteIntercompanyMapping(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gl', 'consolidation', 'mappings'] })
    },
  })

  // Modal states
  const [showCreateRunModal, setShowCreateRunModal] = useState(false)
  const [showCreateMappingModal, setShowCreateMappingModal] = useState(false)
  const [editingMapping, setEditingMapping] = useState<IntercompanyMappingDto | null>(null)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState<string | null>(null)

  // Form states
  const [runForm, setRunForm] = useState<CreateConsolidationRunRequest>({
    parentCompanyId: parentCompanies[0]?.id || '',
    description: '',
    consolidationDate: new Date().toISOString(),
    fiscalYear: new Date().getFullYear(),
    fiscalPeriod: 1,
  })

  const [mappingForm, setMappingForm] = useState<CreateIntercompanyMappingRequest>({
    fromCompanyId: '',
    toCompanyId: '',
    fromAccountNumber: '',
    toAccountNumber: '',
    description: '',
  })

  const handleRunSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    createRunMutation.mutate(runForm)
    setShowCreateRunModal(false)
  }

  const handleMappingSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (editingMapping) {
      updateMappingMutation.mutate({ id: editingMapping.id, data: mappingForm })
    } else {
      createMappingMutation.mutate(mappingForm)
    }
    setShowCreateMappingModal(false)
    setEditingMapping(null)
  }

  const handleDeleteConfirm = (id: string) => {
    deleteMappingMutation.mutate(id)
    setShowDeleteConfirm(null)
  }

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader
          title={activeTab === 'runs' ? 'Consolidation Runs' : 'Intercompany Mappings'}
          description={activeTab === 'runs'
            ? 'Manage and execute consolidation runs for multi-company reporting.'
            : 'Configure due-to/due-from account mappings between companies for consolidation elimination.'}
        />
        <CardContent>
          <div className="flex gap-2 mb-4">
            <button
              onClick={() => setActiveTab('runs')}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                activeTab === 'runs'
                  ? 'bg-primary-600 text-white'
                  : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
              }`}
            >
              Consolidation Runs
            </button>
            <button
              onClick={() => setActiveTab('mappings')}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                activeTab === 'mappings'
                  ? 'bg-primary-600 text-white'
                  : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
              }`}
            >
              Intercompany Mappings
            </button>
          </div>

          {activeTab === 'runs' && (
            <>
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-lg font-semibold">Consolidation Runs</h3>
                <Button onClick={() => setShowCreateRunModal(true)} leftIcon={<Plus className="h-4 w-4" />}>
                  New Run
                </Button>
              </div>

              {runsQuery.isLoading ? (
                <div className="space-y-4">
                  <div className="animate-pulse h-32 bg-gray-100 dark:bg-gray-800 rounded" />
                </div>
              ) : runsQuery.error ? (
                <div className="text-red-600 dark:text-red-400">Error: {getErrorMessage(runsQuery.error)}</div>
              ) : runsQuery.data && runsQuery.data.length === 0 ? (
                <div className="text-center py-8 text-gray-500 dark:text-gray-400">
                  No consolidation runs found. Create your first run.
                </div>
              ) : (
                dataTable(
                  ['Description', 'Consolidation Date', 'Fiscal Period', 'Status', 'Created', 'Actions'],
                  (runsQuery.data || []).map(run => [
                    run.description || '—',
                    formatDate(run.consolidationDate),
                    `P${run.fiscalPeriodId?.slice(-1) || '—'}`,
                    statusBadge(run.status),
                    formatDate(run.createdOn),
                    <div className="flex items-center gap-2">
                      {run.status === 0 && (
                        <Button
                          variant="primary"
                          size="sm"
                          onClick={() => executeRunMutation.mutate(run.id)}
                          disabled={executeRunMutation.isPending}
                          leftIcon={<Play className="h-4 w-4" />}
                        >
                          Execute
                        </Button>
                      )}
                      {run.status === 1 && <Button variant="ghost" size="sm" disabled><Loader2 className="h-4 w-4 animate-spin" /></Button>}
                      {run.status === 2 && <Button variant="ghost" size="sm" disabled leftIcon={<CheckCircle className="h-4 w-4" />}>Completed</Button>}
                      {run.status === 3 && (
                        <Button variant="ghost" size="sm" disabled leftIcon={<XCircle className="h-4 w-4" />}>Failed</Button>
                      )}
                      <Button variant="ghost" size="sm" leftIcon={<Eye className="h-4 w-4" />} title="View Details">
                        View
                      </Button>
                    </div>
                  ])
                )
              )}
            </>
          )}

          {activeTab === 'mappings' && (
            <>
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-lg font-semibold">Intercompany Mappings</h3>
                <Button onClick={() => { setMappingForm({ fromCompanyId: '', toCompanyId: '', fromAccountNumber: '', toAccountNumber: '', description: '' }); setEditingMapping(null); setShowCreateMappingModal(true) }} leftIcon={<Plus className="h-4 w-4" />}>
                  New Mapping
                </Button>
              </div>

              {mappingsQuery.isLoading ? (
                <div className="space-y-4">
                  <div className="animate-pulse h-32 bg-gray-100 dark:bg-gray-800 rounded" />
                </div>
              ) : mappingsQuery.error ? (
                <div className="text-red-600 dark:text-red-400">Error: {getErrorMessage(mappingsQuery.error)}</div>
              ) : mappingsQuery.data && mappingsQuery.data.length === 0 ? (
                <div className="text-center py-8 text-gray-500 dark:text-gray-400">
                  No intercompany mappings configured. Create mappings for consolidation elimination.
                </div>
              ) : (
                dataTable(
                  ['From Company', 'From Account', 'To Company', 'To Account', 'Description', 'Status', 'Actions'],
                  (mappingsQuery.data || []).map(mapping => [
                    mapping.fromCompanyId,
                    mapping.fromAccountNumber,
                    mapping.toCompanyId,
                    mapping.toAccountNumber,
                    mapping.description || '—',
                    mapping.isActive
                      ? <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300"><CheckCircle className="h-3 w-3" />Active</span>
                      : <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300"><XCircle className="h-3 w-3" />Inactive</span>,
                    <div className="flex items-center gap-1">
                      <Button variant="ghost" size="sm" onClick={() => { setMappingForm(mapping); setEditingMapping(mapping); setShowCreateMappingModal(true) }} leftIcon={<Edit className="h-4 w-4" />} title="Edit" />
                      <Button variant="destructive" size="sm" onClick={() => setShowDeleteConfirm(mapping.id)} leftIcon={<Trash2 className="h-4 w-4" />} title="Delete" />
                    </div>
                  ])
                )
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Run Modal */}
      <Modal isOpen={showCreateRunModal} onClose={() => setShowCreateRunModal(false)} title="Create Consolidation Run">
        <form onSubmit={handleRunSubmit} className="space-y-4">
          <Select
            label="Parent Company"
            placeholder="Select parent company..."
            options={parentCompanies.map(c => ({ value: c.id, label: c.name }))}
            value={runForm.parentCompanyId}
            onChange={e => setRunForm({ ...runForm, parentCompanyId: e.target.value })}
            required
          />
          <Input
            label="Description"
            placeholder="e.g., Q4 2024 Consolidation"
            value={runForm.description}
            onChange={e => setRunForm({ ...runForm, description: e.target.value })}
            required
          />
          <Input
            label="Consolidation Date"
            type="date"
            value={runForm.consolidationDate.split('T')[0]}
            onChange={e => setRunForm({ ...runForm, consolidationDate: new Date(e.target.value).toISOString() })}
            required
          />
          <div className="grid grid-cols-2 gap-4">
            <Input
              label="Fiscal Year"
              type="number"
              value={runForm.fiscalYear}
              onChange={e => setRunForm({ ...runForm, fiscalYear: parseInt(e.target.value) })}
              required
            />
            <Input
              label="Fiscal Period"
              type="number"
              min="1"
              max="12"
              value={runForm.fiscalPeriod}
              onChange={e => setRunForm({ ...runForm, fiscalPeriod: parseInt(e.target.value) })}
              required
            />
          </div>
          <div className="flex justify-end gap-2 pt-4 border-t border-gray-200 dark:border-gray-700">
            <Button type="button" variant="secondary" onClick={() => setShowCreateRunModal(false)}>Cancel</Button>
            <Button type="submit" variant="primary" disabled={createRunMutation.isPending}>
              {createRunMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Create Run'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Create/Edit Mapping Modal */}
      <Modal isOpen={showCreateMappingModal} onClose={() => { setShowCreateMappingModal(false); setEditingMapping(null) }} title={editingMapping ? 'Edit Intercompany Mapping' : 'Create Intercompany Mapping'}>
        <form onSubmit={handleMappingSubmit} className="space-y-4">
          <Select
            label="From Company"
            placeholder="Select source company..."
            options={companies.map(c => ({ value: c.id, label: c.name }))}
            value={mappingForm.fromCompanyId}
            onChange={e => setMappingForm({ ...mappingForm, fromCompanyId: e.target.value })}
            required
          />
          <Input
            label="From Account Number"
            placeholder="e.g., 1310"
            value={mappingForm.fromAccountNumber}
            onChange={e => setMappingForm({ ...mappingForm, fromAccountNumber: e.target.value })}
            required
          />
          <Select
            label="To Company"
            placeholder="Select target company..."
            options={companies.map(c => ({ value: c.id, label: c.name }))}
            value={mappingForm.toCompanyId}
            onChange={e => setMappingForm({ ...mappingForm, toCompanyId: e.target.value })}
            required
          />
          <Input
            label="To Account Number"
            placeholder="e.g., 2310"
            value={mappingForm.toAccountNumber}
            onChange={e => setMappingForm({ ...mappingForm, toAccountNumber: e.target.value })}
            required
          />
          <Textarea
            label="Description"
            placeholder="e.g., Due to/from intercompany elimination"
            value={mappingForm.description}
            onChange={e => setMappingForm({ ...mappingForm, description: e.target.value })}
            rows={3}
          />
          <div className="flex justify-end gap-2 pt-4 border-t border-gray-200 dark:border-gray-700">
            <Button type="button" variant="secondary" onClick={() => { setShowCreateMappingModal(false); setEditingMapping(null) }}>Cancel</Button>
            <Button type="submit" variant="primary" disabled={createMappingMutation.isPending || updateMappingMutation.isPending}>
              {createMappingMutation.isPending || updateMappingMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : editingMapping ? 'Update' : 'Create'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <Modal isOpen={true} onClose={() => setShowDeleteConfirm(null)} title="Delete Mapping">
          <p className="text-gray-600 dark:text-gray-400 mb-6">Are you sure you want to delete this intercompany mapping? This action cannot be undone.</p>
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setShowDeleteConfirm(null)}>Cancel</Button>
            <Button variant="destructive" onClick={() => handleDeleteConfirm(showDeleteConfirm!)} disabled={deleteMappingMutation.isPending}>
              {deleteMappingMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Delete'}
            </Button>
          </div>
        </Modal>
      )}
    </div>
  )
}