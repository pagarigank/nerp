import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '@stores/authStore'
import { getAccounts } from '@api/platform'
import { getSuspenseItems, resolveSuspense, discardSuspense } from '@api/gl'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Badge } from '@components/ui/Badge'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { AlertCircle } from 'lucide-react'
import { getErrorMessage } from '@api/client'
import { formatCurrency } from '@utils/helpers'
import type { PostingSuspenseItemDto, SuspenseStatus } from '@/types/gl'

const statusVariant: Record<SuspenseStatus, 'warning' | 'success' | 'neutral'> = {
  Pending: 'warning',
  Resolved: 'success',
  Discarded: 'neutral',
}

export function PostingSuspensePage() {
  const companyId = useAuthStore((s) => s.currentCompany?.id) ?? ''
  const queryClient = useQueryClient()
  const [filter, setFilter] = useState<SuspenseStatus | ''>('')
  const [resolveId, setResolveId] = useState<PostingSuspenseItemDto | null>(null)
  const [resolveAccount, setResolveAccount] = useState('')
  const [resolveDebit, setResolveDebit] = useState('')
  const [resolveCredit, setResolveCredit] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: items = [], isLoading } = useQuery({
    queryKey: ['suspense', companyId, filter],
    queryFn: () => getSuspenseItems(companyId, filter || undefined),
    enabled: !!companyId,
  })
  const { data: accounts = [] } = useQuery({
    queryKey: ['accounts', companyId],
    queryFn: () => getAccounts(companyId),
    enabled: !!companyId,
  })

  const resolveMutation = useMutation({
    mutationFn: () => resolveSuspense(resolveId!.id, {
      accountId: resolveAccount,
      debit: resolveDebit ? Number(resolveDebit) : 0,
      credit: resolveCredit ? Number(resolveCredit) : 0,
    }),
    onSuccess: () => {
      setResolveId(null)
      queryClient.invalidateQueries({ queryKey: ['suspense'] })
    },
    onError: (e) => setError(getErrorMessage(e)),
  })
  const discardMutation = useMutation({
    mutationFn: (id: string) => discardSuspense(id, { note: null }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['suspense'] }),
    onError: (e) => setError(getErrorMessage(e)),
  })

  const filterOptions = useMemo(() => [
    { value: '', label: 'All statuses' },
    { value: 'Pending', label: 'Pending' },
    { value: 'Resolved', label: 'Resolved' },
    { value: 'Discarded', label: 'Discarded' },
  ], [])

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [accounts]
  )

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Posting Suspense / Error Workbench</h1>
      <div className="w-64">
        <Select
          label="Filter by Status"
          options={filterOptions}
          value={filter}
          onChange={(e) => setFilter(e.target.value as SuspenseStatus | '')}
        />
      </div>
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}
      <Card>
        <CardHeader title={`Suspense Items (${items.length})`} />
        <CardContent>
          {isLoading ? <SkeletonTable columns={7} /> : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-gray-500">
                  <th className="py-2">Source</th>
                  <th>Reference</th>
                  <th>Reason</th>
                  <th className="text-right">Debit</th>
                  <th className="text-right">Credit</th>
                  <th>Status</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {items.map((it) => (
                  <tr key={it.id} className="border-b">
                    <td className="py-2">{it.sourceModule}</td>
                    <td>{it.sourceReference}</td>
                    <td>{it.reasonCode}</td>
                    <td className="text-right">{formatCurrency(it.debit)}</td>
                    <td className="text-right">{formatCurrency(it.credit)}</td>
                    <td><Badge variant={statusVariant[it.status]}>{it.status}</Badge></td>
                    <td className="space-x-1">
                      {it.status === 'Pending' && (
                        <>
                          <Button size="sm" onClick={() => { setResolveId(it); setResolveAccount(it.accountId ?? '') ; setResolveDebit(String(it.debit)); setResolveCredit(String(it.credit)) }}>Resolve</Button>
                          <Button size="sm" variant="secondary" onClick={() => discardMutation.mutate(it.id)}>Discard</Button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
                {items.length === 0 && <tr><td colSpan={7} className="py-4 text-center text-gray-500">No suspense items.</td></tr>}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>        <Modal isOpen={!!resolveId} onClose={() => setResolveId(null)} title="Resolve Suspense Item" size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={() => setResolveId(null)} disabled={resolveMutation.isPending}>Cancel</Button>
            <Button variant="primary" disabled={!resolveAccount || resolveMutation.isPending} onClick={() => resolveMutation.mutate()} isLoading={resolveMutation.isPending}>Post Journal & Resolve</Button>
          </>
        }>
        <div className="space-y-4">
          <Combobox
            label="Target Account"
            placeholder="Select account..."
            options={accountOptions}
            value={resolveAccount}
            onChange={setResolveAccount}
            required
          />
          <div className="grid grid-cols-2 gap-4">
            <Input label="Debit" type="number" step="0.01" min="0" value={resolveDebit} onChange={(e) => setResolveDebit(e.target.value)} className="text-right tabular-nums" />
            <Input label="Credit" type="number" step="0.01" min="0" value={resolveCredit} onChange={(e) => setResolveCredit(e.target.value)} className="text-right tabular-nums" />
          </div>
        </div>
      </Modal>
    </div>
  )
}
