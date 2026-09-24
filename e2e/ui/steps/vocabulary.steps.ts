import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Same test account convention as steps/login.steps.ts — override via env vars rather than
// committing real credentials.
const testEmail = process.env.E2E_TEST_EMAIL ?? 'learner@example.com'
const testPassword = process.env.E2E_TEST_PASSWORD ?? 'ChangeMe123!'

// A fresh word per test run avoids collisions with words left behind by previous runs against
// the same shared test account.
let addedWord = ''

Given('the learner is logged in', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill(testEmail)
  await page.getByLabel('Password').fill(testPassword)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
})

When('they add a new vocabulary word', async ({ page }) => {
  addedWord = `e2e-word-${Date.now()}`

  await page.getByRole('link', { name: /My Vocabulary/i }).click()
  await expect(page).toHaveURL(/\/vocabulary$/)

  await page.getByLabel('Word').fill(addedWord)
  await page.getByLabel('Definition').fill('A word added by the personal vocabulary E2E test.')
  await page.getByLabel('Example (optional)').fill('This is an example sentence.')
  await page.getByRole('button', { name: /add word/i }).click()
})

Then('the word appears in their vocabulary list', async ({ page }) => {
  await expect(page.getByText(addedWord)).toBeVisible()
})

When('they start a recall check', async ({ page }) => {
  await page.getByRole('link', { name: /start recall check/i }).click()
  await expect(page).toHaveURL(/\/vocabulary\/check$/)

  await page.getByRole('button', { name: '5 words' }).click()
  await page.getByRole('button', { name: /start check/i }).click()
})

Then('they can complete the check and see their result', async ({ page }) => {
  const completeHeading = page.getByText('Check complete!')
  const knowButton = page.getByRole('button', { name: 'I Know This' })

  // The random check returns anywhere from 1 up to the requested word count — click through
  // however many flashcards appear until the completion screen shows.
  for (let i = 0; i < 5 && !(await completeHeading.isVisible()); i++) {
    await knowButton.click()
  }

  await expect(completeHeading).toBeVisible()
})
