export interface ArCustomer {
  id: string
  customerId: string
  name: string
  legalName?: string
  taxId?: string
  creditLimit: number
  creditHoldDays: number
  defaultPaymentTermId?: string
  taxExempt: boolean
  taxExemptCertificate?: string
  currencyCode: string
  isActive: boolean
}

export interface CreateArCustomerRequest {
  customerId: string
  name: string
  legalName?: string | null
  taxId?: string | null
  creditLimit: number
  creditHoldDays: number
  defaultPaymentTermId?: string | null
  taxExempt: boolean
  taxExemptCertificate?: string | null
  currencyCode?: string | null
}

export type UpdateArCustomerRequest = Omit<CreateArCustomerRequest, 'customerId'>

export interface ArInvoiceBatch {
  id: string
  batchNumber: string
  description: string
  status: string
  invoiceCount: number
  totalAmount: number
}

export interface CreateArInvoiceBatchRequest {
  companyId: string
  batchNumber: string
  description: string
  postingDate: string
  fiscalPeriodId: string
}

export interface ArInvoiceLineItem {
  accountId: string
  description: string
  quantity: number
  unitPrice: number
  taxAmount: number
  discountAmount?: number
}

export interface ArInvoiceBatchLineItem {
  customerId: string
  invoiceNumber: string
  invoiceDate: string
  dueDate: string
  description?: string
  paymentTermId?: string
  projectId?: string
  salesOrderId?: string
  lines: ArInvoiceLineItem[]
}

export interface ArInvoiceLineDetail extends ArInvoiceLineItem {
  totalAmount: number
}

export interface ArInvoiceDetail {
  id: string
  customerId: string
  customerName: string
  invoiceNumber: string
  invoiceDate: string
  dueDate: string
  description: string
  status: string
  totalAmount: number
  balanceDue: number
  lines: ArInvoiceLineDetail[]
}

export interface ArInvoiceBatchDetail {
  id: string
  batchNumber: string
  description: string
  status: string
  postingDate: string
  invoices: ArInvoiceDetail[]
}

export interface ArInvoiceSummary {
  id: string
  customerId: string
  invoiceNumber: string
  invoiceDate: string
  dueDate: string
  totalAmount: number
  balanceDue: number
  status: string
}

export interface CreateStandaloneInvoiceRequest {
  companyId: string
  customerId: string
  invoiceNumber: string
  invoiceDate: string
  dueDate: string
  description?: string | null
  paymentTermId?: string | null
  projectId?: string | null
  salesOrderId?: string | null
  lines: ArInvoiceLineItem[]
}

export interface ArCashReceipt {
  id: string
  customerId: string
  receiptReference: string
  totalAmount: number
  appliedAmount: number
  unappliedAmount: number
  status: string
}

export interface CreateArCashReceiptRequest {
  companyId: string
  customerId: string
  receiptReference: string
  totalAmount: number
  receiptDate: string
  paymentMethod: string
  currencyCode?: string | null
  referenceNumber?: string | null
}

export interface ArCashReceiptApplication {
  id: string
  cashReceiptId: string
  invoiceId: string
  appliedAmount: number
}

export interface ArApplyCashRequest {
  invoiceId: string
  amount: number
}

export interface ArStatement {
  id: string
  customerId: string
  customerCode: string
  customerName: string
  statementNumber: string
  asOfDate: string
  status: string
  totalDue: number
}

export interface ArStatementDetail {
  id: string
  statementNumber: string
  asOfDate: string
  status: string | number
  customerName: string
  customerCode: string
  invoices: {
    invoiceNumber: string
    invoiceDate: string
    dueDate: string
    totalAmount: number
    balanceDue: number
    status: string | number
  }[]
  totalDue: number
}

export interface ArMemo {
  id: string
  customerId: string
  referenceNumber: string
  memoDate: string
  memoType: string
  status: string
  totalAmount: number
  description?: string
}

export interface CreateMemoRequest {
  companyId: string
  customerId: string
  referenceNumber: string
  memoDate: string
  memoType: 'CreditMemo' | 'DebitMemo' | number
  invoiceId?: string | null
  description?: string | null
  lines: {
    accountId: string
    description: string
    quantity: number
    unitPrice: number
    taxAmount: number
    discountAmount: number
  }[]
}

// --- Credit Limit Check ---
export interface CreditLimitCheckResult {
  isApproved: boolean
  currentBalance: number
  creditLimit: number
  availableCredit: number
  message?: string | null
}

// --- AR Reports ---
export interface ArAgingLine {
  customerId: string
  customerCode: string
  customerName: string
  currentBalance: number
  days1To30: number
  days31To60: number
  days61To90: number
  over90Days: number
  totalDue: number
}
export interface ArAgingReport {
  companyId: string
  asOfDate: string
  lines: ArAgingLine[]
  totalCurrent: number
  totalDue: number
  generatedOn: string
}
export interface CustomerTrialBalanceLine {
  customerId: string
  customerCode: string
  customerName: string
  beginningBalance: number
  debits: number
  credits: number
  endingBalance: number
}
export interface CustomerTrialBalanceReport {
  companyId: string
  asOfDate: string
  lines: CustomerTrialBalanceLine[]
  totalBeginningBalance: number
  totalEndingBalance: number
  generatedOn: string
}
export interface CashReceiptsJournalLine {
  receiptId: string
  receiptReference: string
  customerId: string
  customerName: string
  receiptDate: string
  amount: number
  paymentMethod: string
  status: string
}
export interface CashReceiptsJournalReport {
  companyId: string
  fromDate: string
  toDate: string
  lines: CashReceiptsJournalLine[]
  totalAmount: number
  totalReceipts: number
  generatedOn: string
}
export interface SalesJournalLine {
  invoiceId: string
  invoiceNumber: string
  customerId: string
  customerName: string
  invoiceDate: string
  amount: number
  status: string
}
export interface SalesJournalReport {
  companyId: string
  fromDate: string
  toDate: string
  lines: SalesJournalLine[]
  totalAmount: number
  totalInvoices: number
  generatedOn: string
}
export interface FinanceChargeReportLine {
  chargeId: string
  chargeNumber: string
  customerId: string
  customerName: string
  chargeDate: string
  amount: number
  annualRate: number
  status: string
}
export interface FinanceChargeReport {
  companyId: string
  asOfDate: string
  lines: FinanceChargeReportLine[]
  totalCharges: number
  generatedOn: string
}

// --- Phase 4: Collections, Dunning, Allowance, Resale, Credit Memo Apply, Aging-by-Basis, Cash Receipt Match ---

export type CollectionNoteType = 'Call' | 'Email' | 'Letter' | 'Visit' | 'Legal' | 'Promise'
export type CollectionNoteStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed' | 'Escalated'

export interface CollectionNote {
  id: string
  companyId: string
  customerId: string
  note: string
  author: string
  type: CollectionNoteType
  status: CollectionNoteStatus
  assignedTo?: string | null
  followUpDate?: string | null
  promiseToPayDate?: string | null
  relatedDocumentNumber?: string | null
}

export interface CreateCollectionNoteRequest {
  companyId: string
  customerId: string
  note: string
  author: string
  type: CollectionNoteType
  assignedTo?: string | null
  followUpDate?: string | null
  promiseToPayDate?: string | null
  relatedDocumentNumber?: string | null
}

export interface UpdateCollectionNoteRequest {
  note?: string | null
  assignedTo?: string | null
  followUpDate?: string | null
  promiseToPayDate?: string | null
  relatedDocumentNumber?: string | null
}

export type DunningBucket = 'Current' | 'Days1To30' | 'Days31To60' | 'Days61To90' | 'Over90'

export interface DunningTemplate {
  id: string
  companyId: string
  name: string
  subject: string
  body: string
  sequence: number
  bucket: DunningBucket
  minDaysOverdue: number
  maxDaysOverdue: number
  sendEmail: boolean
  sendPdf: boolean
  isActive: boolean
}

export interface CreateDunningTemplateRequest {
  companyId: string
  name: string
  subject: string
  body: string
  sequence: number
  bucket: DunningBucket
  minDaysOverdue: number
  maxDaysOverdue: number
  sendEmail: boolean
  sendPdf: boolean
  isActive?: boolean
}

export interface UpdateDunningTemplateRequest {
  name?: string | null
  subject?: string | null
  body?: string | null
  sequence?: number | null
  bucket?: DunningBucket | null
  minDaysOverdue?: number | null
  maxDaysOverdue?: number | null
  sendEmail?: boolean | null
  sendPdf?: boolean | null
  isActive?: boolean | null
}

export interface RunDunningRequest {
  companyId: string
  asOfDate?: string | null
}

export interface DunningRunResultLine {
  customerId: string
  customerName: string
  invoiceNumber: string
  daysOverdue: number
  amount: number
  templateName: string
}

export interface DunningRunResult {
  lettersGenerated: number
  asOfDate: string
  details: DunningRunResultLine[]
}

export type AllowanceRunStatus = 'Draft' | 'Posted'
export type AllowanceMethod = 'PercentageOfReceivables' | 'AgingCategories' | 'Specific'

export interface AllowanceByBucket {
  bucket: DunningBucket
  outstandingBalance: number
  reserveRate: number
  estimatedAmount: number
}

export interface AllowanceRun {
  id: string
  companyId: string
  name: string
  asOfDate: string
  method: AllowanceMethod
  status: AllowanceRunStatus
  estimatedAmount: number
  isPosted: boolean
  postedBy?: string | null
  postedOn?: string | null
  buckets: AllowanceByBucket[]
}

export interface CreateAllowanceRunRequest {
  companyId: string
  name: string
  asOfDate: string
  method: AllowanceMethod
  percentageOfReceivables?: number
  agingRateCurrent?: number
  agingRate1To30?: number
  agingRate31To60?: number
  agingRate61To90?: number
  agingRateOver90?: number
  specificAmount?: number
}

export interface ApplyCreditMemoRequest {
  targetInvoiceIds?: string[]
}

export interface CreditMemoApplyResult {
  memoId: string
  appliedAmount: number
  appliedInvoiceIds: string[]
}

export interface CashReceiptReferenceMatch {
  cashReceiptId: string
  referenceNumber: string
  matchedInvoiceIds: string[]
  appliedAmount: number
  remainingAmount: number
}

export interface AgingBucketBreakdown {
  bucket: DunningBucket
  outstanding: number
}

export interface ArAgingByBasisReport {
  companyId: string
  basis: string
  asOfDate: string
  agingBreakdown: AgingBucketBreakdown[]
  totalOutstanding: number
  generatedOn: string
}

export interface CollectionDashboard {
  openNotes: number
  escalatedNotes: number
  promisesToPay: number
  totalOutstanding: number
}

export interface ResaleCertificate {
  id: string
  companyId: string
  customerId: string
  certificateNumber: string
  issuedState: string
  issueDate: string
  expiryDate: string
  isActive: boolean
}

export interface CreateResaleCertificateRequest {
  companyId: string
  customerId: string
  certificateNumber: string
  issuedState: string
  issueDate: string
  expiryDate: string
}
