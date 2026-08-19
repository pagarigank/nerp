import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle2, Plus, UserCheck, XCircle } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Modal } from '@components/ui/Modal'
import { Input } from '@components/ui/Input'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getCollectionNotes,
  createCollectionNote,
  assignCollectionNote,
  closeCollectionNote,
  reopenCollectionNote,
  getCollectionsDashboard,
  getCustomers,
} from '@api/ar'
import type {
  CreateCollectionNoteRequest,
  CollectionNoteType,
  CollectionNoteStatus,
  ArCustomer,
} from '@/types/ar'

const NOTE_TYPE_OPTIONS: CollectionNoteType[] = ['Call', 'Email', 'Letter', 'Visit', 'Legal', 'Promise']
const NOTE_STATUS_MAP: Record<string, { variant: 'neutral' | 'info' | 'warning' | 'success' | 'error'; label: string }> = {
  Open: { variant: 'info', label: 'Open' },
  InProgress: { variant: 'warning', label: 'In Progress' },
  Resolved: { variant: 'success', label: 'Resolved' },
  Closed: { variant: 'neutral', label: 'Closed' },
  Escalated: { variant: 'error', label: 'Escalated' },
}

export function CollectionsPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [isOpen, setIsOpen] = useState(false)

  const { data: notes = [], isLoading } = useQuery({
    queryKey: ['ar', 'collection-notes'],
    queryFn: () => getCollectionNotes(),
  })
  const { data: dashboard } = useQuery({
    queryKey: ['ar', 'collections-dashboard'],
    queryFn: () => getCollectionsDashboard(),
  })
  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: () => getCustomers(),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ar', 'collection-notes'] })
    queryClient.invalidateQueries({ queryKey: ['ar', 'collections-dashboard'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: CreateCollectionNoteRequest) => createCollectionNote(data),
    onSuccess: () => {
      setError(null)
      setIsOpen(false)
      invalidate()
    },
    onError: err => setError(getErrorMessage(err)),
  })

  const actionMutation = useMutation({
    mutationFn: ({ id, action }: { id: string; action: 'close' | 'reopen' | 'assign' }) =>
      action === 'close'
        ? closeCollectionNote(id)
        : action === 'reopen'
          ? reopenCollectionNote(id)
          : assignCollectionNote(id, 'collector@erp.com'),
    onSuccess: invalidate,
    onError: err => setError(getErrorMessage(err)),
  })

  return (
    <div className="space-y-6">
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label="Open Notes" value={dashboard?.openNotes ?? 0} />
        <StatCard label="Escalated" value={dashboard?.escalatedNotes ?? 0} accent="error" />
        <StatCard label="Promises to Pay" value={dashboard?.promisesToPay ?? 0} accent="warning" />
        <StatCard label="Total Outstanding" value={formatCurrency(dashboard?.totalOutstanding ?? 0)} />
      </div>

      <Card>
        <CardHeader
          title="Collection Notes"
          description="Track collections activity per customer."
          action={
            <Button variant="primary" size="sm" leftIcon={<Plus className="h-4 w-4" />} onClick={() => setIsOpen(true)}>
              Add Note
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : notes.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No collection notes yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Note</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Follow Up</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {notes.map(note => (
                    <tr key={note.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3">
                        <p className="font-medium text-gray-900 dark:text-white">{customerName(customers, note.customerId)}</p>
                        {note.assignedTo && <p className="text-xs text-gray-500 dark:text-gray-400">→ {note.assignedTo}</p>}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{note.type}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300 max-w-xs truncate">{note.note}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{note.followUpDate ? formatDate(note.followUpDate) : '—'}</td>
                      <td className="px-3 py-3">
                        <StatusBadge status={note.status} />
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          {note.status !== 'Closed' && note.status !== 'Resolved' ? (
                            <Button variant="outline" size="sm" leftIcon={<CheckCircle2 className="h-4 w-4" />} onClick={() => actionMutation.mutate({ id: note.id, action: 'close' })}>
                              Close
                            </Button>
                          ) : (
                            <Button variant="outline" size="sm" leftIcon={<XCircle className="h-4 w-4" />} onClick={() => actionMutation.mutate({ id: note.id, action: 'reopen' })}>
                              Reopen
                            </Button>
                          )}
                          <Button variant="ghost" size="sm" leftIcon={<UserCheck className="h-4 w-4" />} onClick={() => actionMutation.mutate({ id: note.id, action: 'assign' })}>
                            Assign
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <NewNoteModal
        isOpen={isOpen}
        customers={customers}
        onClose={() => setIsOpen(false)}
        onSubmit={data => createMutation.mutate(data)}
        isSubmitting={createMutation.isPending}
      />
    </div>
  )
}

function StatCard({ label, value, accent }: { label: string; value: string | number; accent?: 'error' | 'warning' }) {
  const color = accent === 'error' ? 'text-red-600 dark:text-red-400' : accent === 'warning' ? 'text-amber-600 dark:text-amber-400' : 'text-gray-900 dark:text-white'
  return (
    <Card>
      <CardContent>
        <p className="text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">{label}</p>
        <p className={`mt-1 text-2xl font-bold tabular-nums ${color}`}>{value}</p>
      </CardContent>
    </Card>
  )
}

function StatusBadge({ status }: { status: CollectionNoteStatus }) {
  const map = NOTE_STATUS_MAP[status] ?? { variant: 'neutral' as const, label: status }
  const cls =
    map.variant === 'error'
      ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300'
      : map.variant === 'warning'
        ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300'
        : map.variant === 'success'
          ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-300'
          : 'bg-gray-100 text-gray-700 dark:bg-gray-700/40 dark:text-gray-300'
  return <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${cls}`}>{map.label}</span>
}

function customerName(customers: ArCustomer[], id: string): string {
  return customers.find(c => c.id === id)?.name ?? id.slice(0, 8)
}

interface NewNoteModalProps {
  isOpen: boolean
  customers: ArCustomer[]
  onClose: () => void
  onSubmit: (data: CreateCollectionNoteRequest) => void
  isSubmitting: boolean
}

function NewNoteModal({ isOpen, customers, onClose, onSubmit, isSubmitting }: NewNoteModalProps) {
  const [customerId, setCustomerId] = useState('')
  const [type, setType] = useState<CollectionNoteType>('Call')
  const [note, setNote] = useState('')
  const [followUpDate, setFollowUpDate] = useState('')

  const submit = () => {
    if (!customerId || !note.trim()) return
    onSubmit({
      companyId: customers[0]?.id ?? '',
      customerId,
      note: note.trim(),
      author: 'collector@erp.com',
      type,
      followUpDate: followUpDate ? new Date(followUpDate).toISOString() : null,
    })
    setCustomerId('')
    setNote('')
    setFollowUpDate('')
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="New Collection Note">
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Customer</label>
          <select value={customerId} onChange={e => setCustomerId(e.target.value)} className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm">
            <option value="">Select customer…</option>
            {customers.map(c => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Type</label>
          <select value={type} onChange={e => setType(e.target.value as CollectionNoteType)} className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm">
            {NOTE_TYPE_OPTIONS.map(t => (
              <option key={t} value={t}>{t}</option>
            ))}
          </select>
        </div>
        <Input label="Note" value={note} onChange={e => setNote(e.target.value)} placeholder="Collection note…" />
        <Input label="Follow-up Date" type="date" value={followUpDate} onChange={e => setFollowUpDate(e.target.value)} />
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={submit} isLoading={isSubmitting} disabled={!customerId || !note.trim()}>Save</Button>
        </div>
      </div>
    </Modal>
  )
}
