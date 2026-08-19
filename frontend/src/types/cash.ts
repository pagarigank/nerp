export type BankAccountType =
  | 'Checking'
  | 'Savings'
  | 'MoneyMarket'
  | 'PettyCash'
  | 'Investment'

export type BankAccountStatus = 'Active' | 'Inactive' | 'Closed'

export interface CashBankAccount {
  id: string
  companyId: string
  accountCode: string
  accountName: string
  accountNumber: string
  routingNumber?: string | null
  bankName?: string | null
  currencyCode: string
  accountType: BankAccountType
  openingBalance: number
  currentBalance: number
  glAccountId?: string | null
  status: BankAccountStatus
}

export interface CashBankContact {
  id: string
  name: string
  phone?: string | null
  email?: string | null
  title?: string | null
}

export interface CashBankAccountDetail {
  account: CashBankAccount
  contacts: CashBankContact[]
}

export interface CreateCashBankAccountRequest {
  companyId: string
  accountCode: string
  accountName: string
  accountNumber: string
  routingNumber?: string | null
  bankName?: string | null
  currencyCode?: string | null
  accountType: number
  openingBalance: number
  glAccountId?: string | null
}

export interface UpdateCashBankAccountRequest {
  accountName: string
  accountNumber: string
  routingNumber?: string | null
  bankName?: string | null
  currencyCode?: string | null
  accountType: number
  glAccountId?: string | null
}

export type DepositStatus = 'Draft' | 'Confirmed' | 'Cleared' | 'Voided'
export type DepositLineSource = 'Manual' | 'ArCashReceipt'

export interface DepositLineItem {
  source: number
  sourceReferenceId?: string | null
  amount: number
  description?: string | null
}

export interface CashDeposit {
  id: string
  companyId: string
  bankAccountId: string
  depositNumber: string
  depositDate: string
  reference?: string | null
  status: DepositStatus
  totalAmount: number
}

export interface CashDepositLine {
  id: string
  source: DepositLineSource
  sourceReferenceId?: string | null
  amount: number
  description?: string | null
}

export interface CashDepositDetail {
  deposit: CashDeposit
  lines: CashDepositLine[]
}

export interface CreateCashDepositRequest {
  companyId: string
  bankAccountId: string
  depositNumber: string
  depositDate: string
  reference?: string | null
  lines: DepositLineItem[]
}

export interface CreateDepositFromArRequest {
  companyId: string
  bankAccountId: string
  depositNumber: string
  depositDate: string
  cashReceiptId: string
}

export type BankStatementFormat = 'Csv' | 'Ofx' | 'Bai2' | 'Qbo'
export type BankStatementStatus = 'Imported' | 'Validated' | 'Reconciled' | 'Locked'
export type BankStatementLineStatus = 'Unreconciled' | 'Matched' | 'Cleared' | 'Locked'
export type BankMatchSource = 'ApPayment' | 'ArCashReceipt' | 'Deposit' | 'BankTransfer' | 'BankAdjustment'

export interface CashBankStatement {
  id: string
  companyId: string
  bankAccountId: string
  statementNumber: string
  statementDate: string
  beginningBalance: number
  endingBalance: number
  format: BankStatementFormat
  status: BankStatementStatus
  lineCount: number
}

export interface CashBankStatementLine {
  id: string
  transactionDate: string
  amount: number
  description: string
  referenceNumber?: string | null
  checkNumber?: string | null
  balance: number
  status: BankStatementLineStatus
  matchedTransactionId?: string | null
  matchedSource?: BankMatchSource | null
}

export interface CashBankStatementDetail {
  statement: CashBankStatement
  lines: CashBankStatementLine[]
}

export interface ImportStatementRequest {
  companyId: string
  bankAccountId: string
  statementNumber: string
  statementDate: string
  fileContent: string
  format?: number | null
}

export interface ImportStatementResponse {
  bankStatementId: string
  statementNumber: string
  format: BankStatementFormat
  lineCount: number
  beginningBalance?: number | null
  endingBalance?: number | null
  warnings: string[]
}

export type ReconciliationStatus = 'InProgress' | 'Locked'

export interface ReconciliationSession {
  id: string
  companyId: string
  bankAccountId: string
  bankStatementId: string
  sessionNumber: string
  statementDate: string
  beginningBalance: number
  endingBalance: number
  variance?: number | null
  glJournalBatchId?: string | null
  status: ReconciliationStatus
}

export interface CreateReconciliationSessionResponse {
  sessionId: string
  sessionNumber: string
  beginningBalance: number
  endingBalance: number
}

export type MatchConfidence = 'Exact' | 'Probable' | 'ManualReview' | 'None'

export interface AutoMatchCandidate {
  id: string
  source: BankMatchSource
  reference: string
  amount: number
  date: string
  checkNumber?: string | null
  description?: string | null
}

export interface AutoMatchLine {
  statementLineId: string
  statementAmount: number
  candidate?: AutoMatchCandidate | null
  score: number
  confidence: MatchConfidence
}

export interface MarkLineMatchedRequest {
  statementLineId: string
  transactionId: string
  source: number
  clearedBy?: string | null
}

export interface MarkLineClearedRequest {
  statementLineId: string
  clearedBy?: string | null
}

export interface MarkLineUnmatchedRequest {
  statementLineId: string
  clearedBy?: string | null
}

export interface LockReconciliationRequest {
  varianceGlAccountId: string
  tolerance: number
  lockedBy?: string | null
}

export type BankTransferStatus = 'Draft' | 'InTransit' | 'Completed' | 'Voided'

export interface CashBankTransfer {
  id: string
  companyId: string
  fromBankAccountId: string
  toBankAccountId: string
  transferNumber: string
  amount: number
  transferDate: string
  reference?: string | null
  status: BankTransferStatus
}

export interface CreateBankTransferRequest {
  companyId: string
  fromBankAccountId: string
  toBankAccountId: string
  transferNumber: string
  amount: number
  transferDate: string
  reference?: string | null
}

export type BankFeeType =
  | 'ServiceCharge'
  | 'WireFee'
  | 'ACHFee'
  | 'OverdraftFee'
  | 'NsfFee'
  | 'CreditCardProcessing'
  | 'Other'
export type BankFeeStatus = 'Draft' | 'Posted' | 'Voided'

export interface CashBankFee {
  id: string
  companyId: string
  bankAccountId: string
  feeNumber: string
  feeType: BankFeeType
  amount: number
  feeDate: string
  description?: string | null
  glJournalBatchId?: string | null
  status: BankFeeStatus
}

export interface RecordBankFeeRequest {
  companyId: string
  bankAccountId: string
  feeNumber: string
  feeType: number
  amount: number
  feeDate: string
  description?: string | null
  expenseGlAccountId: string
  postedBy?: string | null
}

export type NsfStatus = 'Processed' | 'Voided'

export interface NsfRecord {
  id: string
  companyId: string
  bankAccountId: string
  cashReceiptId?: string | null
  customerId?: string | null
  nsfNumber: string
  amount: number
  returnedDate: string
  bankReference?: string | null
  reason?: string | null
  nsfFeeAmount?: number | null
  status: NsfStatus
}

export interface ProcessNsfRequest {
  companyId: string
  bankAccountId: string
  cashReceiptId: string
  nsfNumber: string
  amount: number
  returnedDate: string
  bankReference?: string | null
  reason?: string | null
  nsfFeeAmount?: number | null
  processedBy?: string | null
}

export interface CashPosition {
  bankAccountId: string
  accountCode: string
  accountName: string
  accountNumber: string
  currentBalance: number
  currencyCode: string
  outstandingChecks: number
  outstandingDeposits: number
}

export interface OutstandingCheckBucket {
  bucket: string
  amount: number
  checkCount: number
}

export interface OutstandingCheckAging {
  bankAccountId: string
  accountName: string
  asOfDate: string
  buckets: OutstandingCheckBucket[]
}

export interface CashForecast {
  currentCash: number
  openPayables: number
  openReceivables: number
  projectedCashAfterPayables: number
  projectedCashAfterCollections: number
}

export interface ReconciliationSummary {
  bankAccountId: string
  accountName: string
  statementDate: string
  beginningBalance: number
  endingBalance: number
  clearedDeposits: number
  clearedWithdrawals: number
  outstandingChecks: number
  outstandingDeposits: number
  variance: number
  status: ReconciliationStatus
}

export interface ReconciliationDetailLine {
  transactionDate: string
  amount: number
  description: string
  checkNumber?: string | null
  status: BankStatementLineStatus
  matchedTransactionId?: string | null
  matchedSource?: BankMatchSource | null
}

export interface ReconciliationDetail {
  bankAccountId: string
  accountName: string
  statementNumber: string
  statementDate: string
  beginningBalance: number
  endingBalance: number
  variance: number
  status: ReconciliationStatus
  lines: ReconciliationDetailLine[]
}

// --- Phase 5 gap features ---

export interface BankGlMapping {
  id: string
  companyId: string
  bankAccountId: string
  bankAccountName?: string | null
  glAccountId: string
  isDefault: boolean
}

export interface CreateBankGlMappingRequest {
  companyId: string
  bankAccountId: string
  glAccountId: string
  isDefault: boolean
}

export interface UpdateBankGlMappingRequest {
  glAccountId: string
  isDefault: boolean
}

export interface LockboxItem {
  id: string
  referenceNumber: string
  customerId?: string | null
  customerName: string
  amount: number
  remittanceDate?: string | null
  invoiceNumber?: string | null
  receiptCreated: boolean
}

export interface LockboxBatch {
  id: string
  companyId: string
  batchNumber: string
  fileName: string
  format: string
  importedOn: string
  status: string
  totalItems: number
  totalAmount: number
  items: LockboxItem[]
}

export interface CreateLockboxItemRequest {
  referenceNumber: string
  customerId?: string | null
  customerName: string
  amount: number
  remittanceDate?: string | null
  invoiceNumber?: string | null
}

export interface CreateLockboxBatchRequest {
  companyId: string
  batchNumber: string
  fileName: string
  format: string
  items: CreateLockboxItemRequest[]
}

export interface StaleCheckEscheatment {
  id: string
  companyId: string
  bankAccountId: string
  checkId?: string | null
  checkNumber: string
  amount: number
  issueDate: string
  payee: string
  state: string
  status: string
  escheatedOn?: string | null
  reissuedOn?: string | null
}

export interface CreateStaleCheckEscheatmentRequest {
  companyId: string
  bankAccountId: string
  checkId?: string | null
  checkNumber: string
  amount: number
  issueDate: string
  payee: string
  state: string
}

export interface PositivePayException {
  id: string
  companyId: string
  bankAccountId: string
  checkNumber: string
  amount: number
  issueDate: string
  decision: string
  decisionReason: string
  receivedOn: string
  decidedOn?: string | null
}

export interface CreatePositivePayExceptionRequest {
  companyId: string
  bankAccountId: string
  checkNumber: string
  amount: number
  issueDate: string
  decisionReason: string
}

export interface DecidePositivePayRequest {
  decision: string
  decisionReason: string
}

export interface BankDuplicateLine {
  id: string
  companyId: string
  bankAccountId: string
  checkNumber: string
  amount: number
  transactionDate: string
  statementLineId: string
  statementId: string
  detectedOn: string
  resolved: boolean
}

export interface BankFeeAnalysisLine {
  id: string
  feeType: string
  bankAccountId?: string | null
  amount: number
  count: number
}

export interface BankFeeAnalysis {
  id: string
  companyId: string
  year: number
  month: number
  generatedOn: string
  totalFees: number
  lines: BankFeeAnalysisLine[]
}

export interface CashForecastHorizon {
  todayCash: number
  next7DayCash: number
  next30DayCash: number
  openPayablesNext7: number
  openReceivablesNext7: number
  openPayablesNext30: number
  openReceivablesNext30: number
}

export interface OutstandingDeposit {
  bankAccountId: string
  accountName: string
  outstandingDepositAmount: number
  depositCount: number
}

export interface OutstandingDeposits {
  accounts: OutstandingDeposit[]
  totalOutstandingDeposits: number
}
