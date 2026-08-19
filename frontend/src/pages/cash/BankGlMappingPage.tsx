import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle, Link2, Check } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Combobox } from '@components/ui/Combobox'
import { Modal } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getAccounts } from '@api/platform'
import {
  getBankGlMappings,
  createBankGlMapping,
  updateBankGlMapping,
  getBankAccounts,
  DEMO_COMPANY_ID,
} from '@api/cash'
import type { BankGlMapping } from '@/types/cash'

const mappingSchema = z.object({
  bankAccountId: z.string().min(1, 'Select a bank account'),
  glAccountId: z.string().min(1, 'Select a GL account'),
  isDefault: z.boolean(),
})

type MappingForm = z.infer<typeof mappingSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function BankGlMappingPage() {
  const queryClient = useQueryClient()
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<MappingForm>({
    resolver: zodResolver(mappingSchema),
    defaultValues: {
      bankAccountId: '',
      glAccountId: '',
      isDefault: true,
    },
  })

  const { data: mappings = [], isLoading } = useQuery({
    queryKey: ['cash', 'bank-gl-mappings'],
    queryFn: () => getBankGlMappings(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const { data: glAccounts = [] } = useQuery({
    queryKey: ['platform', 'accounts'],
    queryFn: () => getAccounts(),
  })

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountCode} - ${a.accountName}` })),
    [accounts]
  )

  const glAccountOptions = useMemo(
    () => glAccounts.map(a => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })),
    [glAccounts]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'bank-gl-mappings'] })
  }

  const createMutation = useMutation({
    mutationFn: (data: MappingForm) =>
      createBankGlMapping({
        companyId: DEMO_COMPANY_ID,
        bankAccountId: data.bankAccountId,
        glAccountId: data.glAccountId,
        isDefault: data.isDefault,
      }),
    onSuccess: () => {
      invalidate()
      closeForm()
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, glAccountId, isDefault }: { id: string; glAccountId: string; isDefault: boolean }) =>
      updateBankGlMapping(id, { glAccountId, isDefault }),
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const openForm = () => {
    setFormError(null)
    reset({ bankAccountId: '', glAccountId: '', isDefault: true })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
  }

  const onSubmit = (data: MappingForm) => {
    setFormError(null)
    createMutation.mutate(data)
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
          title="Bank to GL Cash Account Mapping"
          description={`${mappings.length} mapping(s) configured`}
          action={
            <Button variant="primary" size="sm" onClick={openForm} leftIcon={<Link2 className="h-4 w-4" />}>
              Add Mapping
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={4} />
          ) : mappings.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              No GL mappings configured. Map each bank account to its corresponding cash GL account.
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bank Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">GL Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Default</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Change GL</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {mappings.map((m: BankGlMapping) => {
                    const ba = accounts.find(a => a.id === m.bankAccountId)
                    const gl = glAccounts.find(a => a.id === m.glAccountId)
                    return (
                      <tr key={m.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        <td className="px-3 py-3 text-gray-900 dark:text-white">
                          {ba ? `${ba.accountCode} - ${ba.accountName}` : m.bankAccountId}
                        </td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                          {gl ? `${gl.accountNumber} - ${gl.description}` : m.glAccountId}
                        </td>
                        <td className="px-3 py-3">
                          {m.isDefault ? (
                            <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400">
                              <Check className="h-4 w-4" /> Default
                            </span>
                          ) : (
                            <span className="text-gray-400">—</span>
                          )}
                        </td>
                        <td className="px-3 py-3">
                          <div className="flex items-center justify-end">
                            <Combobox
                              className="w-56"
                              placeholder="Change GL account..."
                              options={glAccountOptions}
                              value={m.glAccountId}
                              onChange={value =>
                                updateMutation.mutate({ id: m.id, glAccountId: value, isDefault: m.isDefault })
                              }
                            />
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title="Map Bank Account to GL"
        description="Link a bank account to its corresponding cash GL account for reconciliation posting."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={createMutation.isPending}>
              Create Mapping
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="sm:col-span-2">
              <Combobox
                label="Bank Account"
                placeholder="Select bank account..."
                options={accountOptions}
                value={watch('bankAccountId')}
                onChange={value => setValue('bankAccountId', value, { shouldValidate: true })}
                required
              />
            </div>
            <div className="sm:col-span-2">
              <Combobox
                label="GL Cash Account"
                placeholder="Select GL account..."
                options={glAccountOptions}
                value={watch('glAccountId')}
                onChange={value => setValue('glAccountId', value, { shouldValidate: true })}
                required
              />
            </div>
            <div className="sm:col-span-2">
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                <input
                  type="checkbox"
                  {...register('isDefault')}
                  className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                />
                Default mapping for this bank account
              </label>
            </div>
          </div>
        </form>
      </Modal>
    </div>
  )
}
