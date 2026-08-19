// Purchasing module types (Phase 6) — mirrors backend DTOs.

// --- Requisitions ---
export interface RequisitionSummary {
  id: string
  requisitionNumber: string
  companyId: string
  requestorId: string
  requestDate: string
  needByDate?: string | null
  description?: string | null
  status: string
  totalAmount: number
}

export interface CreateRequisitionRequest {
  companyId: string
  requestorId: string
  requisitionNumber: string
  requestDate: string
  needByDate?: string | null
  description?: string | null
  lines: CreateRequisitionLineRequest[]
}

export interface CreateRequisitionLineRequest {
  lineNumber: number
  itemId?: string | null
  description: string
  quantity: number
  unitOfMeasure: string
  estimatedUnitPrice: number
  needByDate?: string | null
  preferredVendorId?: string | null
  accountId?: string | null
  projectId?: string | null
  taskId?: string | null
}

export interface ApproveRequisitionRequest {
  approvedById: string
}
export interface RejectRequisitionRequest {
  rejectedById: string
  reason: string
}

// --- Purchase Orders ---
export interface PurchaseOrderSummary {
  id: string
  poNumber: string
  companyId: string
  vendorId: string
  orderDate: string
  status: string
  totalAmount: number
  remainingAmount: number
}

export interface PurchaseOrderDetail extends PurchaseOrderSummary {
  orderType: string
  shipToName?: string | null
  shipToAddress?: string | null
  lines: PurchaseOrderLineDto[]
}

export interface PurchaseOrderLineDto {
  id: string
  lineNumber: number
  itemId?: string | null
  description: string
  quantity: number
  unitOfMeasure: string
  unitPrice: number
  quantityReceived: number
  quantityInvoiced: number
  extendedPrice: number
}

export interface CreatePurchaseOrderRequest {
  companyId: string
  vendorId: string
  poNumber: string
  orderDate: string
  orderType: string
  shipToName?: string | null
  shipToAddress?: string | null
  paymentTermId?: string | null
  buyerId?: string | null
  buyerNotes?: string | null
  vendorReference?: string | null
  blanketAmountLimit?: number | null
  freightAmount: number
  freightTaxAmount: number
  taxExempt: boolean
  lines: CreatePurchaseOrderLineRequest[]
}

export interface CreatePurchaseOrderLineRequest {
  lineNumber: number
  itemId?: string | null
  description: string
  quantity: number
  unitOfMeasure: string
  unitPrice: number
  taxCode?: string | null
  taxRate: number
  needByDate?: string | null
  accountId?: string | null
  projectId?: string | null
  taskId?: string | null
  requisitionLineId?: string | null
}

export interface ApproveRequest {
  approvedById: string
}
export interface CloseRequest {
  reason?: string | null
}
export interface CancelRequest {
  reason: string
}

// --- Receipts ---
export interface ReceiptSummary {
  id: string
  receiptNumber: string
  companyId: string
  purchaseOrderId?: string | null
  vendorId?: string | null
  receivedDate: string
  status: string
  isReversed: boolean
}

export interface ReceiptDetail extends ReceiptSummary {
  receivedBy?: string | null
  packingSlipNumber?: string | null
  notes?: string | null
  lines: ReceiptLineDto[]
}

export interface ReceiptLineDto {
  id: string
  lineNumber: number
  purchaseOrderLineId?: string | null
  itemId?: string | null
  description: string
  quantityReceived: number
  unitOfMeasure: string
  lotNumber?: string | null
  serialNumber?: string | null
}

export interface CreateReceiptRequest {
  companyId: string
  receiptNumber: string
  purchaseOrderId?: string | null
  vendorId?: string | null
  receivedDate: string
  receivedBy?: string | null
  packingSlipNumber?: string | null
  notes?: string | null
  lines: CreateReceiptLineRequest[]
}

export interface CreateReceiptLineRequest {
  lineNumber: number
  purchaseOrderLineId?: string | null
  itemId?: string | null
  description: string
  quantityReceived: number
  unitOfMeasure: string
  lotNumber?: string | null
  serialNumber?: string | null
  qualityInspectionRequired: boolean
  warehouseId?: string | null
  binLocationId?: string | null
}

export interface ReverseReceiptRequest {
  reason: string
}

// --- Masters ---
export interface VendorItemSummary {
  id: string
  vendorId: string
  itemCode: string
  vendorItemCode: string
  description?: string | null
  unitCost: number
  leadTimeDays: number
  minimumOrderQuantity: number
  isActive: boolean
}
export interface CreateVendorItemRequest {
  companyId: string
  vendorId: string
  itemCode: string
  vendorItemCode: string
  description?: string | null
  unitCost: number
  leadTimeDays: number
  minimumOrderQuantity: number
}

export interface BuyerAgentSummary {
  id: string
  agentCode: string
  name: string
  email?: string | null
  approvalLimit: number
  isActive: boolean
}
export interface CreateBuyerAgentRequest {
  companyId: string
  agentCode: string
  name: string
  email?: string | null
  approvalLimit: number
}

export interface ShippingMethodSummary {
  id: string
  code: string
  description: string
  carrier?: string | null
  isActive: boolean
}
export interface CreateShippingMethodRequest {
  companyId: string
  code: string
  description: string
  carrier?: string | null
}

export interface FOBTermSummary {
  id: string
  code: string
  description: string
  freightResponsibility?: string | null
  isActive: boolean
}
export interface CreateFOBTermRequest {
  companyId: string
  code: string
  description: string
  freightResponsibility?: string | null
}

// --- Reports ---
export interface OpenPOReportRow {
  poId: string
  poNumber: string
  vendorName: string
  orderDate: string
  status: string
  originalAmount: number
  receivedAmount: number
  remainingAmount: number
}
export interface RequisitionStatusReportRow {
  requisitionId: string
  requisitionNumber: string
  status: string
  totalAmount: number
  approvedDate?: string | null
  convertedToPo: boolean
}
export interface ReceivingReportRow {
  receiptId: string
  receiptNumber: string
  vendorName: string
  receivedDate: string
  itemDescription: string
  quantityReceived: number
}
export interface CommittedCostReportRow {
  poId: string
  poNumber: string
  vendorName: string
  committedAmount: number
  receivedAmount: number
  remainingAmount: number
}

// --- PO Templates ---
export interface POTemplate {
  id: string
  templateCode: string
  templateName: string
  companyId: string
  vendorId: string
  orderType: string
  blanketAmount?: number | null
  amountUsed: number
  remainingAmount: number
  isActive: boolean
  isExpired: boolean
}
export interface POTemplateLine {
  id: string
  lineNumber: number
  itemId?: string | null
  description: string
  defaultQuantity?: number | null
  unitOfMeasure: string
  unitPrice: number
  accountId?: string | null
  projectId?: string | null
}
export interface CreatePOTemplateLineRequest {
  lineNumber: number
  itemId?: string | null
  description: string
  defaultQuantity?: number | null
  unitOfMeasure: string
  unitPrice: number
  accountId?: string | null
  projectId?: string | null
}
export interface CreatePOTemplateRequest {
  templateCode: string
  templateName: string
  companyId: string
  vendorId: string
  orderType: string
  description?: string | null
  blanketAmount?: number | null
  effectiveDate?: string | null
  expirationDate?: string | null
  isActive?: boolean
  lines: CreatePOTemplateLineRequest[]
}
export interface ReleasePOTemplateRequest {
  amount: number
}

// --- Requisition Templates ---
export interface RequisitionTemplate {
  id: string
  templateCode: string
  templateName: string
  companyId: string
  description?: string | null
  isActive: boolean
  lineCount: number
}
export interface RequisitionTemplateLine {
  id: string
  lineNumber: number
  itemId?: string | null
  description: string
  defaultQuantity: number
  unitOfMeasure: string
  accountId?: string | null
  projectId?: string | null
}
export interface CreateRequisitionTemplateLineRequest {
  lineNumber: number
  itemId?: string | null
  description: string
  defaultQuantity: number
  unitOfMeasure: string
  accountId?: string | null
  projectId?: string | null
}
export interface CreateRequisitionTemplateRequest {
  templateCode: string
  templateName: string
  companyId: string
  description?: string | null
  isActive?: boolean
  lines: CreateRequisitionTemplateLineRequest[]
}
export interface UpdateRequisitionTemplateRequest {
  description?: string | null
  isActive?: boolean
}

// --- Requisition → PO Conversion ---
export interface ConvertSingleRequisitionRequest {
  requisitionId: string
  preferredVendorId?: string | null
}
export interface ConsolidateRequisitionsRequest {
  requisitionIds: string[]
  vendorId: string
}
export interface ConversionResult {
  purchaseOrderIds: string[]
  message: string
}

// --- Phase 6 gap additions ---

// Extended PO summary with tax/freight/blanket fields.
export interface PurchaseOrderSummaryExtended extends PurchaseOrderSummary {
  blanketAmountLimit?: number | null
  releasedAmount: number
  freightAmount: number
  freightTaxAmount: number
  taxExempt: boolean
  printedDate?: string | null
  emailedToVendorDate?: string | null
}

export interface ReleasePurchaseOrderRequest {
  amount: number
}

export interface PurchaseOrderPrintLine {
  lineNumber: number
  description: string
  quantity: number
  unitOfMeasure: string
  unitPrice: number
  taxCode?: string | null
  taxRate: number
  taxAmount: number
  extendedWithTax: number
}

export interface PurchaseOrderPrint {
  id: string
  poNumber: string
  vendorId: string
  orderDate: string
  status: string
  lines: PurchaseOrderPrintLine[]
  freightAmount: number
  freightTaxAmount: number
  taxTotal: number
  totalWithTax: number
  taxExempt: boolean
  printedDate?: string | null
}

export interface VendorQuoteLine {
  id: string
  itemId?: string | null
  description: string
  quantity: number
  unitOfMeasure: string
  unitPrice: number
  lineTotal: number
}

export interface VendorQuote {
  id: string
  rfxNumber: string
  companyId: string
  vendorId: string
  requestedById?: string | null
  status: string
  validUntil?: string | null
  notes?: string | null
  quoteNumber?: string | null
  quoteDate?: string | null
  quoteFreight: number
  quoteTotal: number
  lines: VendorQuoteLine[]
}

export interface CreateVendorQuoteLineRequest {
  itemId?: string | null
  description: string
  quantity: number
  unitOfMeasure: string
  unitPrice: number
}

export interface CreateVendorQuoteRequest {
  rfxNumber: string
  companyId: string
  vendorId: string
  requestedById?: string | null
  validUntil?: string | null
  notes?: string | null
  lines: CreateVendorQuoteLineRequest[]
}

export interface ReceiveVendorQuoteRequest {
  quoteNumber: string
  quoteDate: string
  freight: number
  lines?: CreateVendorQuoteLineRequest[]
}

export interface RejectVendorQuoteRequest {
  reason?: string | null
}

// --- Reports (additional) ---
export interface POStatusReportRow {
  status: string
  count: number
  totalAmount: number
  averageDaysInStatus: number
}

export interface VendorPerformanceReportRow {
  vendorId: string
  totalPOs: number
  totalAmount: number
  onTimePOs: number
  onTimePercentage: number
}

export interface PurchaseAnalysisReportRow {
  vendorId: string
  buyerId: string
  poCount: number
  totalSpend: number
  totalTax: number
  totalFreight: number
  lineCount: number
}

export interface PriceVarianceReportRow {
  purchaseOrderId: string
  poNumber: string
  itemId?: string | null
  description: string
  vendorStandardCost: number
  poUnitPrice: number
  variancePercent: number
  extendedVariance: number
}

export interface OverReceiptExceptionReportRow {
  receiptId: string
  receiptNumber: string
  receivedDate: string
  vendorId?: string | null
  purchaseOrderLineId: string
  description: string
  orderedQuantity: number
  receivedQuantity: number
  overReceiptPercent: number
  buyerId: string
}
