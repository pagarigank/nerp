import type {
  AddGlAllocationRuleLineRequest,
  AddGlBudgetLineRequest,
  AddGlJournalBatchLineRequest,
  AddGlRecurringTemplateLineRequest,
  AccountDistributionReport,
  BudgetVsActualReport,
  CashFlowReport,
  CloseYearEndRequest,
  ConsolidatedTrialBalanceReport,
  ConsolidationRunDto,
  CreateConsolidationRunRequest,
  CreateGlAllocationRuleRequest,
  CreateGlBudgetRequest,
  CreateGlJournalBatchRequest,
  CreateGlRecurringTemplateRequest,
  CreateIntercompanyMappingRequest,
  DiscardSuspenseRequest,
  ExecuteGlAllocationRequest,
  FinancialStatementReport,
  GeneralLedgerDetailReport,
  GlAllocationRule,
  GlBudget,
  GlJournalBatch,
  GlRecurringTemplate,
  GenerateGlFromRecurringRequest,
  IntercompanyBalanceReport,
  IntercompanyMappingDto,
  IntercompanyDueToFromRequest,
  MultiCurrencyRevaluationReport,
  PeriodEndChecklistItem,
  PostingSuspenseItemDto,
  PrePostingEditLine,
  ResolveSuspenseRequest,
  ReverseGlBatchRequest,
  RevaluationPreview,
  RevaluationResult,
  RevaluationPreviewRequest,
  RevaluationExecuteRequest,
  RollForwardBudgetRequest,
  SuspenseStatus,
  TransferBudgetRequest,
  TrialBalanceReport,
  UnpostedTransactionsReport,
  UpdateGlAllocationRuleRequest,
  UpdateGlRecurringTemplateRequest,
  UpdateIntercompanyMappingRequest,
  YearEndCloseRunDto,
  AccountInquiryDto,
} from '@/types/gl'
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

function companyId(): string {
  return useAuthStore.getState().currentCompany?.id ?? DEMO_COMPANY_ID
}

export function getJournalBatches(companyIdParam?: string): Promise<GlJournalBatch[]> {
  return get('/gl/journal-batches', { companyId: companyIdParam ?? companyId() })
}

export function getJournalBatch(id: string): Promise<GlJournalBatch> {
  return get(`/gl/journal-batches/${id}`)
}

export function createJournalBatch(data: CreateGlJournalBatchRequest): Promise<GlJournalBatch> {
  return post('/gl/journal-batches', data)
}

export function addJournalBatchLine(id: string, data: AddGlJournalBatchLineRequest): Promise<GlJournalBatch> {
  return post(`/gl/journal-batches/${id}/lines`, data)
}

export function removeJournalBatchLine(id: string, lineId: string): Promise<void> {
  return del(`/gl/journal-batches/${id}/lines/${lineId}`)
}

export function releaseJournalBatch(id: string): Promise<GlJournalBatch> {
  return post(`/gl/journal-batches/${id}/release`)
}

export function postJournalBatch(id: string): Promise<GlJournalBatch> {
  return post(`/gl/journal-batches/${id}/post`)
}

export function reverseJournalBatch(id: string, data: ReverseGlBatchRequest): Promise<GlJournalBatch> {
  return post(`/gl/journal-batches/${id}/reverse`, data)
}

export function getNextJournalBatchNumber(companyIdParam?: string): Promise<string> {
  return get('/gl/journal-batches/next-number', { companyId: companyIdParam ?? companyId() })
}

export function getRecurringTemplates(companyIdParam?: string): Promise<GlRecurringTemplate[]> {
  return get('/gl/recurring-templates', { companyId: companyIdParam ?? companyId() })
}

export function getRecurringTemplate(id: string): Promise<GlRecurringTemplate> {
  return get(`/gl/recurring-templates/${id}`)
}

export function createRecurringTemplate(data: CreateGlRecurringTemplateRequest): Promise<GlRecurringTemplate> {
  return post('/gl/recurring-templates', data)
}

export function updateRecurringTemplate(id: string, data: UpdateGlRecurringTemplateRequest): Promise<GlRecurringTemplate> {
  return put(`/gl/recurring-templates/${id}`, data)
}

export function addRecurringTemplateLine(id: string, data: AddGlRecurringTemplateLineRequest): Promise<GlRecurringTemplate> {
  return post(`/gl/recurring-templates/${id}/lines`, data)
}

export function generateFromRecurring(id: string, data: GenerateGlFromRecurringRequest): Promise<GlJournalBatch> {
  return post(`/gl/recurring-templates/${id}/generate`, data)
}

export function activateRecurringTemplate(id: string): Promise<void> {
  return post(`/gl/recurring-templates/${id}/activate`)
}

export function deactivateRecurringTemplate(id: string): Promise<void> {
  return post(`/gl/recurring-templates/${id}/deactivate`)
}

export function getAllocationRules(companyIdParam?: string): Promise<GlAllocationRule[]> {
  return get('/gl/allocation-rules', { companyId: companyIdParam ?? companyId() })
}

export function getAllocationRule(id: string): Promise<GlAllocationRule> {
  return get(`/gl/allocation-rules/${id}`)
}

export function createAllocationRule(data: CreateGlAllocationRuleRequest): Promise<GlAllocationRule> {
  return post('/gl/allocation-rules', data)
}

export function updateAllocationRule(id: string, data: UpdateGlAllocationRuleRequest): Promise<GlAllocationRule> {
  return put(`/gl/allocation-rules/${id}`, data)
}

export function addAllocationRuleLine(id: string, data: AddGlAllocationRuleLineRequest): Promise<GlAllocationRule> {
  return post(`/gl/allocation-rules/${id}/lines`, data)
}

export function executeAllocation(id: string, data: ExecuteGlAllocationRequest): Promise<GlJournalBatch> {
  return post(`/gl/allocation-rules/${id}/execute`, data)
}

export function activateAllocationRule(id: string): Promise<void> {
  return post(`/gl/allocation-rules/${id}/activate`)
}

export function deactivateAllocationRule(id: string): Promise<void> {
  return post(`/gl/allocation-rules/${id}/deactivate`)
}

export function getBudgets(params?: { companyId?: string | undefined; fiscalYearId?: string | undefined }): Promise<GlBudget[]> {
  return get('/gl/budgets', {
    companyId: params?.companyId ?? companyId(),
    fiscalYearId: params?.fiscalYearId,
  })
}

export function getBudget(id: string): Promise<GlBudget> {
  return get(`/gl/budgets/${id}`)
}

export function createBudget(data: CreateGlBudgetRequest): Promise<GlBudget> {
  return post('/gl/budgets', data)
}

export function addBudgetLine(id: string, data: AddGlBudgetLineRequest): Promise<GlBudget> {
  return post(`/gl/budgets/${id}/lines`, data)
}

export function deleteBudget(id: string): Promise<void> {
  return del(`/gl/budgets/${id}`)
}

export function getTrialBalance(params?: { companyId?: string | undefined; fiscalPeriodId?: string | undefined }): Promise<TrialBalanceReport> {
  return get('/gl/reports/trial-balance', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
  })
}

export function getGeneralLedgerDetail(params?: {
  companyId?: string | undefined
  fiscalPeriodId?: string | undefined
  fromDate?: string | undefined
  toDate?: string | undefined
}): Promise<GeneralLedgerDetailReport> {
  return get('/gl/reports/general-ledger-detail', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
    fromDate: params?.fromDate,
    toDate: params?.toDate,
  })
}

export function getUnpostedTransactions(companyIdParam?: string): Promise<UnpostedTransactionsReport> {
  return get('/gl/reports/unposted-transactions', { companyId: companyIdParam ?? companyId() })
}

export function getBalanceSheet(params?: { companyId?: string | undefined; fiscalPeriodId?: string | undefined }): Promise<FinancialStatementReport> {
  return get('/gl/reports/balance-sheet', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
  })
}

export function getIncomeStatement(params?: { companyId?: string | undefined; fiscalPeriodId?: string | undefined }): Promise<FinancialStatementReport> {
  return get('/gl/reports/income-statement', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
  })
}

export function getCashFlow(params?: { companyId?: string | undefined; fiscalPeriodId?: string | undefined }): Promise<CashFlowReport> {
  return get('/gl/reports/cash-flow', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
  })
}

export function getBudgetVsActual(params?: {
  companyId?: string | undefined
  budgetId?: string | undefined
  fiscalPeriodId?: string | undefined
}): Promise<BudgetVsActualReport> {
  return get('/gl/reports/budget-vs-actual', {
    companyId: params?.companyId ?? companyId(),
    budgetId: params?.budgetId,
    fiscalPeriodId: params?.fiscalPeriodId,
  })
}

export function getAccountDistribution(params?: {
  companyId?: string | undefined
  fiscalPeriodId?: string | undefined
  fromDate?: string | undefined
  toDate?: string | undefined
}): Promise<AccountDistributionReport> {
  return get('/gl/reports/account-distribution', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
    fromDate: params?.fromDate,
    toDate: params?.toDate,
  })
}

export function getConsolidatedTrialBalance(params?: { companyId?: string | undefined; fiscalPeriodId?: string | undefined }): Promise<ConsolidatedTrialBalanceReport> {
  return get('/gl/reports/consolidated-trial-balance', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
  })
}

export function getIntercompanyBalance(params?: { companyId?: string | undefined; fiscalPeriodId?: string | undefined }): Promise<IntercompanyBalanceReport> {
  return get('/gl/reports/intercompany-balance', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
  })
}

export function getMultiCurrencyRevaluation(params?: { companyId?: string | undefined; fiscalPeriodId?: string | undefined; revaluationDate?: string | undefined }): Promise<MultiCurrencyRevaluationReport> {
  return get('/gl/reports/multi-currency-revaluation', {
    companyId: params?.companyId ?? companyId(),
    fiscalPeriodId: params?.fiscalPeriodId,
    revaluationDate: params?.revaluationDate,
  })
}

// --- Multi-Currency Revaluation engine ---
export function previewRevaluation(data: RevaluationPreviewRequest): Promise<RevaluationPreview> {
  return post('/gl/revaluation/preview', data)
}
export function executeRevaluation(data: RevaluationExecuteRequest): Promise<RevaluationResult> {
  return post('/gl/revaluation/execute', data)
}

export function createConsolidationRun(data: CreateConsolidationRunRequest): Promise<ConsolidationRunDto> {
  return post('/gl/consolidation/runs', data)
}

export function getConsolidationRun(id: string): Promise<ConsolidationRunDto> {
  return get(`/gl/consolidation/runs/${id}`)
}

export function getConsolidationRuns(parentCompanyId: string): Promise<ConsolidationRunDto[]> {
  return get('/gl/consolidation/runs', { parentCompanyId })
}

export function executeConsolidationRun(id: string): Promise<ConsolidationRunDto> {
  return post(`/gl/consolidation/runs/${id}/execute`)
}

export function createIntercompanyMapping(data: CreateIntercompanyMappingRequest): Promise<IntercompanyMappingDto> {
  return post('/gl/consolidation/intercompany-mappings', data)
}

export function getIntercompanyMappings(params?: { fromCompanyId?: string | undefined; toCompanyId?: string | undefined; isActive?: boolean | undefined }): Promise<IntercompanyMappingDto[]> {
  return get('/gl/consolidation/intercompany-mappings', params)
}

export function updateIntercompanyMapping(mappingId: string, data: UpdateIntercompanyMappingRequest): Promise<IntercompanyMappingDto> {
  return put(`/gl/consolidation/intercompany-mappings/${mappingId}`, data)
}

export function deleteIntercompanyMapping(mappingId: string): Promise<void> {
  return del(`/gl/consolidation/intercompany-mappings/${mappingId}`)
}

// --- Phase 2 gap features ---
export function closeYearEnd(data: CloseYearEndRequest): Promise<YearEndCloseRunDto> {
  return post('/gl/period-close/year-end', data)
}

export function getSuspenseItems(companyId: string, status?: SuspenseStatus): Promise<PostingSuspenseItemDto[]> {
  return get('/gl/period-close/suspense', { companyId, status })
}

export function resolveSuspense(id: string, data: ResolveSuspenseRequest): Promise<string> {
  return post(`/gl/period-close/suspense/${id}/resolve`, data)
}

export function discardSuspense(id: string, data: DiscardSuspenseRequest): Promise<void> {
  return post(`/gl/period-close/suspense/${id}/discard`, data)
}

export function postIntercompanyDueToFrom(data: IntercompanyDueToFromRequest): Promise<void> {
  return post('/gl/period-close/intercompany-due-to-from', data)
}

export function getPrePostingList(companyId: string, fiscalPeriodId: string): Promise<PrePostingEditLine[]> {
  return get('/gl/period-close/pre-posting', { companyId, fiscalPeriodId })
}

export function getPeriodEndChecklist(companyId: string, fiscalPeriodId: string): Promise<PeriodEndChecklistItem[]> {
  return get('/gl/period-close/checklist', { companyId, fiscalPeriodId })
}

export function rollForwardBudget(id: string, data: RollForwardBudgetRequest): Promise<string> {
  return post(`/gl/budgets/${id}/roll-forward`, data)
}

export function transferBudget(id: string, data: TransferBudgetRequest): Promise<string> {
  return post(`/gl/budgets/${id}/transfer`, data)
}

export function getAccountInquiry(accountId: string, params?: { fiscalPeriodId?: string | undefined; fromDate?: string | undefined; toDate?: string | undefined }): Promise<AccountInquiryDto> {
  return get(`/gl/account-inquiry/${accountId}`, {
    fiscalPeriodId: params?.fiscalPeriodId,
    fromDate: params?.fromDate,
    toDate: params?.toDate,
  })
}
