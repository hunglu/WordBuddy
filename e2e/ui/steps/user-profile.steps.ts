import { expect, type APIRequestContext, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

const password = 'ChangeMe123!'
const newPassword = 'NewSecret456!'
const guardianName = 'E2E Guardian'

let email = ''
let expectedDate = ''

const auth = (token: string) => ({ Authorization: `Bearer ${token}` })

async function register(request: APIRequestContext, ageGroup: string, displayName: string) {
  const address = `profile-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email: address, password, displayName, ageGroup },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  const { token } = (await response.json()) as { token: string }
  return { email: address, token }
}

async function logIn(page: Page, address: string, pwd: string): Promise<void> {
  await page.goto('/login')
  await page.getByLabel('Email').fill(address)
  await page.getByLabel('Password').fill(pwd)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
}

async function openProfile(page: Page): Promise<void> {
  await page.goto('/profile')
  await expect(page.getByRole('heading', { name: 'My profile' })).toBeVisible()
  await expect(page.getByLabel('Date of birth')).toBeVisible()
}

function isoYearsAgo(years: number): string {
  const date = new Date()
  date.setFullYear(date.getFullYear() - years)
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

async function saveDateOfBirth(page: Page, value: string): Promise<void> {
  await page.getByLabel('Date of birth').fill(value)
  const saved = page.waitForResponse((r) => r.url().includes('/profile/date-of-birth'))
  await page.getByRole('button', { name: 'Save date of birth' }).click()
  await saved
}

async function submitPasswordChange(page: Page, current: string, next: string): Promise<void> {
  await page.getByLabel('Current password').fill(current)
  await page.getByLabel('New password', { exact: true }).fill(next)
  await page.getByLabel('Confirm new password').fill(next)
  await page.getByRole('button', { name: 'Change password' }).click()
}

Given('a new {string} user is logged in on the profile page', async ({ page, request }, ageGroup: string) => {
  const account = await register(request, ageGroup, 'E2E Profile User')
  email = account.email
  await logIn(page, account.email, password)
  await openProfile(page)
})

Given(
  'a new {string} user with a linked guardian is logged in on the profile page',
  async ({ page, request }, ageGroup: string) => {
    const child = await register(request, ageGroup, 'E2E Profile Child')
    const guardian = await register(request, 'Adult', guardianName)
    const invitation = await request.post('/api/auth/support-links/invitations', {
      headers: auth(child.token),
      data: { inviteAs: 'Learner', relationship: null },
    })
    expect(invitation.status(), await invitation.text()).toBe(201)
    const { code } = (await invitation.json()) as { code: string }
    const accept = await request.post('/api/auth/support-links/accept', {
      headers: auth(guardian.token),
      data: { code, token: null },
    })
    expect(accept.status(), await accept.text()).toBe(200)
    email = child.email
    await logIn(page, child.email, password)
    await openProfile(page)
  },
)

Then('the child hint about asking a guardian is shown', async ({ page }) => {
  await expect(page.getByText('Ask your guardian if you are not sure.')).toBeVisible()
})

When('they save the date of birth {string}', async ({ page }, value: string) => {
  await saveDateOfBirth(page, value)
})

When('they save the date of birth of a 10 year old', async ({ page }) => {
  expectedDate = isoYearsAgo(10)
  await saveDateOfBirth(page, expectedDate)
})

When('they save a date of birth in the future', async ({ page }) => {
  // Zod blocks the submit before any request is sent.
  await page.getByLabel('Date of birth').fill(isoYearsAgo(-1))
  await page.getByRole('button', { name: 'Save date of birth' }).click()
})

When('they reload the profile page', async ({ page }) => {
  await page.reload()
  await expect(page.getByLabel('Date of birth')).toBeVisible()
})

Then('the date of birth field shows {string}', async ({ page }, value: string) => {
  await expect(page.getByLabel('Date of birth')).toHaveValue(value)
})

Then('the date of birth field shows that date', async ({ page }) => {
  await expect(page.getByLabel('Date of birth')).toHaveValue(expectedDate)
})

Then('the date of birth field is empty', async ({ page }) => {
  await expect(page.getByLabel('Date of birth')).toHaveValue('')
})

Then('the profile message {string} is shown', async ({ page }, text: string) => {
  await expect(page.getByText(text)).toBeVisible()
})

When('they change the password to {string} using the current password', async ({ page }, next: string) => {
  expect(next).toBe(newPassword)
  await submitPasswordChange(page, password, next)
})

When('they change the password to {string} using the wrong current password', async ({ page }, next: string) => {
  await submitPasswordChange(page, 'WrongPassword999!', next)
})

Then('a password error is shown and no success message appears', async ({ page }) => {
  await expect(page.locator('p.text-wb-danger')).toBeVisible()
  await expect(page.getByText('Password changed.')).toHaveCount(0)
})

When('they log out and log in with the new password', async ({ page }) => {
  await page.evaluate(() => localStorage.clear())
  await logIn(page, email, newPassword)
})

Then('they are logged in', async ({ page }) => {
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
})

Then('the My supporters card lists the guardian marked {string}', async ({ page }, badge: string) => {
  const card = page.getByRole('region', { name: 'My supporters' })
  await expect(card).toBeVisible()
  await expect(card.getByText(guardianName)).toBeVisible()
  await expect(card.getByText(badge, { exact: true })).toBeVisible()
})

Then('the My supporters card is not shown', async ({ page }) => {
  await expect(page.getByRole('heading', { name: 'My supporters' })).toHaveCount(0)
})
