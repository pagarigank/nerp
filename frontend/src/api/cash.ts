import type {
  AutoMatchLine,
  BankStatementFormat,
  CashBankStatementLine,
  CashBankAccount,
  CashBankAccountDetail,
  CashBankContact,
  CashBankFee,
  CashBankStatement,
  CashBankStatementDetail,
  StatementDownloadReport,
  CashBankTransfer,
  CashDeposit,
  CashDepositDetail,
  CashForecast,
  CashPosition,
  CreateBankTransferRequest,
  CreateCashBankAccountRequest,
  CreateCashDepositRequest,
  CreateDepositFromArRequest,
  CreateReconciliationSessionResponse,
  ImportStatementRequest,
  ImportStatementResponse,
  LockReconciliationRequest,
  MarkLineClearedRequest,
  MarkLineMatchedRequest,
  MarkLineUnmatchedRequest,
  NsfRecord,
  OutstandingCheckAging,
  ProcessNsfRequest,
  ReconciliationDetail,
  ReconciliationSession,
  ReconciliationSummary,
  RecordBankFeeRequest,
  UpdateCashBankAccountRequest,
  BankGlMapping,
  CreateBankGlMappingRequest,
  UpdateBankGlMappingRequest,
  LockboxBatch,
  CreateLockboxBatchRequest,
  StaleCheckEscheatment,
  CreateStaleCheckEscheatmentRequest,
  PositivePayException,
  CreatePositivePayExceptionRequest,
  DecidePositivePayRequest,
  BankDuplicateLine,
  BankFeeAnalysis,
  CashForecastHorizon,
  OutstandingDeposits,
} from '@/types/cash'
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

function companyId(): string {
  const current = useAuthStore.getState().currentCompany
  // Empty id is the "All Companies" sentinel for super admins: omit the
  // companyId query param so the backend returns data across every company.
  return current?.id ?? ''
}

// ---- Bank Accounts ----
export function getBankAccounts(companyIdParam?: string): Promise<CashBankAccount[]> {
  return get('/cash/bank-accounts', { companyId: companyIdParam ?? companyId() })
}

export function getBankAccount(id: string): Promise<CashBankAccountDetail> {
  return get(`/cash/bank-accounts/${id}`)
}

export function createBankAccount(data: CreateCashBankAccountRequest): Promise<CashBankAccount> {
  return post('/cash/bank-accounts', data)
}

export function updateBankAccount(id: string, data: UpdateCashBankAccountRequest): Promise<CashBankAccount> {
  return put(`/cash/bank-accounts/${id}`, data)
}

export function deleteBankAccount(id: string): Promise<void> {
  return del(`/cash/bank-accounts/${id}`)
}

export function activateBankAccount(id: string): Promise<CashBankAccount> {
  return post(`/cash/bank-accounts/${id}/activate`)
}

export function deactivateBankAccount(id: string): Promise<CashBankAccount> {
  return post(`/cash/bank-accounts/${id}/deactivate`)
}

export function closeBankAccount(id: string): Promise<CashBankAccount> {
  return post(`/cash/bank-accounts/${id}/close`)
}

// ---- Deposits ----
export function getDeposits(params?: { companyId?: string; bankAccountId?: string }): Promise<CashDeposit[]> {
  return get('/cash/deposits', {
    companyId: params?.companyId ?? companyId(),
    bankAccountId: params?.bankAccountId,
  })
}

export function getDeposit(id: string): Promise<CashDepositDetail> {
  return get(`/cash/deposits/${id}`)
}

export function createDeposit(data: CreateCashDepositRequest): Promise<CashDepositDetail> {
  return post('/cash/deposits', data)
}

export function createDepositFromAr(data: CreateDepositFromArRequest): Promise<CashDepositDetail> {
  return post('/cash/deposits/from-ar', data)
}

export function confirmDeposit(id: string): Promise<CashDeposit> {
  return post(`/cash/deposits/${id}/confirm`)
}

export function clearDeposit(id: string): Promise<CashDeposit> {
  return post(`/cash/deposits/${id}/clear`)
}

export function deleteDeposit(id: string): Promise<void> {
  return del(`/cash/deposits/${id}`)
}

// ---- Bank Statements ----
export function getBankStatements(params?: { companyId?: string; bankAccountId?: string }): Promise<CashBankStatement[]> {
  return get('/cash/bank-statements', {
    companyId: params?.companyId ?? companyId(),
    bankAccountId: params?.bankAccountId,
  })
}

export function getBankStatement(id: string): Promise<CashBankStatementDetail> {
  return get(`/cash/bank-statements/${id}`)
}

export function importBankStatement(data: ImportStatementRequest): Promise<ImportStatementResponse> {
  return post('/cash/bank-statements/import', data)
}

export function runBankStatementDownload(): Promise<StatementDownloadReport> {
  return post('/cash/bank-statements/run-download')
}

export function validateBankStatement(id: string): Promise<CashBankStatement> {
  return post(`/cash/bank-statements/${id}/validate`)
}

export function deleteBankStatement(id: string): Promise<void> {
  return del(`/cash/bank-statements/${id}`)
}

// ---- Reconciliations ----
export function getReconciliations(companyIdParam?: string): Promise<ReconciliationSession[]> {
  return get('/cash/reconciliations', { companyId: companyIdParam ?? companyId() })
}

export function getReconciliation(id: string): Promise<ReconciliationSession> {
  return get(`/cash/reconciliations/${id}`)
}

export function getReconciliationLines(id: string): Promise<CashBankStatementLine[]> {
  return get(`/cash/reconciliations/${id}/lines`)
}

export function createReconciliationSession(
  statementId: string,
  data: { sessionNumber: string; createdBy: string }
): Promise<CreateReconciliationSessionResponse> {
  return post(`/cash/reconciliations/statement/${statementId}`, data)
}

export function runAutoMatch(id: string): Promise<AutoMatchLine[]> {
  return post(`/cash/reconciliations/${id}/auto-match`)
}

export function markLineMatched(id: string, data: MarkLineMatchedRequest): Promise<CashBankStatementLine> {
  return post(`/cash/reconciliations/${id}/lines/match`, data)
}

export function markLineCleared(id: string, data: MarkLineClearedRequest): Promise<CashBankStatementLine> {
  return post(`/cash/reconciliations/${id}/lines/clear`, data)
}

export function markLineUnmatched(id: string, data: MarkLineUnmatchedRequest): Promise<CashBankStatementLine> {
  return post(`/cash/reconciliations/${id}/lines/unmatch`, data)
}

export function lockReconciliation(id: string, data: LockReconciliationRequest): Promise<ReconciliationSession> {
  return post(`/cash/reconciliations/${id}/lock`, data)
}

// ---- Bank Transfers ----
export function getBankTransfers(companyIdParam?: string): Promise<CashBankTransfer[]> {
  return get('/cash/transfers', { companyId: companyIdParam ?? companyId() })
}

export function createBankTransfer(data: CreateBankTransferRequest): Promise<CashBankTransfer> {
  return post('/cash/transfers', data)
}

export function confirmBankTransfer(id: string): Promise<CashBankTransfer> {
  return post(`/cash/transfers/${id}/confirm`)
}

export function completeBankTransfer(id: string): Promise<CashBankTransfer> {
  return post(`/cash/transfers/${id}/complete`)
}

export function voidBankTransfer(id: string, reason?: string): Promise<CashBankTransfer> {
  const query = reason ? `?reason=${encodeURIComponent(reason)}` : ''
  return post(`/cash/transfers/${id}/void${query}`)
}

// ---- Bank Fees ----
export function getBankFees(companyIdParam?: string): Promise<CashBankFee[]> {
  return get('/cash/bank-fees', { companyId: companyIdParam ?? companyId() })
}

export function recordBankFee(data: RecordBankFeeRequest): Promise<CashBankFee> {
  return post('/cash/bank-fees', data)
}

// ---- NSF ----
export function getNsfRecords(companyIdParam?: string): Promise<NsfRecord[]> {
  return get('/cash/nsf', { companyId: companyIdParam ?? companyId() })
}

export function processNsf(data: ProcessNsfRequest): Promise<NsfRecord> {
  return post('/cash/nsf', data)
}

// ---- Reports ----
export function getCashPosition(companyIdParam?: string): Promise<CashPosition[]> {
  return get('/cash/reports/cash-position', { companyId: companyIdParam ?? companyId() })
}

export function getOutstandingChecks(companyIdParam: string, bankAccountId: string): Promise<OutstandingCheckAging> {
  return get('/cash/reports/outstanding-checks', { companyId: companyIdParam, bankAccountId })
}

export function getPositivePay(companyIdParam: string, bankAccountId: string): Promise<string> {
  return get('/cash/reports/positive-pay', { companyId: companyIdParam, bankAccountId })
}

export function getReconciliationSummary(companyIdParam?: string, bankAccountId?: string): Promise<ReconciliationSummary[]> {
  return get('/cash/reports/reconciliation-summary', {
    companyId: companyIdParam ?? companyId(),
    bankAccountId,
  })
}

export function getReconciliationDetail(sessionId: string): Promise<ReconciliationDetail> {
  return get('/cash/reports/reconciliation-detail', { sessionId })
}

export function getBankActivity(
  companyIdParam: string,
  bankAccountId: string,
  from?: string,
  to?: string
): Promise<ReconciliationDetail['lines']> {
  return get('/cash/reports/bank-activity', { companyId: companyIdParam, bankAccountId, from, to })
}

export function getCashForecast(companyIdParam?: string): Promise<CashForecast> {
  return get('/cash/reports/cash-forecast', { companyId: companyIdParam ?? companyId() })
}

export const bankStatementFormats: Record<BankStatementFormat, string> = {
  Csv: 'CSV',
  Ofx: 'OFX',
  Bai2: 'BAI2',
  Qbo: 'QBO',
}

// --- Phase 5 gap feature API functions ---

export function getBankGlMappings(companyIdParam?: string): Promise<BankGlMapping[]> {
  return get('/cash/bank-gl-mappings', { companyId: companyIdParam ?? companyId() })
}

export function getBankGlMapping(bankAccountId: string): Promise<BankGlMapping> {
  return get(`/cash/bank-accounts/${bankAccountId}/gl-mapping`)
}

export function createBankGlMapping(req: CreateBankGlMappingRequest): Promise<BankGlMapping> {
  return post('/cash/bank-gl-mappings', req)
}

export function updateBankGlMapping(id: string, req: UpdateBankGlMappingRequest): Promise<BankGlMapping> {
  return put(`/cash/bank-gl-mappings/${id}`, req)
}

export function getLockboxBatches(companyIdParam?: string): Promise<LockboxBatch[]> {
  return get('/cash/lockbox-batches', { companyId: companyIdParam ?? companyId() })
}

export function createLockboxBatch(req: CreateLockboxBatchRequest): Promise<LockboxBatch> {
  return post('/cash/lockbox-batches', req)
}

export function postLockboxBatch(id: string): Promise<LockboxBatch> {
  return post(`/cash/lockbox-batches/${id}/post`, {})
}

export function getStaleCheckEscheatments(companyIdParam?: string): Promise<StaleCheckEscheatment[]> {
  return get('/cash/stale-check-escheatments', { companyId: companyIdParam ?? companyId() })
}

export function createStaleCheckEscheatment(req: CreateStaleCheckEscheatmentRequest): Promise<StaleCheckEscheatment> {
  return post('/cash/stale-check-escheatments', req)
}

export function escheatStaleCheck(id: string): Promise<StaleCheckEscheatment> {
  return post(`/cash/stale-check-escheatments/${id}/escheat`, {})
}

export function reissueStaleCheck(id: string): Promise<StaleCheckEscheatment> {
  return post(`/cash/stale-check-escheatments/${id}/reissue`, {})
}

export function getPositivePayExceptions(companyIdParam?: string): Promise<PositivePayException[]> {
  return get('/cash/positive-pay-exceptions', { companyId: companyIdParam ?? companyId() })
}

export function createPositivePayException(req: CreatePositivePayExceptionRequest): Promise<PositivePayException> {
  return post('/cash/positive-pay-exceptions', req)
}

export function decidePositivePay(id: string, req: DecidePositivePayRequest): Promise<PositivePayException> {
  return post(`/cash/positive-pay-exceptions/${id}/decide`, req)
}

export function getDuplicateLines(companyIdParam?: string): Promise<BankDuplicateLine[]> {
  return get('/cash/duplicate-lines', { companyId: companyIdParam ?? companyId() })
}

export function detectDuplicateLines(companyIdParam: string, bankAccountId: string, statementId: string): Promise<BankDuplicateLine[]> {
  return post(`/cash/duplicate-lines/detect?companyId=${companyIdParam}&bankAccountId=${bankAccountId}&statementId=${statementId}`, {})
}

export function resolveDuplicateLine(id: string): Promise<BankDuplicateLine> {
  return post(`/cash/duplicate-lines/${id}/resolve`, {})
}

export function getBankFeeAnalysis(year: number, month: number, companyIdParam?: string): Promise<BankFeeAnalysis> {
  return get('/cash/fee-analysis', { companyId: companyIdParam ?? companyId(), year, month })
}

export function getCashForecastHorizon(companyIdParam?: string): Promise<CashForecastHorizon> {
  return get('/cash/reports/cash-forecast-horizon', { companyId: companyIdParam ?? companyId() })
}

export function getOutstandingDeposits(companyIdParam?: string): Promise<OutstandingDeposits> {
  return get('/cash/reports/outstanding-deposits', { companyId: companyIdParam ?? companyId() })
}

export function deleteBankContact(bankAccountId: string, contactId: string): Promise<void> {
  return del(`/cash/bank-accounts/${bankAccountId}/contacts/${contactId}`)
}

export function getBankAccountDetail(bankAccountId: string): Promise<CashBankAccountDetail> {
  return get(`/cash/bank-accounts/${bankAccountId}`)
}

export function addBankContact(bankAccountId: string, name: string, phone?: string | null, email?: string | null, title?: string | null): Promise<CashBankContact> {
  return post(`/cash/bank-accounts/${bankAccountId}/contacts`, { name, phone, email, title })
}

export function updateBankContact(bankAccountId: string, contactId: string, name: string, phone?: string | null, email?: string | null, title?: string | null): Promise<CashBankContact> {
  return put(`/cash/bank-accounts/${bankAccountId}/contacts/${contactId}`, { name, phone, email, title })
}

export function exportPositivePay(bankAccountId: string, companyIdParam?: string): Promise<string> {
  return get('/cash/reports/positive-pay', { companyId: companyIdParam ?? companyId(), bankAccountId })
}
