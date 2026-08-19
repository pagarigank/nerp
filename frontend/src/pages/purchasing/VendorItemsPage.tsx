// <copyright file="VendorItemsPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getVendorItems, createVendorItem, companyId } from '@api/purchasing'
import { getVendors } from '@api/ap'
import { getItems } from '@api/inventory'
import type { CreateVendorItemRequest } from '@/types/purchasing'

export function VendorItemsPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateVendorItemRequest>({
    companyId: companyId(), vendorId: '', itemCode: '', vendorItemCode: '', description: null, unitCost: 0, leadTimeDays: 0, minimumOrderQuantity: 1,
  })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['purchasing', 'vendor-items'], queryFn: () => getVendorItems() })
  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })

  const vendorOptions = useMemo(() => vendors.map((v: any) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })), [vendors])
  const itemOptions = useMemo(() => items.map((i: any) => ({ value: i.itemCode, label: `${i.itemCode} - ${i.description}` })), [items])
  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach((v: any) => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const createMut = useMutation({
    mutationFn: (d: CreateVendorItemRequest) => createVendorItem(d),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'vendor-items'] }); close() },
    onError: e => setFormError(getErrorMessage(e)),
  })

  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => {
    setFormError(null)
    setForm({ companyId: companyId(), vendorId: '', itemCode: '', vendorItemCode: '', description: null, unitCost: 0, leadTimeDays: 0, minimumOrderQuantity: 1 })
    setOpen(true)
  }
  const submit = () => {
    setFormError(null)
    if (!form.vendorId || !form.itemCode || !form.vendorItemCode) { setFormError('Vendor, Item Code, and Vendor Item Code are required'); return }
    createMut.mutate(form)
  }
  const set = (k: keyof CreateVendorItemRequest, v: string | number | null) => setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="Vendor Items" description={`${rows.length} vendor item(s)`} action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No vendor items yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Item Code</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Vendor Item</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Description</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Cost</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Lead (d)</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">MOQ</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Primary</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: any) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{vendorNames.get(r.vendorId) ?? r.vendorId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.itemCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.vendorItemCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.description ?? '—'}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${r.unitCost.toFixed(2)}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.leadTimeDays}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{r.minimumOrderQuantity}</td>
                      <td className="px-3 py-3">{r.isPrimaryVendor ? <Badge variant="info" size="sm">Primary</Badge> : '—'}</td>
                      <td className="px-3 py-3"><Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
      <Modal isOpen={open} onClose={close} title="New Vendor Item"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={form.vendorId} onChange={e => set('vendorId', e.target.value)} required />
          <Select label="Item" placeholder="Select item..." options={itemOptions} value={form.itemCode} onChange={e => set('itemCode', e.target.value)} required />
          <Input value={form.vendorItemCode} onChange={e => set('vendorItemCode', e.target.value)} label="Vendor Item Code" placeholder="Vendor's part number" required />
          <Input value={form.description ?? ''} onChange={e => set('description', e.target.value || null)} label="Description" placeholder="Vendor's description" />
          <div className="grid grid-cols-3 gap-3">
            <Input type="number" step="0.01" min="0" value={String(form.unitCost)} onChange={e => set('unitCost', Number(e.target.value))} label="Unit Cost" />
            <Input type="number" min="0" value={String(form.leadTimeDays)} onChange={e => set('leadTimeDays', Number(e.target.value))} label="Lead Time (days)" />
            <Input type="number" min="0" value={String(form.minimumOrderQuantity)} onChange={e => set('minimumOrderQuantity', Number(e.target.value))} label="MOQ" />
          </div>
        </div>
      </Modal>
    </div>
  )
}
