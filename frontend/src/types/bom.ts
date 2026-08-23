// Bill of Materials module types (Phase 9)

export interface BomHeaderSummary {
  id: string
  companyId: string
  parentItemId: string
  revision: string
  description: string | null
  bomType: string
  status: string
  effectiveFrom: string | null
  effectiveTo: string | null
  yieldPercentage: number
  estimatedMaterialCost: number | null
  estimatedLaborCost: number | null
  estimatedOverheadCost: number | null
  componentCount: number
  createdOn: string
}

export interface BomComponentLine {
  id: string
  bomHeaderId: string
  componentItemId: string
  quantityPerParent: number
  effectiveQuantity: number
  unitOfMeasure: string
  scrapFactor: number
  operationSequence: number
  workCenterId: string | null
  routingOperationId: string | null
  isPhantom: boolean
  isCritical: boolean
  estimatedUnitCost: number | null
  notes: string | null
}

export interface WorkCenterSummary {
  id: string
  companyId: string
  code: string
  name: string
  department: string | null
  capacityHoursPerDay: number
  efficiencyPercentage: number
  costRatePerHour: number
  isActive: boolean
}

export interface RoutingOperationSummary {
  id: string
  companyId: string
  operationCode: string
  description: string | null
  workCenterId: string | null
  standardSetupTimeMinutes: number
  standardRunTimeMinutesPerUnit: number
  isActive: boolean
}

export interface BomValidationIssue {
  bomHeaderId: string
  parentItemCode: string
  issueType: string
  message: string
}

export interface BomValidationReport {
  totalBomsChecked: number
  issues: BomValidationIssue[]
}

export interface CostRollupDelta {
  bomHeaderId: string
  parentItemCode: string
  previousCost: number
  newCost: number
  changeAmount: number
}

export interface CostRollupReport {
  totalBomsChecked: number
  updatedCount: number
  unchangedCount: number
  biggestDeltas: CostRollupDelta[]
}

export interface BuildOrderSummary {
  id: string
  companyId: string
  buildNumber: string
  transactionType: string
  bomHeaderId: string
  parentItemId: string
  quantityToBuild: number
  unitOfMeasure: string
  warehouseId: string
  buildDate: string
  status: string
  actualYield: number | null
  totalMaterialCost: number | null
  totalLaborCost: number | null
  totalOverheadCost: number | null
  totalCost: number | null
  unitCost: number | null
  notes: string | null
  lineCount: number
}

export interface BomExplosionLine {
  level: number
  componentItemId: string
  quantityPerParent: number
  netQuantity: number
  unitOfMeasure: string
  scrapFactor: number
  isPhantom: boolean
  isCritical: boolean
  operationSequence: number
}

export interface BomCostRollup {
  bomHeaderId: string
  parentItemId: string
  revision: string
  yieldPercentage: number
  totalMaterialCost: number
  totalCost: number
  components: BomCostRollupLine[]
}

export interface BomCostRollupLine {
  componentItemId: string
  quantityPerParent: number
  effectiveQuantity: number
  unitCost: number
  extendedCost: number
  scrapFactor: number
}

export interface BomListingItem {
  bomHeaderId: string
  parentItemId: string
  revision: string
  status: string
  yieldPercentage: number
  components: BomListingLine[]
}

export interface BomListingLine {
  componentItemId: string
  quantityPerParent: number
  effectiveQuantity: number
  unitOfMeasure: string
  scrapFactor: number
  isCritical: boolean
}

export interface BuildHistoryEntry {
  buildOrderId: string
  buildNumber: string
  transactionType: string
  parentItemId: string
  quantityBuilt: number
  actualYield: number | null
  yieldPercentage: number | null
  totalCost: number | null
  unitCost: number | null
  status: string
  buildDate: string
  componentCount: number
  totalScrapCost: number
}

export interface BomAccuracyItem {
  bomHeaderId: string
  parentItemId: string
  revision: string
  status: string
  issueCount: number
  issues: string[]
}

export interface WhereUsedResult {
  bomHeaderId: string
  parentItemId: string
  revision: string
  quantityPerParent: number
  unitOfMeasure: string
  isPhantom: boolean
  operationSequence: number
}
