// Payroll module page (Phase 11): employees, pay codes, union/certified profiles,
// timesheets (with PA project validation + labor dual-post), payroll runs (draft/post/
// accrue/reverse + certified payroll report), and garnishments (CCPA).
import { useState } from 'react'
import { UserCheck, Clock, CalendarRange, FileSpreadsheet, Scale, BadgeDollarSign, Plus } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { companyId as currentCompanyId } from '@api/orderManagement'
import {
  getEmployees, createEmployee,
  getPayCodes, createPayCode,
  getUnionProfiles, createUnionProfile, validatePrevailingWage,
  createTimesheet, submitTimesheet, approveTimesheet,
  createDraftRun, postRun, accrueRun, reverseRun, getCertifiedPayroll,
  createGarnishment, getGarnishmentsForEmployee, computeGarnishments,
} from '@api/payroll'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')

type Tab = 'employees' | 'paycodes' | 'union' | 'timesheets' | 'runs' | 'garnishments'

export function PayrollPage() {
  const queryClient = useQueryClient()
  const [tab, setTab] = useState<Tab>('employees')

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <BadgeDollarSign className="h-6 w-6 text-indigo-600" />
        <h1 className="text-2xl font-semibold">Payroll</h1>
      </div>

      <div className="flex flex-wrap gap-2 border-b">
        {([
          ['employees', 'Employees', UserCheck],
          ['paycodes', 'Pay Codes', FileSpreadsheet],
          ['union', 'Union / Certified', Scale],
          ['timesheets', 'Timesheets', Clock],
          ['runs', 'Payroll Runs', CalendarRange],
          ['garnishments', 'Garnishments', Scale],
        ] as [Tab, string, any][]).map(([key, label, Icon]) => (
          <button
            key={key}
            onClick={() => setTab(key)}
            className={`flex items-center gap-1 px-3 py-2 text-sm border-b-2 ${tab === key ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500'}`}
          >
            <Icon className="h-4 w-4" /> {label}
          </button>
        ))}
      </div>

      {tab === 'employees' && <EmployeesTab qc={queryClient} />}
      {tab === 'paycodes' && <PayCodesTab qc={queryClient} />}
      {tab === 'union' && <UnionTab />}
      {tab === 'timesheets' && <TimesheetsTab qc={queryClient} />}
      {tab === 'runs' && <RunsTab qc={queryClient} />}
      {tab === 'garnishments' && <GarnishmentsTab qc={queryClient} />}
    </div>
  )
}

function EmployeesTab({ qc }: { qc: any }) {
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ employeeCode: '', firstName: '', lastName: '', email: '' })
  const { data = [], isLoading } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const mutation = useMutation({
    mutationFn: () => createEmployee({ companyId: currentCompanyId(), ...form, employmentType: 0, hireDate: new Date().toISOString().slice(0, 10), isBillable: true }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['payroll', 'employees'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'employeeCode', header: 'Code' },
    { key: 'fullName', header: 'Name' },
    { key: 'status', header: 'Status' },
    { key: 'email', header: 'Email' },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Employee</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Employee">
        <div className="space-y-2">
          <Input label="Code" value={form.employeeCode} onChange={(e: any) => setForm({ ...form, employeeCode: e.target.value })} />
          <Input label="First Name" value={form.firstName} onChange={(e: any) => setForm({ ...form, firstName: e.target.value })} />
          <Input label="Last Name" value={form.lastName} onChange={(e: any) => setForm({ ...form, lastName: e.target.value })} />
          <Input label="Email" value={form.email} onChange={(e: any) => setForm({ ...form, email: e.target.value })} />
          {mutation.isError && <p className="text-sm text-red-600">{getErrorMessage(mutation.error)}</p>}
          <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

function PayCodesTab({ qc }: { qc: any }) {
  const [show, setShow] = useState(false)
  const [form, setForm] = useState({ code: '', description: '', type: '0' })
  const { data = [], isLoading } = useQuery({ queryKey: ['payroll', 'paycodes'], queryFn: () => getPayCodes() })
  const mutation = useMutation({
    mutationFn: () => createPayCode({ companyId: currentCompanyId(), code: form.code, description: form.description, type: Number(form.type), glAccountNumber: '6000' }),
    onSuccess: () => { setShow(false); qc.invalidateQueries({ queryKey: ['payroll', 'paycodes'] }) },
  })
  const cols: DataTableColumn<any>[] = [
    { key: 'code', header: 'Code' },
    { key: 'description', header: 'Description' },
    { key: 'type', header: 'Type' },
  ]
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => setShow(true)}><Plus className="h-4 w-4" /> New Pay Code</Button></div>
      <DataTable columns={cols} data={data as any[]} loading={isLoading} />
      <Modal isOpen={show} onClose={() => setShow(false)} title="New Pay Code">
        <div className="space-y-2">
          <Input label="Code" value={form.code} onChange={(e: any) => setForm({ ...form, code: e.target.value })} />
          <Input label="Description" value={form.description} onChange={(e: any) => setForm({ ...form, description: e.target.value })} />
          <Select label="Type" value={form.type} onChange={(e: any) => setForm({ ...form, type: e.target.value })} options={[{ value: '0', label: 'Earnings' }, { value: '1', label: 'Deduction' }, { value: '2', label: 'Tax' }]} />
          <Button onClick={() => mutation.mutate()} disabled={mutation.isPending}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

function UnionTab() {
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

function TimesheetsTab({ qc }: { qc: any }) {
  const [employeeId, setEmployeeId] = useState('')
  const [weekEnding, setWeekEnding] = useState(new Date().toISOString().slice(0, 10))
  const [msg, setMsg] = useState('')
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const createMut = useMutation({
    mutationFn: async () => {
      const ts: any = await createTimesheet({ companyId: currentCompanyId(), employeeId, weekEnding })
      await submitTimesheet(ts.data, employeeId)
      await approveTimesheet(ts.data, employeeId)
      return ts
    },
    onSuccess: (ts: any) => { setMsg(`Timesheet ${ts.data} approved → labor cost posted to Project Accounting + GL.`); qc.invalidateQueries({ queryKey: ['payroll', 'employees'] }) },
    onError: (e) => setMsg(getErrorMessage(e)),
  })
  return (
    <div className="space-y-3 max-w-lg">
      <p className="text-sm text-gray-600">Create + submit + approve a timesheet. Approving posts approved labor hours to the project ledger and GL (wired to Phase 10).</p>
      <Select label="Employee" value={employeeId} onChange={(e: any) => setEmployeeId(e.target.value)} options={(employees as any[]).map((e: any) => ({ value: e.id, label: `${e.employeeCode} — ${e.fullName}` }))} />
      <Input label="Week Ending" type="date" value={weekEnding} onChange={(e: any) => setWeekEnding(e.target.value)} />
      <Button onClick={() => createMut.mutate()} disabled={createMut.isPending || !employeeId}>Create / Submit / Approve</Button>
      {msg && <p className="text-sm">{msg}</p>}
    </div>
  )
}

function RunsTab({ qc }: { qc: any }) {
  const [runId, setRunId] = useState('')
  const [report, setReport] = useState<any>(null)
  const createMut = useMutation({
    mutationFn: () => createDraftRun({ companyId: currentCompanyId(), calendarId: '00000000-0000-0000-0000-000000000000', periodStart: '2026-08-01', periodEnd: '2026-08-31', payDate: '2026-08-20' }),
    onSuccess: (r: any) => setRunId(r.data),
  })
  const postMut = useMutation({ mutationFn: () => postRun(runId, '00000000-0000-0000-0000-000000000000'), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'runs'] }) })
  const accrueMut = useMutation({ mutationFn: () => accrueRun(runId, '2026-08-31') })
  const reverseMut = useMutation({ mutationFn: () => reverseRun(runId, '2026-09-01') })
  const certQ = useQuery({ queryKey: ['payroll', 'certified', runId], queryFn: () => getCertifiedPayroll(runId), enabled: !!runId })

  return (
    <div className="space-y-3 max-w-2xl">
      <div className="flex gap-2 flex-wrap">
        <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Build Draft Run</Button>
        <Button variant="secondary" disabled={!runId || postMut.isPending} onClick={() => postMut.mutate()}>Post Run (→ GL)</Button>
        <Button variant="secondary" disabled={!runId || accrueMut.isPending} onClick={() => accrueMut.mutate()}>Accrue</Button>
        <Button variant="secondary" disabled={!runId || reverseMut.isPending} onClick={() => reverseMut.mutate()}>Reverse</Button>
        <Button variant="secondary" disabled={!runId} onClick={() => setReport((certQ.data as any))}>Certified Payroll Report</Button>
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
    </div>
  )
}

function GarnishmentsTab({ qc }: { qc: any }) {
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
