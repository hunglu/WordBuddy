import { expect, type APIRequestContext, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Moderation is set up through the API (same origin, proxied by the UI) — the moderation page
// itself is not under test here. Same admin convention as steps/navigation.steps.ts.
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@wordbuddy.com'
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123'

// A fresh word per scenario: identical text would dedupe onto a word an earlier run shared.
let sharedWord = ''

function wordCard(page: Page) {
  return page.locator('.rounded-wb-card').filter({ hasText: sharedWord })
}

async function approveAsAdmin(request: APIRequestContext, word: string): Promise<void> {
  const login = await request.post('/api/auth/login', { data: { email: adminEmail, password: adminPassword } })
  expect(login.ok()).toBeTruthy()
  const { token } = (await login.json()) as { token: string }
  const headers = { Authorization: `Bearer ${token}` }

  const pending = await request.get('/api/vocabulary/moderation/pending', { headers })
  expect(pending.ok()).toBeTruthy()
  const items = (await pending.json()) as { id: string; word: string }[]
  const item = items.find((w) => w.word === word)
  expect(item, `"${word}" should be in the moderation queue`).toBeDefined()

  const approve = await request.post(`/api/vocabulary/moderation/${item!.id}`, {
    headers,
    data: { approve: true, visibleToChildren: true },
  })
  expect(approve.status()).toBe(204)
}

Given('the learner has a shared word approved by a moderator', async ({ page, request }) => {
  sharedWord = `e2e-shared-${Date.now()}`

  await page.getByRole('link', { name: /My Vocabulary/i }).click()
  await expect(page).toHaveURL(/\/vocabulary$/)
  await page.getByLabel('Word').fill(sharedWord)
  await page.getByLabel('Definition').fill('A word shared by the vocabulary sharing E2E test.')
  await page.getByRole('button', { name: /add word/i }).click()

  await wordCard(page).getByRole('button', { name: 'Share', exact: true }).click()
  await expect(wordCard(page).getByText('PendingReview')).toBeVisible()

  await approveAsAdmin(request, sharedWord)
  await page.reload()
  await expect(wordCard(page).getByText('Shared', { exact: true })).toBeVisible()
})

When('they open the Community Word Pool', async ({ page }) => {
  await page.goto('/vocabulary/shared')
  await expect(page.getByRole('heading', { name: 'Community Word Pool' })).toBeVisible()
})

Then('their shared word shows a "Your word" badge instead of "Add to My List"', async ({ page }) => {
  await expect(wordCard(page).getByText('Your word')).toBeVisible()
  await expect(wordCard(page).getByRole('button', { name: 'Add to My List' })).toHaveCount(0)
})

When('they delete the shared word from My Vocabulary', async ({ page }) => {
  await page.goto('/vocabulary')
  await wordCard(page).getByRole('button', { name: 'Delete', exact: true }).click()
})

Then('a confirmation dialog says the word will be handed over to WordBuddy', async ({ page }) => {
  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  await expect(dialog).toContainText('handed over to WordBuddy')
  await expect(dialog).toContainText('Community Word Pool')
})

When('they confirm the deletion', async ({ page }) => {
  await page.getByRole('dialog').getByRole('button', { name: 'Delete', exact: true }).click()
  await expect(page.getByRole('dialog')).toHaveCount(0)
})

Then('the shared word is gone from My Vocabulary', async ({ page }) => {
  await expect(page.getByText(sharedWord)).toHaveCount(0)
})

Then('the shared word is in the Community Word Pool with "Add to My List"', async ({ page }) => {
  await page.goto('/vocabulary/shared')
  await expect(wordCard(page).getByRole('button', { name: 'Add to My List' })).toBeVisible()
  await expect(wordCard(page).getByText('Your word')).toHaveCount(0)
})
