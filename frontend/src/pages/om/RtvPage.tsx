import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { createRtv, shipRtv, creditRtv, companyId } from '@api/orderManagement'
import { getVendors } from '@api/ap'
import type { CreateRtvRequest } from '@/types/orderManagement'
import type { Vendor } from '@/types/ap'

const empty = { returnId: '', returnLineId: '', vendorId: '', quantity: 0, unitCost: 0 }

export function RtvPage() {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState(empty)
  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: getVendors })
  const vendorOptions = useMemo(() => vendors.map((v: Vendor) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })), [vendors])
  const [lastId, setLastId] = useState<string | null>(null)

  async function create() {
    setLoading(true)
    setError(null)
    try {
      const res = await createRtv(form.returnId, { ...form, companyId: companyId(), quantity: Number(form.quantity), unitCost: Number(form.unitCost), reference: 'Web RTV' } as CreateRtvRequest)
      setLastId((res as { data?: string }).data ?? null)
      setForm(empty)
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Return-to-Vendor / RMA Disposition (585)</h1>
      <Card className="p-4">
        <div className="grid grid-cols-2 md:grid-cols-6 gap-3">
          <Input value={form.returnId} onChange={(e) => setForm({ ...form, returnId: e.target.value })} placeholder="Return Id" />
          <Input value={form.returnLineId} onChange={(e) => setForm({ ...form, returnLineId: e.target.value })} placeholder="Return Line Id" />
          <Select value={form.vendorId} onChange={(e) => setForm({ ...form, vendorId: e.target.value })} placeholder="Select vendor..." options={vendorOptions} />
          <Input type="number" value={form.quantity} onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })} placeholder="Qty" />
          <Input type="number" value={form.unitCost} onChange={(e) => setForm({ ...form, unitCost: Number(e.target.value) })} placeholder="Unit Cost" />
          <Button variant="primary" disabled={loading} onClick={create}>Create RTV</Button>
        </div>
      </Card>
      {lastId && (
        <Card className="p-4 flex items-center gap-3">
          <span className="text-sm">RTV {lastId.slice(0, 8)} created.</span>
          <Button size="sm" variant="outline" onClick={async () => { await shipRtv(lastId); setError(null) }}>Mark Shipped to Vendor</Button>
          <Button size="sm" variant="success" onClick={async () => { await creditRtv(lastId); setError(null) }}>Receive Vendor Credit</Button>
        </Card>
      )}
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
