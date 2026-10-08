import { expect, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Fresh accounts per scenario: the session depends on the exact word set. Same admin convention as
// steps/vocabulary-sharing.steps.ts.
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@wordbuddy.com'
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123'
const learnerPassword = 'ChangeMe123!'
const STATE_TIMEOUT_MS = 30_000

interface SenseReview {
  senseId: string
  word: string
  definition: string
  audioUrl: string | null
  imageUrl: string | null
}

interface SessionBody {
  newItems: { senseId: string; status: string; dueAtUtc: string }[]
}

let definitionsByWord = new Map<string, string>()
let hiddenWord = ''
let hiddenSenseId = ''
let seenExercises = new Set<string>()
let sensesResponseIds: string[] = []

async function register(request: APIRequestContext, ageGroup: string): Promise<{ email: string; token: string }> {
  const email = `review-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email, password: learnerPassword, displayName: 'E2E Review Learner', ageGroup },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  const { token } = (await response.json()) as { token: string }
  if (ageGroup === 'Child') {
    await linkSupporter(request, token)
  }
  return { email, token }
}

/** WB-24: a child needs an active supporter before learning. Links a fresh adult and waits for the projections. */
async function linkSupporter(request: APIRequestContext, childToken: string): Promise<void> {
  const supporterEmail = `supporter-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const register = await request.post('/api/auth/register', {
    data: { email: supporterEmail, password: learnerPassword, displayName: 'E2E Supporter', ageGroup: 'Adult' },
  })
  expect(register.ok(), await register.text()).toBeTruthy()
  const { token: supporterToken } = (await register.json()) as { token: string }
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
  // Content and Progress read a projection fed by RabbitMQ. Empty review body: 403 while gated, 400 once allowed.
  await expect(async () => {
    const words = await request.get('/api/lessons', { headers: { Authorization: `Bearer ${childToken}` } })
    expect(words.status()).toBe(200)
    const review = await request.post('/api/progress/vocabulary/reviews', {
      headers: { Authorization: `Bearer ${childToken}` },
      data: {},
    })
    expect(review.status()).toBe(400)
  }).toPass({ timeout: STATE_TIMEOUT_MS })
}

async function addWord(request: APIRequestContext, token: string, word: string, definition: string): Promise<string> {
  const response = await request.post('/api/vocabulary', {
    headers: { Authorization: `Bearer ${token}` },
    data: { word, definition, example: null },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  return (await response.json()) as string
}

/** Word states reach Progress through RabbitMQ; wait until all are there. */
async function waitForStates(request: APIRequestContext, token: string, count: number): Promise<{ senseId: string }[]> {
  let states: { senseId: string }[] = []
  await expect(async () => {
    const response = await request.get('/api/progress/vocabulary/words', { headers: { Authorization: `Bearer ${token}` } })
    expect(response.ok()).toBeTruthy()
    states = (await response.json()) as { senseId: string }[]
    expect(states).toHaveLength(count)
  }).toPass({ timeout: STATE_TIMEOUT_MS })
  return states
}

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(learnerPassword)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
}

/** The exercise waiting for an answer: its "Show a hint" button is still enabled. */
function freshExercise(page: Page): Locator {
  return page
    .locator('[data-testid^="exercise-"]')
    .filter({ has: page.getByRole('button', { name: 'Show a hint', disabled: false }) })
}

function svgImage(label: string): string {
  return `data:image/svg+xml,${encodeURIComponent(`<svg xmlns="http://www.w3.org/2000/svg" width="80" height="80"><text y="40">${label}</text></svg>`)}`
}

Given('a new {string} learner with {int} words is logged in', async ({ page, request }, ageGroup: string, count: number) => {
  const { email, token } = await register(request, ageGroup)
  definitionsByWord = new Map()
  const stamp = Date.now()
  for (let i = 0; i < count; i += 1) {
    const word = `rv${i}x${stamp}`
    const definition = `review definition number ${i} of ${stamp}`
    await addWord(request, token, word, definition)
    definitionsByWord.set(word, definition)
  }
  await waitForStates(request, token, count)
  await logIn(page, email)
})

// No upload endpoint for sense images yet (plan, open questions), and dev words have no audio.
// The real Content reply is fetched and only the media URLs are added, so the page can reach
// all 3 exercise types: 3 words with picture + audio, 1 with audio only, the rest with neither.
Given('the review words have pictures and audio where available', async ({ page }) => {
  await page.route('**/api/vocabulary/senses**', async (route) => {
    const response = await route.fetch()
    const senses = (await response.json()) as SenseReview[]
    const withMedia: SenseReview[] = senses.map((sense, index) => ({
      ...sense,
      imageUrl: index < 3 ? svgImage(String(index)) : null,
      audioUrl: index < 4 ? `/e2e-audio/${sense.senseId}.mp3` : null,
    }))
    await route.fulfill({ response, json: withMedia })
  })
})

When('they open the review page', async ({ page }) => {
  await page.goto('/vocabulary/review')
  await expect(page.getByRole('heading', { name: 'Review' })).toBeVisible()
})

When('they answer every exercise correctly', async ({ page }) => {
  seenExercises = new Set()
  const summary = page.getByTestId('session-summary')
  const selfRating = page.getByRole('button', { name: /I Know This|I don't know|^Again$|^Hard$|^Good$|^Easy$/i })

  for (let step = 0; step < 40; step += 1) {
    await expect(freshExercise(page).or(summary)).toBeVisible()
    if (await summary.isVisible()) {
      return
    }
    await expect(selfRating).toHaveCount(0)

    const testId = (await freshExercise(page).getAttribute('data-testid')) ?? ''
    seenExercises.add(testId)
    const exercise = page.getByTestId(testId)

    if (testId !== 'exercise-typing') {
      await exercise.getByRole('button', { name: 'Show a hint' }).click()
    }

    let answer = ''
    for (const [word, definition] of definitionsByWord) {
      if (await exercise.getByText(definition, { exact: true }).isVisible()) {
        answer = word
      }
    }
    expect(answer, 'the shown definition belongs to one of the learner words').not.toBe('')

    if (testId === 'exercise-typing') {
      await exercise.getByLabel('Your answer').fill(answer)
      await exercise.getByRole('button', { name: 'Check' }).click()
    } else {
      await exercise.getByRole('button', { name: answer, exact: true }).click()
    }

    const feedback = page.getByTestId('answer-feedback')
    await expect(feedback.getByText('Correct!')).toBeVisible()
    await feedback.getByRole('button', { name: 'Next' }).click()
    await expect(feedback).toHaveCount(0)
  }

  throw new Error('The review session did not end within 40 exercises.')
})

Then('they saw picture, listening and typing exercises', async () => {
  expect([...seenExercises].sort()).toEqual(['exercise-listening-choice', 'exercise-picture-choice', 'exercise-typing'])
})

Then('the session summary shows their answers', async ({ page }) => {
  const summary = page.getByTestId('session-summary')
  await expect(summary.getByText('Review complete!')).toBeVisible()
  await expect(summary).toContainText(/You answered (\d+) and got \1 right\./)
})

Then('no self-rating was shown', async ({ page }) => {
  await expect(page.getByRole('button', { name: /I Know This|I don't know|^Again$|^Hard$|^Good$|^Easy$/i })).toHaveCount(0)
})

Given('a word shared by an adult and approved as not visible to children', async ({ request }) => {
  const { token } = await register(request, 'Adult')
  hiddenWord = `rvhidden${Date.now()}`
  const wordId = await addWord(request, token, hiddenWord, 'A shared word hidden from children.')
  const [state] = await waitForStates(request, token, 1)
  hiddenSenseId = state.senseId

  const share = await request.post(`/api/vocabulary/${wordId}/share`, { headers: { Authorization: `Bearer ${token}` } })
  expect(share.status()).toBe(204)

  const login = await request.post('/api/auth/login', { data: { email: adminEmail, password: adminPassword } })
  expect(login.ok()).toBeTruthy()
  const { token: adminToken } = (await login.json()) as { token: string }
  const approve = await request.post(`/api/vocabulary/moderation/${wordId}`, {
    headers: { Authorization: `Bearer ${adminToken}` },
    data: { approve: true, visibleToChildren: false },
  })
  expect(approve.status()).toBe(204)
})

// The child cannot add a hidden word to their own list, so the hidden sense id is appended to the
// real session reply. Content must drop it: the server filter is what is under test.
Given('the review session also lists the hidden shared word', async ({ page }) => {
  await page.route('**/api/progress/vocabulary/session', async (route) => {
    const response = await route.fetch()
    const session = (await response.json()) as SessionBody
    session.newItems.push({ senseId: hiddenSenseId, status: 'New', dueAtUtc: new Date().toISOString() })
    await route.fulfill({ response, json: session })
  })
})

When('they open the review page with a controlled clock', async ({ page }) => {
  await page.clock.install()
  const sensesReply = page.waitForResponse((r) => r.url().includes('/api/vocabulary/senses'))
  await page.goto('/vocabulary/review')
  const response = await sensesReply
  expect(response.ok()).toBeTruthy()
  sensesResponseIds = ((await response.json()) as SenseReview[]).map((s) => s.senseId)
  await expect(page.locator('[data-testid^="exercise-"]')).toBeVisible()
})

Then('the hidden shared word is never shown', async ({ page }) => {
  expect(sensesResponseIds).toHaveLength(definitionsByWord.size)
  expect(sensesResponseIds).not.toContain(hiddenSenseId)
  await expect(page.getByText(hiddenWord)).toHaveCount(0)
})

When('10 minutes pass', async ({ page }) => {
  await page.clock.fastForward('10:00')
})

Then('a "nearly done" notice is shown', async ({ page }) => {
  await expect(page.getByTestId('time-cap-notice')).toBeVisible()
  await expect(page.getByTestId('session-summary')).toHaveCount(0)
})

When('5 more minutes pass', async ({ page }) => {
  await page.clock.fastForward('05:00')
})

Then("the session stops with a time's up summary", async ({ page }) => {
  const summary = page.getByTestId('session-summary')
  await expect(summary.getByText("Time's up — great work today!")).toBeVisible()
  await expect(summary).toContainText('The rest will wait for next time.')
  await expect(page.getByText(hiddenWord)).toHaveCount(0)
})
