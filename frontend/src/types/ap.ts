export interface VendorBankAccount {
  id: string
  bankName: string
  accountNumber: string
  routingNumber?: string | null
  isDefault: boolean
}

export interface Vendor {
  id: string
  vendorId: string
  name: string
  legalName?: string | null
  taxId?: string | null
  form1099Category?: number | string | null
  defaultPaymentTermId?: string | null
  isActive: boolean
  backupWithholdingFlag: boolean
  backupWithholdingRate: number
  bankAccounts: VendorBankAccount[]
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateVendorBankAccountRequest {
  bankName: string
  accountNumber: string
  routingNumber?: string | null
  isDefault: boolean
}

export interface CreateVendorRequest {
  vendorId: string
  name: string
  legalName?: string | null
  taxId?: string | null
  form1099Category?: number | string | null
  defaultPaymentTermId?: string | null
  isActive: boolean
  backupWithholdingFlag: boolean
  backupWithholdingRate: number
  bankAccounts: CreateVendorBankAccountRequest[]
}

export interface UpdateVendorRequest {
  name: string
  legalName?: string | null
  taxId?: string | null
  form1099Category?: number | string | null
  defaultPaymentTermId?: string | null
  backupWithholdingFlag: boolean
  backupWithholdingRate: number
}

export interface PaymentTerm {
  id: string
  name: string
  dueDays: number
  discountDays: number
  discountPercent: number
  isActive: boolean
  createdOn: string
  modifiedOn?: string | null
}

export interface CreatePaymentTermRequest {
  name: string
  dueDays: number
  discountDays: number
  discountPercent: number
}

export type UpdatePaymentTermRequest = CreatePaymentTermRequest

export interface VoucherDistribution {
  id: string
  accountId: string
  debit: number
  credit: number
  projectId?: string | null
  taskId?: string | null
}

export interface Voucher {
  id: string
  voucherBatchId: string
  vendorId: string
  voucherType: number | string
  invoiceNumber: string
  invoiceDate: string
  dueDate: string
  totalAmount: number
  discountAmount: number
  description: string
  paymentTermId?: string | null
  purchaseOrderId?: string | null
  receiptLineId?: string | null
  form1099Amount: number
  backupWithholdingAmount: number
  is1099Reportable: boolean
  selectedForPayment: boolean
  distributions: VoucherDistribution[]
}

export interface VoucherBatch {
  id: string
  companyId: string
  batchNumber: string
  description: string
  postingDate: string
  fiscalPeriodId: string
  status: number | string
  vouchers: Voucher[]
  createdOn: string
  modifiedOn?: string | null
}

export interface CreateVoucherBatchRequest {
  companyId: string
  batchNumber: string
  description: string
  postingDate: string
  fiscalPeriodId: string
}

export interface CreateVoucherDistributionRequest {
  accountId: string
  debit?: number | null
  credit?: number | null
  projectId?: string | null
  taskId?: string | null
}

export interface AddVoucherToBatchRequest {
  vendorId: string
  voucherType: number | string
  invoiceNumber: string
  invoiceDate: string
  dueDate: string
  totalAmount: number
  discountAmount: number
  description?: string | null
  paymentTermId?: string | null
  purchaseOrderId?: string | null
  receiptLineId?: string | null
  form1099Amount: number
  backupWithholdingAmount: number
  distributions: CreateVoucherDistributionRequest[]
}

export interface ReverseApBatchRequest {
  reason: string
}

export interface PaymentLine {
  id: string
  paymentId: string
  voucherId: string
  appliedAmount: number
}

export interface Payment {
  id: string
  companyId: string
  vendorId: string
  paymentReference: string
  paymentDate: string
  paymentMethod: number | string
  currencyCode: string
  bankAccountId?: string | null
  status: number | string
  totalAmount: number
  lines: PaymentLine[]
  createdOn: string
  modifiedOn?: string | null
}

export interface CreatePaymentRequest {
  companyId: string
  vendorId: string
  paymentReference: string
  paymentDate: string
  paymentMethod: number | string
  currencyCode: string
  bankAccountId?: string | null
}

export interface SelectVouchersForPaymentRequest {
  voucherIds: string[]
}

export interface VoidPaymentRequest {
  reason: string
}

export interface ApVendorAgingLine {
  vendorId: string
  vendorCode: string
  vendorName: string
  currentBalance: number
  days1To30: number
  days31To60: number
  days61To90: number
  over90Days: number
  totalDue: number
}

export interface ApAgingReport {
  companyId: string
  asOfDate: string
  lines: ApVendorAgingLine[]
  totalCurrent: number
  totalDue: number
  generatedOn: string
}

export interface VendorTrialBalanceLine {
  vendorId: string
  vendorCode: string
  vendorName: string
  beginningBalance: number
  debits: number
  credits: number
  endingBalance: number
}

export interface VendorTrialBalanceReport {
  companyId: string
  asOfDate: string
  lines: VendorTrialBalanceLine[]
  totalBeginningBalance: number
  totalEndingBalance: number
  generatedOn: string
}

export interface ApBatchRegisterLine {
  batchId: string
  batchNumber: string
  description: string
  postingDate: string
  status: string
  voucherCount: number
  totalAmount: number
  totalDiscount: number
}

export interface ApBatchRegisterReport {
  companyId: string
  asOfDate: string
  lines: ApBatchRegisterLine[]
  totalBatches: number
  grandTotal: number
  generatedOn: string
}

export interface CashRequirementLine {
  vendorId: string
  vendorCode: string
  vendorName: string
  voucherId: string
  invoiceNumber: string
  dueDate: string
  originalAmount: number
  discountAmount: number
  netDue: number
  pastDue: boolean
}

export interface CashRequirementsReport {
  companyId: string
  asOfDate: string
  daysAhead: number
  lines: CashRequirementLine[]
  totalDue: number
  totalPastDue: number
  grandTotal: number
  generatedOn: string
}

export interface Form1099ReportLine {
  vendorId: string
  vendorCode: string
  vendorName: string
  taxId?: string | null
  category: string
  totalPayments: number
  backupWithholding: number
}

export interface Form1099Report {
  companyId: string
  taxYear: number
  lines: Form1099ReportLine[]
  totalPayments: number
  totalBackupWithholding: number
  generatedOn: string
}

export interface CheckRegisterLine {
  paymentId: string
  paymentReference: string
  vendorId: string
  vendorName: string
  paymentDate: string
  paymentMethod: string
  amount: number
  status: string
}

export interface CheckRegisterReport {
  companyId: string
  fromDate: string
  toDate: string
  lines: CheckRegisterLine[]
  totalAmount: number
  totalChecks: number
  generatedOn: string
}

export interface ApAccountDistributionLine {
  accountId: string
  accountNumber: string
  accountDescription: string
  debit: number
  credit: number
  transactionCount: number
}

export interface ApAccountDistributionReport {
  companyId: string
  fromDate: string
  toDate: string
  lines: ApAccountDistributionLine[]
  totalDebit: number
  totalCredit: number
  generatedOn: string
}

// --- Three-Way Match ---
export interface ThreeWayMatchLine {
  purchaseOrderLineId?: string | null
  itemCode: string
  description: string
  orderedQuantity: number
  receivedQuantity: number
  invoicedQuantity: number
  unitPrice: number
  extendedAmount: number
}
export interface ThreeWayMatchRequest {
  companyId: string
  vendorId: string
  invoiceNumber: string
  lines: ThreeWayMatchLine[]
  invoiceTotal: number
}
export interface ThreeWayMatchResult {
  isValid: boolean
  hasQuantityVariance: boolean
  hasPriceVariance: boolean
  totalVarianceAmount: number
  tolerancePercent: number
  warnings: string[]
  errors: string[]
}

// --- Backup Withholding ---
export interface BackupWithholdingCalculateRequest {
  vendorId: string
  paymentAmount: number
}
export interface BackupWithholdingResult {
  vendorId: string
  isSubjectToWithholding: boolean
  withholdingRate: number
  withholdingAmount: number
  netPaymentAmount: number
}

// --- Form 1099 ---
export interface Form1099VendorSummary {
  vendorId: string
  vendorIdCode: string
  name: string
  legalName?: string | null
  taxId?: string | null
  category: string
  totalPayments: number
  backupWithholdingAmount: number
}
export interface Form1099SummaryResult {
  companyId: string
  taxYear: number
  vendors: Form1099VendorSummary[]
  totalPayments: number
  totalBackupWithholding: number
}

// --- Phase 3 gap features ---
export interface DuplicateInvoiceCheckDto {
  id: string
  companyId: string
  vendorId: string
  invoiceNumber: string
  amount: number
  conflictingVoucherId?: string | null
  isDuplicate: boolean
  checkedOn: string
}

export interface VendorW9Dto {
  id: string
  vendorId: string
  taxId: string
  legalName: string
  tinVerified: boolean
  tinMatchStatus?: string | null
  capturedOn: string
}
export interface CaptureW9Request {
  taxId: string
  legalName: string
  tinVerified: boolean
  tinMatchStatus?: string | null
}

export type VerificationStatus = 'Pending' | 'Approved' | 'Rejected'
export interface VendorBankVerificationDto {
  id: string
  vendorBankAccountId: string
  routingNumber: string
  accountNumber: string
  status: VerificationStatus
  notes?: string | null
  createdOn: string
}
export interface VerifyBankRequest {
  vendorBankAccountId: string
  routingNumber: string
  accountNumber: string
}
export interface BankDecisionRequest {
  notes?: string | null
}

export interface CashDiscountCaptureDto {
  id: string
  voucherId: string
  vendorId: string
  invoiceAmount: number
  discountAvailable: number
  discountTaken: number
  discountLostAmount: number
  discountLost: boolean
  capturedOn: string
}
export interface LostDiscountSummaryDto {
  companyId: string
  totalAvailable: number
  totalTaken: number
  totalLost: number
  lostCount: number
}

export type EscheatmentStatus = 'Flagged' | 'Reported' | 'Reissued'
export interface StaleCheckEscheatmentDto {
  id: string
  paymentId: string
  vendorId: string
  amount: number
  issuedDate: string
  statutoryDays: number
  status: EscheatmentStatus
  reportedOn?: string | null
}

export interface GrirAccrualDto {
  id: string
  companyId: string
  vendorId: string
  purchaseOrderId?: string | null
  receiptId?: string | null
  accrualAmount: number
  fiscalPeriodId: string
  reversedByAccrualId?: string | null
  createdOn: string
}

export interface VendorStatementLineDto {
  id: string
  reference: string
  statementAmount: number
  bookAmount: number
  difference: number
  isDisputed: boolean
  note?: string | null
}
export interface VendorStatementDto {
  id: string
  companyId: string
  vendorId: string
  statementNumber: string
  statementDate: string
  statementTotal: number
  bookTotal: number
  disputedTotal: number
  status: 'Open' | 'Closed'
  lines: VendorStatementLineDto[]
}
export interface CreateVendorStatementLineRequest {
  reference: string
  statementAmount: number
  bookAmount: number
  isDisputed: boolean
  note?: string | null
}
export interface CreateVendorStatementRequest {
  companyId: string
  vendorId: string
  statementNumber: string
  statementDate: string
  statementTotal: number
  lines: CreateVendorStatementLineRequest[]
}

export type Form1099Type = 'NEC' | 'MISC'
export interface Ap1099ClassificationDto {
  id: string
  vendorId: string
  formType: Form1099Type
  taxYear: number
}
export interface Classify1099Request {
  vendorId: string
  formType: number
  taxYear: number
}

export interface FourWayMatchLineDto {
  itemCode: string
  orderedQuantity: number
  receivedQuantity: number
  invoicedQuantity: number
  inspectedQuantity?: number | null
  unitPrice: number
  extendedAmount: number
}
export interface FourWayMatchRequest {
  companyId: string
  vendorId: string
  invoiceNumber: string
  lines: FourWayMatchLineDto[]
  invoiceTotal: number
  tolerancePercent?: number
}
export interface FourWayMatchResult {
  isValid: boolean
  errors: string[]
  warnings: string[]
  tolerancePercent: number
}
