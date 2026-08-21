import type {
  Account,
  AuditLog,
  Company,
  CreateAccountRequest,
  CreateCompanyRequest,
  CreateCurrencyRequest,
  CreateExchangeRateRequest,
  CreateFiscalPeriodRequest,
  CreateFiscalYearRequest,
  CreateNumberSequenceRequest,
  CreatePlatformUserRequest,
  CreateRoleRequest,
  CreateSegmentTypeRequest,
  CreateSegmentValueRequest,
  Currency,
  ExchangeRate,
  FiscalPeriod,
  FiscalYear,
  NumberSequence,
  PlatformUser,
  Role,
  SegmentType,
  SegmentValue,
  UpdateAccountRequest,
  UpdateCompanyRequest,
  UpdateCurrencyRequest,
  UpdateExchangeRateRequest,
  UpdateFiscalYearRequest,
  UpdateNumberSequenceRequest,
  UpdatePlatformUserRequest,
  UpdateRoleRequest,
  UpdateSegmentTypeRequest,
  UpdateSegmentValueRequest,
  ApprovalWorkflow,
  CreateApprovalWorkflowRequest,
  ApprovalStep,
  CreateApprovalStepRequest,
  SoDRule,
  CreateSoDRuleRequest,
  SoDConflict,
  CompanySetupReport,
  ChartOfAccountsReport,
  FiscalCalendarReport,
  SecurityMatrixReport,
  PeriodClosePreview,
  ApiKey,
  ApiKeyCreated,
  CreateApiKeyRequest,
  UpdateApiKeyScopesRequest,
  ApprovalDelegation,
  CreateApprovalDelegationRequest,
  ApprovalEscalationPolicy,
  CreateApprovalEscalationRequest,
  UpdateApprovalEscalationRequest,
  HolidayCalendarEntry,
  CreateHolidayCalendarRequest,
  UpdateHolidayCalendarRequest,
} from '@/types/platform'
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

export function companyId(): string {
  const current = useAuthStore.getState().currentCompany
  // Empty id is the "All Companies" sentinel for super admins: omit the
  // companyId query param so the backend returns data across every company.
  return current?.id ?? ''
}

export function getCompanies(): Promise<Company[]> {
  return get('/platform/companies')
}

export function getCompany(id: string): Promise<Company> {
  return get(`/platform/companies/${id}`)
}

export function createCompany(data: CreateCompanyRequest): Promise<Company> {
  return post('/platform/companies', data)
}

export function updateCompany(id: string, data: UpdateCompanyRequest): Promise<Company> {
  return put(`/platform/companies/${id}`, data)
}

export function deleteCompany(id: string): Promise<void> {
  return del(`/platform/companies/${id}`)
}

export function getFiscalYears(companyIdParam?: string): Promise<FiscalYear[]> {
  return get('/platform/fiscal-years', { companyId: companyIdParam ?? companyId() })
}

export function createFiscalYear(data: CreateFiscalYearRequest): Promise<FiscalYear> {
  return post('/platform/fiscal-years', data)
}

export function updateFiscalYear(id: string, data: UpdateFiscalYearRequest): Promise<FiscalYear> {
  return put(`/platform/fiscal-years/${id}`, data)
}

export function getFiscalPeriods(companyIdParam?: string): Promise<FiscalPeriod[]> {
  return get('/platform/fiscal-periods', { companyId: companyIdParam ?? companyId() })
}

export function getFiscalPeriod(id: string): Promise<FiscalPeriod> {
  return get(`/platform/fiscal-periods/${id}`)
}

export function getCurrentFiscalPeriod(companyIdParam?: string): Promise<FiscalPeriod> {
  return get('/platform/fiscal-periods/current', { companyId: companyIdParam ?? companyId() })
}

export function createFiscalPeriod(data: CreateFiscalPeriodRequest): Promise<FiscalPeriod> {
  return post('/platform/fiscal-periods', data)
}

export function closeFiscalPeriod(id: string): Promise<void> {
  return post(`/platform/fiscal-periods/${id}/close`)
}

export function openFiscalPeriod(id: string): Promise<void> {
  return post(`/platform/fiscal-periods/${id}/open`)
}

export function getAccounts(companyIdParam?: string): Promise<Account[]> {
  return get('/platform/accounts', { companyId: companyIdParam ?? companyId() })
}

export function createAccount(data: CreateAccountRequest): Promise<Account> {
  return post('/platform/accounts', data)
}

export function updateAccount(id: string, data: UpdateAccountRequest): Promise<Account> {
  return put(`/platform/accounts/${id}`, data)
}

export function deleteAccount(id: string): Promise<void> {
  return del(`/platform/accounts/${id}`)
}

export function getSegmentTypes(companyIdParam?: string): Promise<SegmentType[]> {
  return get('/platform/segment-types', { companyId: companyIdParam ?? companyId() })
}

export function createSegmentType(data: CreateSegmentTypeRequest): Promise<SegmentType> {
  return post('/platform/segment-types', data)
}

export function updateSegmentType(id: string, data: UpdateSegmentTypeRequest): Promise<SegmentType> {
  return put(`/platform/segment-types/${id}`, data)
}

export function deleteSegmentType(id: string): Promise<void> {
  return del(`/platform/segment-types/${id}`)
}

export function getSegmentValues(companyIdParam?: string): Promise<SegmentValue[]> {
  return get('/platform/segment-values', { companyId: companyIdParam ?? companyId() })
}

export function createSegmentValue(data: CreateSegmentValueRequest): Promise<SegmentValue> {
  return post('/platform/segment-values', data)
}

export function updateSegmentValue(id: string, data: UpdateSegmentValueRequest): Promise<SegmentValue> {
  return put(`/platform/segment-values/${id}`, data)
}

export function deleteSegmentValue(id: string): Promise<void> {
  return del(`/platform/segment-values/${id}`)
}

export function getCurrencies(): Promise<Currency[]> {
  return get('/platform/currencies')
}

export function createCurrency(data: CreateCurrencyRequest): Promise<Currency> {
  return post('/platform/currencies', data)
}

export function updateCurrency(id: string, data: UpdateCurrencyRequest): Promise<Currency> {
  return put(`/platform/currencies/${id}`, data)
}

export function getExchangeRates(companyIdParam?: string): Promise<ExchangeRate[]> {
  return get('/platform/exchange-rates', { companyId: companyIdParam ?? companyId() })
}

export function createExchangeRate(data: CreateExchangeRateRequest): Promise<ExchangeRate> {
  return post('/platform/exchange-rates', data)
}

export function updateExchangeRate(id: string, data: UpdateExchangeRateRequest): Promise<ExchangeRate> {
  return put(`/platform/exchange-rates/${id}`, data)
}

export function getNumberSequences(companyIdParam?: string): Promise<NumberSequence[]> {
  return get('/platform/number-sequences', { companyId: companyIdParam ?? companyId() })
}

export function createNumberSequence(data: CreateNumberSequenceRequest): Promise<NumberSequence> {
  return post('/platform/number-sequences', data)
}

export function updateNumberSequence(id: string, data: UpdateNumberSequenceRequest): Promise<NumberSequence> {
  return put(`/platform/number-sequences/${id}`, data)
}

export function getUsers(): Promise<PlatformUser[]> {
  return get('/platform/users')
}

export function createUser(data: CreatePlatformUserRequest): Promise<PlatformUser> {
  return post('/platform/users', data)
}

export function updateUser(id: string, data: UpdatePlatformUserRequest): Promise<PlatformUser> {
  return put(`/platform/users/${id}`, data)
}

export function assignUserRole(id: string, roleId: string, companyId?: string | null): Promise<void> {
  return post(`/platform/users/${id}/roles`, { roleId, companyId: companyId ?? null })
}

export function removeUserRole(id: string, roleId: string, companyId?: string | null): Promise<void> {
  return del(`/platform/users/${id}/roles/${roleId}${companyId ? `?companyId=${companyId}` : ''}`)
}

export function activateUser(id: string): Promise<void> {
  return post(`/platform/users/${id}/activate`)
}

export function deactivateUser(id: string): Promise<void> {
  return post(`/platform/users/${id}/deactivate`)
}

export function deleteUser(id: string): Promise<void> {
  return del(`/platform/users/${id}`)
}

export function getRoles(): Promise<Role[]> {
  return get('/platform/roles')
}

export function createRole(data: CreateRoleRequest): Promise<Role> {
  return post('/platform/roles', data)
}

export function updateRole(id: string, data: UpdateRoleRequest): Promise<Role> {
  return put(`/platform/roles/${id}`, data)
}

export function deleteRole(id: string): Promise<void> {
  return del(`/platform/roles/${id}`)
}

export function assignRolePermission(id: string, permissionId: string): Promise<void> {
  return post(`/platform/roles/${id}/permissions`, { permissionId })
}

export function removeRolePermission(id: string, permissionId: string): Promise<void> {
  return del(`/platform/roles/${id}/permissions/${permissionId}`)
}

export function getAuditLogsByEntity(entityType: string, entityId: string): Promise<AuditLog[]> {
  return get(`/platform/audit-logs/entity/${entityType}/${entityId}`)
}

export function getAuditLogsByUser(
  performedBy: string,
  params?: { from?: string; to?: string }
): Promise<AuditLog[]> {
  return get(`/platform/audit-logs/user/${performedBy}`, {
    from: params?.from,
    to: params?.to,
  })
}

// --- Approval Workflow ---
export function getApprovalWorkflows(moduleParam?: string): Promise<ApprovalWorkflow[]> {
  return get('/platform/approval-workflows', moduleParam ? { module: moduleParam } : undefined)
}
export function createApprovalWorkflow(data: CreateApprovalWorkflowRequest): Promise<ApprovalWorkflow> {
  return post('/platform/approval-workflows', data)
}
export function activateApprovalWorkflow(id: string): Promise<void> {
  return post(`/platform/approval-workflows/${id}/activate`)
}
export function deactivateApprovalWorkflow(id: string): Promise<void> {
  return post(`/platform/approval-workflows/${id}/deactivate`)
}
export function addApprovalStep(workflowId: string, data: CreateApprovalStepRequest): Promise<ApprovalStep> {
  return post(`/platform/approval-workflows/${workflowId}/steps`, data)
}
export function removeApprovalStep(workflowId: string, stepId: string): Promise<void> {
  return del(`/platform/approval-workflows/${workflowId}/steps/${stepId}`)
}

// --- Segregation of Duties ---
export function getSoDRules(moduleParam?: string): Promise<SoDRule[]> {
  return get('/platform/sod/rules', moduleParam ? { module: moduleParam } : undefined)
}
export function createSoDRule(data: CreateSoDRuleRequest): Promise<SoDRule> {
  return post('/platform/sod/rules', data)
}
export function getSoDConflicts(): Promise<SoDConflict[]> {
  return get('/platform/sod/conflicts')
}

// --- Reports ---
export function getCompanySetupReport(): Promise<CompanySetupReport> {
  return get('/platform/reports/company-setup')
}
export function getChartOfAccountsReport(): Promise<ChartOfAccountsReport> {
  return get('/platform/reports/chart-of-accounts')
}
export function getFiscalCalendarReport(): Promise<FiscalCalendarReport> {
  return get('/platform/reports/fiscal-calendar')
}
export function getSecurityMatrixReport(): Promise<SecurityMatrixReport> {
  return get('/platform/reports/security-matrix')
}

// --- Fiscal calendar enhancements ---
export function generatePeriods(fiscalYearId: string): Promise<FiscalPeriod[]> {
  return post(`/platform/fiscal-years/${fiscalYearId}/generate-periods`)
}
export function getPeriodClosePreview(periodId: string): Promise<PeriodClosePreview> {
  return get(`/platform/fiscal-periods/${periodId}/close-preview`)
}

// --- API keys / machine identity ---
export function getApiKeys(companyId: string): Promise<ApiKey[]> {
  return get('/platform/api-keys', { companyId })
}
export function createApiKey(data: CreateApiKeyRequest): Promise<ApiKeyCreated> {
  return post('/platform/api-keys', data)
}
export function updateApiKeyScopes(id: string, data: UpdateApiKeyScopesRequest): Promise<ApiKey> {
  return put(`/platform/api-keys/${id}/scopes`, data)
}
export function activateApiKey(id: string): Promise<void> {
  return post(`/platform/api-keys/${id}/activate`)
}
export function deactivateApiKey(id: string): Promise<void> {
  return post(`/platform/api-keys/${id}/deactivate`)
}
export function deleteApiKey(id: string): Promise<void> {
  return del(`/platform/api-keys/${id}`)
}

// --- Approval delegation & escalation ---
export function getApprovalDelegations(): Promise<ApprovalDelegation[]> {
  return get('/platform/approval-delegations')
}
export function createApprovalDelegation(data: CreateApprovalDelegationRequest): Promise<ApprovalDelegation> {
  return post('/platform/approval-delegations', data)
}
export function revokeApprovalDelegation(id: string): Promise<void> {
  return post(`/platform/approval-delegations/${id}/revoke`)
}
export function deleteApprovalDelegation(id: string): Promise<void> {
  return del(`/platform/approval-delegations/${id}`)
}
export function getApprovalEscalations(workflowId?: string): Promise<ApprovalEscalationPolicy[]> {
  return get('/platform/approval-escalations', workflowId ? { workflowId } : undefined)
}
export function createApprovalEscalation(data: CreateApprovalEscalationRequest): Promise<ApprovalEscalationPolicy> {
  return post('/platform/approval-escalations', data)
}
export function updateApprovalEscalation(id: string, data: UpdateApprovalEscalationRequest): Promise<ApprovalEscalationPolicy> {
  return put(`/platform/approval-escalations/${id}`, data)
}
export function deleteApprovalEscalation(id: string): Promise<void> {
  return del(`/platform/approval-escalations/${id}`)
}

// --- Holiday calendar ---
export function getHolidayCalendar(companyId: string, year?: number): Promise<HolidayCalendarEntry[]> {
  return get('/platform/holiday-calendar', year ? { companyId, year } : { companyId })
}
export function createHolidayEntry(data: CreateHolidayCalendarRequest): Promise<HolidayCalendarEntry> {
  return post('/platform/holiday-calendar', data)
}
export function updateHolidayEntry(id: string, data: UpdateHolidayCalendarRequest): Promise<HolidayCalendarEntry> {
  return put(`/platform/holiday-calendar/${id}`, data)
}
export function deleteHolidayEntry(id: string): Promise<void> {
  return del(`/platform/holiday-calendar/${id}`)
}
export function advanceHolidayDate(companyId: string, from: string, businessDays: number): Promise<string> {
  return get('/platform/holiday-calendar/advance', { companyId, from, businessDays })
}
