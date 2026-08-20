// Payroll module API client (Phase 11).
import { get, post } from './client'
import { companyId as defaultCompanyId } from './inventory'

// --- Employees ---
export function getEmployees(companyIdParam?: string) {
  return get('/payroll/employees', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createEmployee(data: any) {
  return post('/payroll/employees', data)
}

// --- Pay codes ---
export function getPayCodes(companyIdParam?: string) {
  return get('/payroll/pay-codes', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createPayCode(data: any) {
  return post('/payroll/pay-codes', data)
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
export function getCertifiedPayroll(runId: string) {
  return get(`/payroll/runs/${runId}/certified-payroll`)
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
export function terminateGarnishment(id: string) {
  return post(`/payroll/garnishments/${id}/terminate`, {})
}
