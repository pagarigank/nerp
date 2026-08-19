import type {
  AddVoucherToBatchRequest,
  ApAccountDistributionReport,
  ApAgingReport,
  ApBatchRegisterReport,
  CashRequirementsReport,
  CheckRegisterReport,
  CreatePaymentRequest,
  CreatePaymentTermRequest,
  CreateVendorRequest,
  CreateVoucherBatchRequest,
  Form1099SummaryResult,
  Payment,
  PaymentTerm,
  ReverseApBatchRequest,
  SelectVouchersForPaymentRequest,
  UpdatePaymentTermRequest,
  UpdateVendorRequest,
  Vendor,
  VendorTrialBalanceReport,
  VoidPaymentRequest,
  Voucher,
  VoucherBatch,
  ThreeWayMatchRequest,
  ThreeWayMatchResult,
  BackupWithholdingCalculateRequest,
  BackupWithholdingResult,
  DuplicateInvoiceCheckDto,
  VendorW9Dto,
  CaptureW9Request,
  VendorBankVerificationDto,
  VerifyBankRequest,
  BankDecisionRequest,
  CashDiscountCaptureDto,
  LostDiscountSummaryDto,
  StaleCheckEscheatmentDto,
  GrirAccrualDto,
  VendorStatementDto,
  CreateVendorStatementRequest,
  Ap1099ClassificationDto,
  Classify1099Request,
  FourWayMatchRequest,
  FourWayMatchResult,
} from '@/types/ap'
import { useAuthStore } from '@stores/authStore'
import { get, post, put } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

function companyId(): string {
  return useAuthStore.getState().currentCompany?.id ?? DEMO_COMPANY_ID
}

export function getVendors(activeOnly?: boolean): Promise<Vendor[]> {
  return get('/ap/vendors', { activeOnly })
}

export function getVendor(id: string): Promise<Vendor> {
  return get(`/ap/vendors/${id}`)
}

export function createVendor(data: CreateVendorRequest): Promise<Vendor> {
  return post('/ap/vendors', data)
}

export function updateVendor(id: string, data: UpdateVendorRequest): Promise<Vendor> {
  return put(`/ap/vendors/${id}`, data)
}

export function activateVendor(id: string): Promise<void> {
  return post(`/ap/vendors/${id}/activate`)
}

export function deactivateVendor(id: string): Promise<void> {
  return post(`/ap/vendors/${id}/deactivate`)
}

export function getPaymentTerms(activeOnly?: boolean): Promise<PaymentTerm[]> {
  return get('/ap/payment-terms', { activeOnly })
}

export function createPaymentTerm(data: CreatePaymentTermRequest): Promise<PaymentTerm> {
  return post('/ap/payment-terms', data)
}

export function updatePaymentTerm(id: string, data: UpdatePaymentTermRequest): Promise<PaymentTerm> {
  return put(`/ap/payment-terms/${id}`, data)
}

export function activatePaymentTerm(id: string): Promise<void> {
  return post(`/ap/payment-terms/${id}/activate`)
}

export function deactivatePaymentTerm(id: string): Promise<void> {
  return post(`/ap/payment-terms/${id}/deactivate`)
}

export function getVoucherBatches(companyIdParam?: string): Promise<VoucherBatch[]> {
  return get('/ap/voucher-batches', { companyId: companyIdParam ?? companyId() })
}

export function getVoucherBatch(id: string): Promise<VoucherBatch> {
  return get(`/ap/voucher-batches/${id}`)
}

export function createVoucherBatch(data: CreateVoucherBatchRequest): Promise<VoucherBatch> {
  return post('/ap/voucher-batches', data)
}

export function addVoucherToBatch(batchId: string, data: AddVoucherToBatchRequest): Promise<Voucher> {
  return post(`/ap/voucher-batches/${batchId}/vouchers`, data)
}

export function releaseVoucherBatch(id: string): Promise<VoucherBatch> {
  return post(`/ap/voucher-batches/${id}/release`)
}

export function postVoucherBatch(id: string): Promise<VoucherBatch> {
  return post(`/ap/voucher-batches/${id}/post`)
}

export function reverseVoucherBatch(id: string, data: ReverseApBatchRequest): Promise<VoucherBatch> {
  return post(`/ap/voucher-batches/${id}/reverse`, data)
}

export function getNextVoucherBatchNumber(companyIdParam?: string): Promise<string> {
  return get('/ap/voucher-batches/next-number', { companyId: companyIdParam ?? companyId() })
}

export function getPayments(companyIdParam?: string): Promise<Payment[]> {
  return get('/ap/payments', { companyId: companyIdParam ?? companyId() })
}

export function getPayment(id: string): Promise<Payment> {
  return get(`/ap/payments/${id}`)
}

export function createPayment(data: CreatePaymentRequest): Promise<Payment> {
  return post('/ap/payments', data)
}

export function selectVouchersForPayment(id: string, data: SelectVouchersForPaymentRequest): Promise<Payment> {
  return post(`/ap/payments/${id}/select-vouchers`, data)
}

export function issuePayment(id: string): Promise<Payment> {
  return post(`/ap/payments/${id}/issue`)
}

export function voidPayment(id: string, data: VoidPaymentRequest): Promise<Payment> {
  return post(`/ap/payments/${id}/void`, data)
}

export function getAgingReport(companyIdParam?: string): Promise<ApAgingReport> {
  return get('/ap/reports/aging', { companyId: companyIdParam ?? companyId() })
}

export function getVendorTrialBalance(companyIdParam?: string): Promise<VendorTrialBalanceReport> {
  return get('/ap/reports/vendor-trial-balance', { companyId: companyIdParam ?? companyId() })
}

export function getBatchRegister(companyIdParam?: string): Promise<ApBatchRegisterReport> {
  return get('/ap/reports/batch-register', { companyId: companyIdParam ?? companyId() })
}

export function getCashRequirements(params?: { companyId?: string; daysAhead?: number }): Promise<CashRequirementsReport> {
  return get('/ap/reports/cash-requirements', {
    companyId: params?.companyId ?? companyId(),
    daysAhead: params?.daysAhead,
  })
}

export function getForm1099Summary(taxYear: number, companyIdParam?: string): Promise<Form1099SummaryResult> {
  return get('/ap/1099/summary', { companyId: companyIdParam ?? companyId(), taxYear })
}

export function getCheckRegister(params?: { companyId?: string; fromDate?: string; toDate?: string }): Promise<CheckRegisterReport> {
  return get('/ap/reports/check-register', {
    companyId: params?.companyId ?? companyId(),
    fromDate: params?.fromDate,
    toDate: params?.toDate,
  })
}

export function getApAccountDistribution(params?: { companyId?: string; fromDate?: string; toDate?: string }): Promise<ApAccountDistributionReport> {
  return get('/ap/reports/account-distribution', {
    companyId: params?.companyId ?? companyId(),
    fromDate: params?.fromDate,
    toDate: params?.toDate,
  })
}

// --- Three-Way Match ---
export function validateThreeWayMatch(data: ThreeWayMatchRequest): Promise<ThreeWayMatchResult> {
  return post('/ap/three-way-match/validate', data)
}

// --- Backup Withholding ---
export function calculateBackupWithholding(data: BackupWithholdingCalculateRequest): Promise<BackupWithholdingResult> {
  return post('/ap/backup-withholding/calculate', data)
}
export function getBackupWithholdingByVendor(vendorId: string, paymentAmount: number): Promise<BackupWithholdingResult> {
  return get(`/ap/backup-withholding/vendor/${vendorId}`, { paymentAmount })
}

// --- Form 1099 e-File export ---
export function getForm1099Efile(taxYear: number, companyIdParam?: string): Promise<string> {
  return get('/ap/1099/efile', { companyId: companyIdParam ?? companyId(), taxYear })
}

// --- Phase 3 gap features ---
export function checkDuplicateInvoice(data: { companyId: string; vendorId: string; invoiceNumber: string; amount: number; lookbackDays: number }): Promise<DuplicateInvoiceCheckDto> {
  return post('/ap/duplicate-invoice-check', data)
}
export function getDuplicateChecks(companyId: string, onlyDuplicates?: boolean): Promise<DuplicateInvoiceCheckDto[]> {
  return get('/ap/duplicate-invoice-checks', { companyId, onlyDuplicates })
}

export function captureW9(vendorId: string, data: CaptureW9Request): Promise<VendorW9Dto> {
  return post(`/ap/vendors/${vendorId}/w9`, data)
}
export function getW9(vendorId: string): Promise<VendorW9Dto[]> {
  return get(`/ap/vendors/${vendorId}/w9`)
}

export function verifyBankAccount(data: VerifyBankRequest): Promise<VendorBankVerificationDto> {
  return post('/ap/bank-verifications', data)
}
export function approveBankVerification(id: string, data: BankDecisionRequest): Promise<VendorBankVerificationDto> {
  return post(`/ap/bank-verifications/${id}/approve`, data)
}
export function rejectBankVerification(id: string, data: BankDecisionRequest): Promise<VendorBankVerificationDto> {
  return post(`/ap/bank-verifications/${id}/reject`, data)
}
export function getBankVerifications(vendorBankAccountId?: string): Promise<VendorBankVerificationDto[]> {
  return get('/ap/bank-verifications', { vendorBankAccountId })
}

export function captureCashDiscount(data: { voucherId: string; vendorId: string; invoiceAmount: number; discountAvailable: number; discountTaken: number; discountLost: boolean }): Promise<CashDiscountCaptureDto> {
  return post('/ap/cash-discounts', data)
}
export function getCashDiscounts(companyId: string): Promise<CashDiscountCaptureDto[]> {
  return get('/ap/cash-discounts', { companyId })
}
export function getLostDiscountSummary(companyId: string): Promise<LostDiscountSummaryDto> {
  return get('/ap/cash-discounts/lost-summary', { companyId })
}

export function flagStaleChecks(data: { companyId: string; statutoryDays: number }): Promise<StaleCheckEscheatmentDto[]> {
  return post('/ap/escheatment/flag', data)
}
export function getEscheatment(companyId: string): Promise<StaleCheckEscheatmentDto[]> {
  return get('/ap/escheatment', { companyId })
}
export function reportEscheatment(id: string): Promise<StaleCheckEscheatmentDto> {
  return post(`/ap/escheatment/${id}/report`)
}

export function createGrirAccrual(data: { companyId: string; vendorId: string; purchaseOrderId?: string | null; receiptId?: string | null; accrualAmount: number; fiscalPeriodId: string }): Promise<GrirAccrualDto> {
  return post('/ap/grir-accruals', data)
}
export function reverseGrirAccrual(id: string, data: { fiscalPeriodId: string }): Promise<GrirAccrualDto> {
  return post(`/ap/grir-accruals/${id}/reverse`, data)
}
export function getGrirAccruals(companyId: string): Promise<GrirAccrualDto[]> {
  return get('/ap/grir-accruals', { companyId })
}

export function createVendorStatement(data: CreateVendorStatementRequest): Promise<VendorStatementDto> {
  return post('/ap/vendor-statements', data)
}
export function getVendorStatements(companyId: string): Promise<VendorStatementDto[]> {
  return get('/ap/vendor-statements', { companyId })
}
export function closeVendorStatement(id: string): Promise<VendorStatementDto> {
  return post(`/ap/vendor-statements/${id}/close`)
}

export function classify1099(data: Classify1099Request): Promise<Ap1099ClassificationDto> {
  return post('/ap/1099/classify', data)
}
export function get1099Classifications(vendorId?: string, taxYear?: number): Promise<Ap1099ClassificationDto[]> {
  return get('/ap/1099/classifications', { vendorId, taxYear })
}

export function validateFourWayMatch(data: FourWayMatchRequest): Promise<FourWayMatchResult> {
  return post('/ap/four-way-match/validate', data)
}
