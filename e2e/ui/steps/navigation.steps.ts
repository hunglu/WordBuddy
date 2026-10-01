import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

When('they click the {string} sidebar item', async ({ page }, label: string) => {
  await page.locator('aside').getByRole('link', { name: label }).click()
})

// The selected sidebar item is the one carrying aria-current="page" (see AppLayout.tsx).
Then('only the {string} sidebar item is selected', async ({ page }, label: string) => {
  const sidebarLinks = page.locator('aside a')
  await expect(sidebarLinks.filter({ hasText: label })).toHaveAttribute('aria-current', 'page')

  // Exactly one item is marked current, and it is the expected one.
  const selected = page.locator('aside a[aria-current="page"]')
  await expect(selected).toHaveCount(1)
  await expect(selected).toContainText(label)
})

When('they open {string} directly', async ({ page }, path: string) => {
  await page.goto(path)
  await expect(page.locator('aside')).toBeVisible()
})

// Defaults to the Identity dev-seed admin (same convention as e2e/api) — override via env vars
// rather than committing real credentials.
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@wordbuddy.com'
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123'

Given('the admin is logged in', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill(adminEmail)
  await page.getByLabel('Password').fill(adminPassword)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
})
