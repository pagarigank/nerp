// Inventory module API client (Phase 7).
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

export function companyId(): string {
  return useAuthStore.getState().currentCompany?.id ?? DEMO_COMPANY_ID
}

// --- Items ---
export function getItems(companyIdParam?: string): Promise<import('@/types/inventory').ItemSummary[]> {
  return get('/inventory/items', { companyId: companyIdParam ?? companyId() })
}
export function createItem(body: import('@/types/inventory').CreateItemRequest): Promise<string> {
  return post('/inventory/items', body)
}
export function updateItem(id: string, body: import('@/types/inventory').UpdateItemRequest): Promise<string> {
  return put(`/inventory/items/${id}`, body)
}
export function deleteItem(id: string): Promise<string> {
  return del(`/inventory/items/${id}`)
}

// --- Item categories ---
export function getItemCategories(companyIdParam?: string): Promise<import('@/types/inventory').ItemCategorySummary[]> {
  return get('/inventory/item-categories', { companyId: companyIdParam ?? companyId() })
}
export function createItemCategory(body: import('@/types/inventory').CreateItemCategoryRequest): Promise<string> {
  return post('/inventory/item-categories', body)
}
export function updateItemCategory(id: string, body: import('@/types/inventory').UpdateItemCategoryRequest): Promise<string> {
  return put(`/inventory/item-categories/${id}`, body)
}
export function deleteItemCategory(id: string): Promise<string> {
  return del(`/inventory/item-categories/${id}`)
}

// --- Warehouses (master) ---
export function getWarehouses(companyIdParam?: string): Promise<import('@/types/inventory').WarehouseSummary[]> {
  return get('/inventory/warehouses', { companyId: companyIdParam ?? companyId() })
}
export function createWarehouse(body: import('@/types/inventory').CreateWarehouseRequest): Promise<string> {
  return post('/inventory/warehouses', body)
}
export function updateWarehouse(id: string, body: import('@/types/inventory').UpdateWarehouseRequest): Promise<string> {
  return put(`/inventory/warehouses/${id}`, body)
}
export function toggleWarehouseStatus(id: string): Promise<string> {
  return put(`/inventory/warehouses/${id}/toggle-status`, {})
}
export function deleteWarehouse(id: string): Promise<string> {
  return del(`/inventory/warehouses/${id}`)
}

// --- Warehouse bins ---
export function getWarehouseBins(warehouseId?: string): Promise<import('@/types/inventory').WarehouseBinSummary[]> {
  return get('/inventory/warehouse-bins', { warehouseId })
}
export function createWarehouseBin(body: import('@/types/inventory').CreateWarehouseBinRequest): Promise<string> {
  return post('/inventory/warehouse-bins', body)
}

// --- Item stock ---
export function getItemStock(warehouseId?: string, itemId?: string): Promise<import('@/types/inventory').ItemStockSummary[]> {
  return get('/inventory/item-stock', { warehouseId, itemId })
}

// --- Transactions (receipt/issue/adjustment/transfer) ---
export function getTransactions(companyIdParam?: string, itemId?: string, warehouseId?: string, transactionType?: string): Promise<import('@/types/inventory').InventoryTransactionDto[]> {
  return get('/inventory/transactions', { companyId: companyIdParam ?? companyId(), itemId, warehouseId, transactionType })
}
export function createReceipt(body: Record<string, unknown>): Promise<string> {
  return post('/inventory/transactions/receipt', body)
}
export function createIssue(body: Record<string, unknown>): Promise<string> {
  return post('/inventory/transactions/issue', body)
}
export function createAdjustment(body: Record<string, unknown>): Promise<string> {
  return post('/inventory/transactions/adjustment', body)
}
export function createTransfer(body: Record<string, unknown>): Promise<string> {
  return post('/inventory/transactions/transfer', body)
}

// --- Reservations ---
export function getReservations(companyIdParam?: string): Promise<import('@/types/inventory').ReservationSummary[]> {
  return get('/inventory/reservations', { companyId: companyIdParam ?? companyId() })
}
export function createReservation(body: import('@/types/inventory').CreateReservationRequest): Promise<string> {
  return post('/inventory/reservations', body)
}
export function releaseReservation(id: string): Promise<string> {
  return post(`/inventory/reservations/${id}/release`, {})
}
export function cancelReservation(id: string): Promise<string> {
  return post(`/inventory/reservations/${id}/cancel`, {})
}

// --- Quarantine ---
export function getQuarantine(companyIdParam?: string): Promise<import('@/types/inventory').QuarantineSummary[]> {
  return get('/inventory/quarantine', { companyId: companyIdParam ?? companyId() })
}
export function createQuarantine(body: import('@/types/inventory').CreateQuarantineRequest): Promise<string> {
  return post('/inventory/quarantine', body)
}
export function releaseQuarantine(id: string): Promise<string> {
  return post(`/inventory/quarantine/${id}/release`, {})
}
export function disposeQuarantine(id: string): Promise<string> {
  return post(`/inventory/quarantine/${id}/mark-disposed`, {})
}

// --- Expiration ---
export function getExpirations(companyIdParam?: string): Promise<import('@/types/inventory').ExpirationSummary[]> {
  return get('/inventory/expirations', { companyId: companyIdParam ?? companyId() })
}
export function createExpiration(body: import('@/types/inventory').CreateExpirationRequest): Promise<string> {
  return post('/inventory/expirations', body)
}
export function getExpiringSoon(): Promise<import('@/types/inventory').ExpirationSummary[]> {
  return get('/inventory/expirations/expiring-soon', { companyId: companyId() })
}
export function getExpired(): Promise<import('@/types/inventory').ExpirationSummary[]> {
  return get('/inventory/expirations/expired', { companyId: companyId() })
}

// --- Revaluation ---
export function getRevaluations(companyIdParam?: string): Promise<import('@/types/inventory').RevaluationSummary[]> {
  return get('/inventory/revaluations', { companyId: companyIdParam ?? companyId() })
}
export function createRevaluation(body: import('@/types/inventory').CreateRevaluationRequest): Promise<string> {
  return post('/inventory/revaluations', body)
}
export function approveRevaluation(id: string): Promise<string> {
  return post(`/inventory/revaluations/${id}/approve`, {})
}
export function postRevaluation(id: string): Promise<string> {
  return post(`/inventory/revaluations/${id}/post`, {})
}

// --- Landed cost ---
export function getLandedCosts(companyIdParam?: string): Promise<import('@/types/inventory').LandedCostSummary[]> {
  return get('/inventory/landed-costs', { companyId: companyIdParam ?? companyId() })
}
export function createLandedCost(body: import('@/types/inventory').CreateLandedCostRequest): Promise<string> {
  return post('/inventory/landed-costs', body)
}

// --- Cycle counts ---
export function getCycleCounts(companyIdParam?: string): Promise<import('@/types/inventory').CycleCountSummary[]> {
  return get('/inventory/cycle-counts', { companyId: companyIdParam ?? companyId() })
}
export function getCycleCount(id: string): Promise<import('@/types/inventory').CycleCountSummary> {
  return get(`/inventory/cycle-counts/${id}`)
}
export function createCycleCount(body: import('@/types/inventory').CreateCycleCountRequest): Promise<string> {
  return post('/inventory/cycle-counts', body)
}
export function startCycleCount(id: string): Promise<string> {
  return post(`/inventory/cycle-counts/${id}/start`, {})
}
export function completeCycleCount(id: string): Promise<string> {
  return post(`/inventory/cycle-counts/${id}/complete`, {})
}
export function postCycleCount(id: string): Promise<import('@/types/inventory').CycleCountPostResult> {
  return post(`/inventory/cycle-counts/${id}/post`, {})
}
export function cancelCycleCount(id: string): Promise<string> {
  return post(`/inventory/cycle-counts/${id}/cancel`, {})
}
export function updateCycleCountLine(id: string, lineId: string, body: import('@/types/inventory').UpdateCycleCountLineRequest): Promise<string> {
  return put(`/inventory/cycle-counts/${id}/lines/${lineId}`, body)
}

// --- Physical counts ---
export function getPhysicalCounts(companyIdParam?: string): Promise<import('@/types/inventory').PhysicalCountSummary[]> {
  return get('/inventory/physical-counts', { companyId: companyIdParam ?? companyId() })
}
export function getPhysicalCount(id: string): Promise<import('@/types/inventory').PhysicalCountSummary> {
  return get(`/inventory/physical-counts/${id}`)
}
export function createPhysicalCount(body: import('@/types/inventory').CreatePhysicalCountRequest): Promise<string> {
  return post('/inventory/physical-counts', body)
}
export function startPhysicalCount(id: string): Promise<string> {
  return post(`/inventory/physical-counts/${id}/start`, {})
}
export function completePhysicalCount(id: string): Promise<string> {
  return post(`/inventory/physical-counts/${id}/complete`, {})
}
export function postPhysicalCount(id: string): Promise<import('@/types/inventory').PhysicalCountPostResult> {
  return post(`/inventory/physical-counts/${id}/post`, {})
}
export function cancelPhysicalCount(id: string): Promise<string> {
  return post(`/inventory/physical-counts/${id}/cancel`, {})
}
export function updatePhysicalCountLine(id: string, lineId: string, body: import('@/types/inventory').UpdatePhysicalCountLineRequest): Promise<string> {
  return put(`/inventory/physical-counts/${id}/lines/${lineId}`, body)
}
export function generateCountSheet(warehouseId: string, abcClass?: string): Promise<import('@/types/inventory').CountSheet> {
  return post('/inventory/physical-counts/generate-count-sheet', { warehouseId, abcClass })
}

// --- Negative inventory overrides ---
export function getNegativeOverrides(companyIdParam?: string): Promise<import('@/types/inventory').NegativeOverrideSummary[]> {
  return get('/inventory/negative-inventory-overrides', { companyId: companyIdParam ?? companyId() })
}
export function createNegativeOverride(body: import('@/types/inventory').CreateNegativeOverrideRequest): Promise<string> {
  return post('/inventory/negative-inventory-overrides', body)
}
export function approveNegativeOverride(id: string): Promise<string> {
  return post(`/inventory/negative-inventory-overrides/${id}/approve`, {})
}
export function rejectNegativeOverride(id: string): Promise<string> {
  return post(`/inventory/negative-inventory-overrides/${id}/reject`, {})
}

// --- Movements / history ---
export function getMovements(companyIdParam?: string, itemId?: string, warehouseId?: string, transactionType?: string, startDate?: string, endDate?: string): Promise<import('@/types/inventory').MovementSummary[]> {
  return get<{ data: import('@/types/inventory').MovementSummary[] }>('/inventory/movements', { companyId: companyIdParam ?? companyId(), itemId, warehouseId, transactionType, startDate, endDate }).then(r => r.data)
}

// --- Reorder suggestions ---
export function getReorderSuggestions(companyIdParam?: string): Promise<import('@/types/inventory').ReorderSuggestionSummary[]> {
  return get('/inventory/reorder-suggestions', { companyId: companyIdParam ?? companyId() })
}
export function createReorderSuggestion(body: Record<string, unknown>): Promise<string> {
  return post('/inventory/reorder-suggestions', body)
}
export function approveReorderSuggestion(id: string): Promise<string> {
  return post(`/inventory/reorder-suggestions/${id}/approve`, {})
}
export function convertReorderSuggestion(id: string): Promise<string> {
  return post(`/inventory/reorder-suggestions/${id}/convert-to-po`, {})
}

// --- Reports --- (kept at bottom to preserve prior function names)
export function getValuationReport(warehouseId?: string, itemId?: string): Promise<import('@/types/inventory').InventoryValuationRow[]> {
  return get('/inventory/reports/valuation', { companyId: companyId(), warehouseId, itemId })
}
export function getReorderReport(): Promise<import('@/types/inventory').ReorderReportRow[]> {
  return get('/inventory/reports/reorder', { companyId: companyId() })
}
export function getTransactionHistory(from?: string, to?: string, itemId?: string, warehouseId?: string): Promise<import('@/types/inventory').TransactionHistoryRow[]> {
  return get('/inventory/reports/transactions', { companyId: companyId(), from, to, itemId, warehouseId })
}
export function getStockOutReport(): Promise<import('@/types/inventory').StockOutRow[]> {
  return get('/inventory/reports/stock-out', { companyId: companyId() })
}
export function getNegativeReport(): Promise<import('@/types/inventory').NegativeInventoryRow[]> {
  return get('/inventory/reports/negative', { companyId: companyId() })
}
export function getSlowMovingReport(monthsThreshold = 12): Promise<import('@/types/inventory').SlowMovingRow[]> {
  return get('/inventory/reports/slow-moving', { companyId: companyId(), monthsThreshold })
}
export function getAbcAnalysis(): Promise<import('@/types/inventory').AbcAnalysisRow[]> {
  return get('/inventory/reports/abc-analysis', { companyId: companyId() })
}
export function getLotTraceability(itemId?: string): Promise<import('@/types/inventory').LotTraceabilityRow[]> {
  return get('/inventory/reports/lot-traceability', { companyId: companyId(), itemId })
}
export function getSerialTraceability(itemId?: string): Promise<import('@/types/inventory').SerialTraceabilityRow[]> {
  return get('/inventory/reports/serial-traceability', { companyId: companyId(), itemId })
}
export function getInventoryTurnover(from: string, to: string): Promise<import('@/types/inventory').InventoryTurnoverRow[]> {
  return get('/inventory/reports/inventory-turnover', { companyId: companyId(), from, to })
}
export function getCycleCountVariance(): Promise<import('@/types/inventory').CycleCountVarianceRow[]> {
  return get('/inventory/reports/cycle-count-variance', { companyId: companyId() })
}
export function getCycleCountSummary(): Promise<import('@/types/inventory').CycleCountSummaryRow[]> {
  return get('/inventory/reports/cycle-count-summary', { companyId: companyId() })
}
export function getStockCard(itemId: string, warehouseId?: string, from?: string, to?: string): Promise<import('@/types/inventory').StockCardRow[]> {
  return get('/inventory/reports/stock-card', { companyId: companyId(), itemId, warehouseId, from, to })
}

// --- Item vendor assignments ---
export function getItemVendors(itemId: string): Promise<import('@/types/inventory').ItemVendorAssignmentDto[]> {
  return get(`/inventory/items/${itemId}/vendors`)
}
export function createItemVendor(itemId: string, body: Record<string, unknown>): Promise<string> {
  return post(`/inventory/items/${itemId}/vendors`, body)
}
export function updateItemVendor(itemId: string, id: string, body: Record<string, unknown>): Promise<string> {
  return put(`/inventory/items/${itemId}/vendors/${id}`, body)
}
export function deleteItemVendor(itemId: string, id: string): Promise<string> {
  return del(`/inventory/items/${itemId}/vendors/${id}`)
}

// --- Item GL account defaults ---
export function getItemGlAccounts(itemId: string): Promise<import('@/types/inventory').ItemGlAccountDefaultsDto | null> {
  return get(`/inventory/items/${itemId}/gl-accounts`)
}
export function upsertItemGlAccounts(itemId: string, body: Record<string, unknown>): Promise<string> {
  return post(`/inventory/items/${itemId}/gl-accounts`, body)
}

// --- Item UOM Conversions ---
export function getItemUomConversions(itemId: string): Promise<import('@/types/inventory').UomConversionDto[]> {
  return get(`/inventory/items/${itemId}/uom-conversions`)
}
export function createItemUomConversion(itemId: string, body: { fromUOM: string; toUOM: string; conversionFactor: number }): Promise<import('@/types/inventory').UomConversionDto> {
  return post(`/inventory/items/${itemId}/uom-conversions`, body)
}
export function updateItemUomConversion(itemId: string, id: string, body: { conversionFactor: number }): Promise<string> {
  return put(`/inventory/items/${itemId}/uom-conversions/${id}`, body)
}
export function deleteItemUomConversion(itemId: string, id: string): Promise<string> {
  return del(`/inventory/items/${itemId}/uom-conversions/${id}`)
}
export function convertUom(itemId: string, body: { fromUOM: string; toUOM: string; quantity: number }): Promise<import('@/types/inventory').UomConvertResult> {
  return post(`/inventory/items/${itemId}/uom-conversions/convert`, body)
}

// --- Landed Cost Allocations ---
export function getLandedCostAllocations(companyIdParam?: string): Promise<import('@/types/inventory').LandedCostAllocationDto[]> {
  return get('/inventory/landed-cost-allocations', { companyId: companyIdParam ?? companyId() })
}
export function createLandedCostAllocation(data: import('@/types/inventory').CreateLandedCostAllocationRequest): Promise<import('@/types/inventory').LandedCostAllocationDto> {
  return post('/inventory/landed-cost-allocations', data)
}
export function autoAllocateLandedCost(id: string): Promise<import('@/types/inventory').LandedCostAllocationDto> {
  return post(`/inventory/landed-cost-allocations/${id}/auto-allocate`, {})
}
export function postLandedCostAllocation(id: string): Promise<import('@/types/inventory').LandedCostAllocationDto> {
  return post(`/inventory/landed-cost-allocations/${id}/post`, {})
}
export function cancelLandedCostAllocation(id: string): Promise<import('@/types/inventory').LandedCostAllocationDto> {
  return post(`/inventory/landed-cost-allocations/${id}/cancel`, {})
}

// --- Item Substitutions (459) ---
export function getItemSubstitutions(companyIdParam?: string): Promise<import('@/types/inventory').ItemSubstitutionDto[]> {
  return get('/inventory/item-substitutions', { companyId: companyIdParam ?? companyId() })
}
export function createItemSubstitution(body: import('@/types/inventory').CreateItemSubstitutionRequest): Promise<string> {
  return post('/inventory/item-substitutions', body)
}
export function approveItemSubstitution(id: string, approvedBy: string): Promise<import('@/types/inventory').ItemSubstitutionDto> {
  return post(`/inventory/item-substitutions/${id}/approve`, { approvedBy })
}
export function rejectItemSubstitution(id: string, rejectedBy: string, reason: string): Promise<import('@/types/inventory').ItemSubstitutionDto> {
  return post(`/inventory/item-substitutions/${id}/reject`, { rejectedBy, reason })
}

// --- Kit / Bundled Items (460) ---
export function getKitComponents(companyIdParam?: string): Promise<import('@/types/inventory').KitComponentDto[]> {
  return get('/inventory/kit-components', { companyId: companyIdParam ?? companyId() })
}
export function createKitComponent(body: import('@/types/inventory').CreateKitComponentRequest): Promise<string> {
  return post('/inventory/kit-components', body)
}
export function receiveKit(body: Record<string, unknown>): Promise<string> {
  return post('/inventory/kit-components/receipt', body)
}
export function issueKit(body: Record<string, unknown>): Promise<string> {
  return post('/inventory/kit-components/issue', body)
}

// --- Put-away / Picking Rules (463) ---
export function getPutAwayPickingRules(companyIdParam?: string): Promise<import('@/types/inventory').PutAwayPickingRuleDto[]> {
  return get('/inventory/put-away-picking-rules', { companyId: companyIdParam ?? companyId() })
}
export function createPutAwayPickingRule(body: import('@/types/inventory').CreatePutAwayPickingRuleRequest): Promise<string> {
  return post('/inventory/put-away-picking-rules', body)
}
export function recommendPutAway(binId: string, mode: 'putaway' | 'pick'): Promise<import('@/types/inventory').PutAwayPickingRecommendationDto> {
  return get('/inventory/put-away-picking-rules/recommend', { companyId: companyId(), warehouseId: binId, mode })
}

// --- Consignment Stock (458) ---
export function getConsignmentStock(companyIdParam?: string): Promise<import('@/types/inventory').ConsignmentStockDto[]> {
  return get('/inventory/consignment-stock', { companyId: companyIdParam ?? companyId() })
}
export function createConsignmentStock(body: import('@/types/inventory').CreateConsignmentStockRequest): Promise<string> {
  return post('/inventory/consignment-stock', body)
}
export function receiveConsignment(id: string, quantity: number): Promise<import('@/types/inventory').ConsignmentStockDto> {
  return post(`/inventory/consignment-stock/${id}/receive`, { quantity })
}
export function consumeConsignment(id: string, quantity: number, unitCost: number): Promise<import('@/types/inventory').ConsignmentStockDto> {
  return post(`/inventory/consignment-stock/${id}/consume`, { quantity, unitCost })
}

// --- Inventory GL Tie-Out (464) ---
export function getGlTieOut(): Promise<import('@/types/inventory').InventoryGlTieOutRow[]> {
  return get('/inventory/reports/gl-tie-out', { companyId: companyId() })
}

// --- Cycle Count Scheduling (465) ---
export function scheduleCycleCount(body: import('@/types/inventory').ScheduleCycleCountRequest): Promise<import('@/types/inventory').CycleCountSummary[]> {
  return post('/inventory/cycle-counts/schedule', body)
}

// --- Scrap / Disposal (466) ---
export function createScrap(body: Record<string, unknown>): Promise<string> {
  return post('/inventory/transactions/scrap', body)
}

// --- Stock by location / lot (462) ---
export function getItemStockByLocation(warehouseId?: string): Promise<import('@/types/inventory').ItemStockSummary[]> {
  return get('/inventory/item-stock', { companyId: companyId(), warehouseId })
}
