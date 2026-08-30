import { currentCompanyId } from '@/api/company'
import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, Trash2 } from 'lucide-react'
import { formatCurrency } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getBudgets, createBudget, addBudgetLine, deleteBudget} from '@api/gl'
import { getAccounts, getFiscalYears } from '@api/platform'
import type { GlBudget } from '@/types/gl'
import { budgetTypeMap } from './statusMaps'

const budgetTypeOptions = [
  { value: '0', label: 'Original' },
  { value: '1', label: 'Revised' },
  { value: '2', label: 'Encumbrance' },
]

const periodNumberOptions = Array.from({ length: 13 }, (_, i) => ({
  value: String(i + 1),
  label: `Period ${i + 1}`,
}))

const budgetSchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  description: z.string().trim().min(1, 'Description is required'),
  fiscalYearId: z.string().min(1, 'Fiscal year is required'),
  budgetType: z.string().min(1, 'Budget type is required'),
})

type BudgetForm = z.infer<typeof budgetSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

interface AddLineModalProps {
  budget: GlBudget | null
  onClose: () => void
  onSave: (line: { accountId: string; periodNumber: number; amount: number }) => void
  isSaving: boolean
}

function AddLineModal({ budget, onClose, onSave, isSaving }: AddLineModalProps) {
  const [accountId, setAccountId] = useState('')
  const [periodNumber, setPeriodNumber] = useState('1')
  const [amount, setAmount] = useState('')
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

  if (!budget) return null

  const handleSave = () => {
    if (!accountId) {
      setLocalError('Select an account for the line.')
      return
    }
    const amt = Number(amount) || 0
    if (amt < 0) {
      setLocalError('Amount cannot be negative.')
      return
    }
    setLocalError(null)
    onSave({
      accountId,
      periodNumber: Number(periodNumber),
      amount: amt,
    })
  }

  return (
    <Modal
      isOpen={!!budget}
      onClose={onClose}
      title="Add Budget Line"
      description={`Add a period budget to ${budget.name}`}
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
          <Select
            label="Period"
            options={periodNumberOptions}
            value={periodNumber}
            onChange={e => setPeriodNumber(e.target.value)}
          />
          <Input
            label="Amount"
            type="number"
            step="0.01"
            min="0"
            value={amount}
            onChange={e => setAmount(e.target.value)}
            placeholder="0.00"
            className="text-right tabular-nums"
            required
          />
        </div>
      </div>
    </Modal>
  )
}

export function BudgetsPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [addLineFor, setAddLineFor] = useState<GlBudget | null>(null)
  const [budgetToDelete, setBudgetToDelete] = useState<GlBudget | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<BudgetForm>({
    resolver: zodResolver(budgetSchema),
    defaultValues: {
      name: '',
      description: '',
      fiscalYearId: '',
      budgetType: '0',
    },
  })

  const { data: budgets = [], isLoading } = useQuery({
    queryKey: ['gl', 'budgets'],
    queryFn: () => getBudgets(),
  })

  const { data: fiscalYears = [] } = useQuery({
    queryKey: ['platform', 'fiscalYears'],
    queryFn: () => getFiscalYears(),
  })

  const fiscalYearOptions = useMemo(
    () =>
      fiscalYears.map(fy => ({
        value: fy.id,
        label: `${fy.year} - ${fy.description}`,
      })),
    [fiscalYears]
  )

  const fiscalYearById = useMemo(() => new Map(fiscalYears.map(fy => [fy.id, fy])), [fiscalYears])

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['gl', 'budgets'] })
  }

  const createMutation = useMutation({
    mutationFn: createBudget,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const addLineMutation = useMutation({
    mutationFn: ({ id, line }: { id: string; line: { accountId: string; periodNumber: number; amount: number } }) =>
      addBudgetLine(id, line),
    onSuccess: () => {
      invalidate()
      setAddLineFor(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteBudget,
    onSuccess: () => {
      invalidate()
      setBudgetToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setFormError(null)
    reset({
      name: '',
      description: '',
      fiscalYearId: fiscalYears[0]?.id ?? '',
      budgetType: '0',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: BudgetForm) => {
    setFormError(null)
    createMutation.mutate({
      companyId: currentCompanyId(),
      fiscalYearId: data.fiscalYearId,
      name: data.name,
      description: data.description,
      budgetType: Number(data.budgetType),
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
          title="Budgets"
          description={`${budgets.length} budget(s)`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Budget
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : budgets.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No budgets yet. Create a budget to plan and compare against actuals.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Fiscal Year</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Lines</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Total</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {budgets.map(budget => (
                    <tr key={budget.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3">
                        <p className="font-medium text-gray-900 dark:text-white">{budget.name}</p>
                        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{budget.description}</p>
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {fiscalYearById.get(budget.fiscalYearId)?.year ?? '—'}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {budgetTypeMap[String(budget.budgetType)] ?? budget.budgetType}
                      </td>
                      <td className="px-3 py-3 text-right tabular-nums text-gray-700 dark:text-gray-300">
                        {budget.lines.length}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(budget.totalAmount)}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={budget.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {budget.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Add line to ${budget.name}`}
                            onClick={() => {
                              setFormError(null)
                              setAddLineFor(budget)
                            }}
                          >
                            <Plus className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Delete ${budget.name}`}
                            onClick={() => setBudgetToDelete(budget)}
                          >
                            <Trash2 className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
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
        title="New Budget"
        description="Create a budget for a fiscal year. Add period lines after creation."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Create Budget
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input
            {...register('name')}
            label="Name"
            placeholder="e.g. FY2027 Operating Budget"
            {...fieldError(errors.name?.message)}
            required
          />
          <Input
            {...register('description')}
            label="Description"
            placeholder="e.g. Annual operating plan"
            {...fieldError(errors.description?.message)}
            required
          />
          <Select
            {...register('fiscalYearId')}
            label="Fiscal Year"
            placeholder="Select fiscal year..."
            options={fiscalYearOptions}
            {...fieldError(errors.fiscalYearId?.message)}
            required
          />
          <Select
            {...register('budgetType')}
            label="Budget Type"
            options={budgetTypeOptions}
            {...fieldError(errors.budgetType?.message)}
          />
        </form>
      </Modal>

      <AddLineModal
        budget={addLineFor}
        onClose={() => setAddLineFor(null)}
        onSave={line => addLineMutation.mutate({ id: addLineFor!.id, line })}
        isSaving={addLineMutation.isPending}
      />

      <ConfirmDialog
        isOpen={!!budgetToDelete}
        onClose={() => setBudgetToDelete(null)}
        onConfirm={() => deleteMutation.mutate(budgetToDelete!.id)}
        title="Delete Budget"
        message={`Deactivate ${budgetToDelete?.name}? The budget will be marked inactive and hidden from future use.`}
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
