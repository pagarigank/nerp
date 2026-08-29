// Focused diagnostic: open create form on given routes (fresh page per route,
// matching the working harness), fill generically, capture the EXACT POST
// request body + full 500 response body.
import { chromium } from 'playwright'
const BASE = 'http://localhost:3000'
const EMAIL = 'companyadmin@erp.com'
const PW = 'password123'
const ROUTES = process.argv.slice(2)
const sleep = (ms) => new Promise(r => setTimeout(r, ms))

const browser = await chromium.launch({ headless: true })
const context = await browser.newContext()

async function login(page) {
  page.setDefaultTimeout(12000)
  await page.goto(BASE + '/login', { waitUntil: 'domcontentloaded', timeout: 20000 })
  await page.waitForSelector('#email', { state: 'visible', timeout: 15000 })
  await page.fill('#email', EMAIL, { force: true })
  await page.fill('#password', PW, { force: true })
  await page.click('button[type="submit"]')
  await page.waitForTimeout(2500)
}

for (const route of ROUTES) {
  const page = await context.newPage()
  page.setDefaultTimeout(12000)
  const captured = []
  page.on('request', async (req) => {
    if (req.method() === 'POST' && req.url().includes('/api/')) {
      try { const b = req.postData(); captured.push({ url: req.url().replace(BASE, ''), body: b }) } catch {}
    }
  })
  page.on('response', async (res) => {
    if (res.request().method() === 'POST' && res.url().includes('/api/') && res.status() >= 400) {
      let body = ''
      try { body = await res.text() } catch {}
      const c = captured.find(x => x.url === res.url().replace(BASE, ''))
      console.log(`\n### ${res.status()} ${res.url().replace(BASE, '')}`)
      console.log(`REQUEST BODY: ${(c ? c.body : '(not captured)').slice(0, 1800)}`)
      console.log(`RESPONSE BODY: ${body.slice(0, 1800)}`)
    }
  })
  try {
    console.log(`\n===== ${route} =====`)
    await login(page)
    await page.goto(BASE + route, { waitUntil: 'domcontentloaded', timeout: 15000 })
    await sleep(1200)
    const newBtn = page.getByRole('button', { name: /new|create|add|^\+/i }).first()
    if (!(await newBtn.count())) { console.log('  no create button'); await page.close(); continue }
    await newBtn.click().catch(() => {})
    await sleep(1200)
    const inputs = await page.locator('input:visible:not([type="hidden"]):not([disabled])').all()
    for (const el of inputs) {
      try {
        const type = (await el.getAttribute('type')) || 'text'
        if (await el.inputValue().catch(() => '')) continue
        if (type === 'number' || type === 'date') await el.fill('1', { force: true }).catch(() => {})
        else await el.fill('Test', { force: true }).catch(() => {})
      } catch {}
    }
    const selects = await page.locator('select:visible:not([disabled])').all()
    for (const sel of selects) {
      try {
        const opts = await sel.locator('option').all()
        let picked = false
        for (const o of opts) { const v = (await o.getAttribute('value')) || ''; if (v && v !== '' && v !== '0') { await sel.selectOption({ value: v }).catch(() => {}); picked = true; break } }
        if (!picked) await sel.selectOption({ index: 1 }).catch(() => {})
      } catch {}
    }
    await sleep(400)
    const submit = page.getByRole('button', { name: /save|create|submit|add|confirm/i }).first()
    if (await submit.count()) { await submit.click().catch(() => {}); await sleep(3000) }
    else console.log('  no submit found')
  } catch (e) {
    console.log(`  ROUTE ERROR: ${String(e.message).slice(0, 160)}`)
  } finally {
    await page.close()
  }
}
await browser.close()
console.log('\nDIAG DONE')
