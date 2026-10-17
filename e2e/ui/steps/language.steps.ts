import { expect, type APIRequestContext, type Browser, type BrowserContext, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

const password = 'ChangeMe123!'
const SWITCHER = { name: /Language|Ngôn ngữ/ }

let email = ''
let extraContext: BrowserContext | undefined
let activePage: Page | undefined
let marker = ''

async function register(request: APIRequestContext, ageGroup: string): Promise<string> {
  const address = `lang-ui-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@example.com`
  const response = await request.post('/api/auth/register', {
    data: { email: address, password, displayName: 'E2E Language User', ageGroup },
  })
  expect(response.ok(), await response.text()).toBeTruthy()
  return address
}

async function logIn(page: Page, address: string): Promise<void> {
  await page.goto('/login')
  await page.getByLabel('Email').fill(address)
  await page.getByLabel(/Password|Mật khẩu/).fill(password)
  await page.getByRole('button', { name: /log in|đăng nhập/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
}

async function freshPage(browser: Browser, baseURL: string | undefined, locale: string): Promise<Page> {
  await extraContext?.close()
  extraContext = await browser.newContext({ locale, baseURL })
  activePage = await extraContext.newPage()
  return activePage
}

Given('a browser with the language {string}', async ({ browser, baseURL }, locale: string) => {
  await freshPage(browser, baseURL, locale)
})

When('the learner opens the login page', async () => {
  await activePage!.goto('/login')
  marker = `m-${Date.now()}`
  await activePage!.evaluate((m) => ((window as unknown as { __marker: string }).__marker = m), marker)
})

Then('the login button reads {string}', async ({}, label: string) => {
  await expect(activePage!.getByRole('button', { name: label, exact: true })).toBeVisible()
})

When('they choose {string} in the language switcher', async ({}, language: string) => {
  await activePage!.getByRole('combobox', SWITCHER).selectOption({ label: language })
})

Then('the page was not reloaded', async () => {
  const kept = await activePage!.evaluate(() => (window as unknown as { __marker?: string }).__marker)
  expect(kept).toBe(marker)
})

Given(
  'a new {string} user logged in with the browser language {string}',
  async ({ browser, baseURL, request }, ageGroup: string, locale: string) => {
    email = await register(request, ageGroup)
    const page = await freshPage(browser, baseURL, locale)
    await logIn(page, email)
  },
)

When('the language is saved to the profile', async () => {
  // The switcher sends PUT /profile/language when logged in; the choice is stored once it answers.
  await activePage!.waitForResponse((r) => r.url().includes('/profile/language') && r.request().method() === 'PUT')
})

When('they log in again in a fresh browser with the language {string}', async ({ browser, baseURL }, locale: string) => {
  const page = await freshPage(browser, baseURL, locale)
  await logIn(page, email)
})

Then('the sidebar shows {string}', async ({}, text: string) => {
  await expect(activePage!.locator('aside').getByRole('link', { name: text })).toBeVisible()
})

When('they open the lessons page from the sidebar', async () => {
  await activePage!.locator('aside').getByRole('link', { name: 'Bài học' }).click()
})

Then('the lessons heading reads {string}', async ({}, text: string) => {
  await expect(activePage!.getByRole('heading', { level: 1, name: text })).toBeVisible()
})

Then('the first lesson title is still in English', async ({ request }) => {
  const login = await request.post('/api/auth/login', { data: { email, password } })
  const { token } = (await login.json()) as { token: string }
  const lessons = await request.get('/api/lessons', { headers: { Authorization: `Bearer ${token}` } })
  expect(lessons.ok(), await lessons.text()).toBeTruthy()
  const list = (await lessons.json()) as { title: string }[]
  expect(list.length, 'seed data should contain at least one lesson').toBeGreaterThan(0)
  const lessonTitle = list[0].title
  await expect(activePage!.getByText(lessonTitle, { exact: true }).first()).toBeVisible()
  expect(lessonTitle).toMatch(/^[\x20-\x7E]+$/)
})
