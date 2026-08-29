# E2E Form-Based Testing Plan — NERP ERP Frontend

**Goal:** Drive the *actual* React forms in the browser (not raw API calls) for every implemented
module, logged in as the **company admin** (`companyadmin@erp.com` / `password123`, company
`US Operations` = `11111111-1111-1111-1111-111111111111`), and record where the frontend→backend
flow breaks.

**Account used:** companyadmin (company-scoped, not super admin) — per the task instructions.

**Stack:**
- Frontend dev server: `http://localhost:3000` (Vite), proxy `/api` → `http://localhost:5000`.
- Backend API: `http://localhost:5000` (single healthy instance confirmed running).
- Playwright Chromium (installed to `~/.cache/ms-playwright`).
- Run: `cd D:/nerp/frontend && npx playwright test` (config `playwright.config.ts`).
- Login is bootstrapped once by `e2e/auth.setup.ts` into `.auth/companyadmin.json` (storageState)
  so each spec reuses the authenticated session.

## How the harness works (generic create-form flow)
For each module entry in `e2e/modules.spec.ts`:
1. Navigate to the module list page (e.g. `/cash/bank-accounts`).
2. Click the primary "New ..." button (role=button, name matches `/^New /`).
3. Wait for the `role="dialog"` modal.
4. For every native `<input>`/`<select>` in the modal (skipping role=combobox, checkboxes,
   hidden, disabled):
   - email  → `e2e+{module}@example.com`
   - date   → today (`YYYY-MM-DD`)
   - number/currency/qty/balance/limit/cost/amount → `1`
   - else   → `E2E{Module}{shortId}` text
   - `<select>` → first non-disabled, non-empty option.
5. Click the submit button (role=button, name matches `/Create|Save|Add|Submit|Confirm/`).
6. Record outcome:
   - **SUCCESS** — dialog closed AND the new code/text now appears in the list (searched).
   - **VALIDATION** — a `role="alert"` message appeared in-page (frontend or backend validation).
   - **SERVER_ERROR** — a form-level error alert with a non-validation message, or an HTTP 4xx/5xx.
   - **NETWORK_ERROR** — page error / failed API request.
   - **NO_FORM / BLOCKED** — no "New" button, or navigation redirected to login, or page crashed.

Each outcome is appended as a JSON line to `frontend/e2e-results.jsonl` and summarized by
`scripts/e2e-summarize.mjs` into `E2E_SUMMARY.md`.

## Modules covered (all with implemented pages)
| # | Module | Primary list path | Entity created |
|---|--------|-------------------|----------------|
| 1 | Platform | /platform/companies | Company |
| 2 | Platform | /platform/users | User |
| 3 | Platform | /platform/roles | Role |
| 4 | Platform | /platform/segment-types | Segment Type |
| 5 | GL | /gl/journal-batches | Journal Batch |
| 6 | GL | /gl/recurring-templates | Recurring Template |
| 7 | GL | /gl/allocation-rules | Allocation Rule |
| 8 | GL | /gl/budgets | Budget |
| 9 | AP | /ap/vendors | Vendor |
| 10 | AP | /ap/payment-terms | Payment Term |
| 11 | AR | /ar/customers | Customer |
| 12 | AR | /ar/invoice-batches | Invoice Batch |
| 13 | Cash | /cash/bank-accounts | Bank Account |
| 14 | Cash | /cash/transfers | Bank Transfer |
| 15 | Cash | /cash/bank-fees | Bank Fee |
| 16 | Purchasing | /purchasing/requisitions | Requisition |
| 17 | Purchasing | /purchasing/purchase-orders | Purchase Order |
| 18 | Inventory | /inventory/items | Item |
| 19 | Inventory | /inventory/warehouses | Warehouse |
| 20 | Inventory | /inventory/categories | Item Category |
| 21 | OM | /om/sales-orders/new | Sales Order |
| 22 | OM | /om/quotes | Quote |
| 23 | BOM | /bom | BOM |
| 24 | BOM | /bom/work-centers | Work Center |
| 25 | Projects | /projects | Project |
| 26 | Payroll | /payroll/employees | Employee |
| 27 | Payroll | /payroll/paycodes | Pay Code |
| 28 | Field Service | /field-service/work-orders | Work Order |
| 29 | Field Service | /field-service/technicians | Technician |
| 30 | Reporting | /reporting/catalog | Report catalog (read) |

Transaction detail screens (journal entry lines, voucher lines, invoice lines, pick/pack/ship)
and read-only report pages are exercised where feasible but the primary automated assertion is the
**create-form round trip** for each module above.

## What "issues from frontend going to backend" means here
- The backend accepts the request but the UI shows an unexpected error.
- The UI validation passes but the API returns 400/500.
- The API succeeds but the UI does not reflect the new record (stale cache / wrong query param).
- A required field the backend needs is missing/optional in the UI (e.g. the `DEMO_COMPANY_ID`
  hardcoding for non-US-Operations companies).
- Cross-company scoping: companyadmin sees only US Operations data; if a form silently sends the
  wrong companyId the record may land elsewhere or be rejected.

## Running
```bash
cd D:/nerp/frontend
npx playwright test              # full run, writes e2e-results.jsonl
node scripts/e2e-summarize.mjs  # generates E2E_SUMMARY.md
```

## Notes / decisions
- The dev server on :3000 proxies `/api` → :5000; both must be running (they are).
- Auth state is persisted to `localStorage` (`erp-auth-storage`), so the setup project logs in once.
- Each created entity uses a unique code so reruns don't collide and success is verifiable.
- A generic filler is used so the harness is resilient to per-module field differences; modules
  with genuinely required fields not representable as native inputs are flagged for manual review.
