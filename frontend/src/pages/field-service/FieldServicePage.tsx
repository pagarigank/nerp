// Field Service Management module page (Phase 12): work orders (intake -> dispatch ->
// clock-in/out -> lines -> complete -> close), dispatch board, service contracts, equipment,
// technicians, skills, SLAs, territories, rate cards, estimates, preventive maintenance,
// van stock, warranty claims, and reports.
import { useState } from 'react'
import { Plus, Truck, BarChart3 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/inventory'
import {
  getWorkOrders, getWorkOrder, createWorkOrder, dispatchWorkOrder, clockInWorkOrder,
  clockOutWorkOrder, addWorkOrderLine, completeWorkOrder, closeWorkOrder, cancelWorkOrder,
  getEstimates, createEstimate, approveEstimate, convertEstimate,
  getServiceContracts, createServiceContract,
  getEquipment, createEquipment,
  getTechnicians, createTechnician,
  getSlas, createSla,
  getTerritories, createTerritory,
  getRateCards, createRateCard,
  getPreventiveMaintenance, createPreventiveMaintenance,
  getVanStock, createVanStock, issueVanStock,
  getWarrantyClaims, createWarrantyClaim,
  getSlaCompliance, getTechnicianUtilization, getOpenAging, getContractStatus, getPmDue,
} from '@api/fieldService'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

const MONEY = (v: number | null) =>
  v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—'

const TYPE_OPTS = [
  { value: '0', label: 'Service' }, { value: '1', label: 'Install' },
  { value: '2', label: 'Repair' }, { value: '3', label: 'Inspection' },
]
const PRIORITY_OPTS = [
  { value: '0', label: 'Low' }, { value: '1', label: 'Medium' },
  { value: '2', label: 'High' }, { value: '3', label: 'Critical' },
]
const BILLING_OPTS = [
  { value: '0', label: 'Fixed' }, { value: '1', label: 'Time & Material' }, { value: '2', label: 'Usage' },
]
const OWNERSHIP_OPTS = [
  { value: '0', label: 'Company' }, { value: '1', label: 'Customer' }, { value: '2', label: 'Leased' },
]
const LINE_TYPE_OPTS = [
  { value: '0', label: 'Labor' }, { value: '1', label: 'Parts' }, { value: '2', label: 'Travel' }, { value: '3', label: 'Fee' },
]
const FREQUENCY_OPTS = [
  { value: '0', label: 'Daily' }, { value: '1', label: 'Weekly' }, { value: '2', label: 'Monthly' },
  { value: '3', label: 'Quarterly' }, { value: '4', label: 'Annual' },
]

function statusBadge(status: any) {
  const s = String(status ?? '')
  const cls =
    s === 'Completed' ? 'bg-green-100 text-green-800'
    : s === 'InProgress' || s === 'Dispatched' ? 'bg-blue-100 text-blue-800'
    : s === 'Closed' ? 'bg-gray-100 text-gray-800'
    : s === 'Cancelled' ? 'bg-red-100 text-red-800'
    : 'bg-yellow-100 text-yellow-800'
  return <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>{s || '—'}</span>
}

// ---------------------------------------------------------------- Work Orders
export function WorkOrdersTab() {
  const qc = useQueryClient()
  const [selId, setSelId] = useState('')
  const [show, setShow] = useState(false)
  const [tech, setTech] = useState('')
  const [form, setForm] = useState({
    workOrderNumber: '', customerId: '', technicianId: '', type: '0', priority: '1',
    scheduledStart: '', warrantyCovered: false, notes: '',
  })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'work-orders'], queryFn: () => getWorkOrders() })
  const detail = useQuery({ queryKey: ['fs', 'wo', selId], queryFn: () => getWorkOrder(selId), enabled: !!selId })
  const createMut = useMutation({
    mutationFn: () => createWorkOrder({ companyId: currentCompanyId(), workOrderNumber: form.workOrderNumber, customerId: form.customerId || null, technicianId: form.technicianId || null, type: Number(form.type), priority: Number(form.priority), scheduledStart: form.scheduledStart || null, warrantyCovered: form.warrantyCovered, notes: form.notes }),
    onSuccess: () => { setShow(false); setForm({ workOrderNumber: '', customerId: '', technicianId: '', type: '0', priority: '1', scheduledStart: '', warrantyCovered: false, notes: '' }); qc.invalidateQueries({ queryKey: ['fs', 'work-orders'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'workOrderNumber', header: 'WO #' },
    { key: 'type', header: 'Type' },
    { key: 'status', header: 'Status', render: (r: any) => statusBadge(r.status) },
    { key: 'priority', header: 'Priority' },
    { key: 'billableTotal', header: 'Billable', render: (r: any) => MONEY(r.billableTotal) },
    { key: 'billedToAr', header: 'Billed AR', render: (r: any) => (r.billedToAr ? 'Yes' : 'No') },
    { key: 'id', header: '', render: (_: unknown, r: any) => <Button size="sm" variant="outline" onClick={() => setSelId(r.id)}>Open</Button> },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Work Order</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Work Order">
        <div className="space-y-3">
          <Input label="Work Order #" value={form.workOrderNumber} onChange={(e: any) => setForm({ ...form, workOrderNumber: e.target.value })} />
          <Input label="Customer ID (GUID)" value={form.customerId} onChange={(e: any) => setForm({ ...form, customerId: e.target.value })} placeholder="00000000-0000-0000-0000-000000000001" />
          <Input label="Technician ID (GUID, optional)" value={form.technicianId} onChange={(e: any) => setForm({ ...form, technicianId: e.target.value })} />
          <Select label="Type" value={form.type} onChange={(e: any) => setForm({ ...form, type: e.target.value })} options={TYPE_OPTS} />
          <Select label="Priority" value={form.priority} onChange={(e: any) => setForm({ ...form, priority: e.target.value })} options={PRIORITY_OPTS} />
          <Input label="Scheduled Start" type="datetime-local" value={form.scheduledStart} onChange={(e: any) => setForm({ ...form, scheduledStart: e.target.value })} />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.warrantyCovered} onChange={(e: any) => setForm({ ...form, warrantyCovered: e.target.checked })} /> Warranty Covered</label>
          <Input label="Notes" value={form.notes} onChange={(e: any) => setForm({ ...form, notes: e.target.value })} />
          {createMut.isError && <p className="text-sm text-red-600">{getErrorMessage(createMut.error)}</p>}
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.workOrderNumber}>Create</Button>
        </div>
      </Modal>
      {selId && <WorkOrderDetail id={selId} detail={detail} qc={qc} tech={tech} setTech={setTech} />}
    </div>
  )
}

function WorkOrderDetail({ id, detail, qc, tech, setTech }: any) {
  const [showLine, setShowLine] = useState(false)
  const [line, setLine] = useState({ lineType: '0', description: '', quantity: '', unitRate: '', billable: true, itemId: '' })
  const [complete, setComplete] = useState({ laborHours: '', laborCost: '', partsMarkupPercent: '', tripCharge: '', resolution: '' })
  const invalidate = () => { qc.invalidateQueries({ queryKey: ['fs', 'wo', id] }); qc.invalidateQueries({ queryKey: ['fs', 'work-orders'] }) }
  const d = detail.data
  const dispatchMut = useMutation({ mutationFn: () => dispatchWorkOrder(id, { companyId: currentCompanyId(), technicianId: tech }), onSuccess: invalidate })
  const clockInMut = useMutation({ mutationFn: () => clockInWorkOrder(id, { companyId: currentCompanyId() }), onSuccess: invalidate })
  const clockOutMut = useMutation({ mutationFn: () => clockOutWorkOrder(id, { companyId: currentCompanyId(), laborHours: 0, laborCost: 0 }), onSuccess: invalidate })
  const lineMut = useMutation({ mutationFn: () => addWorkOrderLine(id, { companyId: currentCompanyId(), lineType: Number(line.lineType), description: line.description, quantity: Number(line.quantity || 0), unitRate: Number(line.unitRate || 0), billable: line.billable, itemId: line.itemId || null }), onSuccess: () => { setShowLine(false); invalidate() } })
  const completeMut = useMutation({ mutationFn: () => completeWorkOrder(id, { companyId: currentCompanyId(), laborHours: Number(complete.laborHours || 0), laborCost: Number(complete.laborCost || 0), partsMarkupPercent: Number(complete.partsMarkupPercent || 0), tripCharge: Number(complete.tripCharge || 0), resolution: complete.resolution }), onSuccess: invalidate })
  const closeMut = useMutation({ mutationFn: () => closeWorkOrder(id, { companyId: currentCompanyId() }), onSuccess: invalidate })
  const cancelMut = useMutation({ mutationFn: () => cancelWorkOrder(id, { companyId: currentCompanyId() }), onSuccess: invalidate })

  return (
    <div className="rounded-lg border border-gray-200 dark:border-gray-700 p-4 space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold">{d?.workOrderNumber} — {statusBadge(d?.status)}</h3>
        <span className="text-sm text-gray-500">Billable: {MONEY(d?.billableTotal)} {d?.billedToAr ? '· Billed to AR' : ''}</span>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-sm">
        <div>Labor Hrs: {d?.laborHours ?? '—'}</div><div>Labor: {MONEY(d?.laborCost)}</div>
        <div>Parts: {MONEY(d?.partsCost)}</div><div>Travel: {MONEY(d?.travelCost)}</div>
        <div>Fees: {MONEY(d?.fees)}</div><div>Warranty: {d?.warrantyCovered ? 'Yes' : 'No'}</div>
        <div>Clock In: {d?.clockIn ? new Date(d.clockIn).toLocaleString() : '—'}</div><div>Clock Out: {d?.clockOut ? new Date(d.clockOut).toLocaleString() : '—'}</div>
      </div>
      <div className="flex flex-wrap gap-2">
        <Input placeholder="Technician ID to dispatch" className="w-64" value={tech} onChange={(e: any) => setTech(e.target.value)} />
        <Button size="sm" onClick={() => dispatchMut.mutate()} disabled={dispatchMut.isPending || !tech}>Dispatch</Button>
        <Button size="sm" variant="outline" onClick={() => clockInMut.mutate()}>Clock In</Button>
        <Button size="sm" variant="outline" onClick={() => clockOutMut.mutate()}>Clock Out</Button>
        <Button size="sm" variant="outline" onClick={() => setShowLine(true)}>Add Line</Button>
        <Button size="sm" variant="success" onClick={() => completeMut.mutate()}>Complete</Button>
        <Button size="sm" variant="ghost" onClick={() => closeMut.mutate()}>Close</Button>
        <Button size="sm" variant="destructive" onClick={() => cancelMut.mutate()}>Cancel</Button>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
        <Input label="Labor Hours" type="number" value={complete.laborHours} onChange={(e: any) => setComplete({ ...complete, laborHours: e.target.value })} />
        <Input label="Labor Cost" type="number" value={complete.laborCost} onChange={(e: any) => setComplete({ ...complete, laborCost: e.target.value })} />
        <Input label="Parts Markup %" type="number" value={complete.partsMarkupPercent} onChange={(e: any) => setComplete({ ...complete, partsMarkupPercent: e.target.value })} />
        <Input label="Trip Charge" type="number" value={complete.tripCharge} onChange={(e: any) => setComplete({ ...complete, tripCharge: e.target.value })} />
        <Input label="Resolution" className="md:col-span-4" value={complete.resolution} onChange={(e: any) => setComplete({ ...complete, resolution: e.target.value })} />
      </div>
      {dispatchMut.isError && <p className="text-sm text-red-600">{getErrorMessage(dispatchMut.error)}</p>}
      {completeMut.isError && <p className="text-sm text-red-600">{getErrorMessage(completeMut.error)}</p>}
      <Modal isOpen={showLine} onClose={() => setShowLine(false)} title="Add Work Order Line">
        <div className="space-y-3">
          <Select label="Line Type" value={line.lineType} onChange={(e: any) => setLine({ ...line, lineType: e.target.value })} options={LINE_TYPE_OPTS} />
          <Input label="Description" value={line.description} onChange={(e: any) => setLine({ ...line, description: e.target.value })} />
          <Input label="Quantity" type="number" value={line.quantity} onChange={(e: any) => setLine({ ...line, quantity: e.target.value })} />
          <Input label="Unit Rate" type="number" value={line.unitRate} onChange={(e: any) => setLine({ ...line, unitRate: e.target.value })} />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={line.billable} onChange={(e: any) => setLine({ ...line, billable: e.target.checked })} /> Billable</label>
          <Button onClick={() => lineMut.mutate()} disabled={lineMut.isPending}>Add</Button>
        </div>
      </Modal>
      {d?.lines?.length > 0 && (
        <table className="w-full text-sm">
          <thead><tr className="text-left border-b"><th>Type</th><th>Description</th><th>Qty</th><th>Rate</th><th>Total</th><th>Billable</th></tr></thead>
          <tbody>
            {d.lines.map((l: any) => (
              <tr key={l.id} className="border-b">
                <td>{l.lineType}</td><td>{l.description}</td><td>{l.quantity}</td><td>{MONEY(l.unitRate)}</td><td>{MONEY(l.lineTotal)}</td><td>{l.billable ? 'Yes' : 'No'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

// ---------------------------------------------------------------- Dispatch Board
export function DispatchBoardTab() {
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'work-orders'], queryFn: () => getWorkOrders() })
  const open = (data as any[]).filter((w: any) => w.status !== 'Completed' && w.status !== 'Closed' && w.status !== 'Cancelled')
  const cols: DataTableColumn<any>[] = [
    { key: 'workOrderNumber', header: 'WO #' },
    { key: 'priority', header: 'Priority' },
    { key: 'technicianId', header: 'Technician', render: (r: any) => r.technicianId ? r.technicianId.slice(0, 8) : 'Unassigned' },
    { key: 'scheduledStart', header: 'Scheduled', render: (r: any) => r.scheduledStart ? new Date(r.scheduledStart).toLocaleString() : '—' },
    { key: 'status', header: 'Status', render: (r: any) => statusBadge(r.status) },
    { key: 'billableTotal', header: 'Billable', render: (r: any) => MONEY(r.billableTotal) },
  ]
  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2"><Truck className="h-5 w-5 text-indigo-600" /><h2 className="text-lg font-semibold">Dispatch Board</h2></div>
      <DataTable columns={cols} data={open as any[]} loading={isLoading} />
    </div>
  )
}

// ---------------------------------------------------------------- Technicians
export function TechniciansTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ employeeId: '', code: '', firstName: '', lastName: '', status: '0', hourlyRate: '', email: '', phone: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'technicians'], queryFn: () => getTechnicians() })
  const createMut = useMutation({
    mutationFn: () => createTechnician({ companyId: currentCompanyId(), employeeId: form.employeeId || '00000000-0000-0000-0000-000000000000', code: form.code, firstName: form.firstName, lastName: form.lastName, status: Number(form.status), hourlyRate: Number(form.hourlyRate || 0), email: form.email || null, phone: form.phone || null }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'technicians'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'code', header: 'Code' }, { key: 'firstName', header: 'First' }, { key: 'lastName', header: 'Last' },
    { key: 'status', header: 'Status', render: (r: any) => statusBadge(r.status) },
    { key: 'hourlyRate', header: 'Rate', render: (r: any) => MONEY(r.hourlyRate) },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Technician</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Technician">
        <div className="space-y-3">
          <Input label="Code" value={form.code} onChange={(e: any) => setForm({ ...form, code: e.target.value })} />
          <Input label="First Name" value={form.firstName} onChange={(e: any) => setForm({ ...form, firstName: e.target.value })} />
          <Input label="Last Name" value={form.lastName} onChange={(e: any) => setForm({ ...form, lastName: e.target.value })} />
          <Input label="Employee ID (GUID)" value={form.employeeId} onChange={(e: any) => setForm({ ...form, employeeId: e.target.value })} />
          <Input label="Hourly Rate" type="number" value={form.hourlyRate} onChange={(e: any) => setForm({ ...form, hourlyRate: e.target.value })} />
          <Input label="Email" value={form.email} onChange={(e: any) => setForm({ ...form, email: e.target.value })} />
          <Input label="Phone" value={form.phone} onChange={(e: any) => setForm({ ...form, phone: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.code}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- Contracts
export function ContractsTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ contractNumber: '', name: '', customerId: '', billingType: '0', contractValue: '', includesWarranty: false, notes: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'contracts'], queryFn: () => getServiceContracts() })
  const createMut = useMutation({
    mutationFn: () => createServiceContract({ companyId: currentCompanyId(), contractNumber: form.contractNumber, name: form.name, customerId: form.customerId, billingType: Number(form.billingType), contractValue: form.contractValue ? Number(form.contractValue) : null, includesWarranty: form.includesWarranty, notes: form.notes }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'contracts'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'contractNumber', header: 'Contract #' }, { key: 'name', header: 'Name' },
    { key: 'status', header: 'Status', render: (r: any) => statusBadge(r.status) },
    { key: 'billingType', header: 'Billing' }, { key: 'contractValue', header: 'Value', render: (r: any) => MONEY(r.contractValue) },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Contract</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Service Contract">
        <div className="space-y-3">
          <Input label="Contract #" value={form.contractNumber} onChange={(e: any) => setForm({ ...form, contractNumber: e.target.value })} />
          <Input label="Name" value={form.name} onChange={(e: any) => setForm({ ...form, name: e.target.value })} />
          <Input label="Customer ID (GUID)" value={form.customerId} onChange={(e: any) => setForm({ ...form, customerId: e.target.value })} />
          <Select label="Billing Type" value={form.billingType} onChange={(e: any) => setForm({ ...form, billingType: e.target.value })} options={BILLING_OPTS} />
          <Input label="Contract Value" type="number" value={form.contractValue} onChange={(e: any) => setForm({ ...form, contractValue: e.target.value })} />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.includesWarranty} onChange={(e: any) => setForm({ ...form, includesWarranty: e.target.checked })} /> Includes Warranty</label>
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.contractNumber}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- Equipment
export function EquipmentTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ assetTag: '', serialNumber: '', description: '', ownership: '0', underWarranty: false })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'equipment'], queryFn: () => getEquipment() })
  const createMut = useMutation({
    mutationFn: () => createEquipment({ companyId: currentCompanyId(), assetTag: form.assetTag, serialNumber: form.serialNumber, description: form.description, ownership: Number(form.ownership), underWarranty: form.underWarranty }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'equipment'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'assetTag', header: 'Asset Tag' }, { key: 'serialNumber', header: 'Serial' }, { key: 'description', header: 'Description' },
    { key: 'ownership', header: 'Ownership' }, { key: 'underWarranty', header: 'Warranty', render: (r: any) => (r.underWarranty ? 'Yes' : 'No') },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Equipment</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Equipment Asset">
        <div className="space-y-3">
          <Input label="Asset Tag" value={form.assetTag} onChange={(e: any) => setForm({ ...form, assetTag: e.target.value })} />
          <Input label="Serial Number" value={form.serialNumber} onChange={(e: any) => setForm({ ...form, serialNumber: e.target.value })} />
          <Input label="Description" value={form.description} onChange={(e: any) => setForm({ ...form, description: e.target.value })} />
          <Select label="Ownership" value={form.ownership} onChange={(e: any) => setForm({ ...form, ownership: e.target.value })} options={OWNERSHIP_OPTS} />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.underWarranty} onChange={(e: any) => setForm({ ...form, underWarranty: e.target.checked })} /> Under Warranty</label>
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.assetTag}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- SLAs
export function SlasTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ name: '', priority: '1', responseMinutes: '', resolutionMinutes: '', escalate: false })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'slas'], queryFn: () => getSlas() })
  const createMut = useMutation({
    mutationFn: () => createSla({ companyId: currentCompanyId(), name: form.name, priority: Number(form.priority), responseMinutes: Number(form.responseMinutes || 0), resolutionMinutes: Number(form.resolutionMinutes || 0), escalate: form.escalate }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'slas'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'name', header: 'Name' }, { key: 'priority', header: 'Priority' },
    { key: 'responseMinutes', header: 'Response (min)' }, { key: 'resolutionMinutes', header: 'Resolution (min)' },
    { key: 'escalate', header: 'Escalate', render: (r: any) => (r.escalate ? 'Yes' : 'No') },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New SLA</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New SLA Definition">
        <div className="space-y-3">
          <Input label="Name" value={form.name} onChange={(e: any) => setForm({ ...form, name: e.target.value })} />
          <Select label="Priority" value={form.priority} onChange={(e: any) => setForm({ ...form, priority: e.target.value })} options={PRIORITY_OPTS} />
          <Input label="Response Minutes" type="number" value={form.responseMinutes} onChange={(e: any) => setForm({ ...form, responseMinutes: e.target.value })} />
          <Input label="Resolution Minutes" type="number" value={form.resolutionMinutes} onChange={(e: any) => setForm({ ...form, resolutionMinutes: e.target.value })} />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.escalate} onChange={(e: any) => setForm({ ...form, escalate: e.target.checked })} /> Escalate</label>
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.name}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- Territories
export function TerritoriesTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ code: '', name: '', region: '', travelCostPerMile: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'territories'], queryFn: () => getTerritories() })
  const createMut = useMutation({
    mutationFn: () => createTerritory({ companyId: currentCompanyId(), code: form.code, name: form.name, region: form.region || null, travelCostPerMile: Number(form.travelCostPerMile || 0) }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'territories'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'code', header: 'Code' }, { key: 'name', header: 'Name' }, { key: 'region', header: 'Region' },
    { key: 'travelCostPerMile', header: 'Cost/Mile', render: (r: any) => MONEY(r.travelCostPerMile) },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Territory</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Service Territory">
        <div className="space-y-3">
          <Input label="Code" value={form.code} onChange={(e: any) => setForm({ ...form, code: e.target.value })} />
          <Input label="Name" value={form.name} onChange={(e: any) => setForm({ ...form, name: e.target.value })} />
          <Input label="Region" value={form.region} onChange={(e: any) => setForm({ ...form, region: e.target.value })} />
          <Input label="Travel Cost / Mile" type="number" value={form.travelCostPerMile} onChange={(e: any) => setForm({ ...form, travelCostPerMile: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.code}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- Rate Cards
export function RateCardsTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ name: '', laborRatePerHour: '', overtimeRatePerHour: '', tripCharge: '', partsMarkupPercent: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'rate-cards'], queryFn: () => getRateCards() })
  const createMut = useMutation({
    mutationFn: () => createRateCard({ companyId: currentCompanyId(), name: form.name, effectiveDate: new Date().toISOString().slice(0, 10), isActive: true, laborRatePerHour: Number(form.laborRatePerHour || 0), overtimeRatePerHour: Number(form.overtimeRatePerHour || 0), tripCharge: Number(form.tripCharge || 0), partsMarkupPercent: Number(form.partsMarkupPercent || 0) }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'rate-cards'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'name', header: 'Name' }, { key: 'laborRatePerHour', header: 'Labor/hr', render: (r: any) => MONEY(r.laborRatePerHour) },
    { key: 'overtimeRatePerHour', header: 'OT/hr', render: (r: any) => MONEY(r.overtimeRatePerHour) },
    { key: 'tripCharge', header: 'Trip', render: (r: any) => MONEY(r.tripCharge) },
    { key: 'partsMarkupPercent', header: 'Markup %', render: (r: any) => `${r.partsMarkupPercent}%` },
    { key: 'isActive', header: 'Active', render: (r: any) => (r.isActive ? 'Yes' : 'No') },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Rate Card</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Rate Card">
        <div className="space-y-3">
          <Input label="Name" value={form.name} onChange={(e: any) => setForm({ ...form, name: e.target.value })} />
          <Input label="Labor Rate / Hour" type="number" value={form.laborRatePerHour} onChange={(e: any) => setForm({ ...form, laborRatePerHour: e.target.value })} />
          <Input label="Overtime Rate / Hour" type="number" value={form.overtimeRatePerHour} onChange={(e: any) => setForm({ ...form, overtimeRatePerHour: e.target.value })} />
          <Input label="Trip Charge" type="number" value={form.tripCharge} onChange={(e: any) => setForm({ ...form, tripCharge: e.target.value })} />
          <Input label="Parts Markup %" type="number" value={form.partsMarkupPercent} onChange={(e: any) => setForm({ ...form, partsMarkupPercent: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.name}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- Estimates
export function EstimatesTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ estimateNumber: '', customerId: '', billingType: '0', laborEstimate: '', partsEstimate: '', travelEstimate: '', taxEstimate: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'estimates'], queryFn: () => getEstimates() })
  const createMut = useMutation({
    mutationFn: () => createEstimate({ companyId: currentCompanyId(), estimateNumber: form.estimateNumber, customerId: form.customerId || null, billingType: Number(form.billingType), laborEstimate: Number(form.laborEstimate || 0), partsEstimate: Number(form.partsEstimate || 0), travelEstimate: Number(form.travelEstimate || 0), taxEstimate: Number(form.taxEstimate || 0) }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'estimates'] }) },
  })
  const approveMut = useMutation({ mutationFn: (id: string) => approveEstimate(id, { companyId: currentCompanyId() }), onSuccess: () => qc.invalidateQueries({ queryKey: ['fs', 'estimates'] }) })
  const convertMut = useMutation({ mutationFn: (id: string) => convertEstimate(id, { companyId: currentCompanyId(), workOrderNumber: `WO-${Date.now()}` }), onSuccess: () => { qc.invalidateQueries({ queryKey: ['fs', 'estimates'] }); qc.invalidateQueries({ queryKey: ['fs', 'work-orders'] }) } })
  const cols: DataTableColumn<any>[] = [
    { key: 'estimateNumber', header: 'Estimate #' }, { key: 'status', header: 'Status', render: (r: any) => statusBadge(r.status) },
    { key: 'billingType', header: 'Billing' }, { key: 'totalEstimate', header: 'Total', render: (r: any) => MONEY(r.totalEstimate) },
    { key: 'id', header: '', render: (_: unknown, r: any) => (
      <div className="flex gap-1">
        <Button size="sm" variant="outline" onClick={() => approveMut.mutate(r.id)}>Approve</Button>
        <Button size="sm" variant="ghost" onClick={() => convertMut.mutate(r.id)}>Convert</Button>
      </div>
    ) },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Estimate</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Estimate">
        <div className="space-y-3">
          <Input label="Estimate #" value={form.estimateNumber} onChange={(e: any) => setForm({ ...form, estimateNumber: e.target.value })} />
          <Input label="Customer ID (GUID)" value={form.customerId} onChange={(e: any) => setForm({ ...form, customerId: e.target.value })} />
          <Select label="Billing Type" value={form.billingType} onChange={(e: any) => setForm({ ...form, billingType: e.target.value })} options={BILLING_OPTS} />
          <Input label="Labor Estimate" type="number" value={form.laborEstimate} onChange={(e: any) => setForm({ ...form, laborEstimate: e.target.value })} />
          <Input label="Parts Estimate" type="number" value={form.partsEstimate} onChange={(e: any) => setForm({ ...form, partsEstimate: e.target.value })} />
          <Input label="Travel Estimate" type="number" value={form.travelEstimate} onChange={(e: any) => setForm({ ...form, travelEstimate: e.target.value })} />
          <Input label="Tax Estimate" type="number" value={form.taxEstimate} onChange={(e: any) => setForm({ ...form, taxEstimate: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.estimateNumber}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- PM Schedules
export function PmTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ code: '', description: '', frequency: '0', intervalMonths: '', nextDue: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'pm'], queryFn: () => getPreventiveMaintenance() })
  const createMut = useMutation({
    mutationFn: () => createPreventiveMaintenance({ companyId: currentCompanyId(), code: form.code, description: form.description, frequency: Number(form.frequency), intervalMonths: Number(form.intervalMonths || 0), nextDue: form.nextDue || null, isActive: true }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'pm'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'code', header: 'Code' }, { key: 'description', header: 'Description' }, { key: 'frequency', header: 'Frequency' },
    { key: 'intervalMonths', header: 'Interval (mo)' }, { key: 'nextDue', header: 'Next Due', render: (r: any) => r.nextDue ? new Date(r.nextDue).toLocaleDateString() : '—' },
    { key: 'isActive', header: 'Active', render: (r: any) => (r.isActive ? 'Yes' : 'No') },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New PM Schedule</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Preventive Maintenance">
        <div className="space-y-3">
          <Input label="Code" value={form.code} onChange={(e: any) => setForm({ ...form, code: e.target.value })} />
          <Input label="Description" value={form.description} onChange={(e: any) => setForm({ ...form, description: e.target.value })} />
          <Select label="Frequency" value={form.frequency} onChange={(e: any) => setForm({ ...form, frequency: e.target.value })} options={FREQUENCY_OPTS} />
          <Input label="Interval (months)" type="number" value={form.intervalMonths} onChange={(e: any) => setForm({ ...form, intervalMonths: e.target.value })} />
          <Input label="Next Due" type="date" value={form.nextDue} onChange={(e: any) => setForm({ ...form, nextDue: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.code}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- Van Stock
export function VanStockTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ technicianId: '', itemId: '', warehouseId: '', quantityOnHand: '', reorderPoint: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'van-stock'], queryFn: () => getVanStock() })
  const createMut = useMutation({
    mutationFn: () => createVanStock({ companyId: currentCompanyId(), technicianId: form.technicianId, itemId: form.itemId, warehouseId: form.warehouseId, quantityOnHand: Number(form.quantityOnHand || 0), reorderPoint: Number(form.reorderPoint || 0) }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'van-stock'] }) },
  })
  const issueMut = useMutation({ mutationFn: (p: any) => issueVanStock(p.id, { companyId: currentCompanyId(), quantity: Number(p.qty || 0) }), onSuccess: () => qc.invalidateQueries({ queryKey: ['fs', 'van-stock'] }) })
  const cols: DataTableColumn<any>[] = [
    { key: 'technicianId', header: 'Technician', render: (r: any) => r.technicianId?.slice(0, 8) },
    { key: 'itemId', header: 'Item', render: (r: any) => r.itemId?.slice(0, 8) },
    { key: 'quantityOnHand', header: 'On Hand' }, { key: 'reorderPoint', header: 'Reorder Pt' },
    { key: 'id', header: 'Issue', render: (_: unknown, r: any) => (
      <Button size="sm" variant="outline" onClick={() => { const q = prompt('Quantity to issue?'); if (q) issueMut.mutate({ id: r.id, qty: q }) }}>Issue</Button>
    ) },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> Add Van Stock</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="Add Van Stock">
        <div className="space-y-3">
          <Input label="Technician ID (GUID)" value={form.technicianId} onChange={(e: any) => setForm({ ...form, technicianId: e.target.value })} />
          <Input label="Item ID (GUID)" value={form.itemId} onChange={(e: any) => setForm({ ...form, itemId: e.target.value })} />
          <Input label="Warehouse ID (GUID)" value={form.warehouseId} onChange={(e: any) => setForm({ ...form, warehouseId: e.target.value })} />
          <Input label="Quantity On Hand" type="number" value={form.quantityOnHand} onChange={(e: any) => setForm({ ...form, quantityOnHand: e.target.value })} />
          <Input label="Reorder Point" type="number" value={form.reorderPoint} onChange={(e: any) => setForm({ ...form, reorderPoint: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.technicianId || !form.itemId}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- Warranty Claims
export function WarrantyTab() {
  const qc = useQueryClient()
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ claimNumber: '', equipmentAssetId: '', description: '', claimAmount: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['fs', 'warranty-claims'], queryFn: () => getWarrantyClaims() })
  const createMut = useMutation({
    mutationFn: () => createWarrantyClaim({ companyId: currentCompanyId(), claimNumber: form.claimNumber, equipmentAssetId: form.equipmentAssetId, description: form.description, claimAmount: Number(form.claimAmount || 0) }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['fs', 'warranty-claims'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'claimNumber', header: 'Claim #' }, { key: 'claimAmount', header: 'Amount', render: (r: any) => MONEY(r.claimAmount) },
    { key: 'status', header: 'Status', render: (r: any) => statusBadge(r.status) },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Warranty Claim</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Warranty Claim">
        <div className="space-y-3">
          <Input label="Claim #" value={form.claimNumber} onChange={(e: any) => setForm({ ...form, claimNumber: e.target.value })} />
          <Input label="Equipment Asset ID (GUID)" value={form.equipmentAssetId} onChange={(e: any) => setForm({ ...form, equipmentAssetId: e.target.value })} />
          <Input label="Description" value={form.description} onChange={(e: any) => setForm({ ...form, description: e.target.value })} />
          <Input label="Claim Amount" type="number" value={form.claimAmount} onChange={(e: any) => setForm({ ...form, claimAmount: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.claimNumber}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

// ---------------------------------------------------------------- Reports
export function ReportsTab() {
  const [report, setReport] = useState<'sla' | 'util' | 'aging' | 'contract' | 'pm'>('sla')
  const sla = useQuery({ queryKey: ['fs', 'rpt', 'sla'], queryFn: () => getSlaCompliance() })
  const util = useQuery({ queryKey: ['fs', 'rpt', 'util'], queryFn: () => getTechnicianUtilization() })
  const aging = useQuery({ queryKey: ['fs', 'rpt', 'aging'], queryFn: () => getOpenAging() })
  const contract = useQuery({ queryKey: ['fs', 'rpt', 'contract'], queryFn: () => getContractStatus() })
  const pm = useQuery({ queryKey: ['fs', 'rpt', 'pm'], queryFn: () => getPmDue() })

  const tabs: { key: typeof report; label: string; cols: DataTableColumn<any>[]; data: any[]; loading: boolean }[] = [
    { key: 'sla', label: 'SLA Compliance', cols: [
      { key: 'priority', header: 'Priority' }, { key: 'totalWorkOrders', header: 'Total' },
      { key: 'metSla', header: 'Met SLA' }, { key: 'breached', header: 'Breached' },
      { key: 'compliancePercent', header: 'Compliance %', render: (r: any) => `${r.compliancePercent?.toFixed(1)}%` },
    ], data: sla.data ?? [], loading: sla.isLoading },
    { key: 'util', label: 'Technician Utilization', cols: [
      { key: 'technicianId', header: 'Technician', render: (r: any) => r.technicianId?.slice(0, 8) },
      { key: 'assignedHours', header: 'Assigned Hrs' }, { key: 'completedHours', header: 'Completed Hrs' },
      { key: 'utilizationPercent', header: 'Utilization %', render: (r: any) => `${r.utilizationPercent?.toFixed(1)}%` },
    ], data: util.data ?? [], loading: util.isLoading },
    { key: 'aging', label: 'Open WO Aging', cols: [
      { key: 'workOrderNumber', header: 'WO #' }, { key: 'ageDays', header: 'Age (days)' },
      { key: 'priority', header: 'Priority' }, { key: 'status', header: 'Status', render: (r: any) => statusBadge(r.status) },
    ], data: aging.data ?? [], loading: aging.isLoading },
    { key: 'contract', label: 'Contract Status', cols: [
      { key: 'contractNumber', header: 'Contract #' }, { key: 'name', header: 'Name' },
      { key: 'status', header: 'Status', render: (r: any) => statusBadge(r.status) },
      { key: 'totalWorkOrders', header: 'WO Count' }, { key: 'remainingValue', header: 'Remaining', render: (r: any) => MONEY(r.remainingValue) },
    ], data: contract.data ?? [], loading: contract.isLoading },
    { key: 'pm', label: 'PM Due', cols: [
      { key: 'code', header: 'Code' }, { key: 'description', header: 'Description' },
      { key: 'nextDue', header: 'Next Due', render: (r: any) => r.nextDue ? new Date(r.nextDue).toLocaleDateString() : '—' },
      { key: 'isOverdue', header: 'Overdue', render: (r: any) => (r.isOverdue ? 'Yes' : 'No') },
    ], data: pm.data ?? [], loading: pm.isLoading },
  ]

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2"><BarChart3 className="h-5 w-5 text-indigo-600" /><h2 className="text-lg font-semibold">Field Service Reports</h2></div>
      <div className="flex flex-wrap gap-1">
        {tabs.map((t) => (
          <button key={t.key} onClick={() => setReport(t.key)} className={`px-3 py-1.5 text-sm rounded-md ${report === t.key ? 'bg-primary-600 text-white' : 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-300'}`}>{t.label}</button>
        ))}
      </div>
      {tabs.filter((t) => t.key === report).map((t) => (
        <DataTable key={t.key} columns={t.cols} data={t.data} loading={t.loading} />
      ))}
    </div>
  )
}
