# ERP User Guide — Common Tasks by Module

A practical, screen-by-screen reference for the day-to-day tasks in this ERP.
Routes use the left sidebar navigation. Every path below matches the live menu
(`src/navigation.tsx`), so "go to *Module → Sub-item*" = click that item in the sidebar.

> Tip: the sidebar only shows modules your role is allowed to see. If a screen
> is missing, ask an Admin to grant the role (Platform → Roles).

---

## How to read this guide

- **Navigation** = `Module` → `Sub-item` in the left menu (e.g. `Order Management → Sales Orders`).
- **Action buttons** follow a consistent pattern: `New` / `Add Line` to create, `Save` to persist,
  `Release` / `Post` / `Submit` to move a document to the next stage, `Print` / `Export` for output.
- **Search**: list pages have a search box at the top. On **Projects** and **Work Centers** the grid
  is searchable + sortable (click a column header to sort). On **Vendors** and **Customers** the search
  box has a clear (×) button and you can press `Esc` to clear it.
- **Forms**: required fields are marked with a red `*` and block `Save` until filled.

---

## WORKED EXAMPLE — The Sales Cycle (Quote → Order → Ship → Invoice → Payment)

This is the most common end-to-end flow. Follow it in order.

### Step 1 — Create a customer (if new)
`Accounts Receivable → Customers` → click **New Customer**.
Fill: Customer Code, Name, billing/shipping address, payment terms, tax info → **Save**.
*The customer now appears in the customer search on orders and invoices.*

### Step 2 — Optional: write a quote
`Order Management → Quotes` → **New Quote** (or `Order Management → New Quote`).
Pick the customer, add lines (item / qty / price) with **Add Line**, then **Save**.
From a quote you can convert to a sales order (use the order's "from quote" / copy action) or just
create the order directly in the next step.

### Step 3 — Create the sales order
`Order Management → Sales Orders` → **New Sales Order**.
- Select the **Customer** (type-ahead; clear with × if needed).
- **Add Line** for each item: item code, quantity, unit price, requested ship date, warehouse.
- Set shipping method / freight if prompted.
- **Save**, then **Release** (released orders are eligible to pick, pack, and ship).

### Step 4 — Pick, pack, and ship
Two equivalent paths:
- **Guided**: `Order Management → Pick/Pack/Ship` — pick the released order, confirm pick, pack, then ship.
- **Direct**: `Order Management → Shipments` → **New Shipment**, link the sales order, confirm the
  lines shipped, **Save** / **Confirm**. The shipment reduces available inventory.

> Shipping generates the fulfillment record that billing pulls from.

### Step 5 — Invoice the customer
`Accounts Receivable → Invoice Batches` → open (or create) a batch → **New Invoice**.
Link the shipped sales order / shipment; lines and amounts default from the order.
Review, **Save**, then **Post** the batch to create the AR invoice (customer balance increases,
revenue posts to GL).

### Step 6 — Receive and apply the payment
`Accounts Receivable → Cash Receipts` → **New Receipt** (or a batch).
- Enter customer, receipt date, amount, and payment method (check / ACH / card).
- **Apply** the receipt to the open invoice(s). Excess can be left on account or refunded.
- **Save** / **Post**. The customer balance drops and cash posts to the bank (Cash Management).

### Step 7 — (If needed) credit memos / disputes
- `Accounts Receivable → Credit / Debit Memos` to issue a memo.
- `Accounts Receivable → Apply Credit Memo` to offset a memo against an invoice.
- `Accounts Receivable → Collections` / `Dunning` to chase overdue balances;
  `Statements` to email customer statements.

**Result:** customer created → quote (optional) → sales order released → shipped → invoiced → paid,
with GL, inventory, and cash all updated.

---

## Per-Module Common Tasks

### Platform (Admin / System Admin only)
Setup and governance. Most common:
- `Companies` — create/maintain the legal entities you transact in.
- `Fiscal Periods` — open/close accounting periods (you can't post to a closed period).
- `Chart of Accounts` — maintain GL accounts; `Segment Types` / `Segment Values` for dimensional coding.
- `Currencies` / `Exchange Rates` — maintain money and rates.
- `Number Sequences` — document numbering (SO, PO, invoice, voucher, receipt).
- `Users` / `Roles` — grant access (Role = set of permissions). `Roles` page has a module filter
  and a permission matrix; the matrix edit is role-gated.
- `Approval Workflows` / `Approval Delegations` — routing and delegate coverage.
- `Audit Log` — see who changed what.
- `Period Close` / `Year-End Close` — close the books.

### General Ledger
The financial backbone.
- `Journal Batches` — enter manual/journal entries; **New Batch → New Entry**, then **Post**.
- `Recurring Templates` — auto-repeat entries.
- `Allocation Rules` — distribute costs (e.g. overhead) across segments; run allocations.
- `Budgets` / `Budget Roll-Forward` — build and carry budgets.
- `Revaluation` — revalue foreign-currency balances.
- `Account Inquiry` — drill into an account's activity.
- `Pre-Posting Review` / `Posting Suspense` — catch/resolve errors before they post.
- `Period-End Checklist` / `Year-End Close` — close procedures.
- `Reports` / `Consolidation` — financial statements and multi-company consolidation.

### Accounts Payable
Pay your vendors.
- `Vendors` — maintain vendors (search box has clear + Esc). **New Vendor**: code, name, terms,
  1099 category, remit-to bank.
- `Payment Terms` — net-30 etc.
- `Voucher Batches` — enter AP invoices (New Voucher → Add Line → Post). These create liabilities.
- `Three-Way Match` / `Match Exceptions` — match PO / receipt / invoice; resolve exceptions.
- `Payments` — select approved vouchers and pay (check / ACH); post the payment run.
- `1099` / `1099 Processing` / `Vendor W-9 / TIN` / `Backup Withholding` — year-end contractor reporting.
- `GR/IR Accrual` — goods-received / invoice-received clearing.
- `Duplicate Invoice` — block paying the same invoice twice.
- `Vendor Statements` / `Reports` — aging and activity.

### Accounts Receivable
Bill and collect from customers (see the Sales example above for the full flow).
- `Customers` — maintain customers (clearable search).
- `Invoice Batches` — bill shipped orders (Step 5 above).
- `Cash Receipts` — record and apply payments (Step 6).
- `Credit / Debit Memos` / `Apply Credit Memo` — adjustments.
- `Credit Limit` — set/check exposure; `Collections` / `Dunning` / `Finance Charges` for overdue.
- `Statements` — customer statements.
- `Aging by Basis` / `Receipt Match` / `Reports` — analysis.

### Cash Management
Bank and cash control.
- `Bank Accounts` — maintain accounts.
- `Deposits` — group receipts into a deposit slip.
- `Bank Statements` / `Reconciliations` — import statements and reconcile to the ledger.
- `Transfers` — move money between accounts.
- `Bank Fees` / `NSF` — record charges / bounced receipts.
- `Lockbox` / `Positive Pay` / `Stale Checks` — banking services.
- `Outstanding Deposits` / `Forecast Horizon` — cash position.

### Purchasing
Buy from vendors.
- `Requisitions` — request items (New Requisition → Add Line → Submit).
- `Purchase Orders` — create POs from requisitions or directly; **Release** to send to vendor.
- `Approval Queue` — approve pending POs (role-gated).
- `Receipts` — receive goods against a PO; this feeds inventory and the AP 3-way match.
- `Vendor Quotes` / `Vendor Items` / `Vendors` — sourcing.
- `PO Templates` / `Requisition Templates` — reuse common buys.
- `Shipping Methods` / `FOB Terms` / `Buyer Agents` — defaults.
- `Reports` — PO status, spend.

### Inventory
Track items and stock.
- `Items` / `Categories` / `UOM` / `UOM Conversions` — master data.
- `Warehouses` / `Bins` — locations.
- `Stock` / `Stock by Location` / `Stock Card` — on-hand and movement history.
- `Transactions` / `Movements` — manual adjustments.
- `Reservations` — hold stock for orders.
- `Cycle Counts` / `Physical Counts` / `Cycle Count Schedule` — audit accuracy.
- `Quarantine` / `Expiration` / `Scrap / Disposal` / `Negative Overrides` — exception handling.
- `Landed Cost` / `Landed Cost Allocations` — true item cost.
- `Reorder` / `Substitutions` / `Kits` / `Consignment` / `Put-away / Picking` — fulfillment.
- `Revaluation` / `GL Tie-Out` / `Reports` — valuation and reconciliation.

### Order Management
Sales order lifecycle (see Sales example).
- `Sales Orders` — the core order screen.
- `Quotes` / `New Quote` — pre-sale pricing.
- `Shipments` / `Pick/Pack/Ship` — fulfill (Steps 4).
- `Returns` / `Return-to-Vendor` — customer returns / RTV.
- `Blanket Orders` — ongoing release schedules.
- `Substitution Offers` — offer alternates.
- `Order Dashboard` / `Sales Analysis` / `Commissions` / `ATP` / `Freight` — insight.
- `Notes & History` — order timeline.
- `Masters` — OM setup (item/ship defaults).

### Bill of Materials
Build finished goods.
- `BOMs` — component structures.
- `Work Centers` — capacity/resources (grid is searchable + sortable + paginated).
- `Routing Operations` — step sequences.
- `Build Orders` — produce to stock; consumes components, adds finished goods.
- `Reports` — where-used, cost roll-up.

### Project Accounting
Job/site-based work.
- `Projects` — the project (searchable/sortable grid).
- `Tasks` — breakdown; `Budgets` — plan vs actual.
- `Costs` — labor/material/equipment.
- `Billing` — bill the customer (milestone / time & material).
- `Change Orders` — scope changes.
- `WIP / Analysis` — work-in-process and profitability.
- `Overview` / `Reports` — dashboard.

### Payroll
Pay people.
- `Employees` — worker master.
- `Timesheets` — capture hours.
- `Pay Codes` / `Deductions` / `Tax / W-4` / `PTO` / `Garnishments` / `Union / Certified` — setup.
- `Runs & Checks` — process a payroll run; `Manual Checks` for off-cycle.
- `Expenses` — reimbursements.
- `Setup & Config` / `Reports` — configuration and outputs.

### Field Service
Dispatch and on-site work.
- `Work Orders` — jobs; `Dispatch Board` — schedule technicians.
- `Technicians` / `Territories` / `Rate Cards` / `SLAs` — resourcing.
- `Contracts` / `Warranty` — coverage.
- `Equipment` / `Van Stock` — assets and truck inventory.
- `Estimates` / `PM Schedules` — quotes and preventive maintenance.
- `Reports` — field metrics.

### Reporting (BI)
- `Report Catalog` / `Report Viewer` — run reports.
- `Executive Dashboard` — KPIs.
- `Statement Designer` — lay out customer/vendor statements.
- `Quick Query` — ad-hoc data pull.
- `Drill-Back Viewer` — click a number to see the source transaction.
- `Report Scheduler` / `Parameter Sets` / `Usage Analytics` / `Sync Status` — automation and monitoring.

### Integration / EDI
`Integration` — import/export, webhooks, and EDI gateway (single landing page in this build).

---

## Quick "where do I go" index

| I want to… | Go to |
|---|---|
| Set up a new customer | Accounts Receivable → Customers |
| Write a sales quote | Order Management → Quotes → New Quote |
| Enter a sales order | Order Management → Sales Orders → New Sales Order |
| Ship product | Order Management → Pick/Pack/Ship (or Shipments) |
| Bill the customer | Accounts Receivable → Invoice Batches |
| Record a payment | Accounts Receivable → Cash Receipts |
| Add a vendor | Accounts Payable → Vendors → New Vendor |
| Enter a vendor bill | Accounts Payable → Voucher Batches |
| Pay vendors | Accounts Payable → Payments |
| Buy something | Purchasing → Purchase Orders (via Requisitions) |
| Receive goods | Purchasing → Receipts |
| Check stock | Inventory → Stock / Stock by Location |
| Count inventory | Inventory → Cycle Counts / Physical Counts |
| Post a journal | General Ledger → Journal Batches |
| Reconcile the bank | Cash Management → Reconciliations |
| Run payroll | Payroll → Runs & Checks |
| Manage users/roles | Platform → Users / Roles |
| Close the period | Platform → Period Close (GL → Period-End Checklist) |
| See a report | Reporting → Report Catalog / Executive Dashboard |

---

## Notes
- Document flow is sequential: a record must be **Saved** before it can be **Released/Posted**, and a
  downstream step (ship → invoice → payment) requires the upstream one to be done.
- Posted records are immutable — corrections use reversing entries or credit/debit memos, never edits.
- If a screen is blank or errors, confirm the period is open and your role has permission for that module.
