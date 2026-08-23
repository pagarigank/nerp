// Project Accounting module types (Phase 10)

export interface ProjectSummary {
  id: string
  companyId: string
  projectCode: string
  name: string
  description: string | null
  projectType: string
  status: string
  customerId: string | null
  projectManager: string | null
  contractValue: number | null
  originalBudget: number
  revisedBudget: number
  costsToDate: number
  revenueToDate: number
  percentComplete: number
  retainagePercentage: number
  retainageHeld: number
  profitMargin: number | null
  plannedStartDate: string | null
  plannedEndDate: string | null
  actualStartDate: string | null
  actualEndDate: string | null
  taskCount: number
  budgetLineCount: number
}

export interface ProjectTask {
  id: string
  projectId: string
  taskCode: string
  description: string
  parentTaskId: string | null
  budgetedHours: number
  budgetedCost: number
  actualHours: number
  actualCost: number
  percentComplete: number
  sortOrder: number
}

export interface BudgetLine {
  id: string
  projectId: string
  taskId: string
  category: string
  budgetAmount: number
  budgetedHours: number
  actualAmount: number
  actualHours: number
  committedAmount: number
  variance: number
  description: string | null
  isRevised: boolean
}

export interface CostTransaction {
  id: string
  projectId: string
  taskId: string
  category: string
  transactionType: string
  amount: number
  hours: number
  burdenAmount: number
  billableAmount: number
  description: string | null
  sourceReference: string | null
  isBillable: boolean
  status: string
  transactionDate: string
}

export interface CostSummary {
  projectId: string
  totalCosts: number
  totalBudget: number
  remaining: number
  percentComplete: number
  byCategory: Record<string, { actual: number; budget: number; hours: number; variance: number }>
}

export interface ChangeOrder {
  id: string
  projectId: string
  description: string
  amount: number
  category: string
  reason: string | null
  status: string
  submittedDate: string | null
  approvedDate: string | null
  approvedBy: string | null
}

export interface ContractLine {
  id: string
  projectId: string
  description: string
  billingMethod: string
  contractAmount: number
  unitPrice: number | null
  unitQuantity: number | null
  feePercentage: number | null
  notToExceed: number | null
  billedAmount: number
  remaining: number
  percentComplete: number
  isActive: boolean
  notes: string | null
}

export interface BillingScheduleItem {
  id: string
  projectId: string
  description: string
  billingMethod: string
  amount: number
  percentCompleteTrigger: number | null
  scheduledDate: string | null
  sequenceNumber: number
  isBilled: boolean
  billedDate: string | null
}

export interface WipSchedule {
  projectId: string
  contractValue: number
  totalBudget: number
  costsToDate: number
  percentComplete: number
  earnedRevenue: number
  billedToDate: number
  overUnderBilling: number
  retainageHeld: number
}

export interface BillingResult {
  projectId: string
  invoiceAmount: number
  retainageHeld: number
  lines: InvoiceLine[]
}

export interface InvoiceLine {
  contractLineId: string
  description: string
  grossAmount: number
  retainageAmount: number
  netAmount: number
}

// --- Analysis reports (Phase 10 long-tail reports) ---

export interface EmployeeProjectHours {
  projectId: string
  projectCode: string
  projectName: string
  hours: number
  amount: number
}

export interface EmployeeUtilizationRow {
  employeeId: string
  totalHours: number
  billableHours: number
  billablePercent: number
  capacityHours: number
  utilizationPercent: number
  laborCost: number
  projects: EmployeeProjectHours[]
}

export interface EmployeeProfitabilityRow {
  employeeId: string
  billedAmount: number
  unbilledBillableAmount: number
  costAmount: number
  margin: number
  marginPercent: number
}

export interface SubcontractStatusRow {
  subcontractId: string
  subcontractNumber: string
  vendorId: string
  projectId: string
  projectCode: string
  projectName: string
  status: string
  contractAmount: number
  approvedChangeOrders: number
  revisedAmount: number
  invoicedToDate: number
  retainageHeld: number
  remaining: number
}

export interface SubcontractCommitmentRow {
  projectId: string
  projectCode: string
  projectName: string
  openSubcontractCount: number
  committedTotal: number
  invoicedAgainstCommitted: number
  remainingCommitment: number
  projectBudgetRemaining: number
}

export interface CertifiedPayrollRow {
  employeeId: string
  projectId: string
  projectCode: string
  classification: string
  hours: number
  wageAmount: number
  hourlyRate: number
}

export interface PortfolioDashboardRow {
  projectId: string
  projectCode: string
  name: string
  projectManager: string
  contractValue: number
  revenue: number
  costs: number
  marginPercent: number
  percentComplete: number
  budget: number
  estimateAtCompletion: number
  riskStatus: string
}

export interface ProjectAgingRow {
  projectId: string
  projectCode: string
  name: string
  statusGroup: string
  ageDays: number
  overOneYear: boolean
  overBudget: boolean
  negativeMargin: boolean
  actionable: boolean
}

export interface ContractValueAnalysisRow {
  contractType: string
  projectCount: number
  totalContractValue: number
  averageMarginPercent: number
}

export interface PmPerformanceRow {
  projectManager: string
  projectCount: number
  activeCount: number
  completedOnTimeCount: number
  completedWithScheduleCount: number
  averageMarginPercent: number
  onBudgetPercent: number
}

export interface EarnedValueRow {
  projectId: string
  projectCode: string
  bac: number
  bcws: number
  bcwp: number
  acwp: number
  sv: number
  cv: number
  spi: number
  cpi: number
  eac: number
}

export interface PendingCoImpactRow {
  projectId: string
  projectCode: string
  contractValueExcludingPending: number
  approvedChangeOrders: number
  pendingChangeOrders: number
  contractValueIncludingPending: number
  estimateAtCompletion: number
  projectedRevenueIncludingPending: number
  projectedMargin: number
  projectedMarginPercent: number
}

export interface LienWaiverRegisterRow {
  waiverId: string
  subcontractId: string
  subcontractNumber: string
  vendorId: string
  projectId: string
  projectCode: string
  projectName: string
  waiverType: string
  isFinal: boolean
  effectiveDate: string
  amount: number
  description: string | null
}

export interface ContractAssetLiabilityRow {
  projectId: string
  projectCode: string
  name: string
  earnedRevenue: number
  billedRevenue: number
  contractAsset: number
  contractLiability: number
  classification: string
}

// --- ASC 606 revenue recognition ---

export interface PerformanceObligation {
  id: string
  companyId: string
  projectId: string
  description: string
  transactionPriceAllocated: number
  standaloneSellingPriceBasis: string | null
  recognizedRevenueToDate: number
  percentSatisfied: number
  status: string
  satisfiedOn: string | null
  canEditOrDelete: boolean
}

export interface Asc606RecognitionStatus {
  projectId: string
  budgetAtCompletion: number
  estimateAtCompletion: number
  costsPostedToDate: number
  costToCostPercent: number
  obligations: PerformanceObligation[]
}

export interface Asc606AllocationRow {
  obligationId: string
  description: string
  standaloneSellingPriceBasis: string | null
  priorAllocation: number
  newAllocation: number
  sharePercent: number
}

export interface Asc606AllocateResult {
  projectId: string
  totalContractPrice: number
  rows: Asc606AllocationRow[]
}

export interface Asc606RecognizeResult {
  obligationId: string
  projectId: string
  amountRecognized: number
  recognizedRevenueToDate: number
  transactionPriceAllocated: number
  status: string
  satisfiedOn: string | null
  glPostingPending: boolean
  note: string
}

export interface FiveStepObligationRow {
  id: string
  description: string
  standaloneSellingPriceBasis: string | null
  transactionPriceAllocated: number
  allocationSharePercent: number
  recognizedRevenueToDate: number
  percentSatisfied: number
  status: string
  satisfiedOn: string | null
}

export interface FiveStepSummary {
  contract: {
    projectId: string
    projectCode: string
    name: string
    customerId: string | null
    projectType: string
    status: string
    contractValue: number | null
    plannedStartDate: string | null
    plannedEndDate: string | null
  }
  pendingChangeOrderAmount: number
  variableConsiderationConstraintNote: string
  totalContractPriceAllocated: number
  totalRecognizedRevenue: number
  obligations: FiveStepObligationRow[]
}

// --- Project documents ---

export interface ProjectDocumentItem {
  id: string
  companyId: string
  projectId: string
  name: string
  documentType: string
  fileReference: string
  contentType: string | null
  sizeBytes: number | null
  uploadedBy: string
  uploadedOn: string
}

// --- Profit fade / EAC trend ---

export interface EacTrendPoint {
  projectId: string
  projectCode: string
  projectName: string
  capturedOn: string
  originalBudget: number
  budgetAtCompletion: number
  estimateAtCompletion: number
  estimatedMarginPct: number
  pendingChangeOrderAmount: number | null
}

export interface EacPortfolioPoint {
  captureDate: string
  projectCount: number
  averageEstimatedMarginPct: number
  averageEstimateAtCompletion: number
}
