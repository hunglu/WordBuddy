---
title: Define FE theme tokens
status: implemented
type: change
issue: 2
affects: docs/features/ui-theme.md
supersedes: none
version: 9.0
created: 2026-09-30T00:00:00+07:00
updated: 2026-10-01T00:31:45+07:00
---

## Problem

WordBuddy.UI hardcodes Tailwind palette classes (`bg-sky-500`, `text-sky-600`, `bg-emerald-500`,
`text-rose-600`, …) directly in ~10 pages. Colours carry no role, so changing one means editing
every file, and there is no dark mode. A contrast check on 2026-09-30 found four failures:
white on `sky-500` primary buttons (2.77:1), white on `emerald-500` (2.54:1), `text-sky-600`
secondary text (4.10:1) and `text-rose-600` on `sky-50` (4.41:1). There is also no visible
keyboard focus style and input borders are below 3:1.

## Goal

Replace hardcoded palette classes with semantic theme tokens defined once in a Tailwind v4
`@theme` block, following the approved WordBuddy Design System
(https://claude.ai/artifact/1jA6eiuUEABzgvc3mHPJ73): role-named colours (`primary`, `ink`,
`ink-muted`, `vocab-*`, `grammar-*`, `phrase-*`, `success`, `danger`, `focus-ring`,
`border-control`…), radius, spacing and shadow tokens, with a light theme (current look,
contrast-fixed) and a new dark theme.

## Target users

Both children and adults. Child screens keep larger type and tap targets as described in the
design system.

## Success criteria

- All colours in `src/` come from theme tokens; no raw palette classes (`sky-500`, `rose-600`, …) remain in components or pages.
- Every text/background pair meets WCAG AA (4.5:1 body, 3:1 large text, control borders and focus rings) in both light and dark.
- Visible focus ring on every interactive element.
- Dark theme works via `prefers-color-scheme` and an explicit `data-theme` switch.
- `npm run build` and `npm run lint` pass; E2E UI suite still passes.
- `frontend-design` skill and the relevant CLAUDE.md rule updated to reference the tokens.

## Constraints

- Requires changing the `WordBuddy.UI/CLAUDE.md` rule that `src/index.css` contains only
  `@import "tailwindcss"` (CLAUDE.md edits are ask-gated — Sam approves).
- Tailwind + Framer Motion rules stay; no new UI library, no custom font files.
- Out of scope: new screens, layout redesign, a user-facing theme toggle UI (may be a follow-up).
- Open question for `/plan`: whether hover `transition-colors` stays allowed or moves to Framer Motion.

## Revisions

- **v3.0 — 2026-09-30T00:00:00+07:00** — CSS Modules + single CSS bundle. Sam changed the `WordBuddy.UI/CLAUDE.md`
  Styling rule (already applied): Tailwind utilities first, component-scoped
  `Component.module.css` allowed (using `@reference` to `index.css` for Tailwind/tokens), global
  CSS only in `src/index.css`. Added to scope:
  - Set `build.cssCodeSplit: false` in `vite.config.ts` so `vite build` emits one minified CSS
    file (all modules + `index.css`); verify with `npm run build` that `dist/assets/` has exactly
    one `.css` file and report its size (raw + gzip) before and after.
  - Where a migrated component's class string becomes unreadable, move it to a CSS Module that
    uses token variables — optional, not a goal in itself.
  - Update the "Constraints" line above: the CLAUDE.md rule change is done; no further
    CLAUDE.md edit is needed for `index.css`.
  Status reset to `idea` so `/plan` regenerates `plan.md` and `tasks.md`.
- **v5.0 — 2026-09-30T23:16:10+07:00** — Sam's decisions on the v4 plan:
  1. **No CSS transition exception.** The Animations rule stays as written: remove every
     `transition-colors`/`transition-transform` and CSS hover movement; hover colour changes are
     instant, and any hover/press motion goes through Framer Motion (`whileHover`/`whileTap`).
  2. **Variant colour sets.** Colours that depend on a domain value get their own token set,
     named `wb-<domain>-<variant>-<role>` (roles: `bg`, `ink`, `border`). Added to
     `design-tokens.json`: `wb-share-{private,pending,shared,rejected}-*`
     (`VocabularyShareStatus`), `wb-level-{beginner,intermediate,advanced}-*` (`Level`),
     `wb-secondary` (+hover, on-secondary) and `wb-highlight`. Each is a `Record<Enum, string>`
     map in code, like today's `STATUS_COLORS`. Level badges become coloured (new);
     `wb-level-advanced` introduces violet — the coder confirms with Sam before using it.
  3. **Custom vs built-in is visible.** Every custom token is prefixed `wb-`
     (`--color-wb-*`, `--radius-wb-*`, `--shadow-wb-*` → `bg-wb-primary`, `rounded-wb-card`).
     Tailwind's built-in scales (`rounded-md`, `shadow-sm`, spacing) are never overridden;
     spacing tokens are documentation only.
  4. **No theme-flash script** for now.
  5. **`affects: docs/features/ui-theme.md`** — the tester creates it on merge.
  Also: the Constraints line about changing the `index.css` rule is obsolete (done in v3.0).
  Status reset to `idea` for a re-plan.
