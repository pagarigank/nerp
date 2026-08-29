import { test as setup, expect } from '@playwright/test'
import { mkdirSync } from 'fs'

const AUTH_FILE = '.auth/companyadmin.json'

setup('authenticate as company admin', async ({ page }) => {
  mkdirSync('.auth', { recursive: true })

  await page.goto('/login')
  await expect(page).toHaveTitle(/NERP|ERP/i).catch(() => {})

  // Login form fields (LoginPage.tsx uses id="email" / id="password").
  await page.fill('#email', 'companyadmin@erp.com')
  await page.fill('#password', 'password123')
  await page.getByRole('button', { name: /sign in/i }).click()

  // Wait until we land on the dashboard (auth succeeded).
  await page.waitForURL('**/dashboard', { timeout: 20000 })
  await expect(page).toHaveURL(/dashboard/)

  // Persist the auth localStorage (erp-auth-storage) + cookies.
  await page.context().storageState({ path: AUTH_FILE })
})
