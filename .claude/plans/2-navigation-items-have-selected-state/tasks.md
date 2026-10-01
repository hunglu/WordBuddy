# Tasks: 2 navigation Items have selected state

## Frontend

- [x] Add a typed `NavItem` interface with optional `activePaths` in `WordBuddy.UI/src/layouts/AppLayout.tsx` — `npm run build` passes with no `any`. — WordBuddy.UI/src/layouts/AppLayout.tsx
- [x] Set My Vocabulary to `end: true` with `activePaths: ['/vocabulary', '/vocabulary/check']`, and set Shared Pool and Moderation to `end: true` — config compiles. — WordBuddy.UI/src/layouts/AppLayout.tsx
- [ ] Compute the active state from `useLocation()` + `matchPath` for items that have `activePaths`, falling back to NavLink `isActive` for the others — only one item is highlighted on every route.

## Tests

- [ ] Add a Playwright-BDD scenario in `e2e/ui` for the reported steps (My Vocabulary, then Shared Pool) — only Shared Pool shows the selected state.
- [ ] Add a scenario outline in `e2e/ui` covering `/`, `/lessons`, `/vocabulary`, `/vocabulary/check`, `/vocabulary/shared`, `/progress` — exactly one item is selected, and it is the expected one (My Vocabulary for `/vocabulary/check`).
- [ ] Add an admin scenario for `/vocabulary/moderation` — only Moderation is selected.
