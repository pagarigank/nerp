// <copyright file="ReportsCatalogPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  BarChart3, BookOpen, CreditCard, Receipt, Landmark, ShoppingCart,
  Boxes, Truck, Layers, FolderKanban, BadgeDollarSign, Wrench, Settings,
  Search, Star, ExternalLink, Play, FileText, Download,
} from 'lucide-react'
import { formatCurrency } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getGlTrialBalance, getGlBalanceSheet, getGlIncomeStatement, getGlCashFlow,
  getGlBudgetVsActual,
  getApAging, getApVendorTrialBalance, getApBatchRegister, getApCashRequirements,
  getArAging, getArCustomerTrialBalance, getArCashReceiptsJournal, getArSalesJournal,
  getInventoryValuation, getInventoryReorder, getInventoryStockOut,
  getPurchasingOpenPO, getPurchasingVendorPerformance,
  getPayrollRegister, getPayrollSummary, getLaborDistribution,
  getProjectProfitability, getProjectWipSchedule,
  getCashPosition, getCashForecast,
  getFieldServiceSlaCompliance, getFieldServiceTechnicianUtilization,
  getAuditTrail, getSecurityMatrix,
} from '@api/reporting'

// ---------------------------------------------------------------------------
// Report catalog definition
// ---------------------------------------------------------------------------

interface ReportEntry {
  id: string
  name: string
  module: string
  moduleIcon: any
  moduleColor: string
  category: string
  description: string
  endpoint: () => Promise<any>
  columns: { key: string; header: string; align?: 'left' | 'right'; format?: (v: any) => string }[]
  adapter: (raw: any) => any[]
}

const MODULE_ICONS: Record<string, { icon: any; color: string }> = {
  'General Ledger': { icon: BookOpen, color: 'text-indigo-600 bg-indigo-100 dark:bg-indigo-900/30' },
  'Accounts Payable': { icon: CreditCard, color: 'text-amber-600 bg-amber-100 dark:bg-amber-900/30' },
  'Accounts Receivable': { icon: Receipt, color: 'text-emerald-600 bg-emerald-100 dark:bg-emerald-900/30' },
  'Cash Management': { icon: Landmark, color: 'text-teal-600 bg-teal-100 dark:bg-teal-900/30' },
  'Purchasing': { icon: ShoppingCart, color: 'text-orange-600 bg-orange-100 dark:bg-orange-900/30' },
  'Inventory': { icon: Boxes, color: 'text-violet-600 bg-violet-100 dark:bg-violet-900/30' },
  'Order Management': { icon: Truck, color: 'text-cyan-600 bg-cyan-100 dark:bg-cyan-900/30' },
  'Bill of Materials': { icon: Layers, color: 'text-purple-600 bg-purple-100 dark:bg-purple-900/30' },
  'Project Accounting': { icon: FolderKanban, color: 'text-rose-600 bg-rose-100 dark:bg-rose-900/30' },
  'Payroll': { icon: BadgeDollarSign, color: 'text-green-600 bg-green-100 dark:bg-green-900/30' },
  'Field Service': { icon: Wrench, color: 'text-blue-600 bg-blue-100 dark:bg-blue-900/30' },
  'Platform': { icon: Settings, color: 'text-slate-600 bg-slate-100 dark:bg-slate-900/30' },
}

const ALL_REPORTS: ReportEntry[] = [
  // ---- General Ledger ----
  {
    id: 'gl-trial-balance', name: 'Trial Balance', module: 'General Ledger',
    moduleIcon: BookOpen, moduleColor: MODULE_ICONS['General Ledger'].color,
    category: 'Financial Statements', description: 'Account balances by period — debits, credits, ending balance.',
    endpoint: () => getGlTrialBalance(), columns: [
      { key: 'accountNumber', header: 'Account' },
      { key: 'accountDescription', header: 'Description' },
      { key: 'debit', header: 'Debit', align: 'right', format: formatCurrency },
      { key: 'credit', header: 'Credit', align: 'right', format: formatCurrency },
      { key: 'endingBalance', header: 'Balance', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
  },
  {
    id: 'gl-balance-sheet', name: 'Balance Sheet', module: 'General Ledger',
    moduleIcon: BookOpen, moduleColor: MODULE_ICONS['General Ledger'].color,
    category: 'Financial Statements', description: 'Assets, liabilities, and equity by account.',
    endpoint: () => getGlBalanceSheet(), columns: [
      { key: 'accountNumber', header: 'Account' },
      { key: 'accountDescription', header: 'Description' },
      { key: 'balance', header: 'Balance', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
  },
  {
    id: 'gl-income-statement', name: 'Income Statement', module: 'General Ledger',
    moduleIcon: BookOpen, moduleColor: MODULE_ICONS['General Ledger'].color,
    category: 'Financial Statements', description: 'Revenue and expense accounts.',
    endpoint: () => getGlIncomeStatement(), columns: [
      { key: 'accountNumber', header: 'Account' },
      { key: 'accountDescription', header: 'Description' },
      { key: 'balance', header: 'Amount', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
  },
  {
    id: 'gl-cash-flow', name: 'Cash Flow Statement', module: 'General Ledger',
    moduleIcon: BookOpen, moduleColor: MODULE_ICONS['General Ledger'].color,
    category: 'Financial Statements', description: 'Operating, investing, and financing cash flows.',
    endpoint: () => getGlCashFlow(), columns: [
      { key: 'category', header: 'Category' },
      { key: 'accountNumber', header: 'Account' },
      { key: 'accountDescription', header: 'Description' },
      { key: 'amount', header: 'Amount', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
  },
  {
    id: 'gl-budget-vs-actual', name: 'Budget vs Actual', module: 'General Ledger',
    moduleIcon: BookOpen, moduleColor: MODULE_ICONS['General Ledger'].color,
    category: 'Budgeting', description: 'Compare budget allocations to actual posted activity.',
    endpoint: () => getGlBudgetVsActual(), columns: [
      { key: 'accountNumber', header: 'Account' },
      { key: 'accountDescription', header: 'Description' },
      { key: 'budgetAmount', header: 'Budget', align: 'right', format: formatCurrency },
      { key: 'actualAmount', header: 'Actual', align: 'right', format: formatCurrency },
      { key: 'variance', header: 'Variance', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
  },

  // ---- Accounts Payable ----
  {
    id: 'ap-aging', name: 'AP Aging', module: 'Accounts Payable',
    moduleIcon: CreditCard, moduleColor: MODULE_ICONS['Accounts Payable'].color,
    category: 'Aging', description: 'Outstanding payables by aging bucket.',
    endpoint: () => getApAging(), columns: [
      { key: 'vendorName', header: 'Vendor' },
      { key: 'current', header: 'Current', align: 'right', format: formatCurrency },
      { key: 'days30', header: '1-30', align: 'right', format: formatCurrency },
      { key: 'days60', header: '31-60', align: 'right', format: formatCurrency },
      { key: 'days90', header: '61-90', align: 'right', format: formatCurrency },
      { key: 'over90', header: '90+', align: 'right', format: formatCurrency },
      { key: 'total', header: 'Total', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'ap-vendor-tb', name: 'Vendor Trial Balance', module: 'Accounts Payable',
    moduleIcon: CreditCard, moduleColor: MODULE_ICONS['Accounts Payable'].color,
    category: 'Financial', description: 'Vendor balances for GL tie-out.',
    endpoint: () => getApVendorTrialBalance(), columns: [
      { key: 'vendorName', header: 'Vendor' },
      { key: 'balance', header: 'Balance', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'ap-batch-register', name: 'AP Batch Register', module: 'Accounts Payable',
    moduleIcon: CreditCard, moduleColor: MODULE_ICONS['Accounts Payable'].color,
    category: 'Register', description: 'All AP voucher batches with status and amounts.',
    endpoint: () => getApBatchRegister(), columns: [
      { key: 'batchNumber', header: 'Batch #' },
      { key: 'status', header: 'Status' },
      { key: 'lineCount', header: 'Lines', align: 'right' },
      { key: 'totalDebits', header: 'Debits', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.batches ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'ap-cash-req', name: 'Cash Requirements', module: 'Accounts Payable',
    moduleIcon: CreditCard, moduleColor: MODULE_ICONS['Accounts Payable'].color,
    category: 'Cash Planning', description: 'Upcoming payment obligations by due date.',
    endpoint: () => getApCashRequirements(), columns: [
      { key: 'vendorName', header: 'Vendor' },
      { key: 'invoiceNumber', header: 'Invoice #' },
      { key: 'dueDate', header: 'Due Date' },
      { key: 'amount', header: 'Amount', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.vouchers ?? (Array.isArray(r) ? r : []),
  },

  // ---- Accounts Receivable ----
  {
    id: 'ar-aging', name: 'AR Aging', module: 'Accounts Receivable',
    moduleIcon: Receipt, moduleColor: MODULE_ICONS['Accounts Receivable'].color,
    category: 'Aging', description: 'Outstanding receivables by aging bucket.',
    endpoint: () => getArAging(), columns: [
      { key: 'customerName', header: 'Customer' },
      { key: 'current', header: 'Current', align: 'right', format: formatCurrency },
      { key: 'days30', header: '1-30', align: 'right', format: formatCurrency },
      { key: 'days60', header: '31-60', align: 'right', format: formatCurrency },
      { key: 'days90', header: '61-90', align: 'right', format: formatCurrency },
      { key: 'over90', header: '90+', align: 'right', format: formatCurrency },
      { key: 'total', header: 'Total', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'ar-customer-tb', name: 'Customer Trial Balance', module: 'Accounts Receivable',
    moduleIcon: Receipt, moduleColor: MODULE_ICONS['Accounts Receivable'].color,
    category: 'Financial', description: 'Customer balances for GL tie-out.',
    endpoint: () => getArCustomerTrialBalance(), columns: [
      { key: 'customerName', header: 'Customer' },
      { key: 'balance', header: 'Balance', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'ar-cash-receipts', name: 'Cash Receipts Journal', module: 'Accounts Receivable',
    moduleIcon: Receipt, moduleColor: MODULE_ICONS['Accounts Receivable'].color,
    category: 'Journal', description: 'All cash receipts applied to customer accounts.',
    endpoint: () => getArCashReceiptsJournal(), columns: [
      { key: 'receiptNumber', header: 'Receipt #' },
      { key: 'customerName', header: 'Customer' },
      { key: 'amount', header: 'Amount', align: 'right', format: formatCurrency },
      { key: 'appliedTo', header: 'Applied To' },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'ar-sales-journal', name: 'Sales Journal', module: 'Accounts Receivable',
    moduleIcon: Receipt, moduleColor: MODULE_ICONS['Accounts Receivable'].color,
    category: 'Journal', description: 'All sales invoices posted.',
    endpoint: () => getArSalesJournal(), columns: [
      { key: 'invoiceNumber', header: 'Invoice #' },
      { key: 'customerName', header: 'Customer' },
      { key: 'amount', header: 'Amount', align: 'right', format: formatCurrency },
      { key: 'postingDate', header: 'Date' },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },

  // ---- Inventory ----
  {
    id: 'inv-valuation', name: 'Inventory Valuation', module: 'Inventory',
    moduleIcon: Boxes, moduleColor: MODULE_ICONS['Inventory'].color,
    category: 'Valuation', description: 'Item values by warehouse and costing method.',
    endpoint: () => getInventoryValuation(), columns: [
      { key: 'itemCode', header: 'Item' },
      { key: 'description', header: 'Description' },
      { key: 'warehouseName', header: 'Warehouse' },
      { key: 'quantityOnHand', header: 'Qty', align: 'right' },
      { key: 'unitCost', header: 'Unit Cost', align: 'right', format: formatCurrency },
      { key: 'totalValue', header: 'Total Value', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },
  {
    id: 'inv-reorder', name: 'Reorder Report', module: 'Inventory',
    moduleIcon: Boxes, moduleColor: MODULE_ICONS['Inventory'].color,
    category: 'Planning', description: 'Items below reorder point with suggested order quantities.',
    endpoint: () => getInventoryReorder(), columns: [
      { key: 'itemCode', header: 'Item' },
      { key: 'description', header: 'Description' },
      { key: 'quantityOnHand', header: 'On Hand', align: 'right' },
      { key: 'reorderPoint', header: 'Reorder Pt', align: 'right' },
      { key: 'suggestedOrderQty', header: 'Suggested Qty', align: 'right' },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },
  {
    id: 'inv-stock-out', name: 'Stock-Out Report', module: 'Inventory',
    moduleIcon: Boxes, moduleColor: MODULE_ICONS['Inventory'].color,
    category: 'Alerts', description: 'Items with zero on-hand and open demand.',
    endpoint: () => getInventoryStockOut(), columns: [
      { key: 'itemCode', header: 'Item' },
      { key: 'description', header: 'Description' },
      { key: 'quantityOnHand', header: 'On Hand', align: 'right' },
      { key: 'openDemand', header: 'Open Demand', align: 'right' },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },

  // ---- Purchasing ----
  {
    id: 'pur-open-po', name: 'Open PO Report', module: 'Purchasing',
    moduleIcon: ShoppingCart, moduleColor: MODULE_ICONS['Purchasing'].color,
    category: 'Purchasing', description: 'All open purchase orders by vendor, buyer, or project.',
    endpoint: () => getPurchasingOpenPO(), columns: [
      { key: 'poNumber', header: 'PO #' },
      { key: 'vendorName', header: 'Vendor' },
      { key: 'status', header: 'Status' },
      { key: 'originalAmount', header: 'Original', align: 'right', format: formatCurrency },
      { key: 'receivedAmount', header: 'Received', align: 'right', format: formatCurrency },
      { key: 'remainingAmount', header: 'Remaining', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },
  {
    id: 'pur-vendor-perf', name: 'Vendor Performance', module: 'Purchasing',
    moduleIcon: ShoppingCart, moduleColor: MODULE_ICONS['Purchasing'].color,
    category: 'Analysis', description: 'On-time delivery, price variance, quality metrics by vendor.',
    endpoint: () => getPurchasingVendorPerformance(), columns: [
      { key: 'vendorName', header: 'Vendor' },
      { key: 'onTimePercent', header: 'On-Time %', align: 'right' },
      { key: 'totalOrders', header: 'Orders', align: 'right' },
      { key: 'totalAmount', header: 'Total', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },

  // ---- Payroll ----
  {
    id: 'pay-register', name: 'Payroll Register', module: 'Payroll',
    moduleIcon: BadgeDollarSign, moduleColor: MODULE_ICONS['Payroll'].color,
    category: 'Payroll', description: 'All employees: gross, taxes, deductions, net, check detail.',
    endpoint: () => getPayrollRegister(), columns: [
      { key: 'employeeName', header: 'Employee' },
      { key: 'grossPay', header: 'Gross', align: 'right', format: formatCurrency },
      { key: 'taxes', header: 'Taxes', align: 'right', format: formatCurrency },
      { key: 'deductions', header: 'Deductions', align: 'right', format: formatCurrency },
      { key: 'netPay', header: 'Net Pay', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'pay-summary', name: 'Payroll Summary', module: 'Payroll',
    moduleIcon: BadgeDollarSign, moduleColor: MODULE_ICONS['Payroll'].color,
    category: 'Payroll', description: 'Totals by department, pay code: gross wages, employer taxes, net pay.',
    endpoint: () => getPayrollSummary(), columns: [
      { key: 'department', header: 'Department' },
      { key: 'grossWages', header: 'Gross Wages', align: 'right', format: formatCurrency },
      { key: 'employerTaxes', header: 'Employer Taxes', align: 'right', format: formatCurrency },
      { key: 'netPay', header: 'Net Pay', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'pay-labor-dist', name: 'Labor Distribution', module: 'Payroll',
    moduleIcon: BadgeDollarSign, moduleColor: MODULE_ICONS['Payroll'].color,
    category: 'Payroll', description: 'Wage expense by GL account, department, project/task.',
    endpoint: () => getLaborDistribution(), columns: [
      { key: 'glAccount', header: 'GL Account' },
      { key: 'department', header: 'Department' },
      { key: 'projectCode', header: 'Project' },
      { key: 'amount', header: 'Amount', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },

  // ---- Project Accounting ----
  {
    id: 'prj-profitability', name: 'Project Portfolio Dashboard', module: 'Project Accounting',
    moduleIcon: FolderKanban, moduleColor: MODULE_ICONS['Project Accounting'].color,
    category: 'Analysis', description: 'Margin %, % complete, EAC, risk by project.',
    endpoint: () => getProjectProfitability(), columns: [
      { key: 'projectCode', header: 'Project' },
      { key: 'projectName', header: 'Name' },
      { key: 'status', header: 'Status' },
      { key: 'budgetAmount', header: 'Budget', align: 'right', format: formatCurrency },
      { key: 'costsToDate', header: 'Costs', align: 'right', format: formatCurrency },
      { key: 'marginPercent', header: 'Margin %', align: 'right' },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.projects ?? []),
  },
  {
    id: 'prj-wip', name: 'WIP Schedule', module: 'Project Accounting',
    moduleIcon: FolderKanban, moduleColor: MODULE_ICONS['Project Accounting'].color,
    category: 'Billing', description: 'Contract value, costs-to-date, earned revenue, over/under billing.',
    endpoint: () => getProjectWipSchedule(), columns: [
      { key: 'projectCode', header: 'Project' },
      { key: 'contractValue', header: 'Contract', align: 'right', format: formatCurrency },
      { key: 'costsToDate', header: 'Costs', align: 'right', format: formatCurrency },
      { key: 'earnedRevenue', header: 'Earned', align: 'right', format: formatCurrency },
      { key: 'billedToDate', header: 'Billed', align: 'right', format: formatCurrency },
      { key: 'overUnderBilling', header: 'Over/Under', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },

  // ---- Cash Management ----
  {
    id: 'cash-position', name: 'Cash Position', module: 'Cash Management',
    moduleIcon: Landmark, moduleColor: MODULE_ICONS['Cash Management'].color,
    category: 'Treasury', description: 'Real-time balances across all bank accounts.',
    endpoint: () => getCashPosition(), columns: [
      { key: 'accountName', header: 'Account' },
      { key: 'currentBalance', header: 'Balance', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.accounts ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'cash-forecast', name: 'Cash Forecast', module: 'Cash Management',
    moduleIcon: Landmark, moduleColor: MODULE_ICONS['Cash Management'].color,
    category: 'Treasury', description: 'Projected cash flow considering open AP/AR.',
    endpoint: () => getCashForecast(), columns: [
      { key: 'period', header: 'Period' },
      { key: 'openingBalance', header: 'Opening', align: 'right', format: formatCurrency },
      { key: 'projectedInflows', header: 'Inflows', align: 'right', format: formatCurrency },
      { key: 'projectedOutflows', header: 'Outflows', align: 'right', format: formatCurrency },
      { key: 'closingBalance', header: 'Closing', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.periods ?? (Array.isArray(r) ? r : []),
  },

  // ---- Field Service ----
  {
    id: 'fs-sla', name: 'SLA Compliance', module: 'Field Service',
    moduleIcon: Wrench, moduleColor: MODULE_ICONS['Field Service'].color,
    category: 'Operations', description: 'Response/resolution time vs. SLA by priority.',
    endpoint: () => getFieldServiceSlaCompliance(), columns: [
      { key: 'priority', header: 'Priority' },
      { key: 'totalWorkOrders', header: 'Total', align: 'right' },
      { key: 'metSla', header: 'Met SLA', align: 'right' },
      { key: 'breached', header: 'Breached', align: 'right' },
      { key: 'compliancePercent', header: 'Compliance %', align: 'right' },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },
  {
    id: 'fs-tech-util', name: 'Technician Utilization', module: 'Field Service',
    moduleIcon: Wrench, moduleColor: MODULE_ICONS['Field Service'].color,
    category: 'Operations', description: 'Hours assigned, completed, utilization % by technician.',
    endpoint: () => getFieldServiceTechnicianUtilization(), columns: [
      { key: 'technicianId', header: 'Technician' },
      { key: 'assignedHours', header: 'Assigned Hrs', align: 'right' },
      { key: 'completedHours', header: 'Completed Hrs', align: 'right' },
      { key: 'utilizationPercent', header: 'Utilization %', align: 'right' },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },

  // ---- Platform ----
  {
    id: 'plt-audit-trail', name: 'Audit Trail', module: 'Platform',
    moduleIcon: Settings, moduleColor: MODULE_ICONS['Platform'].color,
    category: 'Compliance', description: 'All create/update/delete/post/void actions with user and timestamp.',
    endpoint: () => getAuditTrail(), columns: [
      { key: 'action', header: 'Action' },
      { key: 'entityType', header: 'Entity' },
      { key: 'entityId', header: 'Record' },
      { key: 'performedBy', header: 'User' },
      { key: 'performedOn', header: 'Timestamp' },
    ],
    adapter: (r) => r?.entries ?? (Array.isArray(r) ? r : []),
  },
  {
    id: 'plt-security', name: 'Security Matrix', module: 'Platform',
    moduleIcon: Settings, moduleColor: MODULE_ICONS['Platform'].color,
    category: 'Compliance', description: 'Roles, permissions, and user assignments.',
    endpoint: () => getSecurityMatrix(), columns: [
      { key: 'roleName', header: 'Role' },
      { key: 'permissionCount', header: 'Permissions', align: 'right' },
      { key: 'userCount', header: 'Users', align: 'right' },
    ],
    adapter: (r) => r?.rolePermissions ?? (Array.isArray(r) ? r : []),
  },
]

// ---------------------------------------------------------------------------
// Favorites storage (localStorage)
// ---------------------------------------------------------------------------

function getFavorites(): string[] {
  try {
    const raw = localStorage.getItem('erp-report-favorites')
    return raw ? JSON.parse(raw) : []
  } catch { return [] }
}

function toggleFavorite(id: string): string[] {
  const favs = getFavorites()
  const next = favs.includes(id) ? favs.filter(f => f !== id) : [...favs, id]
  localStorage.setItem('erp-report-favorites', JSON.stringify(next))
  return next
}

// ---------------------------------------------------------------------------
// Report Result Viewer (inline)
// ---------------------------------------------------------------------------

function ReportResult({ report }: { report: ReportEntry }) {
  const [runId, setRunId] = useState(0)

  const { data, isLoading, error } = useQuery({
    queryKey: ['reporting', 'catalog', report.id, runId],
    queryFn: async () => {
      const raw = await report.endpoint()
      return report.adapter(raw)
    },
    enabled: runId > 0,
    staleTime: 30_000,
  })

  const rows = (data as any[]) ?? []

  const handleExportCsv = () => {
    if (rows.length === 0) return
    const headers = report.columns.map(c => c.header)
    const csvRows = rows.map((row: any) =>
      report.columns.map(c => {
        const val = row[c.key]
        return typeof val === 'string' && val.includes(',') ? `"${val}"` : (val ?? '')
      }).join(',')
    )
    const csv = [headers.join(','), ...csvRows].join('\n')
    const blob = new Blob([csv], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url; a.download = `${report.id}.csv`; a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <Card className="mt-4">
      <CardHeader
        title={report.name}
        description={report.description}
        action={
          <div className="flex gap-2">
            <Button size="sm" variant="outline" onClick={() => setRunId(id => id + 1)} leftIcon={<Play className="h-4 w-4" />}>
              Run
            </Button>
            {rows.length > 0 && (
              <Button size="sm" variant="ghost" onClick={handleExportCsv} leftIcon={<Download className="h-4 w-4" />}>
                CSV
              </Button>
            )}
          </div>
        }
      />
      <CardContent className="p-0">
        {runId === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
            Click <strong>Run</strong> to generate this report.
          </p>
        ) : isLoading ? (
          <div className="p-6"><Skeleton className="h-48" /></div>
        ) : error ? (
          <p className="text-sm text-red-600 dark:text-red-400 py-8 text-center">
            {getErrorMessage(error)}
          </p>
        ) : rows.length === 0 ? (
          <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
            No data found for the current filters.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  {report.columns.map(col => (
                    <th key={col.key} className={`px-4 py-2.5 font-medium text-gray-500 dark:text-gray-400 ${col.align === 'right' ? 'text-right' : ''}`}>
                      {col.header}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                {rows.slice(0, 100).map((row: any, ri: number) => (
                  <tr key={ri} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                    {report.columns.map(col => (
                      <td key={col.key} className={`px-4 py-2.5 ${col.align === 'right' ? 'text-right font-tabular tabular-nums' : ''}`}>
                        {col.format ? col.format(row[col.key]) : (row[col.key] ?? '—')}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
            {rows.length > 100 && (
              <p className="text-xs text-gray-400 dark:text-gray-500 py-2 px-4">
                Showing 100 of {rows.length} rows. Export to CSV for full data.
              </p>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

// ---------------------------------------------------------------------------
// Main Page
// ---------------------------------------------------------------------------

export function ReportsCatalogPage() {
  const [search, setSearch] = useState('')
  const [moduleFilter, setModuleFilter] = useState('all')
  const [categoryFilter, setCategoryFilter] = useState('all')
  const [expandedReport, setExpandedReport] = useState<string | null>(null)
  const [favorites, setFavorites] = useState<string[]>(() => getFavorites())

  const modules = useMemo(() => [...new Set(ALL_REPORTS.map(r => r.module))].sort(), [])
  const categories = useMemo(() => [...new Set(ALL_REPORTS.map(r => r.category))].sort(), [])

  const moduleOptions = [{ value: 'all', label: 'All Modules' }, ...modules.map(m => ({ value: m, label: m }))]
  const categoryOptions = [{ value: 'all', label: 'All Categories' }, ...categories.map(c => ({ value: c, label: c }))]

  const filtered = useMemo(() => {
    return ALL_REPORTS.filter(r => {
      if (moduleFilter !== 'all' && r.module !== moduleFilter) return false
      if (categoryFilter !== 'all' && r.category !== categoryFilter) return false
      if (search) {
        const q = search.toLowerCase()
        return r.name.toLowerCase().includes(q) ||
          r.description.toLowerCase().includes(q) ||
          r.module.toLowerCase().includes(q) ||
          r.category.toLowerCase().includes(q)
      }
      return true
    })
  }, [search, moduleFilter, categoryFilter])

  const favReports = useMemo(() => ALL_REPORTS.filter(r => favorites.includes(r.id)), [favorites])

  const handleToggleFav = (id: string) => {
    setFavorites(toggleFavorite(id))
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Report Catalog</h1>
        <p className="text-gray-500 dark:text-gray-400 mt-1">
          Browse and run reports across all modules
        </p>
      </div>

      {/* Favorites Section */}
      {favReports.length > 0 && (
        <Card>
          <CardHeader title="⭐ Favorite Reports" />
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {favReports.map(r => {
                const modInfo = MODULE_ICONS[r.module] ?? { icon: BarChart3, color: 'text-gray-600 bg-gray-100' }
                const Icon = modInfo.icon
                return (
                  <button
                    key={r.id}
                    onClick={() => setExpandedReport(expandedReport === r.id ? null : r.id)}
                    className={`flex items-center gap-2 px-3 py-2 rounded-lg border text-sm transition-colors ${
                      expandedReport === r.id
                        ? 'border-primary-600 bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300'
                        : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'
                    }`}
                  >
                    <Icon className="h-4 w-4" />
                    {r.name}
                  </button>
                )
              })}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Filters */}
      <Card>
        <CardContent>
          <div className="flex flex-wrap items-end gap-4">
            <div className="flex-1 min-w-[200px]">
              <Input
                label="Search Reports"
                placeholder="Search by name, module, or description..."
                value={search}
                onChange={e => setSearch(e.target.value)}
                leftIcon={<Search className="h-4 w-4" />}
              />
            </div>
            <Select
              label="Module"
              options={moduleOptions}
              value={moduleFilter}
              onChange={e => setModuleFilter(e.target.value)}
              className="w-48"
            />
            <Select
              label="Category"
              options={categoryOptions}
              value={categoryFilter}
              onChange={e => setCategoryFilter(e.target.value)}
              className="w-48"
            />
          </div>
        </CardContent>
      </Card>

      {/* Report List */}
      {filtered.length === 0 ? (
        <Card>
          <CardContent className="py-10 text-center">
            <FileText className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
            <p className="text-sm text-gray-500 dark:text-gray-400">
              No reports match your filters. Try adjusting your search criteria.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {filtered.map(report => {
            const modInfo = MODULE_ICONS[report.module] ?? { icon: BarChart3, color: 'text-gray-600 bg-gray-100' }
            const Icon = modInfo.icon
            const isExpanded = expandedReport === report.id

            return (
              <Card key={report.id} className={isExpanded ? 'ring-2 ring-primary-500' : ''}>
                <CardContent className="p-4">
                  <div className="flex items-center gap-4">
                    <div className={`p-2.5 rounded-lg flex-shrink-0 ${modInfo.color}`}>
                      <Icon className="h-5 w-5" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <h3 className="text-sm font-semibold text-gray-900 dark:text-white">{report.name}</h3>
                        <span className="text-xs text-gray-400 dark:text-gray-500">•</span>
                        <span className="text-xs text-gray-500 dark:text-gray-400">{report.module}</span>
                        <span className="text-xs text-gray-400 dark:text-gray-500">•</span>
                        <span className="text-xs text-gray-500 dark:text-gray-400">{report.category}</span>
                      </div>
                      <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{report.description}</p>
                    </div>
                    <div className="flex items-center gap-1 flex-shrink-0">
                      <button
                        onClick={() => handleToggleFav(report.id)}
                        className={`p-1.5 rounded-md transition-colors ${
                          favorites.includes(report.id)
                            ? 'text-amber-500 hover:text-amber-600'
                            : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300'
                        }`}
                        title={favorites.includes(report.id) ? 'Remove from favorites' : 'Add to favorites'}
                      >
                        <Star className={`h-4 w-4 ${favorites.includes(report.id) ? 'fill-current' : ''}`} />
                      </button>
                      <Button
                        size="sm"
                        variant={isExpanded ? 'primary' : 'outline'}
                        onClick={() => setExpandedReport(isExpanded ? null : report.id)}
                      >
                        {isExpanded ? 'Close' : 'Open'}
                      </Button>
                    </div>
                  </div>
                </CardContent>
                {isExpanded && (
                  <ReportResult report={report} />
                )}
              </Card>
            )
          })}
        </div>
      )}

      <p className="text-xs text-gray-400 dark:text-gray-500">
        {filtered.length} report{filtered.length !== 1 ? 's' : ''} available across {modules.length} modules
      </p>
    </div>
  )
}
