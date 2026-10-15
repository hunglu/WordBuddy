import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

const testEmail = process.env.E2E_TEST_EMAIL ?? 'learner@example.com'
const testPassword = process.env.E2E_TEST_PASSWORD ?? 'ChangeMe123!'

// "My words" is a card grid with 24 cards per page (CARD_PAGE_SIZE in the UI).
const CARD_PAGE_SIZE = 24

Given('the learner has more words than fit on one page', async ({ request }) => {
  const login = await request.post('/api/auth/login', { data: { email: testEmail, password: testPassword } })
  expect(login.ok(), await login.text()).toBeTruthy()
  const { token } = (await login.json()) as { token: string }
  const headers = { Authorization: `Bearer ${token}` }

  const mine = await request.get('/api/vocabulary/mine?page=1&pageSize=1', { headers })
  expect(mine.ok(), await mine.text()).toBeTruthy()
  const { totalCount } = (await mine.json()) as { totalCount: number }

  for (let i = totalCount; i <= CARD_PAGE_SIZE; i++) {
    const add = await request.post('/api/vocabulary', {
      headers,
      data: { word: `e2e-page-${Date.now()}-${i}`, definition: 'A word added by the paging E2E test.' },
    })
    expect(add.ok(), await add.text()).toBeTruthy()
  }
})

When('they open their vocabulary list', async ({ page }) => {
  await page.goto('/vocabulary')
})

Then('the vocabulary pager shows page 1', async ({ page }) => {
  await expect(page.getByRole('navigation').getByText(/^Page 1 of \d+/)).toBeVisible()
})

When('they go to the next vocabulary page', async ({ page }) => {
  await page.getByRole('button', { name: 'Next', exact: true }).click()
})

Then('the vocabulary pager shows page 2', async ({ page }) => {
  await expect(page.getByRole('navigation').getByText(/^Page 2 of \d+/)).toBeVisible()
})
