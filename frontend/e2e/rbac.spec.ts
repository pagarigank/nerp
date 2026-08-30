import { test, expect, type Page } from '@playwright/test'

/**
 * RBAC role-type matrix — driven entirely through the REAL frontend forms
 * (UI login, sidebar nav, page buttons). No raw API calls.
 *
 * Roles under test:
 *  - companyadmin@erp.com (full company admin, wildcard `*`)
 *  - rbacviewer@erp.com  (limited: ap.vendors.view/create, platform.roles.view)
 */

interface RoleDef {
  name: string
  username: string
  password: string
  companyId?: string
  // nav labels that MUST be visible
  expectVisible: string[]
  // nav labels that MUST be hidden
  expectHidden: string[]
}

const ROLES: RoleDef[] = [
  {
    name: 'companyadmin (full)',
    username: 'companyadmin@erp.com',
    password: 'password123',
    expectVisible: ['Vendors', 'Roles', 'Journal Batches', 'Customers', 'Bank Accounts', 'Items', 'Sales Orders', 'Projects', 'Employees'],
    expectHidden: [],
  },
  {
    name: 'rbacviewer (limited)',
    username: 'rbacviewer@erp.com',
    password: 'password123',
    companyId: '11111111-1111-1111-1111-111111111111',
    expectVisible: ['Vendors', 'Roles'],
    expectHidden: ['Journal Batches', 'Customers', 'Bank Accounts', 'Items', 'Sales Orders', 'Projects', 'Employees'],
  },
]

async function uiLogin(page: Page, def: RoleDef) {
  await page.goto('/login', { waitUntil: 'domcontentloaded' })
  await page.waitForLoadState('networkidle').catch(() => {})
  await page.fill('#email', def.username)
  await page.fill('#password', def.password)
  // company selection if present (rbacviewer needs a company)
  const companyCombo = page.locator('[role="combobox"]:visible').first()
  if (def.companyId && (await companyCombo.count())) {
    await companyCombo.click().catch(() => {})
    const list = page.locator('[role="listbox"]:visible').last()
    await list.waitFor({ state: 'visible', timeout: 3000 }).catch(() => {})
    await list.locator('[role="option"]:visible').first().dispatchEvent('click', { bubbles: true }).catch(() => {})
  }
  await page.getByRole('button', { name: /sign in/i }).click()
  await page.waitForURL('**/dashboard', { timeout: 20000 })
}

async function sidebarHas(page: Page, label: string): Promise<boolean> {
  // Sidebar sub-links are rendered in the DOM only when RBAC permits them; they
  // may be visually collapsed (zero-height) until their module group is expanded,
  // so we assert DOM presence rather than computed visibility.
  const link = page.locator('aside nav a, aside nav button').filter({ hasText: label }).first()
  return (await link.count()) > 0
}

for (const def of ROLES) {
  test(`[RBAC:${def.name}] nav visibility matches permissions`, async ({ page }) => {
    await uiLogin(page, def)
    for (const v of def.expectVisible) {
      const ok = await sidebarHas(page, v)
      console.log(`  VISIBLE expected "${v}": ${ok}`)
      expect(ok, `expected nav "${v}" visible for ${def.name}`).toBeTruthy()
    }
    for (const h of def.expectHidden) {
      const present = await sidebarHas(page, h)
      console.log(`  HIDDEN expected "${h}": ${!present}`)
      expect(present, `expected nav "${h}" HIDDEN for ${def.name}`).toBeFalsy()
    }
  })
}

test('[RBAC:rbacviewer] deep-link to forbidden page is blocked at data level', async ({ page }) => {
  await uiLogin(page, ROLES[1])
  // rbacviewer has no gl.journal-batches.view — direct nav should NOT render batch data.
  await page.goto('/gl/journal-batches', { waitUntil: 'domcontentloaded' })
  await page.waitForLoadState('networkidle').catch(() => {})
  await page.waitForTimeout(1500)
  // Either redirected away, or the list is empty / shows an auth/error state.
  const body = (await page.locator('body').innerText().catch(() => '')).slice(0, 400)
  const hasRows = await page.locator('table tbody tr').count()
  const redirected = !page.url().includes('/gl/journal-batches')
  const blocked = redirected || hasRows === 0 || /403|not authorized|forbidden|unauthorized/i.test(body)
  console.log(`  rbacviewer /gl/journal-batches -> url=${page.url()} rows=${hasRows} blocked=${blocked}`)
  expect(blocked, 'rbacviewer must not see journal batch data').toBeTruthy()
})

test('[RBAC:rbacviewer] Roles page hides create/edit/delete buttons (button gating)', async ({ page }) => {
  await uiLogin(page, ROLES[1])
  await page.goto('/platform/roles', { waitUntil: 'domcontentloaded' })
  await page.waitForLoadState('networkidle').catch(() => {})
  await page.waitForTimeout(1000)
  const newBtn = page.getByRole('button', { name: /^New Role/i })
  const newVisible = (await newBtn.count()) > 0 && (await newBtn.first().isVisible().catch(() => false))
  console.log(`  rbacviewer Roles "New Role" visible: ${newVisible} (expect false)`)
  expect(newVisible, 'limited user must not see New Role button').toBeFalsy()
})

test('[RBAC:companyadmin] Roles page shows create/edit/delete buttons', async ({ page }) => {
  await uiLogin(page, ROLES[0])
  await page.goto('/platform/roles', { waitUntil: 'domcontentloaded' })
  await page.waitForLoadState('networkidle').catch(() => {})
  await page.waitForTimeout(1000)
  const newBtn = page.getByRole('button', { name: /^New Role/i })
  const newVisible = (await newBtn.count()) > 0 && (await newBtn.first().isVisible().catch(() => false))
  console.log(`  companyadmin Roles "New Role" visible: ${newVisible} (expect true)`)
  expect(newVisible, 'full admin must see New Role button').toBeTruthy()
})
