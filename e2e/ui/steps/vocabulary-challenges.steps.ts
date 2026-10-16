import { expect, type APIRequestContext, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

const password = 'ChangeMe123!'

interface Account {
  email: string
  token: string
}

async function register(request: APIRequestContext, ageGroup: string): Promise<Account> {
  const email = `challenge-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email, password, displayName: 'E2E Challenge User', ageGroup },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  const { token } = (await response.json()) as { token: string }
  return { email, token }
}

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
}

// A child needs an active supporter before any learning page opens (WB-24). The supporter has no Guardian role.
async function linkSupporter(request: APIRequestContext, childToken: string): Promise<void> {
  const supporter = await register(request, 'Adult')
  const invitation = await request.post('/api/auth/support-links/invitations', {
    headers: { Authorization: `Bearer ${childToken}` },
    data: { inviteAs: 'Learner', relationship: null },
  })
  expect(invitation.status(), await invitation.text()).toBe(201)
  const { code } = (await invitation.json()) as { code: string }
  const accept = await request.post('/api/auth/support-links/accept', {
    headers: { Authorization: `Bearer ${supporter.token}` },
    data: { code, token: null },
  })
  expect(accept.ok(), await accept.text()).toBeTruthy()
}

const card = (page: Page, name: string) => page.getByRole('region', { name, exact: true })

Given('a new adult learner is logged in for challenges', async ({ page, request }) => {
  const adult = await register(request, 'Adult')
  await logIn(page, adult.email)
})

Given('a new child learner with a supporter is logged in for challenges', async ({ page, request }) => {
  const child = await register(request, 'Child')
  await linkSupporter(request, child.token)
  await logIn(page, child.email)
})

When('they open the Challenges page', async ({ page }) => {
  await page.getByRole('link', { name: 'Challenges' }).click()
  await expect(page).toHaveURL(/\/challenges$/)
  await expect(page.getByRole('heading', { name: 'Challenges', level: 1 })).toBeVisible()
})

When('they join the {string} challenge', async ({ page }, name: string) => {
  // The scheduler makes the challenges on its own clock: reload until the card is there.
  await expect(async () => {
    await page.reload()
    await expect(card(page, name)).toBeVisible({ timeout: 3_000 })
  }).toPass({ timeout: 90_000 })
  await card(page, name).getByRole('button', { name: 'Join' }).click()
})

Then('the {string} challenge shows as joined', async ({ page }, name: string) => {
  await expect(card(page, name).getByText('Joined', { exact: true })).toBeVisible()
  await expect(card(page, name).getByRole('link', { name: 'View board' })).toBeVisible()
})

When('they open the board of the {string} challenge', async ({ page }, name: string) => {
  await card(page, name).getByRole('link', { name: 'View board' }).click()
  await expect(page).toHaveURL(/\/challenges\/[0-9a-f-]+$/)
  await expect(page.getByRole('heading', { name, level: 1 })).toBeVisible()
})

Then('the board lists them as {string}', async ({ page }, who: string) => {
  // The board is paged (WB-55). A fresh learner has no points, so on a database full of e2e
  // learners they are on a later page: follow "Next" until the row shows.
  const row = page.getByRole('row', { name: new RegExp(`\\(${who}\\)`) })
  const next = page.getByRole('button', { name: /Next/ })
  for (let turn = 0; turn < 50 && !(await row.isVisible()); turn++) {
    await expect(next).toBeVisible()
    if (await next.isDisabled()) break
    await next.click()
  }
  await expect(row).toBeVisible()
})

Then('the message {string} is shown', async ({ page }, text: string) => {
  await expect(page.getByText(text, { exact: true })).toBeVisible()
})

Then('no challenge is listed', async ({ page }) => {
  await expect(page.getByRole('region')).toHaveCount(0)
})
