// <copyright file="PermissionCatalog.cs" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

namespace ERP.Modules.Platform.Domain;

/// <summary>One page within a module, used to build the RBAC permission catalog.</summary>
public sealed record PageEntry(string Page, string Label);

/// <summary>A module and the pages it exposes.</summary>
public sealed record ModuleEntry(string Module, string Label, IReadOnlyList<PageEntry> Pages);

/// <summary>
/// Canonical page registry used to build the RBAC permission catalog and the
/// role editor matrix. Mirrors the left-nav in frontend/src/navigation.tsx; keep
/// the two in sync when pages are added/removed.
/// </summary>
public static class PermissionCatalog
{
    public const string View = "view";
    public const string Create = "create";
    public const string Edit = "edit";
    public const string Delete = "delete";

    public static IReadOnlyList<string> Actions { get; } = new[] { View, Create, Edit, Delete };

    public static IReadOnlyList<ModuleEntry> Modules { get; } = new List<ModuleEntry>
    {
        new("platform", "Platform / System", new List<PageEntry>
        {
            new("companies", "Companies"),
            new("fiscal-periods", "Fiscal Periods"),
            new("accounts", "Chart of Accounts"),
            new("segment-types", "Segment Types"),
            new("segment-values", "Segment Values"),
            new("currencies", "Currencies"),
            new("exchange-rates", "Exchange Rates"),
            new("number-sequences", "Number Sequences"),
            new("approval-workflows", "Approval Workflows"),
            new("period-close", "Period Close"),
            new("api-keys", "API Keys"),
            new("approval-delegations", "Approval Delegations"),
            new("holiday-calendar", "Holiday Calendar"),
            new("sod", "Segregation of Duties"),
            new("users", "Users"),
            new("access-requests", "Access Requests"),
            new("roles", "Roles"),
            new("audit-logs", "Audit Log"),
            new("platform-reports", "Reports"),
        }),
        new("gl", "General Ledger", new List<PageEntry>
        {
            new("journal-batches", "Journal Batches"),
            new("recurring-templates", "Recurring Templates"),
            new("allocation-rules", "Allocation Rules"),
            new("budgets", "Budgets"),
            new("budget-rollforward", "Budget Roll-Forward"),
            new("revaluation", "Revaluation"),
            new("consolidation", "Consolidation"),
            new("account-inquiry", "Account Inquiry"),
            new("pre-posting", "Pre-Posting Review"),
            new("period-end-checklist", "Period-End Checklist"),
            new("year-end-close", "Year-End Close"),
            new("posting-suspense", "Posting Suspense"),
            new("gl-reports", "Reports"),
        }),
        new("ap", "Accounts Payable", new List<PageEntry>
        {
            new("vendors", "Vendors"),
            new("payment-terms", "Payment Terms"),
            new("voucher-batches", "Voucher Batches"),
            new("payments", "Payments"),
            new("three-way-match", "Three-Way Match"),
            new("match-exceptions", "Match Exceptions"),
            new("backup-withholding", "Backup Withholding"),
            new("1099", "1099"),
            new("1099-processing", "1099 Processing"),
            new("duplicate-invoice", "Duplicate Invoice"),
            new("vendor-w9", "Vendor W-9 / TIN"),
            new("bank-verification", "Bank Verify"),
            new("cash-discount", "Cash Discount"),
            new("escheatment", "Escheatment"),
            new("grir-accrual", "GR/IR Accrual"),
            new("vendor-statements", "Vendor Statements"),
            new("ap-reports", "Reports"),
        }),
        new("ar", "Accounts Receivable", new List<PageEntry>
        {
            new("customers", "Customers"),
            new("invoice-batches", "Invoice Batches"),
            new("cash-receipts", "Cash Receipts"),
            new("memos", "Credit / Debit Memos"),
            new("credit-limit", "Credit Limit"),
            new("statements", "Statements"),
            new("finance-charges", "Finance Charges"),
            new("collections", "Collections"),
            new("dunning", "Dunning"),
            new("allowance", "Allowance (Doubtful)"),
            new("resale-certificates", "Resale Certificates"),
            new("credit-memo-apply", "Apply Credit Memo"),
            new("aging-by-basis", "Aging by Basis"),
            new("cash-receipt-match", "Receipt Match"),
            new("ar-reports", "Reports"),
        }),
        new("cash", "Cash Management", new List<PageEntry>
        {
            new("bank-accounts", "Bank Accounts"),
            new("deposits", "Deposits"),
            new("bank-statements", "Bank Statements"),
            new("reconciliations", "Reconciliations"),
            new("transfers", "Transfers"),
            new("bank-fees", "Bank Fees"),
            new("nsf", "NSF"),
            new("gl-mapping", "GL Mapping"),
            new("lockbox", "Lockbox"),
            new("stale-checks", "Stale Checks"),
            new("positive-pay", "Positive Pay"),
            new("fee-analysis", "Fee Analysis"),
            new("forecast-horizon", "Forecast Horizon"),
            new("outstanding-deposits", "Outstanding Deposits"),
            new("cash-reports", "Reports"),
        }),
        new("purchasing", "Purchasing", new List<PageEntry>
        {
            new("requisitions", "Requisitions"),
            new("purchase-orders", "Purchase Orders"),
            new("vendor-quotes", "Vendor Quotes"),
            new("approval-queue", "Approval Queue"),
            new("receipts", "Receipts"),
            new("po-templates", "PO Templates"),
            new("requisition-templates", "Requisition Templates"),
            new("vendor-items", "Vendor Items"),
            new("vendors", "Vendors"),
            new("buyer-agents", "Buyer Agents"),
            new("shipping-methods", "Shipping Methods"),
            new("fob-terms", "FOB Terms"),
            new("purchasing-reports", "Reports"),
        }),
        new("inventory", "Inventory", new List<PageEntry>
        {
            new("items", "Items"),
            new("categories", "Categories"),
            new("warehouses", "Warehouses"),
            new("bins", "Bins"),
            new("stock", "Stock"),
            new("transactions", "Transactions"),
            new("reservations", "Reservations"),
            new("quarantine", "Quarantine"),
            new("expiration", "Expiration"),
            new("revaluation", "Revaluation"),
            new("landed-cost", "Landed Cost"),
            new("cycle-counts", "Cycle Counts"),
            new("physical-counts", "Physical Counts"),
            new("negative-overrides", "Negative Overrides"),
            new("movements", "Movements"),
            new("reorder", "Reorder"),
            new("substitutions", "Substitutions"),
            new("kits", "Kits"),
            new("consignment", "Consignment"),
            new("put-away-picking", "Put-away / Picking"),
            new("stock-by-location", "Stock by Location"),
            new("cycle-count-schedule", "Cycle Count Schedule"),
            new("scrap", "Scrap / Disposal"),
            new("gl-tie-out", "GL Tie-Out"),
            new("stock-card", "Stock Card"),
            new("uom-conversions", "UOM Conversions"),
            new("uoms", "UOM"),
            new("inventory-reports", "Reports"),
        }),
        new("om", "Order Management", new List<PageEntry>
        {
            new("sales-orders", "Sales Orders"),
            new("shipments", "Shipments"),
            new("returns", "Returns"),
            new("quotes", "Quotes"),
            new("blanket-orders", "Blanket Orders"),
            new("substitution-offers", "Substitution Offers"),
            new("return-to-vendor", "Return-to-Vendor"),
            new("order-notes", "Notes & History"),
            new("order-dashboard", "Order Dashboard"),
            new("sales-analysis", "Sales Analysis"),
            new("commissions", "Commissions"),
            new("atp", "ATP"),
            new("freight", "Freight"),
            new("pick-pack-ship", "Pick/Pack/Ship"),
            new("masters", "Masters"),
            new("om-reports", "Reports"),
        }),
        new("bom", "Bill of Materials", new List<PageEntry>
        {
            new("boms", "BOMs"),
            new("work-centers", "Work Centers"),
            new("routing-operations", "Routing Operations"),
            new("build-orders", "Build Orders"),
            new("bom-reports", "Reports"),
        }),
        new("projects", "Project Accounting", new List<PageEntry>
        {
            new("projects", "Projects"),
            new("tasks", "Tasks"),
            new("budgets", "Budgets"),
            new("costs", "Costs"),
            new("billing", "Billing"),
            new("change-orders", "Change Orders"),
            new("analysis", "WIP / Analysis"),
            new("project-reports", "Reports"),
        }),
        new("payroll", "Payroll", new List<PageEntry>
        {
            new("employees", "Employees"),
            new("paycodes", "Pay Codes"),
            new("union", "Union / Certified"),
            new("timesheets", "Timesheets"),
            new("runs", "Runs & Checks"),
            new("expenses", "Expenses"),
            new("tax", "Tax / W-4"),
            new("deductions", "Deductions"),
            new("pto", "PTO"),
            new("manual", "Manual Checks"),
            new("garnishments", "Garnishments"),
            new("setup", "Setup & Config"),
            new("payroll-reports", "Reports"),
        }),
        new("field-service", "Field Service", new List<PageEntry>
        {
            new("work-orders", "Work Orders"),
            new("dispatch", "Dispatch Board"),
            new("technicians", "Technicians"),
            new("contracts", "Contracts"),
            new("equipment", "Equipment"),
            new("slas", "SLAs"),
            new("territories", "Territories"),
            new("rate-cards", "Rate Cards"),
            new("estimates", "Estimates"),
            new("pm", "PM Schedules"),
            new("van-stock", "Van Stock"),
            new("warranty", "Warranty"),
            new("fs-reports", "Reports"),
        }),
        new("reporting", "Reporting / BI", new List<PageEntry>
        {
            new("catalog", "Report Catalog"),
            new("executive", "Executive Dashboard"),
            new("viewer", "Report Viewer"),
            new("designer", "Statement Designer"),
            new("quick-query", "Quick Query"),
            new("drill-back", "Drill-Back Viewer"),
            new("scheduler", "Report Scheduler"),
            new("categories", "Categories"),
            new("parameter-sets", "Parameter Sets"),
            new("usage", "Usage Analytics"),
            new("sync-status", "Sync Status"),
        }),
        new("integration", "Integration", new List<PageEntry>
        {
            new("dashboard", "Dashboard"),
            new("partners", "Trading Partners"),
            new("webhooks", "Webhooks"),
            new("import-export", "Import / Export"),
        }),
        new("tax", "Malaysian Tax (SST)", new List<PageEntry>
        {
            new("tax-codes", "Tax Codes"),
            new("tax-rates", "Tax Rates"),
            new("sst-returns", "SST Returns"),
            new("tax-rules", "Tax Determination Rules"),
            new("exempt-supplies", "Exempt/Zero-Rated Supplies"),
            new("local-tables", "Local Code Tables"),
            new("tax-reports", "Tax Reports"),
            new("sst-registration", "SST Registration"),
            new("tax-calculator", "Tax Calculator"),
        }),
        new("einvoice", "e-Invoice (MyInvois)", new List<PageEntry>
        {
            new("taxpayer-profiles", "Company Profile"),
            new("dashboard", "Compliance Dashboard"),
            new("documents", "Documents"),
            new("queue", "Document Queue"),
            new("status", "Submission Status"),
            new("certificates", "Digital Certificates"),
            new("credentials", "API Credentials"),
            new("tin-validation", "TIN Validation"),
            new("consolidated", "Consolidated e-Invoice"),
            new("item-extensions", "Item Extensions"),
            new("customer-extensions", "Customer Extensions"),
            new("vendor-extensions", "Vendor Extensions"),
            new("reconciliation", "Reconciliation Report"),
            new("einvoice-reports", "e-Invoice Reports"),
        }),
    };
}
