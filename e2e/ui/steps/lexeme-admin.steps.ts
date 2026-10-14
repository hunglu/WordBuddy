import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Step text starts with "lexeme admin:" because playwright-bdd step text is global across files.
// The step "the admin is logged in" lives in navigation.steps.ts.
const learnerPassword = 'ChangeMe123!'

let fragment = ''

async function register(request: import('@playwright/test').APIRequestContext): Promise<{ email: string; token: string }> {
  const email = `lexeme-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email, password: learnerPassword, displayName: 'E2E Lexeme User', ageGroup: 'Adult' },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  return { email, token: ((await response.json()) as { token: string }).token }
}

// A learner adds a word, which creates its lexeme (no part of speech yet).
Given('lexeme admin: a lexeme exists whose word has a unique fragment in the middle', async ({ request }) => {
  fragment = Math.random().toString(36).slice(2, 10) + Date.now().toString(36)
  const { token } = await register(request)
  const add = await request.post('/api/vocabulary', {
    headers: { Authorization: `Bearer ${token}` },
    data: { word: `pre${fragment}post`, definition: 'A made-up word for the lexeme admin E2E test.', example: null },
  })
  expect(add.ok(), await add.text()).toBeTruthy()
})

When('lexeme admin: they search the Lexemes page for that fragment', async ({ page }) => {
  await page.locator('aside').getByRole('link', { name: 'Lexemes' }).click()
  await expect(page.getByRole('heading', { name: 'Lexemes', exact: true })).toBeVisible()
  await page.getByLabel('Search').fill(fragment)
  // The word is only found because the search matches the middle of it.
  await expect(page.getByRole('link', { name: `pre${fragment}post`, exact: true }).first()).toBeVisible()
})

When('lexeme admin: they open the lexeme from the results', async ({ page }) => {
  await page.getByRole('link', { name: `pre${fragment}post`, exact: true }).first().click()
  await expect(page.getByRole('heading', { name: `pre${fragment}post` })).toBeVisible()
})

When(
  'lexeme admin: they set part of speech {string}, UK IPA {string} and CEFR level {string}',
  async ({ page }, partOfSpeech: string, ipa: string, cefr: string) => {
    await page.getByLabel('Part of speech').selectOption(partOfSpeech)
    await page.getByLabel('IPA (UK)').fill(ipa)
    await page.getByLabel('CEFR level').selectOption(cefr)
    await page.getByRole('button', { name: 'Save changes' }).click()
  },
)

Then('lexeme admin: the form shows the message {string}', async ({ page }, message: string) => {
  await expect(page.getByText(message, { exact: true })).toBeVisible()
})

Then(
  'lexeme admin: after a reload the form shows UK IPA {string} and CEFR level {string}',
  async ({ page }, ipa: string, cefr: string) => {
    await page.reload()
    await expect(page.getByLabel('IPA (UK)')).toHaveValue(ipa)
    await expect(page.getByLabel('CEFR level')).toHaveValue(cefr)
  },
)

Given('lexeme admin: a learner is logged in', async ({ page, request }) => {
  const { email } = await register(request)
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(learnerPassword)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
})

When('lexeme admin: they open {string} directly', async ({ page }, path: string) => {
  await page.goto(path)
})

Then('lexeme admin: they are sent back to the home page', async ({ page }) => {
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
})

Then('lexeme admin: the sidebar has no {string} item', async ({ page }, label: string) => {
  await expect(page.locator('aside')).toBeVisible()
  await expect(page.locator('aside').getByRole('link', { name: label })).toHaveCount(0)
})
