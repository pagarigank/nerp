// <copyright file="MovementsPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Input, Select } from '@components/ui/Input'
import { getMovements, getWarehouses, getItems } from '@api/inventory'

function money(n: number) { return n.toFixed(2) }

export function MovementsPage() {
  const [warehouseId, setWarehouseId] = useState('')
  const [itemId, setItemId] = useState('')
  const [txnType, setTxnType] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')

  const { data: warehouses = [] } = useQuery({ queryKey: ['inventory', 'warehouses'], queryFn: () => getWarehouses() })
  const { data: allItems = [] } = useQuery({ queryKey: ['inventory', 'items'], queryFn: () => getItems() })

  const itemOptions = [{ value: '', label: 'All items' }, ...allItems.map(i => ({ value: i.id, label: `${i.itemCode} - ${i.itemName}` }))]
  const whOptions = [{ value: '', label: 'All warehouses' }, ...warehouses.map(w => ({ value: w.id, label: `${w.warehouseCode} - ${w.warehouseName}` }))]
  const typeOptions = [
    { value: '', label: 'All types' },
    { value: 'Receipt', label: 'Receipt' },
    { value: 'Issue', label: 'Issue' },
    { value: 'Adjustment', label: 'Adjustment' },
    { value: 'Transfer', label: 'Transfer' },
    { value: 'Scrap', label: 'Scrap' },
  ]

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['inventory', 'movements', warehouseId, itemId, txnType, startDate, endDate],
    queryFn: () => getMovements(
      undefined,
      itemId || undefined,
      warehouseId || undefined,
      txnType || undefined,
      startDate || undefined,
      endDate || undefined,
    ),
  })

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title="Item Movement History" description={`${rows.length} movement(s)`} />
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-3 mb-4">
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Item</label>
              <Select value={itemId} onChange={e => setItemId(e.target.value)} options={itemOptions} aria-label="Filter item" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Warehouse</label>
              <Select value={warehouseId} onChange={e => setWarehouseId(e.target.value)} options={whOptions} aria-label="Filter warehouse" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Transaction Type</label>
              <Select value={txnType} onChange={e => setTxnType(e.target.value)} options={typeOptions} aria-label="Filter type" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">Start Date</label>
              <Input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 mb-1">End Date</label>
              <Input type="date" value={endDate} onChange={e => setEndDate(e.target.value)} />
            </div>
          </div>

          {isLoading ? (
            <p className="text-sm text-gray-500 py-8 text-center">Loading…</p>
          ) : rows.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">No movements found.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Item</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Warehouse</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Qty</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Unit Cost</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Extended</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Lot/Serial</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Reference</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Notes</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.slice(0, 200).map(r => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.transactionDate).toLocaleDateString()}</td>
                      <td className="px-3 py-3">
                        <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                          r.movementType === 'Receipt' ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400' :
                          r.movementType === 'Issue' || r.movementType === 'Scrap' ? 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400' :
                          r.movementType === 'Transfer' ? 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400' :
                          'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
                        }`}>{r.movementType}</span>
                      </td>
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.itemId.slice(0, 8)}…</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.warehouseId.slice(0, 8)}…</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.quantity}</td>
                      <td className="px-3 py-3 text-right text-gray-700 dark:text-gray-300">{money(r.unitCost ?? 0)}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white font-medium">{money(r.extendedCost ?? 0)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.lotId ? 'LOT' : (r.serialNumberId ? `SN: ${r.serialNumberId}` : '—')}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.referenceNumber ?? '—'}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300 max-w-[200px] truncate">{r.notes ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
