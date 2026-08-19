import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getExchangeRates, createExchangeRate, companyId } from '@api/platform'
import type { ExchangeRate, CreateExchangeRateRequest } from '@/types/platform'

function todayIso(): string { return new Date().toISOString().split('T')[0] ?? '' }

export function ExchangeRatesPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateExchangeRateRequest>({ companyId: companyId(), fromCurrency: '', toCurrency: '', rate: 1, effectiveDate: todayIso() })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['platform', 'exchange-rates'], queryFn: () => getExchangeRates() })
  const createMut = useMutation({ mutationFn: (d: CreateExchangeRateRequest) => createExchangeRate(d), onSuccess: () => { qc.invalidateQueries({ queryKey: ['platform', 'exchange-rates'] }); close() }, onError: e => setFormError(getErrorMessage(e)) })
  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); setForm({ companyId: companyId(), fromCurrency: '', toCurrency: '', rate: 1, effectiveDate: todayIso() }); setOpen(true) }
  const submit = () => { setFormError(null); if (!form.fromCurrency || !form.toCurrency) { setFormError('From and To currency are required'); return } createMut.mutate(form) }
  const set = (k: keyof CreateExchangeRateRequest, v: string | number) => setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="Exchange Rates" description={`${rows.length} rate(s)`} action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No exchange rates yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">From</th><th className="px-3 py-2 font-medium text-gray-500">To</th>
                  <th className="px-3 py-2 font-medium text-gray-500 text-right">Rate</th><th className="px-3 py-2 font-medium text-gray-500">Effective</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: ExchangeRate) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.fromCurrency}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.toCurrency}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.rate.toFixed(4)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(r.effectiveDate).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
      <Modal isOpen={open} onClose={close} title="New Exchange Rate"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <Input value={form.fromCurrency} onChange={e => set('fromCurrency', e.target.value)} label="From Currency" placeholder="USD" required />
          <Input value={form.toCurrency} onChange={e => set('toCurrency', e.target.value)} label="To Currency" placeholder="EUR" required />
          <Input type="number" step="0.0001" min="0" value={String(form.rate)} onChange={e => set('rate', Number(e.target.value))} label="Rate" required />
          <Input type="date" value={form.effectiveDate.slice(0, 10)} onChange={e => set('effectiveDate', e.target.value)} label="Effective Date" required />
        </div>
      </Modal>
    </div>
  )
}
