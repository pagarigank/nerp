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
