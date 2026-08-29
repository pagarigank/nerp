import { defineConfig, devices } from '@playwright/test'

/**
 * Playwright config for NERP ERP frontend E2E (form-based) testing.
 * See D:/nerp/E2E_TESTING.md for the plan.
 */
export default defineConfig({
  testDir: './e2e',
  testMatch: '**/*.spec.ts',
  fullyParallel: false, // keep module flow ordering deterministic
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: [
    ['list'],
    ['json', { outputFile: 'e2e-report.json' }],
  ],
  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15000,
    navigationTimeout: 30000,
  },
  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
      teardown: 'cleanup',
    },
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: '.auth/companyadmin.json',
      },
      dependencies: ['setup'],
    },
    {
      name: 'cleanup',
      testMatch: /auth\.cleanup\.ts/,
    },
  ],
})
