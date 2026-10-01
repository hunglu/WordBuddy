# Plan: 2 navigation Items have selected state

## Summary

In `WordBuddy.UI/src/layouts/AppLayout.tsx`, the **My Vocabulary** item (`/vocabulary`) uses
`end: false`, so React Router's `NavLink` also marks it active on every `/vocabulary/*` route.
That includes `/vocabulary/shared` (the reported bug) and `/vocabulary/moderation`, which is the
same bug for admins. The fix is to give each nav item an explicit list of paths it owns.
**My Vocabulary** owns `/vocabulary` and `/vocabulary/check`. The check-up page belongs to My
Vocabulary and has no sidebar item of its own, so it must keep My Vocabulary highlighted.
**Shared Pool** and **Moderation** each own only their own route.

## Affected services / areas

WordBuddy.UI only: `src/layouts/AppLayout.tsx`. No backend changes.

## Backend approach

None.

## Frontend approach

- In `AppLayout.tsx`, give the nav item type an optional `activePaths?: string[]`. Set it to
  `['/vocabulary', '/vocabulary/check']` for **My Vocabulary**, and set that item's `end` to
  `true`.
- Work out `isActive` from `useLocation().pathname`. When an item has `activePaths`, use
  `activePaths.some((p) => matchPath({ path: p, end: true }, pathname) !== null)`. Otherwise use
  NavLink's own `isActive`. The `className` callback is the only place this logic lives. No new
  state and no Zustand.
- Set `end: true` on `/vocabulary/shared` and `/vocabulary/moderation` as well. They have no
  child routes today, and this makes their match exact.
- Leave `/lessons` and `/progress` as `end: false` so lesson detail sub-routes keep **Lessons**
  highlighted. Leave `/` as `end: true`.
- Use a typed `NavItem` interface. No `any`. Styling and animation stay as they are.

Route inventory checked in `App.tsx`: `/vocabulary`, `/vocabulary/check`, `/vocabulary/shared`,
`/vocabulary/moderation`.

## Child vs. adult

Children and adults see the same sidebar, so the behaviour is the same for both. The
admin-only Moderation item gets the same fix.

## Data / migration notes

None.

## Open questions

- Judgment call: `/vocabulary/check` keeps **My Vocabulary** highlighted, because it is reached
  from that page and has no sidebar entry of its own. Tell me if it should highlight nothing.
- Living spec `docs/features/app-navigation.md` should describe the corrected highlighting rule.
  The tester updates it in `/test`.
