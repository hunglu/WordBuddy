---
feature: App navigation (sidebar)
services: UI
audience: Both
state: shipped
last-updated-by: 2-navigation-items-have-selected-state
---

# App navigation (sidebar)

Signed-in pages share one sidebar (`AppLayout`); exactly one item — the current page — is highlighted.

## What it does

Items: Home, Lessons, My Vocabulary, Shared Pool, My Progress, and Moderation (admins only).

## Rules

| Item | Selected on |
| --- | --- |
| Home | `/` only |
| Lessons | `/lessons` and every sub-route (e.g. `/lessons/42`) |
| My Progress | `/progress` and every sub-route |
| My Vocabulary | `/vocabulary` and `/vocabulary/check` (the check-up has no item of its own) |
| Shared Pool | `/vocabulary/shared` only |
| Moderation | `/vocabulary/moderation` only |

- The selected item carries the highlight style **and** `aria-current="page"`, so screen readers announce the same item sighted users see.
- **Child vs adult:** same sidebar. Moderation is visible to admins only.

## API

None — UI only.

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

`WordBuddy.UI/src/layouts/AppLayout.tsx`. E2E: `e2e/ui/features/navigation.feature`.

## Pending changes

## Change history

- `2-navigation-items-have-selected-state` (#5) — only the current page's item is selected;
  `aria-current` follows the highlight. Merged before a green E2E run; scenarios passed later
  (see that plan's `test-report.md`).
