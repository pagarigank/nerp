import { useMemo, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getLandedCosts, createLandedCost, companyId } from '@api/inventory'
import { getVendors } from '@api/ap'
import type { LandedCostSummary, CreateLandedCostRequest } from '@/types/inventory'
import type { Vendor } from '@/types/ap'

const costTypeOptions = [
  { value: '0', label: 'Freight' },
  { value: '1', label: 'Duty' },
  { value: '2', label: 'Insurance' },
  { value: '3', label: 'Handling' },
  { value: '4', label: 'Other' },
]

const schema = z.object({
  vendorId: z.string().min(1, 'Vendor is required'),
  costCode: z.string().min(1, 'Cost code is required'),
  description: z.string().min(1, 'Description is required'),
  costType: z.string().min(1, 'Type is required'),
  amount: z.coerce.number().positive('Amount must be > 0'),
  costDate: z.string().min(1, 'Date is required'),
  referenceNumber: z.string().optional(),
})
type Form = z.infer<typeof schema>

function fieldError(msg?: string) { return msg ? { error: msg } : {} }
function money(n: number) { return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n) }

export function LandedCostPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [showCreate, setShowCreate] = useState(false)
  const [vendorSearch, setVendorSearch] = useState('')
  const [showVendorDrop, setShowVendorDrop] = useState(false)

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: { vendorId: '', costCode: '', description: '', costType: '0', amount: 0, costDate: new Date().toISOString().slice(0, 10), referenceNumber: '' },
  })

  const { data: rows = [], isLoading } = useQuery({ queryKey: ['inventory', 'landed-costs'], queryFn: () => getLandedCosts() })
  const { data: vendors = [] } = useQuery({ queryKey: ['ap', 'vendors'], queryFn: () => getVendors() })

  const filteredVendors = useMemo(() => {
    const q = vendorSearch.trim().toLowerCase()
    if (!q) return vendors.slice(0, 10)
    return vendors.filter((v: Vendor) => v.name.toLowerCase().includes(q) || v.vendorId.toLowerCase().includes(q)).slice(0, 10)
  }, [vendors, vendorSearch])

  const selectedVendor = useMemo(() => vendors.find((v: Vendor) => v.id === watch('vendorId')), [vendors, watch('vendorId')])

  const createMut = useMutation({
    mutationFn: (d: Form) => createLandedCost({
      companyId: companyId(),
      vendorId: d.vendorId,
      costCode: d.costCode,
      description: d.description,
      costType: Number(d.costType),
      amount: d.amount,
      costDate: new Date(d.costDate).toISOString(),
      referenceNumber: d.referenceNumber || null,
    } as CreateLandedCostRequest),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['inventory', 'landed-costs'] }); setShowCreate(false); reset() },
    onError: e => setErr(getErrorMessage(e)),
  })

  return (
    <div className="space-y-6">
      {err && <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm"><AlertCircle className="h-5 w-5" /> {err}</div>}

      {/* Create Modal */}
      <Modal isOpen={showCreate} onClose={() => setShowCreate(false)} title="New Landed Cost" size="lg"
        footer={<><Button variant="secondary" onClick={() => setShowCreate(false)} disabled={createMut.isPending}>Cancel</Button>
          <Button variant="primary" onClick={handleSubmit(d => createMut.mutate(d))} isLoading={createMut.isPending}>Create</Button></>}>
        <form className="space-y-4" noValidate>
          <div className="grid grid-cols-2 gap-4">
            <Input {...register('costCode')} label="Cost Code" placeholder="e.g. FREIGHT-001" {...fieldError(errors.costCode?.message)} required />
            <Input {...register('costDate')} type="date" label="Cost Date" {...fieldError(errors.costDate?.message)} required />
            <div className="relative">
              <Input value={selectedVendor?.name ?? vendorSearch}
                onChange={e => { setVendorSearch(e.target.value); setShowVendorDrop(true); reset({ ...watch(), vendorId: '' }) }}
                onFocus={() => setShowVendorDrop(true)} onBlur={() => setTimeout(() => setShowVendorDrop(false), 200)}
                label="Vendor" placeholder="Search vendor..." {...fieldError(errors.vendorId?.message)} required />
              {showVendorDrop && filteredVendors.length > 0 && (
                <div className="absolute z-50 mt-1 w-full bg-white dark:bg-gray-800 border rounded-lg shadow-lg max-h-60 overflow-auto">
                  {filteredVendors.map((v: Vendor) => (
                    <button key={v.id} type="button" className="w-full px-3 py-2 text-left text-sm hover:bg-gray-50 dark:hover:bg-gray-700"
                      onMouseDown={() => { reset({ ...watch(), vendorId: v.id }); setVendorSearch(v.name); setShowVendorDrop(false) }}>
                      <span className="font-medium">{v.name}</span> <span className="text-gray-500 text-xs">{v.vendorId}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>
            <Select {...register('costType')} label="Cost Type" options={costTypeOptions} {...fieldError(errors.costType?.message)} required />
            <Input {...register('amount')} type="number" step="0.01" min="0.01" label="Amount" {...fieldError(errors.amount?.message)} required />
            <Input {...register('referenceNumber')} label="Reference #" placeholder="PO / Invoice #" />
          </div>
          <Textarea {...register('description')} label="Description" placeholder="Landed cost description..." rows={2} {...fieldError(errors.description?.message)} />
        </form>
      </Modal>

      {/* List */}
      <Card>
        <CardHeader title="Landed Cost" description={`${rows.length} record(s)`}
          action={<Button variant="primary" size="sm" onClick={() => { setErr(null); setVendorSearch(''); reset(); setShowCreate(true) }} leftIcon={<Plus className="h-4 w-4" />}>New Landed Cost</Button>} />
        <CardContent>
          {isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading...</p> :
            rows.length === 0 ? <p className="text-sm text-gray-500 py-8 text-center">No landed costs.</p> :
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Code</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Vendor</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Allocated</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Remaining</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                  </tr></thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {rows.map((r: LandedCostSummary) => (
                      <tr key={r.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                        <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{r.costCode}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.description}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.costType}</td>
                        <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{r.vendorId.slice(0, 8)}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{money(r.amount)}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{money(r.allocatedAmount)}</td>
                        <td className="px-3 py-3 text-right tabular-nums">{money(r.remainingAmount)}</td>
                        <td className="px-3 py-3"><Badge variant={r.status === 'FullyAllocated' ? 'success' : r.status === 'PendingAllocation' ? 'warning' : 'neutral'} size="sm" dot>{r.status}</Badge></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>}
        </CardContent>
      </Card>
    </div>
  )
}
