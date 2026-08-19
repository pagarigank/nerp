import { useEffect, useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@components/ui/Button'
import { Card } from '@components/ui/Card'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { DataTable } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import { createSubstitutionOffer, getSubstitutionOffers, acceptSubstitutionOffer, rejectSubstitutionOffer, companyId } from '@api/orderManagement'
import { getSalesOrders } from '@api/orderManagement'
import { getItems } from '@api/inventory'
import type { SubstitutionOfferSummary, SalesOrderSummary } from '@/types/orderManagement'
import type { ItemSummary } from '@/types/inventory'

const empty = { salesOrderId: '', salesOrderLineId: '', originalItemId: '', substituteItemId: '', quantity: 0, approvedUnitPrice: 0 }

export function SubstitutionOffersPage() {
  const [loading, setLoading] = useState(true)
  const [data, setData] = useState<SubstitutionOfferSummary[]>([])
  const [error, setError] = useState<string | null>(null)
  const [form, setForm] = useState(empty)
  const [origItemSearch, setOrigItemSearch] = useState('')
  const [subItemSearch, setSubItemSearch] = useState('')
  const [soSearch, setSoSearch] = useState('')
  const [showOrigDrop, setShowOrigDrop] = useState(false)
  const [showSubDrop, setShowSubDrop] = useState(false)
  const [showSoDrop, setShowSoDrop] = useState(false)

  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })
  const { data: salesOrders = [] } = useQuery({ queryKey: ['om', 'sales-orders'], queryFn: () => getSalesOrders() })

  const filteredOrigItems = useMemo(() => {
    const q = origItemSearch.trim().toLowerCase()
    if (!q) return items.slice(0, 10)
    return items.filter((i: ItemSummary) => i.itemCode.toLowerCase().includes(q)).slice(0, 10)
  }, [items, origItemSearch])

  const filteredSubItems = useMemo(() => {
    const q = subItemSearch.trim().toLowerCase()
    if (!q) return items.slice(0, 10)
    return items.filter((i: ItemSummary) => i.itemCode.toLowerCase().includes(q)).slice(0, 10)
  }, [items, subItemSearch])

  const filteredSOs = useMemo(() => {
    const q = soSearch.trim().toLowerCase()
    if (!q) return salesOrders.slice(0, 10)
    return salesOrders.filter((o: SalesOrderSummary) => o.orderNumber.toLowerCase().includes(q)).slice(0, 10)
  }, [salesOrders, soSearch])

  async function load() {
    setLoading(true)
    setError(null)
    try {
      setData((await getSubstitutionOffers(companyId())) as SubstitutionOfferSummary[])
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [])

  async function create() {
    setError(null)
    try {
      await createSubstitutionOffer({ ...form, companyId: companyId(), quantity: Number(form.quantity), approvedUnitPrice: Number(form.approvedUnitPrice), reason: 'Backorder substitution' })
      setForm(empty)
      setOrigItemSearch('')
      setSubItemSearch('')
      setSoSearch('')
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
    }
  }

  const columns = [
    { key: 'salesOrderId', header: 'SO #', render: (v: string) => v.slice(0, 8) },
    { key: 'originalItemId', header: 'Original Item', render: (v: string) => items.find((i: ItemSummary) => i.id === v)?.itemCode ?? v.slice(0, 8) },
    { key: 'substituteItemId', header: 'Substitute', render: (v: string) => items.find((i: ItemSummary) => i.id === v)?.itemCode ?? v.slice(0, 8) },
    { key: 'quantity', header: 'Qty' },
    { key: 'approvedUnitPrice', header: 'Price', render: (v: number) => `$${v.toFixed(2)}` },
    { key: 'status', header: 'Status', render: (v: string) => <Badge variant={v === 'Accepted' ? 'success' : v === 'Rejected' ? 'error' : 'warning'}>{v}</Badge> },
    {
      key: 'actions', header: 'Actions',
      render: (_: unknown, row: SubstitutionOfferSummary) => (
        <div className="flex gap-2">
          <Button size="sm" variant="success" disabled={row.status !== 'Offered'} onClick={async () => { await acceptSubstitutionOffer(row.id); load() }}>Accept</Button>
          <Button size="sm" variant="destructive" disabled={row.status !== 'Offered'} onClick={async () => { await rejectSubstitutionOffer(row.id); load() }}>Reject</Button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Backorder Substitution Offers</h1>
      <Card className="p-4 space-y-3">
        <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
          {/* SO Lookup */}
          <div className="relative">
            <Input value={soSearch} onChange={e => { setSoSearch(e.target.value); setForm({ ...form, salesOrderId: '' }) }}
              onFocus={() => setShowSoDrop(true)} onBlur={() => setTimeout(() => setShowSoDrop(false), 200)}
              placeholder="Sales Order..." label="Sales Order" />
            {showSoDrop && filteredSOs.length > 0 && (
              <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
                {filteredSOs.map((o: SalesOrderSummary) => (
                  <button key={o.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                    onMouseDown={() => { setForm({ ...form, salesOrderId: o.id }); setSoSearch(o.orderNumber); setShowSoDrop(false) }}>
                    <span className="font-medium">{o.orderNumber}</span> <span className="text-gray-500 text-xs">{o.status}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
          {/* Original Item Lookup */}
          <div className="relative">
            <Input value={origItemSearch} onChange={e => { setOrigItemSearch(e.target.value); setForm({ ...form, originalItemId: '' }) }}
              onFocus={() => setShowOrigDrop(true)} onBlur={() => setTimeout(() => setShowOrigDrop(false), 200)}
              placeholder="Original item..." label="Original Item" />
            {showOrigDrop && filteredOrigItems.length > 0 && (
              <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
                {filteredOrigItems.map((i: ItemSummary) => (
                  <button key={i.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                    onMouseDown={() => { setForm({ ...form, originalItemId: i.id }); setOrigItemSearch(i.itemCode); setShowOrigDrop(false) }}>
                    <span className="font-medium">{i.itemCode}</span> <span className="text-gray-500 text-xs">{i.description}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
          {/* Substitute Item Lookup */}
          <div className="relative">
            <Input value={subItemSearch} onChange={e => { setSubItemSearch(e.target.value); setForm({ ...form, substituteItemId: '' }) }}
              onFocus={() => setShowSubDrop(true)} onBlur={() => setTimeout(() => setShowSubDrop(false), 200)}
              placeholder="Substitute item..." label="Substitute Item" />
            {showSubDrop && filteredSubItems.length > 0 && (
              <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
                {filteredSubItems.map((i: ItemSummary) => (
                  <button key={i.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                    onMouseDown={() => { setForm({ ...form, substituteItemId: i.id }); setSubItemSearch(i.itemCode); setShowSubDrop(false) }}>
                    <span className="font-medium">{i.itemCode}</span> <span className="text-gray-500 text-xs">{i.description}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
        <div className="grid grid-cols-3 gap-3">
          <Input value={form.salesOrderLineId} onChange={e => setForm({ ...form, salesOrderLineId: e.target.value })} placeholder="SO Line Id (optional)" label="SO Line ID" />
          <Input type="number" value={form.quantity || ''} onChange={e => setForm({ ...form, quantity: Number(e.target.value) })} placeholder="Qty" label="Quantity" />
          <div className="flex items-end">
            <Button variant="primary" onClick={create} disabled={!form.salesOrderId || !form.originalItemId || !form.substituteItemId}>Create Offer</Button>
          </div>
        </div>
      </Card>
      <Card>
        <DataTable columns={columns} data={data} loading={loading} emptyMessage="No substitution offers" />
      </Card>
      {error && <p className="text-sm text-red-600">{error}</p>}
    </div>
  )
}
