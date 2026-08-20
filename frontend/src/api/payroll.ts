// Payroll module API client (Phase 11).
import { get, post, put, del } from './client'
import { companyId as defaultCompanyId } from './inventory'

// --- Employees ---
export function getEmployees(companyIdParam?: string) {
  return get('/payroll/employees', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createEmployee(data: any) {
  return post('/payroll/employees', data)
}
export function editEmployee(id: string, data: any) {
  return put(`/payroll/employees/${id}`, data)
}

// --- Pay codes ---
export function getPayCodes(companyIdParam?: string) {
  return get('/payroll/pay-codes', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createPayCode(data: any) {
  return post('/payroll/pay-codes', data)
}
export function editPayCode(id: string, data: any) {
  return put(`/payroll/pay-codes/${id}`, data)
}

// --- Union / certified-payroll profiles (prevailing wage + fringe) ---
export function getUnionProfiles(companyIdParam?: string) {
  return get('/payroll/union-profiles', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createUnionProfile(data: any) {
  return post('/payroll/union-profiles', data)
}
export function validatePrevailingWage(tradeClassification: string, jurisdiction: string | null, actualWage: number) {
  const params: any = { tradeClassification, actualWage }
  if (jurisdiction) params.jurisdiction = jurisdiction
  return get('/payroll/union-profiles/validate', params)
}

// --- Payroll calendars ---
export function createPayrollCalendar(data: any) {
  return post('/payroll/calendars', data)
}

// --- Timesheets ---
export function createTimesheet(data: any) {
  return post('/payroll/timesheets', data)
}
export function addTimesheetLine(timesheetId: string, data: any) {
  return post(`/payroll/timesheets/${timesheetId}/lines`, data)
}
export function submitTimesheet(timesheetId: string, supervisorId: string) {
  return post(`/payroll/timesheets/${timesheetId}/submit`, { supervisorId })
}
export function approveTimesheet(timesheetId: string, approvedById: string) {
  return post(`/payroll/timesheets/${timesheetId}/approve`, { approvedById })
}
export function rejectTimesheet(timesheetId: string, reason: string) {
  return post(`/payroll/timesheets/${timesheetId}/reject`, { reason })
}
export function getTimesheet(timesheetId: string) {
  return get(`/payroll/timesheets/${timesheetId}`)
}

// --- Payroll runs ---
export function createDraftRun(data: any) {
  return post('/payroll/runs/draft', data)
}
export function postRun(runId: string, postedById: string) {
  return post(`/payroll/runs/${runId}/post`, { postedById })
}
export function accrueRun(runId: string, accrualDate: string) {
  return post(`/payroll/runs/${runId}/accrue`, { accrualDate })
}
export function reverseRun(runId: string, reversalDate: string) {
  return post(`/payroll/runs/${runId}/reverse`, { reversalDate })
}
export function editRunLine(runId: string, lineId: string, data: any) {
  return post(`/payroll/runs/${runId}/lines/${lineId}/edit`, data)
}
export function voidRun(runId: string) {
  return post(`/payroll/runs/${runId}/void`, {})
}
export function printChecks(runId: string, data: any) {
  return post(`/payroll/runs/${runId}/print-checks`, data)
}
export function getNachaFile(runId: string) {
  return get(`/payroll/runs/${runId}/ach-nacha`)
}
export function getCertifiedPayroll(runId: string) {
  return get(`/payroll/runs/${runId}/certified-payroll`)
}

// --- Expense reports ---
export function createExpenseReport(data: any) {
  return post('/payroll/expenses', data)
}
export function addExpenseLine(reportId: string, data: any) {
  return post(`/payroll/expenses/${reportId}/lines`, data)
}
export function submitExpense(reportId: string) {
  return post(`/payroll/expenses/${reportId}/submit`, {})
}
export function approveExpense(reportId: string, data: any) {
  return post(`/payroll/expenses/${reportId}/approve`, data)
}
export function reimburseExpense(reportId: string) {
  return post(`/payroll/expenses/${reportId}/reimburse`, {})
}

// --- Deductions / benefits ---
export function createDeductionBenefit(data: any) {
  return post('/payroll/deduction-benefits', data)
}
export function getDeductionBenefits() {
  return get('/payroll/deduction-benefits')
}
export function enrollDeductionBenefit(employeeId: string, data: any) {
  return post(`/payroll/employees/${employeeId}/deduction-benefits`, data)
}

// --- W-4 / withholding ---
export function createW4(employeeId: string, data: any) {
  return post(`/payroll/employees/${employeeId}/w4`, data)
}
export function getW4(employeeId: string) {
  return get(`/payroll/employees/${employeeId}/w4`)
}
export function computeWithholding(employeeId: string, taxableWages: number, payFrequency: number) {
  return post('/payroll/withholding/compute', { employeeId, taxableWages, payFrequency })
}
export function createWageBaseLimit(data: any) {
  return post('/payroll/wage-base-limits', data)
}
export function getWageBaseLimits(year?: number) {
  return get('/payroll/wage-base-limits', year ? { year } : {})
}

// --- Workers' comp ---
export function createWorkersCompClassCode(data: any) {
  return post('/payroll/workers-comp-class-codes', data)
}

// --- PTO ---
export function createPtoLedger(employeeId: string, data: any) {
  return post(`/payroll/employees/${employeeId}/pto-ledger`, data)
}
export function accruePto(ledgerId: string, hours: number, asOf: string) {
  return post(`/payroll/pto-ledgers/${ledgerId}/accrue`, { hours, asOf })
}
export function usePto(ledgerId: string, hours: number, asOf: string) {
  return post(`/payroll/pto-ledgers/${ledgerId}/use`, { hours, asOf })
}
export function getPtoLedgers(employeeId?: string) {
  return get('/payroll/pto-ledgers', employeeId ? { employeeId } : {})
}

// --- Manual checks ---
export function createManualCheck(data: any) {
  return post('/payroll/manual-checks', data)
}

// --- Tax & compliance reports ---
export function getFicaCap(runId: string) {
  return post(`/payroll/runs/${runId}/fica-cap`, {})
}
export function getW2Register(companyId: string, year: number) {
  return get('/payroll/w2-register', { companyId, year })
}
export function getForm941(companyId: string, year: number, quarter: number) {
  return get('/payroll/form-941', { companyId, year, quarter })
}
export function getForm940(companyId: string, year: number) {
  return get('/payroll/reports/form-940', { companyId, year })
}
export function getPayrollRegister(companyId: string, year?: number) {
  return get('/payroll/reports/payroll-register', year ? { companyId, year } : { companyId })
}
export function getPayrollSummary(companyId: string, year: number) {
  return get('/payroll/reports/payroll-summary', { companyId, year })
}
export function getLaborDistribution(companyId: string, year?: number) {
  return get('/payroll/reports/labor-distribution', year ? { companyId, year } : { companyId })
}
export function getGarnishmentRegister(companyId: string) {
  return get('/payroll/reports/garnishment-register', { companyId })
}
// --- Garnishments (CCPA) ---
export function createGarnishment(data: any) {
  return post('/payroll/garnishments', data)
}
export function getGarnishmentsForEmployee(employeeId: string) {
  return get(`/payroll/garnishments/employee/${employeeId}`)
}
export function computeGarnishments(employeeId: string, disposableIncome: number) {
  return post(`/payroll/garnishments/employee/${employeeId}/compute`, { disposableIncome })
}
export function getWageBaseReport(companyId: string, year: number) {
  return get('/payroll/reports/wage-base', { companyId, year })
}
export function getPtoReport(companyId: string) {
  return get('/payroll/reports/pto', { companyId })
}
export function getDirectDepositRegister(companyId: string, runId?: string) {
  return get('/payroll/reports/direct-deposit', runId ? { companyId, runId } : { companyId })
}

// --- Tax tables / jurisdictions / employee tax profile (Batch A) ---
export function getTaxTables(companyId: string, year?: number, stateCode?: string) {
  const p: any = { companyId }
  if (year) p.year = year
  if (stateCode) p.stateCode = stateCode
  return get('/payroll/tax-tables', p)
}
export function createTaxTable(data: any) {
  return post('/payroll/tax-tables', data)
}
export function getTaxJurisdictions(companyId: string, stateCode?: string) {
  const p: any = { companyId }
  if (stateCode) p.stateCode = stateCode
  return get('/payroll/tax-jurisdictions', p)
}
export function createTaxJurisdiction(data: any) {
  return post('/payroll/tax-jurisdictions', data)
}
export function getEmployeeTaxProfile(employeeId: string, companyId: string) {
  return get(`/payroll/employees/${employeeId}/tax-profile`, { companyId })
}
export function createEmployeeTaxProfile(employeeId: string, data: any) {
  return post(`/payroll/employees/${employeeId}/tax-profile`, data)
}
export function updateEmployeeTaxProfile(employeeId: string, data: any) {
  return put(`/payroll/employees/${employeeId}/tax-profile`, data)
}

// --- Direct deposit accounts (Batch B) ---
export function getDirectDeposits(employeeId: string) {
  return get(`/payroll/employees/${employeeId}/direct-deposits`)
}
export function createDirectDeposit(employeeId: string, data: any) {
  return post(`/payroll/employees/${employeeId}/direct-deposits`, data)
}
export function updateDirectDeposit(id: string, data: any) {
  return put(`/payroll/direct-deposits/${id}`, data)
}
export function deleteDirectDeposit(id: string) {
  return del(`/payroll/direct-deposits/${id}`)
}

// --- Payroll setup / PTO / new-hire / ACH returns (Batch C) ---
export function getCompanySetup(companyId: string) {
  return get(`/payroll/company-setup?companyId=${companyId}`)
}
export function createCompanySetup(data: any) {
  return post(`/payroll/company-setup`, data)
}
export function getPtoPolicies() {
  return get(`/payroll/pto-policies`)
}
export function createPtoPolicy(data: any) {
  return post(`/payroll/pto-policies`, data)
}
export function getNewHireConfigs(companyId: string) {
  return get(`/payroll/new-hire-configs?companyId=${companyId}`)
}
export function createNewHireConfig(data: any) {
  return post(`/payroll/new-hire-configs`, data)
}
export function getAchReturns(companyId: string) {
  return get(`/payroll/ach-returns?companyId=${companyId}`)
}
export function createAchReturn(data: any) {
  return post(`/payroll/ach-returns`, data)
}
export function processAchReturn(id: string) {
  return post(`/payroll/ach-returns/${id}/process`, {})
}

// --- Tax deposit scheduling (Batch D) ---
export function getTaxDeposits(companyId: string) {
  return get(`/payroll/tax-deposits?companyId=${companyId}`)
}
export function createTaxDeposit(data: any) {
  return post(`/payroll/tax-deposits`, data)
}
export function generateTaxDeposits(runId: string, frequency: string) {
  return post(`/payroll/tax-deposits/generate/${runId}?frequency=${encodeURIComponent(frequency)}`, {})
}
export function markTaxDeposited(id: string, data: any) {
  return post(`/payroll/tax-deposits/${id}/deposit`, data)
}

// --- Batch E: compliance & statutory reporting ---
export function getPositivePay(companyId: string, from?: string, to?: string) {
  const p = `companyId=${companyId}${from ? '&from=' + from : ''}${to ? '&to=' + to : ''}`
  return get(`/payroll/reports/positive-pay?${p}`)
}
export function get1099Nec(companyId: string, year: number) {
  return get(`/payroll/reports/1099-nec?companyId=${companyId}&year=${year}`)
}
export function getMultiStateWithholding(employeeId: string, taxableWages: number) {
  return get(`/payroll/reports/multi-state-withholding?employeeId=${employeeId}&taxableWages=${taxableWages}`)
}
export function getUnionReport(companyId: string) {
  return get(`/payroll/reports/union?companyId=${companyId}`)
}
export function getWorkersCompReport(companyId: string) {
  return get(`/payroll/reports/workers-comp?companyId=${companyId}`)
}
export function terminateEmployee(employeeId: string, body: any) {
  return post(`/payroll/employees/${employeeId}/terminate`, body)
}
