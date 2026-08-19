import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { getItems, getWarehouses } from '@api/inventory'
import { getVendors } from '@api/ap'
import { getConsignmentStock, createConsignmentStock, receiveConsignment, consumeConsignment } from '@api/inventory'
import type { ConsignmentStockDto } from '@/types/inventory'

export function ConsignmentPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [vendorId, setVendorId] = useState('')
  const [itemId, setItemId] = useState('')
  const [whId, setWhId] = useState('')
  const [qty, setQty] = useState('0')

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'consignment'], queryFn: () => getConsignmentStock() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items-mini'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'wh-mini'], queryFn: () => getWarehouses() })
  const { data: vendors = [] } = useQuery({ queryKey: ['inventory', 'vendors-mini'], queryFn: () => getVendors() })

  const create = useMutation({
    mutationFn: () => createConsignmentStock({ companyId: '', vendorId, itemId, warehouseId: whId, quantityOnHand: Number(qty), unitOfMeasure: 'EA', consignmentCost: 0 }),
    onSuccess: () => { setVendorId(''); setItemId(''); setWhId(''); setQty('0'); qc.invalidateQueries({ queryKey: ['inventory', 'consignment'] }) },
    onError: e => setErr(getErrorMessage(e)),
  })
  const receive = useMutation({
    mutationFn: ({ id, q }: { id: string; q: number }) => receiveConsignment(id, q),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'consignment'] }),
    onError: e => setErr(getErrorMessage(e)),
  })
  const consume = useMutation({
    mutationFn: ({ id, q }: { id: string; q: number }) => consumeConsignment(id, q, 0),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'consignment'] }),
    onError: e => setErr(getErrorMessage(e)),
  })

  const name = (id: string) => items.find(i => i.id === id)?.itemCode ?? vendors.find(v => v.id === id)?.name ?? id.slice(0, 8)
  const itemOptions = [{ value: '', label: 'Item…' }, ...items.map(i => ({ value: i.id, label: i.itemCode }))]
  const whOptions = [{ value: '', label: 'Warehouse…' }, ...warehouses.map(w => ({ value: w.id, label: w.warehouseName ?? w.warehouseCode }))]
  const vendorOptions = [{ value: '', label: 'Vendor…' }, ...vendors.map(v => ({ value: v.id, label: v.name }))]

  return (
    <div className="space-y-6">
      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}

      <Card>
        <CardHeader title="New Consignment Stock" description="Vendor-owned inventory held at your warehouse" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-5 gap-3">
            <Select options={vendorOptions} value={vendorId} onChange={e => setVendorId((e.target as HTMLSelectElement).value)} />
            <Select options={itemOptions} value={itemId} onChange={e => setItemId((e.target as HTMLSelectElement).value)} />
            <Select options={whOptions} value={whId} onChange={e => setWhId((e.target as HTMLSelectElement).value)} />
            <Input placeholder="On-hand qty" value={qty} onChange={e => setQty(e.target.value)} />
            <Button onClick={() => create.mutate()} disabled={!vendorId || !itemId || !whId || create.isPending}>{create.isPending ? 'Saving…' : 'Add'}</Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Consignment Stock" description={`${rows.length} consignment line(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No consignment stock.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Vendor</th><th className="px-3 py-2 font-medium text-gray-500">Item</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">On Hand</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: ConsignmentStockDto) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{name(r.vendorId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{name(r.itemId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{name(r.warehouseId)}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.quantityOnHand}</td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          <Button size="sm" variant="outline" disabled={receive.isPending} onClick={() => receive.mutate({ id: r.id, q: Math.max(1, Math.round(r.quantityOnHand * 0.2)) })}>Receive</Button>
                          <Button size="sm" variant="ghost" className="text-red-600" disabled={consume.isPending} onClick={() => consume.mutate({ id: r.id, q: Math.max(1, Math.round(r.quantityOnHand * 0.2)) })}>Consume</Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
