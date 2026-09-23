import { defineConfig, devices } from '@playwright/test'
import { defineBddConfig, cucumberReporter } from 'playwright-bdd'

const testDir = defineBddConfig({
  features: 'features/*.feature',
  steps: 'steps/*.ts',
})

// Defaults to the docker-compose local-dev UI port. Override to point at a different
// environment, e.g. UI_BASE_URL=http://localhost:5173 for the Vite dev server.
const baseURL = process.env.UI_BASE_URL ?? 'http://localhost:3000'

export default defineConfig({
  testDir,
  fullyParallel: true,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    cucumberReporter('html', { outputFile: 'playwright-report/cucumber-report.html' }),
  ],
  use: {
    baseURL,
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
})
