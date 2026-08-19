# Frontend Implementation Plan
## UI/UX, Framework Choices, and Step-by-Step Frontend TODO

**Depends on:** `spec.md` (modules/flows), `architecture.md` (API contracts), `todo.md` (backend endpoints this UI consumes)

---

## 1. Design Brief: What "Good" Looks Like Here

The legacy system's UI (Windows Forms/VB, grid-heavy, keyboard-driven) was fast for power users but dated, Windows-only, and unfriendly to new hires. The goal for this frontend is:

- **As fast to enter data as the legacy grid screens** (batch entry, keyboard navigation, lookups) — power users should never feel the new system is slower.
- **Far easier to learn** than the legacy screens — clear visual hierarchy, guided empty states, inline help, modern form patterns — so new employees ramp up in hours, not weeks.
- **Responsive** — usable on a laptop at a desk, a tablet in a warehouse, or a phone in the field (Field Service, time entry, approvals).
- **Trustworthy-looking** — financial software needs a visual language that reads as precise and serious (clear numeric alignment, unambiguous status states, no playful/rounded "consumer app" styling that undermines confidence in the numbers).

---

## 2. Recommended Frontend Framework Stack

| Concern | Recommendation | Rationale |
|---|---|---|
| Core framework | **React 18+ with TypeScript**, built with **Vite** | Largest ecosystem for the specialized data-grid/form components this app needs; TypeScript catches contract mismatches against the strongly-typed backend (`architecture.md` recommends .NET/typed APIs). |
| Routing | **React Router v6** (or Next.js App Router if SSR/SEO for the vendor/customer portal is desired) | Simple client-side routing is sufficient for an authenticated internal app; consider Next.js specifically for the public-facing Vendor/Customer Portal (Section 5.15/§6 of `spec.md`) where SSR and SEO matter. |
| Styling / design system | **Tailwind CSS** + **shadcn/ui** component primitives | Utility-first styling gives fast, consistent visual iteration; shadcn/ui provides accessible, unstyled-by-default primitives (dialogs, dropdowns, comboboxes) that are easy to re-skin into a "financial software" look rather than a generic consumer-app look. |
| Data grid (the single most important component) | **TanStack Table** (headless) for custom batch-entry grids; **AG Grid** (Community, upgrade to Enterprise if needed) where Excel-like features are required (copy/paste ranges, cell range fill, column pinning, Excel export) | Batch/voucher/journal entry screens (`spec.md` §5.2, 5.3) are the direct modern replacement for the legacy's core grid-entry screens — this is the highest-investment component in the whole frontend. |
| Forms & validation | **React Hook Form** + **Zod** schemas (shared/generated from backend OpenAPI where possible) | Performant for large forms with many fields (vendor master, project master); Zod schemas can mirror backend validation so errors are consistent client/server. |
| Server-state / data fetching | **TanStack Query (React Query)** | Caching, background refetch, optimistic updates for CRUD; pairs naturally with the REST API from `architecture.md` §7. |
| Client/UI state | **Zustand** (lightweight) for cross-component UI state (active company, active period, open batch drafts) | Avoids Redux boilerplate; keeps server state (React Query) and UI state (Zustand) cleanly separated. |
| Charts/dashboards | **Recharts** for standard charts; **Plotly** or **D3** for anything highly custom (e.g., project margin waterfall) | Covers the BI/reporting module's dashboard needs (`spec.md` §5.14). |
| Icons | **lucide-react** | Consistent, clean icon set that pairs well with shadcn/ui. |
| Auth | **MSAL.js** (if Azure Entra ID) or a generic OIDC client (`oidc-client-ts`) | Matches `architecture.md` §6 SSO recommendation. |
| Testing | **Vitest** + **React Testing Library** (unit/component), **Playwright** (E2E) | Playwright is particularly important for testing multi-step transactional flows (voucher entry → post → GL verification) end-to-end. |
| Internationalization | **i18next** (even if v1 ships English-only, wire it in from day one) | Multi-currency (`spec.md` §5.6) users are very likely to eventually need multi-language too. |
| Build/monorepo tooling | **Turborepo** or **Nx** if the frontend is split into multiple deployables (main app, vendor/customer portal, field service mobile web) | Shared component library and API client across apps. |

### Why not a low-code/admin-generator framework?
Admin-panel generators (e.g., generic CRUD scaffolders) are tempting for the many "master data CRUD" screens in `spec.md`, and this plan does recommend building an internal **generic CRUD scaffold component** (Section 4) to move fast on those — but the **transactional/batch-entry screens** (vouchers, journal entries, project billing) have enough custom business logic and grid behavior that a generic admin generator would fight against the requirements. Use a hybrid: generated CRUD list/detail screens for masters, hand-built screens for transactional/batch work.

---

## 3. Core UX Patterns (apply consistently across every module screen)

1. **Global "combo" lookup component** — a searchable typeahead/combobox for any foreign-key field (vendor, customer, item, account, project) that supports keyboard-only selection (type → arrow keys → Enter), directly replacing the legacy "possible values" (F3) lookup pattern power users already expect.
2. **Keyboard-first grid navigation** — Tab/Shift+Tab and Enter move between cells in batch-entry grids exactly like a spreadsheet; support copy/paste of a range from Excel into a grid (critical for adoption by accounting staff migrating off the legacy system or off Excel workarounds).
3. **Draft-batch autosave** — any multi-line entry (journal batch, voucher batch, invoice batch) autosaves as a draft so a browser refresh/crash never loses work; explicit "Release/Post" remains a deliberate, confirmed action (never autosaved into posted state).
4. **Inline validation, not just on-submit** — segment-combination validation (`spec.md` §5.1) and balance-check (`spec.name` batches must balance) should show live in the grid, with a running "Out of Balance: $X" indicator, mirroring the immediate feedback style of the legacy grid screens.
5. **Company/Period switcher** — persistent header control showing active Company and active Fiscal Period, since almost every screen is scoped by both.
6. **Universal search** — a single search bar (Cmd/Ctrl+K) that queries across vendors, customers, items, projects, and transaction numbers, backed by the search-index service in `architecture.md` §3 (Quick Query equivalent).
7. **Drill-back everywhere** — every report/dashboard number that can be traced to source transactions (`spec.md` §5.14) should be clickable, opening a transaction list, not just static text.
8. **Approval banners** — any record pending approval (requisition, change order, expense report) shows a clear status banner and action buttons for approvers, consistent across modules.
9. **Favorites/recents navigation** — a pinned "favorite screens" and "recently visited" list in the main navigation, replacing the legacy "screen list" launcher concept in a more discoverable way.
10. **Consistent status vocabulary and color coding** — Draft (gray) → Pending Approval (amber) → Approved/Released (blue) → Posted (green) → Voided/Reversed (red), used identically across every module.

---

## 4. Visual Design Direction

- **Typography:** A clean, high-legibility sans-serif (e.g., Inter or IBM Plex Sans) for UI text; a **tabular-figure numeric font style** (`font-variant-numeric: tabular-nums`) for every monetary/quantity column so numbers align perfectly in grids — this single detail matters enormously for a system whose users stare at columns of numbers all day.
- **Color palette:** A restrained, low-saturation base (neutrals + one primary brand color) with color reserved almost entirely for **status meaning** (see §3.10) rather than decoration — avoid the "playful SaaS" palette trend; lean toward the calmer, denser visual language of professional financial tools.
- **Density modes:** Offer a **Comfortable** and **Compact** density toggle; power users entering hundreds of transaction lines a day will prefer Compact (mirrors the information density of the legacy grid screens), while occasional users (project managers checking a dashboard) will prefer Comfortable.
- **Dark mode:** Supported from the design-token layer up (Tailwind + CSS variables), since finance/ops staff often work in dim back-office environments and long grid-reading sessions benefit from reduced glare.
- **Print/PDF styles:** Every report and invoice/statement needs a dedicated print stylesheet or server-rendered PDF template (`spec.md` §5.14) — financial documents leaving the system (invoices, statements, checks) must look professional regardless of what the on-screen theme looks like.
- **Empty/first-run states:** Every list screen needs a designed empty state with a clear "how do I create the first one" call to action — a deliberate improvement over the legacy system's blank-grid-with-no-guidance default.

---

## 5. Screen Inventory (maps 1:1 to `todo.md` backend phases)

For each module below: **List/Search screen**, **Detail/Entry screen** (or **Batch Entry grid** for transactional work), and any **specialized workflow screens**.

| Module (todo.md phase) | List/Search | Detail/Entry | Batch/Workflow screens |
|---|---|---|---|
| Platform/System (Ph.1) | Users, Roles, Companies, Segments list | User/Role/Company/Segment detail forms | Period Close wizard, Approval Workflow designer |
| General Ledger (Ph.2) | Chart of Accounts, Journal Batches | Account detail, Journal Entry form | Batch Entry grid, Recurring Entry setup, Allocation Rule builder, Consolidation run screen |
| Accounts Payable (Ph.3) | Vendors, Vouchers, Payments | Vendor detail, Voucher form | Voucher Batch Entry grid, Payment Selection & Run wizard, 1099 processing screen |
| Accounts Receivable (Ph.4) | Customers, Invoices, Receipts | Customer detail, Invoice form | Invoice Batch Entry grid, Cash Receipt & Application screen, Statement run screen |
| Cash Management (Ph.5) | Bank Accounts, Reconciliations | Bank Account detail | Bank Reconciliation workspace (drag-to-match UI) |
| Purchasing (Ph.6) | Requisitions, POs | Requisition form, PO form | Approval queue, Goods Receipt entry |
| Inventory (Ph.7) | Items, Warehouses, Transactions | Item detail | Receipt/Issue/Transfer entry grid, Cycle Count workspace |
| Order Management (Ph.8) | Sales Orders, Shipments | Order entry form | Pick/Pack/Ship workspace, Backorder management |
| Bill of Materials (Ph.9) | BOMs | BOM detail/component grid | Build/Disassemble transaction screen |
| Project Accounting (Ph.10) | Projects, Contracts, Change Orders | Project detail (budget, tasks) | Billing generation wizard (per contract type), Change Order approval, Project Analyzer dashboard, WIP schedule view |
| Payroll (Ph.11) | Employees, Payroll Runs | Employee detail | Timesheet entry grid, Payroll Run wizard (draft → review → final) |
| Field Service (Ph.12) | Work Orders, Contracts | Work Order detail | Dispatch board (calendar/kanban view), Parts/labor capture on mobile |
| BI/Reporting (Ph.13) | Report/Dashboard catalog | Report parameter form | Financial Statement Designer (drag rows/columns), Drill-back transaction viewer |
| Integration/EDI (Ph.14) | Integration endpoints, Import/Export jobs | Endpoint config form | Import validation-preview & commit wizard |

---

## 6. Reusable Component Library (build once, use everywhere)

- [ ] `<LookupCombo>` — searchable FK selector (vendor/customer/item/account/project) with keyboard navigation
- [ ] `<BatchEntryGrid>` — spreadsheet-like grid with keyboard nav, copy/paste, running totals, inline validation, autosave
- [ ] `<StatusBadge>` — standardized Draft/Pending/Approved/Posted/Voided badge (see §3.10 palette)
- [ ] `<CompanyPeriodSwitcher>` — persistent header control
- [ ] `<ApprovalBanner>` — pending-approval call-to-action bar, reusable across Requisitions/Change Orders/Expense Reports
- [ ] `<AuditTrailPanel>` — slide-over panel showing an entity's full audit history
- [ ] `<DrillBackLink>` — wraps any report figure, opens the underlying transaction list
- [ ] `<ReportViewer>` — parameterized report runner with export (PDF/Excel/CSV) actions
- [ ] `<GenericCrudScaffold>` — data-driven list+detail generator for simple master-data entities (used for the many straightforward CRUD masters in §5's table so engineers aren't hand-building nearly-identical forms 20+ times)
- [ ] `<ApprovalWorkflowDesigner>` — visual threshold/routing configuration UI (admin-only)
- [ ] `<UniversalSearch>` (Cmd/Ctrl+K palette)
- [ ] `<CurrencyInput>` / `<QuantityInput>` — locale-aware, tabular-numeral formatted inputs shared by every money/quantity field in the system

---

## 7. Step-by-Step Frontend TODO

### Phase F0 — Foundation
- [ ] Scaffold Vite + React + TypeScript project; set up Tailwind + shadcn/ui theme tokens (colors, spacing, typography per §4).
- [ ] Set up routing shell, authenticated layout (nav sidebar, top bar with Company/Period switcher and Universal Search).
- [ ] Wire up OIDC auth flow (login, token refresh, role/claims retrieval).
- [ ] Generate typed API client from backend OpenAPI spec (keep in sync via CI check).
- [ ] Set up React Query provider, Zustand stores for session/UI state.
- [ ] Build the shared component library skeleton (§6) with Storybook for isolated development/QA of each component.
- [ ] Set up Vitest + React Testing Library + Playwright scaffolding, including a CI job.
- [ ] Establish dark-mode + density-mode toggles at the design-token level.

### Phase F1 — Platform/Admin Screens
- [ ] Company, Fiscal Period, Segment, Chart of Accounts admin screens (mostly `<GenericCrudScaffold>`).
- [ ] User/Role/Permission management screens.
- [ ] Approval Workflow Designer.
- [ ] Audit Trail viewer (global + entity-embedded via `<AuditTrailPanel>`).
- [ ] Period Close wizard (multi-step: sub-ledger status checklist → lock confirmation).

### Phase F2 — General Ledger
- [x] Chart of Accounts list/detail.
- [x] `<BatchEntryGrid>` implementation for Journal Entry (first real use of the core grid component — invest heavily here since every later batch screen reuses it).
- [x] Batch release/post confirmation flow with balance validation display.
- [x] Recurring Entry template screen; Allocation Rule builder.
- [x] Trial Balance / Financial Statement viewer with drill-back.
- [x] Consolidation run screen.

### Phase F3 — Accounts Payable
- [x] Vendor list/detail (with bank account, 1099 category fields).
- [x] Voucher Batch Entry grid (reuses `<BatchEntryGrid>`).
- [ ] 3-way match exception UI (visually flags PO/Receipt/Voucher mismatches).
- [x] Payment Selection & Run wizard (select vendors/vouchers → preview cash requirement → generate checks/ACH).
- [x] Void/reissue payment flow.
- [ ] 1099 processing screen.

### Phase F4 — Accounts Receivable
- [ ] Customer list/detail (credit limit, terms).
- [ ] Invoice Batch Entry grid.
- [ ] Cash Receipt & Application screen (drag/click to apply cash to open invoices).
- [ ] Statement generation & preview screen.
- [ ] Finance charge run screen.

### Phase F5 — Cash Management
- [ ] Bank Account list/detail.
- [ ] Bank Reconciliation workspace (two-pane match UI: system transactions vs. imported bank lines).
- [ ] Bank statement import (file upload + parse preview).

### Phase F6 — Purchasing
- [ ] Requisition entry + approval queue (uses `<ApprovalBanner>`, `<ApprovalWorkflowDesigner>` rules).
- [ ] PO entry/detail, change order UI.
- [ ] Goods Receipt entry screen.

### Phase F7 — Inventory
- [ ] Item master list/detail.
- [ ] Receipt/Issue/Transfer entry grid.
- [ ] Cycle Count workspace (count-sheet entry + variance review).
- [ ] Stock status / valuation dashboards.

### Phase F8 — Order Management
- [ ] Sales Order entry (with live credit-limit and availability check feedback).
- [ ] Pick/Pack/Ship workspace.
- [ ] Backorder management view.

### Phase F9 — Bill of Materials
- [ ] BOM detail/component grid editor.
- [ ] Build/Disassemble transaction screen.

### Phase F10 — Project Accounting (largest frontend effort — mirrors backend Phase 10 complexity)
- [ ] Project detail workspace (tabs: Budget, Tasks, Costs, Billing, Change Orders, Documents).
- [ ] Budget entry/revision screen with version history view.
- [ ] Change Order entry + approval flow.
- [ ] Billing generation wizard — dynamic form per contract type (T&M rate review, Cost-Plus fee display, SOV percent-complete entry grid, Milestone trigger list).
- [ ] WIP Schedule report view with drill-back.
- [ ] Project Analyzer dashboard (budget vs. actual vs. EAC visualizations, margin trend chart).
- [ ] Subcontract management screen (incl. retainage tracking).

### Phase F11 — Payroll
- [ ] Employee master.
- [ ] Timesheet entry grid (with project/task lookup and budget-remaining hint).
- [ ] Expense report entry + approval.
- [ ] Payroll Run wizard (Draft → Review/Edit → Final, with clear step indicator and the ability to void-and-recalculate before final).
- [ ] Certified payroll / union report screens.

### Phase F12 — Field Service
- [ ] Work Order list/detail.
- [ ] Dispatch board (calendar or kanban view, drag-to-assign technician).
- [ ] Mobile-optimized work-order completion screen (parts/labor capture) for field technicians — this screen specifically should be built mobile-first, not just responsive-as-an-afterthought.

### Phase F13 — BI / Reporting
- [ ] Report/Dashboard catalog with favorites.
- [ ] Parameterized `<ReportViewer>` with export actions.
- [ ] Financial Statement Designer (drag-and-drop row/column/formula builder).
- [ ] Drill-back transaction viewer.
- [ ] Executive dashboard (cash position, aging summaries, project portfolio margin).

### Phase F14 — Integration/EDI & Portal
- [ ] Integration endpoint/webhook admin screens.
- [ ] Import validation-preview & commit wizard.
- [ ] Vendor/Customer self-service portal (separate lightweight app or Next.js route group): PO acknowledgment, invoice status lookup, statement download.

### Phase F15 — Polish & Launch
- [ ] Full accessibility audit (WCAG 2.1 AA): keyboard navigation, screen-reader labeling on all grids/forms, color-contrast check on the status-badge palette.
- [ ] Performance pass: virtualize all large grids (react-window/AG Grid virtualization), code-split by module route, audit bundle size.
- [ ] Cross-browser/device testing (desktop, tablet for warehouse/field use, mobile for Field Service/time entry).
- [ ] E2E test coverage for every end-to-end flow in `spec.md` §6 (Procure-to-Pay, Order-to-Cash, Project-to-Bill, Period Close, Payroll Cycle).
- [ ] Onboarding/tour flow for new users (contrasts with the legacy system's steep, undocumented learning curve).
- [ ] Final visual QA pass against the design tokens (§4) for consistency across all ~15 modules.

---

## 8. "Definition of Done" for every frontend screen
- [ ] Keyboard-navigable end-to-end (no mouse-only interactions on core data-entry paths)
- [ ] Uses shared components from §6 rather than one-off implementations
- [ ] Loading, empty, and error states designed (not just the "happy path")
- [ ] Respects field-level permissions returned by the API (hides/disables fields the current role can't see/edit)
- [ ] Responsive from mobile (360px) through desktop (1920px), per module priority in §5
- [ ] Unit + component tests for custom logic; Playwright E2E test if the screen is part of a §6 (`spec.md`) end-to-end flow
- [ ] Meets WCAG 2.1 AA on that screen (contrast, labels, focus order)
