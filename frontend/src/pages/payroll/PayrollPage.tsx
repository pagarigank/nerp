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
} from '@api/payroll'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

const MONEY = (v: number | null) => (v != null ? `$${Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—')

type Tab = 'employees' | 'paycodes' | 'union' | 'timesheets' | 'runs' | 'expenses' | 'tax' | 'deductions' | 'pto' | 'manual' | 'reports' | 'garnishments'

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
          ['runs', 'Runs & Checks', CalendarRange],
          ['expenses', 'Expenses', FileSpreadsheet],
          ['tax', 'Tax / W-4', Scale],
          ['deductions', 'Deductions', FileSpreadsheet],
          ['pto', 'PTO', Clock],
          ['manual', 'Manual Checks', BadgeDollarSign],
          ['reports', 'Reports', FileSpreadsheet],
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
      {tab === 'expenses' && <ExpensesTab qc={queryClient} />}
      {tab === 'tax' && <TaxTab qc={queryClient} />}
      {tab === 'deductions' && <DeductionsTab qc={queryClient} />}
      {tab === 'pto' && <PtoTab qc={queryClient} />}
      {tab === 'manual' && <ManualChecksTab qc={queryClient} />}
      {tab === 'reports' && <ReportsTab qc={queryClient} />}
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
  const voidMut = useMutation({ mutationFn: () => voidRun(runId), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'runs'] }) })
  const printMut = useMutation({ mutationFn: () => printChecks(runId, { checkDate: '2026-08-20', startingCheckNumber: 1001, directDeposit: true }) })
  const [nacha, setNacha] = useState<string | null>(null)
  const certQ = useQuery({ queryKey: ['payroll', 'certified', runId], queryFn: () => getCertifiedPayroll(runId), enabled: !!runId })
  const nachaQ = useQuery({ queryKey: ['payroll', 'nacha', runId], queryFn: () => getNachaFile(runId), enabled: false })
  const loadNacha = () => { nachaQ.refetch().then((r) => setNacha((r.data as any)?.data ?? null)) }

  return (
    <div className="space-y-3 max-w-2xl">
      <div className="flex gap-2 flex-wrap">
        <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Build Draft Run</Button>
        <Button variant="secondary" disabled={!runId || postMut.isPending} onClick={() => postMut.mutate()}>Post Run (→ GL)</Button>
        <Button variant="secondary" disabled={!runId || accrueMut.isPending} onClick={() => accrueMut.mutate()}>Accrue</Button>
        <Button variant="secondary" disabled={!runId || reverseMut.isPending} onClick={() => reverseMut.mutate()}>Reverse</Button>
        <Button variant="secondary" disabled={!runId || voidMut.isPending} onClick={() => voidMut.mutate()}>Void (draft)</Button>
        <Button variant="secondary" disabled={!runId || printMut.isPending} onClick={() => printMut.mutate()}>Print Checks</Button>
        <Button variant="secondary" disabled={!runId} onClick={loadNacha}>NACHA File</Button>
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
      {nacha && (
        <div className="border rounded p-3 text-sm">
          <div className="font-semibold mb-1">NACHA PPD File (first 600 chars)</div>
          <pre className="text-[11px] whitespace-pre-wrap max-h-40 overflow-auto">{nacha.slice(0, 600)}</pre>
        </div>
      )}
    </div>
  )
}

function ExpensesTab({ qc }: { qc: any }) {
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

function TaxTab({ qc }: { qc: any }) {
  const [employeeId, setEmployeeId] = useState('')
  const [w4, setW4] = useState({ filingStatus: '0', multipleJobs: false, dependentsCredit: '2000', otherIncome: '0', deductions: '0' })
  const [calc, setCalc] = useState({ taxableWages: '2000', payFrequency: '2' })
  const { data: employees = [] } = useQuery({ queryKey: ['payroll', 'employees'], queryFn: () => getEmployees() })
  const createMut = useMutation({ mutationFn: () => createW4(employeeId, { filingStatus: Number(w4.filingStatus), multipleJobs: w4.multipleJobs, dependentsCredit: Number(w4.dependentsCredit), otherIncome: Number(w4.otherIncome), deductions: Number(w4.deductions) }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'w4', employeeId] }) })
  const withholdQ = useQuery({ queryKey: ['payroll', 'withhold', employeeId, calc.taxableWages, calc.payFrequency], queryFn: () => computeWithholding(employeeId, Number(calc.taxableWages), Number(calc.payFrequency)), enabled: !!employeeId })
  return (
    <div className="space-y-3 max-w-2xl">
      <Select label="Employee" value={employeeId} onChange={(e: any) => setEmployeeId(e.target.value)} options={(employees as any[]).map((e: any) => ({ value: e.id, label: `${e.employeeCode} — ${e.fullName}` }))} />
      {employeeId && (
        <>
          <div className="border rounded p-3 space-y-2">
            <div className="font-semibold">2020+ W-4 (Pub 15-T Percentage Method)</div>
            <Select label="Filing Status" value={w4.filingStatus} onChange={(e: any) => setW4({ ...w4, filingStatus: e.target.value })} options={[{ value: '0', label: 'Single' }, { value: '1', label: 'Married' }, { value: '2', label: 'Head of Household' }]} />
            <Input type="number" label="Dependents Credit ($)" value={w4.dependentsCredit} onChange={(v: any) => setW4({ ...w4, dependentsCredit: v })} />
            <Input type="number" label="Other Income (annual)" value={w4.otherIncome} onChange={(v: any) => setW4({ ...w4, otherIncome: v })} />
            <Input type="number" label="Deductions (annual)" value={w4.deductions} onChange={(v: any) => setW4({ ...w4, deductions: v })} />
            <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Save W-4</Button>
          </div>
          <div className="border rounded p-3 space-y-2">
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

function DeductionsTab({ qc }: { qc: any }) {
  const [form, setForm] = useState({ description: '', deductionType: '0', isPercent: false, percentRate: '0', flatAmount: '0', glAccountNumber: '2200' })
  const { data: items = [] } = useQuery({ queryKey: ['payroll', 'deductions'], queryFn: () => getDeductionBenefits() })
  const createMut = useMutation({ mutationFn: () => createDeductionBenefit({ companyId: currentCompanyId(), description: form.description, deductionType: Number(form.deductionType), isPercent: form.isPercent, percentRate: Number(form.percentRate), flatAmount: Number(form.flatAmount), glAccountNumber: form.glAccountNumber }), onSuccess: () => qc.invalidateQueries({ queryKey: ['payroll', 'deductions'] }) })
  return (
    <div className="space-y-3">
      <div className="flex justify-end"><Button onClick={() => createMut.mutate()} disabled={createMut.isPending}><Plus className="h-4 w-4" /> New Deduction/Benefit</Button></div>
      <DataTable columns={[{ key: 'description', header: 'Description' }, { key: 'deductionType', header: 'Type' }, { key: 'isPercent', header: '%?', render: (r: any) => (r.isPercent ? 'Yes' : 'No') }, { key: 'glAccountNumber', header: 'GL' }]} data={items as any[]} />
      <Modal isOpen={createMut.isIdle} onClose={() => {}} title="New Deduction/Benefit">
        <div className="space-y-2">
          <Input label="Description" value={form.description} onChange={(e: any) => setForm({ ...form, description: e.target.value })} />
          <Select label="Type" value={form.deductionType} onChange={(e: any) => setForm({ ...form, deductionType: e.target.value })} options={[{ value: '0', label: 'Pre-Tax' }, { value: '1', label: 'Post-Tax' }, { value: '2', label: 'Employer' }]} />
          <Input type="number" label="Percent Rate" value={form.percentRate} onChange={(v: any) => setForm({ ...form, percentRate: v })} />
          <Input type="number" label="Flat Amount" value={form.flatAmount} onChange={(v: any) => setForm({ ...form, flatAmount: v })} />
          <Input label="GL Account" value={form.glAccountNumber} onChange={(e: any) => setForm({ ...form, glAccountNumber: e.target.value })} />
          <Button onClick={() => createMut.mutate()} disabled={createMut.isPending}>Create</Button>
        </div>
      </Modal>
    </div>
  )
}

function PtoTab({ qc }: { qc: any }) {
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

function ManualChecksTab({ qc }: { qc: any }) {
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

function ReportsTab({ qc }: { qc: any }) {
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
