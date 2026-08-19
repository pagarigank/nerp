import { useState } from 'react'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { allocateFreight } from '@api/orderManagement'

export function FreightAllocationPage() {
  const [soId, setSoId] = useState('')
  const [amount, setAmount] = useState(0)
  const [msg, setMsg] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function allocate() {
    if (!soId) return
    setLoading(true)
    setError(null)
    setMsg(null)
    try {
      await allocateFreight(soId, Number(amount))
      setMsg(`Allocated ${amount} of freight across order lines.`)
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Freight Allocation to Invoice Lines (578)</h1>
      <Card className="p-4 grid grid-cols-2 md:grid-cols-3 gap-3 items-end">
        <Input value={soId} onChange={(e) => setSoId(e.target.value)} placeholder="Sales Order GUID" />
        <Input type="number" value={amount} onChange={(e) => setAmount(Number(e.target.value))} placeholder="Freight Amount" />
        <Button variant="primary" disabled={loading} onClick={allocate}>Allocate</Button>
      </Card>
      {msg && <p className="text-sm text-green-600">{msg}</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
