export interface GlJournalEntryLine {
  id: string
  accountId: string
  debit: number
  credit: number
  reference?: string | null
  segmentsJson?: string | null
}

export interface GlJournalBatch {
  id: string
  companyId: string
  batchNumber: string
  description: string
  postingDate: string
  fiscalPeriodId: string
  status: string | number
  totalDebits: number
  totalCredits: number
  isBalanced: boolean
  createdOn: string
  modifiedOn?: string | null
  lines: GlJournalEntryLine[]
}

export interface CreateGlJournalEntryLineRequest {
  accountId: string
  debit?: number | null
  credit?: number | null
  reference?: string | null
  segmentsJson?: string | null
}

export interface CreateGlJournalBatchRequest {
  companyId: string
  batchNumber: string
  description: string
  postingDate: string
  fiscalPeriodId: string
  lines: CreateGlJournalEntryLineRequest[]
}

export type AddGlJournalBatchLineRequest = CreateGlJournalEntryLineRequest

export interface ReverseGlBatchRequest {
  reason: string
}

export interface GlRecurringTemplateLine {
  id: string
  accountId: string
  fixedDebit: number
  fixedCredit: number
  variablePct?: number | null
  reference?: string | null
}

export interface GlRecurringTemplate {
  id: string
  companyId: string
  name: string
  description: string
  frequency: number
  nextRunDate: string
  lastRunDate?: string | null
  isActive: boolean
  lines: GlRecurringTemplateLine[]
}

export interface CreateGlRecurringTemplateRequest {
  companyId: string
  name: string
  description: string
  frequency: number
  nextRunDate: string
  isActive: boolean
}

export interface UpdateGlRecurringTemplateRequest {
  name: string
  description: string
  frequency: number
  nextRunDate: string
  isActive: boolean
}

export interface AddGlRecurringTemplateLineRequest {
  accountId: string
  fixedDebit?: number | null
  fixedCredit?: number | null
  variablePct?: number | null
  reference?: string | null
}

export interface GenerateGlFromRecurringRequest {
  batchNumber: string
  fiscalPeriodId: string
  postingDate: string
}

export interface GlAllocationRuleLine {
  id: string
  targetAccountId: string
  percentage: number
  fixedAmount?: number | null
  reference?: string | null
}

export interface GlAllocationRule {
  id: string
  companyId: string
  name: string
  description: string
  sourceAccountId: string
  method: string | number
  isActive: boolean
  lines: GlAllocationRuleLine[]
}

export interface CreateGlAllocationRuleRequest {
  companyId: string
  name: string
  description: string
  sourceAccountId: string
  method: string | number
  isActive: boolean
}

export interface UpdateGlAllocationRuleRequest {
  name: string
  description: string
  sourceAccountId: string
  method: string | number
  isActive: boolean
}

export interface AddGlAllocationRuleLineRequest {
  targetAccountId: string
  percentage: number
  fixedAmount?: number | null
  reference?: string | null
}

export interface ExecuteGlAllocationRequest {
  batchNumber: string
  sourceAmount: number
  fiscalPeriodId: string
  postingDate: string
}

export interface GlBudgetLine {
  id: string
  accountId: string
  periodNumber: number
  amount: number
  projectId?: string | null
}

export interface GlBudget {
  id: string
  companyId: string
  fiscalYearId: string
  name: string
  description: string
  budgetType: number
  isActive: boolean
  totalAmount: number
  lines: GlBudgetLine[]
}

export interface CreateGlBudgetRequest {
  companyId: string
  fiscalYearId: string
  name: string
  description: string
  budgetType: number
}

export interface AddGlBudgetLineRequest {
  accountId: string
  periodNumber: number
  amount: number
  projectId?: string | null
}

export interface TrialBalanceLine {
  accountId: string
  accountNumber: string
  accountDescription: string
  accountType: number
  normalBalance: number
  beginningBalance: number
  debit: number
  credit: number
  endingBalance: number
}

export interface TrialBalanceReport {
  companyId: string
  companyName: string
  fiscalPeriodId?: string | null
  totalDebit: number
  totalCredit: number
  lines: TrialBalanceLine[]
  generatedOn: string
}

export interface GeneralLedgerDetailLine {
  batchId: string
  batchNumber: string
  postingDate: string
  reference?: string | null
  accountId: string
  accountNumber: string
  accountDescription: string
  debit: number
  credit: number
  segmentsJson?: string | null
}

export interface GeneralLedgerDetailReport {
  companyId: string
  companyName: string
  fiscalPeriodId?: string | null
  fromDate?: string | null
  toDate?: string | null
  totalDebit: number
  totalCredit: number
  lines: GeneralLedgerDetailLine[]
  generatedOn: string
}

export interface UnpostedTransaction {
  batchId: string
  batchNumber: string
  description: string
  postingDate: string
  status: string
  lineCount: number
  totalDebits: number
  totalCredits: number
  createdOn: string
}

export interface UnpostedTransactionsReport {
  companyId: string
  companyName: string
  batches: UnpostedTransaction[]
  generatedOn: string
}

export interface FinancialStatementLine {
  accountId: string
  accountNumber: string
  accountDescription: string
  balance: number
}

export interface FinancialStatementReport {
  companyId: string
  companyName: string
  fiscalPeriodId?: string | null
  statementType: string
  totalAmount: number
  lines: FinancialStatementLine[]
  generatedOn: string
}

export interface CashFlowLine {
  category: string
  accountId?: string | null
  accountNumber: string
  accountDescription: string
  amount: number
}

export interface CashFlowReport {
  companyId: string
  companyName: string
  fiscalPeriodId?: string | null
  netCashOperating: number
  netCashInvesting: number
  netCashFinancing: number
  netCashChange: number
  lines: CashFlowLine[]
  generatedOn: string
}

export interface BudgetVsActualLine {
  accountId: string
  accountNumber: string
  accountDescription: string
  budgetAmount: number
  actualAmount: number
  variance: number
  variancePercent: number
}

export interface BudgetVsActualReport {
  companyId: string
  companyName: string
  budgetId: string
  budgetName: string
  fiscalPeriodId?: string | null
  totalBudget: number
  totalActual: number
  totalVariance: number
  lines: BudgetVsActualLine[]
  generatedOn: string
}

export interface AccountDistributionLine {
  accountId: string
  accountNumber: string
  accountDescription: string
  accountType: number
  debit: number
  credit: number
  netChange: number
  transactionCount: number
}

export interface AccountDistributionReport {
  companyId: string
  companyName: string
  fiscalPeriodId?: string | null
  fromDate?: string | null
  toDate?: string | null
  totalDebit: number
  totalCredit: number
  lines: AccountDistributionLine[]
  generatedOn: string
}

export interface ConsolidatedTrialBalanceLine {
  accountId: string
  accountNumber: string
  accountDescription: string
  accountType: number
  normalBalance: number
  beginningBalance: number
  debit: number
  credit: number
  endingBalance: number
  companyId: string
  companyName: string
}

export interface ConsolidatedTrialBalanceReport {
  parentCompanyId: string
  parentCompanyName: string
  fiscalPeriodId?: string | null
  totalDebit: number
  totalCredit: number
  lines: ConsolidatedTrialBalanceLine[]
  generatedOn: string
}

export interface IntercompanyBalanceLine {
  fromCompanyId: string
  fromCompanyName: string
  toCompanyId: string
  toCompanyName: string
  fromAccountNumber: string
  toAccountNumber: string
  balance: number
}

export interface IntercompanyBalanceReport {
  parentCompanyId: string
  parentCompanyName: string
  fiscalPeriodId?: string | null
  lines: IntercompanyBalanceLine[]
  generatedOn: string
}

export interface MultiCurrencyRevaluationLine {
  accountId: string
  accountNumber: string
  accountDescription: string
  currency: string
  originalBalance: number
  revaluedBalance: number
  gainLoss: number
}

export interface MultiCurrencyRevaluationReport {
  companyId: string
  companyName: string
  fiscalPeriodId: string
  revaluationDate: string
  totalGainLoss: number
  lines: MultiCurrencyRevaluationLine[]
  generatedOn: string
}

export interface ConsolidationRunDto {
  id: string
  parentCompanyId: string
  description: string
  consolidationDate: string
  fiscalPeriodId: string
  status: number
  errorMessage?: string | null
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateConsolidationRunRequest {
  parentCompanyId: string
  description: string
  consolidationDate: string
  fiscalYear: number
  fiscalPeriod: number
}

export interface IntercompanyMappingDto {
  id: string
  fromCompanyId: string
  toCompanyId: string
  fromAccountNumber: string
  toAccountNumber: string
  description: string
  isActive: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateIntercompanyMappingRequest {
  fromCompanyId: string
  toCompanyId: string
  fromAccountNumber: string
  toAccountNumber: string
  description: string
}

export interface UpdateIntercompanyMappingRequest {
  fromAccountNumber: string
  toAccountNumber: string
  description: string
}

// --- Multi-Currency Revaluation ---
export interface RevaluationLinePreview {
  accountId: string
  accountNumber: string
  originalDebit: number
  originalCredit: number
  revaluedDebit: number
  revaluedCredit: number
  gainLoss: number
}

export interface RevaluationPreview {
  linesToRevalue: number
  estimatedGainLoss: number
  lines: RevaluationLinePreview[]
}

export interface RevaluationResult {
  batchId: string
  batchNumber: string
  linesRevalued: number
  totalGainLoss: number
  createdOn: string
}

export interface RevaluationPreviewRequest {
  companyId: string
  fiscalPeriodId: string
  revaluationDate: string
}

export interface RevaluationExecuteRequest {
  companyId: string
  fiscalPeriodId: string
  revaluationDate: string
  revaluationReason: string
}

// --- Phase 2 gap features ---
export type YearEndCloseStatus = 'Pending' | 'Completed' | 'Failed' | 'Reopened'

export interface YearEndCloseRunDto {
  id: string
  companyId: string
  fiscalYearId: string
  retainedEarningsAccountId: string
  closedOn: string
  closedBy: string
  totalRevenue: number
  totalExpense: number
  retainedEarningsAmount: number
  status: YearEndCloseStatus
}

export interface CloseYearEndRequest {
  companyId: string
  fiscalYearId: string
  retainedEarningsAccountId: string
  closedBy: string
}

export type SuspenseStatus = 'Pending' | 'Resolved' | 'Discarded'

export interface PostingSuspenseItemDto {
  id: string
  companyId: string
  sourceModule: string
  sourceReference: string
  accountId?: string | null
  debit: number
  credit: number
  currencyId?: string | null
  reasonCode: string
  errorMessage: string
  status: SuspenseStatus
  resolvedBatchId?: string | null
}

export interface ResolveSuspenseRequest {
  accountId: string
  debit: number
  credit: number
}

export interface DiscardSuspenseRequest {
  note?: string | null
}

export interface IntercompanyDueToFromRequest {
  companyId: string
  fromCompanyId: string
  toCompanyId: string
  amount: number
  dueFromAccountId: string
  dueToAccountId: string
  offsetAccountId: string
  reason: string
}

export interface PrePostingEditLine {
  batchId: string
  batchNumber: string
  accountNumber: string
  accountId: string
  debit: number
  credit: number
  reference: string
  segmentsJson: string
  postingDate: string
  status: string
}

export interface PeriodEndChecklistItem {
  name: string
  passed: boolean
  detail: string
}

export interface RollForwardBudgetRequest {
  targetFiscalYearId: string
}

export interface TransferBudgetRequest {
  accountId: string
  fromPeriodNumber: number
  toPeriodNumber: number
  amount: number
  reason: string
}

export interface AccountInquiryDto {
  accountId: string
  accountNumber: string
  description: string
  accountType: string
  normalBalance: string
  isActive: boolean
  companyId: string
  totalDebit: number
  totalCredit: number
  netBalance: number
  periodBalances: AccountPeriodBalanceDto[]
  lines: AccountInquiryLineDto[]
}

export interface AccountPeriodBalanceDto {
  fiscalPeriodId: string
  periodNumber: number
  periodName: string
  debit: number
  credit: number
  net: number
}

export interface AccountInquiryLineDto {
  batchId: string
  batchNumber: string
  postingDate: string
  status: string
  debit: number
  credit: number
  reference: string
  sourceDocument: string
  segmentsJson: string
}
