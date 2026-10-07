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

// WB-23: the self-rated recall check is replaced by the review page; its old route redirects.
When('they open the old recall check address', async ({ page }) => {
  await page.goto('/vocabulary/check')
})

Then('they land on the review page', async ({ page }) => {
  await expect(page).toHaveURL(/\/vocabulary\/review$/)
  await expect(page.getByRole('heading', { name: 'Review' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Review' })).toBeVisible()
  await expect(page.getByRole('link', { name: /recall check/i })).toHaveCount(0)
})
