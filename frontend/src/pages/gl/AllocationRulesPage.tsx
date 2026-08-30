import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, Pencil, Power, PowerOff, SplitSquareVertical } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getAllocationRules,
  createAllocationRule,
  updateAllocationRule,
  addAllocationRuleLine,
  executeAllocation,
  activateAllocationRule,
  deactivateAllocationRule,
  } from '@api/gl'
import { getAccounts, getFiscalPeriods } from '@api/platform'
import type { GlAllocationRule } from '@/types/gl'
import { allocationMethodMap } from './statusMaps'

const methodOptions = [
  { value: '0', label: 'Percentage' },
  { value: '1', label: 'Fixed Amount' },
  { value: '2', label: 'Equally' },
]

const ruleSchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  description: z.string().trim().min(1, 'Description is required'),
  sourceAccountId: z.string().min(1, 'Source account is required'),
  method: z.string().min(1, 'Method is required'),
  isActive: z.boolean(),
})

type RuleForm = z.infer<typeof ruleSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

interface AddLineModalProps {
  rule: GlAllocationRule | null
  onClose: () => void
  onSave: (line: {
    targetAccountId: string
    percentage: number
    fixedAmount?: number | null
    reference?: string | null
  }) => void
  isSaving: boolean
}

function AddLineModal({ rule, onClose, onSave, isSaving }: AddLineModalProps) {
  const [targetAccountId, setTargetAccountId] = useState('')
  const [percentage, setPercentage] = useState('')
  const [fixedAmount, setFixedAmount] = useState('')
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

  if (!rule) return null

  const handleSave = () => {
    if (!targetAccountId) {
      setLocalError('Select a target account for the line.')
      return
    }
    const isEqually = rule.method === 2 || rule.method === 'Equally'
    if (isEqually) {
      setLocalError(null)
      onSave({
        targetAccountId,
        percentage: 0,
        fixedAmount: null,
        reference: reference.trim() || null,
      })
      return
    }
    if (rule.method === 1 || rule.method === 'FixedAmount') {
      const amount = Number(fixedAmount) || 0
      if (amount <= 0) {
        setLocalError('Enter a fixed amount greater than zero.')
        return
      }
      setLocalError(null)
      onSave({
        targetAccountId,
        percentage: 0,
        fixedAmount: amount,
        reference: reference.trim() || null,
      })
      return
    }
    const pct = Number(percentage) || 0
    if (pct <= 0) {
      setLocalError('Enter a percentage greater than zero.')
      return
    }
    setLocalError(null)
    onSave({
      targetAccountId,
      percentage: pct,
      fixedAmount: null,
      reference: reference.trim() || null,
    })
  }

  const isEqually = rule.method === 2 || rule.method === 'Equally'
  const isFixed = rule.method === 1 || rule.method === 'FixedAmount'

  return (
    <Modal
      isOpen={!!rule}
      onClose={onClose}
      title="Add Allocation Line"
      description={`Add a target allocation to ${rule.name}`}
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
          label="Target Account"
          placeholder="Select account..."
          options={accountOptions}
          value={targetAccountId}
          onChange={setTargetAccountId}
          required
        />
        {!isEqually && (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {isFixed ? (
              <Input
                label="Fixed Amount"
                type="number"
                step="0.01"
                min="0"
                value={fixedAmount}
                onChange={e => setFixedAmount(e.target.value)}
                placeholder="0.00"
                className="text-right tabular-nums"
                required
              />
            ) : (
              <Input
                label="Percentage (%)"
                type="number"
                step="0.01"
                min="0"
                max="100"
                value={percentage}
                onChange={e => setPercentage(e.target.value)}
                placeholder="e.g. 25.00"
                className="text-right tabular-nums"
                required
              />
            )}
            <Input
              label="Reference"
              placeholder="Optional reference"
              value={reference}
              onChange={e => setReference(e.target.value)}
            />
          </div>
        )}
        {isEqually && (
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Equal method: the source amount is divided evenly across all target accounts.
          </p>
        )}
      </div>
    </Modal>
  )
}

interface ExecuteModalProps {
  rule: GlAllocationRule | null
  onClose: () => void
  onExecute: (data: { batchNumber: string; sourceAmount: number; fiscalPeriodId: string; postingDate: string }) => void
  isSaving: boolean
  defaultBatchNumber: string
}

function ExecuteModal({ rule, onClose, onExecute, isSaving, defaultBatchNumber }: ExecuteModalProps) {
  const [batchNumber, setBatchNumber] = useState(defaultBatchNumber)
  const [sourceAmount, setSourceAmount] = useState('')
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

  if (!rule) return null

  const handleExecute = () => {
    if (!batchNumber.trim()) {
      setLocalError('Batch number is required.')
      return
    }
    const amount = Number(sourceAmount) || 0
    if (amount <= 0) {
      setLocalError('Enter a source amount greater than zero.')
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
    onExecute({
      batchNumber: batchNumber.trim(),
      sourceAmount: amount,
      fiscalPeriodId,
      postingDate: new Date(postingDate).toISOString(),
    })
  }

  return (
    <Modal
      isOpen={!!rule}
      onClose={onClose}
      title="Execute Allocation"
      description={`Generate a journal batch from ${rule.name}`}
      size="md"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleExecute} isLoading={isSaving}>
            Execute
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
        <Input
          label="Source Amount"
          type="number"
          step="0.01"
          min="0.01"
          value={sourceAmount}
          onChange={e => setSourceAmount(e.target.value)}
          placeholder="0.00"
          className="text-right tabular-nums"
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

export function AllocationRulesPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingRule, setEditingRule] = useState<GlAllocationRule | null>(null)
  const [addLineFor, setAddLineFor] = useState<GlAllocationRule | null>(null)
  const [executeFor, setExecuteFor] = useState<GlAllocationRule | null>(null)
  const [statusAction, setStatusAction] = useState<{ rule: GlAllocationRule; action: 'activate' | 'deactivate' } | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<RuleForm>({
    resolver: zodResolver(ruleSchema),
    defaultValues: {
      name: '',
      description: '',
      sourceAccountId: '',
      method: '0',
      isActive: true,
    },
  })

  const { data: rules = [], isLoading } = useQuery({
    queryKey: ['gl', 'allocationRules'],
    queryFn: () => getAllocationRules(),
  })

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

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['gl', 'allocationRules'] })
    queryClient.invalidateQueries({ queryKey: ['gl', 'journalBatches'] })
  }

  const createMutation = useMutation({
    mutationFn: createAllocationRule,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: RuleForm }) =>
      updateAllocationRule(id, {
        name: data.name,
        description: data.description,
        sourceAccountId: data.sourceAccountId,
        method: Number(data.method),
        isActive: data.isActive,
      }),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const addLineMutation = useMutation({
    mutationFn: ({ id, line }: { id: string; line: Parameters<typeof addAllocationRuleLine>[1] }) =>
      addAllocationRuleLine(id, line),
    onSuccess: () => {
      invalidate()
      setAddLineFor(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const executeMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: { batchNumber: string; sourceAmount: number; fiscalPeriodId: string; postingDate: string } }) =>
      executeAllocation(id, data),
    onSuccess: () => {
      invalidate()
      setExecuteFor(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const activateMutation = useMutation({
    mutationFn: activateAllocationRule,
    onSuccess: () => {
      invalidate()
      setStatusAction(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deactivateMutation = useMutation({
    mutationFn: deactivateAllocationRule,
    onSuccess: () => {
      invalidate()
      setStatusAction(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingRule(null)
    setFormError(null)
    reset({
      name: '',
      description: '',
      sourceAccountId: '',
      method: '0',
      isActive: true,
    })
    setIsModalOpen(true)
  }

  const openEditForm = (rule: GlAllocationRule) => {
    setEditingRule(rule)
    setFormError(null)
    reset({
      name: rule.name,
      description: rule.description,
      sourceAccountId: rule.sourceAccountId,
      method: String(rule.method),
      isActive: rule.isActive,
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingRule(null)
    setFormError(null)
  }

  const onSubmit = (data: RuleForm) => {
    setFormError(null)
    if (editingRule) {
      updateMutation.mutate({ id: editingRule.id, data })
      return
    }
    createMutation.mutate({
      companyId: currentCompanyId(),
      name: data.name,
      description: data.description,
      sourceAccountId: data.sourceAccountId,
      method: Number(data.method),
      isActive: data.isActive,
    })
  }

  const selectedSourceAccountId = watch('sourceAccountId')
  const sourceName = useMemo(() => {
    const account = accounts.find(a => a.id === selectedSourceAccountId)
    return account ? `${account.accountNumber} - ${account.description}` : ''
  }, [accounts, selectedSourceAccountId])

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
          title="Allocation Rules"
          description={`${rules.length} rule(s) for distributing amounts across accounts`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Rule
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : rules.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No allocation rules yet. Create one to distribute source amounts.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Source Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Method</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Targets</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rules.map(rule => (
                    <tr key={rule.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3">
                        <p className="font-medium text-gray-900 dark:text-white">{rule.name}</p>
                        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{rule.description}</p>
                      </td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">
                        {rule.sourceAccountId}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {allocationMethodMap[String(rule.method)] ?? rule.method}
                      </td>
                      <td className="px-3 py-3 text-right tabular-nums text-gray-700 dark:text-gray-300">
                        {rule.lines.length}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={rule.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {rule.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Execute ${rule.name}`}
                            onClick={() => {
                              setFormError(null)
                              setExecuteFor(rule)
                            }}
                          >
                            <SplitSquareVertical className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${rule.name}`}
                            onClick={() => openEditForm(rule)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Add line to ${rule.name}`}
                            onClick={() => {
                              setFormError(null)
                              setAddLineFor(rule)
                            }}
                          >
                            <Plus className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          {rule.isActive ? (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                              aria-label={`Deactivate ${rule.name}`}
                              onClick={() => setStatusAction({ rule, action: 'deactivate' })}
                            >
                              <PowerOff className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
                          ) : (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20"
                              aria-label={`Activate ${rule.name}`}
                              onClick={() => setStatusAction({ rule, action: 'activate' })}
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
        title={editingRule ? 'Edit Allocation Rule' : 'New Allocation Rule'}
        description={editingRule ? `Update ${editingRule.name}` : 'Create a rule to distribute amounts across accounts.'}
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
              {editingRule ? 'Save Changes' : 'Create Rule'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input
            {...register('name')}
            label="Name"
            placeholder="e.g. Overhead Allocation"
            {...fieldError(errors.name?.message)}
            required
          />
          <Input
            {...register('description')}
            label="Description"
            placeholder="e.g. Distribute overhead to departments"
            {...fieldError(errors.description?.message)}
            required
          />
          <Combobox
            label="Source Account"
            placeholder="Select source account..."
            options={accountOptions}
            value={watch('sourceAccountId')}
            onChange={value => setValue('sourceAccountId', value, { shouldValidate: true })}
            required
          />
          {sourceName && (
            <p className="text-xs text-gray-500 dark:text-gray-400 -mt-2">Distributing from: {sourceName}</p>
          )}
          <Select
            {...register('method')}
            label="Method"
            options={methodOptions}
            {...fieldError(errors.method?.message)}
          />
          <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
            <input type="checkbox" {...register('isActive')} className="h-4 w-4 rounded border-gray-300 text-primary-600" />
            Active
          </label>
          <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
            <SplitSquareVertical className="h-4 w-4" aria-hidden="true" />
            Add target allocation lines after creating the rule.
          </div>
        </form>
      </Modal>

      <AddLineModal
        rule={addLineFor}
        onClose={() => setAddLineFor(null)}
        onSave={line => addLineMutation.mutate({ id: addLineFor!.id, line })}
        isSaving={addLineMutation.isPending}
      />

      <ExecuteModal
        rule={executeFor}
        onClose={() => setExecuteFor(null)}
        onExecute={data => executeMutation.mutate({ id: executeFor!.id, data })}
        isSaving={executeMutation.isPending}
        defaultBatchNumber={`GL-${rules.length + 1}`}
      />

      <ConfirmDialog
        isOpen={!!statusAction}
        onClose={() => setStatusAction(null)}
        onConfirm={() =>
          statusAction?.action === 'activate'
            ? activateMutation.mutate(statusAction.rule.id)
            : deactivateMutation.mutate(statusAction?.rule.id ?? '')
        }
        title={statusAction?.action === 'activate' ? 'Activate Rule' : 'Deactivate Rule'}
        message={
          statusAction?.action === 'activate'
            ? `Activate ${statusAction?.rule.name}? It will be eligible for execution.`
            : `Deactivate ${statusAction?.rule.name}? It will stop being eligible for execution.`
        }
        confirmText={statusAction?.action === 'activate' ? 'Activate' : 'Deactivate'}
        variant={statusAction?.action === 'activate' ? 'primary' : 'danger'}
        isLoading={activateMutation.isPending || deactivateMutation.isPending}
      />
    </div>
  )
}
