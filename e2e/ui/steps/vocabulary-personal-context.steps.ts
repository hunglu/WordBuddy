import { expect, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// A fresh word per scenario avoids collisions with words left behind by earlier runs.
let noteWord = ''

function wordCard(page: Page, text: string) {
  return page.locator('.rounded-wb-card').filter({ hasText: text })
}

Given('the learner has added a word with a note', async ({ page }) => {
  noteWord = `e2e-note-${Date.now()}`

  await page.getByRole('link', { name: /My Vocabulary/i }).click()
  await expect(page).toHaveURL(/\/vocabulary$/)
  await page.getByLabel('Word', { exact: true }).fill(noteWord)
  await page.getByLabel('Definition').fill('A word added by the personal note E2E test.')
  await page.getByLabel(/Where did you meet this word/i).fill('heard at the zoo')
  await page.getByRole('button', { name: /add word/i }).click()
  await expect(wordCard(page, noteWord)).toBeVisible()
})

Then('the word card shows the note', async ({ page }) => {
  await expect(wordCard(page, noteWord).getByText('heard at the zoo')).toBeVisible()
})

When('they change the note on the word card', async ({ page }) => {
  await wordCard(page, noteWord).getByRole('button', { name: 'Edit note', exact: true }).click()
  const form = page.getByRole('form', { name: 'Edit note' })
  await form.getByLabel(/Where did you meet this word/i).fill('read in a book')
  await form.getByRole('button', { name: 'Save', exact: true }).click()
})

Then('the word card shows the changed note', async ({ page }) => {
  await expect(wordCard(page, noteWord).getByText('read in a book')).toBeVisible()
})

When('they clear the note on the word card', async ({ page }) => {
  await wordCard(page, noteWord).getByRole('button', { name: 'Edit note', exact: true }).click()
  await page.getByRole('form', { name: 'Edit note' }).getByRole('button', { name: 'Clear', exact: true }).click()
})

Then('the word card shows no note', async ({ page }) => {
  await expect(wordCard(page, noteWord).getByText('read in a book')).toHaveCount(0)
  await expect(wordCard(page, noteWord).getByRole('button', { name: 'Add note', exact: true })).toBeVisible()
})

When('they open the My Vocabulary page', async ({ page }) => {
  await page.getByRole('link', { name: /My Vocabulary/i }).click()
  await expect(page).toHaveURL(/\/vocabulary$/)
})

Then('the add form says only the learner can see the note', async ({ page }) => {
  await expect(page.getByText('Only you can see this note.')).toBeVisible()
})
