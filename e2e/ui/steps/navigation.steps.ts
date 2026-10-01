import { expect } from '@playwright/test'
import { createBdd } from 'playwright-bdd'

const { When, Then } = createBdd()

// The sidebar marks the selected item with the primary background (see AppLayout.tsx).
const selectedClass = 'bg-wb-primary'

When('they click the {string} sidebar item', async ({ page }, label: string) => {
  await page.locator('aside').getByRole('link', { name: label }).click()
})

Then('only the {string} sidebar item is selected', async ({ page }, label: string) => {
  const sidebarLinks = page.locator('aside a')
  await expect(sidebarLinks.filter({ hasText: label })).toHaveClass(new RegExp(selectedClass))

  // Exactly one item carries the selected style, and it is the expected one.
  const selected = sidebarLinks.and(page.locator(`.${selectedClass}`))
  await expect(selected).toHaveCount(1)
  await expect(selected).toContainText(label)
})
