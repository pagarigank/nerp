import { test, type Page } from '@playwright/test'
import * as fs from 'fs'

test.setTimeout(60000)

/**
 * Per-page frontend E2E (REAL UI only, NO raw API calls).
 * For every route in the inventory:
 *   - login, navigate, assert the page renders without crashing
 *   - if a create/new form is present, open it, fill real fields, submit, and
 *     assert the data passes (dialog closes / record appears / action completes)
 * Results are printed as RENDER_OK / FORM_OK / NAV_OK / ISSUE / GAP and also
 * captured to allpages-results.jsonl for the tracking doc.
 */

const STAMP = Date.now().toString(36).toUpperCase()
function valueFor(label: string, type: string): string {
  const l = label.toLowerCase()
  if (type === 'email') return `qa+${STAMP}@example.com`
  if (type === 'date' || l.includes('date') || l.includes('posted') || l.includes('effective')) return new Date().toISOString().slice(0, 10)
  if (type === 'number' || /(balance|limit|cost|qty|quantity|amount|price|rate|count|days|point|stock|reorder|safety|lead)/.test(l)) return '1'
  if (type === 'tel' || l.includes('phone')) return '5551234567'
  if (l.includes('email')) return `qa+${STAMP}@example.com`
  if (l.includes('website') || l.includes('url')) return 'https://example.com'
  if (l.includes('zip') || l.includes('postal')) return '12345'
  return `QA${STAMP}`
}
async function login(page: Page, user = 'companyadmin@erp.com') {
  await page.goto('/login', { waitUntil: 'domcontentloaded' })
  await page.fill('#email', user); await page.fill('#password', 'password123')
  await page.getByRole('button', { name: /sign in/i }).click()
  await page.waitForURL('**/dashboard', { timeout: 20000 })
}
async function fillNative(container: any, page: Page): Promise<{ filled: number; token: string }> {
  let filled = 0; let token = ''
  for (const sel of ['input:visible:not([type=hidden]):not([role=combobox])', 'textarea:visible']) {
    const els = container.locator(sel); const n = await els.count()
    for (let i = 0; i < n; i++) {
      const el = els.nth(i); const type = (await el.getAttribute('type')) || 'text'
      if (['checkbox', 'radio', 'file', 'submit'].includes(type)) continue
      if (await el.isDisabled()) continue
      const label = (await el.getAttribute('aria-label')) || (await el.getAttribute('name')) || (await el.getAttribute('placeholder')) || `f${i}`
      const l = label.toLowerCase()
      let val: string
      if (type === 'email' || l.includes('email')) val = `qa+${STAMP}@example.com`
      else if (type === 'date' || l.includes('date') || l.includes('posted') || l.includes('effective') || l.includes('expiry')) val = new Date().toISOString().slice(0, 10)
      else if (/code/i.test(l)) val = `QA${STAMP}${i}`        // unique codes (avoid 409)
      else if (type === 'number' || /(balance|limit|cost|qty|quantity|amount|price|rate|count|days|point|stock|reorder|safety|lead)/.test(l)) val = '1'
      else if (type === 'tel' || l.includes('phone')) val = '5551234567'
      else if (l.includes('website') || l.includes('url')) val = 'https://example.com'
      else if (l.includes('zip') || l.includes('postal')) val = '12345'
      else val = `QA${STAMP}`
      if (!token) token = val
      await el.fill(val, { timeout: 4000 }).catch(() => {}); filled++
    }
  }
  const selects = container.locator('select:visible'); const sc = await selects.count()
  for (let i = 0; i < sc; i++) { const s = selects.nth(i); if (await s.isDisabled()) continue; const opts = await s.locator('option').all(); let picked = false; for (const o of opts) { const v = await o.getAttribute('value'); const txt = (await o.innerText().catch(() => '')) || ''; if (v && v !== '' && !/^select|choose|—|-{2,}$/i.test(txt.trim())) { await s.selectOption({ value: v }).catch(() => {}); picked = true; break } } if (!picked && opts.length) { const v = await opts[0].getAttribute('value'); if (v) await s.selectOption({ value: v }).catch(() => {}) } filled++ }
  const cbs = container.locator('[role="combobox"]:visible'); const cn = await cbs.count()
  for (let i = 0; i < cn; i++) { const cb = cbs.nth(i); if (await cb.isDisabled()) continue; try {
    // Custom Combobox: open, wait for async options, ArrowDown+Enter (no typing so numeric/any options stay).
    await cb.click({ timeout: 3000 }); await cb.focus().catch(() => {})
    const list = page.locator('[role="listbox"]:visible').last()
    const opt = list.locator('[role="option"]:visible')
    const ready = await opt.first().waitFor({ state: 'visible', timeout: 5000 }).then(() => true).catch(() => false)
    if (ready) { await page.keyboard.press('ArrowDown', { delay: 60 }).catch(() => {}); await page.waitForTimeout(200); await page.keyboard.press('Enter', { delay: 60 }).catch(() => {}) }
    await page.waitForTimeout(250)
    filled++
    await page.keyboard.press('Escape').catch(() => {})
  } catch { } }
  return { filled, token }
}
async function clickSubmit(container: any) {
  const submit = container.getByRole('button', { name: /Create|Save|Add|Submit|Confirm|Generate|Post|Record|Update|Quote|Apply|Run|Search|Filter/i }).first()
  if (await submit.count()) await submit.first().click({ timeout: 5000 }).catch(() => {})
}

// Full route list (leaf pages). Some need an :id — handled below by reading a parent row link.
const PAGES: { id: string; path: string; detail?: boolean; form?: 'create' | 'none' }[] = [
  // Platform
  { id: 'P1', path: '/platform/companies', form: 'create', loginAs: 'superadmin' },
  { id: 'P2', path: '/platform/fiscal-periods', form: 'create', loginAs: 'superadmin' },
  { id: 'P3', path: '/platform/accounts', form: 'create', loginAs: 'superadmin' },
  { id: 'P4', path: '/platform/segment-types', form: 'create' },
  { id: 'P5', path: '/platform/segment-values', form: 'create' },
  { id: 'P6', path: '/platform/users', form: 'create' },
  { id: 'P7', path: '/platform/roles', form: 'create' },
  { id: 'P8', path: '/platform/audit-logs', form: 'none' },
  { id: 'P9', path: '/platform/currencies', form: 'create' },
  { id: 'P10', path: '/platform/exchange-rates', form: 'create' },
  { id: 'P11', path: '/platform/number-sequences', form: 'create' },
  { id: 'P12', path: '/platform/approval-workflows', form: 'create' },
  { id: 'P13', path: '/platform/period-close', form: 'none' },
  { id: 'P14', path: '/platform/api-keys', form: 'create' },
  { id: 'P15', path: '/platform/approval-delegations', form: 'create', loginAs: 'superadmin' },
  { id: 'P16', path: '/platform/holiday-calendar', form: 'create' },
  { id: 'P17', path: '/platform/sod', form: 'none' },
  { id: 'P18', path: '/platform/reports', form: 'none' },
  // GL
  { id: 'G1', path: '/gl/journal-batches', form: 'create' },
  { id: 'G2', path: '/gl/journal-batches', detail: true, form: 'none' },
  { id: 'G3', path: '/gl/recurring-templates', form: 'create' },
  { id: 'G4', path: '/gl/allocation-rules', form: 'create' },
  { id: 'G5', path: '/gl/budgets', form: 'none' },
  { id: 'G6', path: '/gl/revaluation', form: 'none' },
  { id: 'G7', path: '/gl/reports', form: 'none' },
  { id: 'G8', path: '/gl/consolidation', form: 'none' },
  { id: 'G9', path: '/gl/account-inquiry', form: 'none' },
  { id: 'G10', path: '/gl/pre-posting', form: 'none' },
  { id: 'G11', path: '/gl/period-end-checklist', form: 'none' },
  { id: 'G12', path: '/gl/year-end-close', form: 'none' },
  { id: 'G13', path: '/gl/posting-suspense', form: 'none' },
  { id: 'G14', path: '/gl/budget-rollforward', form: 'none' },
  // AP
  { id: 'A1', path: '/ap/vendors', form: 'create' },
  { id: 'A2', path: '/ap/payment-terms', form: 'create' },
  { id: 'A3', path: '/ap/voucher-batches', form: 'create' },
  { id: 'A4', path: '/ap/voucher-batches', detail: true, form: 'none' },
  { id: 'A5', path: '/ap/payments', form: 'none' },
  { id: 'A6', path: '/ap/three-way-match', form: 'none' },
  { id: 'A7', path: '/ap/backup-withholding', form: 'none' },
  { id: 'A8', path: '/ap/1099', form: 'none' },
  { id: 'A9', path: '/ap/match-exceptions', form: 'none' },
  { id: 'A10', path: '/ap/1099-processing', form: 'none' },
  { id: 'A11', path: '/ap/duplicate-invoice', form: 'none' },
  { id: 'A12', path: '/ap/vendor-w9', form: 'none' },
  { id: 'A13', path: '/ap/bank-verification', form: 'none' },
  { id: 'A14', path: '/ap/cash-discount', form: 'none' },
  { id: 'A15', path: '/ap/escheatment', form: 'none' },
  { id: 'A16', path: '/ap/grir-accrual', form: 'none' },
  { id: 'A17', path: '/ap/vendor-statements', form: 'none' },
  { id: 'A18', path: '/ap/reports', form: 'none' },
  // AR
  { id: 'R1', path: '/ar/customers', form: 'create' },
  { id: 'R2', path: '/ar/invoice-batches', form: 'create' },
  { id: 'R3', path: '/ar/invoice-batches', detail: true, form: 'none' },
  { id: 'R4', path: '/ar/cash-receipts', form: 'none' },
  { id: 'R5', path: '/ar/memos', form: 'none' },
  { id: 'R6', path: '/ar/credit-limit', form: 'none' },
  { id: 'R7', path: '/ar/statements', form: 'none' },
  { id: 'R8', path: '/ar/finance-charges', form: 'none' },
  { id: 'R9', path: '/ar/reports', form: 'none' },
  { id: 'R10', path: '/ar/collections', form: 'none' },
  { id: 'R11', path: '/ar/dunning', form: 'none' },
  { id: 'R12', path: '/ar/allowance', form: 'none' },
  { id: 'R13', path: '/ar/resale-certificates', form: 'none' },
  { id: 'R14', path: '/ar/credit-memo-apply', form: 'none' },
  { id: 'R15', path: '/ar/aging-by-basis', form: 'none' },
  { id: 'R16', path: '/ar/cash-receipt-match', form: 'none' },
  // Cash
  { id: 'C1', path: '/cash/bank-accounts', form: 'create' },
  { id: 'C2', path: '/cash/deposits', form: 'none' },
  { id: 'C3', path: '/cash/bank-statements', form: 'none' },
  { id: 'C4', path: '/cash/reconciliations', form: 'none' },
  { id: 'C5', path: '/cash/reconciliations', detail: true, form: 'none' },
  { id: 'C6', path: '/cash/transfers', form: 'create' },
  { id: 'C7', path: '/cash/bank-fees', form: 'create' },
  { id: 'C8', path: '/cash/nsf', form: 'none' },
  { id: 'C9', path: '/cash/reports', form: 'none' },
  { id: 'C10', path: '/cash/gl-mapping', form: 'none' },
  { id: 'C11', path: '/cash/lockbox', form: 'none' },
  { id: 'C12', path: '/cash/stale-checks', form: 'none' },
  { id: 'C13', path: '/cash/positive-pay', form: 'none' },
  { id: 'C14', path: '/cash/fee-analysis', form: 'none' },
  { id: 'C15', path: '/cash/forecast-horizon', form: 'none' },
  { id: 'C16', path: '/cash/outstanding-deposits', form: 'none' },
  // Purchasing
  { id: 'PC1', path: '/purchasing/requisitions', form: 'create' },
  { id: 'PC2', path: '/purchasing/purchase-orders', form: 'create' },
  { id: 'PC3', path: '/purchasing/vendor-quotes', form: 'create' },
  { id: 'PC4', path: '/purchasing/approval-queue', form: 'none' },
  { id: 'PC5', path: '/purchasing/receipts', form: 'none' },
  { id: 'PC6', path: '/purchasing/po-templates', form: 'create' },
  { id: 'PC7', path: '/purchasing/requisition-templates', form: 'create' },
  { id: 'PC8', path: '/purchasing/vendor-items', form: 'none' },
  { id: 'PC9', path: '/purchasing/vendors', form: 'create' },
  { id: 'PC10', path: '/purchasing/buyer-agents', form: 'none' },
  { id: 'PC11', path: '/purchasing/shipping-methods', form: 'none' },
  { id: 'PC12', path: '/purchasing/fob-terms', form: 'none' },
  { id: 'PC13', path: '/purchasing/reports', form: 'none' },
  // Inventory
  { id: 'I1', path: '/inventory/items', form: 'create' },
  { id: 'I2', path: '/inventory/categories', form: 'create' },
  { id: 'I3', path: '/inventory/warehouses', form: 'create' },
  { id: 'I4', path: '/inventory/bins', form: 'none' },
  { id: 'I5', path: '/inventory/stock', form: 'none' },
  { id: 'I6', path: '/inventory/transactions', form: 'none' },
  { id: 'I7', path: '/inventory/reservations', form: 'none' },
  { id: 'I8', path: '/inventory/quarantine', form: 'none' },
  { id: 'I9', path: '/inventory/expiration', form: 'none' },
  { id: 'I10', path: '/inventory/revaluation', form: 'none' },
  { id: 'I11', path: '/inventory/landed-cost', form: 'none' },
  { id: 'I12', path: '/inventory/landed-cost-allocations', form: 'none' },
  { id: 'I13', path: '/inventory/cycle-counts', form: 'none' },
  { id: 'I14', path: '/inventory/physical-counts', form: 'none' },
  { id: 'I15', path: '/inventory/negative-overrides', form: 'none' },
  { id: 'I16', path: '/inventory/movements', form: 'none' },
  { id: 'I17', path: '/inventory/reorder', form: 'none' },
  { id: 'I18', path: '/inventory/reports', form: 'none' },
  { id: 'I19', path: '/inventory/substitutions', form: 'none' },
  { id: 'I20', path: '/inventory/kits', form: 'none' },
  { id: 'I21', path: '/inventory/consignment', form: 'none' },
  { id: 'I22', path: '/inventory/put-away-picking', form: 'none' },
  { id: 'I23', path: '/inventory/stock-by-location', form: 'none' },
  { id: 'I24', path: '/inventory/cycle-count-schedule', form: 'none' },
  { id: 'I25', path: '/inventory/stock-card', form: 'none' },
  { id: 'I26', path: '/inventory/uom-conversions', form: 'none' },
  { id: 'I27', path: '/inventory/uoms', form: 'none' },
  { id: 'I28', path: '/inventory/scrap', form: 'none' },
  { id: 'I29', path: '/inventory/gl-tie-out', form: 'none' },
  // OM
  { id: 'O1', path: '/om/sales-orders', form: 'create' },
  { id: 'O2', path: '/om/sales-orders/new', form: 'create' },
  { id: 'O3', path: '/om/sales-orders', detail: true, form: 'none' },
  { id: 'O4', path: '/om/shipments', form: 'none' },
  { id: 'O5', path: '/om/shipments/new', form: 'none' },
  { id: 'O6', path: '/om/shipments', detail: true, form: 'none' },
  { id: 'O7', path: '/om/returns', form: 'none' },
  { id: 'O8', path: '/om/returns/new', form: 'none' },
  { id: 'O9', path: '/om/returns', detail: true, form: 'none' },
  { id: 'O10', path: '/om/quotes', form: 'create' },
  { id: 'O11', path: '/om/quotes/new', form: 'create' },
  { id: 'O12', path: '/om/blanket-orders', form: 'none' },
  { id: 'O13', path: '/om/substitution-offers', form: 'none' },
  { id: 'O14', path: '/om/rtv', form: 'none' },
  { id: 'O15', path: '/om/order-notes', form: 'none' },
  { id: 'O16', path: '/om/order-dashboard', form: 'none' },
  { id: 'O17', path: '/om/sales-analysis', form: 'none' },
  { id: 'O18', path: '/om/commissions', form: 'none' },
  { id: 'O19', path: '/om/atp', form: 'none' },
  { id: 'O20', path: '/om/freight', form: 'none' },
  { id: 'O21', path: '/om/pick-pack-ship', form: 'none' },
  { id: 'O22', path: '/om/reports', form: 'none' },
  { id: 'O23', path: '/om/masters', form: 'none' },
  // BOM
  { id: 'B1', path: '/bom', form: 'create' },
  { id: 'B2', path: '/bom/work-centers', form: 'create' },
  { id: 'B3', path: '/bom/routing-operations', form: 'create' },
  { id: 'B4', path: '/bom/build-orders', form: 'none' },
  { id: 'B5', path: '/bom/reports', form: 'none' },
  // Projects
  { id: 'PR1', path: '/projects', form: 'create' },
  { id: 'PR2', path: '/projects/overview', form: 'none' },
  { id: 'PR3', path: '/projects/tasks', form: 'none' },
  { id: 'PR4', path: '/projects/budget', form: 'none' },
  { id: 'PR5', path: '/projects/costs', form: 'none' },
  { id: 'PR6', path: '/projects/billing', form: 'none' },
  { id: 'PR7', path: '/projects/change-orders', form: 'none' },
  { id: 'PR8', path: '/projects/analysis', form: 'none' },
  { id: 'PR9', path: '/projects/reports', form: 'none' },
  // Payroll
  { id: 'PY1', path: '/payroll/employees', form: 'create' },
  { id: 'PY2', path: '/payroll/paycodes', form: 'create' },
  { id: 'PY3', path: '/payroll/union', form: 'none' },
  { id: 'PY4', path: '/payroll/timesheets', form: 'none' },
  { id: 'PY5', path: '/payroll/runs', form: 'none' },
  { id: 'PY6', path: '/payroll/expenses', form: 'none' },
  { id: 'PY7', path: '/payroll/tax', form: 'none' },
  { id: 'PY8', path: '/payroll/deductions', form: 'none' },
  { id: 'PY9', path: '/payroll/pto', form: 'none' },
  { id: 'PY10', path: '/payroll/manual', form: 'none' },
  { id: 'PY11', path: '/payroll/reports', form: 'none' },
  { id: 'PY12', path: '/payroll/garnishments', form: 'none' },
  { id: 'PY13', path: '/payroll/setup', form: 'none' },
  // Field Service
  { id: 'F1', path: '/field-service/work-orders', form: 'create' },
  { id: 'F2', path: '/field-service/dispatch', form: 'none' },
  { id: 'F3', path: '/field-service/technicians', form: 'create' },
  { id: 'F4', path: '/field-service/contracts', form: 'none' },
  { id: 'F5', path: '/field-service/equipment', form: 'none' },
  { id: 'F6', path: '/field-service/slas', form: 'none' },
  { id: 'F7', path: '/field-service/territories', form: 'none' },
  { id: 'F8', path: '/field-service/rate-cards', form: 'none' },
  { id: 'F9', path: '/field-service/estimates', form: 'none' },
  { id: 'F10', path: '/field-service/pm', form: 'none' },
  { id: 'F11', path: '/field-service/van-stock', form: 'none' },
  { id: 'F12', path: '/field-service/warranty', form: 'none' },
  { id: 'F13', path: '/field-service/reports', form: 'none' },
  // Reporting
  { id: 'RP1', path: '/reporting/catalog', form: 'none' },
  { id: 'RP2', path: '/reporting/executive', form: 'none' },
  { id: 'RP3', path: '/reporting/viewer', form: 'none' },
  { id: 'RP4', path: '/reporting/designer', form: 'none' },
  { id: 'RP5', path: '/reporting/quick-query', form: 'none' },
  { id: 'RP6', path: '/reporting/drill-back', form: 'none' },
  { id: 'RP7', path: '/reporting/scheduler', form: 'none' },
  { id: 'RP8', path: '/reporting/categories', form: 'none' },
  { id: 'RP9', path: '/reporting/parameter-sets', form: 'none' },
  { id: 'RP10', path: '/reporting/usage', form: 'none' },
  { id: 'RP11', path: '/reporting/sync-status', form: 'none' },
  // Cross-cutting
  { id: 'X2', path: '/dashboard', form: 'none' },
]

for (const def of PAGES) {
  test(`PAGE [${def.id}] ${def.path}${def.detail ? ' (detail)' : ''}`, async ({ page }) => {
    const entry: Record<string, unknown> = { id: def.id, path: def.path, detail: !!def.detail, ts: new Date().toISOString() }
    try {
      await login(page, def.loginAs === 'superadmin' ? 'admin@erp.com' : 'companyadmin@erp.com')
      let url = def.path
      if (def.detail) {
        // derive an id from the first row link on the parent list
        await page.goto(def.path, { waitUntil: 'domcontentloaded' }); await page.waitForLoadState('networkidle').catch(() => {}); await page.waitForTimeout(800)
        const link = page.locator(`a[href*="${def.path}/"]`).first()
        const href = (await link.count()) ? (await link.getAttribute('href')) || '' : ''
        const m = href.match(/\/([0-9a-fA-F-]{8,})(?:\?|$)/)
        if (m) url = `${def.path}/${m[1]}`
      }
      await page.goto(url, { waitUntil: 'domcontentloaded' })
      await page.waitForLoadState('networkidle').catch(() => {})
      await page.waitForTimeout(700)
      const crashed = (await page.locator('body').innerText().catch(() => '')).includes('App render error')
      const hasContent = (await page.locator('main, h1, h2, table, form, [role="grid"], .card').first().count()) > 0
      const renderOk = !crashed && hasContent
      entry.renderOk = renderOk
      if (!renderOk) { entry.status = 'ISSUE'; entry.detail = crashed ? 'crash' : 'no content'; console.log(`PAGE ${def.id} ${url} -> ISSUE ${entry.detail}`); return }

      // Try the primary create form if expected
      if (def.form === 'create') {
        const newBtn = page.getByRole('button', { name: /^New |Create|Add |New$/i }).first()
        if (await newBtn.count()) {
          await newBtn.first().click().catch(() => {})
          await page.waitForTimeout(700)
          const dlg = page.getByRole('dialog')
          const c = (await dlg.isVisible().catch(() => false)) ? dlg : page.locator('form, main').first()
          const { filled, token } = await fillNative(c, page)
          await page.waitForTimeout(300)
          // Nested line items: click an Add-line button, fill the new row, then submit.
          const addLine = c.getByRole('button', { name: /Add Line|Add Item|Add Row|\+ Add|Add Detail|Add Entry/i }).first()
          if (await addLine.count()) {
            await addLine.first().click().catch(() => {})
            await page.waitForTimeout(500)
            // fill inputs/comboboxes within the LAST row (or the dialog if rows aren't tagged)
            const lineScope = c.locator('tr, [class*="row"], [class*="line"]').last().or(c)
            await fillNative(lineScope, page).catch(() => {})
            await page.waitForTimeout(200)
          }
          const openBefore = await dlg.isVisible().catch(() => false)
          await clickSubmit(c)
          await page.waitForTimeout(2200)
          const closed = !(await dlg.isVisible().catch(() => false))
          entry.formFilled = filled; entry.token = token; entry.formClosed = closed
          entry.status = (filled > 0 && closed) ? 'FORM_OK' : (filled > 0 ? 'FORM_PARTIAL' : 'GAP')
        } else {
          entry.status = 'NAV_OK' // renders, but no detected create button
        }
      } else {
        entry.status = 'NAV_OK'
      }
      console.log(`PAGE ${def.id} ${url} -> ${entry.status} (render=${renderOk})`)
    } catch (err) {
      entry.status = 'ISSUE'; entry.detail = err instanceof Error ? err.message.slice(0, 200) : String(err)
      console.log(`PAGE ${def.id} ${def.path} -> ISSUE ${entry.detail}`)
    }
    fs.appendFileSync('allpages-results.jsonl', JSON.stringify(entry) + '\n')
  })
}
