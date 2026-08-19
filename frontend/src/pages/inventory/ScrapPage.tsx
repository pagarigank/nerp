import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { getItems, getWarehouses } from '@api/inventory'
import { useQuery } from '@tanstack/react-query'
import { createScrap } from '@api/inventory'

const REASONS = ['DAMAGED', 'OBSOLETE', 'EXPIRED', 'DEFECTIVE', 'SAMPLE', 'OTHER']

export function ScrapPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const [itemId, setItemId] = useState('')
  const [whId, setWhId] = useState('')
  const [qty, setQty] = useState('1')
  const [reason, setReason] = useState('DAMAGED')
  const [ref, setRef] = useState('')

  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items-mini'], queryFn: () => getItems() })
  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'wh-mini'], queryFn: () => getWarehouses() })

  const scrap = useMutation({
    mutationFn: () => createScrap({
      companyId: '',
      itemId,
      warehouseId: whId,
      quantity: Number(qty),
      scrapReasonCode: reason,
      unitOfMeasure: 'EA',
      referenceNumber: ref || 'SCRAP-' + Date.now(),
    }),
    onSuccess: (id) => { setOk('Scrap posted: ' + id.slice(0, 8)); setErr(null); qc.invalidateQueries({ queryKey: ['inventory', 'stock'] }) },
    onError: e => { setErr(getErrorMessage(e)); setOk(null) },
  })

  const itemOptions = [{ value: '', label: 'Item…' }, ...items.map(i => ({ value: i.id, label: i.itemCode }))]
  const whOptions = [{ value: '', label: 'Warehouse…' }, ...warehouses.map(w => ({ value: w.id, label: w.warehouseName ?? w.warehouseCode }))]

  return (
    <div className="space-y-6">
      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}
      {ok && <div className="p-4 rounded-lg bg-green-50 border border-green-200 text-green-700 text-sm">{ok}</div>}

      <Card>
        <CardHeader title="Scrap / Disposal" description="Write off inventory. Posts Dr Scrap Loss (6900) / Cr Inventory Asset (1400) to GL." />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-6 gap-3">
            <Select options={itemOptions} value={itemId} onChange={e => setItemId((e.target as HTMLSelectElement).value)} />
            <Select options={whOptions} value={whId} onChange={e => setWhId((e.target as HTMLSelectElement).value)} />
            <Input placeholder="Qty" value={qty} onChange={e => setQty(e.target.value)} />
            <Select options={REASONS.map(r => ({ value: r, label: r }))} value={reason} onChange={e => setReason((e.target as HTMLSelectElement).value)} />
            <Input placeholder="Reference" value={ref} onChange={e => setRef(e.target.value)} />
            <Button onClick={() => scrap.mutate()} disabled={!itemId || !whId || scrap.isPending}>{scrap.isPending ? 'Posting…' : 'Post Scrap'}</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
