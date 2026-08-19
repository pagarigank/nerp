import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { ArrowLeft, Plus, AlertCircle, Trash2 } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import {
  getJournalBatch,
  addJournalBatchLine,
  removeJournalBatchLine,
  releaseJournalBatch,
  postJournalBatch,
  reverseJournalBatch,
} from '@api/gl'
import { getAccounts } from '@api/platform'
import type { GlJournalEntryLine } from '@/types/gl'
import { journalBatchStatusMap } from './statusMaps'

interface AddLineModalProps {
  isOpen: boolean
  onClose: () => void
  onSave: (line: { accountId: string; debit?: number | null; credit?: number | null; reference?: string | null }) => void
  isSaving: boolean
  error: string | null
}

function AddLineModal({ isOpen, onClose, onSave, isSaving, error }: AddLineModalProps) {
  const [accountId, setAccountId] = useState('')
  const [debit, setDebit] = useState('')
  const [credit, setCredit] = useState('')
  const [reference, setReference] = useState('')
  const [localError, setLocalError] = useState<string | null>(null)

  const { data: accounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const accountOptions = useMemo(
    () =>
      accounts.map(a => ({
        value: a.id,
        label: `${a.accountNumber} - ${a.description}`,
      })),
    [accounts]
  )

  const handleSave = () => {
    if (!accountId) {
      setLocalError('Select an account for the line.')
      return
    }
    const d = Number(debit) || 0
    const c = Number(credit) || 0
    if (d <= 0 && c <= 0) {
      setLocalError('Enter a debit or credit amount greater than zero.')
      return
    }
    if (d > 0 && c > 0) {
      setLocalError('A line can have either a debit or a credit, not both.')
      return
    }
    setLocalError(null)
    onSave({
      accountId,
      debit: d > 0 ? d : null,
      credit: c > 0 ? c : null,
      reference: reference.trim() || null,
    })
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Add Journal Line"
      description="Add a debit or credit line to this journal batch."
      size="md"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleSave} isLoading={isSaving}>
            Add Line
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        {(localError || error) && (
          <div
            className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm"
            role="alert"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
            <span>{localError || error}</span>
          </div>
        )}
        <Combobox
          label="Account"
          placeholder="Select account..."
          options={accountOptions}
          value={accountId}
          onChange={value => {
            setAccountId(value)
            setLocalError(null)
          }}
          required
        />
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="Debit"
            type="number"
            step="0.01"
            min="0"
            value={debit}
            onChange={e => setDebit(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
          />
          <Input
            label="Credit"
            type="number"
            step="0.01"
            min="0"
            value={credit}
            onChange={e => setCredit(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
          />
        </div>
        <Input
          label="Reference"
          placeholder="Optional line reference"
          value={reference}
          onChange={e => setReference(e.target.value)}
        />
      </div>
    </Modal>
  )
}

export function JournalBatchDetailPage() {
  const { batchId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [isAddOpen, setIsAddOpen] = useState(false)
  const [actionDialog, setActionDialog] = useState<'release' | 'post' | null>(null)
  const [isReverseOpen, setIsReverseOpen] = useState(false)
  const [reverseReason, setReverseReason] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)

  const { data: batch, isLoading, isError } = useQuery({
    queryKey: ['gl', 'journalBatch', batchId],
    queryFn: () => getJournalBatch(batchId),
    enabled: !!batchId,
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['gl', 'journalBatch', batchId] })
    queryClient.invalidateQueries({ queryKey: ['gl', 'journalBatches'] })
  }

  const addLineMutation = useMutation({
    mutationFn: (line: { accountId: string; debit?: number | null; credit?: number | null; reference?: string | null }) =>
      addJournalBatchLine(batchId, line),
    onSuccess: () => {
      invalidate()
      setIsAddOpen(false)
    },
    onError: err => setActionError(getErrorMessage(err)),
  })

  const removeLineMutation = useMutation({
    mutationFn: (lineId: string) => removeJournalBatchLine(batchId, lineId),
    onSuccess: () => invalidate(),
    onError: err => setActionError(getErrorMessage(err)),
  })

  const releaseMutation = useMutation({
    mutationFn: () => releaseJournalBatch(batchId),
    onSuccess: () => {
      invalidate()
      setActionDialog(null)
    },
    onError: err => {
      setActionDialog(null)
      setActionError(getErrorMessage(err))
    },
  })

  const postMutation = useMutation({
    mutationFn: () => postJournalBatch(batchId),
    onSuccess: () => {
      invalidate()
      setActionDialog(null)
    },
    onError: err => {
      setActionDialog(null)
      setActionError(getErrorMessage(err))
    },
  })

  const reverseMutation = useMutation({
    mutationFn: () => reverseJournalBatch(batchId, { reason: reverseReason }),
    onSuccess: reversed => {
      invalidate()
      setIsReverseOpen(false)
      setReverseReason('')
      navigate(`/gl/journal-batches/${reversed.id}`)
    },
    onError: err => setActionError(getErrorMessage(err)),
  })

  const lines = batch?.lines ?? []
  const totalDebits = lines.reduce((sum, l) => sum + l.debit, 0)
  const totalCredits = lines.reduce((sum, l) => sum + l.credit, 0)
  const isBalanced = lines.length >= 2 && Math.abs(totalDebits - totalCredits) < 0.005

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    )
  }

  if (isError || !batch) {
    return (
      <Card>
        <CardContent className="py-10 text-center">
          <p className="text-sm text-gray-500 dark:text-gray-400">Journal batch not found.</p>
          <Button variant="outline" size="sm" className="mt-4" asChild>
            <Link to="/gl/journal-batches">Back to Batches</Link>
          </Button>
        </CardContent>
      </Card>
    )
  }

  const isDraft = batch.status === 0 || batch.status === 'Draft'
  const isBalancedStatus = batch.status === 1 || batch.status === 'Balanced'
  const isPosted = batch.status === 2 || batch.status === 'Posted'
  const isEditable = isDraft

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/gl/journal-batches">
            <ArrowLeft className="h-4 w-4" aria-hidden="true" />
            Back
          </Link>
        </Button>
      </div>

      {actionError && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{actionError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title={batch.batchNumber}
          description={batch.description}
          action={
            <div className="flex items-center gap-2">
              <MapStatusBadge value={batch.status} mapping={journalBatchStatusMap} size="md" />
              {isDraft && (
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => {
                    setActionError(null)
                    setActionDialog('release')
                  }}
                >
                  Release
                </Button>
              )}
              {isBalancedStatus && (
                <Button
                  variant="success"
                  size="sm"
                  onClick={() => {
                    setActionError(null)
                    setActionDialog('post')
                  }}
                >
                  Post
                </Button>
              )}
              {isPosted && (
                <Button
                  variant="destructive"
                  size="sm"
                  onClick={() => {
                    setActionError(null)
                    setIsReverseOpen(true)
                  }}
                >
                  Reverse
                </Button>
              )}
            </div>
          }
        />
        <CardContent>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
            <div>
              <p className="text-gray-500 dark:text-gray-400">Posting Date</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5">{formatDate(batch.postingDate)}</p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Lines</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5 tabular-nums">{lines.length}</p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Total Debits</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5 font-tabular tabular-nums">
                {formatCurrency(totalDebits)}
              </p>
            </div>
            <div>
              <p className="text-gray-500 dark:text-gray-400">Total Credits</p>
              <p className="font-medium text-gray-900 dark:text-white mt-0.5 font-tabular tabular-nums">
                {formatCurrency(totalCredits)}
              </p>
            </div>
          </div>
          <div className="mt-4">
            {isBalanced ? (
              <Badge variant="success" size="sm" dot>
                Balanced — debits equal credits
              </Badge>
            ) : (
              <Badge variant="warning" size="sm" dot>
                Unbalanced — {formatCurrency(Math.abs(totalDebits - totalCredits))} difference
              </Badge>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader
          title="Journal Lines"
          description="Debit and credit lines in this batch"
          action={
            isEditable && (
              <Button
                variant="primary"
                size="sm"
                leftIcon={<Plus className="h-4 w-4" />}
                onClick={() => {
                  setActionError(null)
                  setIsAddOpen(true)
                }}
              >
                Add Line
              </Button>
            )
          }
        />
        <CardContent>
          {lines.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No lines in this batch yet. {isEditable ? 'Click "Add Line" to create one.' : ''}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Reference</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Debit</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Credit</th>
                    {isEditable && <th className="px-3 py-2 w-10" />}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {lines.map((line: GlJournalEntryLine) => (
                    <tr key={line.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">{line.accountId}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{line.reference ?? '—'}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {line.debit > 0 ? formatCurrency(line.debit) : '—'}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {line.credit > 0 ? formatCurrency(line.credit) : '—'}
                      </td>
                      {isEditable && (
                        <td className="px-3 py-3 text-center">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Remove line ${line.id}`}
                            onClick={() => removeLineMutation.mutate(line.id)}
                          >
                            <Trash2 className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">
                    <td colSpan={2} className="px-3 py-2 text-right font-medium text-gray-700 dark:text-gray-300">
                      Totals
                    </td>
                    <td className="px-3 py-2 text-right font-tabular tabular-nums font-semibold text-gray-900 dark:text-white">
                      {formatCurrency(totalDebits)}
                    </td>
                    <td className="px-3 py-2 text-right font-tabular tabular-nums font-semibold text-gray-900 dark:text-white">
                      {formatCurrency(totalCredits)}
                    </td>
                    {isEditable && <td />}
                  </tr>
                </tfoot>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <AddLineModal
        isOpen={isAddOpen}
        onClose={() => setIsAddOpen(false)}
        onSave={line => addLineMutation.mutate(line)}
        isSaving={addLineMutation.isPending}
        error={actionError}
      />

      <ConfirmDialog
        isOpen={actionDialog === 'release'}
        onClose={() => setActionDialog(null)}
        onConfirm={() => releaseMutation.mutate()}
        title="Release Batch"
        message="Release this batch to move it from Draft to Balanced status. Lines can still be added until it is posted."
        confirmText="Release"
        isLoading={releaseMutation.isPending}
      />

      <ConfirmDialog
        isOpen={actionDialog === 'post'}
        onClose={() => setActionDialog(null)}
        onConfirm={() => postMutation.mutate()}
        title="Post Batch"
        message="Posting requires the batch to be balanced. Posted records are immutable and cannot be edited."
        confirmText="Post"
        variant="danger"
        isLoading={postMutation.isPending}
      />

      <Modal
        isOpen={isReverseOpen}
        onClose={() => {
          setIsReverseOpen(false)
          setReverseReason('')
        }}
        title="Reverse Batch"
        description="Reversing creates a new batch with inverted lines. The original batch becomes immutable."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={() => setIsReverseOpen(false)} disabled={reverseMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => reverseMutation.mutate()}
              isLoading={reverseMutation.isPending}
              disabled={!reverseReason.trim()}
            >
              Reverse Batch
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input
            label="Reason"
            placeholder="Required — why is this batch being reversed?"
            value={reverseReason}
            onChange={e => setReverseReason(e.target.value)}
            required
          />
        </div>
      </Modal>
    </div>
  )
}
