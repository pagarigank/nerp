import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { getItems, getWarehouses } from '@api/inventory'
import { getKitComponents, createKitComponent, receiveKit, issueKit } from '@api/inventory'
import type { KitComponentDto } from '@/types/inventory'

export function KitsPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [kitItemId, setKitItemId] = useState('')
  const [componentItemId, setComponentItemId] = useState('')
  const [qtyPerKit, setQtyPerKit] = useState('1')
  const [kitId, setKitId] = useState('')
  const [whId, setWhId] = useState('')
  const [qty, setQty] = useState('1')

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'kits'], queryFn: () => getKitComponents() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items-mini'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'wh-mini'], queryFn: () => getWarehouses() })

  const create = useMutation({
    mutationFn: () => createKitComponent({ companyId: '', kitItemId, componentItemId, quantityPerKit: Number(qtyPerKit), unitOfMeasure: 'EA' }),
    onSuccess: () => { setKitItemId(''); setComponentItemId(''); qc.invalidateQueries({ queryKey: ['inventory', 'kits'] }) },
    onError: e => setErr(getErrorMessage(e)),
  })
  const receive = useMutation({
    mutationFn: () => receiveKit({ companyId: '', kitItemId: kitId, warehouseId: whId, quantity: Number(qty), unitOfMeasure: 'EA', transactionDate: new Date().toISOString().slice(0, 10), referenceNumber: 'KIT-RCV-' + Date.now() }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'kits'] }),
    onError: e => setErr(getErrorMessage(e)),
  })
  const issue = useMutation({
    mutationFn: () => issueKit({ companyId: '', kitItemId: kitId, warehouseId: whId, quantity: Number(qty), unitOfMeasure: 'EA', transactionDate: new Date().toISOString().slice(0, 10), referenceNumber: 'KIT-ISS-' + Date.now() }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['inventory', 'kits'] }),
    onError: e => setErr(getErrorMessage(e)),
  })

  const name = (id: string) => items.find(i => i.id === id)?.itemCode ?? id.slice(0, 8)
  const itemOptions = [{ value: '', label: 'Item…' }, ...items.map(i => ({ value: i.id, label: i.itemCode }))]
  const whOptions = [{ value: '', label: 'Warehouse…' }, ...warehouses.map(w => ({ value: w.id, label: w.warehouseName ?? w.warehouseCode }))]

  return (
    <div className="space-y-6">
      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}

      <Card>
        <CardHeader title="Kit Components" description="Define components that make up a kit/bundle" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-5 gap-3">
            <Select options={itemOptions} value={kitItemId} onChange={e => setKitItemId((e.target as HTMLSelectElement).value)} />
            <Select options={itemOptions} value={componentItemId} onChange={e => setComponentItemId((e.target as HTMLSelectElement).value)} />
            <Input placeholder="Qty per kit" value={qtyPerKit} onChange={e => setQtyPerKit(e.target.value)} />
            <div />
            <Button onClick={() => create.mutate()} disabled={!kitItemId || !componentItemId || create.isPending}>{create.isPending ? 'Saving…' : 'Add'}</Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Receive / Issue Kit" description="Explode kit into components on receipt; consume components on issue" />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-5 gap-3">
            <Select options={itemOptions} value={kitId} onChange={e => setKitId((e.target as HTMLSelectElement).value)} />
            <Select options={whOptions} value={whId} onChange={e => setWhId((e.target as HTMLSelectElement).value)} />
            <Input placeholder="Quantity" value={qty} onChange={e => setQty(e.target.value)} />
            <div />
            <div className="flex gap-1">
              <Button size="sm" onClick={() => receive.mutate()} disabled={!kitId || !whId || receive.isPending}>Receive</Button>
              <Button size="sm" variant="outline" onClick={() => issue.mutate()} disabled={!kitId || !whId || issue.isPending}>Issue</Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Kit Component List" description={`${rows.length} component(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No kit components.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Kit</th><th className="px-3 py-2 font-medium text-gray-500">Component</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Qty/Kit</th><th className="px-3 py-2 font-medium text-gray-500">UOM</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: KitComponentDto) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{name(r.kitItemId)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{name(r.componentItemId)}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.quantityPerKit}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.unitOfMeasure ?? 'EA'}</td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
    </div>
  )
}
