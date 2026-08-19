// <copyright file="ThreeWayMatchPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Calculator, AlertCircle, CheckCircle2, XCircle, Plus, Trash2 } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { validateThreeWayMatch, getVendors } from '@api/ap'
import { companyId } from '@api/platform'
import type { ThreeWayMatchRequest, ThreeWayMatchResult } from '@/types/ap'

interface MatchLine {
  itemCode: string
  description: string
  orderedQuantity: number
  receivedQuantity: number
  invoicedQuantity: number
  unitPrice: number
}

export function ThreeWayMatchPage() {
  const [formError, setFormError] = useState<string | null>(null)
  const [vendorId, setVendorId] = useState('')
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [invoiceTotal, setInvoiceTotal] = useState('0')
  const [lines, setLines] = useState<MatchLine[]>([
    { itemCode: '', description: '', orderedQuantity: 1, receivedQuantity: 1, invoicedQuantity: 1, unitPrice: 0 },
  ])
  const [result, setResult] = useState<ThreeWayMatchResult | null>(null)

  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })

  const vendorOptions = useMemo(
    () => vendors.map(v => ({ value: v.id, label: `${v.vendorId} - ${v.name}` })),
    [vendors]
  )

  const mut = useMutation({
    mutationFn: () => {
      const payload: ThreeWayMatchRequest = {
        companyId: companyId(),
        vendorId,
        invoiceNumber,
        invoiceTotal: Number(invoiceTotal),
        lines: lines.map(l => ({
          itemCode: l.itemCode,
          description: l.description,
          orderedQuantity: l.orderedQuantity,
          receivedQuantity: l.receivedQuantity,
          invoicedQuantity: l.invoicedQuantity,
          unitPrice: l.unitPrice,
          extendedAmount: l.invoicedQuantity * l.unitPrice,
        })),
      }
      return validateThreeWayMatch(payload)
    },
    onSuccess: (d) => { setResult(d); setFormError(null) },
    onError: (e) => { setResult(null); setFormError(getErrorMessage(e)) },
  })

  const setLine = (i: number, k: keyof MatchLine, v: string | number) =>
    setLines(ls => ls.map((l, idx) => idx === i ? { ...l, [k]: v } : l))

  const addLine = () =>
    setLines(ls => [...ls, { itemCode: '', description: '', orderedQuantity: 1, receivedQuantity: 1, invoicedQuantity: 1, unitPrice: 0 }])

  const removeLine = (i: number) =>
    setLines(ls => ls.filter((_, idx) => idx !== i))

  const runningTotal = lines.reduce((sum, l) => sum + l.invoicedQuantity * l.unitPrice, 0)

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <Card>
        <CardHeader title="Three-Way Match" description="Validate PO ↔ Receipt ↔ Voucher before voucher posting" />
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Select
              label="Vendor"
              placeholder="Select vendor..."
              options={vendorOptions}
              value={vendorId}
              onChange={e => setVendorId(e.target.value)}
              required
            />
            <Input value={invoiceNumber} onChange={e => setInvoiceNumber(e.target.value)} label="Invoice #" required />
            <Input type="number" step="0.01" min="0" value={invoiceTotal} onChange={e => setInvoiceTotal(e.target.value)} label="Invoice Total" />
          </div>

          <div className="border-t pt-4 border-gray-200 dark:border-gray-700">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm font-medium text-gray-700 dark:text-gray-300">Lines</p>
              <Button size="sm" variant="outline" onClick={addLine} leftIcon={<Plus className="h-4 w-4" />}>Add Line</Button>
            </div>
            <div className="space-y-2">
              {lines.map((l, i) => (
                <div key={i} className="grid grid-cols-2 md:grid-cols-7 gap-2 items-end">
                  <Input value={l.itemCode} onChange={e => setLine(i, 'itemCode', e.target.value)} placeholder="Item Code" label={i === 0 ? 'Item' : undefined} />
                  <Input value={l.description} onChange={e => setLine(i, 'description', e.target.value)} placeholder="Description" label={i === 0 ? 'Description' : undefined} />
                  <Input type="number" min="0" value={String(l.orderedQuantity)} onChange={e => setLine(i, 'orderedQuantity', Number(e.target.value))} label={i === 0 ? 'Ordered' : undefined} />
                  <Input type="number" min="0" value={String(l.receivedQuantity)} onChange={e => setLine(i, 'receivedQuantity', Number(e.target.value))} label={i === 0 ? 'Received' : undefined} />
                  <Input type="number" min="0" value={String(l.invoicedQuantity)} onChange={e => setLine(i, 'invoicedQuantity', Number(e.target.value))} label={i === 0 ? 'Invoiced' : undefined} />
                  <Input type="number" step="0.01" min="0" value={String(l.unitPrice)} onChange={e => setLine(i, 'unitPrice', Number(e.target.value))} label={i === 0 ? 'Unit Price' : undefined} />
                  <div className="flex items-center gap-1">
                    <span className="text-sm text-gray-500 tabular-nums">{(l.invoicedQuantity * l.unitPrice).toFixed(2)}</span>
                    {lines.length > 1 && (
                      <IconButton size="sm" variant="ghost" className="text-red-600" onClick={() => removeLine(i)} aria-label="Remove line">
                        <Trash2 className="h-3 w-3" />
                      </IconButton>
                    )}
                  </div>
                </div>
              ))}
            </div>
            <div className="mt-2 flex justify-end">
              <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Line Total: <span className="tabular-nums">${runningTotal.toFixed(2)}</span></span>
            </div>
          </div>

          <Button variant="primary" onClick={() => mut.mutate()} disabled={mut.isPending || !vendorId || !invoiceNumber}>
            <Calculator className="h-4 w-4" /> Validate Match
          </Button>
        </CardContent>
      </Card>

      {result && (
        <Card>
          <CardHeader
            title="Match Result"
            description={`Tolerance ${result.tolerancePercent}%`}
            action={result.isValid
              ? <Badge variant="success" size="sm" dot>Valid</Badge>
              : <Badge variant="error" size="sm" dot>Invalid</Badge>}
          />
          <CardContent>
            <div className="flex flex-wrap gap-2 mb-3">
              {result.errors.length > 0 && <Badge variant="error" size="sm">{result.errors.length} Error(s)</Badge>}
              {result.warnings.length > 0 && <Badge variant="warning" size="sm">{result.warnings.length} Warning(s)</Badge>}
            </div>
            <ul className="space-y-1 text-sm">
              {result.warnings.map((w, i) => (
                <li key={`w${i}`} className="flex items-center gap-2 text-amber-700 dark:text-amber-400"><AlertCircle className="h-4 w-4" /> {w}</li>
              ))}
              {result.errors.map((e, i) => (
                <li key={`e${i}`} className="flex items-center gap-2 text-red-700 dark:text-red-400"><XCircle className="h-4 w-4" /> {e}</li>
              ))}
              {result.warnings.length === 0 && result.errors.length === 0 && (
                <li className="flex items-center gap-2 text-green-700 dark:text-green-400"><CheckCircle2 className="h-4 w-4" /> No variances detected.</li>
              )}
            </ul>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
