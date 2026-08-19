import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Search, Pencil, Trash2, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getPaymentTerms,
  createPaymentTerm,
  updatePaymentTerm,
  activatePaymentTerm,
  deactivatePaymentTerm,
} from '@api/ap'
import type { PaymentTerm, UpdatePaymentTermRequest } from '@/types/ap'

const paymentTermSchema = z.object({
  name: z.string().trim().min(1, 'Name is required'),
  dueDays: z.coerce.number().int('Must be a whole number').min(0, 'Cannot be negative'),
  discountDays: z.coerce.number().int('Must be a whole number').min(0, 'Cannot be negative'),
  discountPercent: z.coerce.number().min(0, 'Cannot be negative').max(100, 'Cannot exceed 100'),
})

type PaymentTermForm = z.infer<typeof paymentTermSchema>

const defaultValues: PaymentTermForm = {
  name: '',
  dueDays: 30,
  discountDays: 0,
  discountPercent: 0,
}

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function PaymentTermsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [editingTerm, setEditingTerm] = useState<PaymentTerm | null>(null)
  const [termToDeactivate, setTermToDeactivate] = useState<PaymentTerm | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PaymentTermForm>({
    resolver: zodResolver(paymentTermSchema),
    defaultValues,
  })

  const { data: terms = [], isLoading } = useQuery({
    queryKey: ['ap', 'paymentTerms'],
    queryFn: () => getPaymentTerms(),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['ap', 'paymentTerms'] })
  }

  const createMutation = useMutation({
    mutationFn: createPaymentTerm,
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdatePaymentTermRequest }) => updatePaymentTerm(id, data),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivatePaymentTerm(id),
    onSuccess: invalidate,
    onError: err => setFormError(getErrorMessage(err)),
  })

  const activateMutation = useMutation({
    mutationFn: (id: string) => activatePaymentTerm(id),
    onSuccess: invalidate,
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openCreateForm = () => {
    setEditingTerm(null)
    setFormError(null)
    reset(defaultValues)
    setIsModalOpen(true)
  }

  const openEditForm = (term: PaymentTerm) => {
    setEditingTerm(term)
    setFormError(null)
    reset({
      name: term.name,
      dueDays: term.dueDays,
      discountDays: term.discountDays,
      discountPercent: term.discountPercent,
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setEditingTerm(null)
    setFormError(null)
  }

  const onSubmit = (data: PaymentTermForm) => {
    setFormError(null)
    const payload = {
      name: data.name,
      dueDays: data.dueDays,
      discountDays: data.discountDays,
      discountPercent: data.discountPercent,
    }
    if (editingTerm) {
      updateMutation.mutate({ id: editingTerm.id, data: payload })
      return
    }
    createMutation.mutate(payload)
  }

  const filteredTerms = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return terms
    return terms.filter(t => t.name.toLowerCase().includes(q))
  }, [terms, search])

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
          title="Payment Terms"
          description={`${terms.length} term(s) on file`}
          action={
            <Button variant="primary" size="sm" onClick={openCreateForm} leftIcon={<Plus className="h-4 w-4" />}>
              New Payment Term
            </Button>
          }
        />
        <CardContent>
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by name..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search payment terms"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={5} />
          ) : filteredTerms.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search ? 'No payment terms match your search.' : 'No payment terms yet. Create your first term.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Terms</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Discount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredTerms.map(term => (
                    <tr key={term.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{term.name}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {term.dueDays === 0 ? 'Due on receipt' : `Net ${term.dueDays}`}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {term.discountPercent > 0 ? (
                          <span>
                            {term.discountPercent}% {term.discountDays} day(s)
                          </span>
                        ) : (
                          <span className="text-gray-400 dark:text-gray-500">—</span>
                        )}
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={term.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {term.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`Edit ${term.name}`}
                            onClick={() => openEditForm(term)}
                          >
                            <Pencil className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          {term.isActive ? (
                            <IconButton
                              size="sm"
                              variant="ghost"
                              className="text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/20"
                              aria-label={`Deactivate ${term.name}`}
                              onClick={() => setTermToDeactivate(term)}
                            >
                              <Trash2 className="h-4 w-4" aria-hidden="true" />
                            </IconButton>
                          ) : (
                            <Button size="sm" variant="outline" onClick={() => activateMutation.mutate(term.id)}>
                              Reactivate
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

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title={editingTerm ? 'Edit Payment Term' : 'New Payment Term'}
        description={editingTerm ? `Update ${editingTerm.name}` : 'Define payment terms used on vendor invoices'}
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
              {editingTerm ? 'Save Changes' : 'Create Payment Term'}
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <Input
            {...register('name')}
            label="Name"
            placeholder="e.g. Net 30"
            {...fieldError(errors.name?.message)}
            required
          />
          <Input
            {...register('dueDays')}
            type="number"
            step="1"
            min="0"
            label="Due in (days)"
            hint="Number of days from the invoice date until payment is due. 0 = due on receipt."
            {...fieldError(errors.dueDays?.message)}
            required
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              {...register('discountDays')}
              type="number"
              step="1"
              min="0"
              label="Discount Window (days)"
              hint="Days within which the early-payment discount applies"
              {...fieldError(errors.discountDays?.message)}
              required
            />
            <Input
              {...register('discountPercent')}
              type="number"
              step="0.01"
              min="0"
              max="100"
              label="Discount Percent (%)"
              hint="e.g. 2 for 2/10 net 30"
              {...fieldError(errors.discountPercent?.message)}
              required
            />
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!termToDeactivate}
        onClose={() => setTermToDeactivate(null)}
        onConfirm={() => termToDeactivate && deactivateMutation.mutate(termToDeactivate.id)}
        title="Deactivate Payment Term"
        message={
          termToDeactivate
            ? `Are you sure you want to deactivate "${termToDeactivate.name}"? Existing invoices are unaffected.`
            : ''
        }
        confirmText="Deactivate"
        variant="danger"
        isLoading={deactivateMutation.isPending}
      />
    </div>
  )
}
