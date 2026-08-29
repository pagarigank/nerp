// E2E harness — drives the ACTUAL ERP UI as companyadmin@erp.com.
// Tier 1: smoke every route (console/page/network >=400).
// Tier 2: open primary "New/Create" form per module, fill required fields, submit, record backend status.
// Writes results incrementally to scripts/e2e-results.json (survives timeouts).
// Supports slicing via env START (0-based) and COUNT (routes to process this run).
// Uses a FRESH page per route so network/console/pageerror listeners never leak across routes.
import { chromium } from 'playwright'
import { writeFileSync, mkdirSync, readFileSync } from 'node:fs'

const BASE = 'http://localhost:3000'
const EMAIL = 'companyadmin@erp.com'
const PW = 'password123'
const OUT = 'scripts/e2e-results.json'

const ROUTES = [
  '/dashboard',
  '/platform/companies','/platform/fiscal-periods','/platform/accounts','/platform/segment-types','/platform/segment-values','/platform/currencies','/platform/exchange-rates','/platform/number-sequences','/platform/approval-workflows','/platform/period-close','/platform/api-keys','/platform/approval-delegations','/platform/holiday-calendar','/platform/sod','/platform/users','/platform/roles','/platform/audit-logs','/platform/reports',
  '/gl/journal-batches','/gl/recurring-templates','/gl/allocation-rules','/gl/budgets','/gl/budget-rollforward','/gl/revaluation','/gl/reports','/gl/consolidation','/gl/account-inquiry','/gl/pre-posting','/gl/period-end-checklist','/gl/year-end-close','/gl/posting-suspense',
  '/ap/vendors','/ap/payment-terms','/ap/voucher-batches','/ap/payments','/ap/three-way-match','/ap/match-exceptions','/ap/backup-withholding','/ap/1099','/ap/1099-processing','/ap/duplicate-invoice','/ap/vendor-w9','/ap/bank-verification','/ap/cash-discount','/ap/escheatment','/ap/grir-accrual','/ap/vendor-statements','/ap/reports',
  '/ar/customers','/ar/invoice-batches','/ar/cash-receipts','/ar/memos','/ar/credit-limit','/ar/statements','/ar/finance-charges','/ar/collections','/ar/dunning','/ar/allowance','/ar/resale-certificates','/ar/credit-memo-apply','/ar/aging-by-basis','/ar/cash-receipt-match','/ar/reports',
  '/cash/bank-accounts','/cash/deposits','/cash/bank-statements','/cash/reconciliations','/cash/transfers','/cash/bank-fees','/cash/nsf','/cash/reports','/cash/gl-mapping','/cash/lockbox','/cash/stale-checks','/cash/positive-pay','/cash/fee-analysis','/cash/forecast-horizon','/cash/outstanding-deposits',
  '/purchasing/requisitions','/purchasing/purchase-orders','/purchasing/vendor-quotes','/purchasing/approval-queue','/purchasing/receipts','/purchasing/po-templates','/purchasing/requisition-templates','/purchasing/vendor-items','/purchasing/vendors','/purchasing/buyer-agents','/purchasing/shipping-methods','/purchasing/fob-terms','/purchasing/reports',
  '/inventory/items','/inventory/categories','/inventory/warehouses','/inventory/bins','/inventory/stock','/inventory/transactions','/inventory/reservations','/inventory/quarantine','/inventory/expiration','/inventory/revaluation','/inventory/landed-cost','/inventory/landed-cost-allocations','/inventory/cycle-counts','/inventory/physical-counts','/inventory/negative-overrides','/inventory/movements','/inventory/reorder','/inventory/substitutions','/inventory/kits','/inventory/consignment','/inventory/put-away-picking','/inventory/stock-by-location','/inventory/cycle-count-schedule','/inventory/scrap','/inventory/gl-tie-out','/inventory/stock-card','/inventory/uom-conversions','/inventory/uoms','/inventory/reports',
  '/om/sales-orders','/om/shipments','/om/returns','/om/quotes','/om/blanket-orders','/om/substitution-offers','/om/rtv','/om/order-notes','/om/order-dashboard','/om/sales-analysis','/om/commissions','/om/atp','/om/freight','/om/pick-pack-ship','/om/reports','/om/masters',
  '/bom','/bom/work-centers','/bom/routing-operations','/bom/build-orders','/bom/reports',
  '/projects','/projects/overview','/projects/tasks','/projects/budget','/projects/costs','/projects/billing','/projects/change-orders','/projects/analysis','/projects/reports',
  '/payroll/employees','/payroll/paycodes','/payroll/union','/payroll/timesheets','/payroll/runs','/payroll/expenses','/payroll/tax','/payroll/deductions','/payroll/pto','/payroll/manual','/payroll/reports','/payroll/garnishments','/payroll/setup',
  '/field-service/work-orders','/field-service/dispatch','/field-service/technicians','/field-service/contracts','/field-service/equipment','/field-service/slas','/field-service/territories','/field-service/rate-cards','/field-service/estimates','/field-service/pm','/field-service/van-stock','/field-service/warranty','/field-service/reports',
  '/reporting/catalog','/reporting/executive','/reporting/viewer','/reporting/designer','/reporting/quick-query','/reporting/drill-back','/reporting/scheduler','/reporting/categories','/reporting/parameter-sets','/reporting/usage','/reporting/sync-status',
]

const CREATE_TARGETS = {
  '/gl/journal-batches': 'journal batch',
  '/ap/vendors': 'vendor',
  '/ar/customers': 'customer',
  '/cash/bank-accounts': 'bank account',
  '/purchasing/requisitions': 'requisition',
  '/inventory/items': 'item',
  '/om/sales-orders': 'sales order',
  '/bom': 'BOM',
  '/projects': 'project',
  '/payroll/employees': 'employee',
  '/field-service/work-orders': 'work order',
}

const sleep = (ms) => new Promise(r => setTimeout(r, ms))
const START = parseInt(process.env.START || '0', 10)
const COUNT = parseInt(process.env.COUNT || String(ROUTES.length), 10)
let SLICE = ROUTES.slice(START, START + COUNT)
if (process.env.TARGETS) {
  const wanted = process.env.TARGETS.split(',').map(s => s.trim())
  SLICE = ROUTES.filter(r => wanted.includes(r))
}

function loadExisting() {
  try { return JSON.parse(readFileSync(OUT, 'utf8')) } catch { return [] }
}

// Attach collectors to a FRESH page; listeners are discarded when the page is closed.
function attachCollectors(page, bucket) {
  page.on('console', (m) => { if (m.type() === 'error') bucket.console.push(m.text().slice(0, 200)) })
  page.on('pageerror', (e) => bucket.pageErrors.push(String(e.message).slice(0, 200)))
  page.on('response', async (r) => {
    const s = r.status()
    if (s >= 400) {
      let body = ''
      try { body = (await r.text()).slice(0, 200) } catch { /* ignore */ }
      bucket.net.push(`${s} ${r.request().method()} ${r.url().replace(BASE, '')} :: ${body}`)
    }
  })
}

async function fillForm(page) {
  const inputs = await page.locator('input:visible:not([type="hidden"]):not([disabled])').all()
  for (const el of inputs) {
    try {
      const type = (await el.getAttribute('type')) || 'text'
      const valNow = await el.inputValue().catch(() => '')
      if (valNow) continue
      if (type === 'number' || type === 'date') await el.fill('1', { force: true }).catch(() => {})
      else if (type === 'checkbox' || type === 'radio') { /* leave */ }
      else await el.fill('Test', { force: true }).catch(() => {})
    } catch { /* ignore */ }
  }
  const selects = await page.locator('select:visible:not([disabled])').all()
  for (const sel of selects) {
    try {
      const opts = await sel.locator('option').all()
      let picked = false
      for (const o of opts) {
        const v = (await o.getAttribute('value')) || ''
        if (v && v !== '' && v !== '0') { await sel.selectOption({ value: v }).catch(() => {}); picked = true; break }
      }
      if (!picked) await sel.selectOption({ index: 1 }).catch(() => {})
    } catch { /* ignore */ }
  }
}

// Pick the first non-disabled real option (skip placeholder '' and '0').
async function pickSelect(sel) {
  const opts = await sel.locator('option').all()
  for (const o of opts) {
    const v = (await o.getAttribute('value')) || ''
    if (v && v !== '' && v !== '0') { await sel.selectOption({ value: v }).catch(() => {}); return true }
  }
  await sel.selectOption({ index: 1 }).catch(() => {})
  return false
}

async function tryCreate(page, bucket) {
  const newBtn = page.getByRole('button', { name: /new|create|add|^\+/i }).first()
  if (!(await newBtn.count())) return { attempted: false, reason: 'no create button' }
  await newBtn.click().catch(() => {})
  await sleep(1200)
  await fillForm(page)
  await sleep(400)
  const submit = page.getByRole('button', { name: /save|create|submit|add|confirm/i }).first()
  if (await submit.count()) {
    let captured = null
    const onResp = async (r) => {
      if (r.request().method() === 'POST' && r.status() >= 400) {
        let b = ''
        try { b = (await r.text()).slice(0, 200) } catch { /* ignore */ }
        captured = `${r.status()} ${r.url().replace(BASE, '')} :: ${b}`
        bucket.net.push(captured)
      }
    }
    page.on('response', onResp)
    await submit.click().catch(() => {})
    await sleep(2500)
    page.off('response', onResp)
    return { attempted: true, status: captured ? 'server-error' : 'submitted' }
  }
  return { attempted: true, status: 'no-submit-found' }
}

async function main() {
  mkdirSync('scripts', { recursive: true })
  const results = loadExisting()
  const browser = await chromium.launch({ headless: true })
  const context = await browser.newContext()
  const loginPage = await context.newPage()
  loginPage.setDefaultTimeout(12000)
  // login once; token persists in context localStorage
  await loginPage.goto(BASE + '/login', { waitUntil: 'domcontentloaded', timeout: 20000 })
  await loginPage.waitForSelector('#email', { state: 'visible', timeout: 15000 })
  await loginPage.fill('#email', EMAIL, { force: true })
  await loginPage.fill('#password', PW, { force: true })
  await loginPage.click('button[type="submit"]')
  await loginPage.waitForTimeout(2500)
  await loginPage.close()

  for (const route of SLICE) {
    const page = await context.newPage()
    page.setDefaultTimeout(12000)
    const bucket = { route, console: [], pageErrors: [], net: [], title: '', create: null }
    attachCollectors(page, bucket)
    try {
      await page.goto(BASE + route, { waitUntil: 'domcontentloaded', timeout: 15000 })
      await sleep(900)
      if (CREATE_TARGETS[route]) bucket.create = await tryCreate(page, bucket)
      const title = await page.locator('h1').first().textContent().catch(() => '')
      bucket.title = (title || '').slice(0, 80)
      bucket.pass = bucket.pageErrors.length === 0 && !bucket.net.some(n => n.startsWith('5'))
      const idx = results.findIndex(r => r.route === route)
      if (idx >= 0) results[idx] = bucket; else results.push(bucket)
      writeFileSync(OUT, JSON.stringify(results, null, 2))
      const five = bucket.net.filter(n => n.startsWith('5')).length
      console.log(`[smoke] ${route} title="${bucket.title}" errs=${bucket.pageErrors.length} net4xx=${bucket.net.length} 5xx=${five} create=${bucket.create ? bucket.create.status : 'n/a'}`)
    } catch (e) {
      bucket.fatal = String(e.message).slice(0, 200)
      bucket.pass = false
      const idx = results.findIndex(r => r.route === route)
      if (idx >= 0) results[idx] = bucket; else results.push(bucket)
      writeFileSync(OUT, JSON.stringify(results, null, 2))
      console.log(`[FAIL] ${route} :: ${bucket.fatal}`)
    } finally {
      await page.close()
    }
  }
  await browser.close()
  const fails = results.filter(r => !r.pass)
  console.log(`\n=== SLICE DONE: ${SLICE.length} processed, total ${results.length} routes, ${fails.length} failing ===`)
}

main().catch((e) => { console.error('HARNESS ERROR', e); process.exit(1) })
