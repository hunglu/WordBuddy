import { expect, type APIRequestContext } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Same admin convention as steps/navigation.steps.ts. The step "the admin is logged in" lives there.
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@wordbuddy.com'
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123'
const learnerPassword = 'ChangeMe123!'
const STATE_TIMEOUT_MS = 30_000

let lessonTitle = ''
let childEmail = ''
let safeWord = ''
let hiddenWord = ''

async function adminToken(request: APIRequestContext): Promise<string> {
  const login = await request.post('/api/auth/login', { data: { email: adminEmail, password: adminPassword } })
  expect(login.ok(), await login.text()).toBeTruthy()
  return ((await login.json()) as { token: string }).token
}

async function register(request: APIRequestContext, ageGroup: 'Child' | 'Adult'): Promise<{ email: string; token: string }> {
  const email = `lesson-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email, password: learnerPassword, displayName: 'E2E Lesson User', ageGroup },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  return { email, token: ((await response.json()) as { token: string }).token }
}

/** WB-24: a child needs an active supporter before learning. Links a fresh adult and waits for the projection. */
async function linkSupporter(request: APIRequestContext, childToken: string): Promise<void> {
  const { token: supporterToken } = await register(request, 'Adult')
  const invitation = await request.post('/api/auth/support-links/invitations', {
    headers: { Authorization: `Bearer ${childToken}` },
    data: { inviteAs: 'Learner', relationship: null },
  })
  expect(invitation.status(), await invitation.text()).toBe(201)
  const { code } = (await invitation.json()) as { code: string }
  const accept = await request.post('/api/auth/support-links/accept', {
    headers: { Authorization: `Bearer ${supporterToken}` },
    data: { code, token: null },
  })
  expect(accept.ok(), await accept.text()).toBeTruthy()
  await expect(async () => {
    const lessons = await request.get('/api/lessons', { headers: { Authorization: `Bearer ${childToken}` } })
    expect(lessons.status()).toBe(200)
  }).toPass({ timeout: STATE_TIMEOUT_MS })
}

/** An approved shared-pool word that children may not see. */
async function createHiddenSharedWord(request: APIRequestContext, admin: string, word: string): Promise<string> {
  const { token } = await register(request, 'Adult')
  const add = await request.post('/api/vocabulary', {
    headers: { Authorization: `Bearer ${token}` },
    data: { word, definition: 'A shared word hidden from children.', example: null },
  })
  expect(add.ok(), await add.text()).toBeTruthy()
  const wordId = (await add.json()) as string
  const share = await request.post(`/api/vocabulary/${wordId}/share`, { headers: { Authorization: `Bearer ${token}` } })
  expect(share.status()).toBe(204)
  const approve = await request.post(`/api/vocabulary/moderation/${wordId}`, {
    headers: { Authorization: `Bearer ${admin}` },
    data: { approve: true, visibleToChildren: false },
  })
  expect(approve.status()).toBe(204)
  return wordId
}

async function systemSenseId(request: APIRequestContext, admin: string, word: string): Promise<string> {
  const response = await request.get(`/api/lessons/admin/senses?search=${word}`, { headers: { Authorization: `Bearer ${admin}` } })
  expect(response.ok(), await response.text()).toBeTruthy()
  const found = (await response.json()) as { senseId: string; word: string; source: string }[]
  const match = found.find((s) => s.word === word && s.source === 'System')
  expect(match, `system word "${word}" should exist`).toBeDefined()
  return match!.senseId
}

// ---- Scenario 1: authoring in the UI

When('they create a lesson titled with a unique name in Lesson authoring', async ({ page }) => {
  lessonTitle = `E2E lesson ${Date.now()}`
  await page.locator('aside').getByRole('link', { name: 'Lesson authoring' }).click()
  await expect(page.getByRole('heading', { name: 'Lesson authoring' })).toBeVisible()
  await page.getByRole('link', { name: 'New lesson' }).click()

  await page.getByLabel('Title').fill(lessonTitle)
  await page.getByLabel('Description').fill('Created by the lesson authoring E2E test.')
  await page.getByRole('button', { name: 'Create draft' }).click()

  // After saving, the editor shows the draft with its word panel.
  await expect(page.getByRole('heading', { name: lessonTitle })).toBeVisible()
  await expect(page.getByText('Draft', { exact: true })).toBeVisible()
})

When('they add the word {string} to the lesson', async ({ page }, word: string) => {
  await page.getByLabel('Add a word').fill(word)
  const row = page.locator('li').filter({ hasText: word }).filter({ has: page.getByRole('button', { name: 'Add', exact: true }) }).first()
  await row.getByRole('button', { name: 'Add', exact: true }).click()
  await expect(page.getByRole('button', { name: `Remove ${word}` })).toBeVisible()
})

When('they publish the lesson', async ({ page }) => {
  await page.getByRole('button', { name: 'Publish', exact: true }).click()
})

Then('the lesson shows the {string} status', async ({ page }, status: string) => {
  await expect(page.getByText(status, { exact: true })).toBeVisible()
})

Then('the lesson list shows the lesson as published with 1 word', async ({ page }) => {
  await page.getByRole('link', { name: '← All lessons' }).click()
  const row = page.locator('tr').filter({ hasText: lessonTitle })
  await expect(row.getByText('Published', { exact: true })).toBeVisible()
  await expect(row.getByRole('cell', { name: '1', exact: true })).toBeVisible()
})

// ---- Scenario 2: the child filter

let lessonId = ''

Given('a published lesson with a child-safe word and a word hidden from children', async ({ request }) => {
  const admin = await adminToken(request)
  const headers = { Authorization: `Bearer ${admin}` }
  safeWord = 'Dog'
  hiddenWord = `lessonhidden${Date.now()}`

  // The id returned when a word is added is its sense id.
  const hiddenSense = await createHiddenSharedWord(request, admin, hiddenWord)
  const safeSense = await systemSenseId(request, admin, safeWord)

  const create = await request.post('/api/lessons/admin', {
    headers,
    data: {
      title: `E2E child filter ${Date.now()}`,
      description: 'Child filter E2E.',
      type: 'Vocabulary',
      level: 'Beginner',
      targetAgeGroup: 'Child',
    },
  })
  expect(create.status(), await create.text()).toBe(201)
  lessonId = (await create.json()) as string

  for (const senseId of [safeSense, hiddenSense]) {
    const attach = await request.post(`/api/lessons/admin/${lessonId}/senses`, { headers, data: { senseId } })
    expect(attach.status(), await attach.text()).toBe(204)
  }
  const publish = await request.post(`/api/lessons/admin/${lessonId}/publish`, { headers })
  expect(publish.status(), await publish.text()).toBe(204)
})

When('a child opens the lesson', async ({ page, request }) => {
  const { email, token } = await register(request, 'Child')
  childEmail = email
  await linkSupporter(request, token)

  await page.goto('/login')
  await page.getByLabel('Email').fill(childEmail)
  await page.getByLabel('Password').fill(learnerPassword)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)

  await page.goto(`/lessons/${lessonId}`)
})

Then('the child sees the child-safe word', async ({ page }) => {
  await expect(page.getByText(safeWord, { exact: true })).toBeVisible()
})

Then('the child does not see the hidden word', async ({ page }) => {
  await expect(page.getByText(safeWord, { exact: true })).toBeVisible()
  await expect(page.getByText(hiddenWord)).toHaveCount(0)
})
