import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, CheckCircle, Send } from 'lucide-react'
import { Modal } from '@components/ui/Modal'
import { Button } from '@components/ui/Button'
import { getErrorMessage } from '@api/client'
import { resubmitDocument, type EInvoiceDocument } from '@api/einvoice'

interface ResubmitModalProps {
  isOpen: boolean
  onClose: () => void
  document: EInvoiceDocument | null
}

export function ResubmitModal({ isOpen, onClose, document }: ResubmitModalProps) {
  const queryClient = useQueryClient()
  const [payload, setPayload] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const resubmitMutation = useMutation({
    mutationFn: () => {
      if (!document) throw new Error('No document selected')
      return resubmitDocument(document.id, payload)
    },
    onSuccess: (result) => {
      if (result.success) {
        setSuccess(true)
        queryClient.invalidateQueries({ queryKey: ['einvoice', 'documents'] })
        setTimeout(() => {
          setSuccess(false)
          setPayload('')
          onClose()
        }, 2000)
      } else {
        setError(result.error || 'Resubmission failed')
      }
    },
    onError: (err) => {
      setError(getErrorMessage(err))
    },
  })

  const handleSubmit = () => {
    setError(null)
    setSuccess(false)
    if (!payload.trim()) {
      setError('Corrected payload is required')
      return
    }
    resubmitMutation.mutate()
  }

  const handleClose = () => {
    setPayload('')
    setError(null)
    setSuccess(false)
    onClose()
  }

  if (!document) return null

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title={`Resubmit ${document.documentType}`}
      description={`Original status: ${document.status}${document.lhdnsErrorMessage ? ` — Error: ${document.lhdnsErrorMessage}` : ''}`}
      size="xl"
      footer={
        <>
          <Button variant="secondary" onClick={handleClose} disabled={resubmitMutation.isPending}>
            Cancel
          </Button>
          <Button
            variant="primary"
            onClick={handleSubmit}
            isLoading={resubmitMutation.isPending}
            leftIcon={<Send className="h-4 w-4" />}
          >
            Resubmit to MyInvois
          </Button>
        </>
      }
    >
      {success ? (
        <div className="flex items-center gap-3 p-4 rounded-lg bg-green-50 border border-green-200 dark:bg-green-900/20 dark:border-green-800">
          <CheckCircle className="h-5 w-5 text-green-600 dark:text-green-400" />
          <span className="text-sm text-green-700 dark:text-green-300">
            Document resubmitted successfully! New submission is being processed.
          </span>
        </div>
      ) : (
        <div className="space-y-4">
          {error && (
            <div className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm">
              <AlertCircle className="h-4 w-4 flex-shrink-0" />
              {error}
            </div>
          )}

          <div className="text-sm text-gray-600 dark:text-gray-400">
            <p className="mb-2">
              Paste the corrected UBL JSON payload below. The payload will be validated before submission.
            </p>
            <p className="text-xs text-gray-500 dark:text-gray-500">
              Original: {document.sourceDocumentType} | Amount: {document.currencyCode}{' '}
              {document.totalAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
            </p>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Corrected UBL Payload (JSON)
            </label>
            <textarea
              value={payload}
              onChange={(e) => setPayload(e.target.value)}
              rows={20}
              className="block w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm font-mono text-gray-900 dark:text-white placeholder-gray-400 focus:border-emerald-500 focus:ring-emerald-500"
              placeholder='{"invoice": { ... }}'
              spellCheck={false}
            />
          </div>
        </div>
      )}
    </Modal>
  )
}
