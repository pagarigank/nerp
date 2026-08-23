// Project workspace tabs: ASC 606 obligations, documents, EAC trend.
import { useState } from 'react'
import { DollarSign, Pencil, Plus, Trash2 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import {
  getAsc606Obligations,
  createAsc606Obligation,
  updateAsc606Obligation,
  deleteAsc606Obligation,
  allocateAsc606ContractPrice,
  getAsc606RecognitionStatus,
  recognizeAsc606Revenue,
  getAsc606FiveStepSummary,
  getProjectDocuments,
  addProjectDocument,
  deleteProjectDocument,
  getEacTrend,
} from '@api/projectAccounting'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type {
  ProjectSummary,
  PerformanceObligation,
  ProjectDocumentItem,
  EacTrendPoint,
} from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')
const PCT = (v?: number | null) => (v != null && !Number.isNaN(v) ? `${v.toFixed(1)}%` : '—')

function KpiCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white p-3 dark:border-gray-700 dark:bg-gray-800">
      <p className="text-xs text-gray-500">{label}</p>
      <p className="text-lg font-semibold text-gray-900 dark:text-white">{value}</p>
    </div>
  )
}

export function Asc606Tab({ project, setError }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [editObligation, setEditObligation] = useState<PerformanceObligation | null>(null)
  const [showAllocate, setShowAllocate] = useState(false)
  const [recognizing, setRecognizing] = useState<PerformanceObligation | null>(null)
  const [form, setForm] = useState({ description: '', transactionPriceAllocated: 0, standaloneSellingPriceBasis: '' })
  const [allocatePrice, setAllocatePrice] = useState('')
  const [recognizeAmount, setRecognizeAmount] = useState('')

  const { data: obligations = [] } = useQuery({
    queryKey: ['projects', project.id, 'asc606'],
    queryFn: () => getAsc606Obligations(project.id),
  })
  const { data: status } = useQuery({
    queryKey: ['projects', project.id, 'asc606', 'status'],
    queryFn: () => getAsc606RecognitionStatus(project.id),
  })
  const { data: fiveStep } = useQuery({
    queryKey: ['projects', project.id, 'asc606', 'five-step'],
    queryFn: () => getAsc606FiveStepSummary(project.id),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'asc606'] })

  const addMutation = useMutation({
    mutationFn: (data: any) => createAsc606Obligation(project.id, data),
    onSuccess: () => { invalidate(); setShowAdd(false); setForm({ description: '', transactionPriceAllocated: 0, standaloneSellingPriceBasis: '' }) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })
  const updateMutation = useMutation({
    mutationFn: ({ obligationId, data }: { obligationId: string; data: any }) => updateAsc606Obligation(project.id, obligationId, data),
    onSuccess: () => { invalidate(); setEditObligation(null) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })
  const deleteMutation = useMutation({
    mutationFn: (obligationId: string) => deleteAsc606Obligation(project.id, obligationId),
    onSuccess: invalidate,
    onError: (e: any) => setError(getErrorMessage(e)),
  })
  const allocateMutation = useMutation({
    mutationFn: () => allocateAsc606ContractPrice(project.id, Number(allocatePrice)),
    onSuccess: () => { invalidate(); setShowAllocate(false); setAllocatePrice('') },
    onError: (e: any) => setError(getErrorMessage(e)),
  })
  const recognizeMutation = useMutation({
    mutationFn: ({ obligationId, amount }: { obligationId: string; amount: number }) =>
      recognizeAsc606Revenue(project.id, obligationId, { amount }),
    onSuccess: (result: any) => { invalidate(); setRecognizing(null); setRecognizeAmount(''); alert(result.glPostingPending ? 'Recognition recorded. GL posting pending.' : 'Recognition recorded and posted.') },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const columns: DataTableColumn<PerformanceObligation>[] = [
    { key: 'description', header: 'Description' },
    { key: 'standaloneSellingPriceBasis', header: 'SSP Basis', render: (r: PerformanceObligation) => r.standaloneSellingPriceBasis ?? '—' },
    { key: 'transactionPriceAllocated', header: 'Allocated Price', align: 'right', render: (r: PerformanceObligation) => MONEY(r.transactionPriceAllocated) },
    { key: 'recognizedRevenueToDate', header: 'Recognized', align: 'right', render: (r: PerformanceObligation) => MONEY(r.recognizedRevenueToDate) },
    { key: 'percentSatisfied', header: '% Satisfied', align: 'right', render: (r: PerformanceObligation) => PCT(r.percentSatisfied) },
    { key: 'status', header: 'Status' },
    { key: 'actions', header: 'Actions', render: (_: unknown, r: PerformanceObligation) => (
      <div className="flex gap-1">
        <Button size="sm" onClick={() => { setRecognizing(r); setRecognizeAmount(String(r.transactionPriceAllocated - r.recognizedRevenueToDate)) }}>Recognize</Button>
        {r.canEditOrDelete && (
          <>
            <Button size="sm" variant="outline" onClick={() => { setEditObligation(r); setForm({ description: r.description, transactionPriceAllocated: r.transactionPriceAllocated, standaloneSellingPriceBasis: r.standaloneSellingPriceBasis ?? '' }) }}><Pencil className="h-3.5 w-3.5" /></Button>
            <Button size="sm" variant="destructive" onClick={() => deleteMutation.mutate(r.id)}><Trash2 className="h-3.5 w-3.5" /></Button>
          </>
        )}
      </div>
    )},
  ]

  return (
    <div className="space-y-4">
      {status && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <KpiCard label="Budget at Completion" value={MONEY(status.budgetAtCompletion)} />
          <KpiCard label="Costs Posted" value={MONEY(status.costsPostedToDate)} />
          <KpiCard label="EAC" value={MONEY(status.estimateAtCompletion)} />
          <KpiCard label="Cost-to-Cost % Complete" value={PCT(status.costToCostPercent)} />
        </div>
      )}

      <div className="flex justify-between">
        <h3 className="font-semibold text-gray-900 dark:text-white">Performance Obligations</h3>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setShowAdd(true)}><Plus className="h-4 w-4 mr-1" /> Add Obligation</Button>
          <Button onClick={() => setShowAllocate(true)}><DollarSign className="h-4 w-4 mr-1" /> Allocate Contract Price</Button>
        </div>
      </div>

      <DataTable data={obligations as PerformanceObligation[]} columns={columns} emptyMessage="No performance obligations defined." />

      {fiveStep && (
        <div className="space-y-3 rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
          <h3 className="font-semibold text-gray-900 dark:text-white">Five-Step Summary</h3>
          <div className="text-sm space-y-1">
            <p><span className="text-gray-500">Contract:</span> {fiveStep.contract.projectCode} — {fiveStep.contract.name} ({fiveStep.contract.projectType}, {fiveStep.contract.status})</p>
            <p><span className="text-gray-500">Contract Value:</span> {MONEY(fiveStep.contract.contractValue)}</p>
            <p><span className="text-gray-500">Total Allocated Transaction Price:</span> {MONEY(fiveStep.totalContractPriceAllocated)}</p>
            <p><span className="text-gray-500">Total Recognized Revenue:</span> {MONEY(fiveStep.totalRecognizedRevenue)}</p>
            <p><span className="text-gray-500">Pending Change Orders:</span> {MONEY(fiveStep.pendingChangeOrderAmount)}</p>
            <p className="text-xs text-amber-600">{fiveStep.variableConsiderationConstraintNote}</p>
          </div>
          <table className="w-full text-sm">
            <thead><tr className="border-b dark:border-gray-700">
              <th className="px-2 py-1 text-left text-gray-500">Obligation</th>
              <th className="px-2 py-1 text-right text-gray-500">Share %</th>
              <th className="px-2 py-1 text-right text-gray-500">Allocated</th>
              <th className="px-2 py-1 text-right text-gray-500">Recognized</th>
              <th className="px-2 py-1 text-right text-gray-500">% Satisfied</th>
              <th className="px-2 py-1 text-left text-gray-500">Status</th>
            </tr></thead>
            <tbody>
              {fiveStep.obligations.map(o => (
                <tr key={o.id} className="border-b dark:border-gray-700/50">
                  <td className="px-2 py-1 font-medium">{o.description}</td>
                  <td className="px-2 py-1 text-right">{PCT(o.allocationSharePercent)}</td>
                  <td className="px-2 py-1 text-right">{MONEY(o.transactionPriceAllocated)}</td>
                  <td className="px-2 py-1 text-right">{MONEY(o.recognizedRevenueToDate)}</td>
                  <td className="px-2 py-1 text-right">{PCT(o.percentSatisfied)}</td>
                  <td className="px-2 py-1">{o.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {(showAdd || editObligation) && (
        <Modal title={editObligation ? 'Edit Obligation' : 'Add Obligation'} isOpen={showAdd || !!editObligation} onClose={() => { setShowAdd(false); setEditObligation(null) }}>
          <div className="space-y-4">
            <Input label="Description" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Allocated Transaction Price" type="number" step="0.01" value={form.transactionPriceAllocated} onChange={e => setForm(f => ({ ...f, transactionPriceAllocated: Number(e.target.value) }))} />
              <Input label="SSP Basis" value={form.standaloneSellingPriceBasis} onChange={e => setForm(f => ({ ...f, standaloneSellingPriceBasis: e.target.value }))} placeholder="e.g. AdjustedMarket" />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => { setShowAdd(false); setEditObligation(null) }}>Cancel</Button>
              <Button onClick={() => {
                if (editObligation) {
                  updateMutation.mutate({ obligationId: editObligation.id, data: { description: form.description, transactionPriceAllocated: form.transactionPriceAllocated, standaloneSellingPriceBasis: form.standaloneSellingPriceBasis || undefined } })
                } else {
                  addMutation.mutate({ description: form.description, transactionPriceAllocated: form.transactionPriceAllocated, standaloneSellingPriceBasis: form.standaloneSellingPriceBasis || undefined })
                }
              }}>{editObligation ? 'Save' : 'Add'}</Button>
            </div>
          </div>
        </Modal>
      )}

      {showAllocate && (
        <Modal title="Allocate Total Contract Price" isOpen={showAllocate} onClose={() => setShowAllocate(false)}>
          <div className="space-y-4">
            <p className="text-sm text-gray-500">Distributes the total contract price across obligations proportionate to their standalone selling price (equal split when no basis exists). Blocked once any revenue has been recognized.</p>
            <Input label="Total Contract Price" type="number" step="0.01" value={allocatePrice} onChange={e => setAllocatePrice(e.target.value)} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => setShowAllocate(false)}>Cancel</Button>
              <Button onClick={() => allocateMutation.mutate()}>Allocate</Button>
            </div>
          </div>
        </Modal>
      )}

      {recognizing && (
        <Modal title={`Recognize Revenue — ${recognizing.description}`} isOpen={!!recognizing} onClose={() => setRecognizing(null)}>
          <div className="space-y-4">
            <p className="text-sm text-gray-500">Allocated: {MONEY(recognizing.transactionPriceAllocated)} | Recognized to date: {MONEY(recognizing.recognizedRevenueToDate)}</p>
            <Input label="Amount to Recognize" type="number" step="0.01" value={recognizeAmount} onChange={e => setRecognizeAmount(e.target.value)} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => setRecognizing(null)}>Cancel</Button>
              <Button onClick={() => recognizeMutation.mutate({ obligationId: recognizing.id, amount: Number(recognizeAmount) })}>Recognize</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

export function DocumentsTab({ project, setError }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const queryClient = useQueryClient()
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState({ name: '', documentType: 'Contract', fileReference: '', contentType: '' })

  const { data: documents = [] } = useQuery({
    queryKey: ['projects', project.id, 'documents'],
    queryFn: () => getProjectDocuments(project.id),
  })

  const addMutation = useMutation({
    mutationFn: (data: any) => addProjectDocument(project.id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'documents'] }); setShowAdd(false); setForm({ name: '', documentType: 'Contract', fileReference: '', contentType: '' }) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const deleteMutation = useMutation({
    mutationFn: (documentId: string) => deleteProjectDocument(project.id, documentId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'documents'] }),
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const columns: DataTableColumn<ProjectDocumentItem>[] = [
    { key: 'name', header: 'Name' },
    { key: 'documentType', header: 'Type' },
    { key: 'fileReference', header: 'File Reference' },
    { key: 'contentType', header: 'Content Type', render: (r: ProjectDocumentItem) => r.contentType ?? '—' },
    { key: 'sizeBytes', header: 'Size', align: 'right', render: (r: ProjectDocumentItem) => (r.sizeBytes != null ? `${(r.sizeBytes / 1024).toFixed(1)} KB` : '—') },
    { key: 'uploadedOn', header: 'Uploaded', render: (r: ProjectDocumentItem) => new Date(r.uploadedOn).toLocaleDateString() },
    { key: 'uploadedBy', header: 'By' },
    { key: 'actions', header: '', render: (_: unknown, r: ProjectDocumentItem) => (
      <Button size="sm" variant="destructive" onClick={() => deleteMutation.mutate(r.id)}><Trash2 className="h-3.5 w-3.5" /></Button>
    )},
  ]

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button onClick={() => setShowAdd(true)}><Plus className="h-4 w-4 mr-1" /> Add Document</Button>
      </div>
      <DataTable data={documents as ProjectDocumentItem[]} columns={columns} emptyMessage="No documents attached." />
      {showAdd && (
        <Modal title="Add Document" isOpen={showAdd} onClose={() => setShowAdd(false)}>
          <div className="space-y-4">
            <Input label="Name" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
            <Select label="Document Type" value={form.documentType} onChange={e => setForm(f => ({ ...f, documentType: e.target.value }))}
              options={['Contract', 'Drawing', 'Correspondence', 'Other'].map(t => ({ value: t, label: t }))} />
            <Input label="File Reference (storage path or URI)" value={form.fileReference} onChange={e => setForm(f => ({ ...f, fileReference: e.target.value }))} placeholder="s3://bucket/project/contract.pdf" />
            <Input label="Content Type (optional)" value={form.contentType} onChange={e => setForm(f => ({ ...f, contentType: e.target.value }))} placeholder="application/pdf" />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => setShowAdd(false)}>Cancel</Button>
              <Button onClick={() => addMutation.mutate({ name: form.name, documentType: form.documentType, fileReference: form.fileReference, contentType: form.contentType || undefined })}>Add</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

export function EacTrendTab({ project }: { project: ProjectSummary }) {
  const { data: points = [], isLoading } = useQuery({
    queryKey: ['projects', project.id, 'eac-trend'],
    queryFn: () => getEacTrend(project.id),
  })

  const rows = points as EacTrendPoint[]

  return (
    <div className="space-y-4">
      <p className="text-sm text-gray-500">
        Daily estimate-at-completion snapshots captured by the EAC recalculation job. Estimated margin = (contract value − EAC) ÷ contract value.
      </p>
      {isLoading && <p className="text-sm text-gray-400">Loading…</p>}
      {!isLoading && rows.length === 0 && (
        <p className="text-sm text-gray-400">No EAC snapshots yet. Snapshots are captured daily for active projects by the nightly job.</p>
      )}
      {rows.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">Captured On</th>
                <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Original Budget</th>
                <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">BAC</th>
                <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">EAC</th>
                <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Est. Margin %</th>
                <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Pending COs</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
              {rows.map((row, i) => (
                <tr key={i}>
                  <td className="px-4 py-2 text-sm">{new Date(row.capturedOn).toLocaleString()}</td>
                  <td className="px-4 py-2 text-sm text-right tabular-nums">{MONEY(row.originalBudget)}</td>
                  <td className="px-4 py-2 text-sm text-right tabular-nums">{MONEY(row.budgetAtCompletion)}</td>
                  <td className="px-4 py-2 text-sm text-right tabular-nums">{MONEY(row.estimateAtCompletion)}</td>
                  <td className={`px-4 py-2 text-sm text-right tabular-nums ${row.estimatedMarginPct < 0 ? 'text-red-600' : row.estimatedMarginPct < 5 ? 'text-amber-600' : 'text-green-600'}`}>{PCT(row.estimatedMarginPct)}</td>
                  <td className="px-4 py-2 text-sm text-right tabular-nums">{row.pendingChangeOrderAmount != null ? MONEY(row.pendingChangeOrderAmount) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
