import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getCurrencies, createCurrency } from '@api/platform'
import type { Currency, CreateCurrencyRequest } from '@/types/platform'

export function CurrenciesPage() {
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [form, setForm] = useState<CreateCurrencyRequest>({ code: '', name: '', symbol: '', decimalPlaces: 2 })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['platform', 'currencies'], queryFn: () => getCurrencies() })
  const createMut = useMutation({ mutationFn: (d: CreateCurrencyRequest) => createCurrency(d), onSuccess: () => { qc.invalidateQueries({ queryKey: ['platform', 'currencies'] }); close() }, onError: e => setFormError(getErrorMessage(e)) })
  const close = () => { setOpen(false); setFormError(null) }
  const openForm = () => { setFormError(null); setForm({ code: '', name: '', symbol: '', decimalPlaces: 2 }); setOpen(true) }
  const submit = () => { setFormError(null); if (!form.code || !form.name || !form.symbol) { setFormError('Code, Name and Symbol are required'); return } createMut.mutate(form) }
  const set = (k: keyof CreateCurrencyRequest, v: string | number) => setForm(f => ({ ...f, [k]: v }))

  return (
    <div className="space-y-6">
      {formError && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert"><AlertCircle className="h-5 w-5" /> <span>{formError}</span></div>}
      <Card>
        <CardHeader title="Currencies" description={`${rows.length} currency(ies)`} action={<Button variant="primary" size="sm" onClick={openForm} leftIcon={<Plus className="h-4 w-4" />}>New</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No currencies yet.</p> :
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="px-3 py-2 font-medium text-gray-500">Code</th><th className="px-3 py-2 font-medium text-gray-500">Name</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Symbol</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Decimals</th>
                  <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                </tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((r: Currency) => (
                    <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.code}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.name}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.symbol}</td>
                      <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{r.decimalPlaces}</td>
                      <td className="px-3 py-3"><Badge variant={r.isActive ? 'success' : 'neutral'} size="sm" dot>{r.isActive ? 'Active' : 'Inactive'}</Badge></td>
                    </tr>
                  ))}
                </tbody>
              </table></div>}
        </CardContent>
      </Card>
      <Modal isOpen={open} onClose={close} title="New Currency"
        footer={<><Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Create</Button></>}>
        <div className="space-y-4">
          <Input value={form.code} onChange={e => set('code', e.target.value)} label="Code" placeholder="USD" required />
          <Input value={form.name} onChange={e => set('name', e.target.value)} label="Name" placeholder="US Dollar" required />
          <Input value={form.symbol} onChange={e => set('symbol', e.target.value)} label="Symbol" placeholder="$" required />
          <Input type="number" min="0" value={String(form.decimalPlaces)} onChange={e => set('decimalPlaces', Number(e.target.value))} label="Decimal Places" />
        </div>
      </Modal>
    </div>
  )
}
