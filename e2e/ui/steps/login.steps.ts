import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Test account must already exist in the target environment's Identity service.
// Override via env vars rather than committing real credentials.
const testEmail = process.env.E2E_TEST_EMAIL ?? 'learner@example.com'
const testPassword = process.env.E2E_TEST_PASSWORD ?? 'ChangeMe123!'

Given('the learner is on the login page', async ({ page }) => {
  await page.goto('/login')
})

When('they submit valid credentials', async ({ page }) => {
  await page.getByLabel('Email').fill(testEmail)
  await page.getByLabel('Password').fill(testPassword)
  await page.getByRole('button', { name: /log in/i }).click()
})

Then('they land on the dashboard', async ({ page }) => {
  // Successful login redirects to "/" (DashboardPage) — anywhere else (e.g. still on /login) fails.
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
  await expect(page.locator('h1')).toBeVisible()
})
