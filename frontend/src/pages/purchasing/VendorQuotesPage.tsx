// <copyright file="VendorQuotesPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, Search, AlertCircle, Trophy, XCircle, Trash2 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getVendorQuotes,
  createVendorQuote,
  receiveVendorQuote,
  awardVendorQuote,
  rejectVendorQuote,
  companyId,
} from '@api/purchasing'
import { getVendors } from '@api/ap'
import { getItems, getItemUomConversions } from '@api/inventory'
import type { UomConversionDto } from '@/types/inventory'
import type { CreateVendorQuoteLineRequest } from '@/types/purchasing'

function statusBadge(status: string) {
  const s = status.toLowerCase()
  if (s.includes('award')) return <Badge variant="success" size="sm" dot>{status}</Badge>
  if (s.includes('reject')) return <Badge variant="error" size="sm" dot>{status}</Badge>
  if (s.includes('receiv')) return <Badge variant="info" size="sm" dot>{status}</Badge>
  if (s.includes('request')) return <Badge variant="warning" size="sm" dot>{status}</Badge>
  return <Badge variant="neutral" size="sm" dot>{status}</Badge>
}

export function VendorQuotesPage() {
  const qc = useQueryClient()
  const [search, setSearch] = useState('')
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [rfxNumber, setRfxNumber] = useState('')
  const [vendorId, setVendorId] = useState('')
  const [lines, setLines] = useState<CreateVendorQuoteLineRequest[]>([
    { itemId: '', description: '', quantity: 1, unitOfMeasure: 'EA', unitPrice: 0 },
  ])
  const [lineUomOptions, setLineUomOptions] = useState<Record<number, { value: string; label: string }[]>>({})

  const { data: quotes = [], isLoading } = useQuery({
    queryKey: ['purchasing', 'vendor-quotes'],
    queryFn: () => getVendorQuotes(),
  })
  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })

  const vendorOptions = useMemo(() => vendors.map((v: any) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })), [vendors])
  const itemOptions = useMemo(() => items.map((i: any) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])
  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach((v: any) => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const createMutation = useMutation({
    mutationFn: () => createVendorQuote({
      rfxNumber,
      companyId: companyId(),
      vendorId,
      requestedById: null,
      validUntil: null,
      notes: null,
      lines: lines.filter(l => l.description.trim().length > 0),
    }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'vendor-quotes'] }); close() },
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const actionMutation = useMutation({
    mutationFn: (p: { id: string; op: 'receive' | 'award' | 'reject' }) => {
      if (p.op === 'receive') return receiveVendorQuote(p.id, { quoteNumber: 'Q-' + Date.now(), quoteDate: new Date().toISOString().split('T')[0] ?? '', freight: 0, lines: [] })
      if (p.op === 'award') return awardVendorQuote(p.id)
      return rejectVendorQuote(p.id, null)
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['purchasing', 'vendor-quotes'] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const close = () => { setOpen(false); setFormError(null); setRfxNumber(''); setVendorId(''); setLines([{ itemId: '', description: '', quantity: 1, unitOfMeasure: 'EA', unitPrice: 0 }]) }
  const openForm = () => { setFormError(null); setOpen(true) }

  const updateLine = (i: number, k: keyof CreateVendorQuoteLineRequest, v: string | number) => {
    setLines(ls => ls.map((l, idx) => idx === i ? { ...l, [k]: v } : l))
  }

  const selectItem = (i: number, itemId: string) => {
    const item = items.find((it: any) => it.id === itemId)
    updateLine(i, 'itemId', itemId)
    if (item && !lines[i].description) {
      updateLine(i, 'description', item.description)
    }
  }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return quotes
    return quotes.filter((v: any) => v.rfxNumber.toLowerCase().includes(q) || (vendorNames.get(v.vendorId) ?? '').toLowerCase().includes(q))
  }, [quotes, search, vendorNames])

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Vendor Quotes (RFQ)" description={`${quotes.length} quote(s)`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New RFQ</Button>} />
        <CardContent>
          <div className="mb-4 max-w-md"><Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search RFQ # or vendor..." aria-label="Search quotes" leftIcon={<Search className="h-4 w-4" />} /></div>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            filtered.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">{search ? 'No matches.' : 'No vendor quotes yet.'}</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">RFQ #</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Quote Total</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Lines</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filtered.map((v: any) => (
                    <tr key={v.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{v.rfxNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{vendorNames.get(v.vendorId) ?? v.vendorId.slice(0, 8)}</td>
                      <td className="px-3 py-3">{statusBadge(v.status)}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${v.quoteTotal.toFixed(2)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{v.lines?.length ?? 0}</td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex justify-end gap-1">
                          {v.status === 'Requested' && <Button size="sm" variant="outline" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: v.id, op: 'receive' })}>Receive Quote</Button>}
                          {v.status === 'Received' && <Button size="sm" variant="primary" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: v.id, op: 'award' })}><Trophy className="h-3.5 w-3.5" /> Award</Button>}
                          {v.status === 'Received' && <Button size="sm" variant="ghost" className="text-red-600" disabled={actionMutation.isPending} onClick={() => actionMutation.mutate({ id: v.id, op: 'reject' })}><XCircle className="h-3.5 w-3.5" /> Reject</Button>}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Modal isOpen={open} onClose={close} title="New Vendor Quote (RFQ)" size="lg"
        footer={<><Button variant="secondary" onClick={close} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={() => createMutation.mutate()} isLoading={createMutation.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input value={rfxNumber} onChange={e => setRfxNumber(e.target.value)} label="RFQ #" placeholder="RFQ-0001" required />
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={vendorId} onChange={e => setVendorId(e.target.value)} required />
          </div>
          <div className="border-t pt-3 border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm font-medium text-gray-700 dark:text-gray-300">Quote Lines</p>
              <Button size="sm" variant="outline" onClick={() => setLines([...lines, { itemId: '', description: '', quantity: 1, unitOfMeasure: 'EA', unitPrice: 0 }])} leftIcon={<Plus className="h-3.5 w-3.5" />}>Add Line</Button>
            </div>
            {lines.map((l, i) => (
              <div key={i} className="grid grid-cols-2 md:grid-cols-6 gap-2 mb-2 items-end">
                <Select options={itemOptions} value={l.itemId ?? ''} onChange={e => selectItem(i, e.target.value)} label={i === 0 ? 'Item' : undefined} />
                <Input value={l.description} onChange={e => updateLine(i, 'description', e.target.value)} placeholder="Description" label={i === 0 ? 'Description' : undefined} />
                <Input type="number" step="0.01" min="0.01" value={String(l.quantity)} onChange={e => updateLine(i, 'quantity', Number(e.target.value))} label={i === 0 ? 'Qty' : undefined} />
                <select value={l.unitOfMeasure} onChange={(e) => { updateLine(i, 'unitOfMeasure', e.target.value); const itemId = l.itemId; if (itemId) { const item = items.find((x: any) => x.id === itemId); const baseUom = item?.baseUnitOfMeasure || 'EA'; setLineUomOptions(prev => ({ ...prev, [i]: [{ value: baseUom, label: baseUom + ' (base)' }] })); void getItemUomConversions(itemId).then((convs: UomConversionDto[]) => { const opts = [{ value: baseUom, label: baseUom + ' (base)' }]; for (const c of convs) { if (c.fromUOM === baseUom) opts.push({ value: c.toUOM, label: `${c.toUOM} (${c.conversionFactor}x)` }); } setLineUomOptions(prev => ({ ...prev, [i]: opts })); }).catch(() => {}); } }} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1">
                  {(lineUomOptions[i] ?? [{ value: 'EA', label: 'EA' }]).map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                </select>
                <Input type="number" step="0.01" min="0" value={String(l.unitPrice)} onChange={e => updateLine(i, 'unitPrice', Number(e.target.value))} label={i === 0 ? 'Unit Price' : undefined} />
                <div className="flex items-center gap-1">
                  <span className="text-sm text-gray-500 tabular-nums">${(l.quantity * l.unitPrice).toFixed(2)}</span>
                  {lines.length > 1 && (
                    <button type="button" onClick={() => setLines(lines.filter((_, idx) => idx !== i))} className="text-red-500 hover:text-red-700">
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      </Modal>
    </div>
  )
}
