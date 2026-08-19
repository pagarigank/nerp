import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, Pencil, Power, PowerOff, Wand2, CalendarClock } from 'lucide-react'
import { formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getRecurringTemplates,
  createRecurringTemplate,
  updateRecurringTemplate,
  addRecurringTemplateLine,
  generateFromRecurring,
  activateRecurringTemplate,
  deactivateRecurringTemplate,
  DEMO_COMPANY_ID,
} from '@api/gl'
import { getAccounts, getFiscalPeriods } from '@api/platform'
import type { GlRecurringTemplate } from '@/types/gl'
import { recurringFrequencyMap } from './statusMaps'

const frequencyOptions = [
  { value: '0', label: 'Monthly' },
  { value: '1', label: 'Quarterly' },
  { value: '2', label: 'Semi-Annually' },
  { value: '3', label: 'Annually' },
  { value: '4', label: 'Custom' },
]

const templateSchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  description: z.string().trim().min(1, 'Description is required'),
  frequency: z.string().min(1, 'Frequency is required'),
  nextRunDate: z.string().min(1, 'Next run date is required'),
  isActive: z.boolean(),
})

type TemplateForm = z.infer<typeof templateSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

interface AddLineModalProps {
  template: GlRecurringTemplate | null
  onClose: () => void
  onSave: (line: {
    accountId: string
    fixedDebit?: number | null
    fixedCredit?: number | null
    variablePct?: number | null
    reference?: string | null
  }) => void
  isSaving: boolean
}

function AddLineModal({ template, onClose, onSave, isSaving }: AddLineModalProps) {
  const [accountId, setAccountId] = useState('')
  const [fixedDebit, setFixedDebit] = useState('')
  const [fixedCredit, setFixedCredit] = useState('')
  const [variablePct, setVariablePct] = useState('')
  const [reference, setReference] = useState('')
  const [localError, setLocalError] = useState<string | null>(null)

  const { data: accounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const accountOptions = useMemo(
    () =>
      accounts.map(a => ({
        value: a.id,
        label: `${a.accountNumber} - ${a.description}`,
      })),
    [accounts]
  )

  if (!template) return null

  const handleSave = () => {
    if (!accountId) {
      setLocalError('Select an account for the line.')
      return
    }
    const d = Number(fixedDebit) || 0
    const c = Number(fixedCredit) || 0
    const pct = Number(variablePct) || 0
    if (d <= 0 && c <= 0 && pct <= 0) {
      setLocalError('Enter a fixed debit, fixed credit, or variable percentage.')
      return
    }
    if (d > 0 && c > 0) {
      setLocalError('A line can have either a fixed debit or a fixed credit, not both.')
      return
    }
    setLocalError(null)
    onSave({
      accountId,
      fixedDebit: d > 0 ? d : null,
      fixedCredit: c > 0 ? c : null,
      variablePct: pct > 0 ? pct : null,
      reference: reference.trim() || null,
    })
  }

  return (
    <Modal
      isOpen={!!template}
      onClose={onClose}
      title="Add Template Line"
      description={`Add a line to ${template.name}`}
      size="md"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleSave} isLoading={isSaving}>
            Add Line
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        {localError && (
          <div
            className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm"
            role="alert"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
            <span>{localError}</span>
          </div>
        )}
        <Combobox
          label="Account"
          placeholder="Select account..."
          options={accountOptions}
          value={accountId}
          onChange={setAccountId}
          required
        />
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="Fixed Debit"
            type="number"
            step="0.01"
            min="0"
            value={fixedDebit}
            onChange={e => setFixedDebit(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
          />
          <Input
            label="Fixed Credit"
            type="number"
            step="0.01"
            min="0"
            value={fixedCredit}
            onChange={e => setFixedCredit(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
          />
          <Input
            label="Variable %"
            type="number"
            step="0.01"
            min="0"
            value={variablePct}
            onChange={e => setVariablePct(e.target.value)}
            placeholder="e.g. 5.00"
            className="text-right tabular-nums"
            hint="Percentage of batch total for this line"
          />
          <Input
            label="Reference"
            placeholder="Optional reference"
            value={reference}
            onChange={e => setReference(e.target.value)}
          />
        </div>
      </div>
    </Modal>
  )
}

interface GenerateModalProps {
  template: GlRecurringTemplate | null
  onClose: () => void
  onGenerate: (data: { batchNumber: string; fiscalPeriodId: string; postingDate: string }) => void
  isSaving: boolean
  defaultBatchNumber: string
}

function GenerateModal({ template, onClose, onGenerate, isSaving, defaultBatchNumber }: GenerateModalProps) {
  const [batchNumber, setBatchNumber] = useState(defaultBatchNumber)
  const [fiscalPeriodId, setFiscalPeriodId] = useState('')
  const [postingDate, setPostingDate] = useState(new Date().toISOString().slice(0, 10))
  const [localError, setLocalError] = useState<string | null>(null)

  const { data: periods = [] } = useQuery({
    queryKey: ['platform', 'fiscalPeriods'],
    queryFn: () => getFiscalPeriods(),
  })

  const periodOptions = useMemo(
    () =>
      periods.map(p => ({
        value: p.id,
        label: `P${p.periodNumber} - ${p.description}`,
      })),
    [periods]
  )

  if (!template) return null

  const handleGenerate = () => {
    if (!batchNumber.trim()) {
      setLocalError('Batch number is required.')
      return
    }
    if (!fiscalPeriodId) {
      setLocalError('Select a fiscal period.')
      return
    }
    if (!postingDate) {
      setLocalError('Posting date is required.')
      return
    }
    setLocalError(null)
    onGenerate({
      batchNumber: batchNumber.trim(),
      fiscalPeriodId,
      postingDate: new Date(postingDate).toISOString(),
    })
  }

  return (
    <Modal
      isOpen={!!template}
      onClose={onClose}
      title="Generate Journal Batch"
      description={`Create a journal batch from ${template.name}`}
      size="md"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleGenerate} isLoading={isSaving}>
            Generate
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        {localError && (
          <div
            className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm"
            role="alert"
          >
            <AlertCircle className="h-4 w-4 flex-shrink-0" aria-hidden="true" />
            <span>{localError}</span>
          </div>
        )}
        <Input
          label="Batch Number"
          value={batchNumber}
          onChange={e => setBatchNumber(e.target.value)}
          required
        />
        <Select
          label="Fiscal Period"
          placeholder="Select fiscal period..."
          options={periodOptions}
          value={fiscalPeriodId}
          onChange={e => setFiscalPeriodId(e.target.value)}
          required
        />
        <Input
          label="Posting Date"
          type="date"
          value={postingDate}
          onChange={e => setPostingDate(e.target.value)}
          required
        />
      </div>
    </Modal>
  )
}

export function RecurringTemplatesPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingTemplate, setEditingTemplate] = useState<GlRecurringTemplate | null>(null)
  const [addLineFor, setAddLineFor] = useState<GlRecurringTemplate | null>(null)
  const [generateFor, setGenerateFor] = useState<GlRecurringTemplate | null>(null)
  const [statusAction, setStatusAction] = useState<{ template: GlRecurringTemplate; action: 'activate' | 'deactivate' } | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<TemplateForm>({
    resolver: zodResolver(templateSchema),
    defaultValues: {
      name: '',
      description: '',
      frequency: '0',
      nextRunDate: new Date().toISOString().slice(0, 10),
      isActive: true,
    },
  })

  const { data: templates = [], isLoading } = useQuery({
    queryKey: ['gl', 'recurringTemplates'],
    queryFn: () => getRecurringTemplates(),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['gl', 'recurringTemplates'] })
    queryClient.invalidateQueries({ queryKey: ['gl', 'journalBatches'] })
  }

  const createMutation = useMutation({
    mutationFn: createRecurringTemplate,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: Omit<TemplateForm, 'isActive'> & { isActive: boolean } }) =>
      updateRecurringTemplate(id, {
        name: data.name,
        description: data.description,
        frequency: Number(data.frequency),
        nextRunDate: new Date(data.nextRunDate).toISOString(),
        isActive: data.isActive,
      }),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const addLineMutation = useMutation({
    mutationFn: ({ id, line }: { id: string; line: Parameters<typeof addRecurringTemplateLine>[1] }) =>
      addRecurringTemplateLine(id, line),
    onSuccess: () => {
      invalidate()
      setAddLineFor(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const generateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { batchNumber: string; fiscalPeriodId: string; postingDate: string } }) =>
      generateFromRecurring(id, data),
    onSuccess: () => {
      invalidate()
      setGenerateFor(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const activateMutation = useMutation({
    mutationFn: activateRecurringTemplate,
    onSuccess: () => {
      invalidate()
      setStatusAction(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deactivateMutation = useMutation({
    mutationFn: deactivateRecurringTemplate,
    onSuccess: () => {
      invalidate()
      setStatusAction(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingTemplate(null)
    setFormError(null)
    reset({
      name: '',
      description: '',
      frequency: '0',
      nextRunDate: new Date().toISOString().slice(0, 10),
      isActive: true,
    })
    setIsModalOpen(true)
  }

  const openEditForm = (template: GlRecurringTemplate) => {
    setEditingTemplate(template)
    setFormError(null)
    reset({
      name: template.name,
      description: template.description,
      frequency: String(template.frequency),
      nextRunDate: template.nextRunDate.slice(0, 10),
      isActive: template.isActive,
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingTemplate(null)
    setFormError(null)
  }

  const onSubmit = (data: TemplateForm) => {
    setFormError(null)
    if (editingTemplate) {
      updateMutation.mutate({ id: editingTemplate.id, data })
      return
    }
    createMutation.mutate({
      companyId: DEMO_COMPANY_ID,
      name: data.name,
      description: data.description,
      frequency: Number(data.frequency),
      nextRunDate: new Date(data.nextRunDate).toISOString(),
      isActive: data.isActive,
    })
  }

  return (
    <div className="space-y-6">
      {formError && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Recurring Templates"
          description={`${templates.length} template(s) for scheduled journal entries`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Template
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : templates.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No recurring templates yet. Create one to schedule journal entries.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Frequency</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Next Run</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Lines</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {templates.map(template => (
                    <tr key={template.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3">
                        <p className="font-medium text-gray-900 dark:text-white">{template.name}</p>
                        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{template.description}</p>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {recurringFrequencyMap[String(template.frequency)] ?? template.frequency}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(template.nextRunDate)}</td>
                      <td className="px-3 py-3 text-right tabular-nums text-gray-700 dark:text-gray-300">
                        {template.lines.length}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={template.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {template.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Generate batch from ${template.name}`}
                            onClick={() => {
                              setFormError(null)
                              setGenerateFor(template)
                            }}
                          >
                            <CalendarClock className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${template.name}`}
                            onClick={() => openEditForm(template)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Add line to ${template.name}`}
                            onClick={() => {
                              setFormError(null)
                              setAddLineFor(template)
                            }}
                          >
                            <Plus className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          {template.isActive ? (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                              aria-label={`Deactivate ${template.name}`}
                              onClick={() => setStatusAction({ template, action: 'deactivate' })}
                            >
                              <PowerOff className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
                          ) : (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20"
                              aria-label={`Activate ${template.name}`}
                              onClick={() => setStatusAction({ template, action: 'activate' })}
                            >
                              <Power className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
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

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title={editingTemplate ? 'Edit Recurring Template' : 'New Recurring Template'}
        description={editingTemplate ? `Update ${editingTemplate.name}` : 'Create a template for scheduled journal entries.'}
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending || updateMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={handleSubmit(onSubmit)}
              isLoading={createMutation.isPending || updateMutation.isPending}
            >
              {editingTemplate ? 'Save Changes' : 'Create Template'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input
            {...register('name')}
            label="Name"
            placeholder="e.g. Monthly Rent Accrual"
            {...fieldError(errors.name?.message)}
            required
          />
          <Input
            {...register('description')}
            label="Description"
            placeholder="e.g. Recurring rent expense accrual"
            {...fieldError(errors.description?.message)}
            required
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Select
              {...register('frequency')}
              label="Frequency"
              options={frequencyOptions}
              {...fieldError(errors.frequency?.message)}
            />
            <Input
              {...register('nextRunDate')}
              type="date"
              label="Next Run Date"
              {...fieldError(errors.nextRunDate?.message)}
              required
            />
          </div>
          <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
            <input type="checkbox" {...register('isActive')} className="h-4 w-4 rounded border-gray-300 text-primary-600" />
            Active
          </label>
          <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
            <Wand2 className="h-4 w-4" aria-hidden="true" />
            Add template lines after creating the template.
          </div>
        </form>
      </Modal>

      <AddLineModal
        template={addLineFor}
        onClose={() => setAddLineFor(null)}
        onSave={line => addLineMutation.mutate({ id: addLineFor!.id, line })}
        isSaving={addLineMutation.isPending}
      />

      <GenerateModal
        template={generateFor}
        onClose={() => setGenerateFor(null)}
        onGenerate={data => generateMutation.mutate({ id: generateFor!.id, data })}
        isSaving={generateMutation.isPending}
        defaultBatchNumber={`GL-${templates.length + 1}`}
      />

      <ConfirmDialog
        isOpen={!!statusAction}
        onClose={() => setStatusAction(null)}
        onConfirm={() =>
          statusAction?.action === 'activate'
            ? activateMutation.mutate(statusAction.template.id)
            : deactivateMutation.mutate(statusAction?.template.id ?? '')
        }
        title={statusAction?.action === 'activate' ? 'Activate Template' : 'Deactivate Template'}
        message={
          statusAction?.action === 'activate'
            ? `Activate ${statusAction?.template.name}? It will be eligible for scheduled generation.`
            : `Deactivate ${statusAction?.template.name}? It will stop being eligible for scheduled generation.`
        }
        confirmText={statusAction?.action === 'activate' ? 'Activate' : 'Deactivate'}
        variant={statusAction?.action === 'activate' ? 'primary' : 'danger'}
        isLoading={activateMutation.isPending || deactivateMutation.isPending}
      />
    </div>
  )
}
