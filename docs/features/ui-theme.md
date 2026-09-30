---
feature: UI theme tokens
services: UI
audience: Both
state: shipped
last-updated-by: define-fe-theme-tokens
---

# UI theme tokens

## What it does

Every custom colour, radius and shadow in WordBuddy.UI is defined once, in a Tailwind v4 `@theme`
block in `src/index.css`. Components use only token utilities (`bg-wb-primary`, `text-wb-ink`,
`rounded-wb-card`, `shadow-wb-card`). Raw palette classes, `white`/`black` and hex values are not
used outside `index.css`. There is a light theme (the original look, with contrast fixes) and a
dark theme. Every text/background token pair meets WCAG AA in both themes (text at least 4.5:1,
control borders and focus ring at least 3:1).

## Rules

- **Naming.** Every custom token starts with `wb-` (`--color-wb-*`, `--radius-wb-*`,
  `--shadow-wb-*`), so a review can tell custom tokens from Tailwind built-ins. Built-in scales
  (`rounded-md`, `shadow-sm`, spacing) are never overridden. `--font-sans` is the only built-in
  that is set. Spacing tokens are for documentation only.
- **Variant naming.** Colours that depend on a domain value use `wb-<domain>-<variant>-<role>`,
  with the roles `bg`, `ink` and `border`, for example `wb-share-pending-ink` or
  `wb-level-advanced-bg`.
- **Variant maps.** Enum-driven colours live in `src/theme/variants.ts` as
  `Record<Enum, string>` maps (`shareStatusClasses` for `VocabularyShareStatus`, `levelClasses`
  for `Level`). A missing enum member is a compile error. Class strings are written out in full.
  Level badges are Beginner = emerald, Intermediate = sky, Advanced = violet.
- **Token source.** `design-tokens.json` (copy in `.claude/skills/frontend-design/`) is the source
  of values. The role table is in the `frontend-design` skill.
- **Dark mode precedence.** An explicit `data-theme="dark"` or `"light"` on `<html>` wins.
  Without it, `prefers-color-scheme` decides. There is no user-facing toggle and no theme-flash
  script.
- **Focus ring.** A base rule gives every `a, button, input, select, textarea, [tabindex]` a 4px
  `wb-focus-ring` box-shadow on `:focus-visible`.
- **Motion.** There are no CSS transitions. Hover colour changes are instant. Lift and press motion
  uses Framer Motion `whileHover`/`whileTap`, and the app respects reduced motion
  (`MotionConfig reducedMotion="user"`).
- **Bundle / CSS Modules.** `build.cssCodeSplit: false`, so the build emits one CSS file. A
  `Component.module.css` is allowed only when a `className` becomes unreadable. It must use
  `@reference` to `index.css` and contain only `wb-` utilities or variables, with no hex values or
  transitions. There are none today.
- **Special cases.** `wb-brand-sky` is decorative only and never sits behind text. Success and
  danger colours always come with a label.
- **Child vs adult.** Colours, contrast, focus, dark mode and motion are the same for both.
  Child screens keep their larger type and tap targets. The theme changes no sizes, content or
  authorization.

## API

None. This is frontend only.

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

This applies to every page and layout: PublicLayout, AppLayout, Dashboard, Lessons, LessonDetail,
Login, Register, Progress, and the Vocabulary Builder/Check/Moderation/SharedPool pages.

## Pending changes

None.

## Change history

- `define-fe-theme-tokens` (#2): semantic `wb-` theme tokens, dark mode, focus ring, and no CSS transitions
