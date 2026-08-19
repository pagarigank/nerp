// <copyright file="CreditMemosPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle, Trash2 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getMemos, createMemo, getCustomers, DEMO_COMPANY_ID } from '@api/ar'
import type { ArMemo, CreateMemoRequest } from '@/types/ar'

function statusBadge(status: string) {
  const s = status.toLowerCase()
  if (s.includes('void')) return <Badge variant="error" size="sm" dot>{status}</Badge>
  if (s.includes('applied')) return <Badge variant="success" size="sm" dot>{status}</Badge>
  if (s.includes('open')) return <Badge variant="info" size="sm" dot>{status}</Badge>
  return <Badge variant="neutral" size="sm" dot>{status}</Badge>
}

interface MemoLine {
  accountId: string
  description: string
  quantity: string
  unitPrice: string
  taxAmount: string
  discountAmount: string
}

function blankLine(): MemoLine {
  return { accountId: '', description: '', quantity: '1', unitPrice: '0', taxAmount: '0', discountAmount: '0' }
}

export function CreditMemosPage() {
  const qc = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [customerId, setCustomerId] = useState('')
  const [referenceNumber, setReferenceNumber] = useState('')
  const [memoDate, setMemoDate] = useState(new Date().toISOString().slice(0, 10))
  const [memoType, setMemoType] = useState<'CreditMemo' | 'DebitMemo'>('CreditMemo')
  const [description, setDescription] = useState('')
  const [lines, setLines] = useState<MemoLine[]>([blankLine()])

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['ar', 'memos'], queryFn: () => getMemos() })
  const { data: customers = [] } = useQuery({ queryKey: ['ar', 'customers'], queryFn: getCustomers })

  const customerOptions = useMemo(() => customers.map((c: any) => ({ value: c.id, label: `${c.customerId} - ${c.name}` })), [customers])
  const customerNames = useMemo(() => {
    const map = new Map<string, string>()
    customers.forEach((c: any) => map.set(c.id, c.name))
    return map
  }, [customers])

  const createMutation = useMutation({
    mutationFn: (data: CreateMemoRequest) => createMemo(data),
    onSuccess: () => { setFormError(null); setIsOpen(false); qc.invalidateQueries({ queryKey: ['ar', 'memos'] }) },
    onError: e => setFormError(getErrorMessage(e)),
  })

  const totalAmount = lines.reduce((sum, l) => sum + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0) + (Number(l.taxAmount) || 0) - (Number(l.discountAmount) || 0), 0)

  const updateLine = (i: number, k: keyof MemoLine, v: string) => {
    setLines(ls => ls.map((l, idx) => idx === i ? { ...l, [k]: v } : l))
  }

  const addLine = () => setLines(ls => [...ls, blankLine()])
  const removeLine = (i: number) => setLines(ls => ls.filter((_, idx) => idx !== i))

  const submit = () => {
    setFormError(null)
    if (!customerId) { setFormError('Customer is required'); return }
    if (!referenceNumber.trim()) { setFormError('Reference number is required'); return }
    const validLines = lines.filter(l => l.description.trim() && Number(l.unitPrice) > 0)
    if (validLines.length === 0) { setFormError('Add at least one line with description and price'); return }

    createMutation.mutate({
      companyId: DEMO_COMPANY_ID,
      customerId,
      referenceNumber: referenceNumber.trim(),
      memoDate: new Date(memoDate).toISOString(),
      memoType: memoType === 'CreditMemo' ? 0 : 1,
      description: description || null,
      invoiceId: null,
      lines: validLines.map(l => ({
        accountId: l.accountId || '',
        description: l.description.trim(),
        quantity: Number(l.quantity) || 1,
        unitPrice: Number(l.unitPrice) || 0,
        taxAmount: Number(l.taxAmount) || 0,
        discountAmount: Number(l.discountAmount) || 0,
      })),
    })
  }

  const openForm = () => {
    setFormError(null)
    setCustomerId(''); setReferenceNumber(''); setMemoDate(new Date().toISOString().slice(0, 10))
    setMemoType('CreditMemo'); setDescription(''); setLines([blankLine()])
    setIsOpen(true)
  }

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Credit / Debit Memos" description={`${rows.length} memo(s)`}
          action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New Memo</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No memos yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Reference</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Customer</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Type</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Date</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: ArMemo) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.referenceNumber}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{customerNames.get(r.customerId) ?? r.customerId.slice(0, 8)}</td>
                      <td className="px-3 py-3"><Badge variant={r.memoType === 'CreditMemo' ? 'info' : 'warning'} size="sm">{r.memoType === 'CreditMemo' ? 'Credit' : 'Debit'}</Badge></td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.memoDate).toLocaleDateString()}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${r.totalAmount.toFixed(2)}</td>
                      <td className="px-3 py-3">{statusBadge(r.status)}</td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>

      <Modal isOpen={isOpen} onClose={() => setIsOpen(false)} title="New Credit / Debit Memo" size="lg"
        footer={<><Button variant="secondary" onClick={() => setIsOpen(false)} disabled={createMutation.isPending}>Cancel</Button>
          <Button variant="primary" onClick={submit} isLoading={createMutation.isPending}>Create Memo</Button></>}>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Select label="Customer" placeholder="Select customer..." options={customerOptions} value={customerId} onChange={e => setCustomerId(e.target.value)} required />
            <Select label="Type" options={[{ value: 'CreditMemo', label: 'Credit Memo' }, { value: 'DebitMemo', label: 'Debit Memo' }]} value={memoType} onChange={e => setMemoType(e.target.value as any)} />
            <Input value={referenceNumber} onChange={e => setReferenceNumber(e.target.value)} label="Reference #" placeholder="e.g. CM-001" required />
            <Input type="date" value={memoDate} onChange={e => setMemoDate(e.target.value)} label="Date" required />
          </div>
          <Input value={description} onChange={e => setDescription(e.target.value)} label="Description" placeholder="Optional description" />

          <div className="border-t pt-3 border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm font-medium text-gray-700 dark:text-gray-300">Memo Lines</p>
              <Button size="sm" variant="outline" onClick={addLine}>+ Add Line</Button>
            </div>
            {lines.map((l, i) => (
              <div key={i} className="grid grid-cols-2 md:grid-cols-5 gap-2 mb-2 items-end">
                <Input value={l.description} onChange={e => updateLine(i, 'description', e.target.value)} placeholder="Description" label={i === 0 ? 'Description' : undefined} />
                <Input type="number" step="1" min="1" value={l.quantity} onChange={e => updateLine(i, 'quantity', e.target.value)} label={i === 0 ? 'Qty' : undefined} />
                <Input type="number" step="0.01" min="0" value={l.unitPrice} onChange={e => updateLine(i, 'unitPrice', e.target.value)} placeholder="0.00" label={i === 0 ? 'Unit Price' : undefined} />
                <Input type="number" step="0.01" min="0" value={l.taxAmount} onChange={e => updateLine(i, 'taxAmount', e.target.value)} placeholder="0.00" label={i === 0 ? 'Tax' : undefined} />
                <div className="flex items-center gap-2">
                  <span className="text-sm text-gray-500 tabular-nums">${((Number(l.quantity) || 0) * (Number(l.unitPrice) || 0) + (Number(l.taxAmount) || 0) - (Number(l.discountAmount) || 0)).toFixed(2)}</span>
                  {lines.length > 1 && <button type="button" onClick={() => removeLine(i)} className="text-red-500 hover:text-red-700"><Trash2 className="h-3.5 w-3.5" /></button>}
                </div>
              </div>
            ))}
            <div className="mt-2 flex justify-end text-sm font-medium">
              Total: <span className="ml-2 tabular-nums">${totalAmount.toFixed(2)}</span>
            </div>
          </div>
        </div>
      </Modal>
    </div>
  )
}
