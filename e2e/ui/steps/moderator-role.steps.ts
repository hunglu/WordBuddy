import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Step text starts with "moderator role:" because playwright-bdd step text is global across files.
// The step "the admin is logged in" lives in navigation.steps.ts.
// Every scenario uses a fresh account, so the seeded users keep their roles.
const password = 'ChangeMe123!'
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@wordbuddy.com'
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123'

let account = { email: '', id: '' }

Given('moderator role: a new adult account exists', async ({ request }) => {
  const email = `moderator-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email, password, displayName: 'E2E Moderator UI', ageGroup: 'Adult' },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  account = { email, id: ((await response.json()) as { user: { id: string } }).user.id }
})

Given('moderator role: the admin has granted it Moderator', async ({ request }) => {
  const login = await request.post('/api/auth/login', { data: { email: adminEmail, password: adminPassword } })
  expect(login.ok(), await login.text()).toBeTruthy()
  const { token } = (await login.json()) as { token: string }
  const grant = await request.put(`/api/auth/admin/users/${account.id}/moderator`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { isModerator: true },
  })
  expect(grant.status(), await grant.text()).toBe(204)
})

Given('moderator role: that account is logged in', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill(account.email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
})

When('moderator role: the admin opens {string} and searches for that account', async ({ page }, item: string) => {
  await page.locator('aside').getByRole('link', { name: item }).click()
  await expect(page.getByRole('heading', { name: 'Users & roles' })).toBeVisible()
  await page.getByLabel('Search by name or email').fill(account.email)
  await expect(page.getByText(account.email, { exact: true })).toBeVisible()
})

When('moderator role: the admin clicks {string}', async ({ page }, label: string) => {
  await page.getByRole('button', { name: label }).click()
})

Then('moderator role: the account row shows the {string} badge', async ({ page }, badge: string) => {
  await expect(page.getByText(badge, { exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Revoke Moderator' })).toBeVisible()
})

Then('moderator role: the account row shows no {string} badge', async ({ page }, badge: string) => {
  await expect(page.getByRole('button', { name: 'Grant Moderator' })).toBeVisible()
  await expect(page.getByText(badge, { exact: true })).toHaveCount(0)
})

Then('moderator role: the sidebar has the items {string}', async ({ page }, items: string) => {
  for (const label of items.split(', ')) {
    await expect(page.locator('aside').getByRole('link', { name: label })).toBeVisible()
  }
})

Then('moderator role: the sidebar has no {string} item', async ({ page }, label: string) => {
  await expect(page.locator('aside')).toBeVisible()
  await expect(page.locator('aside').getByRole('link', { name: label })).toHaveCount(0)
})

When('moderator role: they open {string} directly', async ({ page }, path: string) => {
  await page.goto(path)
})

Then('moderator role: they are sent back to the home page', async ({ page }) => {
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
})
