// <copyright file="reporting.ts" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useAuthStore } from '@stores/authStore'
import { get } from './client'

function companyId(): string {
  const current = useAuthStore.getState().currentCompany
  return current?.id ?? ''
}

// ---------------------------------------------------------------------------
// Cross-module executive dashboard data
// ---------------------------------------------------------------------------

export interface ExecutiveDashboardData {
  cashPosition: CashPositionSummary
  arAging: ArAgingSummary
  apAging: ApAgingSummary
  projectPortfolio: ProjectPortfolioSummary
  inventoryValue: InventoryValueSummary
  recentActivity: ActivityItem[]
  generatedOn: string
}

export interface CashPositionSummary {
  totalCash: number
  accounts: { name: string; balance: number }[]
}

export interface ArAgingSummary {
  totalOutstanding: number
  current: number
  days30: number
  days60: number
  days90: number
  over90: number
}

export interface ApAgingSummary {
  totalOutstanding: number
  current: number
  days30: number
  days60: number
  days90: number
  over90: number
}

export interface ProjectPortfolioSummary {
  totalProjects: number
  activeProjects: number
  totalBudget: number
  totalCosts: number
  avgMarginPercent: number
  overBudgetCount: number
}

export interface InventoryValueSummary {
  totalValue: number
  totalItems: number
  lowStockCount: number
  expiringCount: number
}

export interface ActivityItem {
  id: string
  module: string
  description: string
  amount: number
  timestamp: string
  type: string
}

// ---------------------------------------------------------------------------
// Report catalog definitions
// ---------------------------------------------------------------------------

export interface ReportDefinition {
  id: string
  name: string
  module: string
  category: string
  description: string
  params: ReportParam[]
  apiEndpoint: string
  apiParams: Record<string, string>
}

export interface ReportParam {
  name: string
  label: string
  type: 'text' | 'date' | 'select' | 'number' | 'company' | 'period'
  required: boolean
  options?: { value: string; label: string }[]
  defaultValue?: string
}

// ---------------------------------------------------------------------------
// Cross-module report data fetchers
// ---------------------------------------------------------------------------

export async function getExecutiveDashboard(): Promise<ExecutiveDashboardData> {
  // Aggregate data from multiple modules in parallel
  const [bankAccounts, arAging, apAging, inventory, projectDashboard] =
    await Promise.allSettled([
      get<any[]>('/cash/bank-accounts', { companyId: companyId() }),
      get<any>('/ap/reports/aging', { companyId: companyId() }),
      get<any>('/ar/reports/aging', { companyId: companyId() }),
      get<any[]>('/inventory/reports/valuation', { companyId: companyId() }),
      get<any[]>('/projects/analysis/portfolio-dashboard', { companyId: companyId() }),
    ])

  // Process cash position
  const bankData = bankAccounts.status === 'fulfilled' ? bankAccounts.value : []
  const totalCash = Array.isArray(bankData)
    ? bankData.reduce((sum: number, a: any) => sum + (a.currentBalance ?? a.openingBalance ?? 0), 0)
    : 0

  // Process AR aging
  const arData = arAging.status === 'fulfilled' ? arAging.value : null
  const arSummary: ArAgingSummary = {
    totalOutstanding: arData?.totalOutstanding ?? 0,
    current: arData?.current ?? arData?.buckets?.current ?? 0,
    days30: arData?.days30 ?? arData?.buckets?.days1to30 ?? 0,
    days60: arData?.days60 ?? arData?.buckets?.days31to60 ?? 0,
    days90: arData?.days90 ?? arData?.buckets?.days61to90 ?? 0,
    over90: arData?.over90 ?? arData?.buckets?.over90 ?? 0,
  }

  // Process AP aging
  const apData = apAging.status === 'fulfilled' ? apAging.value : null
  const apSummary: ApAgingSummary = {
    totalOutstanding: apData?.totalOutstanding ?? 0,
    current: apData?.current ?? apData?.buckets?.current ?? 0,
    days30: apData?.days30 ?? apData?.buckets?.days1to30 ?? 0,
    days60: apData?.days60 ?? apData?.buckets?.days31to60 ?? 0,
    days90: apData?.days90 ?? apData?.buckets?.days61to90 ?? 0,
    over90: apData?.over90 ?? apData?.buckets?.over90 ?? 0,
  }

  // Process inventory value
  const invData = inventory.status === 'fulfilled' ? inventory.value : []
  const invTotal = Array.isArray(invData)
    ? invData.reduce((sum: number, i: any) => sum + (i.totalValue ?? i.extendedValue ?? 0), 0)
    : 0

  // Process project portfolio
  const projData = projectDashboard.status === 'fulfilled' ? projectDashboard.value : []
  const projSummary: ProjectPortfolioSummary = {
    totalProjects: Array.isArray(projData) ? projData.length : 0,
    activeProjects: Array.isArray(projData) ? projData.filter((p: any) => p.status === 'Active').length : 0,
    totalBudget: Array.isArray(projData) ? projData.reduce((s: number, p: any) => s + (p.budgetAmount ?? 0), 0) : 0,
    totalCosts: Array.isArray(projData) ? projData.reduce((s: number, p: any) => s + (p.costsToDate ?? 0), 0) : 0,
    avgMarginPercent: Array.isArray(projData) && projData.length > 0
      ? projData.reduce((s: number, p: any) => s + (p.marginPercent ?? 0), 0) / projData.length
      : 0,
    overBudgetCount: Array.isArray(projData) ? projData.filter((p: any) => (p.costsToDate ?? 0) > (p.budgetAmount ?? 0)).length : 0,
  }

  return {
    cashPosition: {
      totalCash,
      accounts: Array.isArray(bankData)
        ? bankData.map((a: any) => ({ name: a.name ?? a.accountName ?? 'Account', balance: a.currentBalance ?? a.openingBalance ?? 0 }))
        : [],
    },
    arAging: arSummary,
    apAging: apSummary,
    projectPortfolio: projSummary,
    inventoryValue: {
      totalValue: invTotal,
      totalItems: Array.isArray(invData) ? invData.length : 0,
      lowStockCount: 0,
      expiringCount: 0,
    },
    recentActivity: [],
    generatedOn: new Date().toISOString(),
  }
}

// ---------------------------------------------------------------------------
// Individual module report wrappers (for catalog)
// ---------------------------------------------------------------------------

// GL Reports
export function getGlTrialBalance(params?: { fiscalPeriodId?: string }) {
  return get<any>('/gl/reports/trial-balance', { companyId: companyId(), ...params })
}

export function getGlBalanceSheet(params?: { fiscalPeriodId?: string }) {
  return get<any>('/gl/reports/balance-sheet', { companyId: companyId(), ...params })
}

export function getGlIncomeStatement(params?: { fiscalPeriodId?: string }) {
  return get<any>('/gl/reports/income-statement', { companyId: companyId(), ...params })
}

export function getGlCashFlow(params?: { fiscalPeriodId?: string }) {
  return get<any>('/gl/reports/cash-flow', { companyId: companyId(), ...params })
}

export function getGlBudgetVsActual(params?: { budgetId?: string; fiscalPeriodId?: string }) {
  return get<any>('/gl/reports/budget-vs-actual', { companyId: companyId(), ...params })
}

// AP Reports
export function getApAging() {
  return get<any>('/ap/reports/aging', { companyId: companyId() })
}

export function getApVendorTrialBalance() {
  return get<any>('/ap/reports/vendor-trial-balance', { companyId: companyId() })
}

export function getApBatchRegister() {
  return get<any>('/ap/reports/batch-register', { companyId: companyId() })
}

export function getApCashRequirements() {
  return get<any>('/ap/reports/cash-requirements', { companyId: companyId() })
}

// AR Reports
export function getArAging() {
  return get<any>('/ar/reports/aging', { companyId: companyId() })
}

export function getArCustomerTrialBalance() {
  return get<any>('/ar/reports/customer-trial-balance', { companyId: companyId() })
}

export function getArCashReceiptsJournal() {
  return get<any>('/ar/reports/cash-receipts-journal', { companyId: companyId() })
}

export function getArSalesJournal() {
  return get<any>('/ar/reports/sales-journal', { companyId: companyId() })
}

// Inventory Reports
export function getInventoryValuation() {
  return get<any[]>('/inventory/reports/valuation', { companyId: companyId() })
}

export function getInventoryReorder() {
  return get<any[]>('/inventory/reports/reorder', { companyId: companyId() })
}

export function getInventoryStockOut() {
  return get<any[]>('/inventory/reports/stock-out', { companyId: companyId() })
}

// Purchasing Reports
export function getPurchasingOpenPO() {
  return get<any[]>('/purchasing/reports/open-po', { companyId: companyId() })
}

export function getPurchasingVendorPerformance() {
  return get<any[]>('/purchasing/reports/vendor-performance', { companyId: companyId() })
}

// Payroll Reports
export function getPayrollRegister() {
  return get<any>('/payroll/reports/payroll-register', { companyId: companyId() })
}

export function getPayrollSummary() {
  return get<any>('/payroll/reports/payroll-summary', { companyId: companyId() })
}

export function getLaborDistribution() {
  return get<any>('/payroll/reports/labor-distribution', { companyId: companyId() })
}

// Project Reports
export function getProjectProfitability() {
  return get<any>('/projects/analysis/portfolio-dashboard', { companyId: companyId() })
}

export function getProjectWipSchedule() {
  return get<any[]>('/projects/analysis/wip', { companyId: companyId() })
}

// Cash Reports
export function getCashPosition() {
  return get<any>('/cash/reports/cash-position', { companyId: companyId() })
}

export function getCashForecast() {
  return get<any>('/cash/reports/cash-forecast', { companyId: companyId() })
}

// Field Service Reports
export function getFieldServiceSlaCompliance() {
  return get<any>('/field-service/reports/sla-compliance', { companyId: companyId() })
}

export function getFieldServiceTechnicianUtilization() {
  return get<any>('/field-service/reports/technician-utilization', { companyId: companyId() })
}

// Platform Reports
export function getAuditTrail(params?: { fromDate?: string; toDate?: string; entityType?: string }) {
  return get<any>('/platform/reports/audit-trail', { companyId: companyId(), ...params })
}

export function getSecurityMatrix() {
  return get<any>('/platform/reports/security-matrix', { companyId: companyId() })
}
