// Inventory > UOM Conversions (per-item unit of measure conversions).
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import {
  getItems,
  getItemUomConversions,
  createItemUomConversion,
  updateItemUomConversion,
  deleteItemUomConversion,
  convertUom,
} from '@api/inventory'

const COMMON_UOMS = ['EA', 'BX', 'CS', 'PK', 'KG', 'G', 'LB', 'OZ', 'L', 'ML', 'M', 'CM', 'FT', 'HR', 'DAY']

export function UomConversionsPage() {
  const qc = useQueryClient()
  const [itemId, setItemId] = useState('')
  const [fromUOM, setFromUOM] = useState('EA')
  const [toUOM, setToUOM] = useState('CS')
  const [factor, setFactor] = useState('1')
  const [convFrom, setConvFrom] = useState('EA')
  const [convTo, setConvTo] = useState('CS')
  const [convQty, setConvQty] = useState('1')
  const [err, setErr] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const [result, setResult] = useState<number | null>(null)

  const { data: items = [] } = useQuery({
    queryKey: ['inventory', 'items-mini'],
    queryFn: () => getItems(),
  })
  const { data: conversions = [], isLoading } = useQuery({
    queryKey: ['inventory', 'uom-conversions', itemId],
    queryFn: () => getItemUomConversions(itemId),
    enabled: !!itemId,
  })

  const addConv = useMutation({
    mutationFn: () =>
      createItemUomConversion(itemId, {
        fromUOM,
        toUOM,
        conversionFactor: Number(factor),
      }),
    onSuccess: () => {
      setOk('Conversion added')
      setErr(null)
      setFactor('1')
      void qc.invalidateQueries({ queryKey: ['inventory', 'uom-conversions', itemId] })
    },
    onError: (e) => {
      setErr(getErrorMessage(e))
      setOk(null)
    },
  })

  const delConv = useMutation({
    mutationFn: (id: string) => deleteItemUomConversion(itemId, id),
    onSuccess: () => {
      setOk('Conversion deleted')
      setErr(null)
      void qc.invalidateQueries({ queryKey: ['inventory', 'uom-conversions', itemId] })
    },
    onError: (e) => {
      setErr(getErrorMessage(e))
      setOk(null)
    },
  })

  const doConvert = useMutation({
    mutationFn: () =>
      convertUom(itemId, {
        fromUOM: convFrom,
        toUOM: convTo,
        quantity: Number(convQty),
      }),
    onSuccess: (r) => {
      setResult(r.convertedQuantity)
      setErr(null)
    },
    onError: (e) => {
      setErr(getErrorMessage(e))
      setResult(null)
    },
  })

  const itemOptions = [
    { value: '', label: 'Select item…' },
    ...items.map((i) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })),
  ]
  const uomOptions = [...new Set([...COMMON_UOMS, fromUOM, toUOM, convFrom, convTo])].map((u) => ({
    value: u,
    label: u,
  }))

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-slate-800 dark:text-slate-100">UOM Conversions</h1>
        <p className="text-sm text-slate-500">Define unit-of-measure conversions per item and convert quantities between UOMs.</p>
      </div>

      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}
      {ok && <div className="p-4 rounded-lg bg-green-50 border border-green-200 text-green-700 text-sm">{ok}</div>}

      <Card>
        <CardHeader>Select Item</CardHeader>
        <CardContent className="flex gap-4 items-end flex-wrap">
          <Select
            label="Item"
            options={itemOptions}
            value={itemId}
            onChange={(e) => setItemId(e.target.value)}
          />
        </CardContent>
      </Card>

      {itemId && (
        <Card>
          <CardHeader>Conversions for selected item</CardHeader>
          <CardContent>
            {isLoading ? (
              <p className="text-sm text-slate-500">Loading…</p>
            ) : conversions.length === 0 ? (
              <p className="text-sm text-slate-500">No conversions defined yet.</p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left border-b border-slate-200 dark:border-slate-700">
                    <th className="py-2">From</th>
                    <th className="py-2">To</th>
                    <th className="py-2">Factor</th>
                    <th className="py-2"></th>
                  </tr>
                </thead>
                <tbody>
                  {conversions.map((c) => (
                    <tr key={c.id} className="border-b border-slate-100 dark:border-slate-800">
                      <td className="py-2">{c.fromUOM}</td>
                      <td className="py-2">{c.toUOM}</td>
                      <td className="py-2">{c.conversionFactor}</td>
                      <td className="py-2 text-right">
                        <Button variant="ghost" size="sm" onClick={() => delConv.mutate(c.id)}>
                          Delete
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            <div className="mt-6 flex gap-4 items-end flex-wrap border-t border-slate-200 dark:border-slate-700 pt-4">
              <Select label="From UOM" options={uomOptions} value={fromUOM} onChange={(e) => setFromUOM(e.target.value)} />
              <Select label="To UOM" options={uomOptions} value={toUOM} onChange={(e) => setToUOM(e.target.value)} />
              <Input label="Factor (1 From = ? To)" value={factor} onChange={(e) => setFactor(e.target.value)} />
              <Button onClick={() => addConv.mutate()} disabled={!fromUOM || !toUOM || !factor}>
                Add Conversion
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>Convert Quantity</CardHeader>
        <CardContent>
          <div className="flex gap-4 items-end flex-wrap">
            <Input label="Quantity" value={convQty} onChange={(e) => setConvQty(e.target.value)} />
            <Select label="From" options={uomOptions} value={convFrom} onChange={(e) => setConvFrom(e.target.value)} />
            <Select label="To" options={uomOptions} value={convTo} onChange={(e) => setConvTo(e.target.value)} />
            <Button onClick={() => doConvert.mutate()} disabled={!itemId || !convFrom || !convTo || !convQty}>
              Convert
            </Button>
            {result !== null && (
              <div className="text-lg font-semibold text-slate-800 dark:text-slate-100">
                = {result} {convTo}
              </div>
            )}
          </div>
          {!itemId && <p className="text-sm text-slate-400 mt-2">Select an item above to enable conversion.</p>}
        </CardContent>
      </Card>
    </div>
  )
}
