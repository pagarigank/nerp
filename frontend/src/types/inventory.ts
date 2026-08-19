// Inventory module domain types (Phase 7) — mirrors the InventoryReportService DTOs.

export interface InventoryValuationRow {
  itemId: string
  itemCode: string
  description: string
  warehouseId: string
  onHandQuantity: number
  unitCost: number
  extendedValue: number
  abcClass: string
}

export interface ReorderReportRow {
  itemId: string
  itemCode: string
  description: string
  warehouseId: string
  onHandQuantity: number
  allocatedQuantity: number
  availableQuantity: number
  reorderPoint: number
  safetyStock: number
  suggestedOrderQuantity: number
  preferredVendorId: string
  vendorCost?: number | null
  leadTimeDays: number
}

export interface TransactionHistoryRow {
  id: string
  itemId: string
  warehouseId: string
  transactionType: string
  quantity: number
  unitOfMeasure: string
  unitCost: number
  extendedCost: number
  transactionDate: string
  lotId?: string | null
  serialNumber?: string | null
  referenceNumber?: string | null
  projectId?: string | null
}

export interface StockOutRow {
  itemId: string
  itemCode: string
  description: string
  warehouseId: string
  onHandQuantity: number
  allocatedQuantity: number
  reorderPoint: number
}

export interface NegativeInventoryRow {
  itemId: string
  itemCode: string
  description: string
  warehouseId: string
  onHandQuantity: number
  allocatedQuantity: number
}

export interface SlowMovingRow {
  itemId: string
  itemCode: string
  description: string
  warehouseId: string
  onHandQuantity: number
  unitCost: number
  onHandValue: number
  lastMovementDate?: string | null
  monthsThreshold: number
}

export interface AbcAnalysisRow {
  itemId: string
  itemCode: string
  description: string
  usageValue: number
  percentOfTotal: number
  cumulativePercent: number
  abcClass: string
}

export interface LotTraceabilityRow {
  lotId: string
  lotNumber: string
  itemId: string
  itemCode: string
  description: string
  warehouseId: string
  receivedDate: string
  expirationDate?: string | null
  status: string
  receivedQuantity: number
  issuedQuantity: number
  remainingQuantity: number
}

export interface SerialTraceabilityRow {
  serialId: string
  serialNo: string
  itemId: string
  itemCode: string
  description: string
  warehouseId: string
  receivedDate: string
  status: string
  customerId?: string | null
  installationDate?: string | null
  warrantyInfo?: string | null
}

export interface InventoryTurnoverRow {
  itemId: string
  itemCode: string
  description: string
  cogs: number
  averageInventory: number
  turnover: number
}

export interface CycleCountVarianceRow {
  countId: string
  countNumber: string
  itemId: string
  itemCode: string
  description: string
  warehouseId?: string | null
  binId?: string | null
  systemQuantity: number
  countedQuantity: number
  varianceQuantity: number
  varianceValue: number
  variancePercent: number
  notes?: string | null
}

export interface CycleCountSummaryRow {
  warehouseId: string
  lineCount: number
  totalSystemQuantity: number
  totalCountedQuantity: number
  totalVarianceQuantity: number
  totalVarianceValue: number
}

// --- Items (list + create) ---
export interface ItemSummary {
  id: string
  itemCode: string
  description: string
  longDescription?: string | null
  itemType: string
  baseUnitOfMeasure: string
  costingMethod?: string | null
  status: string
  standardCost?: number | null
  reorderPoint?: number | null
  reorderQuantity?: number | null
  safetyStock?: number | null
  leadTimeDays?: number | null
}

export interface CreateItemRequest {
  itemCode: string
  description: string
  longDescription?: string | null
  companyId: string
  itemType: number
  baseUnitOfMeasure: string
  costingMethod: number
  itemCategoryId: string
  standardCost?: number | null
  reorderPoint?: number | null
  reorderQuantity?: number | null
  safetyStock?: number | null
  leadTimeDays?: number | null
  // Physical attributes (backend-supported)
  weight?: number | null
  length?: number | null
  width?: number | null
  height?: number | null
  weightUnit?: string | null
  isHazardousMaterial?: boolean
  hazardClass?: string | null
  countryOfOrigin?: string | null
  hsCode?: string | null
  storageCondition?: string | null
  isKit?: boolean
}

// --- Warehouse (master) ---
export interface WarehouseSummary {
  id: string
  warehouseCode: string
  warehouseName: string
  companyId: string
  warehouseType: string
  address?: string | null
  isActive: boolean
}

export interface CreateWarehouseRequest {
  warehouseCode: string
  warehouseName: string
  companyId: string
  warehouseType: string
  address?: string | null
}

// --- Item categories ---
export interface ItemCategorySummary {
  id: string
  categoryCode: string
  description: string
  companyId: string
  inventoryAccountId: string
  cogsAccountId: string
  varianceAccountId: string
}

export interface CreateItemCategoryRequest {
  categoryCode: string
  description: string
  companyId: string
  inventoryAccountId?: string | null
  cogsAccountId?: string | null
  varianceAccountId?: string | null
}

// --- Warehouse bins ---
export interface WarehouseBinSummary {
  id: string
  warehouseId: string
  binCode: string
  aisle?: string | null
  rack?: string | null
  shelf?: string | null
  isActive: boolean
}

export interface CreateWarehouseBinRequest {
  warehouseId: string
  binCode: string
  aisle?: string | null
  rack?: string | null
  shelf?: string | null
}

// --- Item stock ---
export interface ItemStockSummary {
  id: string
  itemId: string
  itemCode: string
  itemDescription: string
  warehouseId: string
  warehouseCode: string
  binId?: string | null
  quantityOnHand: number
  quantityAllocated: number
  quantityOnOrder: number
  quantityAvailable: number
  lastTransactionDate?: string | null
}

// --- Reservations ---
export interface ReservationSummary {
  id: string
  companyId: string
  itemId: string
  warehouseId: string
  binId?: string | null
  quantity: number
  unitOfMeasure: string
  sourceType: string
  sourceId: string
  lotNumber?: string | null
  serialNumber?: string | null
  expirationDate?: string | null
  notes?: string | null
  status: string
  releasedQuantity: number
  remainingQuantity: number
}

export interface CreateReservationRequest {
  itemId: string
  warehouseId: string
  binId?: string | null
  quantity: number
  unitOfMeasure: string
  sourceType: string
  sourceId: string
  lotNumber?: string | null
  serialNumber?: string | null
  expirationDate?: string | null
  notes?: string | null
}

// --- Quarantine ---
export interface QuarantineSummary {
  id: string
  companyId: string
  itemId: string
  warehouseId: string
  binId?: string | null
  lotId?: string | null
  serialNumberId?: string | null
  quantity: number
  unitOfMeasure: string
  reason: string
  referenceNumber?: string | null
  notes?: string | null
  status: string
  quarantineDate: string
  quarantinedBy: string
  releasedDate?: string | null
}

export interface CreateQuarantineRequest {
  itemId: string
  warehouseId: string
  binId?: string | null
  lotId?: string | null
  serialNumberId?: string | null
  quantity: number
  unitOfMeasure: string
  reason: string
  referenceNumber?: string | null
  notes?: string | null
}

// --- Expiration ---
export interface ExpirationSummary {
  id: string
  companyId: string
  itemId: string
  warehouseId: string
  lotId?: string | null
  serialNumberId?: string | null
  expirationDate: string
  quantity: number
  notes?: string | null
  status: string
}

export interface CreateExpirationRequest {
  itemId: string
  warehouseId: string
  lotId?: string | null
  serialNumberId?: string | null
  expirationDate: string
  quantity: number
  notes?: string | null
}

// --- Revaluation ---
export interface RevaluationSummary {
  id: string
  companyId: string
  revaluationNumber: string
  revaluationDate: string
  method: string
  notes?: string | null
  status: string
  totalAdjustmentValue: number
}

export interface CreateRevaluationRequest {
  revaluationDate: string
  method: string
  notes?: string | null
}

// --- Landed cost ---
export interface LandedCostSummary {
  id: string
  companyId: string
  vendorId: string
  costCode: string
  description: string
  costType: string
  amount: number
  costDate: string
  referenceNumber?: string | null
  status: string
  allocatedAmount: number
  remainingAmount: number
}

export interface CreateLandedCostRequest {
  companyId: string
  vendorId: string
  costCode: string
  description: string
  costType: string
  amount: number
  costDate: string
  referenceNumber?: string | null
}

// --- Cycle count ---
export interface CycleCountSummary {
  id: string
  companyId: string
  warehouseId: string
  countNumber: string
  countDate: string
  status: string
  notes?: string | null
  lines?: CycleCountLineDto[]
}

export interface CycleCountLineDto {
  id: string
  itemId: string
  itemCode?: string
  itemDescription?: string
  binId?: string | null
  systemQuantity: number
  countedQuantity?: number | null
  variance?: number | null
  lotNumber?: string | null
  serialNumber?: string | null
  notes?: string | null
}

export interface CreateCycleCountRequest {
  companyId: string
  warehouseId: string
  countNumber: string
  countDate: string
  notes?: string | null
  lines?: CreateCycleCountLineRequest[]
}

export interface CreateCycleCountLineRequest {
  itemId: string
  binId?: string | null
  countedQuantity?: number | null
  lotNumber?: string | null
  serialNumber?: string | null
  notes?: string | null
}

export interface UpdateCycleCountLineRequest {
  countedQuantity?: number | null
  notes?: string | null
}

export interface CycleCountPostResult {
  cycleCountId: string
  totalLines: number
  variancesFound: number
  adjustmentsCreated: number
  variances: CycleCountVarianceDto[]
}

export interface CycleCountVarianceDto {
  itemId: string
  itemCode: string
  itemDescription: string
  binId?: string | null
  lotNumber?: string | null
  serialNumber?: string | null
  systemQuantity: number
  countedQuantity: number
  variance: number
  varianceValue: number
}

// --- Physical count ---
export interface PhysicalCountSummary {
  id: string
  companyId: string
  warehouseId: string
  countNumber: string
  countDate: string
  status: string
  blindCount: boolean
  notes?: string | null
  lines?: PhysicalCountLineDto[]
}

export interface PhysicalCountLineDto {
  id: string
  itemId: string
  itemCode?: string
  itemDescription?: string
  binId?: string | null
  systemQuantity: number
  countedQuantity?: number | null
  variance?: number | null
  lotNumber?: string | null
  serialNumber?: string | null
  notes?: string | null
}

export interface CreatePhysicalCountRequest {
  companyId: string
  warehouseId: string
  countNumber: string
  countDate: string
  blindCount: boolean
  notes?: string | null
  lines?: CreatePhysicalCountLineRequest[]
}

export interface CreatePhysicalCountLineRequest {
  itemId: string
  binId?: string | null
  countedQuantity?: number | null
  lotNumber?: string | null
  serialNumber?: string | null
  notes?: string | null
}

export interface UpdatePhysicalCountLineRequest {
  countedQuantity?: number | null
  notes?: string | null
}

export interface PhysicalCountPostResult {
  physicalCountId: string
  totalLines: number
  variancesFound: number
  adjustmentsCreated: number
  variances: PhysicalCountVarianceDto[]
}

export interface PhysicalCountVarianceDto {
  itemId: string
  itemCode: string
  itemDescription: string
  binId?: string | null
  lotNumber?: string | null
  serialNumber?: string | null
  systemQuantity: number
  countedQuantity: number
  variance: number
  varianceValue: number
}

export interface CountSheetLine {
  itemId: string
  itemCode: string
  itemDescription: string
  binId?: string | null
  binCode?: string | null
  systemQuantity: number
  unitOfMeasure: string
  countedQuantity: number
  lotNumber: string
  serialNumber: string
}

export interface CountSheet {
  warehouseId: string
  warehouseCode?: string | null
  generatedAt: string
  lines: CountSheetLine[]
}

// --- Negative inventory override ---
export interface NegativeOverrideSummary {
  id: string
  companyId: string
  itemId: string
  warehouseId: string
  binId?: string | null
  requestedQuantity: number
  unitOfMeasure: string
  reason: string
  requestedBy: string
  referenceNumber?: string | null
  status: string
  approvedBy?: string | null
  approvedDate?: string | null
  approvalNotes?: string | null
}

export interface CreateNegativeOverrideRequest {
  itemId: string
  warehouseId: string
  binId?: string | null
  requestedQuantity: number
  unitOfMeasure: string
  reason: string
  referenceNumber?: string | null
}

// --- Movements / history ---
export interface MovementSummary {
  id: string
  itemId: string
  warehouseId: string
  movementType: string
  quantity: number
  unitOfMeasure: string
  unitCost?: number | null
  extendedCost?: number | null
  movementDate: string
  lotId?: string | null
  serialNumberId?: string | null
  referenceNumber?: string | null
}

// Matches InventoryTransactionDto returned by GET /inventory/transactions
export interface InventoryTransactionDto {
  id: string
  itemId: string
  warehouseId: string
  transactionType: string
  quantity: number
  unitOfMeasure: string
  unitCost: number
  extendedCost: number
  transactionDate: string
  lotId?: string | null
  serialNumber?: string | null
  referenceNumber?: string | null
}

// --- Reorder suggestion ---
export interface ReorderSuggestionSummary {
  id: string
  companyId: string
  itemId: string
  warehouseId: string
  suggestedQuantity: number
  reason?: string | null
  status: string
}

// --- Item vendor assignment (Phase 7 CRUD gap) ---
export interface ItemVendorAssignmentDto {
  id: string
  itemId: string
  vendorId: string
  isPrimaryVendor: boolean
  vendorItemCode?: string | null
  vendorDescription?: string | null
  vendorCost?: number | null
  leadTimeDays?: number | null
  minimumOrderQuantity?: number | null
  isActive: boolean
}

// Item GL account defaults (Phase 7 CRUD gap)
export interface ItemGlAccountDefaultsDto {
  id: string
  itemId: string
  inventoryAssetAccountId?: string | null
  cogsAccountId?: string | null
  varianceAccountId?: string | null
  purchasePriceVarianceAccountId?: string | null
  salesRevenueAccountId?: string | null
  inventoryAdjustmentAccountId?: string | null
  landedCostClearingAccountId?: string | null
}

// --- Landed Cost Allocations ---
export interface LandedCostAllocationLineDto {
  id: string
  itemId: string
  quantityReceived: number
  unitCost: number
  allocationMethod: string
  allocatedAmount: number
  landedCostId?: string | null
  description?: string | null
}
export interface LandedCostAllocationDto {
  id: string
  companyId: string
  receiptTransactionId: string
  allocationNumber: string
  allocationDate: string
  status: string
  notes?: string | null
  totalAllocatedCost: number
  createdAt: string
  createdBy?: string | null
  lines: LandedCostAllocationLineDto[]
}
export interface CreateLandedCostAllocationLineRequest {
  itemId: string
  quantityReceived: number
  unitCost: number
  allocationMethod: string
  allocatedAmount: number
  landedCostId?: string | null
  description?: string | null
}
export interface CreateLandedCostAllocationRequest {
  companyId: string
  receiptTransactionId: string
  allocationNumber: string
  allocationDate: string
  notes?: string | null
  lines: CreateLandedCostAllocationLineRequest[]
}

// --- Item Substitutions (459) ---
export interface ItemSubstitutionDto {
  id: string
  companyId: string
  itemId: string
  substituteItemId: string
  direction: number
  reason?: string | null
  status: string
  requiresApproval: boolean
  approvedBy?: string | null
  rejectedBy?: string | null
  rejectionReason?: string | null
}
export interface CreateItemSubstitutionRequest {
  companyId: string
  itemId: string
  substituteItemId: string
  direction: number
  reason?: string | null
  requiresApproval?: boolean
}

// --- Kit / Bundled Items (460) ---
export interface KitComponentDto {
  id: string
  companyId: string
  kitItemId: string
  componentItemId: string
  quantityPerKit: number
  unitOfMeasure?: string | null
}
export interface CreateKitComponentRequest {
  companyId: string
  kitItemId: string
  componentItemId: string
  quantityPerKit: number
  unitOfMeasure?: string | null
}

// --- Put-away / Picking Rules (463) ---
export interface PutAwayPickingRuleDto {
  id: string
  companyId: string
  warehouseId: string
  binId: string
  putAwayRank: number
  pickSequence: number
  pickingPolicy: number
}
export interface CreatePutAwayPickingRuleRequest {
  companyId: string
  warehouseId: string
  binId: string
  putAwayRank: number
  pickSequence: number
  pickingPolicy: number
}
export interface PutAwayPickingRecommendationDto {
  mode: string
  binId: string | null
  pickingPolicy?: string | null
  reason?: string | null
}

// --- Consignment Stock (458) ---
export interface ConsignmentStockDto {
  id: string
  companyId: string
  vendorId: string
  itemId: string
  warehouseId: string
  lotId?: string | null
  quantityOnHand: number
  unitOfMeasure?: string | null
  consignmentCost?: number | null
}
export interface CreateConsignmentStockRequest {
  companyId: string
  vendorId: string
  itemId: string
  warehouseId: string
  lotId?: string | null
  quantityOnHand: number
  unitOfMeasure?: string | null
  consignmentCost?: number | null
}

// --- Inventory GL Tie-Out (464) ---
export interface InventoryGlTieOutRow {
  itemCategoryId: string
  glAccountNumber: string
  subLedgerValue: number
}

// --- Cycle Count Scheduling (465) ---
export interface ScheduleCycleCountRequest {
  companyId: string
  warehouseId?: string | null
  frequencyMonths?: number
  abcClass?: string | null
  countDate?: string | null
}

// --- Item Stock by location / lot (462) ---
export interface ItemStockSummary {
  id: string
  companyId: string
  itemId: string
  warehouseId: string
  binId?: string | null
  lotId?: string | null
  onHandQuantity: number
  allocatedQuantity: number
  availableQuantity: number
  onOrderQuantity: number
}
