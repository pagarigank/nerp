// Base API types
export interface ApiResponse<T> {
  isSuccess: boolean
  data?: T
  message?: string
  errors?: string[]
  statusCode?: number
  timestamp: string
  traceId?: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface PagedRequest {
  page: number
  pageSize: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
  filters?: Record<string, string | number | boolean | null>
}

// Entity types
export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  isActive: boolean
  roles: Role[]
  permissions: string[]
  createdAt: string
  updatedAt: string
}

export interface Company {
  id: string
  code: string
  name: string
  legalName: string
  taxId: string
  baseCurrency: string
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  phone?: string
  email?: string
  website?: string
  isActive: boolean
  parentCompanyId?: string
  createdAt: string
  updatedAt: string
}

export interface FiscalPeriod {
  id: string
  companyId: string
  fiscalYear: number
  periodNumber: number
  periodName: string
  startDate: string
  endDate: string
  isCurrent: boolean
  isClosed: boolean
  closedAt?: string
  closedBy?: string
  createdAt: string
  updatedAt: string
}

export interface Role {
  id: string
  name: string
  description: string
  permissions: string[]
  isSystem: boolean
  createdAt: string
  updatedAt: string
}

export interface Permission {
  id: string
  code: string
  name: string
  description: string
  module: string
  category: string
}

export interface SegmentType {
  id: string
  code: string
  name: string
  description?: string
  length: number
  isRequired: boolean
  validationRegex?: string
  displayOrder: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface SegmentValue {
  id: string
  segmentTypeId: string
  segmentType?: SegmentType
  value: string
  description: string
  isActive: boolean
  validFrom: string
  validTo?: string
  createdAt: string
  updatedAt: string
}

export interface Account {
  id: string
  companyId: string
  accountKey: string
  segments: Record<string, string>
  name: string
  description?: string
  accountType: 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense'
  normalBalance: 'Debit' | 'Credit'
  isActive: boolean
  isControlAccount: boolean
  controlAccountType?: 'Vendor' | 'Customer' | 'Inventory' | 'FixedAsset' | 'Payroll'
  allowPosting: boolean
  requireProject: boolean
  requireDepartment: boolean
  createdAt: string
  updatedAt: string
}

export interface Currency {
  id: string
  code: string
  name: string
  symbol: string
  decimalPlaces: number
  isBase: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface ExchangeRate {
  id: string
  fromCurrency: string
  toCurrency: string
  rate: number
  rateDate: string
  rateType: 'Spot' | 'Average' | 'PeriodEnd'
  source: string
  createdAt: string
  updatedAt: string
}

export interface NumberingSequence {
  id: string
  companyId: string
  module: string
  prefix: string
  nextNumber: number
  numberLength: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

// Financial types
export interface JournalBatch {
  id: string
  companyId: string
  fiscalPeriodId: string
  batchNumber: string
  description: string
  reference?: string
  status: 'Draft' | 'Released' | 'Posted' | 'Reversed' | 'Voided'
  totalDebits: number
  totalCredits: number
  lineCount: number
  postedAt?: string
  postedBy?: string
  createdAt: string
  updatedAt: string
  createdBy: string
  lines: JournalLine[]
}

export interface JournalLine {
  id: string
  batchId: string
  lineNumber: number
  accountId: string
  account?: Account
  accountKey: string
  description: string
  debitAmount: number
  creditAmount: number
  currency: string
  exchangeRate: number
  baseDebitAmount: number
  baseCreditAmount: number
  projectId?: string
  taskId?: string
  reference?: string
  createdAt: string
  updatedAt: string
}

export interface Vendor {
  id: string
  companyId: string
  vendorNumber: string
  name: string
  legalName?: string
  taxId?: string
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  phone?: string
  email?: string
  contactPerson?: string
  paymentTermsId: string
  paymentTerms?: PaymentTerms
  currencyId: string
  currency?: Currency
  taxGroupId?: string
  is1099Vendor: boolean
  _1099Category?: string
  defaultAccountId?: string
  defaultAccount?: Account
  isActive: boolean
  isOnHold: boolean
  holdReason?: string
  createdAt: string
  updatedAt: string
}

export interface PaymentTerms {
  id: string
  code: string
  name: string
  description?: string
  dueDays: number
  discountDays: number
  discountPercent: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface Voucher {
  id: string
  companyId: string
  fiscalPeriodId: string
  voucherNumber: string
  vendorId: string
  vendor?: Vendor
  invoiceNumber: string
  invoiceDate: string
  invoiceAmount: number
  baseInvoiceAmount: number
  currencyId: string
  currency?: Currency
  exchangeRate: number
  status: 'Draft' | 'Released' | 'Posted' | 'Voided' | 'Paid'
  description: string
  paymentTermsId: string
  paymentTerms?: PaymentTerms
  dueDate: string
  discountDate?: string
  discountAmount: number
  baseDiscountAmount: number
  distributionLines: VoucherDistributionLine[]
  postedAt?: string
  postedBy?: string
  voidedAt?: string
  voidedBy?: string
  voidReason?: string
  createdAt: string
  updatedAt: string
  createdBy: string
}

export interface VoucherDistributionLine {
  id: string
  voucherId: string
  lineNumber: number
  accountId: string
  account?: Account
  accountKey: string
  description: string
  amount: number
  baseAmount: number
  projectId?: string
  taskId?: string
  is1099: boolean
  createdAt: string
  updatedAt: string
}

export interface Customer {
  id: string
  companyId: string
  customerNumber: string
  name: string
  legalName?: string
  taxId?: string
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  phone?: string
  email?: string
  contactPerson?: string
  paymentTermsId: string
  paymentTerms?: PaymentTerms
  currencyId: string
  currency?: Currency
  taxGroupId?: string
  creditLimit: number
  creditHold: boolean
  defaultAccountId?: string
  defaultAccount?: Account
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface Invoice {
  id: string
  companyId: string
  fiscalPeriodId: string
  invoiceNumber: string
  customerId: string
  customer?: Customer
  invoiceDate: string
  dueDate: string
  invoiceAmount: number
  baseInvoiceAmount: number
  currencyId: string
  currency?: Currency
  exchangeRate: number
  status: 'Draft' | 'Released' | 'Posted' | 'Voided' | 'Paid'
  description: string
  paymentTermsId: string
  paymentTerms?: PaymentTerms
  distributionLines: InvoiceDistributionLine[]
  postedAt?: string
  postedBy?: string
  voidedAt?: string
  voidedBy?: string
  voidReason?: string
  createdAt: string
  updatedAt: string
  createdBy: string
}

export interface InvoiceDistributionLine {
  id: string
  invoiceId: string
  lineNumber: number
  accountId: string
  account?: Account
  accountKey: string
  description: string
  quantity: number
  unitPrice: number
  amount: number
  baseAmount: number
  projectId?: string
  taskId?: string
  taxAmount: number
  taxCode?: string
  createdAt: string
  updatedAt: string
}

export interface Project {
  id: string
  companyId: string
  projectNumber: string
  name: string
  description?: string
  projectType: 'TimeAndMaterials' | 'CostPlus' | 'FixedPrice' | 'Milestone'
  status: 'Planning' | 'Active' | 'OnHold' | 'Completed' | 'Cancelled'
  startDate: string
  endDate?: string
  estimatedEndDate?: string
  customerId?: string
  customer?: Customer
  projectManagerId?: string
  projectManager?: User
  billingCurrencyId: string
  billingCurrency?: Currency
  isActive: boolean
  isBillable: boolean
  createdAt: string
  updatedAt: string
  createdBy: string
}

export interface ProjectTask {
  id: string
  projectId: string
  taskNumber: string
  name: string
  description?: string
  status: 'NotStarted' | 'InProgress' | 'OnHold' | 'Completed' | 'Cancelled'
  startDate: string
  endDate?: string
  estimatedHours: number
  actualHours: number
  estimatedCost: number
  actualCost: number
  parentTaskId?: string
  parentTask?: ProjectTask
  children?: ProjectTask[]
  createdAt: string
  updatedAt: string
}

export interface Item {
  id: string
  companyId: string
  itemNumber: string
  name: string
  description?: string
  itemType: 'Inventory' | 'NonInventory' | 'Service' | 'Assembly'
  unitOfMeasure: string
  standardCost: number
  listPrice: number
  salesAccountId?: string
  salesAccount?: Account
  cogsAccountId?: string
  cogsAccount?: Account
  inventoryAccountId?: string
  inventoryAccount?: Account
  costMethod: 'FIFO' | 'LIFO' | 'Average' | 'Standard'
  isActive: boolean
  isTaxable: boolean
  taxGroupId?: string
  weight?: number
  volume?: number
  createdAt: string
  updatedAt: string
}

export interface Warehouse {
  id: string
  companyId: string
  code: string
  name: string
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  isDefault: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface PurchaseOrder {
  id: string
  companyId: string
  fiscalPeriodId: string
  poNumber: string
  vendorId: string
  vendor?: Vendor
  orderDate: string
  requiredDate?: string
  promisedDate?: string
  status: 'Draft' | 'Submitted' | 'Approved' | 'PartiallyReceived' | 'Received' | 'Cancelled' | 'Closed'
  currencyId: string
  currency?: Currency
  exchangeRate: number
  subtotal: number
  taxAmount: number
  totalAmount: number
  baseTotalAmount: number
  lines: PurchaseOrderLine[]
  notes?: string
  approvedAt?: string
  approvedBy?: string
  createdAt: string
  updatedAt: string
  createdBy: string
}

export interface PurchaseOrderLine {
  id: string
  purchaseOrderId: string
  lineNumber: number
  itemId?: string
  item?: Item
  description: string
  quantityOrdered: number
  quantityReceived: number
  quantityInvoiced: number
  unitPrice: number
  baseUnitPrice: number
  discountPercent: number
  discountAmount: number
  taxAmount: number
  taxCode?: string
  accountId?: string
  account?: Account
  projectId?: string
  taskId?: string
  promisedDate?: string
  createdAt: string
  updatedAt: string
}

export interface SalesOrder {
  id: string
  companyId: string
  fiscalPeriodId: string
  orderNumber: string
  customerId: string
  customer?: Customer
  orderDate: string
  requiredDate?: string
  promisedDate?: string
  status: 'Draft' | 'Confirmed' | 'PartiallyShipped' | 'Shipped' | 'Invoiced' | 'Cancelled' | 'Closed'
  currencyId: string
  currency?: Currency
  exchangeRate: number
  subtotal: number
  taxAmount: number
  totalAmount: number
  baseTotalAmount: number
  lines: SalesOrderLine[]
  notes?: string
  createdAt: string
  updatedAt: string
  createdBy: string
}

export interface SalesOrderLine {
  id: string
  salesOrderId: string
  lineNumber: number
  itemId?: string
  item?: Item
  description: string
  quantityOrdered: number
  quantityShipped: number
  quantityInvoiced: number
  unitPrice: number
  baseUnitPrice: number
  discountPercent: number
  discountAmount: number
  taxAmount: number
  taxCode?: string
  accountId?: string
  account?: Account
  projectId?: string
  taskId?: string
  promisedDate?: string
  createdAt: string
  updatedAt: string
}

export interface InventoryTransaction {
  id: string
  companyId: string
  fiscalPeriodId: string
  transactionNumber: string
  transactionType: 'Receipt' | 'Issue' | 'Transfer' | 'Adjustment' | 'CycleCount' | 'Assembly' | 'Disassembly'
  transactionDate: string
  warehouseId: string
  warehouse?: Warehouse
  toWarehouseId?: string
  toWarehouse?: Warehouse
  status: 'Draft' | 'Posted' | 'Voided'
  lines: InventoryTransactionLine[]
  reference?: string
  notes?: string
  postedAt?: string
  postedBy?: string
  createdAt: string
  updatedAt: string
  createdBy: string
}

export interface InventoryTransactionLine {
  id: string
  transactionId: string
  lineNumber: number
  itemId: string
  item?: Item
  quantity: number
  unitCost: number
  baseUnitCost: number
  totalCost: number
  baseTotalCost: number
  lotNumber?: string
  serialNumber?: string
  location?: string
  projectId?: string
  taskId?: string
  accountId?: string
  account?: Account
  createdAt: string
  updatedAt: string
}

export interface BankAccount {
  id: string
  companyId: string
  accountNumber: string
  accountName: string
  bankName: string
  bankRoutingNumber: string
  accountType: 'Checking' | 'Savings' | 'MoneyMarket' | 'CreditCard'
  currencyId: string
  currency?: Currency
  glAccountId: string
  glAccount?: Account
  isActive: boolean
  isReconciled: boolean
  lastReconciledDate?: string
  lastReconciledBalance: number
  currentBalance: number
  createdAt: string
  updatedAt: string
}

export interface Employee {
  id: string
  companyId: string
  employeeNumber: string
  firstName: string
  lastName: string
  middleName?: string
  fullName: string
  email?: string
  phone?: string
  addressLine1: string
  addressLine2?: string
  city: string
  state: string
  postalCode: string
  country: string
  hireDate: string
  terminationDate?: string
  status: 'Active' | 'OnLeave' | 'Terminated' | 'Deceased'
  departmentId?: string
  department?: Department
  positionId?: string
  position?: Position
  supervisorId?: string
  supervisor?: Employee
  payFrequency: 'Weekly' | 'BiWeekly' | 'SemiMonthly' | 'Monthly'
  payRate: number
  payRateType: 'Hourly' | 'Salary'
  currencyId: string
  currency?: Currency
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface Department {
  id: string
  companyId: string
  code: string
  name: string
  description?: string
  managerId?: string
  manager?: Employee
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface Position {
  id: string
  companyId: string
  code: string
  name: string
  description?: string
  minSalary: number
  maxSalary: number
  currencyId: string
  currency?: Currency
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface Timesheet {
  id: string
  companyId: string
  fiscalPeriodId: string
  employeeId: string
  employee?: Employee
  weekEndingDate: string
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected' | 'Posted'
  totalHours: number
  totalOvertimeHours: number
  lines: TimesheetLine[]
  submittedAt?: string
  submittedBy?: string
  approvedAt?: string
  approvedBy?: string
  rejectedAt?: string
  rejectedBy?: string
  rejectReason?: string
  postedAt?: string
  postedBy?: string
  createdAt: string
  updatedAt: string
  createdBy: string
}

export interface TimesheetLine {
  id: string
  timesheetId: string
  lineNumber: number
  date: string
  projectId?: string
  project?: Project
  taskId?: string
  task?: ProjectTask
  payCodeId: string
  payCode?: PayCode
  hours: number
  overtimeHours: number
  rate: number
  baseRate: number
  amount: number
  baseAmount: number
  description?: string
  isBillable: boolean
  createdAt: string
  updatedAt: string
}

export interface PayCode {
  id: string
  companyId: string
  code: string
  name: string
  description?: string
  payType: 'Regular' | 'Overtime' | 'DoubleTime' | 'Holiday' | 'Vacation' | 'Sick' | 'Other'
  multiplier: number
  isTaxable: boolean
  isBillable: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface WorkOrder {
  id: string
  companyId: string
  woNumber: string
  customerId?: string
  customer?: Customer
  equipmentId?: string
  equipment?: Equipment
  status: 'Open' | 'Scheduled' | 'InProgress' | 'OnHold' | 'Completed' | 'Cancelled' | 'Closed'
  priority: 'Low' | 'Medium' | 'High' | 'Emergency'
  scheduledStart: string
  scheduledEnd?: string
  actualStart?: string
  actualEnd?: string
  assignedTechnicianId?: string
  assignedTechnician?: Employee
  description: string
  resolution?: string
  laborHours: number
  laborCost: number
  partsCost: number
  totalCost: number
  billableAmount: number
  projectId?: string
  project?: Project
  taskId?: string
  task?: ProjectTask
  createdAt: string
  updatedAt: string
  createdBy: string
}

export interface Equipment {
  id: string
  companyId: string
  equipmentNumber: string
  name: string
  description?: string
  serialNumber?: string
  modelNumber?: string
  manufacturer?: string
  customerId?: string
  customer?: Customer
  location?: string
  warrantyExpiration?: string
  lastServiceDate?: string
  nextServiceDate?: string
  status: 'Active' | 'Inactive' | 'Retired' | 'UnderRepair'
  isActive: boolean
  createdAt: string
  updatedAt: string
}

// Query parameter types
export interface QueryParams extends PagedRequest {
  search?: string
  status?: string
  dateFrom?: string
  dateTo?: string
  companyId?: string
  fiscalPeriodId?: string
}