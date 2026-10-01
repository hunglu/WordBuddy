---
feature: App navigation (sidebar)
services: UI
audience: Both
state: shipped
last-updated-by: 2-navigation-items-have-selected-state
---

# App navigation (sidebar)

## What it does

Signed-in pages share a sidebar (`AppLayout`) with Home, Lessons, My Vocabulary, Shared Pool and
My Progress, plus Moderation for admins. Exactly one item is highlighted: the one for the current
page.

## Rules

- **Home** is selected only on `/`.
- **Lessons** and **My Progress** are selected on their route and every page under it
  (e.g. `/lessons/42`).
- **My Vocabulary** is selected only on `/vocabulary` and `/vocabulary/check` (the check-up page
  has no sidebar item of its own).
- **Shared Pool** (`/vocabulary/shared`) and **Moderation** (`/vocabulary/moderation`) are
  selected only on their own route.
- The selected item has the highlight style and `aria-current="page"`, so screen readers announce
  the same item that sighted users see.
- Children and adults get the same sidebar. Moderation is shown only to admins.

## API

None. UI only.

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

`WordBuddy.UI/src/layouts/AppLayout.tsx`. E2E: `e2e/ui/features/navigation.feature`.

## Pending changes

## Change history

- `2-navigation-items-have-selected-state` — only the current page's sidebar item is selected;
  `aria-current` follows the highlight (#5). Merged without a green E2E run; see that plan's
  `test-report.md`.
