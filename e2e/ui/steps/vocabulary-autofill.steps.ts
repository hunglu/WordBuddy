import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// WB-25. The Content container has no Claude key, so a live look-up always returns
// `autofillUnavailable = true`. The happy path therefore stubs the two auto-fill calls in the
// browser; the failure path uses the real backend (a live dictionary miss can take several seconds).
const stubSenseId = '00000000-0000-4000-8000-000000000025'
const stubDefinition = 'Finding something good without looking for it.'
let unknownWord = ''

Given('the auto-fill service knows the word {string}', async ({ page }, word: string) => {
  await page.route('**/api/vocabulary/autofill?**', (route) =>
    route.fulfill({
      json: {
        word,
        autofillUnavailable: false,
        senses: [
          {
            senseId: stubSenseId,
            word,
            definition: stubDefinition,
            partOfSpeech: 'Noun',
            ipaUk: '/ˌser.ənˈdɪp.ə.ti/',
            ipaUs: null,
            audioUkUrl: null,
            audioUsUrl: null,
            examples: ['Meeting her was pure serendipity.'],
            translations: [{ locale: 'vi', text: 'sự tình cờ may mắn' }],
            collocations: [],
            synonyms: [],
            antonyms: [],
            topicTags: [],
            registerNote: null,
            origin: 'AutoFill',
            awaitingApproval: false,
          },
        ],
      },
    }),
  )
  await page.route(`**/api/vocabulary/autofill/${stubSenseId}/add-to-mine`, (route) =>
    route.fulfill({ json: stubSenseId }),
  )
})

When('they auto-fill the word {string}', async ({ page }, word: string) => {
  await page.getByRole('link', { name: /My Vocabulary/i }).click()
  await expect(page).toHaveURL(/\/vocabulary$/)
  await page.getByLabel('Word').fill(word)
  await page.getByRole('button', { name: 'Auto-fill' }).click()
})

Then('they see the auto-filled sense card', async ({ page }) => {
  await expect(page.getByText(stubDefinition)).toBeVisible()
  await expect(page.getByText('Noun')).toBeVisible()
  await expect(page.getByText('sự tình cờ may mắn')).toBeVisible()
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
