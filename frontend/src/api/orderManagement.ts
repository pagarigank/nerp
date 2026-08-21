import type {
  CreateReturnRequest,
  CreateSalesOrderRequest,
  CreateShipmentRequest,
  ReturnDetail,
  ReturnSummary,
  SalesOrderDetail,
  SalesOrderSummary,
  ShipmentDetail,
  ShipmentSummary,
} from '@/types/orderManagement'
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

export function companyId(): string {
  const current = useAuthStore.getState().currentCompany
  // Empty id is the "All Companies" sentinel for super admins: omit the
  // companyId query param so the backend returns data across every company.
  return current?.id ?? ''
}

// Sales Orders
export function getSalesOrders(companyIdParam?: string): Promise<SalesOrderSummary[]> {
  return get('/om/sales-orders', { companyId: companyIdParam ?? companyId() })
}

export function getSalesOrder(id: string): Promise<SalesOrderDetail> {
  return get(`/om/sales-orders/${id}`)
}

export function createSalesOrder(data: CreateSalesOrderRequest): Promise<string> {
  return post('/om/sales-orders', data)
}

export function confirmSalesOrder(id: string): Promise<string> {
  return post(`/om/sales-orders/${id}/confirm`)
}

export function cancelSalesOrder(id: string): Promise<string> {
  return post(`/om/sales-orders/${id}/cancel`)
}

export function placeCreditHold(id: string, reason: string): Promise<string> {
  return post(`/om/sales-orders/${id}/credit-hold`, { reason })
}

export function releaseCreditHold(id: string): Promise<string> {
  return post(`/om/sales-orders/${id}/release-hold`)
}

// Shipments
export function getShipments(companyIdParam?: string): Promise<ShipmentSummary[]> {
  return get('/om/shipments', { companyId: companyIdParam ?? companyId() })
}

export function getShipment(id: string): Promise<ShipmentDetail> {
  return get(`/om/shipments/${id}`)
}

export function createShipment(data: CreateShipmentRequest): Promise<string> {
  return post('/om/shipments', data)
}

export function confirmShipment(id: string): Promise<string> {
  return post(`/om/shipments/${id}/confirm`)
}

// Returns (RMA)
export function getReturns(companyIdParam?: string): Promise<ReturnSummary[]> {
  return get('/om/returns', { companyId: companyIdParam ?? companyId() })
}

export function getReturn(id: string): Promise<ReturnDetail> {
  return get(`/om/returns/${id}`)
}

export function createReturn(data: CreateReturnRequest): Promise<string> {
  return post('/om/returns', data)
}

export function confirmReturn(id: string): Promise<string> {
  return post(`/om/returns/${id}/confirm`)
}

// Reference masters
export function getShippingMethods(companyIdParam?: string): Promise<import('@/types/orderManagement').ShippingMethodSummary[]> {
  return get('/om/shipping-methods', { companyId: companyIdParam ?? companyId() })
}
export function getSalesReps(companyIdParam?: string): Promise<import('@/types/orderManagement').SalesRepSummary[]> {
  return get('/om/sales-reps', { companyId: companyIdParam ?? companyId() })
}
export function getSalesTerritories(companyIdParam?: string): Promise<import('@/types/orderManagement').SalesTerritorySummary[]> {
  return get('/om/sales-territories', { companyId: companyIdParam ?? companyId() })
}
export function getSalesOrderTypes(companyIdParam?: string): Promise<import('@/types/orderManagement').SalesOrderTypeSummary[]> {
  return get('/om/sales-order-types', { companyId: companyIdParam ?? companyId() })
}
export function getPricingRules(companyIdParam?: string): Promise<import('@/types/orderManagement').PricingRuleSummary[]> {
  return get('/om/pricing-rules', { companyId: companyIdParam ?? companyId() })
}
export function getTaxCodes(companyIdParam?: string): Promise<import('@/types/orderManagement').TaxCodeSummary[]> {
  return get('/om/tax-codes', { companyId: companyIdParam ?? companyId() })
}

// Reference masters — create / update / delete (full CRUD)
export function createShippingMethod(data: import('@/types/orderManagement').CreateShippingMethodRequest): Promise<string> {
  return post('/om/shipping-methods', data)
}
export function updateShippingMethod(id: string, data: import('@/types/orderManagement').UpdateShippingMethodRequest): Promise<string> {
  return put(`/om/shipping-methods/${id}`, data)
}
export function deleteShippingMethod(id: string): Promise<string> {
  return del(`/om/shipping-methods/${id}`)
}

export function createSalesRep(data: import('@/types/orderManagement').CreateSalesRepRequest): Promise<string> {
  return post('/om/sales-reps', data)
}
export function updateSalesRep(id: string, data: import('@/types/orderManagement').UpdateSalesRepRequest): Promise<string> {
  return put(`/om/sales-reps/${id}`, data)
}
export function deleteSalesRep(id: string): Promise<string> {
  return del(`/om/sales-reps/${id}`)
}

export function createSalesTerritory(data: import('@/types/orderManagement').CreateSalesTerritoryRequest): Promise<string> {
  return post('/om/sales-territories', data)
}
export function updateSalesTerritory(id: string, data: import('@/types/orderManagement').UpdateSalesTerritoryRequest): Promise<string> {
  return put(`/om/sales-territories/${id}`, data)
}
export function deleteSalesTerritory(id: string): Promise<string> {
  return del(`/om/sales-territories/${id}`)
}

export function createSalesOrderType(data: import('@/types/orderManagement').CreateSalesOrderTypeRequest): Promise<string> {
  return post('/om/sales-order-types', data)
}
export function updateSalesOrderType(id: string, data: import('@/types/orderManagement').UpdateSalesOrderTypeRequest): Promise<string> {
  return put(`/om/sales-order-types/${id}`, data)
}
export function deleteSalesOrderType(id: string): Promise<string> {
  return del(`/om/sales-order-types/${id}`)
}

export function createPricingRule(data: import('@/types/orderManagement').CreatePricingRuleRequest): Promise<string> {
  return post('/om/pricing-rules', data)
}
export function updatePricingRule(id: string, data: import('@/types/orderManagement').UpdatePricingRuleRequest): Promise<string> {
  return put(`/om/pricing-rules/${id}`, data)
}
export function deletePricingRule(id: string): Promise<string> {
  return del(`/om/pricing-rules/${id}`)
}
export function evaluatePrice(data: import('@/types/orderManagement').EvaluatePriceRequest): Promise<import('@/types/orderManagement').PricingResult> {
  return post('/om/pricing-rules/evaluate', data)
}

export function createTaxCode(data: import('@/types/orderManagement').CreateTaxCodeRequest): Promise<string> {
  return post('/om/tax-codes', data)
}
export function updateTaxCode(id: string, data: import('@/types/orderManagement').UpdateTaxCodeRequest): Promise<string> {
  return put(`/om/tax-codes/${id}`, data)
}
export function deleteTaxCode(id: string): Promise<string> {
  return del(`/om/tax-codes/${id}`)
}

// Sales reports
export function getOpenOrdersReport(): Promise<import('@/types/orderManagement').OpenOrderRow[]> {
  return get('/om/reports/open-orders', { companyId: companyId() })
}
export function getBackordersReport(): Promise<import('@/types/orderManagement').BackorderRow[]> {
  return get('/om/reports/backorders', { companyId: companyId() })
}
export function getShipmentRegisterReport(from?: string, to?: string): Promise<import('@/types/orderManagement').ShipmentRegisterRow[]> {
  return get('/om/reports/shipment-register', { companyId: companyId(), from, to })
}
export function getSalesAnalysisReport(from?: string, to?: string): Promise<import('@/types/orderManagement').SalesAnalysisRow[]> {
  return get('/om/reports/sales-analysis', { companyId: companyId(), from, to })
}
export function getCreditHoldsReport(): Promise<import('@/types/orderManagement').CreditHoldRow[]> {
  return get('/om/reports/credit-holds', { companyId: companyId() })
}
export function getDropShipStatusReport(): Promise<import('@/types/orderManagement').DropShipStatusRow[]> {
  return get('/om/reports/drop-ship-status', { companyId: companyId() })
}
export function getSalesTaxReport(from?: string, to?: string): Promise<import('@/types/orderManagement').SalesTaxRow[]> {
  return get('/om/reports/sales-tax', { companyId: companyId(), from, to })
}

// Additional reports
export function getSalesTrendReport(from?: string, to?: string): Promise<import('@/types/orderManagement').SalesTrendRow[]> {
  return get('/om/reports/sales-trend', { companyId: companyId(), from, to })
}
export function getCustomerOrderHistory(customerId: string): Promise<import('@/types/orderManagement').CustomerOrderHistoryRow[]> {
  return get('/om/reports/customer-order-history', { companyId: companyId(), customerId })
}
export function getShippingLog(from?: string, to?: string): Promise<import('@/types/orderManagement').ShippingLogRow[]> {
  return get('/om/reports/shipping-log', { companyId: companyId(), from, to })
}
export function getFreightAnalysis(from?: string, to?: string): Promise<import('@/types/orderManagement').FreightAnalysisRow[]> {
  return get('/om/reports/freight-analysis', { companyId: companyId(), from, to })
}

// Discount approval
export function approveDiscount(id: string, approvedBy: string): Promise<string> {
  return post(`/om/sales-orders/${id}/discount-approval`, { approvedBy })
}

// Tax exemption certificates
export function getTaxExemptions(companyIdParam?: string, customerId?: string): Promise<import('@/types/orderManagement').TaxExemptionCertificateSummary[]> {
  return get('/om/tax-exemptions', { companyId: companyIdParam ?? companyId(), customerId })
}
export function createTaxExemption(body: Record<string, unknown>): Promise<string> {
  return post('/om/tax-exemptions', body)
}
export function updateTaxExemption(id: string, body: Record<string, unknown>): Promise<string> {
  return put(`/om/tax-exemptions/${id}`, body)
}
export function revokeTaxExemption(id: string): Promise<string> {
  return post(`/om/tax-exemptions/${id}/revoke`)
}
export function deleteTaxExemption(id: string): Promise<string> {
  return del(`/om/tax-exemptions/${id}`)
}

// Fulfillment documents
export function getPickList(orderId: string): Promise<import('@/types/orderManagement').PickList> {
  return get(`/om/fulfillment/pick-list/${orderId}`)
}
export function getPackingSlip(shipmentId: string): Promise<import('@/types/orderManagement').PackingSlip> {
  return get(`/om/fulfillment/packing-slip/${shipmentId}`)
}

// ----- Phase 8 gap features -------------------------------------------------
// 582 Quote-to-order conversion
export function configureQuote(id: string, expiryDate?: string) {
  return post(`/om/sales-orders/${id}/configure-quote`, { expiryDate })
}
export function sendQuote(id: string) {
  return post(`/om/sales-orders/${id}/send-quote`, {})
}
export function acceptQuote(id: string) {
  return post(`/om/sales-orders/${id}/accept-quote`, {})
}
export function rejectQuote(id: string) {
  return post(`/om/sales-orders/${id}/reject-quote`, {})
}
export function reviseQuote(id: string) {
  return post(`/om/sales-orders/${id}/revise-quote`, {})
}
export function convertQuote(id: string, newOrderNumber: string) {
  return post(`/om/sales-orders/${id}/convert-quote`, { newOrderNumber })
}

// 583 Blanket / standing orders
export function createBlanketOrder(req: import('@/types/orderManagement').CreateBlanketOrderRequest) {
  return post('/om/blanket-orders', req)
}
export function getBlanketOrders(companyId?: string) {
  return get(`/om/blanket-orders${companyId ? `?companyId=${companyId}` : ''}`)
}
export function addBlanketRelease(id: string, req: import('@/types/orderManagement').AddReleaseRequest) {
  return post(`/om/blanket-orders/${id}/releases`, req)
}

// 584 Backorder substitution offers
export function createSubstitutionOffer(req: import('@/types/orderManagement').CreateSubstitutionOfferRequest) {
  return post('/om/substitution-offers', req)
}
export function acceptSubstitutionOffer(id: string) {
  return post(`/om/substitution-offers/${id}/accept`, {})
}
export function rejectSubstitutionOffer(id: string, reason?: string) {
  return post(`/om/substitution-offers/${id}/reject`, { reason })
}
export function getSubstitutionOffers(companyId?: string, salesOrderId?: string) {
  const qs: string[] = []
  if (companyId) qs.push(`companyId=${companyId}`)
  if (salesOrderId) qs.push(`salesOrderId=${salesOrderId}`)
  return get(`/om/substitution-offers${qs.length ? `?${qs.join('&')}` : ''}`)
}

// 585 Return-to-vendor
export function createRtv(returnId: string, req: import('@/types/orderManagement').CreateRtvRequest) {
  return post(`/om/returns/${returnId}/rtv`, req)
}
export function shipRtv(id: string) {
  return post(`/om/rtv/${id}/ship`, {})
}
export function creditRtv(id: string) {
  return post(`/om/rtv/${id}/credit`, {})
}

// 589 Order notes + change history
export function addOrderNote(id: string, req: import('@/types/orderManagement').AddNoteRequest) {
  return post(`/om/sales-orders/${id}/notes`, req)
}
export function getOrderNotes(id: string) {
  return get(`/om/sales-orders/${id}/notes`)
}
export function recordOrderHistory(id: string, req: import('@/types/orderManagement').RecordHistoryRequest) {
  return post(`/om/sales-orders/${id}/history`, req)
}
export function getOrderHistory(id: string) {
  return get(`/om/sales-orders/${id}/history`)
}

// 588 Customer acknowledgment document
export function getAcknowledgment(id: string) {
  return get(`/om/sales-orders/${id}/acknowledgment`)
}

// 587 Order-status dashboard
export function getOrderStatusDashboard(companyId?: string) {
  return get(`/om/dashboard/order-status${companyId ? `?companyId=${companyId}` : ''}`)
}

// 578 Freight allocation
export function allocateFreight(id: string, freightAmount: number) {
  return post(`/om/sales-orders/${id}/allocate-freight`, { freightAmount })
}

// 581 Available-to-Promise
export function checkAtp(itemId: string, warehouseId: string, quantity: number) {
  return get(`/om/atp?itemId=${itemId}&warehouseId=${warehouseId}&quantity=${quantity}`)
}

