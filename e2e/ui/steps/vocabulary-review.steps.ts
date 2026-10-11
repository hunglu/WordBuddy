import { expect, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Fresh accounts per scenario: the session depends on the exact word set. Same admin convention as
// steps/vocabulary-sharing.steps.ts.
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@wordbuddy.com'
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123'
const learnerPassword = 'ChangeMe123!'
const STATE_TIMEOUT_MS = 30_000

interface SessionBody {
  newItems: { senseId: string; status: string; dueAtUtc: string }[]
}

interface ReviewRequestBody {
  exerciseId: string
  answer: { optionKey?: string; text?: string }
}

let definitionsByWord = new Map<string, string>()
let hiddenWord = ''
let hiddenSenseId = ''
let seenExercises = new Set<string>()
let firstWrongWord = ''
let exerciseStatusBySenseId = new Map<string, number>()

// WB-28: the server builds the exercises and checks the answers. A real stack has no word images and
// only autofilled words have audio, so every exercise there is Typing. The "stubbed server" scenario
// replaces the exercise and review replies to check that the UI renders all 3 types from the server
// exercise, sends the raw answer, and shows the server's verdict.
const STUB_WORD = 'stubword'
const STUB_RIGHT_KEY = 'key-right'
const STUB_TYPES = ['PictureChoice', 'ListeningChoice', 'Typing'] as const

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

Given('the exercise and review replies come from a stubbed server', async ({ page }) => {
  let created = 0
  await page.route('**/api/progress/vocabulary/exercises', async (route) => {
    const exerciseType = STUB_TYPES[created % STUB_TYPES.length]
    created += 1
    await route.fulfill({ json: stubExercise(exerciseType, created) })
  })
  await page.route('**/api/progress/vocabulary/reviews', async (route) => {
    const body = route.request().postDataJSON() as ReviewRequestBody
    const isCorrect = body.answer.optionKey === STUB_RIGHT_KEY || body.answer.text?.trim().toLowerCase() === STUB_WORD
    await route.fulfill({
      json: {
        status: 'Learning',
        dueAtUtc: new Date().toISOString(),
        rating: isCorrect ? 'Good' : 'Again',
        isCorrect,
        correctAnswer: STUB_WORD,
      },
    })
  })
})

function stubExercise(exerciseType: (typeof STUB_TYPES)[number], index: number) {
  const isChoice = exerciseType !== 'Typing'
  return {
    exerciseId: `00000000-0000-4000-8000-${String(index).padStart(12, '0')}`,
    exerciseType,
    skill: exerciseType === 'PictureChoice' ? 'Meaning' : exerciseType === 'ListeningChoice' ? 'Listening' : 'Spelling',
    prompt: {
      definition: 'a stubbed definition',
      imageUrl: exerciseType === 'PictureChoice' ? svgImage('stub') : null,
      audioUrl: exerciseType === 'ListeningChoice' ? '/e2e-audio/stub.mp3' : null,
      personalContext: null,
      hintFirstLetter: exerciseType === 'Typing' ? STUB_WORD.charAt(0) : null,
    },
    options: isChoice
      ? [
          { key: STUB_RIGHT_KEY, text: STUB_WORD },
          { key: 'key-2', text: 'otherone' },
          { key: 'key-3', text: 'othertwo' },
          { key: 'key-4', text: 'otherthree' },
        ]
      : [],
  }
}

When('they open the review page', async ({ page }) => {
  await page.goto('/vocabulary/review')
  await expect(page.getByRole('heading', { name: 'Review' })).toBeVisible()
})

// Real server: the words have no picture, so every exercise is Typing. The answer comes from the
// definition the learner sees; the browser never knows the right word before the server replies.
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

When('they answer every stubbed exercise correctly', async ({ page }) => {
  seenExercises = new Set()
  const summary = page.getByTestId('session-summary')

  for (let step = 0; step < 40; step += 1) {
    await expect(freshExercise(page).or(summary)).toBeVisible()
    if (await summary.isVisible()) {
      return
    }

    const testId = (await freshExercise(page).getAttribute('data-testid')) ?? ''
    seenExercises.add(testId)
    const exercise = page.getByTestId(testId)

    if (testId === 'exercise-typing') {
      // Other case and spaces: the server trims and ignores case, the browser does not judge.
      await exercise.getByLabel('Your answer').fill(`  ${STUB_WORD.toUpperCase()} `)
      await exercise.getByRole('button', { name: 'Check' }).click()
    } else {
      await exercise.getByRole('button', { name: STUB_WORD, exact: true }).click()
    }

    const feedback = page.getByTestId('answer-feedback')
    await expect(feedback.getByText('Correct!')).toBeVisible()
    await feedback.getByRole('button', { name: 'Next' }).click()
    await expect(feedback).toHaveCount(0)
  }

  throw new Error('The stubbed review session did not end within 40 exercises.')
})

When('they answer the first exercise wrongly', async ({ page }) => {
  const exercise = page.getByTestId('exercise-typing')
  await expect(exercise).toBeVisible()
  for (const [word, definition] of definitionsByWord) {
    if (await exercise.getByText(definition, { exact: true }).isVisible()) {
      firstWrongWord = word
    }
  }
  expect(firstWrongWord, 'the shown definition belongs to one of the learner words').not.toBe('')

  await exercise.getByLabel('Your answer').fill('definitely-not-the-word')
  await exercise.getByRole('button', { name: 'Check' }).click()
})

Then('the server says the answer was wrong and shows the right word', async ({ page }) => {
  const feedback = page.getByTestId('answer-feedback')
  await expect(feedback.getByText(`The answer was "${firstWrongWord}".`)).toBeVisible()
  await feedback.getByRole('button', { name: 'Next' }).click()
})

Then('the same number of words is still left and none is counted correct', async ({ page }) => {
  // The wrong word goes to the end of the queue, so the queue did not shrink.
  await expect(page.getByText(`${definitionsByWord.size} left · 0 correct`)).toBeVisible()
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

// The child cannot add a hidden word to their own list, so the hidden sense id is put first in the
// real session reply. The server has no exercise for it (404), so the page must skip it and never show it.
Given('the review session also lists the hidden shared word', async ({ page }) => {
  exerciseStatusBySenseId = new Map()
  await page.route('**/api/progress/vocabulary/session', async (route) => {
    const response = await route.fetch()
    const session = (await response.json()) as SessionBody
    session.newItems.unshift({ senseId: hiddenSenseId, status: 'New', dueAtUtc: new Date().toISOString() })
    await route.fulfill({ response, json: session })
  })
  page.on('response', (response) => {
    if (response.url().includes('/api/progress/vocabulary/exercises') && response.request().method() === 'POST') {
      const body = response.request().postDataJSON() as { senseId: string }
      exerciseStatusBySenseId.set(body.senseId, response.status())
    }
  })
})

When('they open the review page with a controlled clock', async ({ page }) => {
  await page.clock.install()
  await page.goto('/vocabulary/review')
  await expect(page.locator('[data-testid^="exercise-"]')).toBeVisible()
})

Then('the hidden shared word is never shown', async ({ page }) => {
  // The first queue item is the hidden word. The server refuses it; the page moved on to a real word.
  expect(exerciseStatusBySenseId.get(hiddenSenseId)).toBe(404)
  await expect(page.getByText(hiddenWord)).toHaveCount(0)
  await expect(page.getByText(`${definitionsByWord.size} left`)).toBeVisible()
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
