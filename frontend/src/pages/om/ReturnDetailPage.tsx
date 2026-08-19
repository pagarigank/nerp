import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, CheckCircle } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { getErrorMessage } from '@api/client'
import { confirmReturn, getReturn } from '@api/orderManagement'
import type { ReturnDetail } from '@/types/orderManagement'

export function ReturnDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [returnEntity, setReturn] = useState<ReturnDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actioning, setActioning] = useState(false)

  useEffect(() => {
    if (!id) return
    void load()
  }, [id])

  async function load() {
    if (!id) return
    setLoading(true)
    setError(null)
    try {
      setReturn(await getReturn(id))
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

  if (loading) return <p className="text-gray-500">Loading…</p>
  if (!returnEntity) return <p className="text-red-600">{error ?? 'Return not found.'}</p>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <Button variant="ghost" size="sm" onClick={() => navigate('/om/returns')}>
          <ArrowLeft className="h-4 w-4" /> Back
        </Button>
        {returnEntity.status === 'Draft' && (
          <Button variant="success" disabled={actioning} onClick={handleConfirm}>
            <CheckCircle className="h-4 w-4" /> Confirm Return
          </Button>
        )}
      </div>

      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
          {returnEntity.returnNumber}
        </h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Status: <span className="font-medium">{returnEntity.status}</span> · {returnEntity.returnDate}
        </p>
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
