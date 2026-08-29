import { useEffect, useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { DataTable } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import { addOrderNote, getOrderNotes, getOrderHistory, getAcknowledgment, recordOrderHistory, companyId } from '@api/orderManagement'
import type { SalesOrderNoteSummary, SalesOrderChangeHistorySummary, AcknowledgmentDocument } from '@/types/orderManagement'

export function OrderNotesPage() {
  const [soId, setSoId] = useState('')
  const [soSearch, setSoSearch] = useState('')
  const [showSoDrop, setShowSoDrop] = useState(false)
  const { data: salesOrders = [] } = useQuery({ queryKey: ['om', 'sales-orders'], queryFn: () => getSalesOrders() })
  const filteredSOs = useMemo(() => {
    const q = soSearch.trim().toLowerCase()
    if (!q) return salesOrders.slice(0, 10)
    return salesOrders.filter((o: { id: string; orderNumber: string; status: string }) => o.orderNumber.toLowerCase().includes(q)).slice(0, 10)
  }, [salesOrders, soSearch])
  const [notes, setNotes] = useState<SalesOrderNoteSummary[]>([])
  const [history, setHistory] = useState<SalesOrderChangeHistorySummary[]>([])
  const [ack, setAck] = useState<AcknowledgmentDocument | null>(null)
  const [text, setText] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function refresh() {
    if (!soId) return
    setLoading(true)
    setError(null)
    try {
      const [n, h] = await Promise.all([getOrderNotes(soId), getOrderHistory(soId)])
      setNotes((n as { data: SalesOrderNoteSummary[] }).data ?? [])
      setHistory((h as { data: SalesOrderChangeHistorySummary[] }).data ?? [])
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void refresh()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [soId])

  async function addNote() {
    if (!soId || !text) return
    setError(null)
    try {
      await addOrderNote(soId, { companyId: companyId(), text, isCustomerFacing: false, noteType: 'General' })
      setText('')
      await refresh()
    } catch (e) {
      setError(getErrorMessage(e))
    }
  }

  async function loadAck() {
    if (!soId) return
    setError(null)
    try {
      const res = await getAcknowledgment(soId)
      setAck((res as { data: AcknowledgmentDocument }).data ?? null)
    } catch (e) {
      setError(getErrorMessage(e))
    }
  }

  async function recordHistory() {
    if (!soId) return
    setError(null)
    try {
      await recordOrderHistory(soId, { companyId: companyId(), changeType: 'ManualNote', reasonCode: 'WEB' })
      await refresh()
    } catch (e) {
      setError(getErrorMessage(e))
    }
  }

  const noteCols = [
    { key: 'text', header: 'Note' },
    { key: 'noteType', header: 'Type' },
    { key: 'isCustomerFacing', header: 'Customer Facing', render: (row: any) => (row.isCustomerFacing ? 'Yes' : 'No') },
    { key: 'createdBy', header: 'By', render: (row: any) => row.createdBy ?? '-' },
  ]
  const histCols = [
    { key: 'changeType', header: 'Change' },
    { key: 'fieldName', header: 'Field', render: (row: any) => row.fieldName ?? '-' },
    { key: 'oldValue', header: 'Old', render: (row: any) => row.oldValue ?? '-' },
    { key: 'newValue', header: 'New', render: (row: any) => row.newValue ?? '-' },
    { key: 'changedBy', header: 'By', render: (row: any) => row.changedBy ?? '-' },
    { key: 'changeDate', header: 'When' },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Order Notes, History &amp; Acknowledgment (589 / 588)</h1>
      <Card className="p-4 flex items-end gap-3">
        <div className="flex-1 relative">
          <Input value={soSearch} onChange={(e) => { setSoSearch(e.target.value); setSoId('') }}
            onFocus={() => setShowSoDrop(true)} onBlur={() => setTimeout(() => setShowSoDrop(false), 200)}
            placeholder="Search sales order..." />
          {showSoDrop && filteredSOs.length > 0 && (
            <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
              {filteredSOs.map((o: { id: string; orderNumber: string; status: string }) => (
                <button key={o.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                  onMouseDown={() => { setSoId(o.id); setSoSearch(o.orderNumber); setShowSoDrop(false) }}>
                  <span className="font-medium">{o.orderNumber}</span> <span className="text-gray-500 text-xs">{o.status}</span>
                </button>
              ))}
            </div>
          )}
        </div>
        <Button variant="outline" onClick={loadAck} disabled={!soId}>Load Acknowledgment</Button>
      </Card>
      <Card className="p-4 space-y-3">
        <div className="flex items-end gap-3">
          <Input value={text} onChange={(e) => setText(e.target.value)} placeholder="New note text" className="flex-1" />
          <Button variant="primary" onClick={addNote}>Add Note</Button>
          <Button variant="outline" onClick={recordHistory}>Log History</Button>
        </div>
        <h2 className="font-semibold">Notes</h2>
        <DataTable columns={noteCols} data={notes} loading={loading} emptyMessage="No notes" />
        <h2 className="font-semibold">Change History</h2>
        <DataTable columns={histCols} data={history} loading={loading} emptyMessage="No history" />
        {ack && (
          <div className="border rounded p-3">
            <h2 className="font-semibold mb-2">Acknowledgment — {ack.orderNumber}</h2>
            <p className="text-sm">Customer: {ack.customerId.slice(0, 8)} · Date: {ack.orderDate}</p>
            <table className="w-full text-sm mt-2">
              <thead>
                <tr className="text-left border-b"><th>Item</th><th>Desc</th><th>Qty</th><th>Price</th><th>UoM</th></tr>
              </thead>
              <tbody>
                {ack.lines.map((l, i) => (
                  <tr key={i} className="border-b">
                    <td>{l.itemId.slice(0, 8)}</td><td>{l.description}</td><td>{l.quantity}</td><td>{l.unitPrice}</td><td>{l.unitOfMeasure}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
