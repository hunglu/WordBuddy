import { expect, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// A fresh word per scenario avoids collisions with words left behind by earlier runs.
let typoWord = ''
let fixedWord = ''

function wordCard(page: Page, text: string) {
  return page.locator('.rounded-wb-card').filter({ hasText: text })
}

Given('the learner has added a private word with a typo', async ({ page }) => {
  typoWord = `e2e-edit-${Date.now()}`
  fixedWord = `${typoWord}-fixed`

  await page.getByRole('link', { name: /My Vocabulary/i }).click()
  await expect(page).toHaveURL(/\/vocabulary$/)
  await page.getByLabel('Word', { exact: true }).fill(typoWord)
  await page.getByLabel('Definition').fill('A word added by the edit E2E test.')
  await page.getByRole('button', { name: /add word/i }).click()
  await expect(wordCard(page, typoWord)).toBeVisible()
})

When('they edit the word and save the corrected text', async ({ page }) => {
  const card = wordCard(page, typoWord)
  await card.getByRole('button', { name: 'Edit', exact: true }).click()
  await card.getByLabel('Word', { exact: true }).fill(fixedWord)
  await card.getByRole('button', { name: 'Save', exact: true }).click()
})

Then('the corrected word is in their vocabulary list', async ({ page }) => {
  await expect(wordCard(page, fixedWord)).toBeVisible()
})

Then('the typo word is gone from their vocabulary list', async ({ page }) => {
  await expect(page.getByText(typoWord, { exact: true })).toHaveCount(0)
})

When('they submit the word for sharing', async ({ page }) => {
  await wordCard(page, typoWord).getByRole('button', { name: 'Share', exact: true }).click()
  await expect(wordCard(page, typoWord).getByText('PendingReview')).toBeVisible()
})

Then('the word has no Edit button', async ({ page }) => {
  await expect(wordCard(page, typoWord).getByRole('button', { name: 'Edit', exact: true })).toHaveCount(0)
})
