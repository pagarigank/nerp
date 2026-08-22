// Order Management (Phase 8) types — mirror the backend response records.

export type SalesOrderStatus = 'Draft' | 'Confirmed' | 'Shipped' | 'Closed' | 'Cancelled'
export type ShipmentStatus = 'Draft' | 'Confirmed' | 'InTransit' | 'Delivered' | 'Cancelled'

export interface SalesOrderLineSummary {
  id: string
  lineNumber: number
  itemId: string
  description: string
  quantity: number
  unitPrice: number
  unitOfMeasure: string
  discountPercent: number
  taxPercent: number
  warehouseId?: string | null
  projectId?: string | null
  accountId?: string | null
  itemCategoryId?: string | null
  isDropShip: boolean
  dropShipVendorId?: string | null
  shippedQuantity: number
  lineTotal: number
  appliedPricingRuleId?: string | null
  dropShipConfirmedOn?: string | null
}

export interface SalesOrderSummary {
  id: string
  orderNumber: string
  companyId: string
  customerId: string
  orderDate: string
  status: SalesOrderStatus
  totalAmount: number
  pendingDropShipCount?: number
  firstPendingDropShipLineId?: string | null
}

export interface SalesOrderDetail {
  id: string
  orderNumber: string
  companyId: string
  customerId: string
  orderDate: string
  shipToAddress?: string | null
  billToAddress?: string | null
  paymentTermId?: string | null
  salesRepId?: string | null
  shippingMethod?: string | null
  customerPoNumber?: string | null
  salesOrderTypeId?: string | null
  taxCodeId?: string | null
  taxExemptionCertificateId?: string | null
  status: SalesOrderStatus
  isOnCreditHold: boolean
  requiresDiscountApproval: boolean
  discountApproved: boolean
  lines: SalesOrderLineSummary[]
}

export interface CreateSalesOrderLineRequest {
  lineNumber: number
  itemId: string
  description: string
  quantity: number
  unitPrice: number
  unitOfMeasure: string
  discountPercent: number
  taxPercent: number
  warehouseId?: string | null
  projectId?: string | null
  accountId?: string | null
  itemCategoryId?: string | null
  isDropShip?: boolean
  dropShipVendorId?: string | null
}

export interface CreateSalesOrderRequest {
  orderNumber: string
  companyId: string
  customerId: string
  orderDate: string
  shipToAddress?: string | null
  billToAddress?: string | null
  paymentTermId?: string | null
  salesRepId?: string | null
  shippingMethod?: string | null
  customerPoNumber?: string | null
  salesOrderTypeId?: string | null
  taxCodeId?: string | null
  taxExemptionCertificateId?: string | null
  lines: CreateSalesOrderLineRequest[]
}

export interface CreateShipmentLineRequest {
  lineNumber: number
  itemId: string
  description: string
  quantity: number
  unitPrice: number
  unitOfMeasure: string
  warehouseId?: string | null
  salesOrderLineId?: string | null
  projectId?: string | null
  accountId?: string | null
  discountPercent: number
  taxPercent: number
}

export interface CreateShipmentRequest {
  shipmentNumber: string
  companyId: string
  customerId: string
  salesOrderId?: string | null
  shipmentDate: string
  carrier?: string | null
  trackingNumber?: string | null
  freightCost: number
  lines: CreateShipmentLineRequest[]
}

export interface ShipmentLineSummary {
  id: string
  lineNumber: number
  itemId: string
  description: string
  quantity: number
  unitPrice: number
  unitOfMeasure: string
  warehouseId?: string | null
  salesOrderLineId?: string | null
  projectId?: string | null
  accountId?: string | null
  discountPercent: number
  taxPercent: number
}

export interface ShipmentSummary {
  id: string
  shipmentNumber: string
  companyId: string
  customerId: string
  salesOrderId?: string | null
  shipmentDate: string
  status: ShipmentStatus
  totalAmount: number
}

export interface ShipmentDetail {
  id: string
  shipmentNumber: string
  companyId: string
  customerId: string
  salesOrderId?: string | null
  shipmentDate: string
  carrier?: string | null
  trackingNumber?: string | null
  freightCost: number
  status: ShipmentStatus
  lines: ShipmentLineSummary[]
}

export interface CreateShipmentLineRequest {
  lineNumber: number
  itemId: string
  description: string
  quantity: number
  unitPrice: number
  unitOfMeasure: string
  warehouseId?: string | null
  salesOrderLineId?: string | null
  projectId?: string | null
  accountId?: string | null
  discountPercent: number
  taxPercent: number
}

export type ReturnStatus = 'Draft' | 'Confirmed' | 'Received' | 'Cancelled' | 'PendingApproval'

export interface ReturnLineSummary {
  id: string
  lineNumber: number
  itemId: string
  description: string
  quantity: number
  unitPrice: number
  unitOfMeasure: string
  warehouseId?: string | null
  shipmentLineId?: string | null
  salesOrderLineId?: string | null
  accountId?: string | null
  discountPercent: number
  taxPercent: number
  restockDisposition?: string | null
  lineTotal: number
}

export interface ReturnSummary {
  id: string
  returnNumber: string
  companyId: string
  customerId: string
  shipmentId?: string | null
  salesOrderId?: string | null
  returnDate: string
  status: ReturnStatus
  totalAmount: number

  /** Gross Σ qty × unit price — drives the approval threshold gate. */
  returnValue: number
  isApproved: boolean
}

export interface ReturnDetail {
  id: string
  returnNumber: string
  companyId: string
  customerId: string
  shipmentId?: string | null
  salesOrderId?: string | null
  returnDate: string
  reasonCode?: string | null
  note?: string | null
  status: ReturnStatus
  isApproved: boolean
  rejectionReason?: string | null
  returnValue: number
  lines: ReturnLineSummary[]
}

export interface CreateReturnLineRequest {
  lineNumber: number
  itemId: string
  description: string
  quantity: number
  unitPrice: number
  unitOfMeasure: string
  warehouseId?: string | null
  shipmentLineId?: string | null
  salesOrderLineId?: string | null
  accountId?: string | null
  discountPercent: number
  taxPercent: number
  restockDisposition?: string | null
}

export interface CreateReturnRequest {
  returnNumber: string
  companyId: string
  customerId: string
  shipmentId?: string | null
  salesOrderId?: string | null
  returnDate: string
  reasonCode?: string | null
  note?: string | null
  lines: CreateReturnLineRequest[]
}

export interface UpdateSalesOrderLineRequest {
  quantity: number
  unitPrice: number
  discountPercent: number
  taxPercent: number
  warehouseId?: string | null
  projectId?: string | null
  accountId?: string | null
  itemCategoryId?: string | null
  description?: string | null
}

// ---- Phase 8 masters & pricing/tax ----
export type PricingRuleScope = 'Standard' | 'CustomerSpecific' | 'QuantityBreak' | 'Promotional'
export type SalesOrderTypeCode = 'Quote' | 'Order' | 'Return' | 'CreditOnly'

export interface PricingResult {
  unitPrice: number
  discountPercent: number
  appliedRuleId?: string | null
  appliedScope?: string | null
}

export interface EvaluatePriceRequest {
  companyId: string
  baseUnitPrice: number
  customerId?: string | null
  itemId?: string | null
  itemCategoryId?: string | null
  quantity: number
  asOf: string
}

export interface ShippingMethodSummary {
  id: string
  code: string
  description: string
  carrier?: string | null
  baseCost: number
  isActive: boolean
}
export interface SalesRepSummary {
  id: string
  code: string
  name: string
  commissionRate: number
  territoryId?: string | null
  email?: string | null
  isActive: boolean
}
export interface SalesTerritorySummary {
  id: string
  code: string
  name: string
  region?: string | null
  defaultCommissionRate: number
  isActive: boolean
}
export interface SalesOrderTypeSummary {
  id: string
  code: string
  description: string
  typeCode: SalesOrderTypeCode
  revenueAccountId?: string | null
  isActive: boolean
}
export interface PricingRuleSummary {
  id: string
  code: string
  description: string
  scope: PricingRuleScope
  prioritySequence: number
  discountPercent: number
  unitPriceOverride?: number | null
  customerId?: string | null
  itemId?: string | null
  itemCategoryId?: string | null
  minimumQuantity?: number | null
  isActive: boolean
}
export interface TaxCodeSummary {
  id: string
  code: string
  description: string
  jurisdiction: string
  rate: number
  isTaxable: boolean
  isActive: boolean
}

// ---- Masters CRUD request types ----
export type SalesOrderTypeCodeValue = 'Quote' | 'Order' | 'Return' | 'CreditOnly'
export type PricingRuleScopeValue = 'Standard' | 'CustomerSpecific' | 'QuantityBreak' | 'Promotional'

export interface CreateShippingMethodRequest {
  companyId: string
  code: string
  description: string
  carrier?: string | null
  baseCost: number
  trackingUrlTemplate?: string | null
}
export interface UpdateShippingMethodRequest {
  description: string
  carrier?: string | null
  baseCost: number
  isActive: boolean
  trackingUrlTemplate?: string | null
}
export interface CreateSalesRepRequest {
  companyId: string
  code: string
  name: string
  commissionRate: number
  territoryId?: string | null
  email?: string | null
}
export interface UpdateSalesRepRequest {
  name: string
  commissionRate: number
  territoryId?: string | null
  isActive: boolean
  email?: string | null
}
export interface CreateSalesTerritoryRequest {
  companyId: string
  code: string
  name: string
  region?: string | null
  defaultCommissionRate: number
}
export interface UpdateSalesTerritoryRequest {
  name: string
  region?: string | null
  defaultCommissionRate: number
  isActive: boolean
}
export interface CreateSalesOrderTypeRequest {
  companyId: string
  code: string
  description: string
  typeCode: SalesOrderTypeCodeValue
  revenueAccountId?: string | null
}
export interface UpdateSalesOrderTypeRequest {
  description: string
  typeCode: SalesOrderTypeCodeValue
  revenueAccountId?: string | null
  isActive: boolean
}
export interface CreatePricingRuleRequest {
  companyId: string
  code: string
  description: string
  scope: PricingRuleScopeValue
  prioritySequence: number
  discountPercent: number
  unitPriceOverride?: number | null
  customerId?: string | null
  itemId?: string | null
  itemCategoryId?: string | null
  minimumQuantity?: number | null
  effectiveFrom?: string | null
  effectiveTo?: string | null
}
export interface UpdatePricingRuleRequest {
  description: string
  prioritySequence: number
  discountPercent: number
  unitPriceOverride?: number | null
  itemCategoryId?: string | null
  minimumQuantity?: number | null
  effectiveFrom?: string | null
  effectiveTo?: string | null
  isActive: boolean
}
export interface CreateTaxCodeRequest {
  companyId: string
  code: string
  description: string
  jurisdiction: string
  rate: number
  isTaxable: boolean
  effectiveFrom?: string | null
  effectiveTo?: string | null
}
export interface UpdateTaxCodeRequest {
  description: string
  rate: number
  isTaxable: boolean
  effectiveFrom?: string | null
  effectiveTo?: string | null
  isActive: boolean
}

// ---- Phase 8 sales reports ----
export interface OpenOrderRow {
  orderId: string
  orderNumber: string
  customerId?: string | null
  orderDate: string
  status: string
  orderedQty: number
  backorderedQty: number
  orderValue: number
  salesRepId?: string | null
  isOnCreditHold: boolean
}
export interface BackorderRow {
  lineId: string
  orderId: string
  orderNumber: string
  customerId?: string | null
  itemId: string
  warehouseId?: string | null
  orderedQty: number
  shippedQty: number
  backorderedQty: number
  unitPrice: number
}
export interface ShipmentRegisterRow {
  shipmentId: string
  shipmentNumber: string
  salesOrderId?: string | null
  customerId?: string | null
  shipDate: string
  status: string
  freightCost: number
  shipmentValue: number
}
export interface SalesAnalysisRow {
  itemId: string
  customerId?: string | null
  quantity: number
  shippedQuantity: number
  netSales: number
  taxAmount: number
}
export interface CreditHoldRow {
  orderId: string
  orderNumber: string
  customerId?: string | null
  reason: string
  orderDate: string
  status: string
}
export interface DropShipStatusRow {
  lineId: string
  orderId: string
  orderNumber: string
  customerId?: string | null
  itemId: string
  dropShipVendorId?: string | null
  orderedQty: number
  shippedQty: number
  backorderedQty: number
}
export interface SalesTaxRow {
  taxPercent: number
  quantity: number
  taxableAmount: number
  taxAmount: number
}

// Additional sales reports
export interface SalesTrendRow {
  year: number
  month: number
  quantity: number
  netSales: number
  taxAmount: number
}
export interface CustomerOrderHistoryRow {
  orderId: string
  orderNumber: string
  orderDate: string
  status: string
  orderValue: number
  shippedQty: number
  orderedQty: number
  remainingQty: number
}
export interface ShippingLogRow {
  shipmentId: string
  shipmentNumber: string
  salesOrderId?: string | null
  customerId?: string | null
  shipDate: string
  carrier: string
  trackingNumber: string
  freightCost: number
  shipmentValue: number
}
export interface FreightAnalysisRow {
  carrier: string
  shipmentCount: number
  freightCost: number
  goodsValue: number
}

// Tax exemption certificate
export interface TaxExemptionCertificateSummary {
  id: string
  companyId: string
  certificateNumber: string
  customerId?: string | null
  jurisdiction: string
  validFrom: string
  validTo: string
  exemptItemsDescription?: string | null
  notes?: string | null
  isActive: boolean
}

// Fulfillment documents
export interface PickListLine {
  itemId: string
  description: string
  quantity: number
  unitOfMeasure: string
  warehouseId?: string | null
  remainingToPick: number
}
export interface PickList {
  orderId: string
  orderNumber: string
  companyId: string
  customerId?: string | null
  status: string
  orderDate: string
  lines: PickListLine[]
}
export interface PackingSlipLine {
  itemId: string
  description: string
  quantity: number
  unitOfMeasure: string
}
export interface PackingSlip {
  shipmentId: string
  shipmentNumber: string
  companyId: string
  customerId?: string | null
  salesOrderId?: string | null
  shipmentDate: string
  carrier?: string | null
  trackingNumber?: string | null
  lines: PackingSlipLine[]
}

// ----- Phase 8 gap feature types -------------------------------------------

export type QuoteStatus = 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Converted'
export type BlanketOrderStatus = 'Open' | 'Closed' | 'Expired'
export type SubstitutionOfferStatus = 'Offered' | 'Accepted' | 'Rejected'

export interface ConfigureQuoteRequest {
  expiryDate?: string | null
}
export interface ConvertQuoteRequest {
  newOrderNumber: string
}
export interface CreateBlanketOrderRequest {
  orderNumber: string
  companyId: string
  customerId: string
  orderDate: string
  totalQuantity: number
  totalValue: number
  validFrom: string
  validTo: string
  currency?: string | null
}
export interface AddReleaseRequest {
  quantity: number
  value: number
  releaseDate: string
  reference?: string | null
}
export interface BlanketOrderSummary {
  id: string
  orderNumber: string
  companyId: string
  customerId: string
  totalQuantity: number
  totalValue: number
  releasedQuantity: number
  remainingQuantity: number
  validFrom: string
  validTo: string
  status: BlanketOrderStatus
}
export interface CreateSubstitutionOfferRequest {
  companyId: string
  salesOrderId: string
  salesOrderLineId: string
  originalItemId: string
  substituteItemId: string
  quantity: number
  approvedUnitPrice: number
  reason?: string | null
}
export interface SubstitutionOfferSummary {
  id: string
  salesOrderId: string
  originalItemId: string
  substituteItemId: string
  quantity: number
  approvedUnitPrice: number
  status: SubstitutionOfferStatus
}
export interface CreateRtvRequest {
  companyId: string
  returnLineId: string
  vendorId: string
  quantity: number
  unitCost: number
  reference?: string | null
}
export interface AddNoteRequest {
  companyId: string
  text: string
  isCustomerFacing: boolean
  noteType: string
  attachmentLink?: string | null
}
export interface SalesOrderNoteSummary {
  id: string
  salesOrderId: string
  text: string
  isCustomerFacing: boolean
  noteType: string
  attachmentLink?: string | null
  createdBy?: string | null
  createdOn: string
}
export interface RecordHistoryRequest {
  companyId: string
  changeType: string
  fieldName?: string | null
  oldValue?: string | null
  newValue?: string | null
  reasonCode?: string | null
}
export interface SalesOrderChangeHistorySummary {
  id: string
  changeType: string
  fieldName?: string | null
  oldValue?: string | null
  newValue?: string | null
  reasonCode?: string | null
  changedBy?: string | null
  changeDate: string
}
export interface AcknowledgmentLine {
  itemId: string
  description: string
  quantity: number
  unitPrice: number
  unitOfMeasure: string
}
export interface AcknowledgmentDocument {
  orderNumber: string
  customerId: string
  orderDate: string
  lines: AcknowledgmentLine[]
}
export interface OrderStatusRow {
  status: string
  orderCount: number
  remainingToShip: number
}
export interface AtpResult {
  itemId: string
  warehouseId: string
  requestedQuantity: number
  available: number
  isSufficient: boolean
  promisedDate: string
}
