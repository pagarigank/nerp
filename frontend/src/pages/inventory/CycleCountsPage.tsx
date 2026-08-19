import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, AlertCircle, Edit3 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getCycleCounts, getCycleCount, createCycleCount,
  startCycleCount, completeCycleCount, postCycleCount, cancelCycleCount,
  updateCycleCountLine,
  getWarehouses,
} from '@api/inventory'
import type {
  CycleCountSummary, CycleCountLineDto, CreateCycleCountRequest,
  CycleCountPostResult, WarehouseSummary,
} from '@/types/inventory'

const createSchema = z.object({
  warehouseId: z.string().min(1, 'Warehouse is required'),
  countNumber: z.string().min(1, 'Count # is required'),
  countDate: z.string().min(1, 'Date is required'),
  notes: z.string().optional(),
})
type CreateForm = z.infer<typeof createSchema>

function fieldError(msg?: string) { return msg ? { error: msg } : {} }
function formatCurrency(n: number) { return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n) }

export function CycleCountsPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [viewId, setViewId] = useState<string | null>(null)
  const [postResult, setPostResult] = useState<CycleCountPostResult | null>(null)

  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['inventory', 'cycle-counts'],
    queryFn: () => getCycleCounts(),
  })

  const { data: detail } = useQuery({
    queryKey: ['inventory', 'cycle-count', viewId],
    queryFn: () => getCycleCount(viewId!),
    enabled: !!viewId,
  })

  const start = useMutation({
    mutationFn: (id: string) => startCycleCount(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'cycle-counts'] }); qc.invalidateQueries({ queryKey: ['inventory', 'cycle-count'] }) },
    onError: e => setErr(getErrorMessage(e)),
  })
  const complete = useMutation({
    mutationFn: (id: string) => completeCycleCount(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'cycle-counts'] }); qc.invalidateQueries({ queryKey: ['inventory', 'cycle-count'] }) },
    onError: e => setErr(getErrorMessage(e)),
  })
  const post = useMutation({
    mutationFn: (id: string) => postCycleCount(id),
    onSuccess: (result) => { qc.invalidateQueries({ queryKey: ['inventory', 'cycle-counts'] }); qc.invalidateQueries({ queryKey: ['inventory', 'cycle-count'] }); setPostResult(result) },
    onError: e => setErr(getErrorMessage(e)),
  })
  const cancel = useMutation({
    mutationFn: (id: string) => cancelCycleCount(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'cycle-counts'] }); qc.invalidateQueries({ queryKey: ['inventory', 'cycle-count'] }) },
    onError: e => setErr(getErrorMessage(e)),
  })

  const { register, handleSubmit, reset, formState: { errors: createErrors } } = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { warehouseId: '', countNumber: `CC-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}`, countDate: new Date().toISOString().slice(0, 10), notes: '' },
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateForm) => {
      const body: CreateCycleCountRequest = {
        companyId: '11111111-1111-1111-1111-111111111111',
        warehouseId: data.warehouseId,
        countNumber: data.countNumber,
        countDate: data.countDate,
        notes: data.notes || null,
        lines: [],
      }
      return createCycleCount(body)
    },
    onSuccess: (id) => { qc.invalidateQueries({ queryKey: ['inventory', 'cycle-counts'] }); setShowCreate(false); reset(); setViewId(id) },
    onError: e => setErr(getErrorMessage(e)),
  })

  const warehouseOptions = useMemo(() => warehouses.map((w: WarehouseSummary) => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` })), [warehouses])
  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter((r: CycleCountSummary) => r.countNumber.toLowerCase().includes(q))
  }, [rows, search])

  const act = (m: { mutate: (id: string) => void }, id: string) => { setErr(null); m.mutate(id) }

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"><AlertCircle className="h-5 w-5" /> {err}</div>}

      {/* Post Result Modal */}
      <Modal isOpen={!!postResult} onClose={() => setPostResult(null)} title="Post Result" footer={<Button onClick={() => setPostResult(null)}>Close</Button>}>
        {postResult && (
          <div className="space-y-3">
            <div className="grid grid-cols-3 gap-4 text-center">
              <div><p className="text-2xl font-bold">{postResult.totalLines}</p><p className="text-xs text-gray-500">Total Lines</p></div>
              <div><p className="text-2xl font-bold text-amber-600">{postResult.variancesFound}</p><p className="text-xs text-gray-500">Variances</p></div>
              <div><p className="text-2xl font-bold text-green-600">{postResult.adjustmentsCreated}</p><p className="text-xs text-gray-500">Adjustments</p></div>
            </div>
            {postResult.variances.length > 0 && (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b"><th className="px-2 py-1 text-left">Item</th><th className="px-2 py-1 text-right">System</th><th className="px-2 py-1 text-right">Counted</th><th className="px-2 py-1 text-right">Variance</th><th className="px-2 py-1 text-right">Value</th></tr></thead>
                  <tbody>
                    {postResult.variances.map((v, i) => (
                      <tr key={i} className="border-b"><td className="px-2 py-1">{v.itemCode}</td><td className="px-2 py-1 text-right tabular-nums">{v.systemQuantity}</td><td className="px-2 py-1 text-right tabular-nums">{v.countedQuantity}</td><td className={`px-2 py-1 text-right tabular-nums font-medium ${v.variance > 0 ? 'text-green-600' : 'text-red-600'}`}>{v.variance > 0 ? '+' : ''}{v.variance}</td><td className="px-2 py-1 text-right tabular-nums">{formatCurrency(v.varianceValue)}</td></tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </Modal>

      {/* Create Form Modal */}
      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)} title="New Cycle Count" size="lg"
        footer={<><Button variant="secondary" onClick={() => setShowCreate(false)} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit((d) => createMutation.mutate(d))} isLoading={createMutation.isPending}>Create Count</Button></>}>
        <form className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-4">
            <Input {...register('countNumber')} label="Count Number" {...fieldError(createErrors.countNumber?.message)} required />
            <Input {...register('countDate')} type="date" label="Count Date" {...fieldError(createErrors.countDate?.message)} required />
            <Select {...register('warehouseId')} label="Warehouse" options={warehouseOptions} placeholder="Select warehouse..." {...fieldError(createErrors.warehouseId?.message)} required />
          </div>
          <Textarea {...register('notes')} label="Notes" placeholder="Optional notes..." rows={2} />
        </form>
      </Modal>

      {/* Count Detail Modal */}
      <Modal isOpen={!!viewId} onClose={() => setViewId(null)} title={detail ? `Cycle Count: ${detail.countNumber}` : 'Loading...'} size="xl"
        footer={<Button variant="secondary" onClick={() => setViewId(null)}>Close</Button>}>
        {detail && (
          <CycleCountDetailView
            detail={detail}
            onStart={() => act(start, detail.id)}
            onComplete={() => act(complete, detail.id)}
            onPost={() => act(post, detail.id)}
            onCancel={() => act(cancel, detail.id)}
            isPending={start.isPending || complete.isPending || post.isPending || cancel.isPending}
          />
        )}
      </Modal>

      {/* List */}
      <Card>
        <CardHeader title="Cycle Counts" description={`${rows.length} count(s)`}
          action={<Button variant="primary" size="sm" onClick={() => { setErr(null); setShowCreate(true) }} leftIcon={<Plus className="h-4 w-4" />}>New Count</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search by count number..." leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading...</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No cycle counts yet.'}</p> :
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Count #</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Lines</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {filtered.map((r: CycleCountSummary) => (
                      <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer" onClick={() => setViewId(r.id)}>
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.countNumber}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.countDate).toLocaleDateString()}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.lines?.length ?? 0}</td>
                        <td className="px-3 py-3"><Badge variant={r.status === 'Posted' ? 'success' : r.status === 'InProgress' ? 'warning' : r.status === 'Completed' ? 'info' : 'neutral'} size="sm" dot>{r.status}</Badge></td>
                        <td className="px-3 py-3 text-right">
                          <div className="flex justify-end gap-1" onClick={e => e.stopPropagation()}>
                            {r.status === 'Draft' && <Button size="sm" variant="outline" onClick={() => act(start, r.id)}>Start</Button>}
                            {r.status === 'InProgress' && <Button size="sm" variant="outline" onClick={() => act(complete, r.id)}>Complete</Button>}
                            {r.status === 'Completed' && <Button size="sm" variant="primary" onClick={() => act(post, r.id)}>Post</Button>}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>}
        </CardContent>
      </Card>
    </div>
  )
}

function CycleCountDetailView({ detail, onStart, onComplete, onPost, onCancel, isPending }: {
  detail: CycleCountSummary
  onStart: () => void
  onComplete: () => void
  onPost: () => void
  onCancel: () => void
  isPending: boolean
}) {
  const qc = useQueryClient()
  const [editingLine, setEditingLine] = useState<string | null>(null)
  const [countedQty, setCountedQty] = useState('')
  const [lineNotes, setLineNotes] = useState('')
  const [lineErr, setLineErr] = useState<string | null>(null)

  const updateLine = useMutation({
    mutationFn: ({ lineId, qty, notes }: { lineId: string; qty: number; notes: string }) =>
      updateCycleCountLine(detail.id, lineId, { countedQuantity: qty, notes: notes || null }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'cycle-count', detail.id] }); setEditingLine(null); setLineErr(null) },
    onError: e => setLineErr(getErrorMessage(e)),
  })

  const lines = detail.lines ?? []
  const missingCount = lines.filter(l => l.countedQuantity == null).length

  function startEdit(line: CycleCountLineDto) {
    setEditingLine(line.id)
    setCountedQty(line.countedQuantity?.toString() ?? '')
    setLineNotes(line.notes ?? '')
    setLineErr(null)
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Badge variant={detail.status === 'Posted' ? 'success' : detail.status === 'InProgress' ? 'warning' : 'neutral'} size="md" dot>{detail.status}</Badge>
          <span className="text-sm text-gray-500">{lines.length} line(s){missingCount > 0 && detail.status === 'InProgress' ? ` (${missingCount} need count)` : ''}</span>
          {detail.notes && <span className="text-xs text-gray-400 italic">{detail.notes}</span>}
        </div>
        <div className="flex gap-2">
          {detail.status === 'Draft' && <Button size="sm" variant="primary" onClick={onStart} disabled={isPending}>Start Count</Button>}
          {detail.status === 'InProgress' && <Button size="sm" variant="primary" onClick={onComplete} disabled={isPending || missingCount > 0}>{missingCount > 0 ? `${missingCount} lines missing` : 'Complete'}</Button>}
          {detail.status === 'Completed' && <Button size="sm" variant="primary" onClick={onPost} disabled={isPending}>Post Adjustments</Button>}
          {detail.status !== 'Posted' && <Button size="sm" variant="ghost" className="text-red-600" onClick={onCancel} disabled={isPending}>Cancel</Button>}
        </div>
      </div>

      {lineErr && <div className="p-3 rounded bg-red-50 text-red-700 text-sm">{lineErr}</div>}

      <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Item</th>
              <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">System Qty</th>
              <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Counted Qty</th>
              <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Variance</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Lot/Serial</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Notes</th>
              {(detail.status === 'InProgress' || detail.status === 'Draft') && <th className="px-3 py-2 w-10" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {lines.map(line => (
              <tr key={line.id} className="bg-white dark:bg-gray-900">
                <td className="px-3 py-2 text-sm font-medium text-gray-900 dark:text-white">{line.itemCode ?? line.itemId.slice(0, 8)}</td>
                <td className="px-3 py-2 text-sm text-right tabular-nums text-gray-700 dark:text-gray-300">{line.systemQuantity}</td>
                <td className="px-3 py-2 text-sm text-right tabular-nums font-medium text-gray-900 dark:text-white">
                  {editingLine === line.id ? (
                    <input type="number" step="0.01" value={countedQty} onChange={e => setCountedQty(e.target.value)}
                      className="w-24 text-right rounded border border-blue-300 px-2 py-1 tabular-nums" autoFocus />
                  ) : (
                    line.countedQuantity ?? <span className="text-gray-400 italic">Not counted</span>
                  )}
                </td>
                <td className={`px-3 py-2 text-sm text-right tabular-nums font-medium ${line.variance != null && line.variance !== 0 ? (line.variance > 0 ? 'text-green-600' : 'text-red-600') : 'text-gray-400'}`}>
                  {line.variance != null ? (line.variance > 0 ? '+' : '') + line.variance : '—'}
                </td>
                <td className="px-3 py-2 text-xs text-gray-500">
                  {line.lotNumber && <span>Lot: {line.lotNumber}</span>}
                  {line.serialNumber && <span>SN: {line.serialNumber}</span>}
                  {!line.lotNumber && !line.serialNumber && '—'}
                </td>
                <td className="px-3 py-2 text-xs text-gray-500">{line.notes || '—'}</td>
                {(detail.status === 'InProgress' || detail.status === 'Draft') && (
                  <td className="px-3 py-2">
                    {editingLine === line.id ? (
                      <div className="flex gap-1">
                        <Button size="sm" variant="primary" onClick={() => { const qty = parseFloat(countedQty); if (isNaN(qty)) { setLineErr('Invalid quantity'); return } updateLine.mutate({ lineId: line.id, qty, notes: lineNotes }) }} disabled={updateLine.isPending}>Save</Button>
                        <Button size="sm" variant="ghost" onClick={() => setEditingLine(null)}>Cancel</Button>
                      </div>
                    ) : (
                      <button className="text-blue-600 hover:text-blue-800" onClick={() => startEdit(line)}><Edit3 className="h-4 w-4" /></button>
                    )}
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
