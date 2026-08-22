import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, CheckCircle } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { Modal } from '@components/ui/Modal'
import { Textarea } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import {
  RETURN_APPROVAL_THRESHOLD,
  approveReturn,
  confirmReturn,
  getReturn,
  rejectReturn,
  submitReturnForApproval,
} from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import type { ReturnDetail } from '@/types/orderManagement'
import type { ArCustomer } from '@/types/ar'

export function ReturnDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [returnEntity, setReturn] = useState<ReturnDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actioning, setActioning] = useState(false)
  const [customers, setCustomers] = useState<ArCustomer[]>([])
  const [showReject, setShowReject] = useState(false)
  const [rejectReason, setRejectReason] = useState('')
  const customerName = (id?: string | null) => id ? customers.find((c: ArCustomer) => c.id === id)?.name ?? id.slice(0, 8) : '—'

  useEffect(() => {
    if (!id) return
    void load()
  }, [id])

  async function load() {
    if (!id) return
    setLoading(true)
    setError(null)
    try {
      const [ret, custs] = await Promise.all([getReturn(id), getCustomers()])
      setReturn(ret)
      setCustomers(custs)
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  async function handleConfirm() {
    if (!id) return
    setActioning(true)
    try {
      await confirmReturn(id)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setActioning(false)
    }
  }

  async function handle(fn: () => Promise<unknown>) {
    if (!id) return
    setActioning(true)
    try {
      await fn()
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setActioning(false)
    }
  }

  if (loading) return <p className="text-gray-500">Loading…</p>
  if (!returnEntity) return <p className="text-red-600">{error ?? 'Return not found.'}</p>

  const needsApproval = returnEntity.returnValue > RETURN_APPROVAL_THRESHOLD && !returnEntity.isApproved
  const grossValue = returnEntity.lines.reduce((sum, l) => sum + l.quantity * l.unitPrice, 0)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={() => navigate('/om/returns')}>
          <ArrowLeft className="h-4 w-4" /> Back
        </Button>
        <div className="flex gap-2">
          {returnEntity.status === 'Draft' && needsApproval && (
            <Button variant="outline" disabled={actioning} onClick={() => void handle(() => submitReturnForApproval(returnEntity.id))}>
              Submit for Approval
            </Button>
          )}
          {returnEntity.status === 'Draft' && !needsApproval && (
            <Button variant="success" disabled={actioning} onClick={handleConfirm}>
              <CheckCircle className="h-4 w-4" /> Confirm Return
            </Button>
          )}
          {returnEntity.status === 'PendingApproval' && (
            <>
              <Button variant="success" disabled={actioning} onClick={() => void handle(() => approveReturn(returnEntity.id))}>
                Approve
              </Button>
              <Button variant="destructive" disabled={actioning} onClick={() => setShowReject(true)}>
                Reject
              </Button>
            </>
          )}
        </div>
      </div>

      <Modal isOpen={showReject} onClose={() => setShowReject(false)} title={`Reject ${returnEntity.returnNumber}`}
        footer={<><Button variant="secondary" onClick={() => setShowReject(false)}>Cancel</Button>
          <Button variant="destructive" isLoading={actioning}
            onClick={() => {
              void handle(async () => {
                await rejectReturn(returnEntity.id, rejectReason || 'Rejected without reason.')
                setShowReject(false)
                setRejectReason('')
              })
            }}>Reject Return</Button></>}>
        <Textarea label="Rejection Reason" rows={3} value={rejectReason} onChange={(e) => setRejectReason(e.target.value)} placeholder="Why is this return rejected?" />
      </Modal>

      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
          {returnEntity.returnNumber}
        </h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Status: <span className="font-medium">{returnEntity.status}</span> · {returnEntity.returnDate}
        </p>
        <div className="mt-2 grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
          <div><span className="text-gray-500">Customer:</span> <span className="font-medium">{customerName(returnEntity.customerId)}</span></div>
          <div><span className="text-gray-500">Reason:</span> <span className="font-medium">{returnEntity.reasonCode ?? '—'}</span></div>
          <div><span className="text-gray-500">Return Value (gross):</span> <span className="font-medium">${grossValue.toFixed(2)}</span></div>
          <div><span className="text-gray-500">Total:</span> <span className="font-medium">${returnEntity.lines.reduce((sum, l) => sum + l.lineTotal, 0).toFixed(2)}</span></div>
        </div>
        {returnEntity.rejectionReason && (
          <p className="mt-2 text-sm text-red-600">Rejected: {returnEntity.rejectionReason}</p>
        )}
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">
          {error}
        </div>
      )}

      <div className="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">#</th>
              <th className="px-4 py-2 text-left text-xs font-medium uppercase text-gray-500">Item</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Qty</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Price</th>
              <th className="px-4 py-2 text-right text-xs font-medium uppercase text-gray-500">Line Total</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {returnEntity.lines.map((l) => (
              <tr key={l.id}>
                <td className="px-4 py-2 text-sm">{l.lineNumber}</td>
                <td className="px-4 py-2 text-sm">{l.description}</td>
                <td className="px-4 py-2 text-right text-sm">{l.quantity}</td>
                <td className="px-4 py-2 text-right text-sm">${l.unitPrice.toFixed(2)}</td>
                <td className="px-4 py-2 text-right text-sm">${l.lineTotal.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
