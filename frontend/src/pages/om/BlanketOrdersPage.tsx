import { useEffect, useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { DataTable } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import { createBlanketOrder, getBlanketOrders, addBlanketRelease, companyId } from '@api/orderManagement'
import { getCustomers } from '@api/ar'
import type { BlanketOrderSummary } from '@/types/orderManagement'
import type { ArCustomer } from '@/types/ar'

const empty = { orderNumber: '', customerId: '', totalQuantity: 0, totalValue: 0, validFrom: '', validTo: '' }

export function BlanketOrdersPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<BlanketOrderSummary[]>([])
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState(empty)
  const [relQty, setRelQty] = useState<Record<string, string>>({})
  const [custSearch, setCustSearch] = useState('')
  const [showCustDrop, setShowCustDrop] = useState(false)

  const { data: customers = [] } = useQuery({ queryKey: ['ar', 'customers'], queryFn: getCustomers })

  const filteredCusts = useMemo(() => {
    const q = custSearch.trim().toLowerCase()
    if (!q) return customers.slice(0, 10)
    return customers.filter((c: ArCustomer) => c.name.toLowerCase().includes(q) || c.customerId.toLowerCase().includes(q)).slice(0, 10)
  }, [customers, custSearch])

  async function load() {
    setLoading(true)
    setError(null)
    try {
      setData((await getBlanketOrders(companyId())) as BlanketOrderSummary[])
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function create() {
    setError(null)
    try {
      await createBlanketOrder({
        ...form,
        companyId: companyId(),
        orderDate: new Date().toISOString().slice(0, 10),
        totalQuantity: Number(form.totalQuantity),
        totalValue: Number(form.totalValue),
        currency: 'USD',
      })
      setForm(empty)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    }
  }

  async function release(id: string) {
    const q = Number(relQty[id] || 0)
    if (!q) return
    setError(null)
    try {
      await addBlanketRelease(id, { quantity: q, value: q, releaseDate: new Date().toISOString().slice(0, 10), reference: 'Web' })
      setRelQty((p) => ({ ...p, [id]: '' }))
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    }
  }

  const columns = [
    { key: 'orderNumber', header: 'Order #' },
    { key: 'status', header: 'Status', render: (v: string) => <Badge variant={v === 'Open' ? 'success' : v === 'Expired' ? 'error' : 'neutral'}>{v}</Badge> },
    { key: 'totalQuantity', header: 'Total Qty' },
    { key: 'releasedQuantity', header: 'Released' },
    { key: 'remainingQuantity', header: 'Remaining' },
    {
      key: 'release',
      header: 'Release',
      render: (_: unknown, row: BlanketOrderSummary) => (
        <div className="flex gap-2">
          <Input value={relQty[row.id] ?? ''} onChange={(e) => setRelQty((p) => ({ ...p, [row.id]: e.target.value }))} placeholder="Qty" className="w-20" />
          <Button size="sm" variant="primary" disabled={!relQty[row.id]} onClick={() => release(row.id)}>Release</Button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Blanket / Standing Orders (583)</h1>
      <Card className="p-4">
        <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
          <Input value={form.orderNumber} onChange={(e) => setForm({ ...form, orderNumber: e.target.value })} placeholder="Order #" />
          <div className="relative">
            <Input value={customers.find((c: ArCustomer) => c.id === form.customerId)?.name ?? custSearch}
              onChange={(e) => { setCustSearch(e.target.value); setShowCustDrop(true); setForm({ ...form, customerId: '' }) }}
              onFocus={() => setShowCustDrop(true)} onBlur={() => setTimeout(() => setShowCustDrop(false), 200)}
              placeholder="Search customer..." />
            {showCustDrop && filteredCusts.length > 0 && (
              <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
                {filteredCusts.map((c: ArCustomer) => (
                  <button key={c.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                    onMouseDown={() => { setForm({ ...form, customerId: c.id }); setCustSearch(c.name); setShowCustDrop(false) }}>
                    <span className="font-medium">{c.name}</span> <span className="text-gray-500 text-xs">{c.customerId}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
          <Input type="number" value={form.totalQuantity} onChange={(e) => setForm({ ...form, totalQuantity: Number(e.target.value) })} placeholder="Total Qty" />
          <Input type="number" value={form.totalValue} onChange={(e) => setForm({ ...form, totalValue: Number(e.target.value) })} placeholder="Total Value" />
          <Button variant="primary" onClick={create}>Create</Button>
        </div>
      </Card>
      <Card>
        <DataTable columns={columns} data={data} loading={loading} emptyMessage="No blanket orders" />
      </Card>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
