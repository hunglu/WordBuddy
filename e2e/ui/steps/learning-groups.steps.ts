import { expect, type APIRequestContext, type Page } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Fresh accounts per scenario. Same admin convention as steps/vocabulary-sharing.steps.ts.
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@wordbuddy.com'
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@123'
const password = 'ChangeMe123!'

const adultName = 'Alice Adult'
const childRealName = 'Secret Childname'
// Aliases are unique per user, so each scenario draws a fresh one (letters only).
let childAlias = 'Sunny'
const childAvatar = '🦊'

interface Account {
  email: string
  token: string
}

let supporter: Account | null = null
let childEmail = ''
let word = ''
let probeWord = ''
let groupId = ''

const auth = (token: string) => ({ Authorization: `Bearer ${token}` })
const unique = () => `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`

async function register(request: APIRequestContext, ageGroup: string, displayName: string): Promise<Account> {
  const email = `group-ui-${unique()}@example.com`
  const response = await request.post('/api/auth/register', { data: { email, password, displayName, ageGroup } })
  expect(response.ok(), await response.text()).toBeTruthy()
  const { token } = (await response.json()) as { token: string }
  return { email, token }
}

async function link(request: APIRequestContext, learner: Account, supporterAccount: Account): Promise<void> {
  const invitation = await request.post('/api/auth/support-links/invitations', {
    headers: auth(learner.token),
    data: { inviteAs: 'Learner', relationship: null },
  })
  expect(invitation.status(), await invitation.text()).toBe(201)
  const { code } = (await invitation.json()) as { code: string }
  const accept = await request.post('/api/auth/support-links/accept', {
    headers: auth(supporterAccount.token),
    data: { code, token: null },
  })
  expect(accept.ok(), await accept.text()).toBeTruthy()
}

/** Adds a word as the supporter, shares it and lets the admin approve it. Returns the word (sense) id. */
async function shareWord(request: APIRequestContext, text: string, visibleToChildren: boolean): Promise<string> {
  const token = supporter!.token
  const add = await request.post('/api/vocabulary', {
    headers: auth(token),
    data: { word: text, definition: 'A word for the groups E2E test.', example: null },
  })
  expect(add.ok(), await add.text()).toBeTruthy()
  const id = (await add.json()) as string
  const share = await request.post(`/api/vocabulary/${id}/share`, { headers: auth(token) })
  expect(share.status(), await share.text()).toBe(204)
  const login = await request.post('/api/auth/login', { data: { email: adminEmail, password: adminPassword } })
  expect(login.ok(), await login.text()).toBeTruthy()
  const admin = (await login.json()) as { token: string }
  const moderate = await request.post(`/api/vocabulary/moderation/${id}`, {
    headers: auth(admin.token),
    data: { approve: true, visibleToChildren },
  })
  expect(moderate.status(), await moderate.text()).toBe(204)
  return id
}

let probeWordId = ''

async function logIn(page: Page, email: string): Promise<void> {
  await page.goto('/login')
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: /log in/i }).click()
  await expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)
}

Given(
  'a supporter who supports an adult learner and a child learner with an alias and avatar',
  async ({ request }) => {
    supporter = await register(request, 'Adult', 'E2E Group Supporter')
    const adult = await register(request, 'Adult', adultName)
    const child = await register(request, 'Child', childRealName)
    childEmail = child.email
    childAlias = `Sunny${Array.from({ length: 6 }, () => String.fromCharCode(97 + Math.floor(Math.random() * 26))).join('')}`
    const profile = await request.put('/api/auth/profile', {
      headers: auth(child.token),
      data: { alias: childAlias, avatarId: 'fox' },
    })
    expect(profile.ok(), await profile.text()).toBeTruthy()
    // The first supporter of a child is the Primary, so adding the child needs no further approval.
    await link(request, adult, supporter)
    await link(request, child, supporter)
    word = `group-ui-${unique()}`
    probeWord = `group-ui-adult-${unique()}`
    probeWordId = ''
    groupId = ''
  },
)

Given('a child-safe shared word exists', async ({ request }) => {
  await shareWord(request, word, true)
  probeWordId = await shareWord(request, probeWord, false)
})

Given('the group supporter is logged in', async ({ page }) => {
  await logIn(page, supporter!.email)
})

Given('the group requests fail', async ({ page }) => {
  await page.route('**/api/auth/groups**', (route) => route.fulfill({ status: 502, body: 'Bad Gateway' }))
})

When('the supporter creates the group {string}', async ({ page }, name: string) => {
  await page.getByRole('link', { name: 'Groups' }).click()
  await page.getByLabel('New group').fill(name)
  await page.getByRole('button', { name: 'Create group' }).click()
  await expect(page).toHaveURL(/\/groups\/[0-9a-f-]+$/)
  groupId = page.url().split('/').pop()!
  await expect(page.getByRole('heading', { name, level: 1 })).toBeVisible()
})

When('the supporter adds both learners to the group', async ({ page }) => {
  await page.getByRole('checkbox', { name: adultName }).check()
  await page.getByRole('checkbox', { name: childAlias }).check()
  await page.getByRole('button', { name: 'Add selected' }).click()
  const members = page.getByRole('region', { name: 'Members' })
  await expect(members.getByText(adultName)).toBeVisible()
  await expect(members.getByText(childAlias)).toBeVisible()
})

When('the supporter assigns the shared word to the group', async ({ page, request }) => {
  // Content learns the members through RabbitMQ. An adult-only word is a side-effect free probe:
  // it is skipped for every member, so the skipped count equals the member count.
  await expect(async () => {
    const probe = await request.post(`/api/vocabulary/groups/${groupId}/words`, {
      headers: auth(supporter!.token),
      data: { senseIds: [probeWordId] },
    })
    expect(probe.status()).toBe(200)
    expect(((await probe.json()) as { skippedForChildren: number }).skippedForChildren).toBe(2)
  }).toPass({ timeout: 30_000 })

  await page.getByRole('tab', { name: 'Words' }).click()
  await page.getByLabel('Search the shared pool').fill(word)
  await page.getByRole('checkbox', { name: new RegExp(word) }).check()
  await page.getByRole('button', { name: /^Assign 1 word$/ }).click()
})

When('the supporter opens the Groups page', async ({ page }) => {
  await page.getByRole('link', { name: 'Groups' }).click()
})

Then('the assignment result says {int} words were added', async ({ page }, count: number) => {
  await expect(page.getByRole('status')).toContainText(`Added ${count}, already had 0`)
})

Then('the Dashboard tab lists both learners', async ({ page }) => {
  await page.getByRole('tab', { name: 'Dashboard' }).click()
  const table = page.getByRole('region', { name: 'Group dashboard' })
  // Progress learns the members through RabbitMQ; reload until the table shows them.
  await expect(async () => {
    await page.reload()
    await page.getByRole('tab', { name: 'Dashboard' }).click()
    await expect(table.getByRole('cell', { name: new RegExp(adultName) })).toBeVisible({ timeout: 2_000 })
    await expect(table.getByRole('cell', { name: new RegExp(childAlias) })).toBeVisible({ timeout: 2_000 })
  }).toPass({ timeout: 30_000 })
  await expect(table.getByText(childRealName)).toHaveCount(0)
})

Then('the child is listed by alias and avatar', async ({ page }) => {
  const members = page.getByRole('region', { name: 'Members' })
  await expect(members.getByText(childAlias, { exact: true })).toBeVisible()
  await expect(members.getByText(childAvatar)).toBeVisible()
})

Then("the child's real name and email are not on the page", async ({ page }) => {
  await expect(page.getByText(childRealName)).toHaveCount(0)
  await expect(page.getByText(childEmail)).toHaveCount(0)
})

Then('a groups error message is shown', async ({ page }) => {
  // useMyGroups keeps TanStack Query's default retries (3 with backoff, ~7 s) before it reports the error.
  await expect(page.getByText("Couldn't load your groups right now.")).toBeVisible({ timeout: 20_000 })
})
