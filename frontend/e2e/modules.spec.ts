import { test, expect, Page } from '@playwright/test'
import { appendFileSync, mkdirSync } from 'fs'

/**
 * Generic form-driven E2E harness for the NERP ERP frontend.
 * Fills native inputs, textareas, selects AND Comboboxes via the REAL forms,
 * submits, and records the outcome. Crucially, it captures any non-2xx /api/
 * response (status + body) during submit so we can see FRONTEND -> BACKEND
 * errors precisely. See D:/nerp/E2E_TESTING.md.
 */

interface ModuleDef {
  module: string
  path: string
  formPath?: string
  newButton?: RegExp | string
  confirmSearch?: string
  readOnly?: boolean
}
interface ApiError { url: string; status: number; body: string }

const MODULES: ModuleDef[] = [
  { module: 'Platform:Companies', path: '/platform/companies', confirmSearch: 'E2E' },
  { module: 'Platform:Users', path: '/platform/users' },
  { module: 'Platform:Roles', path: '/platform/roles' },
  { module: 'Platform:SegmentTypes', path: '/platform/segment-types' },
  { module: 'GL:JournalBatches', path: '/gl/journal-batches' },
  { module: 'GL:RecurringTemplates', path: '/gl/recurring-templates' },
  { module: 'GL:AllocationRules', path: '/gl/allocation-rules' },
  { module: 'GL:Budgets', path: '/gl/budgets' },
  { module: 'AP:Vendors', path: '/ap/vendors', confirmSearch: 'E2E' },
  { module: 'AP:PaymentTerms', path: '/ap/payment-terms' },
  { module: 'AR:Customers', path: '/ar/customers', confirmSearch: 'E2E' },
  { module: 'AR:InvoiceBatches', path: '/ar/invoice-batches' },
  { module: 'Cash:BankAccounts', path: '/cash/bank-accounts', confirmSearch: 'E2E' },
  { module: 'Cash:Transfers', path: '/cash/transfers' },
  { module: 'Cash:BankFees', path: '/cash/bank-fees' },
  { module: 'Purchasing:Requisitions', path: '/purchasing/requisitions' },
  { module: 'Purchasing:PurchaseOrders', path: '/purchasing/purchase-orders' },
  { module: 'Inventory:Items', path: '/inventory/items', confirmSearch: 'E2E' },
  { module: 'Inventory:Warehouses', path: '/inventory/warehouses' },
  { module: 'Inventory:Categories', path: '/inventory/categories' },
  { module: 'OM:SalesOrders', path: '/om/sales-orders', formPath: '/om/sales-orders/new' },
  { module: 'OM:Quotes', path: '/om/quotes', formPath: '/om/quotes/new' },
  { module: 'BOM:Boms', path: '/bom' },
  { module: 'BOM:WorkCenters', path: '/bom/work-centers' },
  { module: 'Projects:Projects', path: '/projects' },
  { module: 'Payroll:Employees', path: '/payroll/employees' },
  { module: 'Payroll:PayCodes', path: '/payroll/paycodes' },
  { module: 'FieldService:WorkOrders', path: '/field-service/work-orders' },
  { module: 'FieldService:Technicians', path: '/field-service/technicians' },
  { module: 'Reporting:Catalog', path: '/reporting/catalog', readOnly: true },
]

const RESULTS = 'e2e-results.jsonl'
mkdirSync('.', { recursive: true })
function record(result: Record<string, unknown>) {
  appendFileSync(RESULTS, JSON.stringify(result) + '\n')
}

const STAMP = Date.now().toString(36).toUpperCase()
function valueFor(label: string, type: string): string {
  const l = label.toLowerCase()
  if (type === 'email') return `e2e+${STAMP}@example.com`
  if (type === 'date' || l.includes('date') || l.includes('posted') || l.includes('effective'))
    return new Date().toISOString().slice(0, 10)
  if (type === 'number' || /(balance|limit|cost|qty|quantity|amount|price|rate|count|days|point|stock|reorder|safety|lead)/.test(l))
    return '1'
  if (type === 'tel' || l.includes('phone')) return '5551234567'
  if (l.includes('email')) return `e2e+${STAMP}@example.com`
  if (l.includes('website') || l.includes('url')) return 'https://example.com'
  if (l.includes('zip') || l.includes('postal')) return '12345'
  return `E2E${STAMP}`
}

async function fillNative(page: Page, container: any): Promise<{ filled: number; skipped: string[] }> {
  const skipped: string[] = []
  let filled = 0
  for (const sel of ['input:visible', 'textarea:visible']) {
    const els = container.locator(sel)
    const n = await els.count()
    for (let i = 0; i < n; i++) {
      const el = els.nth(i)
      const role = (await el.getAttribute('role')) || ''
      if (role === 'combobox') continue
      const type = (await el.getAttribute('type')) || 'text'
      if (type === 'hidden' || type === 'checkbox' || type === 'radio' || type === 'file' || type === 'submit') continue
      if (await el.isDisabled()) continue
      const label = (await el.getAttribute('aria-label')) || (await el.getAttribute('name')) || (await el.getAttribute('placeholder')) || `field${i}`
      const val = valueFor(label, type)
      try { await el.fill(val, { timeout: 4000 }); await el.dispatchEvent('input').catch(() => {}); filled++ }
      catch { skipped.push(label) }
    }
  }
  const selects = container.locator('select:visible')
  const sc = await selects.count()
  for (let i = 0; i < sc; i++) {
    const sel = selects.nth(i)
    if (await sel.isDisabled()) continue
    const opts = await sel.locator('option').all()
    let chosen = -1
    let chosenValue = ''
    for (let o = 0; o < opts.length; o++) {
      const v = await opts[o].getAttribute('value')
      if (v && v.length > 0) { chosen = o; chosenValue = v; break }
    }
    if (chosen >= 0) {
      // Select by value (not index) so a proper change event fires and RHF state updates.
      await sel.selectOption({ value: chosenValue }).catch(() => skipped.push('select'))
      // Give RHF a tick to propagate the change.
      await sel.page().waitForTimeout(60).catch(() => {})
      filled++
    }
    else skipped.push('select-no-option')
  }
  return { filled, skipped }
}

async function fillComboboxes(page: Page, container: any): Promise<{ filled: number; skipped: string[]; noOptions: string[] }> {
  const skipped: string[] = []
  const noOptions: string[] = []
  let filled = 0
  const cbs = container.locator('[role="combobox"]:visible')
  const n = await cbs.count()
  for (let i = 0; i < n; i++) {
    const cb = cbs.nth(i)
    if (await cb.isDisabled()) continue
    const label = ((await cb.getAttribute('aria-label')) || (await cb.getAttribute('aria-labelledby')) || `combobox${i}`).toLowerCase()
    // For paired From/To (or Source/Destination) selectors, pick distinct options so
    // validations like "source and destination must differ" pass. From/Source -> first
    // option, To/Destination -> second option (when >=2 exist).
    const isTo = /to\b|destination/.test(label)
    const preferredIndex = isTo ? 1 : 0
    try {
      // Open the listbox by clicking the combobox input.
      await cb.click({ timeout: 3000 })
      const listbox = page.locator('[role="listbox"]:visible').last()
      const ok = await listbox.waitFor({ state: 'visible', timeout: 3500 }).then(() => true).catch(() => false)
      if (!ok) { skipped.push(label); continue }
      // Confirm at least one option exists.
      const optCount = await listbox.locator('[role="option"]:visible').count()
      if (optCount === 0) { noOptions.push(label); await page.keyboard.press('Escape').catch(() => {}); continue }
      // Select the option by dispatching a click directly on the DOM node. The listbox
      // is portaled to <body> and often sits *under* a Modal backdrop overlay, so a
      // real (coordinate-based) click hits the overlay instead of the <li> and React's
      // onClick never fires. dispatchEvent reaches the node directly and triggers the
      // handler regardless of stacking order.
      const pick = Math.min(preferredIndex, optCount - 1)
      await listbox.locator('[role="option"]:visible').nth(pick).dispatchEvent('click', { bubbles: true })
      await page.keyboard.press('Escape').catch(() => {})
      filled++
    } catch {
      skipped.push(label)
    }
  }
  return { filled, skipped, noOptions }
}

async function openCreateForm(page: Page, def: ModuleDef): Promise<{ opened: boolean; detail?: string }> {
  if (def.formPath) {
    await page.goto(def.formPath, { waitUntil: 'domcontentloaded' })
    await page.waitForLoadState('networkidle').catch(() => {})
    return { opened: true }
  }
  const openBtn = def.newButton
    ? page.getByRole('button', { name: def.newButton })
    : page.getByRole('button', { name: /^New / })
  if (await openBtn.count()) { await openBtn.first().click(); return { opened: true } }
  // Fallback: text-based create affordance (Record, Add, New, Create, +).
  const icon = page.getByRole('button').filter({ hasText: /record|new|create|add|\+/i }).first()
  if (await icon.count()) { await icon.first().click(); return { opened: true } }
  // Fallback: icon-only button in the page CONTENT (exclude the global app-shell
  // "Open menu" hamburger and table row action buttons).
  const plus = page
    .locator('main button:has(svg), [role="main"] button:has(svg), .page-content button:has(svg)')
    .filter({ hasNot: page.locator('[aria-label="Open menu"]') })
    .first()
  if (await plus.count()) { await plus.first().click(); return { opened: true } }
  return { opened: false, detail: 'no "New" or create button found on page' }
}

async function attachApiCapture(page: Page): Promise<ApiError[]> {
  const errors: ApiError[] = []
  page.on('response', async (res) => {
    const url = res.url()
    if (!url.includes('/api/')) return
    const status = res.status()
    if (status < 400) return
    try {
      const body = (await res.text()).slice(0, 600)
      errors.push({ url: url.replace('http://localhost:3000', ''), status, body })
    } catch { /* ignore */ }
  })
  return errors
}

for (const def of MODULES) {
  test(`[${def.module}] create-form flow`, async ({ page }) => {
    const entry: Record<string, unknown> = { module: def.module, path: def.path, ts: new Date().toISOString() }
    const apiErrors = await attachApiCapture(page)
    try {
      await page.goto(def.path, { waitUntil: 'domcontentloaded' })
      if (/login/.test(page.url())) { record({ ...entry, status: 'BLOCKED', detail: 'redirected to /login' }); return }
      await page.waitForLoadState('networkidle').catch(() => {})
      if (def.readOnly) { record({ ...entry, status: 'READ_ONLY_OK', detail: 'rendered', filled: 0, skipped: [] }); return }

      const open = await openCreateForm(page, def)
      if (!open.opened) { record({ ...entry, status: 'NO_FORM', detail: open.detail || 'no create affordance' }); return }

      const dialog = page.getByRole('dialog')
      const isDialog = !def.formPath
      const dialogVisible = isDialog
        ? await dialog.waitFor({ state: 'visible', timeout: 5000 }).then(() => true).catch(() => false)
        : false
      const container = dialogVisible ? dialog : page.locator('form, main').first()

      const native = await fillNative(page, container)
      const combo = await fillComboboxes(page, container)
      const filled = native.filled + combo.filled
      const skipped = [...native.skipped, ...combo.skipped]

      const submit = container
        .getByRole('button', { name: /Create|Save|Add|Submit|Confirm|Generate|Post|Record|Quote/i })
        .first()
      if (!(await submit.count())) {
        record({ ...entry, status: 'NO_FORM', detail: 'no submit button found', filled, skipped, apiErrors });
        return
      }
      // Let RHF register the programmatic fills and run validation before submitting.
      await page.waitForTimeout(400)
      await submit.click({ timeout: 5000 })

      // Wait for either dialog close or API response.
      await page.waitForTimeout(2500)
      const dialogGone = dialogVisible
        ? await dialog.waitFor({ state: 'detached', timeout: 3000 }).then(() => true).catch(() => false)
        : true

      // Prefer an explicit backend error.
      const createErr = apiErrors.find(e => e.url.includes('/api/') && /post/i.test('x') && e.status >= 400)
      if (createErr) {
        record({
          ...entry,
          status: 'SERVER_ERROR',
          detail: `API ${createErr.status} ${createErr.url} :: ${createErr.body}`,
          filled,
          skipped,
          noOptions: combo.noOptions,
          apiErrors,
        })
        return
      }
      if (dialogGone) {
        if (def.confirmSearch) {
          const found = await page.locator('body').getByText(def.confirmSearch, { exact: false }).first()
            .waitFor({ timeout: 4000 }).then(() => true).catch(() => false)
          record({ ...entry, status: found ? 'SUCCESS' : 'SUCCESS_NO_CONFIRM', detail: found ? `record '${def.confirmSearch}' visible` : 'dialog closed; list entry not confirmed', filled, skipped, noOptions: combo.noOptions })
        } else {
          record({ ...entry, status: 'SUCCESS', detail: 'dialog closed after submit', filled, skipped, noOptions: combo.noOptions })
        }
        return
      }

      // Still open: capture inline error / api error with FULL dialog text.
      const txt = (await container.innerText().catch(() => '')) || ''
      const alert = container.getByRole('alert').first()
      const alertTxt = (await alert.innerText().catch(() => '')) || ''
      const detail = (alertTxt || txt).trim().slice(0, 600)
      if (combo.noOptions.length) {
        record({ ...entry, status: 'VALIDATION', detail: `required combobox had no options: ${combo.noOptions.join(', ')}; ${detail}`, filled, skipped, noOptions: combo.noOptions, apiErrors })
      } else if (alertTxt || /required|invalid|must|cannot|exceeds|greater than|less than|format|error|failed|unable|not allowed|missing/i.test(txt)) {
        const isValidation = /required|invalid|must|cannot|exceeds|greater than|less than|format|missing/i.test(detail)
        record({ ...entry, status: isValidation ? 'VALIDATION' : 'SERVER_ERROR', detail: detail || 'dialog open with error', filled, skipped, noOptions: combo.noOptions, apiErrors })
      } else {
        record({ ...entry, status: 'VALIDATION', detail: `dialog remained open, submit likely disabled (unfilled required field). Full form text: ${txt.replace(/\n+/g,' | ').slice(0,500)}`, filled, skipped, noOptions: combo.noOptions, apiErrors })
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err)
      const isNetwork = /net::|ERR_|Failed to fetch|ECONNREFUSED|timeout/i.test(msg)
      record({ ...entry, status: isNetwork ? 'NETWORK_ERROR' : 'SERVER_ERROR', detail: msg.slice(0, 300), apiErrors })
    }
  })
}
