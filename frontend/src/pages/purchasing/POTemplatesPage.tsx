// <copyright file="POTemplatesPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { UomSelect } from '@components/ui/UomSelect'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getPOTemplates, createPOTemplate, releasePOTemplate, companyId } from '@api/purchasing'
import { getVendors } from '@api/ap'
import { getItems } from '@api/inventory'
import type { CreatePOTemplateRequest, POTemplate } from '@/types/purchasing'

export function POTemplatesPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [releaseId, setReleaseId] = useState<string | null>(null)
  const [releaseAmount, setReleaseAmount] = useState('0')
  const [form, setForm] = useState<CreatePOTemplateRequest>({
    companyId: companyId(), templateCode: '', templateName: '', vendorId: '', orderType: 'Standard',
    description: null, blanketAmount: null, effectiveDate: null, expirationDate: null, isActive: true,
    lines: [{ lineNumber: 1, itemId: null, description: '', defaultQuantity: 1, unitOfMeasure: 'EA', unitPrice: 0, accountId: null, projectId: null }],
  })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['purchasing', 'po-templates'], queryFn: () => getPOTemplates() })
  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })
  const { data: items = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })

  const vendorOptions = useMemo(() => vendors.map((v: any) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })), [vendors])
  const itemOptions = useMemo(() => items.map((i: any) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })), [items])
  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach((v: any) => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const createMut = useMutation({ mutationFn: (d: CreatePOTemplateRequest) => createPOTemplate(d), onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'po-templates'] }); close() }, onError: e => setFormError(getErrorMessage(e)) })
  const releaseMut = useMutation({ mutationFn: () => releasePOTemplate(releaseId!, Number(releaseAmount)), onSuccess: () => { qc.invalidateQueries({ queryKey: ['purchasing', 'po-templates'] }); setReleaseId(null); setReleaseAmount('0') }, onError: e => setFormError(getErrorMessage(e)) })

  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => {
    setFormError(null)
    setForm({ companyId: companyId(), templateCode: '', templateName: '', vendorId: '', orderType: 'Standard', description: null, blanketAmount: null, effectiveDate: null, expirationDate: null, isActive: true, lines: [{ lineNumber: 1, itemId: null, description: '', defaultQuantity: 1, unitOfMeasure: 'EA', unitPrice: 0, accountId: null, projectId: null }] })
    setOpen(true)
  }
  const submit = () => {
    setFormError(null)
    if (!form.templateCode || !form.templateName || !form.vendorId) { setFormError('Template Code, Name and Vendor are required'); return }
    createMut.mutate(form)
  }
  const set = (k: keyof CreatePOTemplateRequest, v: string | number | null | boolean) => setForm(f => ({ ...f, [k]: v }))

  const setLine = (i: number, k: string, v: string | number | null) => {
    setForm(f => ({
      ...f,
      lines: f.lines.map((l, idx) => idx === i ? { ...l, [k]: v } : l),
    }))
  }

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="PO Templates" description={`${rows.length} blanket PO template(s)`} action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No PO templates yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Code</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Name</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Type</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Used / Remaining</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right"></th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: POTemplate) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.templateCode}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.templateName}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{vendorNames.get(r.vendorId) ?? r.vendorId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.orderType}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${r.amountUsed.toFixed(2)} / ${r.remainingAmount.toFixed(2)}</td>
                      <td className="px-3 py-3">
                        {r.isExpired ? <Badge variant="error" size="sm" dot>Expired</Badge> :
                          <Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge>}
                      </td>
                      <td className="px-3 py-3 text-right">
                        <Button size="sm" variant="outline" onClick={() => { setReleaseId(r.id); setReleaseAmount('0') }}>Release</Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Modal isOpen={open} onClose={close} title="New PO Template" size="lg"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input value={form.templateCode} onChange={e => set('templateCode', e.target.value)} label="Template Code" required />
            <Input value={form.templateName} onChange={e => set('templateName', e.target.value)} label="Template Name" required />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={form.vendorId} onChange={e => set('vendorId', e.target.value)} required />
            <Select label="Order Type" options={[{ value: 'Standard', label: 'Standard' }, { value: 'Blanket', label: 'Blanket' }, { value: 'Standing', label: 'Standing' }]} value={form.orderType} onChange={e => set('orderType', e.target.value)} />
          </div>
          <Input value={form.description ?? ''} onChange={e => set('description', e.target.value || null)} label="Description" />
          <div className="grid grid-cols-2 gap-3">
            <Input type="number" step="0.01" min="0" value={form.blanketAmount != null ? String(form.blanketAmount) : ''} onChange={e => set('blanketAmount', e.target.value ? Number(e.target.value) : null)} label="Blanket Amount" />
            <div className="flex items-end">
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 pb-2">
                <input type="checkbox" checked={form.isActive} onChange={e => set('isActive', e.target.checked)} className="h-4 w-4" /> Active
              </label>
            </div>
          </div>

          {/* Lines */}
          <div className="border-t pt-4 border-gray-200 dark:border-gray-700">
            <h3 className="text-sm font-medium text-gray-900 dark:text-white mb-2">Template Lines</h3>
            <div className="space-y-2">
              {form.lines.map((line, i) => (
                <div key={i} className="grid grid-cols-2 md:grid-cols-6 gap-2 items-end">
                  <Select options={itemOptions} value={line.itemId ?? ''} onChange={e => setLine(i, 'itemId', e.target.value || null)} label={i === 0 ? 'Item' : undefined} />
                  <Input value={line.description} onChange={e => setLine(i, 'description', e.target.value)} label={i === 0 ? 'Description' : undefined} />
                  <Input type="number" step="0.01" min="0" value={String(line.defaultQuantity)} onChange={e => setLine(i, 'defaultQuantity', Number(e.target.value))} label={i === 0 ? 'Qty' : undefined} />
                  <UomSelect value={line.unitOfMeasure} onChange={(e) => setLine(i, 'unitOfMeasure', e.target.value)} label={i === 0 ? 'UOM' : undefined} className="w-full text-sm rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1" />
                  <Input type="number" step="0.01" min="0" value={String(line.unitPrice)} onChange={e => setLine(i, 'unitPrice', Number(e.target.value))} label={i === 0 ? 'Price' : undefined} />
                </div>
              ))}
            </div>
          </div>
        </div>
      </Modal>

      <Modal isOpen={releaseId !== null} onClose={() => setReleaseId(null)} title="Release Against Template"
        footer={<><Button variant="secondary" onClick={() => setReleaseId(null)} disabled={releaseMut.isPending}>Cancel</Button><Button variant="primary" onClick={() => releaseMut.mutate()} isLoading={releaseMut.isPending}>Release</Button></>}>
        <div className="space-y-4">
          <Input type="number" step="0.01" min="0" value={releaseAmount} onChange={e => setReleaseAmount(e.target.value)} label="Release Amount" required />
        </div>
      </Modal>
    </div>
  )
}
