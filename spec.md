# Software Requirements Specification (SRS)
## Modern Project-Centric ERP — Modeled on Microsoft Dynamics SL (Solomon)

**Document version:** 1.0
**Status:** Draft for engineering planning
**Companion documents:** `architecture.md`, `todo.md`, `frontend.md`

---

## 1. Purpose & Background

### 1.1 Purpose
This specification defines the functional and non-functional requirements for a modern, web-based ERP system whose module set, business logic, and end-to-end workflows are modeled on **Microsoft Dynamics SL** (formerly **Solomon IV**), the Windows-based, SQL Server-native, project-accounting-focused ERP. The goal is not to clone the legacy UI, but to reproduce and modernize the **business capability** that made Dynamics SL a durable system of record for project-driven organizations (construction, engineering, government contracting, professional services), while removing its legacy constraints (Windows-only client, single-currency bias, no native web/mobile access).

### 1.2 Reference system background
- **Origin:** Founded as TLB, Inc. in 1980 (Findlay, Ohio), later Solomon Software. Released Solomon I–III on DOS, then **Solomon IV for Windows in 1994** — one of the first true Windows-native, multi-user accounting systems with deep job-costing/project-accounting capability.
- **Ownership history:** Acquired by **Great Plains Software (2000)**, then by **Microsoft (2001)** as part of the Great Plains acquisition, rebranded **Microsoft Dynamics SL**. Development/support was later outsourced to Solomon Cloud Solutions (2004).
- **Versions:** Solomon IV → Dynamics SL 6.0 → 6.5 → 7.0 → SL 2011 → SL 2015 → **SL 2018 (final release)**. SL 2011/2015 are already end-of-support; SL 2018 is a maintenance-only, "sunset" product for Microsoft, with Business Central / Finance & Operations positioned as the go-forward replacements.
- **Architecture being modernized:** Classic two-tier client/server. A Visual Basic-based fat client (one small executable per screen/process — highly modular but with no separate application/business-rule server tier) talks directly to a SQL Server database built natively for SQL Server (not ported from a flat-file system). Screens and reports are identified by a `module.screen.version` numbering scheme (e.g., `03.010.00` = Accounts Payable Voucher and Adjustment Entry, `01.610.00` = GL Trial Balance, `98.290.00` = Database Maintenance in System Manager). This spec's module list preserves this same functional decomposition as *bounded contexts*, not as literal screen numbers.
- **Editions:** Historically sold as **Business Essentials** (core financials + distribution) and **Advanced Management** (adds deep project accounting, billing, and forecasting), with several modules available "à la carte" on top of Business Essentials. This SRS treats that split as a **licensing/feature-flag boundary**, not a technical one — every module below should be buildable and toggleable independently.

### 1.3 Product vision
Build a cloud-deployable, API-first, multi-company, multi-currency ERP that:
1. Preserves the accounting rigor of the legacy system (batch/voucher-based sub-ledgers, strict GL tie-out, period-based closing, full drill-back from GL to source transaction).
2. Preserves the project-accounting depth that differentiated Dynamics SL (multi-tier billing, cost allocation, revenue recognition, certified payroll, contract/subcontract management).
3. Replaces the legacy client with a responsive web application usable from any device, with no loss of the "fast data-entry grid" experience power users expect.
4. Exposes every business capability as a documented API so it can be automated, integrated, or embedded.

---

## 2. Scope

### 2.1 In scope
All modules and workflows listed in Section 5, their CRUD masters, their transactional/batch processes, their standard reports, and the cross-module postings that tie them together in the General Ledger.

### 2.2 Out of scope (v1)
- Full discrete/process manufacturing (MRP, shop floor, routing) — only light Bill of Materials (kitting/assembly) is in scope.
- Full HRIS (benefits administration, applicant tracking) — Payroll is in scope, broader HR is not.
- Native CRM — only a CRM integration boundary (equivalent to the legacy "Microsoft CRM Integration") is in scope.
- Country-specific localizations beyond US GAAP / US payroll & tax (multi-currency is in scope; multi-jurisdictional statutory reporting is not, in v1).

### 2.3 Assumptions
- Single database of record per deployment (or per tenant, in a multi-tenant hosting model); Multi-Company is a first-class concept *within* that database, not a separate deployment per company.
- Double-entry accounting throughout; every sub-ledger transaction that affects the balance sheet or income statement must generate a balanced GL entry.
- All monetary transactions are auditable and, once posted, are corrected only via reversing/adjusting entries — never by deleting posted history.

---

## 3. Actors / Roles

| Role | Description |
|---|---|
| System Administrator | Configures companies, fiscal calendars, chart of accounts, security roles, integrations. |
| Controller / Finance Manager | Owns GL, period close, financial reporting, consolidations. |
| AP Clerk | Enters vendor invoices/vouchers, processes payment runs. |
| AR Clerk / Billing Specialist | Enters customer invoices, applies cash receipts, manages collections. |
| Purchasing Agent / Buyer | Creates requisitions and purchase orders, manages vendor pricing. |
| Warehouse / Inventory Clerk | Receives goods, issues stock, performs cycle counts. |
| Project Manager | Owns project budgets, change orders, billing approval, margin tracking. |
| Payroll Administrator | Processes timesheets, payroll runs, certified payroll reporting. |
| Field Technician / Dispatcher | Manages service calls, work orders, time & materials capture. |
| Executive / Report Viewer | Read-only access to dashboards and financial/operational reports. |
| External Vendor/Customer (Portal) | Self-service PO acknowledgment, invoice status, statements. |
| API/Integration Client | Machine identity used by EDI, middleware, or custom automation. |

---

## 4. High-Level Module Map

Grouped exactly as the legacy product grouped its "Business Ready Licensing" guide, so the mapping to a modernization backlog stays traceable:

1. **Financial Management** — General Ledger, Accounts Payable, Accounts Receivable, Cash Management/Bank Reconciliation, Currency Manager, Multi-Company, Fixed Assets (extension), Allocation/Recurring Entries.
2. **Supply Chain / Distribution Management** — Purchasing, Requisitions, Order Management (Sales), Inventory, Landed Cost, Bill of Materials, Advanced Shipping, EDI/eCommerce Gateway.
3. **Project Management (Project Accounting)** — Project Controller, Project Allocator, Project Analyzer, Flexible Billings, Project Budgeting, Time and Expense, Contract/Subcontract Management, Employee Utilization.
4. **Payroll & Workforce** — Payroll, Time and Expense for Projects, Certified Payroll/Union Reporting.
5. **Field Service Management** — Service Dispatch, Work Order, Contract/Warranty tracking.
6. **Business Intelligence & Reporting** — Native Report Viewer, Report Manager/Designer, Quick Query Editor/Viewer, Management-Reporter-style financial statement designer, embedded BI/dashboards, drill-back.
7. **Platform / System Services** — System Manager (security, company/database admin), Customization Manager (form/field extensibility without core changes), Web Services/API layer, Business Portal (self-service web access), Import/Export & Integration Manager, Workflow/Approvals, Audit Trail.

---

## 5. Detailed Module Specifications

Each module below lists: **Purpose**, **Core Entities**, **Key Transactions/Processes**, **Business Rules**, **Standard Reports**, **Integration Points**.

### 5.1 Shared/System Information (foundation module — build first)
- **Purpose:** Company, fiscal calendar, chart-of-accounts segment structure, currency, and security foundation every other module depends on. Legacy equivalent: "Shared Information" (e.g., Flexkey Table Maintenance) + "System Manager."
- **Core Entities:** Company, Fiscal Year/Period, Chart of Accounts (with configurable **segments** — analogous to legacy "Flexkeys": Account, Sub-account/Cost Center, Department, Project, etc.), Currency & Exchange Rate Table, Numbering Sequences, Security Role, User, Screen/Field Permission.
- **Key Transactions:** Create/close fiscal period; define segment structure and validation combinations; maintain exchange rate tables; assign roles to users; configure approval workflow thresholds.
- **Business Rules:** Segment combinations must validate against an approved combination table before any transaction can post to them (equivalent to legacy "wildcard"/cross-reference validation). Only one fiscal period may be "current" per company at a time; prior periods can be reopened only by an administrator role, and the action is logged.
- **Standard Reports:** Company Setup Listing, Chart of Accounts Listing, Fiscal Calendar, Security/Role Matrix, Audit Trail Report.
- **Integration Points:** Every module reads Company/Period/Segment/Currency master data from here; Security enforces API-level and UI-level permissions.

### 5.2 General Ledger (GL)
- **Purpose:** System of record for all financial postings; source of the trial balance and financial statements.
- **Core Entities:** Account, Journal Batch, Journal Entry (header/detail), Recurring Entry Template, Allocation Rule, Budget.
- **Key Transactions/Processes:** Manual journal entry; batch entry & batch release; **posting** (the irreversible action that moves a batch from draft to permanent ledger history — legacy equivalent of "Post Transactions"); recurring entry generation; allocation processing; period close/roll-forward; consolidation across companies; multi-currency revaluation.
- **Business Rules:** A batch must balance (debits = credits) before it can be released; posted entries are immutable — corrections require a reversing entry; sub-ledgers (AP, AR, Inventory, Payroll, Project) post to GL only through controlled interfaces, never by direct user entry into control accounts.
- **Standard Reports:** Trial Balance, Detail/Summary General Ledger, Unposted Transactions, Balance Sheet, Income Statement, Statement of Cash Flows, Budget vs. Actual, Account Distribution.
- **Integration Points:** Receives posted summaries from every sub-ledger; feeds the financial-statement designer (Section 5.13) and consolidation engine.

### 5.3 Accounts Payable (AP)
- **Purpose:** Manage vendor obligations from invoice receipt through payment; voucher-based (i.e., every payable is a "voucher," not a bare invoice record — the legacy system's terminology and control model).
- **Core Entities:** Vendor, Voucher (invoice/credit/debit adjustment), Payment (check/ACH/wire), 1099 Category, Payment Term, Vendor Bank Account.
- **Key Transactions/Processes:** Voucher/adjustment entry; quick-voucher entry; payment selection & batch check/ACH run; void/reissue payment; 1099 processing and e-filing; backup withholding; vendor aging.
- **Business Rules:** A voucher cannot be selected for payment until its batch is posted; discounts must respect payment terms; a void payment must reverse the original GL distribution automatically; vendor 1099 totals must reconcile to the GL vendor-control account.
- **Standard Reports:** AP Aging, Vendor Trial Balance, AP Batch Register, Cash Requirements, 1099 Summary/Detail, Check Register, Account Distribution.
- **Integration Points:** Purchasing (3-way match: PO ↔ Receipt ↔ Voucher); Project Controller (cost distribution to jobs); Cash Management (payment clearing); GL (control account postings).

### 5.4 Accounts Receivable (AR)
- **Purpose:** Manage customer invoicing, cash application, and collections.
- **Core Entities:** Customer, Invoice, Credit/Debit Memo, Cash Receipt, Finance Charge, Collection Note.
- **Key Transactions/Processes:** Invoice entry (manual or generated from Order Management/Project Billing); cash receipt entry & auto/manual application; unapplied cash handling; finance charge calculation; write-off/adjustment; statement generation; collections workflow.
- **Business Rules:** An invoice cannot be voided once cash has been applied to it without first unapplying; customer credit limit checks fire at order/invoice entry; AR aging buckets are period-end driven, not just day-count driven, to match legacy behavior.
- **Standard Reports:** AR Aging, Customer Statement, Cash Receipts Journal, Sales Journal, Customer Trial Balance, Finance Charge Report.
- **Integration Points:** Order Management (shipment → invoice), Project Controller/Flexible Billings (project invoice → AR), Cash Management (deposit reconciliation), GL.

### 5.5 Cash Management / Bank Reconciliation
- **Purpose:** Track cash position across bank accounts and reconcile to bank statements.
- **Core Entities:** Bank Account, Deposit, Reconciliation Session, Bank Transaction Import Line.
- **Key Transactions/Processes:** Deposit entry; bank statement import (BAI2/OFX/CSV); auto-match to AP payments/AR receipts; manual reconciliation; positive pay file export.
- **Business Rules:** Reconciled items lock against further edits; unreconciled variance must be explained/adjusted through GL, not silently dropped.
- **Standard Reports:** Bank Reconciliation Report, Cash Position/Forecast, Outstanding Checks/Deposits.
- **Integration Points:** AP (checks/ACH), AR (deposits), GL.

### 5.6 Currency Manager & Multi-Company
- **Purpose:** Support multi-currency transactions and multi-entity consolidation.
- **Core Entities:** Currency, Exchange Rate Table, Company Hierarchy, Intercompany Due-to/Due-from Mapping.
- **Key Transactions/Processes:** Rate table maintenance; realized/unrealized gain-loss revaluation at period end; intercompany transaction posting with automatic due-to/due-from balancing; consolidated reporting across companies.
- **Business Rules:** Every foreign-currency transaction stores both transaction-currency and functional-currency amounts at the rate in effect on the transaction date; consolidation must eliminate intercompany balances.
- **Standard Reports:** Multi-Currency Revaluation Report, Consolidated Trial Balance, Intercompany Balance Report.

### 5.7 Purchasing & Requisitions
- **Purpose:** Manage the procure-to-pay cycle from internal request through PO issuance and receipt.
- **Core Entities:** Requisition, Purchase Order (header/line), Vendor Item/Pricing, Receipt.
- **Key Transactions/Processes:** Requisition entry & approval routing; PO creation (manual, from requisition, or from reorder point); PO change orders; goods receipt (full/partial); 3-way match exception handling; blanket/standing POs.
- **Business Rules:** A PO line cannot be received beyond a configurable over-receipt tolerance without approval; a requisition above threshold requires manager approval before it can generate a PO.
- **Standard Reports:** Open PO Report, PO Status, Requisition Status, Receiving Report, Vendor Performance.
- **Integration Points:** Inventory (receipt increases on-hand), AP (3-way match), Project Controller (PO committed cost against project budget).

### 5.8 Inventory
- **Purpose:** Track on-hand quantity, cost, and movement of stocked items across warehouses.
- **Core Entities:** Item Master, Warehouse/Bin, Lot/Serial, Inventory Transaction (receipt, issue, transfer, adjustment, cycle count), Costing Layer (FIFO/LIFO/Average/Standard).
- **Key Transactions/Processes:** Item receipt/issue/transfer; physical/cycle count and variance posting; landed cost allocation; item revaluation; reorder point/replenishment suggestion.
- **Business Rules:** Negative on-hand requires explicit permission; costing method is set per item and cannot be changed mid-year without a controlled revaluation process.
- **Standard Reports:** Stock Status, Inventory Valuation, Transaction History, Cycle Count Variance, Reorder Report.
- **Integration Points:** Purchasing (receipts), Order Management (shipments), GL (inventory/COGS postings), Bill of Materials.

### 5.9 Order Management (Sales Order Processing)
- **Purpose:** Manage the order-to-cash cycle for goods/services sold to customers.
- **Core Entities:** Sales Order (header/line), Shipment, Return Material Authorization (RMA), Pricing/Discount Rule.
- **Key Transactions/Processes:** Order entry with credit-limit/availability check; pick/pack/ship; partial shipment & back-order handling; invoice generation from shipment; return processing and restocking.
- **Business Rules:** An order cannot ship more than the confirmed quantity without an override; invoice must be generated before or concurrently with shipment recognition per revenue-recognition policy.
- **Standard Reports:** Open Order Report, Backorder Report, Shipment Register, Sales Analysis.
- **Integration Points:** Inventory (allocation/shipment), AR (invoice), Advanced Shipping/EDI.

### 5.10 Bill of Materials
- **Purpose:** Light assembly/kitting support — build a finished item from component items.
- **Core Entities:** BOM Header, Component Line, Assembly/Disassembly Transaction.
- **Key Transactions/Processes:** Build (assemble) transaction consuming components and producing the parent item; disassemble/unbuild; BOM costing roll-up.
- **Business Rules:** A build transaction cannot complete if any component is short unless a negative-inventory override is granted.
- **Standard Reports:** BOM Where-Used, Assembly Cost Roll-up, Build Transaction History.

### 5.11 Project Accounting Suite (Project Controller / Allocator / Analyzer / Flexible Billings / Budgeting)
This is the differentiating capability of the legacy system and should be treated as its own bounded context with the richest business-rule set in the platform.
- **Purpose:** Track cost and revenue at the project/task level, independent of and reconciled to the company-level GL, and drive project billing.
- **Core Entities:** Project, Task/Phase, Account Category (project-level chart of accounts overlay), Budget (original/revised/EAC/FAC), Cost Transaction, Allocation Rule, Billing Schedule/Contract Line, Change Order, Subcontract, Employee Utilization Record.
- **Key Transactions/Processes (the canonical flow):**
  1. A cost transaction posts simultaneously to the **company GL** (e.g., debit Materials Expense/credit AP) **and** to the **project ledger** (same amount against the project's Materials category).
  2. **Project Allocator** applies markup/burden rules (e.g., a configurable % markup) to compute billable amount and posts the resulting unbilled AR / unbilled revenue entries at the project level.
  3. **Flexible Billings** selects unbilled amounts (by contract type — Time & Materials, Cost-Plus, Fixed-Price/Schedule-of-Values, Milestone/Scheduled Billing) and generates a customer invoice.
  4. Posting the invoice creates GL entries (AR / Revenue) **and** relieves the project's unbilled AR/revenue balance.
  5. **Project Analyzer** provides margin, budget-vs-actual, and estimate-at-completion (EAC) analysis at any point without waiting for period close.
- **Business Rules:** Every project transaction must net to zero variance between the project ledger and GL at period end (a required reconciliation, mirroring the legacy "Project Controller ties to GL" control); budgets are versioned (original vs. revised) and changes require a Change Order record with approval; certified payroll and prevailing-wage rules apply to labor postings on government contracts; subcontractor retainage is tracked separately from base subcontract cost.
- **Standard Reports:** Project Profitability (Budget vs. Actual vs. EAC), WIP (Work-in-Progress) Schedule, Unbilled AR/Revenue Report, Billing Register, Change Order Log, Employee Utilization, Subcontract Status, Certified Payroll Report.
- **Integration Points:** Every cost-generating module (AP, Payroll, Inventory issues, Purchasing commitments) posts into the project ledger; AR receives generated invoices; GL receives the company-level mirror of every project posting.

### 5.12 Payroll & Time/Expense
- **Purpose:** Process employee pay, timesheets, and expense reports, with project/task labor distribution.
- **Core Entities:** Employee, Pay Code, Timesheet, Expense Report, Payroll Run/Check, Tax Table, Deduction/Benefit, Union/Certified-Payroll Profile.
- **Key Transactions/Processes:** Time entry (by project/task where applicable); expense report entry & approval; payroll calculation run; check/direct-deposit generation; tax filing exports; certified payroll (e.g., Davis-Bacon-style) reporting; union reporting.
- **Business Rules:** Time charged to a project must validate against an open project/task and (optionally) an approved budget line; payroll runs are batch/versioned so a run can be voided and recalculated before final posting; direct deposit requires pre-note validation.
- **Standard Reports:** Payroll Register, Tax Liability Report, Certified Payroll Report, Union Report, Time & Expense by Project.
- **Integration Points:** Project Controller (labor cost distribution), GL (payroll liability/expense postings), AP (benefit/tax remittance).

### 5.13 Field Service Management
- **Purpose:** Manage service calls, dispatch, and work orders, often billed through the same Project/AR pipeline.
- **Core Entities:** Service Contract/Warranty, Work Order, Dispatch Board Entry, Technician, Equipment/Asset serviced.
- **Key Transactions/Processes:** Service call intake; dispatch assignment/scheduling; work order completion with parts/labor capture; warranty/contract coverage check; billing hand-off to AR/Project Billing.
- **Business Rules:** Parts consumed on a work order must relieve Inventory; labor captured must respect technician availability/scheduling conflicts.
- **Standard Reports:** Work Order Status, Technician Utilization, Contract Coverage/Expiration, Service Billing Summary.

### 5.14 Business Intelligence & Reporting
- **Purpose:** Provide the reporting depth the legacy system delivered via **Crystal Reports** (900+ standard reports, customizable), a **Management-Reporter-style** financial statement designer with drill-back, and ad hoc **Quick Query** access, modernized into a web reporting/BI layer.
- **Core Entities:** Report Definition, Report Parameter Set, Financial Statement Layout (rows/columns/formulas over the GL), Saved Query, Dashboard, Scheduled Report Subscription.
- **Key Transactions/Processes:** Run report with parameters (company/period/segment filters); design/modify a financial statement layout with drill-back to the underlying journal detail; schedule recurring report delivery (email/portal); export to Excel/PDF/CSV.
- **Business Rules:** Every "summary" report must support drill-back to the transaction(s) that produced the number, matching the legacy system's drill-down expectation; report access respects the same row/field-level security as the underlying module.
- **Standard Reports (categories, not exhaustive):** All reports listed under each module above, plus company-level dashboards (cash position, AR/AP aging summary, project portfolio margin, budget vs. actual).
- **Integration Points:** Reads from every module's data store (ideally through read replicas or a reporting data mart, not the live transactional database — see `architecture.md`).

### 5.15 Platform / System Services
- **Purpose:** Cross-cutting services every module depends on.
- **Core Entities:** User, Role, Permission, Approval Workflow Definition, Audit Log Entry, Integration Endpoint/Webhook, Import/Export Job, Customization Definition (custom field/screen extension — legacy equivalent of "Customization Manager," which let admins change screens without touching core code).
- **Key Transactions/Processes:** User/role provisioning; approval workflow configuration (thresholds, routing); audit log query; API key/webhook management; bulk import/export (CSV/Excel/EDI) with validation and error reporting; extensibility — add custom fields to any entity without a core code change.
- **Business Rules:** All create/update/delete/post actions on financial entities are written to an immutable audit log (who/when/before-after); role permissions are enforced at the API layer, not just the UI.
- **Standard Reports:** Audit Trail, User Activity Log, Import/Export Job History, Security Role Matrix.

---

## 6. End-to-End Business Flows

### 6.1 Procure-to-Pay (P2P)
Requisition → Approval → Purchase Order → Goods Receipt (Inventory ↑) → Voucher (3-way match against PO & Receipt) → Payment Selection → Payment Run → Bank Reconciliation → GL postings at each step (accrual on receipt, liability on voucher, cash relief on payment).

### 6.2 Order-to-Cash (O2C)
Sales Order (credit check) → Allocation/Pick → Shipment (Inventory ↓, COGS recognized) → Invoice (AR ↑, Revenue ↑) → Cash Receipt & Application → Bank Deposit/Reconciliation → GL postings at each step.

### 6.3 Project-to-Bill (the signature legacy flow, detailed in 5.11)
Cost incurred (AP voucher, Payroll, Inventory issue, or Subcontract invoice) → simultaneous GL + Project ledger posting → Allocator applies markup/burden → Unbilled AR/Revenue recognized at project level → Flexible Billings generates invoice per contract type (T&M, Cost-Plus, Fixed-Price/SOV, Milestone) → Invoice posts to AR & GL, relieves unbilled balance → Cash receipt closes the loop → Project Analyzer reports margin/EAC at any time in the cycle.

### 6.4 Financial Period Close
Sub-ledger cutoff (AP/AR/Inventory/Payroll batches posted or explicitly held) → Sub-ledger-to-GL tie-out reports (e.g., Vendor Trial Balance vs. GL control account, mirroring the legacy year-end checklist pattern) → Allocations & recurring entries generated → Currency revaluation → Adjusting entries → Trial balance review → Financial statements generated → Period locked (no further posting without administrator reopen) → Next period opened.

### 6.5 Payroll Cycle
Timesheet/expense entry & approval → Labor distribution to projects → Payroll calculation (draft) → Review/edit → Final calculation & check/direct-deposit generation → GL posting (wage expense, tax/benefit liabilities) → Tax filing export → Certified payroll/union reporting where applicable.

---

## 7. Non-Functional Requirements

| Category | Requirement |
|---|---|
| Auditability | Every financial posting is traceable to its originating transaction and user; posted history is immutable (correct via reversal only). |
| Multi-company | A single deployment supports multiple legal entities with independent charts of accounts, fiscal calendars, and currencies, plus intercompany elimination. |
| Multi-currency | Full transaction-currency + functional-currency dual storage; period-end revaluation. |
| Security | Role-based access control down to field level on financial screens; SSO (OIDC/SAML) support; all financial actions require authenticated, authorized identity (human or service). |
| Compliance | US GAAP-consistent double-entry accounting; 1099 processing; certified payroll / prevailing wage reporting; SOX-friendly audit trail and segregation-of-duties controls on approval workflows. |
| Performance | Grid-based batch entry screens (e.g., voucher entry, journal entry) must support at least 500 lines per batch with sub-second row operations. |
| Scalability | Support at minimum 500 concurrent users and 10M+ GL transaction lines per company per year without degradation. |
| Availability | 99.9% uptime target for production; scheduled maintenance windows communicated in advance. |
| Extensibility | Custom fields and custom validation rules addable to any entity without modifying core module code (legacy "Customization Manager" equivalent). |
| Interoperability | REST/GraphQL API for every module; EDI gateway for PO/Invoice/ASN document types; webhook events for key lifecycle transitions (posted, approved, shipped, paid). |
| Data Retention | Financial transaction history retained indefinitely (or per configurable statutory retention policy) with cost-effective cold storage for aged data. |

---

## 8. Core Data Model (Summary)

- **Company** 1—* **Fiscal Period**
- **Company** 1—* **Chart of Accounts Segment Value** (Account, Sub-account/Cost Center, Department, Project, ...)
- **GL Journal Batch** 1—* **Journal Entry Line** → references Segment Value combination
- **Vendor** 1—* **Voucher** 1—* **Voucher Distribution Line** (→ GL account / Project / Task)
- **Customer** 1—* **Invoice** 1—* **Invoice Line** (→ GL account / Project / Task)
- **Project** 1—* **Task** 1—* **Cost Transaction** / **Billing Line**
- **Item** 1—* **Inventory Transaction** (→ Warehouse, Lot/Serial, Cost Layer)
- **Purchase Order** 1—* **PO Line** 1—* **Receipt Line** 1—* **Voucher Distribution Line** (3-way match chain)
- **Sales Order** 1—* **Order Line** 1—* **Shipment Line** 1—* **Invoice Line** (O2C chain)
- **Employee** 1—* **Timesheet Line** (→ Project/Task) and 1—* **Payroll Check** 1—* **Check Distribution Line**

All "Distribution Line" / posting-producing tables share a common `GLPostingReference` shape: company, period, account, sub-account/segments, amount, currency, source module, source document ID — this is the join point for the entire General Ledger and is the most important shared contract in the system (see `architecture.md` §5 for the canonical posting-event schema).

---

## 9. Glossary

| Term | Meaning |
|---|---|
| Voucher | An AP payable record (invoice, credit, or debit adjustment) awaiting or after posting; the legacy system's core AP transaction unit. |
| Batch | A group of entries (journal, voucher, invoice, etc.) that must balance and is posted as a unit. |
| Post/Posting | The irreversible action of moving a batch from draft into permanent ledger history. |
| Flexkey / Segment | A configurable component of the chart-of-accounts key (e.g., natural account, department, project) used to build the full posting account string. |
| Unbilled AR/Revenue | Project-level asset/revenue recognized for work performed but not yet invoiced to the customer. |
| EAC | Estimate at Completion — a forecasted total cost/revenue for a project based on actuals-to-date plus remaining estimate. |
| 3-Way Match | Validation that a payable voucher agrees with both its Purchase Order and its Goods Receipt before payment. |
| Certified Payroll | Government-contract labor reporting (e.g., U.S. Davis-Bacon Act) proving prevailing wages were paid. |
| Drill-back | The ability to navigate from a summarized report line to the individual transactions that compose it.
