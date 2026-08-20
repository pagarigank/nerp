// Central navigation model for the ERP app shell.
// Single source of truth for the left main/sub menu, top header title, and breadcrumbs.
import {
  Home,
  Settings,
  BookOpen,
  Banknote,
  Receipt,
  Landmark,
  ShoppingCart,
  Boxes,
  Truck,
  Layers,
  FolderKanban,
  BadgeDollarSign,
  Wrench,
  BarChart3,
  Plug,
  type LucideIcon,
} from 'lucide-react'

export interface NavSubItem {
  label: string
  to: string
}

export interface NavModule {
  name: string
  /** Base route of the module (its index page). */
  href: string
  icon: LucideIcon
  /** Roles allowed to see this module. '*' = everyone. */
  roles: string[]
  sub: NavSubItem[]
}

export const mainNavigation: NavModule[] = [
  {
    name: 'Dashboard',
    href: '/dashboard',
    icon: Home,
    roles: ['*'],
    sub: [],
  },
  {
    name: 'Platform',
    href: '/platform',
    icon: Settings,
    roles: ['Admin', 'SystemAdmin'],
    sub: [
      { label: 'Companies', to: '/platform/companies' },
      { label: 'Fiscal Periods', to: '/platform/fiscal-periods' },
      { label: 'Chart of Accounts', to: '/platform/accounts' },
      { label: 'Segment Types', to: '/platform/segment-types' },
      { label: 'Segment Values', to: '/platform/segment-values' },
      { label: 'Currencies', to: '/platform/currencies' },
      { label: 'Exchange Rates', to: '/platform/exchange-rates' },
      { label: 'Number Sequences', to: '/platform/number-sequences' },
      { label: 'Approval Workflows', to: '/platform/approval-workflows' },
      { label: 'Period Close', to: '/platform/period-close' },
      { label: 'API Keys', to: '/platform/api-keys' },
      { label: 'Approval Delegations', to: '/platform/approval-delegations' },
      { label: 'Holiday Calendar', to: '/platform/holiday-calendar' },
      { label: 'Segregation of Duties', to: '/platform/sod' },
      { label: 'Users', to: '/platform/users' },
      { label: 'Roles', to: '/platform/roles' },
      { label: 'Audit Log', to: '/platform/audit-logs' },
      { label: 'Reports', to: '/platform/reports' },
    ],
  },
  {
    name: 'General Ledger',
    href: '/gl',
    icon: BookOpen,
    roles: ['*'],
    sub: [
      { label: 'Journal Batches', to: '/gl/journal-batches' },
      { label: 'Recurring Templates', to: '/gl/recurring-templates' },
      { label: 'Allocation Rules', to: '/gl/allocation-rules' },
      { label: 'Budgets', to: '/gl/budgets' },
      { label: 'Budget Roll-Forward', to: '/gl/budget-rollforward' },
      { label: 'Revaluation', to: '/gl/revaluation' },
      { label: 'Reports', to: '/gl/reports' },
      { label: 'Consolidation', to: '/gl/consolidation' },
      { label: 'Account Inquiry', to: '/gl/account-inquiry' },
      { label: 'Pre-Posting Review', to: '/gl/pre-posting' },
      { label: 'Period-End Checklist', to: '/gl/period-end-checklist' },
      { label: 'Year-End Close', to: '/gl/year-end-close' },
      { label: 'Posting Suspense', to: '/gl/posting-suspense' },
    ],
  },
  {
    name: 'Accounts Payable',
    href: '/ap',
    icon: Banknote,
    roles: ['*'],
    sub: [
      { label: 'Vendors', to: '/ap/vendors' },
      { label: 'Payment Terms', to: '/ap/payment-terms' },
      { label: 'Voucher Batches', to: '/ap/voucher-batches' },
      { label: 'Payments', to: '/ap/payments' },
      { label: 'Three-Way Match', to: '/ap/three-way-match' },
      { label: 'Match Exceptions', to: '/ap/match-exceptions' },
      { label: 'Backup Withholding', to: '/ap/backup-withholding' },
      { label: '1099', to: '/ap/1099' },
      { label: '1099 Processing', to: '/ap/1099-processing' },
      { label: 'Duplicate Invoice', to: '/ap/duplicate-invoice' },
      { label: 'Vendor W-9 / TIN', to: '/ap/vendor-w9' },
      { label: 'Bank Verify', to: '/ap/bank-verification' },
      { label: 'Cash Discount', to: '/ap/cash-discount' },
      { label: 'Escheatment', to: '/ap/escheatment' },
      { label: 'GR/IR Accrual', to: '/ap/grir-accrual' },
      { label: 'Vendor Statements', to: '/ap/vendor-statements' },
      { label: 'Reports', to: '/ap/reports' },
    ],
  },
  {
    name: 'Accounts Receivable',
    href: '/ar',
    icon: Receipt,
    roles: ['*'],
    sub: [
      { label: 'Customers', to: '/ar/customers' },
      { label: 'Invoice Batches', to: '/ar/invoice-batches' },
      { label: 'Cash Receipts', to: '/ar/cash-receipts' },
      { label: 'Credit / Debit Memos', to: '/ar/memos' },
      { label: 'Credit Limit', to: '/ar/credit-limit' },
      { label: 'Statements', to: '/ar/statements' },
      { label: 'Finance Charges', to: '/ar/finance-charges' },
      { label: 'Collections', to: '/ar/collections' },
      { label: 'Dunning', to: '/ar/dunning' },
      { label: 'Allowance (Doubtful)', to: '/ar/allowance' },
      { label: 'Resale Certificates', to: '/ar/resale-certificates' },
      { label: 'Apply Credit Memo', to: '/ar/credit-memo-apply' },
      { label: 'Aging by Basis', to: '/ar/aging-by-basis' },
      { label: 'Receipt Match', to: '/ar/cash-receipt-match' },
      { label: 'Reports', to: '/ar/reports' },
    ],
  },
  {
    name: 'Cash Management',
    href: '/cash',
    icon: Landmark,
    roles: ['*'],
    sub: [
      { label: 'Bank Accounts', to: '/cash/bank-accounts' },
      { label: 'Deposits', to: '/cash/deposits' },
      { label: 'Bank Statements', to: '/cash/bank-statements' },
      { label: 'Reconciliations', to: '/cash/reconciliations' },
      { label: 'Transfers', to: '/cash/transfers' },
      { label: 'Bank Fees', to: '/cash/bank-fees' },
      { label: 'NSF', to: '/cash/nsf' },
      { label: 'Reports', to: '/cash/reports' },
      { label: 'GL Mapping', to: '/cash/gl-mapping' },
      { label: 'Lockbox', to: '/cash/lockbox' },
      { label: 'Stale Checks', to: '/cash/stale-checks' },
      { label: 'Positive Pay', to: '/cash/positive-pay' },
      { label: 'Fee Analysis', to: '/cash/fee-analysis' },
      { label: 'Forecast Horizon', to: '/cash/forecast-horizon' },
      { label: 'Outstanding Deposits', to: '/cash/outstanding-deposits' },
    ],
  },
  {
    name: 'Purchasing',
    href: '/purchasing',
    icon: ShoppingCart,
    roles: ['*'],
    sub: [
      { label: 'Requisitions', to: '/purchasing/requisitions' },
      { label: 'Purchase Orders', to: '/purchasing/purchase-orders' },
      { label: 'Vendor Quotes', to: '/purchasing/vendor-quotes' },
      { label: 'Approval Queue', to: '/purchasing/approval-queue' },
      { label: 'Receipts', to: '/purchasing/receipts' },
      { label: 'PO Templates', to: '/purchasing/po-templates' },
      { label: 'Requisition Templates', to: '/purchasing/requisition-templates' },
      { label: 'Vendor Items', to: '/purchasing/vendor-items' },
      { label: 'Buyer Agents', to: '/purchasing/buyer-agents' },
      { label: 'Shipping Methods', to: '/purchasing/shipping-methods' },
      { label: 'FOB Terms', to: '/purchasing/fob-terms' },
      { label: 'Reports', to: '/purchasing/reports' },
    ],
  },
  {
    name: 'Inventory',
    href: '/inventory',
    icon: Boxes,
    roles: ['*'],
    sub: [
      { label: 'Items', to: '/inventory/items' },
      { label: 'Categories', to: '/inventory/categories' },
      { label: 'Warehouses', to: '/inventory/warehouses' },
      { label: 'Bins', to: '/inventory/bins' },
      { label: 'Stock', to: '/inventory/stock' },
      { label: 'Transactions', to: '/inventory/transactions' },
      { label: 'Reservations', to: '/inventory/reservations' },
      { label: 'Quarantine', to: '/inventory/quarantine' },
      { label: 'Expiration', to: '/inventory/expiration' },
      { label: 'Revaluation', to: '/inventory/revaluation' },
      { label: 'Landed Cost', to: '/inventory/landed-cost' },
      { label: 'Landed Cost Allocations', to: '/inventory/landed-cost-allocations' },
      { label: 'Cycle Counts', to: '/inventory/cycle-counts' },
      { label: 'Physical Counts', to: '/inventory/physical-counts' },
      { label: 'Negative Overrides', to: '/inventory/negative-overrides' },
      { label: 'Movements', to: '/inventory/movements' },
      { label: 'Reorder', to: '/inventory/reorder' },
      { label: 'Substitutions', to: '/inventory/substitutions' },
      { label: 'Kits', to: '/inventory/kits' },
      { label: 'Consignment', to: '/inventory/consignment' },
      { label: 'Put-away / Picking', to: '/inventory/put-away-picking' },
      { label: 'Stock by Location', to: '/inventory/stock-by-location' },
      { label: 'Cycle Count Schedule', to: '/inventory/cycle-count-schedule' },
      { label: 'Scrap / Disposal', to: '/inventory/scrap' },
      { label: 'GL Tie-Out', to: '/inventory/gl-tie-out' },
      { label: 'Stock Card', to: '/inventory/stock-card' },
      { label: 'UOM Conversions', to: '/inventory/uom-conversions' },
      { label: 'UOM', to: '/inventory/uoms' },
      { label: 'Reports', to: '/inventory/reports' },
    ],
  },
  {
    name: 'Order Management',
    href: '/om',
    icon: Truck,
    roles: ['*'],
    sub: [
      { label: 'Sales Orders', to: '/om/sales-orders' },
      { label: 'Shipments', to: '/om/shipments' },
      { label: 'Returns', to: '/om/returns' },
      { label: 'Quotes', to: '/om/quotes' },
      { label: 'Blanket Orders', to: '/om/blanket-orders' },
      { label: 'Substitution Offers', to: '/om/substitution-offers' },
      { label: 'Return-to-Vendor', to: '/om/rtv' },
      { label: 'Notes & History', to: '/om/order-notes' },
      { label: 'Order Dashboard', to: '/om/order-dashboard' },
      { label: 'Sales Analysis', to: '/om/sales-analysis' },
      { label: 'ATP', to: '/om/atp' },
      { label: 'Freight', to: '/om/freight' },
      { label: 'Pick/Pack/Ship', to: '/om/pick-pack-ship' },
      { label: 'Reports', to: '/om/reports' },
      { label: 'Masters', to: '/om/masters' },
    ],
  },
  {
    name: 'Bill of Materials',
    href: '/bom',
    icon: Layers,
    roles: ['*'],
    sub: [
      { label: 'BOMs', to: '/bom' },
      { label: 'Work Centers', to: '/bom/work-centers' },
      { label: 'Build Orders', to: '/bom/build-orders' },
      { label: 'Reports', to: '/bom/reports' },
    ],
  },
  {
    name: 'Project Accounting',
    href: '/projects',
    icon: FolderKanban,
    roles: ['*'],
    sub: [
      { label: 'Projects', to: '/projects' },
      { label: 'Tasks', to: '/projects?section=tasks' },
      { label: 'Budgets', to: '/projects?section=budget' },
      { label: 'Costs', to: '/projects?section=costs' },
      { label: 'Billing', to: '/projects?section=billing' },
      { label: 'Change Orders', to: '/projects?section=change-orders' },
      { label: 'WIP / Analysis', to: '/projects?section=analysis' },
    ],
  },
  {
    name: 'Payroll',
    href: '/payroll',
    icon: BadgeDollarSign,
    roles: ['*'],
    sub: [
      { label: 'Employees', to: '/payroll?tab=employees' },
      { label: 'Pay Codes', to: '/payroll?tab=paycodes' },
      { label: 'Union / Certified', to: '/payroll?tab=union' },
      { label: 'Timesheets', to: '/payroll?tab=timesheets' },
      { label: 'Runs & Checks', to: '/payroll?tab=runs' },
      { label: 'Expenses', to: '/payroll?tab=expenses' },
      { label: 'Tax / W-4', to: '/payroll?tab=tax' },
      { label: 'Deductions', to: '/payroll?tab=deductions' },
      { label: 'PTO', to: '/payroll?tab=pto' },
      { label: 'Manual Checks', to: '/payroll?tab=manual' },
      { label: 'Reports', to: '/payroll?tab=reports' },
      { label: 'Garnishments', to: '/payroll?tab=garnishments' },
      { label: 'Setup & Config', to: '/payroll?tab=setup' },
    ],
  },
  {
    name: 'Field Service',
    href: '/field-service',
    icon: Wrench,
    roles: ['*'],
    sub: [],
  },
  {
    name: 'Reporting',
    href: '/reporting',
    icon: BarChart3,
    roles: ['*'],
    sub: [],
  },
  {
    name: 'Integration',
    href: '/integration',
    icon: Plug,
    roles: ['*'],
    sub: [],
  },
]

export interface NavResolved {
  module: NavModule
  sub?: NavSubItem
}
/** Resolve the current pathname to its module (and deepest matching sub-item). */
export function resolveNav(pathname: string): NavResolved | null {
  let moduleMatch: NavModule | null = null
  let subMatch: NavSubItem | undefined

  for (const m of mainNavigation) {
    if (pathname === m.href || pathname.startsWith(m.href + '/')) {
      moduleMatch = m
      let best: NavSubItem | undefined
      for (const s of m.sub) {
        if (pathname === s.to || pathname.startsWith(s.to + '/')) {
          if (!best || s.to.length > best.to.length) best = s
        }
      }
      subMatch = best
      break
    }
  }

  if (!moduleMatch) return null
  const result: NavResolved = { module: moduleMatch }
  if (subMatch) result.sub = subMatch
  return result
}
