import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { Given, When, Then } = createBdd()

// Dark wb-surface-page (#0f172a) as computed by the browser.
const darkSurfacePage = 'rgb(15, 23, 42)'

Given("the learner's system prefers a dark colour scheme", async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' })
})

When('the document root has data-theme {string}', async ({ page }, theme: string) => {
  await page.evaluate((t) => document.documentElement.setAttribute('data-theme', t), theme)
})

Then('the page background is the dark surface colour', async ({ page }) => {
  const surface = await page.evaluate(() =>
    getComputedStyle(document.documentElement).getPropertyValue('--color-wb-surface-page').trim(),
  )
  expect(surface.toLowerCase()).toBe('#0f172a')
  // The auth layout paints the page with the wb-auth gradient; its start colour is the dark surface.
  const background = await page.evaluate(() => {
    const el = document.querySelector('#root > *') ?? document.body
    const s = getComputedStyle(el)
    return `${s.backgroundColor} ${s.backgroundImage}`
  })
  expect(background).toContain(darkSurfacePage)
})

When('they tab to the log in button', async ({ page }) => {
  const button = page.getByRole('button', { name: /log in/i })
  for (let i = 0; i < 20; i++) {
    await page.keyboard.press('Tab')
    if (await button.evaluate((el) => el === document.activeElement)) return
  }
  throw new Error('Log in button never received keyboard focus')
})

Then('the focused element shows a focus ring', async ({ page }) => {
  const shadow = await page.evaluate(() =>
    document.activeElement ? getComputedStyle(document.activeElement).boxShadow : 'none',
  )
  expect(shadow).not.toBe('none')
})

When('they open the lessons page', async ({ page }) => {
  await page.getByRole('link', { name: /lessons/i }).first().click()
  await expect(page).toHaveURL(/\/lessons/)
  await expect(page.locator('h1')).toBeVisible()
})

Then('no button, link or card has a CSS transition', async ({ page }) => {
  const offenders = await page.evaluate(() =>
    Array.from(document.querySelectorAll('button, a, [class*="rounded-wb-card"]'))
      .filter((el) =>
        getComputedStyle(el)
          .transitionDuration.split(',')
          .some((d) => parseFloat(d) !== 0),
      )
      .map((el) => el.outerHTML.slice(0, 120)),
  )
  expect(offenders).toEqual([])
})
