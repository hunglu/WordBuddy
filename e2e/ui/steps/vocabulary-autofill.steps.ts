import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { When, Then } = createBdd()

// WB-25. Runs against the real stack. The e2e Content container uses the Development-only fake
// auto-fill clients (`Autofill__UseFakeClients=true`): "serendipity" is a known word, any other
// word returns `autofillUnavailable = true`.
const knownDefinition = 'Finding something good by chance.'
let unknownWord = ''

When('they auto-fill the word {string}', async ({ page }, word: string) => {
  await page.getByRole('link', { name: /My Vocabulary/i }).click()
  await expect(page).toHaveURL(/\/vocabulary$/)
  await page.getByLabel('Word').fill(word)
  await page.getByRole('button', { name: 'Auto-fill' }).click()
})

Then('they see the auto-filled sense card', async ({ page }) => {
  await expect(page.getByText(knownDefinition).first()).toBeVisible()
  await expect(page.getByText('Noun').first()).toBeVisible()
  await expect(page.getByText('sự tình cờ may mắn').first()).toBeVisible()
})

When('they add the auto-filled sense', async ({ page }) => {
  await page.getByRole('button', { name: 'Add', exact: true }).click()
})

Then('the sense card shows it was added', async ({ page }) => {
  await expect(page.getByRole('button', { name: 'Added', exact: true })).toBeDisabled()
})

When('they auto-fill an unknown word', async ({ page }) => {
  unknownWord = `zzqxautofill${Date.now().toString(36).replace(/[0-9]/g, (d) => "abcdefghij"[Number(d)])}`
  await page.getByRole('link', { name: /My Vocabulary/i }).click()
  await expect(page).toHaveURL(/\/vocabulary$/)
  await page.getByLabel('Word').fill(unknownWord)
  await page.getByRole('button', { name: 'Auto-fill' }).click()
})

Then('they are told to fill in the form instead', async ({ page }) => {
  await expect(page.getByRole('status')).toContainText(/fill in/i, { timeout: 30_000 })
})

When('they complete the manual form', async ({ page }) => {
  await expect(page.getByLabel('Word')).toHaveValue(unknownWord)
  await page.getByLabel('Definition').fill('A word added after auto-fill was unavailable.')
  await page.getByRole('button', { name: /add word/i }).click()
})

Then('the unknown word appears in their vocabulary list', async ({ page }) => {
  await expect(page.getByText(unknownWord)).toBeVisible()
})
