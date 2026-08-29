# End-to-End (E2E) Testing Plan — All Implemented Modules

Generated: 2026-08-29
Test account: `companyadmin@erp.com` (company-scoped to **US Operations**, company `11111111-1111-1111-1111-111111111111`) — NOT superadmin, per instruction.
Browsers: headless Chromium via Playwright (frontend `:3000` → Vite proxy → API `:5000`).

## Objective
Drive the **actual UI forms** (not raw API calls) for every implemented module, using the real login, navigation, list loads, and create/edit forms, to surface **frontend → backend** defects (page crashes, 500s, 403s, broken submits, schema/validation mismatches).

## Approach
Two tiers, both executed by `scripts/e2e-modules.mjs`:
- **Tier 1 — Route Smoke (ALL routes):** For each nav route, log in, navigate, wait for load, and record: console errors, page (uncaught) errors, and any network response with status ≥ 400 (especially 500). A clean load with a rendered table/list = PASS.
- **Tier 2 — Form E2E (primary entity per module):** Open the module's primary "New/Create" form via the real button, fill all *required* fields with valid data, submit, and record the resulting API status:
  - `2xx` → PASS (create works end-to-end)
  - `400` with a structured validation message → expected (data-shape issue surfaced, not a crash) — recorded but not a blocker unless it indicates a contract bug
  - `500` → FAIL (real frontend→backend defect to fix)
  - console/page error on submit → FAIL

## Modules & Routes Under Test
Routes are derived from `frontend/src/navigation.tsx`. Each module below lists routes; the **bold** route is the Tier-2 primary entity (form E2E target).

### 1. Dashboard
- `/dashboard`

### 2. Platform (Admin)
- `/platform/companies`, `/platform/fiscal-periods`, **`/platform/accounts`** (Chart of Accounts), `/platform/segment-types`, `/platform/segment-values`, `/platform/currencies`, `/platform/exchange-rates`, `/platform/number-sequences`, `/platform/approval-workflows`, `/platform/period-close`, `/platform/api-keys`, `/platform/approval-delegations`, `/platform/holiday-calendar`, `/platform/sod`, `/platform/users`, `/platform/roles`, `/platform/audit-logs`, `/platform/reports`

### 3. General Ledger
- **`/gl/journal-batches`**, `/gl/recurring-templates`, `/gl/allocation-rules`, `/gl/budgets`, `/gl/budget-rollforward`, `/gl/revaluation`, `/gl/reports`, `/gl/consolidation`, `/gl/account-inquiry`, `/gl/pre-posting`, `/gl/period-end-checklist`, `/gl/year-end-close`, `/gl/posting-suspense`

### 4. Accounts Payable
- **`/ap/vendors`**, `/ap/payment-terms`, `/ap/voucher-batches`, `/ap/payments`, `/ap/three-way-match`, `/ap/match-exceptions`, `/ap/backup-withholding`, `/ap/1099`, `/ap/1099-processing`, `/ap/duplicate-invoice`, `/ap/vendor-w9`, `/ap/bank-verification`, `/ap/cash-discount`, `/ap/escheatment`, `/ap/grir-accrual`, `/ap/vendor-statements`, `/ap/reports`

### 5. Accounts Receivable
- **`/ar/customers`**, `/ar/invoice-batches`, `/ar/cash-receipts`, `/ar/memos`, `/ar/credit-limit`, `/ar/statements`, `/ar/finance-charges`, `/ar/collections`, `/ar/dunning`, `/ar/allowance`, `/ar/resale-certificates`, `/ar/credit-memo-apply`, `/ar/aging-by-basis`, `/ar/cash-receipt-match`, `/ar/reports`

### 6. Cash Management
- **`/cash/bank-accounts`**, `/cash/deposits`, `/cash/bank-statements`, `/cash/reconciliations`, `/cash/transfers`, `/cash/bank-fees`, `/cash/nsf`, `/cash/reports`, `/cash/gl-mapping`, `/cash/lockbox`, `/cash/stale-checks`, `/cash/positive-pay`, `/cash/fee-analysis`, `/cash/forecast-horizon`, `/cash/outstanding-deposits`

### 7. Purchasing
- **`/purchasing/requisitions`**, `/purchasing/purchase-orders`, `/purchasing/vendor-quotes`, `/purchasing/approval-queue`, `/purchasing/receipts`, `/purchasing/po-templates`, `/purchasing/requisition-templates`, `/purchasing/vendor-items`, `/purchasing/vendors`, `/purchasing/buyer-agents`, `/purchasing/shipping-methods`, `/purchasing/fob-terms`, `/purchasing/reports`

### 8. Inventory
- **`/inventory/items`**, `/inventory/categories`, `/inventory/warehouses`, `/inventory/bins`, `/inventory/stock`, `/inventory/transactions`, `/inventory/reservations`, `/inventory/quarantine`, `/inventory/expiration`, `/inventory/revaluation`, `/inventory/landed-cost`, `/inventory/landed-cost-allocations`, `/inventory/cycle-counts`, `/inventory/physical-counts`, `/inventory/negative-overrides`, `/inventory/movements`, `/inventory/reorder`, `/inventory/substitutions`, `/inventory/kits`, `/inventory/consignment`, `/inventory/put-away-picking`, `/inventory/stock-by-location`, `/inventory/cycle-count-schedule`, `/inventory/scrap`, `/inventory/gl-tie-out`, `/inventory/stock-card`, `/inventory/uom-conversions`, `/inventory/uoms`, `/inventory/reports`

### 9. Order Management
- **`/om/sales-orders`**, `/om/shipments`, `/om/returns`, `/om/quotes`, `/om/blanket-orders`, `/om/substitution-offers`, `/om/rtv`, `/om/order-notes`, `/om/order-dashboard`, `/om/sales-analysis`, `/om/commissions`, `/om/atp`, `/om/freight`, `/om/pick-pack-ship`, `/om/reports`, `/om/masters`

### 10. Bill of Materials
- **`/bom`** (BOMs), `/bom/work-centers`, `/bom/routing-operations`, `/bom/build-orders`, `/bom/reports`

### 11. Project Accounting
- **`/projects`**, `/projects/overview`, `/projects/tasks`, `/projects/budget`, `/projects/costs`, `/projects/billing`, `/projects/change-orders`, `/projects/analysis`, `/projects/reports`

### 12. Payroll
- **`/payroll/employees`**, `/payroll/paycodes`, `/payroll/union`, `/payroll/timesheets`, `/payroll/runs`, `/payroll/expenses`, `/payroll/tax`, `/payroll/deductions`, `/payroll/pto`, `/payroll/manual`, `/payroll/reports`, `/payroll/garnishments`, `/payroll/setup`

### 13. Field Service
- **`/field-service/work-orders`**, `/field-service/dispatch`, `/field-service/technicians`, `/field-service/contracts`, `/field-service/equipment`, `/field-service/slas`, `/field-service/territories`, `/field-service/rate-cards`, `/field-service/estimates`, `/field-service/pm`, `/field-service/van-stock`, `/field-service/warranty`, `/field-service/reports`

### 14. Reporting
- `/reporting/catalog`, `/reporting/executive`, `/reporting/viewer`, `/reporting/designer`, `/reporting/quick-query`, `/reporting/drill-back`, `/reporting/scheduler`, `/reporting/categories`, `/reporting/parameter-sets`, `/reporting/usage`, `/reporting/sync-status`

## Pass / Fail Criteria
- **Tier 1 PASS:** route loads, no uncaught page errors, no console errors, no network ≥ 400 on initial load.
- **Tier 2 PASS:** create form submits → API 2xx (or clean 400 validation that is expected for the entity), no uncaught errors.
- **FAIL (must fix):** 500 on load or submit, uncaught JS exception, blank/crashed page, or a 400 that reveals a frontend→backend contract bug (wrong field names, missing required mapping, etc.).

## Execution Notes
- Run via `node scripts/e2e-modules.mjs` from `D:/nerp/frontend`.
- Results written to `scripts/e2e-results.json` (machine-readable) and summarized in `E2E_SUMMARY.md`.
- Auth: login form posts to `/api/v1/auth/login` with `{ username: email, password }`; token persisted to `localStorage` by `authStore`.
- Company isolation is expected: `companyadmin` may receive 403 for cross-company queries — recorded as isolation behavior, not a defect, unless it breaks a page the user should access.
