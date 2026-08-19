// <copyright file="VendorStatementPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { Plus, Trash2, AlertCircle } from 'lucide-react'
import { getVendors, createVendorStatement, getVendorStatements, closeVendorStatement } from '@api/ap'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import type { Vendor, VendorStatementDto, CreateVendorStatementLineRequest } from '@/types/ap'

interface StatementLine {
  reference: string
  statementAmount: string
  bookAmount: string
  isDisputed: boolean
  note: string
}

export function VendorStatementPage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [vendorId, setVendorId] = useState('')
  const [statementNumber, setStatementNumber] = useState('')
  const [statementDate, setStatementDate] = useState(new Date().toISOString().slice(0, 10))
  const [lines, setLines] = useState<StatementLine[]>([
    { reference: '', statementAmount: '', bookAmount: '', isDisputed: false, note: '' },
  ])
  const [err, setErr] = useState<string | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors(), enabled: !!companyId })
  const { data: statements = [], isLoading } = useQuery({ queryKey: ['statements', companyId], queryFn: () => getVendorStatements(companyId), enabled: !!companyId })

  const vendorOptions = useMemo(
    () => vendors.map((v: Vendor) => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  const vendorNames = useMemo(() => {
    const map = new Map<string, string>()
    vendors.forEach((v: Vendor) => map.set(v.id, `${v.vendorId} - ${v.name}`))
    return map
  }, [vendors])

  const setLine = (i: number, k: keyof StatementLine, v: string | boolean) =>
    setLines(ls => ls.map((l, idx) => idx === i ? { ...l, [k]: v } : l))

  const addLine = () =>
    setLines(ls => [...ls, { reference: '', statementAmount: '', bookAmount: '', isDisputed: false, note: '' }])

  const removeLine = (i: number) =>
    setLines(ls => ls.filter((_, idx) => idx !== i))

  const statementTotal = lines.reduce((sum, l) => sum + (Number(l.statementAmount) || 0), 0)
  const bookTotal = lines.reduce((sum, l) => sum + (Number(l.bookAmount) || 0), 0)
  const disputedTotal = lines.filter(l => l.isDisputed).reduce((sum, l) => sum + (Number(l.statementAmount) || 0), 0)

  const create = useMutation({
    mutationFn: () => createVendorStatement({
      companyId,
      vendorId,
      statementNumber,
      statementDate: new Date(statementDate).toISOString(),
      statementTotal,
      lines: lines.map(l => ({
        reference: l.reference,
        statementAmount: Number(l.statementAmount) || 0,
        bookAmount: Number(l.bookAmount) || 0,
        isDisputed: l.isDisputed,
        note: l.note || null,
      })),
    }),
    onSuccess: () => {
      setErr(null); setVendorId(''); setStatementNumber(''); setStatementDate(new Date().toISOString().slice(0, 10))
      setLines([{ reference: '', statementAmount: '', bookAmount: '', isDisputed: false, note: '' }])
      queryClient.invalidateQueries({ queryKey: ['statements'] })
    },
    onError: (e) => setErr(getErrorMessage(e)),
  })

  const close = useMutation({
    mutationFn: (id: string) => closeVendorStatement(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['statements'] }),
    onError: (e) => setErr(getErrorMessage(e)),
  })

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{err}</span></div>}

      <Card>
        <CardHeader title="Import / Create Statement" description="Record a vendor statement and reconcile against book balances" />
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Select label="Vendor" placeholder="Select vendor..." options={vendorOptions} value={vendorId} onChange={e => setVendorId(e.target.value)} required />
            <Input placeholder="e.g. STMT-2026-001" value={statementNumber} onChange={e => setStatementNumber(e.target.value)} label="Statement Number" required />
            <Input type="date" value={statementDate} onChange={e => setStatementDate(e.target.value)} label="Statement Date" required />
          </div>

          <div className="border-t pt-4 border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm font-medium text-gray-700 dark:text-gray-300">Statement Lines</p>
              <Button size="sm" variant="outline" onClick={addLine} leftIcon={<Plus className="h-4 w-4" />}>Add Line</Button>
            </div>
            <div className="space-y-2">
              {lines.map((l, i) => (
                <div key={i} className="grid grid-cols-2 md:grid-cols-6 gap-2 items-end">
                  <Input value={l.reference} onChange={e => setLine(i, 'reference', e.target.value)} placeholder="Reference (INV-001)" label={i === 0 ? 'Reference' : undefined} />
                  <Input type="number" step="0.01" min="0" value={l.statementAmount} onChange={e => setLine(i, 'statementAmount', e.target.value)} placeholder="0.00" label={i === 0 ? 'Stmt Amount' : undefined} />
                  <Input type="number" step="0.01" min="0" value={l.bookAmount} onChange={e => setLine(i, 'bookAmount', e.target.value)} placeholder="0.00" label={i === 0 ? 'Book Amount' : undefined} />
                  <div className="flex items-center gap-2">
                    <input type="checkbox" checked={l.isDisputed} onChange={e => setLine(i, 'isDisputed', e.target.checked)} className="h-4 w-4" />
                    <span className="text-sm text-gray-600">{i === 0 ? 'Disputed' : ''}</span>
                  </div>
                  <Input value={l.note} onChange={e => setLine(i, 'note', e.target.value)} placeholder="Note" label={i === 0 ? 'Note' : undefined} />
                  <div className="flex items-center gap-2">
                    {lines.length > 1 && (
                      <IconButton size="sm" variant="ghost" className="text-red-600" onClick={() => removeLine(i)} aria-label="Remove line">
                        <Trash2 className="h-3 w-3" />
                      </IconButton>
                    )}
                  </div>
                </div>
              ))}
            </div>
            <div className="mt-3 grid grid-cols-3 gap-4 text-sm">
              <div><span className="text-gray-500">Statement Total:</span> <span className="font-medium tabular-nums">${statementTotal.toFixed(2)}</span></div>
              <div><span className="text-gray-500">Book Total:</span> <span className="font-medium tabular-nums">${bookTotal.toFixed(2)}</span></div>
              <div><span className="text-gray-500">Disputed:</span> <span className="font-medium tabular-nums text-amber-600">${disputedTotal.toFixed(2)}</span></div>
            </div>
          </div>

          <Button variant="primary" disabled={!vendorId || !statementNumber || create.isPending} onClick={() => create.mutate()} isLoading={create.isPending}>
            Add Statement
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Reconciliations" description={`${statements.length} statement(s)`} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> : (
            statements.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No statements.</p> : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Statement</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Stmt Total</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Book</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Disputed</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Lines</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Action</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {statements.map((s: VendorStatementDto) => (
                      <tr key={s.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{s.statementNumber}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{vendorNames.get(s.vendorId) ?? s.vendorId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-right text-gray-900 dark:text-white tabular-nums">${s.statementTotal.toLocaleString()}</td>
                        <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300 tabular-nums">${s.bookTotal.toLocaleString()}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{s.disputedTotal > 0 ? <span className="text-amber-600">${s.disputedTotal.toLocaleString()}</span> : '—'}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{s.lines?.length ?? 0}</td>
                        <td className="px-3 py-3"><Badge variant={s.status === 'Closed' ? 'success' : 'warning'} size="sm" dot>{s.status}</Badge></td>
                        <td className="px-3 py-3 text-right">
                          {s.status === 'Open' && (
                            <Button variant="outline" size="sm" disabled={close.isPending} onClick={() => close.mutate(s.id)}>Close</Button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
          )}
        </CardContent>
      </Card>
    </div>
  )
}
