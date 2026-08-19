import type {
  ArApplyCashRequest,
  ArCashReceipt,
  ArCashReceiptApplication,
  ArCustomer,
  ArInvoiceBatch,
  ArInvoiceBatchDetail,
  ArInvoiceBatchLineItem,
  ArInvoiceSummary,
  ArMemo,
  ArStatement,
  ArStatementDetail,
  CreateArCashReceiptRequest,
  CreateArCustomerRequest,
  CreateArInvoiceBatchRequest,
  CreateStandaloneInvoiceRequest,
  UpdateArCustomerRequest,
  CreditLimitCheckResult,
  ArAgingReport,
  CustomerTrialBalanceReport,
  CashReceiptsJournalReport,
  SalesJournalReport,
  FinanceChargeReport,
  CollectionNote,
  CreateCollectionNoteRequest,
  UpdateCollectionNoteRequest,
  DunningTemplate,
  CreateDunningTemplateRequest,
  UpdateDunningTemplateRequest,
  RunDunningRequest,
  DunningRunResult,
  AllowanceRun,
  CreateAllowanceRunRequest,
  ApplyCreditMemoRequest,
  CreditMemoApplyResult,
  CashReceiptReferenceMatch,
  ArAgingByBasisReport,
  CollectionDashboard,
  ResaleCertificate,
  CreateResaleCertificateRequest,
} from '@/types/ar'
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

function companyId(): string {
  return useAuthStore.getState().currentCompany?.id ?? DEMO_COMPANY_ID
}

export function getCustomers(): Promise<ArCustomer[]> {
  return get('/ar/customers')
}

export function createCustomer(data: CreateArCustomerRequest): Promise<ArCustomer> {
  return post('/ar/customers', data)
}

export function updateCustomer(id: string, data: UpdateArCustomerRequest): Promise<ArCustomer> {
  return put(`/ar/customers/${id}`, data)
}

export function deleteCustomer(id: string): Promise<void> {
  return del(`/ar/customers/${id}`)
}

export function getInvoices(params?: { companyId?: string | undefined; customerId?: string | undefined }): Promise<ArInvoiceSummary[]> {
  return get('/ar/invoices', { companyId: params?.companyId ?? companyId(), customerId: params?.customerId })
}

export function getInvoiceBatches(companyIdParam?: string): Promise<ArInvoiceBatch[]> {
  return get('/ar/invoices/batches', { companyId: companyIdParam ?? companyId() })
}

export function getInvoiceBatch(batchId: string): Promise<ArInvoiceBatchDetail> {
  return get(`/ar/invoices/batches/${batchId}`)
}

export function createInvoiceBatch(data: CreateArInvoiceBatchRequest): Promise<ArInvoiceBatch> {
  return post('/ar/invoices/batches', data)
}

export function addInvoiceBatchLines(batchId: string, items: ArInvoiceBatchLineItem[]): Promise<void> {
  return post(`/ar/invoices/batches/${batchId}/lines`, items)
}

export function releaseInvoiceBatch(batchId: string): Promise<void> {
  return post(`/ar/invoices/batches/${batchId}/release`)
}

export function postInvoiceBatch(batchId: string): Promise<void> {
  return post(`/ar/invoices/batches/${batchId}/post`)
}

export function createStandaloneInvoice(data: CreateStandaloneInvoiceRequest): Promise<ArInvoiceBatch> {
  return post('/ar/invoices/standalone', data)
}

export function voidInvoice(invoiceId: string): Promise<void> {
  return post(`/ar/invoices/${invoiceId}/void`)
}

export function getCashReceipts(companyIdParam?: string): Promise<ArCashReceipt[]> {
  return get('/ar/cash-receipts', { companyId: companyIdParam ?? companyId() })
}

export function createCashReceipt(data: CreateArCashReceiptRequest): Promise<ArCashReceipt> {
  return post('/ar/cash-receipts', data)
}

export function applyCash(receiptId: string, data: ArApplyCashRequest): Promise<void> {
  return post(`/ar/cash-receipts/${receiptId}/apply`, data)
}

export function autoApplyCash(receiptId: string): Promise<ArCashReceiptApplication[]> {
  return post(`/ar/cash-receipts/${receiptId}/auto-apply`)
}

export function generateStatements(companyIdParam?: string): Promise<{ count: number; asOfDate: string }> {
  return post(`/ar/statements/generate?companyId=${companyIdParam ?? companyId()}`)
}

export function getStatements(companyIdParam?: string): Promise<ArStatement[]> {
  return get('/ar/statements', { companyId: companyIdParam ?? companyId() })
}

export function getStatement(id: string): Promise<ArStatementDetail> {
  return get(`/ar/statements/${id}`)
}

export function calculateFinanceCharges(params?: {
  companyId?: string
  annualRate?: number
}): Promise<{ count: number; asOfDate: string; annualRate: number }> {
  const rate = params?.annualRate != null ? `&annualRate=${params.annualRate}` : ''
  return post(`/ar/finance-charges/calculate?companyId=${params?.companyId ?? companyId()}${rate}`)
}

export function getMemos(companyIdParam?: string): Promise<ArMemo[]> {
  return get('/ar/memos', { companyId: companyIdParam ?? companyId() })
}

export function getMemo(id: string): Promise<ArMemo> {
  return get(`/ar/memos/${id}`)
}

export function createMemo(data: import('@/types/ar').CreateMemoRequest): Promise<ArMemo> {
  return post('/ar/memos', data)
}

// --- Credit Limit Check ---
export function checkCreditLimit(customerId: string, amount: number): Promise<CreditLimitCheckResult> {
  return get(`/ar/credit-limit/check/${customerId}`, { amount })
}

// --- AR Reports ---
export function getArAgingReport(asOfDate?: string, companyIdParam?: string): Promise<ArAgingReport> {
  return get('/ar/reports/aging', { asOfDate, companyId: companyIdParam ?? companyId() })
}
export function getCustomerTrialBalance(asOfDate?: string, companyIdParam?: string): Promise<CustomerTrialBalanceReport> {
  return get('/ar/reports/customer-trial-balance', { asOfDate, companyId: companyIdParam ?? companyId() })
}
export function getCashReceiptsJournal(fromDate?: string, toDate?: string, companyIdParam?: string): Promise<CashReceiptsJournalReport> {
  return get('/ar/reports/cash-receipts-journal', { fromDate, toDate, companyId: companyIdParam ?? companyId() })
}
export function getSalesJournal(fromDate?: string, toDate?: string, companyIdParam?: string): Promise<SalesJournalReport> {
  return get('/ar/reports/sales-journal', { fromDate, toDate, companyId: companyIdParam ?? companyId() })
}
export function getArFinanceChargeReport(asOfDate?: string, companyIdParam?: string): Promise<FinanceChargeReport> {
  return get('/ar/reports/finance-charge', { asOfDate, companyId: companyIdParam ?? companyId() })
}

// --- Phase 4: Collections ---
export function getCollectionNotes(companyIdParam?: string): Promise<CollectionNote[]> {
  return get('/ar/collection-notes', { companyId: companyIdParam ?? companyId() })
}
export function createCollectionNote(data: CreateCollectionNoteRequest): Promise<CollectionNote> {
  return post('/ar/collection-notes', data)
}
export function updateCollectionNote(id: string, data: UpdateCollectionNoteRequest): Promise<CollectionNote> {
  return put(`/ar/collection-notes/${id}`, data)
}
export function assignCollectionNote(id: string, assignedTo: string): Promise<CollectionNote> {
  return post(`/ar/collection-notes/${id}/assign`, { assignedTo })
}
export function closeCollectionNote(id: string): Promise<CollectionNote> {
  return post(`/ar/collection-notes/${id}/close`)
}
export function reopenCollectionNote(id: string): Promise<CollectionNote> {
  return post(`/ar/collection-notes/${id}/reopen`)
}
export function getCollectionsDashboard(companyIdParam?: string): Promise<CollectionDashboard> {
  return get('/ar/collections-dashboard', { companyId: companyIdParam ?? companyId() })
}

// --- Phase 4: Dunning ---
export function getDunningTemplates(companyIdParam?: string): Promise<DunningTemplate[]> {
  return get('/ar/dunning-templates', { companyId: companyIdParam ?? companyId() })
}
export function createDunningTemplate(data: CreateDunningTemplateRequest): Promise<DunningTemplate> {
  return post('/ar/dunning-templates', data)
}
export function updateDunningTemplate(id: string, data: UpdateDunningTemplateRequest): Promise<DunningTemplate> {
  return put(`/ar/dunning-templates/${id}`, data)
}
export function runDunning(request: RunDunningRequest): Promise<DunningRunResult> {
  return post('/ar/dunning-templates/run', request)
}

// --- Phase 4: Allowance for Doubtful Accounts ---
export function getAllowanceRuns(companyIdParam?: string): Promise<AllowanceRun[]> {
  return get('/ar/allowance-runs', { companyId: companyIdParam ?? companyId() })
}
export function createAllowanceRun(data: CreateAllowanceRunRequest): Promise<AllowanceRun> {
  return post('/ar/allowance-runs', data)
}
export function postAllowanceRun(id: string): Promise<AllowanceRun> {
  return post(`/ar/allowance-runs/${id}/post`)
}

// --- Phase 4: Resale Certificates ---
export function getResaleCertificates(companyIdParam?: string): Promise<ResaleCertificate[]> {
  return get('/ar/resale-certificates', { companyId: companyIdParam ?? companyId() })
}
export function createResaleCertificate(data: CreateResaleCertificateRequest): Promise<ResaleCertificate> {
  return post('/ar/resale-certificates', data)
}
export function updateResaleCertificate(id: string, data: CreateResaleCertificateRequest): Promise<ResaleCertificate> {
  return put(`/ar/resale-certificates/${id}`, data)
}

// --- Phase 4: Credit Memo Application ---
export function applyCreditMemo(memoId: string, data: ApplyCreditMemoRequest): Promise<CreditMemoApplyResult> {
  return post(`/ar/credit-memos/${memoId}/apply`, data)
}

// --- Phase 4: AR Aging by Due Date vs Invoice Date ---
export function getArAgingByBasis(basis: string, asOfDate?: string, companyIdParam?: string): Promise<ArAgingByBasisReport> {
  return get('/ar/reports/aging-by-basis', { basis, asOfDate, companyId: companyIdParam ?? companyId() })
}

// --- Phase 4: Cash Receipt matching by reference ---
export function matchCashReceiptByReference(receiptId: string): Promise<CashReceiptReferenceMatch> {
  return post(`/ar/cash-receipts/${receiptId}/match-by-reference`)
}

