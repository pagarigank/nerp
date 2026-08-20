import { useState } from 'react'
import { Plus, DollarSign } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/orderManagement'
import { getContractLines, addContractLine, generateInvoice, getWipSchedule } from '@api/projectAccounting'
import { useQuery, useMutation } from '@tanstack/react-query'
import { ProjectSectionPage } from './ProjectSectionPage'
import type { ProjectSummary, ContractLine } from '@/types/projectAccounting'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')
const PCT = (v: number) => `${v.toFixed(1)}%`

export function ProjectBillingPage() {
  return (
    <ProjectSectionPage title="Project Billing">
      {({ project, setError, queryClient }) => <BillingContent project={project} setError={setError} queryClient={queryClient} />}
    </ProjectSectionPage>
  )
}

function BillingContent({ project, setError, queryClient }: { project: ProjectSummary; setError: (e: string | null) => void; queryClient: any }) {
  const [showAddContract, setShowAddContract] = useState(false)
  const [contractForm, setContractForm] = useState({ description: '', billingMethod: 'TimeAndMaterials', contractAmount: 0, unitPrice: 0, unitQuantity: 0, feePercentage: 0, notToExceed: 0, notes: '' })
  const { data: contracts = [] } = useQuery({ queryKey: ['projects', project.id, 'contracts'], queryFn: () => getContractLines(project.id) })
  const { data: wip } = useQuery({ queryKey: ['projects', project.id, 'wip'], queryFn: () => getWipSchedule(project.id) })

  const addContractMutation = useMutation({
    mutationFn: (data: any) => addContractLine(project.id, data),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['projects', project.id, 'contracts'] }); setShowAddContract(false) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })
  const generateMutation = useMutation({
    mutationFn: () => generateInvoice(project.id),
    onSuccess: (data: any) => { queryClient.invalidateQueries({ queryKey: ['projects', project.id] }); setError(null); alert(`Invoice generated: $${data.invoiceAmount}`) },
    onError: (e: any) => setError(getErrorMessage(e)),
  })

  const contractColumns: DataTableColumn<ContractLine>[] = [
    { key: 'description', header: 'Description' },
    { key: 'billingMethod', header: 'Method' },
    { key: 'contractAmount', header: 'Contract', align: 'right', render: (r: ContractLine) => MONEY(r.contractAmount) },
    { key: 'billedAmount', header: 'Billed', align: 'right', render: (r: ContractLine) => MONEY(r.billedAmount) },
    { key: 'remaining', header: 'Remaining', align: 'right', render: (r: ContractLine) => MONEY(r.remaining) },
    { key: 'notToExceed', header: 'NTE', align: 'right', render: (r: ContractLine) => MONEY(r.notToExceed) },
    { key: 'percentComplete', header: '% Complete', align: 'right', render: (r: ContractLine) => PCT(r.percentComplete) },
  ]

  return (
    <div className="space-y-6">
      {wip && (
        <div className="grid grid-cols-4 gap-4 text-sm">
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800"><p className="text-gray-500">Contract Value</p><p className="font-semibold">{MONEY(wip.contractValue)}</p></div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800"><p className="text-gray-500">Costs to Date</p><p className="font-semibold">{MONEY(wip.costsToDate)}</p></div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800"><p className="text-gray-500">Earned Revenue</p><p className="font-semibold">{MONEY(wip.earnedRevenue)}</p></div>
          <div className="rounded-lg border bg-white p-3 dark:border-gray-700 dark:bg-gray-800"><p className="text-gray-500">Over/Under Billing</p><p className={`font-semibold ${wip.overUnderBilling >= 0 ? 'text-green-600' : 'text-red-600'}`}>{MONEY(wip.overUnderBilling)}</p></div>
        </div>
      )}
      <div className="flex justify-between items-center">
        <h3 className="font-semibold text-gray-900 dark:text-white">Contract Lines</h3>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setShowAddContract(true)}><Plus className="h-4 w-4 mr-1" /> Add Line</Button>
          <Button onClick={() => generateMutation.mutate()}><DollarSign className="h-4 w-4 mr-1" /> Generate Invoice</Button>
        </div>
      </div>
      <DataTable data={contracts as ContractLine[]} columns={contractColumns} emptyMessage="No contract lines defined." />
      {showAddContract && (
        <Modal title="Add Contract Line" isOpen={showAddContract} onClose={() => setShowAddContract(false)}>
          <div className="space-y-4">
            <Input label="Description" value={contractForm.description} onChange={e => setContractForm(f => ({ ...f, description: e.target.value }))} />
            <Select label="Billing Method" value={contractForm.billingMethod} onChange={e => setContractForm(f => ({ ...f, billingMethod: e.target.value }))}
              options={['TimeAndMaterials', 'CostPlus', 'FixedPrice', 'UnitPrice', 'Milestone'].map(m => ({ value: m, label: m }))} />
            <div className="grid grid-cols-2 gap-4">
              <Input label="Contract Amount" type="number" step="0.01" value={contractForm.contractAmount} onChange={e => setContractForm(f => ({ ...f, contractAmount: Number(e.target.value) }))} />
              <Input label="Not-to-Exceed" type="number" step="0.01" value={contractForm.notToExceed} onChange={e => setContractForm(f => ({ ...f, notToExceed: Number(e.target.value) }))} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <Input label="Unit Price" type="number" step="0.01" value={contractForm.unitPrice} onChange={e => setContractForm(f => ({ ...f, unitPrice: Number(e.target.value) }))} />
              <Input label="Unit Quantity" type="number" value={contractForm.unitQuantity} onChange={e => setContractForm(f => ({ ...f, unitQuantity: Number(e.target.value) }))} />
            </div>
            <Input label="Fee % (Cost-Plus)" type="number" step="0.01" value={contractForm.feePercentage} onChange={e => setContractForm(f => ({ ...f, feePercentage: Number(e.target.value) }))} />
            <Input label="Notes" value={contractForm.notes} onChange={e => setContractForm(f => ({ ...f, notes: e.target.value }))} />
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="outline" onClick={() => setShowAddContract(false)}>Cancel</Button>
              <Button onClick={() => addContractMutation.mutate({
                companyId: currentCompanyId(), description: contractForm.description, billingMethod: contractForm.billingMethod,
                contractAmount: contractForm.contractAmount, unitPrice: contractForm.unitPrice || null, unitQuantity: contractForm.unitQuantity || null,
                feePercentage: contractForm.feePercentage || null, notToExceed: contractForm.notToExceed || null, notes: contractForm.notes || null,
              })}>Add Line</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}
