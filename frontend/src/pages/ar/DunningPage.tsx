import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Mail, FileText, Play } from 'lucide-react'
import { formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Modal } from '@components/ui/Modal'
import { Input } from '@components/ui/Input'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getDunningTemplates,
  createDunningTemplate,
  runDunning,
} from '@api/ar'
import type {
  CreateDunningTemplateRequest,
  DunningBucket,
  DunningRunResult,
} from '@/types/ar'

const BUCKETS: DunningBucket[] = ['Current', 'Days1To30', 'Days31To60', 'Days61To90', 'Over90']

export function DunningPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [runResult, setRunResult] = useState<DunningRunResult | null>(null)

  const { data: templates = [], isLoading } = useQuery({
    queryKey: ['ar', 'dunning-templates'],
    queryFn: () => getDunningTemplates(),
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateDunningTemplateRequest) => createDunningTemplate(data),
    onSuccess: () => {
      setError(null)
      setIsOpen(false)
      queryClient.invalidateQueries({ queryKey: ['ar', 'dunning-templates'] })
    },
    onError: err => setError(getErrorMessage(err)),
  })

  const runMutation = useMutation({
    mutationFn: () => runDunning({ companyId: '' }),
    onSuccess: result => {
      setError(null)
      setRunResult(result)
    },
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
          title="Dunning Letter Templates"
          description="Escalation schedule that generates reminder letters by aging bucket."
          action={
            <div className="flex gap-2">
              <Button variant="outline" size="sm" leftIcon={<Play className="h-4 w-4" />} onClick={() => runMutation.mutate()} isLoading={runMutation.isPending}>
                Run Dunning
              </Button>
              <Button variant="primary" size="sm" leftIcon={<Mail className="h-4 w-4" />} onClick={() => setIsOpen(true)}>
                New Template
              </Button>
            </div>
          }
        />
        {runResult && (
          <CardContent>
            <div className="rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-800 dark:bg-emerald-900/20 dark:border-emerald-800 dark:text-emerald-300 p-4 text-sm">
              Generated <strong>{runResult.lettersGenerated}</strong> dunning letter(s) as of{' '}
              <strong>{formatDate(runResult.asOfDate)}</strong>.
            </div>
          </CardContent>
        )}
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : templates.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No dunning templates yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">#</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bucket</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Days Overdue</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Delivery</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Active</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {templates.map(t => (
                    <tr key={t.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-primary-600 dark:text-primary-400">{t.sequence}</td>
                      <td className="px-3 py-3">
                        <p className="font-medium text-gray-900 dark:text-white">{t.name}</p>
                        <p className="text-xs text-gray-500 dark:text-gray-400 truncate max-w-xs">{t.subject}</p>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{t.bucket}</td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">{t.minDaysOverdue}–{t.maxDaysOverdue}</td>
                      <td className="px-3 py-3">
                        <div className="flex gap-1">
                          {t.sendEmail && <Mail className="h-4 w-4 text-blue-500" />}
                          {t.sendPdf && <FileText className="h-4 w-4 text-gray-500" />}
                        </div>
                      </td>
                      <td className="px-3 py-3">{t.isActive ? <span className="text-emerald-600">Yes</span> : <span className="text-gray-400">No</span>}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <NewTemplateModal isOpen={isOpen} onClose={() => setIsOpen(false)} onSubmit={data => createMutation.mutate(data)} isSubmitting={createMutation.isPending} />
    </div>
  )
}

interface NewTemplateModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: CreateDunningTemplateRequest) => void
  isSubmitting: boolean
}

function NewTemplateModal({ isOpen, onClose, onSubmit, isSubmitting }: NewTemplateModalProps) {
  const [name, setName] = useState('')
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [sequence, setSequence] = useState(1)
  const [bucket, setBucket] = useState<DunningBucket>('Days1To30')
  const [minDays, setMinDays] = useState(1)
  const [maxDays, setMaxDays] = useState(30)
  const [sendEmail, setSendEmail] = useState(true)
  const [sendPdf, setSendPdf] = useState(false)

  const submit = () => {
    if (!name.trim() || !subject.trim()) return
    onSubmit({
      companyId: '',
      name: name.trim(),
      subject: subject.trim(),
      body: body.trim(),
      sequence,
      bucket,
      minDaysOverdue: minDays,
      maxDaysOverdue: maxDays,
      sendEmail,
      sendPdf,
    })
    setName(''); setSubject(''); setBody(''); setSequence(s => s + 1)
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="New Dunning Template">
      <div className="space-y-4">
        <Input label="Name" value={name} onChange={e => setName(e.target.value)} />
        <Input label="Subject" value={subject} onChange={e => setSubject(e.target.value)} />
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Body</label>
          <textarea value={body} onChange={e => setBody(e.target.value)} rows={3} className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm" />
        </div>
        <div className="grid grid-cols-3 gap-3">
          <Input label="Sequence" type="number" value={String(sequence)} onChange={e => setSequence(Number(e.target.value))} />
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bucket</label>
            <select value={bucket} onChange={e => setBucket(e.target.value as DunningBucket)} className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm">
              {BUCKETS.map(b => <option key={b} value={b}>{b}</option>)}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <Input label="Min Days" type="number" value={String(minDays)} onChange={e => setMinDays(Number(e.target.value))} />
            <Input label="Max Days" type="number" value={String(maxDays)} onChange={e => setMaxDays(Number(e.target.value))} />
          </div>
        </div>
        <div className="flex gap-4">
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={sendEmail} onChange={e => setSendEmail(e.target.checked)} /> Email</label>
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={sendPdf} onChange={e => setSendPdf(e.target.checked)} /> PDF</label>
        </div>
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={submit} isLoading={isSubmitting} disabled={!name.trim() || !subject.trim()}>Save</Button>
        </div>
      </div>
    </Modal>
  )
}
