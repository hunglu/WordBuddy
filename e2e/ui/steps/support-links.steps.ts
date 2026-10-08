import { expect, type APIRequestContext, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Fresh accounts per scenario. Same admin convention as steps/vocabulary-review.steps.ts.
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@wordbuddy.com'
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123'
const password = 'ChangeMe123!'

interface Account {
  email: string
  token: string
}

let child: Account | null = null
let invitationToken = ''
let escalatedRequestId = ''
let escalatedLinkId = ''
let learnerToken = ''

const auth = (token: string) => ({ Authorization: `Bearer ${token}` })

async function register(request: APIRequestContext, ageGroup: string): Promise<Account> {
  const email = `support-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email, password, displayName: 'E2E Support User', ageGroup },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  const { token } = (await response.json()) as { token: string }
  return { email, token }
}

async function createInvitation(request: APIRequestContext, token: string): Promise<{ code: string; token: string }> {
  const response = await request.post('/api/auth/support-links/invitations', {
    headers: auth(token),
    data: { inviteAs: 'Learner', relationship: null },
  })
  expect(response.status(), await response.text()).toBe(201)
  return (await response.json()) as { code: string; token: string }
}

async function logIn(page: Page, email: string, pwd: string): Promise<void> {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(pwd)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
}

const gateHeading = (page: Page) => page.getByRole('heading', { name: 'Add a supporter' })

// Scenario 1 ----------------------------------------------------------------

Given('a new {string} learner is logged in', async ({ page, request }, ageGroup: string) => {
  const account = await register(request, ageGroup)
  await logIn(page, account.email, password)
})

When('they open the vocabulary page', async ({ page }) => {
  await page.goto('/vocabulary')
})

Then('the {string} screen is shown', async ({ page }, _name: string) => {
  await expect(gateHeading(page)).toBeVisible()
})

When('they follow the {string} link', async ({ page }, name: string) => {
  await page.getByRole('link', { name }).click()
})

Then('they are on the Supporters page', async ({ page }) => {
  await expect(page).toHaveURL(/\/support$/)
  await expect(page.getByRole('heading', { name: 'Supporters', level: 1 })).toBeVisible()
})

// Scenario 2 ----------------------------------------------------------------

Given('a new child has created a support invitation', async ({ request }) => {
  child = await register(request, 'Child')
  invitationToken = (await createInvitation(request, child.token)).token
})

Given('a new adult is logged in', async ({ page, request }) => {
  const adult = await register(request, 'Adult')
  await logIn(page, adult.email, password)
})

When('the adult opens the invitation link and accepts it', async ({ page }) => {
  await page.goto(`/support/accept/${encodeURIComponent(invitationToken)}`)
  await page.getByRole('button', { name: 'Accept invitation' }).click()
})

Then('the link is shown as active', async ({ page }) => {
  await expect(page.getByText('Accepted. The link is active.')).toBeVisible()
})

When('the child logs in and opens the vocabulary page', async ({ page }) => {
  await page.context().clearCookies()
  await page.evaluate(() => window.localStorage.clear())
  await logIn(page, child!.email, password)
  // The gate reads HasActiveSupporter from Identity; the learning APIs read the projection,
  // which follows through RabbitMQ (eventual consistency, review.md finding 1).
  await expect(async () => {
    await page.goto('/vocabulary')
    await expect(gateHeading(page)).toHaveCount(0, { timeout: 2_000 })
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible({ timeout: 2_000 })
  }).toPass({ timeout: 30_000 })
})

Then('the {string} screen is not shown', async ({ page }, _name: string) => {
  await expect(gateHeading(page)).toHaveCount(0)
})

// Scenario 3 ----------------------------------------------------------------

Given('an active support link with an escalated unlink request', async ({ request }) => {
  const learner = await register(request, 'Adult')
  const supporter = await register(request, 'Adult')
  learnerToken = learner.token
  const { code } = await createInvitation(request, learner.token)

  const accept = await request.post('/api/auth/support-links/accept', {
    headers: auth(supporter.token),
    data: { code, token: null },
  })
  expect(accept.ok(), await accept.text()).toBeTruthy()
  escalatedLinkId = ((await accept.json()) as { id: string }).id

  const unlink = await request.post(`/api/auth/support-links/${escalatedLinkId}/unlink-request`, {
    headers: auth(supporter.token),
  })
  expect(unlink.status(), await unlink.text()).toBe(204)

  const escalate = await request.post(`/api/auth/support-links/${escalatedLinkId}/unlink-request/escalate`, {
    headers: auth(supporter.token),
  })
  expect(escalate.status(), `escalate needs SupportLinks__UnlinkOverrideWaitDays=0: ${await escalate.text()}`).toBe(204)

  const login = await request.post('/api/auth/login', { data: { email: adminEmail, password: adminPassword } })
  expect(login.ok(), await login.text()).toBeTruthy()
  const { token: adminToken } = (await login.json()) as { token: string }
  const list = await request.get('/api/auth/admin/unlink-requests', { headers: auth(adminToken) })
  expect(list.ok(), await list.text()).toBeTruthy()
  const requests = (await list.json()) as { id: string; linkId: string }[]
  escalatedRequestId = requests.find((r) => r.linkId === escalatedLinkId)!.id
})

Given('the admin is logged in on the support links page', async ({ page }) => {
  await logIn(page, adminEmail, adminPassword)
  await page.goto('/admin/support-links')
  await expect(page.getByRole('heading', { name: 'Support links (admin)' })).toBeVisible()
})

When('the admin completes the unlink with a reason', async ({ page }) => {
  await page.locator(`#reason-${escalatedRequestId}`).fill('E2E: supporter no longer responds')
  const card = page.locator('div').filter({ has: page.locator(`#reason-${escalatedRequestId}`) }).last()
  await card.getByRole('button', { name: 'Complete unlink' }).click()
})

Then('the request is no longer listed', async ({ page }) => {
  await expect(page.locator(`#reason-${escalatedRequestId}`)).toHaveCount(0)
})

Then('the link is revoked', async ({ request }) => {
  const response = await request.get('/api/auth/support-links', { headers: auth(learnerToken) })
  expect(response.ok(), await response.text()).toBeTruthy()
  const body = (await response.json()) as { asLearner: { id: string; status: string }[] }
  const link = body.asLearner.find((l) => l.id === escalatedLinkId)
  expect(link === undefined || link.status === 'Revoked').toBeTruthy()
})
