// Payroll module page (Phase 11): employees, pay codes, union/certified profiles,
// timesheets (with PA project validation + labor dual-post), payroll runs (draft/post/
// accrue/reverse + certified payroll report), and garnishments (CCPA).
import { useState, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { UserCheck, Clock, CalendarRange, FileSpreadsheet, Scale, BadgeDollarSign, Plus, Trash2 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getAccounts } from '@api/platform'
import { companyId as currentCompanyId } from '@api/orderManagement'
import {
  getEmployees, createEmployee, editEmployee,
  getPayCodes, createPayCode, editPayCode,
  getUnionProfiles, createUnionProfile, validatePrevailingWage,
  createTimesheet, submitTimesheet, approveTimesheet,
  createDraftRun, postRun, accrueRun, reverseRun, getCertifiedPayroll,
  editRunLine, voidRun, printChecks, getNachaFile,
  createExpenseReport, addExpenseLine, submitExpense, approveExpense, reimburseExpense,
  createDeductionBenefit, getDeductionBenefits, enrollDeductionBenefit,
  createW4, getW4, computeWithholding, createWageBaseLimit, getWageBaseLimits,
  createWorkersCompClassCode,
  createPtoLedger, accruePto, usePto, getPtoLedgers,
  createManualCheck,
  getFicaCap, getW2Register, getForm941, getForm940,
  getPayrollRegister, getPayrollSummary, getLaborDistribution,
  getGarnishmentRegister, getWageBaseReport, getPtoReport, getDirectDepositRegister,
  createGarnishment, getGarnishmentsForEmployee, computeGarnishments,
  getTaxLiability, getDeductionRegister, getCertifiedPayrollWh347, getTimeExpenseByProject,
  getEmployeeEarnings, getW2Reconciliation, getForm941Reconciliation, getPayrollAccrual,
  getEftpsSchedule, getAchReturnReport, getNewHireReport, getWorkersCompPremium,
  getPendingLiabilityPayments, payLiabilities,
  sendPrenote, verifyDirectDeposit,
  getTaxFilingExport,
} from '@api/payroll'
import { getProjects, getProjectTasks } from '@api/projectAccounting'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')

type Tab = 'employees' | 'paycodes' | 'union' | 'timesheets' | 'runs' | 'expenses' | 'tax' | 'deductions' | 'pto' | 'manual' | 'reports' | 'garnishments' | 'setup'
const TABS: Tab[] = ['employees', 'paycodes', 'union', 'timesheets', 'runs', 'expenses', 'tax', 'deductions', 'pto', 'manual', 'reports', 'garnishments', 'setup']

export function PayrollPage() {
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()
  const tabParam = searchParams.get('tab') as Tab | null
  const tab: Tab = tabParam && TABS.includes(tabParam) ? tabParam : 'employees'
  const setTab = (key: Tab) => setSearchParams(key === 'employees' ? {} : { tab: key })

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <BadgeDollarSign className="h-6 w-6 text-indigo-600" />
        <h1 className="text-2xl font-semibold">Payroll</h1>
      </div>

      {tab === 'employees' && <EmployeesTab qc={queryClient} />}
      {tab === 'paycodes' && <PayCodesTab qc={queryClient} />}
      {tab === 'union' && <UnionTab />}
      {tab === 'timesheets' && <TimesheetsTab qc={queryClient} />}
      {tab === 'runs' && <RunsTab qc={queryClient} />}
      {tab === 'expenses' && <ExpensesTab qc={queryClient} />}
      {tab === 'tax' && <TaxTab qc={queryClient} />}
      {tab === 'deductions' && <DeductionsTab qc={queryClient} />}
      {tab === 'pto' && <PtoTab qc={queryClient} />}
      {tab === 'manual' && <ManualChecksTab qc={queryClient} />}
      {tab === 'reports' && <ReportsTab qc={queryClient} />}
      {tab === 'garnishments' && <GarnishmentsTab qc={queryClient} />}
      {tab === 'setup' && <SetupTab qc={queryClient} />}
    </div>
  )
}

export function EmployeesTab({ qc }: { qc: any }) {
  const [show, setShow] = useState(false)
  const [editEmp, setEditEmp] = useState<any>(null)
  const [form, setForm] = useState({ employeeCode: '', firstName: '', lastName: '', email: '', hireDate: '', employmentType: '0', status: '1' })
  const [selId, setSelId] = useState('')
  const [search, setSearch] = useState('')
  const { data = [], isLoading } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const mutation = useMutation({
    mutationFn: () => createEmployee({ companyId: currentCompanyId(), ...form, employmentType: 0, hireDate: new Date().toISOString().slice(0, 10), isBillable: true }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['payroll', 'employees'] }) },
  })
  const updateMut = useMutation({
    mutationFn: () => editEmployee(editEmp.id, { firstName: form.firstName, lastName: form.lastName, email: form.email, status: Number(form.status) }),
    onSuccess: () => { setShow(false); setEditEmp(null); qc.invalidateQueries({ queryKey: ['payroll', 'employees'] }) },
  })

  const { data: dds = [], refetch: refetchDd } = useQuery({ queryKey: ['payroll', 'dd', selId], queryFn: () => getDirectDeposits(selId), enabled: !!selId })
  const [ddForm, setDdForm] = useState({ bankName: '', routingNumber: '', accountNumber: '', accountType: 'Checking', allocationPercentage: '', fixedAmount: '', isRemainder: false })
  const ddMut = useMutation({
    mutationFn: () => createDirectDeposit(selId, { companyId: currentCompanyId(), ...ddForm, allocationPercentage: ddForm.allocationPercentage ? Number(ddForm.allocationPercentage) : null, fixedAmount: ddForm.fixedAmount ? Number(ddForm.fixedAmount) : null }),
    onSuccess: () => { setDdForm({ bankName: '', routingNumber: '', accountNumber: '', accountType: 'Checking', allocationPercentage: '', fixedAmount: '', isRemainder: false }); refetchDd() },
  })
  const delMut = useMutation({ mutationFn: (id: string) => deleteDirectDeposit(id), onSuccess: () => refetchDd() })
  const prenoteMut = useMutation({
    mutationFn: (p: { employeeId: string; id: string }) => sendPrenote(p.employeeId, p.id),
    onSuccess: () => refetchDd(),
  })
  const verifyMut = useMutation({
    mutationFn: (p: { employeeId: string; id: string }) => verifyDirectDeposit(p.employeeId, p.id),
    onSuccess: () => refetchDd(),
  })
  const filtered = (data as any[]).filter((e: any) => {
    const q = search.trim().toLowerCase()
    if (!q) return true
    return (e.employeeCode ?? '').toLowerCase().includes(q) || (e.fullName ?? '').toLowerCase().includes(q) || (e.email ?? '').toLowerCase().includes(q)
  })

  const cols: DataTableColumn<any>[] = [
    { key: 'employeeCode', header: 'Code' },
    { key: 'fullName', header: 'Name' },
    { key: 'email', header: 'Email' },
    { key: 'status', header: 'Status', render: (r: any) => (
      <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${r.status === 1 || r.status === 'Active' ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>{r.status === 1 || r.status === 'Active' ? 'Active' : 'Inactive'}</span>
    )},
    { key: 'id', header: '', render: (_: unknown, r: any) => (
      <Button size="sm" variant="outline" onClick={() => {
        setEditEmp(r)
        setForm({ employeeCode: r.employeeCode, firstName: r.firstName ?? '', lastName: r.lastName ?? '', email: r.email ?? '', hireDate: r.hireDate?.slice(0, 10) ?? '', employmentType: String(r.employmentType ?? 0), status: String(r.status ?? 1) })
        setShow(true)
      }}>Edit</Button>
    )},
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Employee</Button></div>
      <Input label="Search" value={search} onChange={(e: any) => setSearch(e.target.value)} placeholder="Search employees..." />
      <DataTable columns={cols} data={filtered as any[]} loading={isLoading}
        onRowClick={(row: any) => setSelId(row.id)} />
      {selId && (
        <div className="border rounded p-3 space-y-2">
          <div className="font-semibold">Direct Deposit Accounts</div>
          <DataTable columns={[
            { key: 'bankName', header: 'Bank' },
            { key: 'routingNumber', header: 'Routing' },
            { key: 'maskedAccount', header: 'Account' },
            { key: 'accountType', header: 'Type' },
            { key: 'allocationPercentage', header: '%', align: 'right', render: (v: any) => v != null ? `${v}%` : '' },
            { key: 'fixedAmount', header: 'Fixed', align: 'right', render: (v: any) => v != null ? MONEY(v) : '' },
            { key: 'isRemainder', header: 'Remainder', render: (v: boolean) => (v ? 'Yes' : '') },
            { key: 'prenoteSentOn', header: 'Prenote Sent', render: (v: any) => v ? String(v).slice(0, 10) : '—' },
            { key: 'verifiedOn', header: 'Verified', render: (v: any) => v ? String(v).slice(0, 10) : '—' },
            { key: 'id', header: '', render: (_: unknown, r: any) => (
              <div className="flex gap-1">
                {!r.verifiedOn && <Button size="sm" variant="outline" onClick={() => prenoteMut.mutate({ employeeId: selId, id: r.id })}>Send Prenote</Button>}
                {r.prenoteSentOn && !r.verifiedOn && <Button size="sm" onClick={() => verifyMut.mutate({ employeeId: selId, id: r.id })}>Verify</Button>}
                <Button size="sm" variant="destructive" onClick={() => delMut.mutate(r.id)}><Trash2 className="h-3.5 w-3.5" /></Button>
              </div>
            )},
          ]} data={dds as any[]} emptyMessage="No direct deposit accounts." />
          <div className="grid grid-cols-2 md:grid-cols-3 gap-2 items-end border-t pt-2">
            <Input label="Bank Name" value={ddForm.bankName} onChange={(v: any) => setDdForm({ ...ddForm, bankName: v })} />
            <Input label="Routing #" value={ddForm.routingNumber} onChange={(v: any) => setDdForm({ ...ddForm, routingNumber: v })} />
            <Input label="Account #" value={ddForm.accountNumber} onChange={(v: any) => setDdForm({ ...ddForm, accountNumber: v })} />
            <Input label="Type" value={ddForm.accountType} onChange={(v: any) => setDdForm({ ...ddForm, accountType: v })} />
            <Input type="number" label="% Alloc" value={ddForm.allocationPercentage} onChange={(v: any) => setDdForm({ ...ddForm, allocationPercentage: v })} />
            <Input type="number" label="Fixed $" value={ddForm.fixedAmount} onChange={(v: any) => setDdForm({ ...ddForm, fixedAmount: v })} />
            <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={ddForm.isRemainder} onChange={(e: any) => setDdForm({ ...ddForm, isRemainder: e.target.checked })} /> Remainder</label>
            <Button onClick={() => ddMut.mutate()} disabled={ddMut.isPending}>Add Account</Button>
          </div>
        </div>
      )}
      <Modal isOpen={show} onClose={() => { setShow(false); setEditEmp(null) }} title={editEmp ? 'Edit Employee' : 'New Employee'}>
        <div className="space-y-2">
          {!editEmp && <Input label="Code" value={form.employeeCode} onChange={(e: any) => setForm({ ...form, employeeCode: e.target.value })} />}
          <div className="grid grid-cols-2 gap-2">
            <Input label="First Name" value={form.firstName} onChange={(e: any) => setForm({ ...form, firstName: e.target.value })} />
            <Input label="Last Name" value={form.lastName} onChange={(e: any) => setForm({ ...form, lastName: e.target.value })} />
          </div>
          <Input label="Email" value={form.email} onChange={(e: any) => setForm({ ...form, email: e.target.value })} />
          {!editEmp && <>
            <Input label="Hire Date" type="date" value={form.hireDate} onChange={(e: any) => setForm({ ...form, hireDate: e.target.value })} />
            <Select label="Employment Type" value={form.employmentType} onChange={(e: any) => setForm({ ...form, employmentType: e.target.value })} options={[{ value: '0', label: 'Full-Time' }, { value: '1', label: 'Part-Time' }, { value: '2', label: 'Contractor' }]} />
          </>}
          {editEmp && <Select label="Status" value={form.status} onChange={(e: any) => setForm({ ...form, status: e.target.value })} options={[{ value: '1', label: 'Active' }, { value: '2', label: 'Inactive' }, { value: '3', label: 'Terminated' }]} />}
          {mutation.isError && <p className="text-sm text-red-600">{getErrorMessage(mutation.error)}</p>}
          <Button onClick={() => editEmp ? updateMut.mutate() : mutation.mutate()} disabled={mutation.isPending || updateMut.isPending}>{editEmp ? 'Save' : 'Create'}</Button>
        </div>
      </Modal>
    </div>
  )
}

export function PayCodesTab({ qc }: { qc: any }) {
  const [show, setShow] = useState(false)
  const [editPC, setEditPC] = useState<any>(null)
  const [form, setForm] = useState({ code: '', description: '', type: '0', glAccountNumber: '6000' })
  const { data = [], isLoading } = useQuery({ queryKey: ['payroll', 'paycodes'], queryFn: () => getPayCodes() })
  const { data: glAccounts = [] } = useQuery({ queryKey: ['platform', 'accounts'], queryFn: () => getAccounts() })
  const glOptions = (glAccounts as any[]).map((a: any) => ({ value: a.accountNumber, label: `${a.accountNumber} - ${a.description}` }))
  const mutation = useMutation({
    mutationFn: () => createPayCode({ companyId: currentCompanyId(), code: form.code, description: form.description, type: Number(form.type), glAccountNumber: form.glAccountNumber }),
    onSuccess: () => { setShow(false); setEditPC(null); qc.invalidateQueries({ queryKey: ['payroll', 'paycodes'] }) },
  })
  const updateMut = useMutation({
    mutationFn: () => editPayCode(editPC.id, { description: form.description, glAccountNumber: form.glAccountNumber }),
    onSuccess: () => { setShow(false); setEditPC(null); qc.invalidateQueries({ queryKey: ['payroll', 'paycodes'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'code', header: 'Code' },
    { key: 'description', header: 'Description' },
    { key: 'type', header: 'Type' },
    { key: 'glAccountNumber', header: 'GL Account' },
    { key: 'id', header: '', render: (_: unknown, r: any) => (
      <Button size="sm" variant="outline" onClick={() => { setEditPC(r); setForm({ code: r.code, description: r.description, type: String(r.type), glAccountNumber: r.glAccountNumber ?? '6000' }); setShow(true) }}>Edit</Button>
    )},
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => { setEditPC(null); setForm({ code: '', description: '', type: '0', glAccountNumber: '6000' }); setShow(true) }}><Plus className="h-4 w-4" /> New Pay Code</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => { setShow(false); setEditPC(null) }} title={editPC ? 'Edit Pay Code' : 'New Pay Code'}>
        <div className="space-y-2">
          <Input label="Code" value={form.code} onChange={(e: any) => setForm({ ...form, code: e.target.value })} disabled={!!editPC} />
          <Input label="Description" value={form.description} onChange={(e: any) => setForm({ ...form, description: e.target.value })} />
          <Select label="Type" value={form.type} onChange={(e: any) => setForm({ ...form, type: e.target.value })} options={[{ value: '0', label: 'Earnings' }, { value: '1', label: 'Deduction' }, { value: '2', label: 'Tax' }]} disabled={!!editPC} />
          <Select label="GL Account" value={form.glAccountNumber} onChange={(e: any) => setForm({ ...form, glAccountNumber: e.target.value })} options={glOptions} placeholder="Select account..." />
          <Button onClick={() => editPC ? updateMut.mutate() : mutation.mutate()} disabled={mutation.isPending || updateMut.isPending}>{editPC ? 'Save' : 'Create'}</Button>
        </div>
      </Modal>
    </div>
  )
}

export function UnionTab() {
  const [show, setShow] = useState(false)
  const [validate, setValidate] = useState({ trade: 'Electrician', jurisdiction: 'CA', wage: '55' })
  const [showValidate, setShowValidate] = useState(false)
  const [form, setForm] = useState({ tradeClassification: 'Electrician', jurisdiction: 'CA', prevailingWageRate: '50', fringeBenefitRate: '10' })
  const { data = [], isLoading } = useQuery({ queryKey: ['payroll', 'union'], queryFn: () => getUnionProfiles() })
  const createMut = useMutation({ mutationFn: () => createUnionProfile({ companyId: currentCompanyId(), tradeClassification: form.tradeClassification, jurisdiction: form.jurisdiction || null, prevailingWageRate: Number(form.prevailingWageRate), fringeBenefitRate: Number(form.fringeBenefitRate) }), onSuccess: () => setShow(false) })
  const validateQ = useQuery({
    queryKey: ['payroll', 'validate', validate.trade, validate.jurisdiction, validate.wage],
    queryFn: () => validatePrevailingWage(validate.trade, validate.jurisdiction || null, Number(validate.wage)),
    enabled: showValidate,
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'tradeClassification', header: 'Trade' },
    { key: 'jurisdiction', header: 'Jurisdiction' },
    { key: 'prevailingWageRate', header: 'Prevailing Wage', render: (r: any) => MONEY(r.prevailingWageRate) },
    { key: 'fringeBenefitRate', header: 'Fringe', render: (r: any) => MONEY(r.fringeBenefitRate) },
    { key: 'totalPrevailingRate', header: 'Total', render: (r: any) => MONEY(r.totalPrevailingRate) },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end gap-2">
        <Button variant="secondary" onClick={() => { setShowValidate(true); setValidate({ ...validate }) }}>Validate Prevailing Wage</Button>
        <Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Profile</Button>
      </div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Union / Certified Profile">
        <div className="space-y-2">
          <Input label="Trade Classification" value={form.tradeClassification} onChange={(e: any) => setForm({ ...form, tradeClassification: e.target.value })} />
          <Input label="Jurisdiction" value={form.jurisdiction} onChange={(e: any) => setForm({ ...form, jurisdiction: e.target.value })} />
          <Input type="number" label="Prevailing Wage Rate" value={form.prevailingWageRate} onChange={(v: any) => setForm({ ...form, prevailingWageRate: v })} />
          <Input type="number" label="Fringe Benefit Rate" value={form.fringeBenefitRate} onChange={(v: any) => setForm({ ...form, fringeBenefitRate: v })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Create</Button>
        </div>
      </Modal>
      <Modal isOpen={showValidate} onClose={() => setShowValidate(false)} title="Prevailing Wage Validation (Davis-Bacon)">
        <div className="space-y-2">
          <Input label="Trade" value={validate.trade} onChange={(e: any) => setValidate({ ...validate, trade: e.target.value })} />
          <Input label="Jurisdiction" value={validate.jurisdiction} onChange={(e: any) => setValidate({ ...validate, jurisdiction: e.target.value })} />
          <Input type="number" label="Actual Wage" value={validate.wage} onChange={(v: any) => setValidate({ ...validate, wage: v })} />
          {validateQ.isLoading && <p className="text-sm">Checking…</p>}
          {(validateQ.data as any) && (
            <div className={`p-2 rounded ${(validateQ.data as any).meetsRate ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'}`}>
              {(validateQ.data as any).found
                ? `Prevailing ${MONEY((validateQ.data as any).prevailingWageRate)} + fringe ${MONEY((validateQ.data as any).fringeBenefitRate)} = ${MONEY((validateQ.data as any).totalPrevailingRate)}. Actual ${MONEY((validateQ.data as any).actualWage)} → ${(validateQ.data as any).meetsRate ? 'MEETS' : 'BELOW'} prevailing rate.`
                : 'No prevailing-wage profile found for this trade/jurisdiction.'}
            </div>
          )}
        </div>
      </Modal>
    </div>
  )
}

export function TimesheetsTab({ qc }: { qc: any }) {
  const [employeeId, setEmployeeId] = useState('')
  const [weekEnding, setWeekEnding] = useState(new Date().toISOString().slice(0, 10))
  const [lines, setLines] = useState<{ payCodeId: string; hours: number; projectId: string; taskId: string; description: string }[]>([{ payCodeId: '', hours: 8, projectId: '', taskId: '', description: '' }])
  const [msg, setMsg] = useState('')
  const [tsId, setTsId] = useState<string | null>(null)
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const { data: payCodes = [] } = useQuery({ queryKey: ['payroll', 'paycodes'], queryFn: () => getPayCodes() })
  const { data: projects = [] } = useQuery({ queryKey: ['projects'], queryFn: () => getProjects() })

  // Collect unique project IDs across all lines to prefetch tasks
  const lineProjectIds = [...new Set(lines.map(l => l.projectId).filter(Boolean))]
  // For simplicity, we fetch tasks per-project only when user picks a project
  // Store tasks map: projectId -> tasks[]
  const [taskMap, setTaskMap] = useState<Record<string, any[]>>({})
  const fetchTasks = async (projId: string) => {
    if (!projId || taskMap[projId]) return
    try {
      const tasks = await getProjectTasks(projId)
      setTaskMap(prev => ({ ...prev, [projId]: tasks as any[] }))
    } catch { /* ignore */ }
  }

  const createMut = useMutation({
    mutationFn: async () => {
      const ts: any = await createTimesheet({ companyId: currentCompanyId(), employeeId, weekEnding })
      const id = ts.data ?? ts
      setTsId(id)
      for (const line of lines) {
        if (line.payCodeId && line.hours > 0) {
          await addTimesheetLine(id, {
            payCodeId: line.payCodeId,
            hours: line.hours,
            projectId: line.projectId || null,
            taskId: line.taskId || null,
            description: line.description || null,
          })
        }
      }
      await submitTimesheet(id, employeeId)
      await approveTimesheet(id, employeeId)
      return id
    },
    onSuccess: (id: any) => { setMsg(`Timesheet ${id} created, submitted, and approved → labor cost posted to Project Accounting + GL.`); qc.invalidateQueries({ queryKey: ['payroll', 'employees'] }) },
    onError: (e) => setMsg(getErrorMessage(e)),
  })

  function addLine() { setLines(prev => [...prev, { payCodeId: '', hours: 8, projectId: '', taskId: '', description: '' }]) }
  function removeLine(i: number) { setLines(prev => prev.filter((_, idx) => idx !== i)) }
  function updateLine(i: number, field: string, value: any) {
    setLines(prev => prev.map((l, idx) => idx === i ? { ...l, [field]: value } : l))
  }

  const payCodeOpts = (payCodes as any[]).map((p: any) => ({ value: p.id, label: `${p.code} - ${p.description}` }))
  const projectOpts = (projects as any[]).map((p: any) => ({ value: p.id, label: `${p.projectCode} - ${p.name}` }))

  return (
    <div className="space-y-3 max-w-2xl">
      <p className="text-sm text-gray-600">Create a timesheet with line entries (pay code, hours, project, task). Submitting + approving posts labor cost to Project Accounting and GL.</p>
      <div className="grid grid-cols-2 gap-2">
        <Select label="Employee" value={employeeId} onChange={(e: any) => setEmployeeId(e.target.value)} options={(employees as any[]).map((e: any) => ({ value: e.id, label: `${e.employeeCode} — ${e.fullName}` }))} />
        <Input label="Week Ending" type="date" value={weekEnding} onChange={(e: any) => setWeekEnding(e.target.value)} />
      </div>
      <div className="space-y-2">
        <div className="flex justify-between items-center">
          <span className="text-sm font-medium">Timesheet Lines</span>
          <Button size="sm" variant="outline" onClick={addLine}>+ Add Line</Button>
        </div>
        {lines.map((line, i) => {
          const lineTasks = line.projectId ? (taskMap[line.projectId] || []) : []
          return (
          <div key={i} className="border rounded p-2 space-y-2">
            <div className="grid grid-cols-3 gap-2 items-end">
              <Select label="Pay Code" value={line.payCodeId} onChange={(e: any) => updateLine(i, 'payCodeId', e.target.value)} options={payCodeOpts} placeholder="Select..." />
              <Input label="Hours" type="number" step="0.25" value={line.hours} onChange={(e: any) => updateLine(i, 'hours', Number(e.target.value))} />
              <Select label="Project (billable)" value={line.projectId} onChange={(e: any) => { updateLine(i, 'projectId', e.target.value); if (e.target.value) fetchTasks(e.target.value); updateLine(i, 'taskId', ''); }} options={[{ value: '', label: '— None —' }, ...projectOpts]} placeholder="Optional..." />
            </div>
            <div className="grid grid-cols-2 gap-2 items-end">
              <Select label="Task" value={line.taskId} onChange={(e: any) => updateLine(i, 'taskId', e.target.value)} options={[{ value: '', label: '— None —' }, ...lineTasks.map((t: any) => ({ value: t.id, label: `${t.taskCode} - ${t.description}` }))]} placeholder="Select task..." />
              <Input label="Description" value={line.description} onChange={(e: any) => updateLine(i, 'description', e.target.value)} placeholder="Line description..." />
            </div>
            {lines.length > 1 && <Button size="sm" variant="ghost" className="text-red-600" onClick={() => removeLine(i)}>Remove</Button>}
          </div>
          )
        })}
      </div>
      <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !employeeId}>Create / Submit / Approve</Button>
      {msg && <p className="text-sm text-green-700">{msg}</p>}
    </div>
  )
}

export function RunsTab({ qc }: { qc: any }) {
  const [runId, setRunId] = useState('')
  const [report, setReport] = useState<any>(null)
  const [runForm, setRunForm] = useState({ calendarId: '', periodStart: new Date().toISOString().slice(0, 10), periodEnd: new Date().toISOString().slice(0, 10), payDate: new Date().toISOString().slice(0, 10) })
  const [postById, setPostById] = useState('')
  const [accrualDate, setAccrualDate] = useState(new Date().toISOString().slice(0, 10))
  const [reversalDate, setReversalDate] = useState('')
  const [checkDate, setCheckDate] = useState(new Date().toISOString().slice(0, 10))
  const [startingCheckNo, setStartingCheckNo] = useState('1001')
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const createMut = useMutation({
    mutationFn: () => createDraftRun({
      companyId: currentCompanyId(),
      calendarId: runForm.calendarId || '00000000-0000-0000-0000-000000000000',
      periodStart: runForm.periodStart,
      periodEnd: runForm.periodEnd,
      payDate: runForm.payDate,
    }),
    onSuccess: (r: any) => setRunId(r.data),
  })
  const postMut = useMutation({ mutationFn: () => postRun(runId, postById || currentCompanyId()), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'runs'] }) })
  const accrueMut = useMutation({ mutationFn: () => accrueRun(runId, accrualDate) })
  const reverseMut = useMutation({ mutationFn: () => reverseRun(runId, reversalDate || new Date().toISOString().slice(0, 10)) })
  const voidMut = useMutation({ mutationFn: () => voidRun(runId), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'runs'] }) })
  const printMut = useMutation({ mutationFn: () => printChecks(runId, { checkDate, startingCheckNumber: Number(startingCheckNo), directDeposit: true }) })
  const [nacha, setNacha] = useState<string | null>(null)
  const certQ = useQuery({ queryKey: ['payroll', 'certified', runId], queryFn: () => getCertifiedPayroll(runId), enabled: !!runId })
  const nachaQ = useQuery({ queryKey: ['payroll', 'nacha', runId], queryFn: () => getNachaFile(runId), enabled: false })
  const loadNacha = () => { nachaQ.refetch().then((r) => setNacha((r.data as any)?.data ?? null)) }
  const cid = currentCompanyId()
  const depositsQ = useQuery({ queryKey: ['payroll', 'tax-deposits', cid], queryFn: () => getTaxDeposits(cid) })
  const genMut = useMutation({ mutationFn: () => generateTaxDeposits(runId, 'Monthly'), onSuccess: () => depositsQ.refetch() })
  const depMut = useMutation({ mutationFn: (id: string) => markTaxDeposited(id, { depositedAmount: 0, depositedOn: new Date().toISOString().slice(0, 10) }), onSuccess: () => depositsQ.refetch() })

  return (
    <div className="space-y-3 max-w-2xl">
      <div className="border rounded p-3 space-y-2">
        <div className="font-semibold text-sm">Run Parameters</div>
        <div className="grid grid-cols-2 gap-2">
          <Input label="Period Start" type="date" value={runForm.periodStart} onChange={(e: any) => setRunForm(p => ({ ...p, periodStart: e.target.value }))} />
          <Input label="Period End" type="date" value={runForm.periodEnd} onChange={(e: any) => setRunForm(p => ({ ...p, periodEnd: e.target.value }))} />
          <Input label="Pay Date" type="date" value={runForm.payDate} onChange={(e: any) => setRunForm(p => ({ ...p, payDate: e.target.value }))} />
          <Input label="Calendar Id (optional)" value={runForm.calendarId} onChange={(e: any) => setRunForm(p => ({ ...p, calendarId: e.target.value }))} placeholder="Leave empty for auto" />
        </div>
        <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Build Draft Run</Button>
      </div>
      <div className="flex gap-2 flex-wrap">
        <Button variant="secondary" disabled={!runId || postMut.isPending} onClick={() => postMut.mutate()}>Post Run (→ GL)</Button>
        <Button variant="secondary" disabled={!runId || accrueMut.isPending} onClick={() => accrueMut.mutate()}>Accrue</Button>
        <Button variant="secondary" disabled={!runId || reverseMut.isPending} onClick={() => reverseMut.mutate()}>Reverse</Button>
        <Button variant="secondary" disabled={!runId || voidMut.isPending} onClick={() => voidMut.mutate()}>Void (draft)</Button>
        <Button variant="secondary" disabled={!runId || printMut.isPending} onClick={() => printMut.mutate()}>Print Checks</Button>
        <Button variant="secondary" disabled={!runId} onClick={loadNacha}>NACHA File</Button>
        <Button variant="secondary" disabled={!runId} onClick={() => setReport((certQ.data as any))}>Certified Payroll Report</Button>
        <Button variant="secondary" disabled={!runId || genMut.isPending} onClick={() => genMut.mutate()}>Generate Tax Deposits</Button>
      </div>
      {runId && <p className="text-sm">Run: <code>{runId}</code></p>}
      {createMut.data && <p className="text-sm text-green-700">Draft run created from approved timesheets.</p>}
      {postMut.isSuccess && <p className="text-sm text-green-700">Run posted → GL wage/payroll-liability entries.</p>}
      {accrueMut.isSuccess && <p className="text-sm text-green-700">Accrual posted.</p>}
      {reverseMut.isSuccess && <p className="text-sm text-green-700">Run reversed (negating GL entry).</p>}
      {report && (
        <div className="border rounded p-3 text-sm space-y-1">
          <div className="font-semibold">Certified Payroll (Davis-Bacon)</div>
          <div>Period: {report.periodStart?.slice(0, 10)} → {report.periodEnd?.slice(0, 10)} | Pay: {report.payDate?.slice(0, 10)}</div>
          <div>Total Gross: {MONEY(report.totalGross)} | Total Fringe: {MONEY(report.totalFringe)}</div>
          {(report.rows || []).map((r: any, i: number) => (
            <div key={i} className="flex justify-between border-t pt-1">
              <span>{r.employeeName} — {r.tradeClassification} ({r.totalHours}h)</span>
              <span>{MONEY(r.grossWage)} | fringe {MONEY(r.fringeCost)} | {r.meetsPrevailing ? 'MEETS' : 'BELOW'} prevailing</span>
            </div>
          ))}
        </div>
      )}
      {nacha && (
        <div className="border rounded p-3 text-sm">
          <div className="font-semibold mb-1">NACHA PPD File (first 600 chars)</div>
          <pre className="text-[11px] whitespace-pre-wrap max-h-40 overflow-auto">{nacha.slice(0, 600)}</pre>
        </div>
      )}
      <div className="border rounded p-3 space-y-2">
        <div className="font-semibold">Scheduled Tax Deposits (EFTPS)</div>
        <DataTable columns={[
          { key: 'taxType', header: 'Type' },
          { key: 'agency', header: 'Agency' },
          { key: 'depositDate', header: 'Due', render: (r: any) => r.depositDate?.slice(0, 10) },
          { key: 'estimatedAmount', header: 'Est.', align: 'right', render: (v: any) => MONEY(v) },
          { key: 'formType', header: 'Form' },
          { key: 'deposited', header: 'Status', render: (v: boolean) => (v ? 'Deposited' : 'Open') },
          { key: 'id', header: '', render: (_: unknown, r: any) => !r.deposited ? <Button size="sm" onClick={() => depMut.mutate(r.id)}>Mark Dep.</Button> : null },
        ]} data={(depositsQ.data as any)?.data || []} emptyMessage="No tax deposits scheduled. Post a run and click Generate Tax Deposits." />
      </div>
      <LiabilityPaymentsSection />
    </div>
  )
}

function LiabilityPaymentsSection() {
  const qc = useQueryClient()
  const cid = currentCompanyId()
  const [results, setResults] = useState<any[] | null>(null)
  const pendingQ = useQuery({ queryKey: ['payroll', 'liability-pending', cid], queryFn: () => getPendingLiabilityPayments(cid) })
  const payMut = useMutation({
    mutationFn: () => payLiabilities({ companyId: cid, payThroughDate: new Date().toISOString().slice(0, 10) }),
    onSuccess: (r: any) => {
      setResults((r?.data as any[]) || [])
      qc.invalidateQueries({ queryKey: ['payroll', 'liability-pending', cid] })
      qc.invalidateQueries({ queryKey: ['payroll', 'tax-deposits', cid] })
    },
  })
  const groups = ((pendingQ.data as any)?.data || []) as any[]
  return (
    <div className="border rounded p-3 space-y-2">
      <div className="font-semibold">Payroll Liability Payments (via AP vouchers)</div>
      <DataTable columns={[
        { key: 'vendorCode', header: 'Vendor' },
        { key: 'agencyName', header: 'Agency' },
        { key: 'kind', header: 'Kind' },
        { key: 'amount', header: 'Amount', align: 'right', render: (v: any) => MONEY(v) },
        { key: 'depositCount', header: 'Items' },
        { key: 'uncoveredPostedRunCount', header: 'Unsched. Runs', render: (v: number) => v || '' },
      ]} data={groups} loading={pendingQ.isLoading} emptyMessage="No unpaid liabilities due." />
      <div className="flex gap-2">
        <Button variant="secondary" disabled={groups.length === 0 || payMut.isPending} onClick={() => payMut.mutate()}>Pay Liabilities (create AP vouchers)</Button>
      </div>
      {(payMut.isError) && <p className="text-sm text-red-600">{getErrorMessage(payMut.error)}</p>}
      {payMut.isSuccess && <p className="text-sm text-green-700">AP vouchers posted; GL payroll liabilities relieved through the AP posting path.</p>}
      {results && results.length > 0 && (
        <div className="border rounded p-2 text-sm space-y-1">
          <div className="font-semibold">Payment Summary</div>
          {results.map((r, i) => (
            <div key={i} className="flex justify-between border-t pt-1">
              <span>{r.vendorCode} — {r.agencyName}</span>
              <span>{MONEY(r.amount)} | voucher {String(r.voucherId).slice(0, 8)}…</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export function ExpensesTab({ qc }: { qc: any }) {
  const [reportId, setReportId] = useState('')
  const [form, setForm] = useState({ employeeId: '', description: '' })
  const [line, setLine] = useState({ expenseType: '0', amount: '0', description: 'Mileage', miles: '100', perDiemDays: '0', projectId: '' })
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const createMut = useMutation({ mutationFn: () => createExpenseReport({ companyId: currentCompanyId(), employeeId: form.employeeId, description: form.description }), onSuccess: (r: any) => setReportId((r as any).data) })
  const addLineMut = useMutation({ mutationFn: () => addExpenseLine(reportId, line), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'expenses', reportId] }) })
  const submitMut = useMutation({ mutationFn: () => submitExpense(reportId) })
  const approveMut = useMutation({ mutationFn: () => approveExpense(reportId, { managerApproved: false }) })
  const reimburseMut = useMutation({ mutationFn: () => reimburseExpense(reportId), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'expenses', reportId] }) })
  return (
    <div className="space-y-3 max-w-2xl">
      <Select label="Employee" value={form.employeeId} onChange={(e: any) => setForm({ ...form, employeeId: e.target.value })} options={(employees as any[]).map((e: any) => ({ value: e.id, label: `${e.employeeCode} — ${e.fullName}` }))} />
      <Input label="Description" value={form.description} onChange={(e: any) => setForm({ ...form, description: e.target.value })} />
      <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.employeeId}>Create Report</Button>
      {reportId && (
        <div className="space-y-2 border rounded p-3">
          <div className="font-semibold">Add Line (mileage/per-diem auto-compute on server)</div>
          <Select label="Type" value={line.expenseType} onChange={(e: any) => setLine({ ...line, expenseType: e.target.value })} options={[{ value: '0', label: 'Mileage' }, { value: '1', label: 'Meals' }, { value: '2', label: 'Lodging' }, { value: '3', label: 'Per Diem' }, { value: '4', label: 'Other' }]} />
          <Input type="number" label="Amount" value={line.amount} onChange={(v: any) => setLine({ ...line, amount: v })} />
          <Input type="number" label="Miles (mileage)" value={line.miles} onChange={(v: any) => setLine({ ...line, miles: v })} />
          <Input type="number" label="Per-Diem Days" value={line.perDiemDays} onChange={(v: any) => setLine({ ...line, perDiemDays: v })} />
          <Input label="Project ID (billable)" value={line.projectId} onChange={(e: any) => setLine({ ...line, projectId: e.target.value })} />
          <Button onClick={() => addLineMut.mutate()} disabled={addLineMut.isPending}>Add Line</Button>
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => submitMut.mutate()} disabled={submitMut.isPending}>Submit</Button>
            <Button variant="secondary" onClick={() => approveMut.mutate()} disabled={approveMut.isPending}>Approve</Button>
            <Button variant="secondary" onClick={() => reimburseMut.mutate()} disabled={reimburseMut.isPending}>Reimburse (→ GL + Project)</Button>
          </div>
        </div>
      )}
      {reimburseMut.isSuccess && <p className="text-sm text-green-700">Reimbursed → Expense GL (Dr 6000 / Cr 2200) + billable lines post to Project Accounting.</p>}
    </div>
  )
}

export function TaxTab({ qc }: { qc: any }) {
  const [employeeId, setEmployeeId] = useState('')
  const [w4, setW4] = useState({ filingStatus: '0', multipleJobs: false, dependentsCredit: '2000', otherIncome: '0', deductions: '0' })
  const [calc, setCalc] = useState({ taxableWages: '2000', payFrequency: '2' })
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const { data: taxTables = [] } = useQuery({ queryKey: ['payroll', 'tax-tables'], queryFn: () => getTaxTables(currentCompanyId()) })
  const { data: jurisdictions = [] } = useQuery({ queryKey: ['payroll', 'tax-jurisdictions'], queryFn: () => getTaxJurisdictions(currentCompanyId()) })
  const [profile, setProfile] = useState({ residentState: '', workState: '', addFed: '0', addState: '0', exemptFed: false, exemptState: false })
  const { data: empProfile } = useQuery({ queryKey: ['payroll', 'tax-profile', employeeId], queryFn: () => getEmployeeTaxProfile(employeeId, currentCompanyId()), enabled: !!employeeId })
  const createMut = useMutation({ mutationFn: () => createW4(employeeId, { filingStatus: Number(w4.filingStatus), multipleJobs: w4.multipleJobs, dependentsCredit: Number(w4.dependentsCredit), otherIncome: Number(w4.otherIncome), deductions: Number(w4.deductions) }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'w4', employeeId] }) })
  const profileMut = useMutation({
    mutationFn: () => empProfile
      ? updateEmployeeTaxProfile(employeeId, { companyId: currentCompanyId(), ...profile })
      : createEmployeeTaxProfile(employeeId, { companyId: currentCompanyId(), ...profile }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'tax-profile', employeeId] }),
  })
  const withholdQ = useQuery({ queryKey: ['payroll', 'withhold', employeeId, calc.taxableWages, calc.payFrequency], queryFn: () => computeWithholding(employeeId, Number(calc.taxableWages), Number(calc.payFrequency)), enabled: !!employeeId })

  // Seed profile fields from loaded employee profile.
  useEffect(() => { if (empProfile) setProfile({ residentState: empProfile.residentState ?? '', workState: empProfile.workState ?? '', addFed: String(empProfile.additionalFederalWithholding ?? 0), addState: String(empProfile.additionalStateWithholding ?? 0), exemptFed: empProfile.exemptFederal ?? false, exemptState: empProfile.exemptState ?? false }) }, [empProfile])

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="border rounded p-3 space-y-2">
          <div className="font-semibold">Tax Tables</div>
          {(taxTables as any[]).length === 0 ? <p className="text-sm text-gray-500">No tax tables configured.</p> : (
            <DataTable data={taxTables as any[]} columns={[
              { key: 'name', header: 'Name' },
              { key: 'level', header: 'Level' },
              { key: 'year', header: 'Year' },
              { key: 'bracketCount', header: 'Brackets', align: 'right' },
            ]} emptyMessage="No tax tables." />
          )}
        </div>
        <div className="border rounded p-3 space-y-2">
          <div className="font-semibold">Tax Jurisdictions</div>
          {(jurisdictions as any[]).length === 0 ? <p className="text-sm text-gray-500">No jurisdictions configured.</p> : (
            <DataTable data={jurisdictions as any[]} columns={[
              { key: 'code', header: 'Code' },
              { key: 'name', header: 'Name' },
              { key: 'level', header: 'Level' },
              { key: 'hasReciprocalAgreement', header: 'Reciprocal', render: (v: boolean) => (v ? 'Yes' : 'No') },
            ]} emptyMessage="No jurisdictions." />
          )}
        </div>
      </div>

      <Select label="Employee" value={employeeId} onChange={(e: any) => setEmployeeId(e.target.value)} options={(employees as any[]).map((e: any) => ({ value: e.id, label: `${e.employeeCode} — ${e.fullName}` }))} />
      {employeeId && (
        <>
          <div className="border rounded p-3 space-y-2 max-w-2xl">
            <div className="font-semibold">Employee Tax Profile</div>
            <Input label="Resident State" value={profile.residentState} onChange={(v: any) => setProfile({ ...profile, residentState: v })} />
            <Input label="Work State" value={profile.workState} onChange={(v: any) => setProfile({ ...profile, workState: v })} />
            <Input type="number" label="Additional Federal Withholding ($)" value={profile.addFed} onChange={(v: any) => setProfile({ ...profile, addFed: v })} />
            <Input type="number" label="Additional State Withholding ($)" value={profile.addState} onChange={(v: any) => setProfile({ ...profile, addState: v })} />
            <div className="flex gap-4">
              <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={profile.exemptFed} onChange={(e: any) => setProfile({ ...profile, exemptFed: e.target.checked })} /> Exempt Federal</label>
              <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={profile.exemptState} onChange={(e: any) => setProfile({ ...profile, exemptState: e.target.checked })} /> Exempt State</label>
            </div>
            <Button onClick={() => profileMut.mutate()} disabled={profileMut.isPending}>Save Tax Profile</Button>
          </div>
          <div className="border rounded p-3 space-y-2 max-w-2xl">
            <div className="font-semibold">2020+ W-4 (Pub 15-T Percentage Method)</div>
            <Select label="Filing Status" value={w4.filingStatus} onChange={(e: any) => setW4({ ...w4, filingStatus: e.target.value })} options={[{ value: '0', label: 'Single' }, { value: '1', label: 'Married' }, { value: '2', label: 'Head of Household' }]} />
            <Input type="number" label="Dependents Credit ($)" value={w4.dependentsCredit} onChange={(v: any) => setW4({ ...w4, dependentsCredit: v })} />
            <Input type="number" label="Other Income (annual)" value={w4.otherIncome} onChange={(v: any) => setW4({ ...w4, otherIncome: v })} />
            <Input type="number" label="Deductions (annual)" value={w4.deductions} onChange={(v: any) => setW4({ ...w4, deductions: v })} />
            <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Save W-4</Button>
          </div>
          <div className="border rounded p-3 space-y-2 max-w-2xl">
            <div className="font-semibold">Compute Withholding</div>
            <Input type="number" label="Taxable Wages" value={calc.taxableWages} onChange={(v: any) => setCalc({ ...calc, taxableWages: v })} />
            <Select label="Pay Frequency" value={calc.payFrequency} onChange={(e: any) => setCalc({ ...calc, payFrequency: e.target.value })} options={[{ value: '0', label: 'Weekly' }, { value: '1', label: 'Biweekly' }, { value: '2', label: 'Semimonthly' }, { value: '3', label: 'Monthly' }]} />
            {withholdQ.isLoading && <p className="text-sm">Computing…</p>}
            {(withholdQ.data as any) && (
              <div className="text-sm">
                Method: <b>{(withholdQ.data as any).method}</b> | FIT: <b>{MONEY((withholdQ.data as any).federalIncomeTax)}</b>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}

export function DeductionsTab({ qc }: { qc: any }) {
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ description: '', deductionType: '0', isPercent: false, percentRate: '0', flatAmount: '0', glAccountNumber: '2200' })
  const { data: items = [] } = useQuery({ queryKey: ['payroll', 'deductions'], queryFn: () => getDeductionBenefits() })
  const createMut = useMutation({
    mutationFn: () => createDeductionBenefit({
      companyId: currentCompanyId(),
      code: (form.description || 'DB').toUpperCase().replace(/[^A-Z0-9]/g, '').slice(0, 12) || 'DB',
      description: form.description,
      type: Number(form.deductionType),
      isPreTax: form.deductionType === '0',
      defaultRate: form.isPercent ? Number(form.percentRate) : Number(form.flatAmount),
      glAccountNumber: form.glAccountNumber,
    }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['payroll', 'deductions'] }); setOpen(false) },
  })
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setOpen(true)} disabled={createMut.isPending}><Plus className="h-4 w-4" /> New Deduction/Benefit</Button></div>
      <DataTable columns={[
        { key: 'description', header: 'Description' },
        { key: 'type', header: 'Type' },
        { key: 'isPreTax', header: 'Pre-Tax?', render: (v: any) => (v ? 'Yes' : 'No') },
        { key: 'defaultRate', header: 'Rate' },
        { key: 'glAccountNumber', header: 'GL' },
      ]} data={items as any[]} />
      <Modal isOpen={open} onClose={() => setOpen(false)} title="New Deduction/Benefit">
        <div className="space-y-2">
          <Input label="Description" value={form.description} onChange={(e: any) => setForm({ ...form, description: e.target.value })} />
          <Select label="Type" value={form.deductionType} onChange={(e: any) => setForm({ ...form, deductionType: e.target.value })} options={[{ value: '0', label: 'Pre-Tax' }, { value: '1', label: 'Post-Tax' }, { value: '2', label: 'Employer' }]} />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.isPercent} onChange={(e: any) => setForm({ ...form, isPercent: e.target.checked })} /> Rate is a percent</label>
          <Input type="number" label="Percent Rate" value={form.percentRate} onChange={(v: any) => setForm({ ...form, percentRate: v })} />
          <Input type="number" label="Flat Amount" value={form.flatAmount} onChange={(v: any) => setForm({ ...form, flatAmount: v })} />
          <Input label="GL Account" value={form.glAccountNumber} onChange={(e: any) => setForm({ ...form, glAccountNumber: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

export function PtoTab({ qc }: { qc: any }) {
  const [form, setForm] = useState({ employeeId: '', policyName: 'Vacation', accrualRate: '1.5', maxAccrual: '120', carryoverLimit: '40' })
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const { data: ledgers = [] } = useQuery({ queryKey: ['payroll', 'pto-ledgers'], queryFn: () => getPtoLedgers() })
  const createMut = useMutation({ mutationFn: () => createPtoLedger(form.employeeId, { policyName: form.policyName, accrualRate: Number(form.accrualRate), maxAccrual: Number(form.maxAccrual), carryoverLimit: Number(form.carryoverLimit) }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'pto-ledgers'] }) })
  return (
    <div className="space-y-3">
      <div className="border rounded p-3 space-y-2 max-w-lg">
        <div className="font-semibold">New PTO Ledger</div>
        <Select label="Employee" value={form.employeeId} onChange={(e: any) => setForm({ ...form, employeeId: e.target.value })} options={(employees as any[]).map((e: any) => ({ value: e.id, label: `${e.employeeCode} — ${e.fullName}` }))} />
        <Input label="Policy" value={form.policyName} onChange={(e: any) => setForm({ ...form, policyName: e.target.value })} />
        <Input type="number" label="Accrual Rate (hrs/period)" value={form.accrualRate} onChange={(v: any) => setForm({ ...form, accrualRate: v })} />
        <Input type="number" label="Max Accrual" value={form.maxAccrual} onChange={(v: any) => setForm({ ...form, maxAccrual: v })} />
        <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.employeeId}>Create</Button>
      </div>
      <DataTable columns={[{ key: 'policyName', header: 'Policy' }, { key: 'accrued', header: 'Accrued' }, { key: 'used', header: 'Used' }, { key: 'available', header: 'Available' }, { key: 'carryover', header: 'Carryover' }]} data={ledgers as any[]} />
    </div>
  )
}

export function ManualChecksTab({ qc }: { qc: any }) {
  const [form, setForm] = useState({ employeeId: '', payDate: '2026-08-20', grossPay: '500', netPay: '450', isDirectDeposit: false, checkNumber: '' })
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const createMut = useMutation({ mutationFn: () => createManualCheck({ companyId: currentCompanyId(), employeeId: form.employeeId, payDate: form.payDate, grossPay: Number(form.grossPay), netPay: Number(form.netPay), isDirectDeposit: form.isDirectDeposit, checkNumber: form.checkNumber || null }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'manual-checks'] }) })
  return (
    <div className="space-y-3 max-w-lg">
      <div className="border rounded p-3 space-y-2">
        <div className="font-semibold">Off-cycle Manual Check</div>
        <Select label="Employee" value={form.employeeId} onChange={(e: any) => setForm({ ...form, employeeId: e.target.value })} options={(employees as any[]).map((e: any) => ({ value: e.id, label: `${e.employeeCode} — ${e.fullName}` }))} />
        <Input label="Pay Date" type="date" value={form.payDate} onChange={(e: any) => setForm({ ...form, payDate: e.target.value })} />
        <Input type="number" label="Gross" value={form.grossPay} onChange={(v: any) => setForm({ ...form, grossPay: v })} />
        <Input type="number" label="Net" value={form.netPay} onChange={(v: any) => setForm({ ...form, netPay: v })} />
        <Input label="Check #" value={form.checkNumber} onChange={(e: any) => setForm({ ...form, checkNumber: e.target.value })} />
        <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !form.employeeId}>Create Manual Check</Button>
      </div>
    </div>
  )
}

export function ReportsTab({ qc }: { qc: any }) {
  const company = currentCompanyId()
  const [year, setYear] = useState('2026')
  const y = Number(year)
  const regQ = useQuery({ queryKey: ['payroll', 'report', 'register', company, y], queryFn: () => getPayrollRegister(company, y) })
  const sumQ = useQuery({ queryKey: ['payroll', 'report', 'summary', company, y], queryFn: () => getPayrollSummary(company, y) })
  const labQ = useQuery({ queryKey: ['payroll', 'report', 'labor', company], queryFn: () => getLaborDistribution(company) })
  const garQ = useQuery({ queryKey: ['payroll', 'report', 'garn', company], queryFn: () => getGarnishmentRegister(company) })
  const wbQ = useQuery({ queryKey: ['payroll', 'report', 'wb', company, y], queryFn: () => getWageBaseReport(company, y) })
  const ptoQ = useQuery({ queryKey: ['payroll', 'report', 'pto', company], queryFn: () => getPtoReport(company) })
  const ddQ = useQuery({ queryKey: ['payroll', 'report', 'dd', company], queryFn: () => getDirectDepositRegister(company) })
  const f940Q = useQuery({ queryKey: ['payroll', 'report', '940', company, y], queryFn: () => getForm940(company, y) })
  const [show, setShow] = useState(false)
  return (
    <div className="space-y-3">
      <div className="flex items-end gap-2">
        <Input type="number" label="Year" value={year} onChange={(v: any) => setYear(v)} />
        <Button variant="secondary" onClick={() => setShow(true)}>Open Report Drawer</Button>
      </div>
      {(sumQ.data as any) && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-sm">
          <div className="border rounded p-2">YTD Gross<br /><b>{MONEY((sumQ.data as any).totalGross)}</b></div>
          <div className="border rounded p-2">EE Tax<br /><b>{MONEY((sumQ.data as any).totalEmployeeTax)}</b></div>
          <div className="border rounded p-2">ER Tax<br /><b>{MONEY((sumQ.data as any).totalEmployerTax)}</b></div>
          <div className="border rounded p-2">Net<br /><b>{MONEY((sumQ.data as any).totalNet)}</b></div>
        </div>
      )}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Payroll Register</div>
          <DataTable columns={[{ key: 'payDate', header: 'Pay Date', render: (r: any) => r.payDate?.slice(0, 10) }, { key: 'status', header: 'Status' }, { key: 'totalGross', header: 'Gross', render: (r: any) => MONEY(r.totalGross) }, { key: 'lineCount', header: 'Lines' }]} data={(regQ.data as any)?.data || []} loading={regQ.isLoading} />
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Labor Distribution</div>
          <DataTable columns={[{ key: 'employeeId', header: 'Employee' }, { key: 'regularHours', header: 'Reg' }, { key: 'overtimeHours', header: 'OT' }, { key: 'grossWages', header: 'Gross', render: (r: any) => MONEY(r.grossWages) }]} data={(labQ.data as any)?.data || []} loading={labQ.isLoading} />
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Garnishment Register</div>
          <DataTable columns={[{ key: 'type', header: 'Type' }, { key: 'priority', header: 'Prio' }, { key: 'disposableIncomePercent', header: '% DI' }]} data={(garQ.data as any)?.data || []} loading={garQ.isLoading} />
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Wage Base</div>
          <DataTable columns={[{ key: 'employeeId', header: 'Employee' }, { key: 'ytdWages', header: 'YTD', render: (r: any) => MONEY(r.ytdWages) }, { key: 'ssPct', header: '% SS Cap' }, { key: 'futaMet', header: 'FUTA Met', render: (r: any) => (r.futaMet ? 'Yes' : 'No') }]} data={(wbQ.data as any)?.data || []} loading={wbQ.isLoading} />
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">PTO Report</div>
          <DataTable columns={[{ key: 'policyName', header: 'Policy' }, { key: 'accrued', header: 'Acc' }, { key: 'used', header: 'Used' }, { key: 'available', header: 'Avail' }]} data={(ptoQ.data as any)?.data || []} loading={ptoQ.isLoading} />
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Direct Deposit Register</div>
          <DataTable columns={[{ key: 'employeeId', header: 'Employee' }, { key: 'netPay', header: 'Net', render: (r: any) => MONEY(r.netPay) }, { key: 'achTraceNumber', header: 'ACH Trace' }]} data={(ddQ.data as any)?.data || []} loading={ddQ.isLoading} />
        </div>
      </div>
      {(f940Q.data as any) && (
        <div className="border rounded p-2 text-sm">Form 940 — FUTA wages: <b>{MONEY((f940Q.data as any).futaWages)}</b> | FUTA tax: <b>{MONEY((f940Q.data as any).futaTax)}</b></div>
      )}

      {/* Batch E: compliance & statutory reporting */}
      <ComplianceSection qc={qc} company={company} year={y} />

      {/* Batch F: extended reporting */}
      <BatchFReportsSection company={company} year={y} />

      {/* Batch G: tax-filing exports */}
      <TaxFilingExportsSection company={company} year={y} />
    </div>
  )
}

function TaxFilingExportsSection({ company, year }: { company: string; year: number }) {
  const [quarter, setQuarter] = useState('1')
  const [payload, setPayload] = useState<{ title: string; content: string } | null>(null)
  const loadExport = (kind: string, withQuarter: boolean) => {
    getTaxFilingExport(kind, company, year, withQuarter ? Number(quarter) : undefined)
      .then((r: any) => setPayload({ title: kind.toUpperCase(), content: String(r?.data?.data ?? r?.data ?? '') }))
  }
  return (
    <div className="border rounded p-3 space-y-2">
      <div className="font-semibold">Tax Filing Exports</div>
      <div className="flex gap-2 items-end flex-wrap">
        <Input type="number" label="Quarter" value={quarter} onChange={(v: any) => setQuarter(v)} />
        <Button size="sm" variant="secondary" onClick={() => loadExport('941', true)}>941 (Q)</Button>
        <Button size="sm" variant="secondary" onClick={() => loadExport('940', false)}>940 (Y)</Button>
        <Button size="sm" variant="secondary" onClick={() => loadExport('w2', false)}>W-2 (Y)</Button>
        <Button size="sm" variant="secondary" onClick={() => loadExport('w3', false)}>W-3 (Y)</Button>
        <Button size="sm" variant="secondary" onClick={() => loadExport('state-quarterly', true)}>State Quarterly (Q)</Button>
      </div>
      {payload && (
        <div>
          <div className="text-sm font-semibold mb-1">{payload.title} payload (first 1200 chars)</div>
          <pre className="text-[11px] whitespace-pre-wrap border rounded p-2 max-h-48 overflow-auto">{payload.content.slice(0, 1200)}</pre>
        </div>
      )}
    </div>
  )
}

const NOTE = 'mt-1 text-xs text-gray-500'
const DATE = (v: string | null | undefined) => (v ? v.slice(0, 10) : '—')

function BatchFReportsSection({ company, year }: { company: string; year: number }) {
  const [quarter, setQuarter] = useState('1')
  const qtr = Number(quarter)
  const taxQ = useQuery({ queryKey: ['payroll', 'report', 'tax-liab', company, year, qtr], queryFn: () => getTaxLiability(company, year, qtr) })
  const dedQ = useQuery({ queryKey: ['payroll', 'report', 'ded-reg', company], queryFn: () => getDeductionRegister(company) })
  const cpQ = useQuery({ queryKey: ['payroll', 'report', 'cp347', company], queryFn: () => getCertifiedPayrollWh347(company) })
  const tepQ = useQuery({ queryKey: ['payroll', 'report', 'time-exp', company], queryFn: () => getTimeExpenseByProject(company) })
  const earnQ = useQuery({ queryKey: ['payroll', 'report', 'earnings', company, year], queryFn: () => getEmployeeEarnings(company, year) })
  const w2Q = useQuery({ queryKey: ['payroll', 'report', 'w2rec', company, year], queryFn: () => getW2Reconciliation(company, year) })
  const f941Q = useQuery({ queryKey: ['payroll', 'report', 'f941rec', company, year, qtr], queryFn: () => getForm941Reconciliation(company, year, qtr) })
  const accrualQ = useQuery({ queryKey: ['payroll', 'report', 'accrual', company], queryFn: () => getPayrollAccrual(company) })
  const eftpsQ = useQuery({ queryKey: ['payroll', 'report', 'eftps', company], queryFn: () => getEftpsSchedule(company) })
  const achQ = useQuery({ queryKey: ['payroll', 'report', 'ach-ret', company], queryFn: () => getAchReturnReport(company) })
  const nhQ = useQuery({ queryKey: ['payroll', 'report', 'new-hire', company], queryFn: () => getNewHireReport(company) })
  const wcpQ = useQuery({ queryKey: ['payroll', 'report', 'wc-prem', company, year], queryFn: () => getWorkersCompPremium(company, year) })

  return (
    <div className="space-y-3 border-t pt-3 mt-3">
      <div className="font-semibold">Extended Reporting (Batch F)</div>
      <div className="flex items-end gap-2">
        <Select label="Quarter (tax liability / 941)" value={quarter} onChange={(e: any) => setQuarter(e.target.value)} options={[{ value: '1', label: 'Q1' }, { value: '2', label: 'Q2' }, { value: '3', label: 'Q3' }, { value: '4', label: 'Q4' }]} />
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Tax Liability ({year} Q{qtr})</div>
          <DataTable columns={[
            { key: 'jurisdiction', header: 'Jurisdiction' },
            { key: 'dueDate', header: 'Due', render: (r: any) => DATE(r.dueDate) },
            { key: 'amountOwed', header: 'Owed', align: 'right', render: (r: any) => MONEY(r.amountOwed) },
            { key: 'status', header: 'Status' },
          ]} data={taxQ.data?.rows || []} loading={taxQ.isLoading} />
          <p className={NOTE}>EE withheld {(taxQ.data as any)?.totalEmployeeTaxWithheld != null ? MONEY((taxQ.data as any).totalEmployeeTaxWithheld) : '—'} · ER accrued {(taxQ.data as any)?.totalEmployerTaxAccrued != null ? MONEY((taxQ.data as any).totalEmployerTaxAccrued) : '—'} · Depositor: {(taxQ.data as any)?.depositorStatus ?? '—'} · {(taxQ.data as any)?.dueDateHint ?? ''}</p>
          <p className={NOTE}>{taxQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Deduction / Benefit Register</div>
          <DataTable columns={[
            { key: 'type', header: 'Type' },
            { key: 'benefitCode', header: 'Code' },
            { key: 'employeeId', header: 'Employee' },
            { key: 'amount', header: 'Amount', render: (r: any) => MONEY(r.amount) },
            { key: 'percent', header: '%', render: (r: any) => (r.percent != null ? `${r.percent}%` : '') },
          ]} data={dedQ.data?.rows || []} loading={dedQ.isLoading} />
          <p className={NOTE}>Remittance due to vendors: <b>{MONEY(dedQ.data?.totalRemittanceDue ?? null)}</b></p>
          <p className={NOTE}>{dedQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Certified Payroll (WH-347 style)</div>
          <DataTable columns={[
            { key: 'employeeCode', header: 'Employee #' },
            { key: 'employeeName', header: 'Name' },
            { key: 'tradeClassification', header: 'Trade/Class' },
            { key: 'regularHours', header: 'Reg Hrs' },
            { key: 'overtimeHours', header: 'OT Hrs' },
            { key: 'baseRate', header: 'Base Rate', render: (r: any) => MONEY(r.baseRate) },
            { key: 'fringeCost', header: 'Fringe', render: (r: any) => MONEY(r.fringeCost) },
            { key: 'gross', header: 'Gross', render: (r: any) => MONEY(r.gross) },
            { key: 'meetsPrevailing', header: 'Prev.', render: (r: any) => (r.meetsPrevailing ? 'MEETS' : 'BELOW') },
          ]} data={cpQ.data?.rows || []} loading={cpQ.isLoading} />
          <p className={NOTE}>{cpQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Time &amp; Expense by Project</div>
          <DataTable columns={[
            { key: 'projectId', header: 'Project', render: (r: any) => r.projectId || '(unassigned)' },
            { key: 'hours', header: 'Hours' },
            { key: 'laborCost', header: 'Labor Cost', render: (r: any) => MONEY(r.laborCost) },
            { key: 'expenses', header: 'Expenses', render: (r: any) => MONEY(r.expenses) },
            { key: 'billableAmount', header: 'Billable', render: (r: any) => MONEY(r.billableAmount) },
          ]} data={tepQ.data?.rows || []} loading={tepQ.isLoading} />
          <p className={NOTE}>{tepQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Employee Earnings YTD ({year})</div>
          <DataTable columns={[
            { key: 'employeeCode', header: 'Code' },
            { key: 'employeeName', header: 'Name' },
            { key: 'gross', header: 'Gross', render: (r: any) => MONEY(r.gross) },
            { key: 'employeeTax', header: 'EE Tax', render: (r: any) => MONEY(r.employeeTax) },
            { key: 'deductions', header: 'Deduct', render: (r: any) => MONEY(r.deductions) },
            { key: 'net', header: 'Net', render: (r: any) => MONEY(r.net) },
          ]} data={earnQ.data?.rows || []} loading={earnQ.isLoading} />
          <p className={NOTE}>{earnQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">W-2 Reconciliation ({year})</div>
          {w2Q.data && (
            <div className="text-sm space-y-0.5">
              <div>W-2 wages: <b>{MONEY(w2Q.data.w2Wages)}</b> (runs {MONEY(w2Q.data.runWages)} + manual {MONEY(w2Q.data.manualCheckWages)})</div>
              <div>Expected GL wage expense: <b>{MONEY(w2Q.data.expectedGlWageExpense)}</b> | Variance placeholder: {MONEY(w2Q.data.variance)} | GL tie-out: {w2Q.data.glTieOutPending ? 'PENDING' : 'Done'}</div>
            </div>
          )}
          <p className={NOTE}>{w2Q.data?.assumption}</p>
          <p className={NOTE}>{w2Q.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Form 941 Reconciliation ({year} Q{qtr})</div>
          {f941Q.data && (
            <div className="text-sm space-y-0.5">
              <div>Total wages: <b>{MONEY(f941Q.data.totalWages)}</b></div>
              <div>FIT withheld (est): {MONEY(f941Q.data.federalIncomeTaxWithheldEstimated)}</div>
              <div>FICA EE (est): {MONEY(f941Q.data.employeeFicaEstimated)} | FICA ER (est): {MONEY(f941Q.data.employerFicaEstimated)}</div>
              <div>Actual aggregate EE tax: {MONEY(f941Q.data.employeeTaxWithheldActual)} | ER tax: {MONEY(f941Q.data.employerTaxActual)}</div>
              <div>GL tie-out: {f941Q.data.glTieOutPending ? 'PENDING' : 'Done'}</div>
            </div>
          )}
          <p className={NOTE}>{f941Q.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Payroll Accrual (unpaid earned wages)</div>
          {accrualQ.data && (
            <div className="text-sm">
              Accrued wages: <b>{MONEY(accrualQ.data.accruedWages)}</b> · ER tax est ({(accrualQ.data.employerTaxRateUsed * 100).toFixed(2)}%): {MONEY(accrualQ.data.employerTaxAccrualEstimate)} · Last posted period end: {DATE(accrualQ.data.lastPostedPeriodEnd)}
            </div>
          )}
          <DataTable columns={[
            { key: 'employeeId', header: 'Employee' },
            { key: 'hours', header: 'Hours' },
            { key: 'accruedWages', header: 'Accrued', render: (r: any) => MONEY(r.accruedWages) },
          ]} data={accrualQ.data?.rows || []} loading={accrualQ.isLoading} />
          <p className={NOTE}>{accrualQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">EFTPS Deposit Schedule ({(eftpsQ.data as any)?.depositorStatus ?? '—'})</div>
          <DataTable columns={[
            { key: 'taxType', header: 'Type' },
            { key: 'depositDate', header: 'Due', render: (r: any) => DATE(r.depositDate) },
            { key: 'estimatedAmount', header: 'Est.', render: (r: any) => MONEY(r.estimatedAmount) },
            { key: 'state', header: 'State' },
          ]} data={eftpsQ.data?.rows || []} loading={eftpsQ.isLoading} />
          <p className={NOTE}>Upcoming {(eftpsQ.data as any)?.upcomingCount ?? 0} ({MONEY((eftpsQ.data as any)?.upcomingAmount ?? null)}) · Missed {(eftpsQ.data as any)?.missedCount ?? 0} ({MONEY((eftpsQ.data as any)?.missedAmount ?? null)}) · Next due {DATE((eftpsQ.data as any)?.nextDueDate)}</p>
          <p className={NOTE}>{eftpsQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">ACH Return Report</div>
          <DataTable columns={[
            { key: 'returnCode', header: 'Code' },
            { key: 'description', header: 'Description' },
            { key: 'amount', header: 'Amount', render: (r: any) => MONEY(r.amount) },
            { key: 'action', header: 'Action' },
            { key: 'processed', header: 'Processed', render: (r: any) => (r.processed ? 'Yes' : 'No') },
          ]} data={achQ.data?.rows || []} loading={achQ.isLoading} />
          <p className={NOTE}>{achQ.data?.byCode.map((c: any) => `${c.returnCode}: ${c.count}`).join(' · ')}</p>
          <p className={NOTE}>{achQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">New-Hire Reporting (last 90 days)</div>
          <DataTable columns={[
            { key: 'employeeCode', header: 'Code' },
            { key: 'hireDate', header: 'Hired', render: (r: any) => DATE(r.hireDate) },
            { key: 'state', header: 'State' },
            { key: 'agencyName', header: 'Agency' },
            { key: 'dueBy', header: 'Due By', render: (r: any) => DATE(r.dueBy) },
            { key: 'submissionStatus', header: 'Status', render: (r: any) => (r.overdue ? `${r.submissionStatus} (OVERDUE)` : r.submissionStatus) },
          ]} data={nhQ.data?.rows || []} loading={nhQ.isLoading} />
          <p className={NOTE}>{nhQ.data?.note}</p>
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Workers&apos; Comp Premium ({year})</div>
          <DataTable columns={[
            { key: 'classCode', header: 'Class Code' },
            { key: 'state', header: 'State' },
            { key: 'payrollBasis', header: 'Payroll Basis', render: (r: any) => MONEY(r.payrollBasis) },
            { key: 'estimatedPremium', header: 'Est. Premium', render: (r: any) => MONEY(r.estimatedPremium) },
            { key: 'actualBooked', header: 'Actual', render: (r: any) => (r.actualBooked != null ? MONEY(r.actualBooked) : 'n/a') },
          ]} data={wcpQ.data?.rows || []} loading={wcpQ.isLoading} />
          <p className={NOTE}>Total estimated premium: <b>{MONEY(wcpQ.data?.totalEstimatedPremium ?? null)}</b> · Unmatched trade payroll: {MONEY(wcpQ.data?.unmatchedTradePayroll ?? null)}</p>
          <p className={NOTE}>{wcpQ.data?.note}</p>
        </div>
      </div>
    </div>
  )
}

function ComplianceSection({ qc, company, year }: { qc: any; company: string; year: number }) {
  const ppQ = useQuery({ queryKey: ['payroll', 'report', 'positive-pay', company], queryFn: () => getPositivePay(company) })
  const necQ = useQuery({ queryKey: ['payroll', 'report', '1099', company, year], queryFn: () => get1099Nec(company, year) })
  const unionQ = useQuery({ queryKey: ['payroll', 'report', 'union', company], queryFn: () => getUnionReport(company) })
  const wcQ = useQuery({ queryKey: ['payroll', 'report', 'wc', company], queryFn: () => getWorkersCompReport(company) })
  const [msEmp, setMsEmp] = useState('')
  const [msWages, setMsWages] = useState('1000')
  const msQ = useQuery({ queryKey: ['payroll', 'report', 'ms', msEmp, msWages], queryFn: () => getMultiStateWithholding(msEmp, Number(msWages)), enabled: !!msEmp })
  return (
    <div className="space-y-3 border-t pt-3 mt-3">
      <div className="font-semibold">Compliance &amp; Statutory (Batch E)</div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Positive Pay (issued checks)</div>
          <DataTable columns={[{ key: 'checkNumber', header: 'Check' }, { key: 'employeeId', header: 'Employee' }, { key: 'amount', header: 'Amount', render: (r: any) => MONEY(r.amount) }, { key: 'isDirectDeposit', header: 'DD', render: (r: any) => (r.isDirectDeposit ? 'Yes' : 'No') }]} data={(ppQ.data as any)?.data || []} loading={ppQ.isLoading} />
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">1099-NEC ({year})</div>
          <DataTable columns={[{ key: 'recipientId', header: 'Recipient' }, { key: 'nonemployeeCompensation', header: 'NEC', render: (r: any) => MONEY(r.nonemployeeCompensation) }, { key: 'federalIncomeTaxWithheld', header: 'FIT WH', render: (r: any) => MONEY(r.federalIncomeTaxWithheld) }]} data={(necQ.data as any)?.data || []} loading={necQ.isLoading} />
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Union Prevailing Wage</div>
          <DataTable columns={[{ key: 'tradeClassification', header: 'Trade' }, { key: 'jurisdiction', header: 'Jurisdiction' }, { key: 'prevailingWageRate', header: 'Rate', render: (r: any) => MONEY(r.prevailingWageRate) }, { key: 'fringeBenefitRate', header: 'Fringe', render: (r: any) => MONEY(r.fringeBenefitRate) }]} data={(unionQ.data as any)?.data || []} loading={unionQ.isLoading} />
        </div>
        <div className="border rounded p-2">
          <div className="font-semibold mb-1">Workers' Comp Class Codes</div>
          <DataTable columns={[{ key: 'classCode', header: 'Code' }, { key: 'state', header: 'State' }, { key: 'ratePer100', header: 'Rate/100' }, { key: 'effectiveRatePer100', header: 'Eff Rate', render: (r: any) => MONEY(r.effectiveRatePer100) }]} data={(wcQ.data as any)?.data || []} loading={wcQ.isLoading} />
        </div>
      </div>
      <div className="border rounded p-2">
        <div className="font-semibold mb-1">Multi-State Withholding Estimator</div>
        <div className="flex items-end gap-2 mb-2">
          <Input label="Employee Id" value={msEmp} onChange={(v: any) => setMsEmp(v)} />
          <Input type="number" label="Taxable Wages" value={msWages} onChange={(v: any) => setMsWages(v)} />
        </div>
        {(msQ.data as any) && (
          <div className="text-sm">
            Federal WH: <b>{MONEY((msQ.data as any).federalWithholding)}</b>
            <div className="mt-1">
              {(msQ.data as any).states?.map((s: any, i: number) => (
                <span key={i} className="inline-block mr-3 border rounded px-2 py-1">{s.state}: {MONEY(s.stateWithholding)}{s.exempt ? ' (exempt)' : ''}</span>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

export function GarnishmentsTab({ qc }: { qc: any }) {
  const [employeeId, setEmployeeId] = useState('')
  const [di, setDi] = useState('1000')
  const [form, setForm] = useState({ type: '0', disposableIncomePercent: '50', caseNumber: '' })
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const listQ = useQuery({ queryKey: ['payroll', 'garnishments', employeeId], queryFn: () => getGarnishmentsForEmployee(employeeId), enabled: !!employeeId })
  const computeQ = useQuery({ queryKey: ['payroll', 'garnish-compute', employeeId, di], queryFn: () => computeGarnishments(employeeId, Number(di)), enabled: !!employeeId })
  const createMut = useMutation({ mutationFn: () => createGarnishment({ companyId: currentCompanyId(), employeeId, type: Number(form.type), disposableIncomePercent: Number(form.disposableIncomePercent), caseNumber: form.caseNumber || null }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'garnishments', employeeId] }) })

  return (
    <div className="space-y-3 max-w-2xl">
      <Select label="Employee" value={employeeId} onChange={(e: any) => setEmployeeId(e.target.value)} options={(employees as any[]).map((e: any) => ({ value: e.id, label: `${e.employeeCode} — ${e.fullName}` }))} />
      {employeeId && (
        <>
          <div className="flex gap-2 items-end">
            <Input type="number" label="Disposable Income" value={di} onChange={(v: any) => setDi(v)} />
          </div>
          {(computeQ.data as any) && (
            <div className="border rounded p-3 text-sm space-y-1">
              <div>Total withheld: <b>{MONEY((computeQ.data as any).totalWithheld)}</b> | Net after: {MONEY((computeQ.data as any).netAfterGarnishment)}</div>
              {((computeQ.data as any).lines || []).map((l: any, i: number) => (
                <div key={i} className="flex justify-between border-t pt-1">
                  <span>{l.type} (priority {l.priority})</span>
                  <span>withheld {MONEY(l.withheld)} — {l.reason}</span>
                </div>
              ))}
            </div>
          )}
          <div className="border rounded p-3 space-y-2">
            <div className="font-semibold">Add Garnishment</div>
            <Select label="Type" value={form.type} onChange={(e: any) => setForm({ ...form, type: e.target.value })} options={[{ value: '0', label: 'Child Support' }, { value: '1', label: 'Federal Tax Levy' }, { value: '2', label: 'State Tax Levy' }, { value: '3', label: 'Student Loan' }, { value: '4', label: 'Creditor' }]} />
            <Input type="number" label="Disposable Income %" value={form.disposableIncomePercent} onChange={(v: any) => setForm({ ...form, disposableIncomePercent: v })} />
            <Input label="Case #" value={form.caseNumber} onChange={(e: any) => setForm({ ...form, caseNumber: e.target.value })} />
            <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Add</Button>
          </div>
          <DataTable
            columns={[
              { key: 'type', header: 'Type' },
              { key: 'priority', header: 'Priority' },
              { key: 'disposableIncomePercent', header: '% DI' },
              { key: 'caseNumber', header: 'Case #' },
              { key: 'isActive', header: 'Active', render: (r: any) => (r.isActive ? 'Yes' : 'Terminated') },
            ]}
            data={(listQ.data || []) as any[]}
            loading={listQ.isLoading}
          />
        </>
      )}
    </div>
  )
}


// --- Setup tab: company payroll setup, PTO policies, new-hire configs, ACH returns ---
export function SetupTab({ qc }: { qc: any }) {
  const cid = currentCompanyId()
  const { data: setup } = useQuery({ queryKey: ['payroll', 'company-setup', cid], queryFn: () => getCompanySetup(cid) })
  const [form, setForm] = useState({ ein: '', federalTaxId: '', stateTaxId: '', sutAState: '', eftpsPin: '', depositSchedule: 'Monthly', socialSecurityRate: '0.062', medicareRate: '0.0145', futaRate: '0.006', sutARate: '0.027' })
  const setupMut = useMutation({ mutationFn: () => createCompanySetup({ companyId: cid, ...form, socialSecurityRate: Number(form.socialSecurityRate), medicareRate: Number(form.medicareRate), futaRate: Number(form.futaRate), sutARate: Number(form.sutARate) }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'company-setup', cid] }) })

  const { data: ptos = [] } = useQuery({ queryKey: ['payroll', 'pto-policies'], queryFn: () => getPtoPolicies() })
  const [pto, setPto] = useState({ name: '', accrualRate: '', accrualBasis: 'PerHourWorked', maxAccrual: '', carryoverLimit: '', cashOutAllowed: false, cashOutRate: '' })
  const ptoMut = useMutation({ mutationFn: () => createPtoPolicy({ companyId: cid, ...pto, accrualRate: Number(pto.accrualRate), maxAccrual: Number(pto.maxAccrual), carryoverLimit: Number(pto.carryoverLimit), cashOutRate: pto.cashOutRate ? Number(pto.cashOutRate) : null }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'pto-policies'] }) })

  const { data: nhc = [] } = useQuery({ queryKey: ['payroll', 'new-hire', cid], queryFn: () => getNewHireConfigs(cid) })
  const [nh, setNh] = useState({ stateCode: '', agencyName: '', dueWindowDays: '20', transmissionMethod: 'SFTP', sftpEndpoint: '', agencyId: '' })
  const nhMut = useMutation({ mutationFn: () => createNewHireConfig({ companyId: cid, ...nh, dueWindowDays: Number(nh.dueWindowDays) }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'new-hire', cid] }) })

  const { data: returns = [] } = useQuery({ queryKey: ['payroll', 'ach-returns', cid], queryFn: () => getAchReturns(cid) })
  const [ret, setRet] = useState({ traceNumber: '', returnCode: '', description: '', amount: '', returnAction: 'Reissue' })
  const retMut = useMutation({ mutationFn: () => createAchReturn({ companyId: cid, ...ret, amount: Number(ret.amount) }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'ach-returns', cid] }) })
  const procMut = useMutation({ mutationFn: (id: string) => processAchReturn(id), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'ach-returns', cid] }) })

  return (
    <div className="space-y-4">
      <div className="border rounded p-3 space-y-2">
        <div className="font-semibold">Company Payroll Setup</div>
        {setup?.data ? (
          <div className="text-sm">
            <div>EIN: {setup.data.ein} • Federal Tax ID: {setup.data.federalTaxId}</div>
            <div>State Tax ID: {setup.data.stateTaxId || '—'} • SUTA State: {setup.data.sutAState || '—'}</div>
            <div>Deposit Schedule: {setup.data.depositSchedule} • SS: {(setup.data.socialSecurityRate * 100).toFixed(2)}% • Medicare: {(setup.data.medicareRate * 100).toFixed(2)}%</div>
            <div>FUTA: {(setup.data.futaRate * 100).toFixed(2)}% • SUTA: {(setup.data.sutARate * 100).toFixed(2)}%</div>
          </div>
        ) : (
          <div className="grid grid-cols-2 md:grid-cols-3 gap-2 items-end">
            <Input label="EIN" value={form.ein} onChange={(v: any) => setForm({ ...form, ein: v })} />
            <Input label="Federal Tax ID" value={form.federalTaxId} onChange={(v: any) => setForm({ ...form, federalTaxId: v })} />
            <Input label="State Tax ID" value={form.stateTaxId} onChange={(v: any) => setForm({ ...form, stateTaxId: v })} />
            <Input label="SUTA State" value={form.sutAState} onChange={(v: any) => setForm({ ...form, sutAState: v })} />
            <Input label="EFTPS PIN" value={form.eftpsPin} onChange={(v: any) => setForm({ ...form, eftpsPin: v })} />
            <Input label="Deposit Schedule" value={form.depositSchedule} onChange={(v: any) => setForm({ ...form, depositSchedule: v })} />
            <Input type="number" label="SS Rate" value={form.socialSecurityRate} onChange={(v: any) => setForm({ ...form, socialSecurityRate: v })} />
            <Input type="number" label="Medicare Rate" value={form.medicareRate} onChange={(v: any) => setForm({ ...form, medicareRate: v })} />
            <Input type="number" label="FUTA Rate" value={form.futaRate} onChange={(v: any) => setForm({ ...form, futaRate: v })} />
            <Input type="number" label="SUTA Rate" value={form.sutARate} onChange={(v: any) => setForm({ ...form, sutARate: v })} />
            <Button onClick={() => setupMut.mutate()} disabled={setupMut.isPending}>Save Setup</Button>
          </div>
        )}
      </div>

      <div className="border rounded p-3 space-y-2">
        <div className="font-semibold">PTO Policies</div>
        <DataTable columns={[
          { key: 'name', header: 'Name' },
          { key: 'accrualBasis', header: 'Basis' },
          { key: 'accrualRate', header: 'Rate', align: 'right' },
          { key: 'maxAccrual', header: 'Max', align: 'right' },
          { key: 'carryoverLimit', header: 'Carryover', align: 'right' },
          { key: 'cashOutAllowed', header: 'Cash-out', render: (v: boolean) => (v ? 'Yes' : 'No') },
        ]} data={ptos as any[]} emptyMessage="No PTO policies." />
        <div className="grid grid-cols-2 md:grid-cols-4 gap-2 items-end border-t pt-2">
          <Input label="Name" value={pto.name} onChange={(v: any) => setPto({ ...pto, name: v })} />
          <Input label="Basis" value={pto.accrualBasis} onChange={(v: any) => setPto({ ...pto, accrualBasis: v })} />
          <Input type="number" label="Accrual Rate" value={pto.accrualRate} onChange={(v: any) => setPto({ ...pto, accrualRate: v })} />
          <Input type="number" label="Max" value={pto.maxAccrual} onChange={(v: any) => setPto({ ...pto, maxAccrual: v })} />
          <Input type="number" label="Carryover" value={pto.carryoverLimit} onChange={(v: any) => setPto({ ...pto, carryoverLimit: v })} />
          <Input type="number" label="Cash-out Rate" value={pto.cashOutRate} onChange={(v: any) => setPto({ ...pto, cashOutRate: v })} />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={pto.cashOutAllowed} onChange={(e: any) => setPto({ ...pto, cashOutAllowed: e.target.checked })} /> Cash-out</label>
          <Button onClick={() => ptoMut.mutate()} disabled={ptoMut.isPending}>Add Policy</Button>
        </div>
      </div>

      <div className="border rounded p-3 space-y-2">
        <div className="font-semibold">New-Hire Reporting Config</div>
        <DataTable columns={[
          { key: 'stateCode', header: 'State' },
          { key: 'agencyName', header: 'Agency' },
          { key: 'dueWindowDays', header: 'Due (days)', align: 'right' },
          { key: 'transmissionMethod', header: 'Method' },
        ]} data={nhc as any[]} emptyMessage="No new-hire configs." />
        <div className="grid grid-cols-2 md:grid-cols-4 gap-2 items-end border-t pt-2">
          <Input label="State" value={nh.stateCode} onChange={(v: any) => setNh({ ...nh, stateCode: v })} />
          <Input label="Agency" value={nh.agencyName} onChange={(v: any) => setNh({ ...nh, agencyName: v })} />
          <Input type="number" label="Due Window" value={nh.dueWindowDays} onChange={(v: any) => setNh({ ...nh, dueWindowDays: v })} />
          <Input label="Method" value={nh.transmissionMethod} onChange={(v: any) => setNh({ ...nh, transmissionMethod: v })} />
          <Button onClick={() => nhMut.mutate()} disabled={nhMut.isPending}>Add Config</Button>
        </div>
      </div>

      <div className="border rounded p-3 space-y-2">
        <div className="font-semibold">ACH Returns</div>
        <DataTable columns={[
          { key: 'returnCode', header: 'Code' },
          { key: 'description', header: 'Description' },
          { key: 'amount', header: 'Amount', align: 'right', render: (v: any) => MONEY(v) },
          { key: 'returnAction', header: 'Action' },
          { key: 'processed', header: 'Processed', render: (v: boolean) => (v ? 'Yes' : 'No') },
          { key: 'id', header: '', render: (_: unknown, r: any) => !r.processed ? <Button size="sm" onClick={() => procMut.mutate(r.id)}>Process</Button> : null },
        ]} data={returns as any[]} emptyMessage="No ACH returns." />
        <div className="grid grid-cols-2 md:grid-cols-5 gap-2 items-end border-t pt-2">
          <Input label="Trace #" value={ret.traceNumber} onChange={(v: any) => setRet({ ...ret, traceNumber: v })} />
          <Input label="Return Code" value={ret.returnCode} onChange={(v: any) => setRet({ ...ret, returnCode: v })} />
          <Input label="Description" value={ret.description} onChange={(v: any) => setRet({ ...ret, description: v })} />
          <Input type="number" label="Amount" value={ret.amount} onChange={(v: any) => setRet({ ...ret, amount: v })} />
          <Input label="Action" value={ret.returnAction} onChange={(v: any) => setRet({ ...ret, returnAction: v })} />
          <Button onClick={() => retMut.mutate()} disabled={retMut.isPending}>Record Return</Button>
        </div>
      </div>
    </div>
  )
}
