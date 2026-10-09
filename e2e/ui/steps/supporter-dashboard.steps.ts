import { expect, type APIRequestContext, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

const password = 'ChangeMe123!'

interface Account {
  email: string
  token: string
}

let supporter: Account | null = null

const auth = (token: string) => ({ Authorization: `Bearer ${token}` })

async function register(request: APIRequestContext, ageGroup: string): Promise<Account> {
  const email = `dashboard-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email, password, displayName: 'E2E Dashboard User', ageGroup },
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

Given('a new {string} dashboard learner is logged in', async ({ page, request }, ageGroup: string) => {
  const account = await register(request, ageGroup)
  await logIn(page, account.email)
})

When('they open their dashboard', async ({ page }) => {
  await page.goto('/dashboard/me')
})

Then('the dashboard shows the Retention and Streak cards', async ({ page }) => {
  await expect(page.getByRole('heading', { name: 'My dashboard', level: 1 })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Retention' })).toBeVisible()
  await expect(page.getByText(/streak/i).first()).toBeVisible()
})

Given('a dashboard learner has an active supporter', async ({ request }) => {
  const learner = await register(request, 'Adult')
  supporter = await register(request, 'Adult')
  const invitation = await request.post('/api/auth/support-links/invitations', {
    headers: auth(learner.token),
    data: { inviteAs: 'Learner', relationship: null },
  })
  expect(invitation.status(), await invitation.text()).toBe(201)
  const { code } = (await invitation.json()) as { code: string }
  const accept = await request.post('/api/auth/support-links/accept', {
    headers: auth(supporter.token),
    data: { code, token: null },
  })
  expect(accept.ok(), await accept.text()).toBeTruthy()
})

Given('the supporter is logged in', async ({ page }) => {
  await logIn(page, supporter!.email)
})

When('the supporter opens the learner dashboard from the Supporters page', async ({ page }) => {
  await page.goto('/support')
  await page.getByRole('link', { name: 'View dashboard' }).click()
})

Then('the learner dashboard is shown', async ({ page }) => {
  await expect(page).toHaveURL(/\/dashboard\/learners\/[0-9a-f-]+$/)
  await expect(page.getByRole('heading', { name: "Learner's dashboard", level: 1 })).toBeVisible()
  // The supporter link reaches Progress through RabbitMQ; a reload can be needed until it lands.
  await expect(async () => {
    await page.reload()
    await expect(page.getByRole('heading', { name: 'Retention' })).toBeVisible({ timeout: 2_000 })
  }).toPass({ timeout: 30_000 })
})

Given('the dashboard requests fail', async ({ page }) => {
  await page.route('**/api/progress/dashboard/**', (route) => route.fulfill({ status: 502, body: 'Bad Gateway' }))
})

Then('an error message is shown instead of the dashboard', async ({ page }) => {
  await expect(page.getByText("Couldn't load the dashboard right now. Please try again later.")).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Retention' })).toHaveCount(0)
})
