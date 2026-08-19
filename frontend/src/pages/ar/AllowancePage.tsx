import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Calculator, CheckCircle2, Plus } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Modal } from '@components/ui/Modal'
import { Input } from '@components/ui/Input'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getAllowanceRuns, createAllowanceRun, postAllowanceRun } from '@api/ar'
import type {
  CreateAllowanceRunRequest,
  AllowanceMethod,
} from '@/types/ar'

const METHODS: AllowanceMethod[] = ['PercentageOfReceivables', 'AgingCategories', 'Specific']

export function AllowancePage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [isOpen, setIsOpen] = useState(false)

  const { data: runs = [], isLoading } = useQuery({
    queryKey: ['ar', 'allowance-runs'],
    queryFn: () => getAllowanceRuns(),
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateAllowanceRunRequest) => createAllowanceRun(data),
    onSuccess: () => {
      setError(null)
      setIsOpen(false)
      queryClient.invalidateQueries({ queryKey: ['ar', 'allowance-runs'] })
    },
    onError: err => setError(getErrorMessage(err)),
  })

  const postMutation = useMutation({
    mutationFn: (id: string) => postAllowanceRun(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['ar', 'allowance-runs'] }),
    onError: err => setError(getErrorMessage(err)),
  })

  return (
    <div className="space-y-6">
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Allowance for Doubtful Accounts"
          description="Estimate and reserve for uncollectible receivables."
          action={
            <Button variant="primary" size="sm" leftIcon={<Plus className="h-4 w-4" />} onClick={() => setIsOpen(true)}>
              New Estimate
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : runs.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No allowance runs yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">As Of</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Method</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Estimated</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {runs.map(run => (
                    <tr key={run.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{run.name}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(run.asOfDate)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{run.method}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{formatCurrency(run.estimatedAmount)}</td>
                      <td className="px-3 py-3">{run.isPosted ? <span className="text-emerald-600">Posted</span> : <span className="text-amber-600">Draft</span>}</td>
                      <td className="px-3 py-3">
                        <div className="flex justify-end">
                          {!run.isPosted && (
                            <Button variant="outline" size="sm" leftIcon={<CheckCircle2 className="h-4 w-4" />} onClick={() => postMutation.mutate(run.id)} isLoading={postMutation.isPending}>
                              Post
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <NewRunModal isOpen={isOpen} onClose={() => setIsOpen(false)} onSubmit={data => createMutation.mutate(data)} isSubmitting={createMutation.isPending} />
    </div>
  )
}

interface NewRunModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: CreateAllowanceRunRequest) => void
  isSubmitting: boolean
}

function NewRunModal({ isOpen, onClose, onSubmit, isSubmitting }: NewRunModalProps) {
  const [name, setName] = useState('')
  const [asOfDate, setAsOfDate] = useState(new Date().toISOString().slice(0, 10))
  const [method, setMethod] = useState<AllowanceMethod>('PercentageOfReceivables')
  const [pct, setPct] = useState(5)
  const [rate1, setRate1] = useState(2)
  const [rate30, setRate30] = useState(5)
  const [rate60, setRate60] = useState(10)
  const [rate90, setRate90] = useState(25)
  const [specific, setSpecific] = useState(0)

  const submit = () => {
    if (!name.trim()) return
    onSubmit({
      companyId: '',
      name: name.trim(),
      asOfDate: new Date(asOfDate).toISOString(),
      method,
      percentageOfReceivables: pct,
      agingRateCurrent: rate1,
      agingRate1To30: rate30,
      agingRate31To60: rate60,
      agingRate61To90: rate90,
      agingRateOver90: 50,
      specificAmount: specific,
    })
    setName('')
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="New Allowance Estimate">
      <div className="space-y-4">
        <Input label="Name" value={name} onChange={e => setName(e.target.value)} />
        <Input label="As Of Date" type="date" value={asOfDate} onChange={e => setAsOfDate(e.target.value)} />
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Method</label>
          <select value={method} onChange={e => setMethod(e.target.value as AllowanceMethod)} className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm">
            {METHODS.map(m => <option key={m} value={m}>{m}</option>)}
          </select>
        </div>
        {method === 'PercentageOfReceivables' && (
          <Input label="% of Receivables" type="number" value={String(pct)} onChange={e => setPct(Number(e.target.value))} />
        )}
        {method === 'AgingCategories' && (
          <div className="grid grid-cols-2 gap-2">
            <Input label="Current %" type="number" value={String(rate1)} onChange={e => setRate1(Number(e.target.value))} />
            <Input label="1–30 %" type="number" value={String(rate30)} onChange={e => setRate30(Number(e.target.value))} />
            <Input label="31–60 %" type="number" value={String(rate60)} onChange={e => setRate60(Number(e.target.value))} />
            <Input label="61–90 %" type="number" value={String(rate90)} onChange={e => setRate90(Number(e.target.value))} />
          </div>
        )}
        {method === 'Specific' && (
          <Input label="Specific Amount" type="number" value={String(specific)} onChange={e => setSpecific(Number(e.target.value))} />
        )}
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={submit} isLoading={isSubmitting} disabled={!name.trim()} leftIcon={<Calculator className="h-4 w-4" />}>Estimate</Button>
        </div>
      </div>
    </Modal>
  )
}
