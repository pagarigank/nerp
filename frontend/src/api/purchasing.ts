// Purchasing module API client (Phase 6).
import { useAuthStore } from '@stores/authStore'
import { get, post } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

export function companyId(): string {
  return useAuthStore.getState().currentCompany?.id ?? DEMO_COMPANY_ID
}

function userId(): string {
  return useAuthStore.getState().user?.id ?? DEMO_COMPANY_ID
}

export { userId }

function today(): string {
  return new Date().toISOString().split('T')[0] ?? ''
}

// --- Requisitions ---
export function getRequisitions(): Promise<import('@/types/purchasing').RequisitionSummary[]> {
  return get('/purchasing/requisitions')
}
export function createRequisition(d: import('@/types/purchasing').CreateRequisitionRequest) {
  return post('/purchasing/requisitions', d)
}
export function submitRequisition(id: string) {
  return post(`/purchasing/requisitions/${id}/submit-for-approval`)
}
export function approveRequisition(id: string) {
  return post(`/purchasing/requisitions/${id}/approve`, { approvedById: userId() })
}
export function rejectRequisition(id: string, reason: string) {
  return post(`/purchasing/requisitions/${id}/reject`, { rejectedById: userId(), reason })
}
export function cancelRequisition(id: string) {
  return post(`/purchasing/requisitions/${id}/cancel`)
}

// --- Purchase Orders ---
export function getPurchaseOrders(): Promise<import('@/types/purchasing').PurchaseOrderSummary[]> {
  return get('/purchasing/purchase-orders')
}
export function getPurchaseOrder(id: string): Promise<import('@/types/purchasing').PurchaseOrderDetail> {
  return get(`/purchasing/purchase-orders/${id}`)
}
export function createPurchaseOrder(d: import('@/types/purchasing').CreatePurchaseOrderRequest) {
  return post('/purchasing/purchase-orders', d)
}
export function approvePurchaseOrder(id: string) {
  return post(`/purchasing/purchase-orders/${id}/approve`, { approvedById: userId() })
}
export function submitPurchaseOrder(id: string) {
  return post(`/purchasing/purchase-orders/${id}/submit-for-approval`)
}
export function closePurchaseOrder(id: string, reason?: string) {
  return post(`/purchasing/purchase-orders/${id}/close`, { reason: reason ?? null })
}
export function cancelPurchaseOrder(id: string, reason: string) {
  return post(`/purchasing/purchase-orders/${id}/cancel`, { reason })
}

// --- Receipts ---
export function getReceipts(): Promise<import('@/types/purchasing').ReceiptSummary[]> {
  return get('/purchasing/receipts')
}
export function getReceipt(id: string): Promise<import('@/types/purchasing').ReceiptDetail> {
  return get(`/purchasing/receipts/${id}`)
}
export function createReceipt(d: import('@/types/purchasing').CreateReceiptRequest) {
  return post('/purchasing/receipts', d)
}
export function postReceipt(id: string) {
  return post(`/purchasing/receipts/${id}/post`)
}
export function reverseReceipt(id: string, reason: string) {
  return post(`/purchasing/receipts/${id}/reverse`, { reason })
}

// --- Masters ---
export function getVendorItems(): Promise<import('@/types/purchasing').VendorItemSummary[]> {
  return get('/purchasing/vendor-items')
}
export function createVendorItem(d: import('@/types/purchasing').CreateVendorItemRequest) {
  return post('/purchasing/vendor-items', d)
}
export function getBuyerAgents(): Promise<import('@/types/purchasing').BuyerAgentSummary[]> {
  return get('/purchasing/buyer-agents')
}
export function createBuyerAgent(d: import('@/types/purchasing').CreateBuyerAgentRequest) {
  return post('/purchasing/buyer-agents', d)
}
export function getShippingMethods(): Promise<import('@/types/purchasing').ShippingMethodSummary[]> {
  return get('/purchasing/shipping-methods')
}
export function createShippingMethod(d: import('@/types/purchasing').CreateShippingMethodRequest) {
  return post('/purchasing/shipping-methods', d)
}
export function getFOBTerms(): Promise<import('@/types/purchasing').FOBTermSummary[]> {
  return get('/purchasing/fob-terms')
}
export function createFOBTerm(d: import('@/types/purchasing').CreateFOBTermRequest) {
  return post('/purchasing/fob-terms', d)
}

// --- Reports ---
export function getOpenPOReport(): Promise<import('@/types/purchasing').OpenPOReportRow[]> {
  return get('/purchasing/reports/open-po')
}
export function getRequisitionStatusReport(): Promise<import('@/types/purchasing').RequisitionStatusReportRow[]> {
  return get('/purchasing/reports/requisition-status')
}
export function getReceivingReport(): Promise<import('@/types/purchasing').ReceivingReportRow[]> {
  return get('/purchasing/reports/receiving-report')
}
export function getCommittedCostReport(): Promise<import('@/types/purchasing').CommittedCostReportRow[]> {
  return get('/purchasing/reports/committed-cost')
}

// Convenience helpers for form defaults.
export const newRequisitionDefaults = () => ({
  companyId: companyId(),
  requestorId: userId(),
  requisitionNumber: '',
  requestDate: today(),
  needByDate: null as string | null,
  description: '',
  lines: [] as import('@/types/purchasing').CreateRequisitionLineRequest[],
})

export const newPurchaseOrderDefaults = () => ({
  companyId: companyId(),
  vendorId: '',
  poNumber: '',
  orderDate: today(),
  orderType: 'Standard',
  shipToName: '',
  shipToAddress: '',
  paymentTermId: null as string | null,
  buyerId: null as string | null,
  buyerNotes: '',
  vendorReference: '',
  blanketAmountLimit: null as number | null,
  freightAmount: 0,
  freightTaxAmount: 0,
  taxExempt: false,
  lines: [] as import('@/types/purchasing').CreatePurchaseOrderLineRequest[],
})

export const newReceiptDefaults = () => ({
  companyId: companyId(),
  receiptNumber: '',
  purchaseOrderId: null as string | null,
  vendorId: null as string | null,
  receivedDate: today(),
  receivedBy: '',
  packingSlipNumber: '',
  notes: '',
  lines: [] as import('@/types/purchasing').CreateReceiptLineRequest[],
})

// --- PO Templates ---
export function getPOTemplates(companyIdParam?: string): Promise<import('@/types/purchasing').POTemplate[]> {
  return get('/purchasing/po-templates', { companyId: companyIdParam ?? companyId() })
}
export function createPOTemplate(data: import('@/types/purchasing').CreatePOTemplateRequest): Promise<import('@/types/purchasing').POTemplate> {
  return post('/purchasing/po-templates', data)
}
export function releasePOTemplate(id: string, amount: number): Promise<import('@/types/purchasing').POTemplate> {
  return post(`/purchasing/po-templates/${id}/release`, { amount })
}

// --- Requisition Templates ---
export function getRequisitionTemplates(companyIdParam?: string): Promise<import('@/types/purchasing').RequisitionTemplate[]> {
  return get('/purchasing/requisition-templates', { companyId: companyIdParam ?? companyId() })
}
export function createRequisitionTemplate(data: import('@/types/purchasing').CreateRequisitionTemplateRequest): Promise<import('@/types/purchasing').RequisitionTemplate> {
  return post('/purchasing/requisition-templates', data)
}
export function updateRequisitionTemplate(id: string, data: import('@/types/purchasing').UpdateRequisitionTemplateRequest): Promise<import('@/types/purchasing').RequisitionTemplate> {
  return post(`/purchasing/requisition-templates/${id}`, data)
}

// --- Requisition → PO Conversion ---
export function convertRequisitionToPo(data: import('@/types/purchasing').ConvertSingleRequisitionRequest): Promise<import('@/types/purchasing').ConversionResult> {
  return post('/purchasing/requisition-conversion/convert-single', data)
}
export function consolidateRequisitionsToPo(data: import('@/types/purchasing').ConsolidateRequisitionsRequest): Promise<import('@/types/purchasing').ConversionResult> {
  return post('/purchasing/requisition-conversion/consolidate', data)
}

// --- Phase 6 gap additions ---

// Blanket / standing PO release (draw-down)
export function releasePurchaseOrder(id: string, amount: number): Promise<import('@/types/purchasing').PurchaseOrderSummaryExtended> {
  return post(`/purchasing/purchase-orders/${id}/release`, { amount })
}
// PO print (marks printed + returns print-ready DTO)
export function printPurchaseOrder(id: string): Promise<import('@/types/purchasing').PurchaseOrderPrint> {
  return post(`/purchasing/purchase-orders/${id}/print`)
}
// PO email to vendor (marks emailed)
export function emailPurchaseOrderToVendor(id: string): Promise<import('@/types/purchasing').PurchaseOrderSummaryExtended> {
  return post(`/purchasing/purchase-orders/${id}/email-vendor`)
}
// Approval queue (pending-approval POs)
export function getPurchaseOrderApprovalQueue(): Promise<import('@/types/purchasing').PurchaseOrderSummaryExtended[]> {
  return get('/purchasing/purchase-orders/approval-queue')
}

// Vendor quotes (RFQ workflow)
export function getVendorQuotes(vendorId?: string): Promise<import('@/types/purchasing').VendorQuote[]> {
  return get('/purchasing/vendor-quotes', vendorId ? { vendorId } : {})
}
export function getVendorQuote(id: string): Promise<import('@/types/purchasing').VendorQuote> {
  return get(`/purchasing/vendor-quotes/${id}`)
}
export function createVendorQuote(data: import('@/types/purchasing').CreateVendorQuoteRequest): Promise<import('@/types/purchasing').VendorQuote> {
  return post('/purchasing/vendor-quotes', data)
}
export function receiveVendorQuote(id: string, data: import('@/types/purchasing').ReceiveVendorQuoteRequest): Promise<import('@/types/purchasing').VendorQuote> {
  return post(`/purchasing/vendor-quotes/${id}/receive`, data)
}
export function awardVendorQuote(id: string): Promise<import('@/types/purchasing').VendorQuote> {
  return post(`/purchasing/vendor-quotes/${id}/award`)
}
export function rejectVendorQuote(id: string, reason?: string | null): Promise<import('@/types/purchasing').VendorQuote> {
  return post(`/purchasing/vendor-quotes/${id}/reject`, { reason: reason ?? null })
}

// --- Additional reports ---
export function getPOStatusReport(): Promise<import('@/types/purchasing').POStatusReportRow[]> {
  return get('/purchasing/reports/po-status')
}
export function getVendorPerformanceReport(): Promise<import('@/types/purchasing').VendorPerformanceReportRow[]> {
  return get('/purchasing/reports/vendor-performance')
}
export function getPurchaseAnalysisReport(): Promise<import('@/types/purchasing').PurchaseAnalysisReportRow[]> {
  return get('/purchasing/reports/purchase-analysis')
}
export function getPriceVarianceReport(threshold?: number): Promise<import('@/types/purchasing').PriceVarianceReportRow[]> {
  return get('/purchasing/reports/price-variance', threshold ? { threshold } : {})
}
export function getOverReceiptExceptionReport(): Promise<import('@/types/purchasing').OverReceiptExceptionReportRow[]> {
  return get('/purchasing/reports/over-receipt-exceptions')
}
