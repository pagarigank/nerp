export interface Company {
  id: string
  name: string
  legalName: string
  baseCurrency: string
  taxId?: string | null
  address?: string | null
  isActive: boolean
  parentCompanyId?: string
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateCompanyRequest {
  name: string
  legalName: string
  baseCurrency: string
  taxId?: string | null
  address?: string | null
}

export type UpdateCompanyRequest = CreateCompanyRequest

export interface FiscalYear {
  id: string
  companyId: string
  year: number
  description: string
  startDate: string
  endDate: string
  isClosed: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateFiscalYearRequest {
  companyId: string
  year: number
  description: string
  startDate: string
  endDate: string
}

export interface UpdateFiscalYearRequest {
  description: string
  startDate: string
  endDate: string
}

export interface FiscalPeriod {
  id: string
  fiscalYearId: string
  companyId: string
  periodNumber: number
  description: string
  startDate: string
  endDate: string
  status: number
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateFiscalPeriodRequest {
  fiscalYearId: string
  companyId: string
  periodNumber: number
  description: string
  startDate: string
  endDate: string
}

export interface Account {
  id: string
  companyId: string
  accountNumber: string
  description: string
  accountType: number
  normalBalance: number
  isActive: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateAccountRequest {
  companyId: string
  accountNumber: string
  description: string
  accountType: number
  normalBalance: number
  isActive: boolean
}

export interface UpdateAccountRequest {
  description: string
  accountType: number
  normalBalance: number
  isActive: boolean
}

export interface SegmentType {
  id: string
  companyId: string
  name: string
  code: string
  displayOrder: number
  isRequired: boolean
  isActive: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateSegmentTypeRequest {
  companyId: string
  name: string
  code: string
  displayOrder: number
  isRequired: boolean
}

export interface UpdateSegmentTypeRequest {
  name: string
  code: string
  displayOrder: number
  isRequired: boolean
}

export interface SegmentValue {
  id: string
  segmentTypeId: string
  companyId: string
  value: string
  description: string
  displayOrder: number
  isActive: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateSegmentValueRequest {
  segmentTypeId: string
  companyId: string
  value: string
  description: string
  displayOrder: number
}

export interface UpdateSegmentValueRequest {
  value: string
  description: string
  displayOrder: number
}

export interface Currency {
  id: string
  code: string
  name: string
  symbol: string
  decimalPlaces: number
  isActive: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateCurrencyRequest {
  code: string
  name: string
  symbol: string
  decimalPlaces: number
}

export interface UpdateCurrencyRequest {
  name: string
  symbol: string
  decimalPlaces: number
}

export interface ExchangeRate {
  id: string
  companyId: string
  fromCurrency: string
  toCurrency: string
  rate: number
  effectiveDate: string
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateExchangeRateRequest {
  companyId: string
  fromCurrency: string
  toCurrency: string
  rate: number
  effectiveDate: string
}

export interface UpdateExchangeRateRequest {
  rate: number
  effectiveDate: string
}

export interface NumberSequence {
  id: string
  companyId: string
  name: string
  prefix: string
  nextValue: number
  increment: number
  minValue: number
  maxValue: number
  isActive: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateNumberSequenceRequest {
  companyId: string
  name: string
  prefix: string
  nextValue: number
  increment: number
  minValue: number
  maxValue: number
}

export interface UpdateNumberSequenceRequest {
  name: string
  prefix: string
  increment: number
  minValue: number
  maxValue: number
}

export interface PlatformUser {
  id: string
  username: string
  email: string
  displayName: string
  phoneNumber?: string | null
  isActive: boolean
  lastLoginAt?: string | null
  createdOn: string
  modifiedOn?: string | null
  roles: UserRoleRef[]
}

export interface UserRoleRef {
  roleId: string
  roleName: string
  companyId?: string | null
  companyName?: string | null
  isGlobal: boolean
}

export interface CreatePlatformUserRequest {
  username: string
  email: string
  displayName: string
  phoneNumber?: string | null
  password: string
}

export interface UpdatePlatformUserRequest {
  email: string
  displayName: string
  phoneNumber?: string | null
  password?: string | null
}

export interface Role {
  id: string
  name: string
  description: string
  isActive: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateRoleRequest {
  name: string
  description: string
}

export interface UpdateRoleRequest {
  name: string
  description: string
}

export interface AuditLog {
  id: string
  action: string
  entityType: string
  entityId: string
  performedBy: string
  performedOn: string
  ipAddress?: string | null
  userAgent?: string | null
  correlationId?: string | null
  oldValues?: string | null
  newValues?: string | null
}

// --- Approval Workflow ---
export interface ApprovalStep {
  id: string
  workflowId: string
  stepOrder: number
  description: string
  approverRoleId?: string | null
  specificApproverUserId?: string | null
  requiredApprovals: number
  minAmount?: number | null
  maxAmount?: number | null
}

export interface ApprovalWorkflow {
  id: string
  module: string
  documentType: string
  description: string
  isActive: boolean
  thresholdAmount?: number | null
  steps: ApprovalStep[]
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateApprovalWorkflowRequest {
  module: string
  documentType: string
  description: string
  companyId?: string | null
  thresholdAmount?: number | null
  steps: CreateApprovalStepRequest[]
}

export interface CreateApprovalStepRequest {
  stepOrder: number
  description: string
  approverRoleId?: string | null
  specificApproverUserId?: string | null
  requiredApprovals: number
  minAmount?: number | null
  maxAmount?: number | null
}

export interface UpdateApprovalWorkflowRequest {
  description: string
  thresholdAmount?: number | null
}

// --- Segregation of Duties ---
export interface SoDRule {
  id: string
  module: string
  actionA: string
  actionB: string
  description: string
  documentType?: string | null
  isActive: boolean
  thresholdAmount?: number | null
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateSoDRuleRequest {
  module: string
  actionA: string
  actionB: string
  description: string
  documentType?: string | null
  thresholdAmount?: number | null
}

export interface UpdateSoDRuleRequest {
  module: string
  actionA: string
  actionB: string
  description: string
  documentType?: string | null
  thresholdAmount?: number | null
}

export interface SoDConflict {
  id: string
  ruleId: string
  userId: string
  module: string
  documentType: string
  documentId: string
  conflictType: string
  detectedOn: string
  resolved: boolean
  resolution?: string | null
  resolvedBy?: string | null
  resolvedOn?: string | null
}

// --- Reports ---
export interface ReportUserRef {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
}
export interface ReportRoleRef {
  id: string
  name: string
  description: string
}
export interface ReportAccountRef {
  id: string
  accountNumber: string
  accountName: string
}
export interface ReportFiscalYearRef {
  id: string
  fiscalYear: number
  periodName: string
}
export interface ReportFiscalPeriodRef {
  id: string
  periodNumber: number
  periodName: string
  startDate: string
  endDate: string
}
export interface CompanySetupReport {
  company: unknown
  fiscalYears: ReportFiscalYearRef[]
  segmentTypes: unknown[]
  currencies: unknown[]
  numberSequences: unknown[]
  users: ReportUserRef[]
  roles: ReportRoleRef[]
  generatedOn: string
}
export interface ChartOfAccountsReport {
  companyId: string
  companyName: string
  accounts: ReportAccountRef[]
  segmentTypes: unknown[]
  generatedOn: string
}
export interface FiscalCalendarReport {
  companyId: string
  companyName: string
  fiscalYears: { fiscalYear: ReportFiscalYearRef; periods: ReportFiscalPeriodRef[] }[]
  generatedOn: string
}
export interface SecurityMatrixReport {
  companyId: string
  companyName: string
  roles: ReportRoleRef[]
  users: ReportUserRef[]
  permissions: unknown[]
  rolePermissions: unknown[]
  userRoles: unknown[]
  generatedOn: string
}

// --- Fiscal calendar enhancements ---
export enum FiscalCalendarType {
  Standard = 0,
  Period13 = 1,
  FourFourFive = 2,
}

export enum FiscalYearEndType {
  Calendar = 0,
  NonCalendar = 1,
}

export interface FiscalYear {
  id: string
  companyId: string
  year: number
  description: string
  startDate: string
  endDate: string
  isClosed: boolean
  calendarType: FiscalCalendarType
  yearEndType: FiscalYearEndType
  createdOn: string
  modifiedOn?: string | null
}

export interface PeriodClosePreview {
  periodId: string
  periodNumber: number
  description: string
  startDate: string
  endDate: string
  pendingGlBatches: number
  warnings: string[]
}

// --- API keys / machine identity ---
export interface ApiKey {
  id: string
  companyId: string
  name: string
  keyPrefix: string
  scopes: string[]
  isActive: boolean
  expiresOn?: string | null
  lastUsedOn?: string | null
  createdOn: string
  modifiedOn?: string | null
}

export interface ApiKeyCreated {
  id: string
  name: string
  keyPrefix: string
  secret: string
  expiresOn?: string | null
}

export interface CreateApiKeyRequest {
  companyId: string
  name: string
  scopes: string[]
  expiresOn?: string | null
}

export interface UpdateApiKeyScopesRequest {
  scopes: string[]
}

// --- Approval delegation & escalation ---
export interface ApprovalDelegation {
  id: string
  delegatorUserId: string
  delegateUserId: string
  module?: string | null
  documentType?: string | null
  workflowId?: string | null
  startsOn: string
  endsOn: string
  isActive: boolean
}

export interface CreateApprovalDelegationRequest {
  delegatorUserId: string
  delegateUserId: string
  startsOn: string
  endsOn: string
  module?: string | null
  documentType?: string | null
  workflowId?: string | null
}

export interface ApprovalEscalationPolicy {
  id: string
  workflowId: string
  stepOrder: number
  slaMinutes: number
  escalateToRoleId?: string | null
  escalateToUserId?: string | null
  notifyOnEscalation: boolean
  isActive: boolean
}

export interface CreateApprovalEscalationRequest {
  workflowId: string
  stepOrder: number
  slaMinutes: number
  escalateToRoleId?: string | null
  escalateToUserId?: string | null
  notifyOnEscalation?: boolean
}

export interface UpdateApprovalEscalationRequest {
  slaMinutes: number
  escalateToRoleId?: string | null
  escalateToUserId?: string | null
  notifyOnEscalation: boolean
}

// --- Holiday calendar ---
export interface HolidayCalendarEntry {
  id: string
  companyId: string
  date: string
  description: string
  isWorkingDay: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateHolidayCalendarRequest {
  companyId: string
  date: string
  description: string
  isWorkingDay?: boolean
}

export interface UpdateHolidayCalendarRequest {
  description: string
  isWorkingDay: boolean
}
