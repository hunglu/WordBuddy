---
name: frontend-design
description: Designs and builds WordBuddy.UI screens and components that look and feel like the rest of the app — kid-friendly, bright, rounded, wb- theme tokens (light + dark), Tailwind utilities plus CSS Modules, Framer Motion animation, TanStack Query loading/error/empty states. Use whenever the user asks to create, redesign, restyle, or polish a page, component, layout, card, form, or empty/error state in WordBuddy.UI, or says "make it look nicer", "design a screen for X", "UI for feature Y". Not for backend work (use develop-webapi).
---

# WordBuddy frontend design

Make new UI indistinguishable from the existing pages (`WordBuddy.UI/src/pages/LessonsPage.tsx` is
the reference). Hard rules come from `WordBuddy.UI/CLAUDE.md` — Tailwind utilities + CSS Modules, Framer Motion only,
TanStack Query only, strict TS without `enum`. This skill adds the *visual* language.

## 1. Before designing

1. Read the feature's living spec (`docs/features/<feature>.md`) or `plan.md` — know the audience.
   **Child vs. adult matters:** child screens get bigger type, bigger targets, more emoji/colour,
   fewer words; adult screens may be denser.
2. Open 1–2 existing pages closest to what you're building and reuse their structure.
3. Check `src/components/` for something reusable before writing a new component.

## 2. Theme tokens

All colours, radii and shadows come from the `@theme` block in `WordBuddy.UI/src/index.css`.
Source of truth for names and values: `design-tokens.json` next to this skill. Never write a
Tailwind palette class (`sky-500`, `emerald-100`, `white`, ...) or a hex value in a component.

### Naming convention

- Every custom token is prefixed `wb-`: `--color-wb-*` → `bg-wb-primary`, `text-wb-ink`,
  `border-wb-border-control`; `--radius-wb-*` → `rounded-wb-card`; `--shadow-wb-*` → `shadow-wb-card`.
- Tailwind built-ins (`rounded-md`, `shadow-sm`, spacing) are **never overridden**; `--font-sans`
  is the only built-in that is set.
- Variant sets are named `wb-<domain>-<variant>-<role>`, roles `bg` / `ink` / `border`
  (`wb-share-pending-ink`, `wb-level-advanced-bg`).
- A missing colour is a design decision: ask Sam and add it to `design-tokens.json` + `index.css`
  (light and both dark blocks) — never inline a palette class "for now".

### Colour tokens (utility = `bg-`/`text-`/`border-`/`from-`… + name)

| Token | Light | Dark | Usage |
| --- | --- | --- | --- |
| `wb-surface-page` | `#f0f9ff` | `#0f172a` | App background behind sidebar and pages (today: bg-sky-50). |
| `wb-surface-card` | `#ffffff` | `#1e293b` | Cards, sidebar, inactive pills, inputs (today: bg-white). |
| `wb-ink` | `#0c4a6e` | `#e0f2fe` | Headings and primary text (today: text-sky-900). 9.5:1 light, 15.6:1 dark. |
| `wb-ink-muted` | `#0369a1` | `#7dd3fc` | Secondary text, loading and empty states. CHANGED from sky-600 (#0284c7, 4.1:1 — fails AA body text) to sky-700 (5.9:1). |
| `wb-primary` | `#0369a1` | `#38bdf8` | Primary button and active tab fill. CHANGED from sky-500 (#0ea5e9) — white on sky-500 is 2.8:1 and fails; white on sky-700 is 5.9:1. |
| `wb-primary-hover` | `#075985` | `#7dd3fc` | Primary fill on hover/pressed. |
| `wb-on-primary` | `#ffffff` | `#0f172a` | Text and icons on primary. Dark theme uses dark text on a light-blue fill (8.3:1). |
| `wb-brand-sky` | `#0ea5e9` | `#0ea5e9` | Brand hue for large decorative shapes, charts and illustrations only — never behind text (2.8:1 with white). |
| `wb-border-subtle` | `#bae6fd` | `#334155` | Decorative card and pill outlines (today: border-sky-200). Not for form controls — below 3:1. |
| `wb-border-control` | `#0284c7` | `#64748b` | Input and select borders; must reach 3:1 against surface-card (4.1:1 light, 3.1:1 dark). |
| `wb-focus-ring` | `#0284c7` | `#38bdf8` | Keyboard focus ring (ring-4). New — no focus style exists today. |
| `wb-vocab-bg` | `#e0f2fe` | `#082f49` | Vocabulary lesson card fill (today: bg-sky-100). |
| `wb-vocab-ink` | `#075985` | `#bae6fd` | Text on vocab-bg. 6.6:1 light, 10.5:1 dark. |
| `wb-vocab-border` | `#7dd3fc` | `#0369a1` | Vocabulary card outline (today: border-sky-300). |
| `wb-grammar-bg` | `#d1fae5` | `#022c22` | Grammar lesson card fill (today: bg-emerald-100). |
| `wb-grammar-ink` | `#065f46` | `#a7f3d0` | Text on grammar-bg. 6.8:1 light, 11.8:1 dark. |
| `wb-grammar-border` | `#6ee7b7` | `#047857` | Grammar card outline (today: border-emerald-300). |
| `wb-phrase-bg` | `#fef3c7` | `#451a03` | Daily phrase card fill, streak highlights (today: bg-amber-100). |
| `wb-phrase-ink` | `#92400e` | `#fde68a` | Text on phrase-bg. 6.4:1 light, 12:1 dark. |
| `wb-phrase-border` | `#fcd34d` | `#b45309` | Daily phrase card outline (today: border-amber-300). |
| `wb-success` | `#047857` | `#34d399` | Correct answer, completed lesson, success button. CHANGED from emerald-500/600 (white 2.5–3.8:1) to emerald-700 (5.5:1). Always paired with ✓ and a label. |
| `wb-on-success` | `#ffffff` | `#0f172a` | Text on success fill. |
| `wb-danger` | `#be123c` | `#fb7185` | Errors, wrong answers, log out. CHANGED from rose-600 (4.4:1 on surface-page) to rose-700. Always paired with text, never colour alone. |
| `wb-danger-soft` | `#ffe4e6` | `#4c0519` | Hover fill behind danger text, wrong-answer background. |
| `wb-share-private-bg` | `#e0f2fe` | `#082f49` | VocabularyShareStatus.Private chip fill. |
| `wb-share-private-ink` | `#075985` | `#bae6fd` | Text on wb-share-private-bg (6.6:1 / 10.5:1). |
| `wb-share-private-border` | `#7dd3fc` | `#0369a1` | Private chip outline. |
| `wb-share-pending-bg` | `#fef3c7` | `#451a03` | VocabularyShareStatus.PendingReview chip fill. |
| `wb-share-pending-ink` | `#92400e` | `#fde68a` | Text on wb-share-pending-bg (6.4:1 / 12:1). |
| `wb-share-pending-border` | `#fcd34d` | `#b45309` | PendingReview chip outline. |
| `wb-share-shared-bg` | `#d1fae5` | `#022c22` | VocabularyShareStatus.Shared chip fill. |
| `wb-share-shared-ink` | `#065f46` | `#a7f3d0` | Text on wb-share-shared-bg (6.8:1 / 11.8:1). |
| `wb-share-shared-border` | `#6ee7b7` | `#047857` | Shared chip outline. |
| `wb-share-rejected-bg` | `#ffe4e6` | `#4c0519` | VocabularyShareStatus.Rejected chip fill. |
| `wb-share-rejected-ink` | `#9f1239` | `#fecdd3` | Text on wb-share-rejected-bg (6.7:1 / 11.1:1). |
| `wb-share-rejected-border` | `#fda4af` | `#be123c` | Rejected chip outline. |
| `wb-level-beginner-bg` | `#d1fae5` | `#022c22` | Level.Beginner badge fill. NEW — levels are uncoloured today. |
| `wb-level-beginner-ink` | `#065f46` | `#a7f3d0` | Text on wb-level-beginner-bg (6.8:1). |
| `wb-level-intermediate-bg` | `#e0f2fe` | `#082f49` | Level.Intermediate badge fill. NEW. |
| `wb-level-intermediate-ink` | `#075985` | `#bae6fd` | Text on wb-level-intermediate-bg (6.6:1). |
| `wb-level-advanced-bg` | `#ede9fe` | `#2e1065` | Level.Advanced badge fill (violet). Approved by Sam 2026-10-01. |
| `wb-level-advanced-ink` | `#5b21b6` | `#ddd6fe` | Text on wb-level-advanced-bg (7.6:1 / 11:1). |
| `wb-secondary` | `#b45309` | `#fbbf24` | Secondary action fill ("Not sure"/skip, phrase audio play). CHANGED from amber-400/500 with white text (≈2:1) to amber-700 (5.0:1). |
| `wb-secondary-hover` | `#92400e` | `#fcd34d` | wb-secondary on hover. |
| `wb-on-secondary` | `#ffffff` | `#0f172a` | Text/icon on wb-secondary. |
| `wb-highlight` | `#b45309` | `#fbbf24` | Streak and highlight numbers (🔥). CHANGED from text-amber-500 (2.1:1) to amber-700 (5.0:1). |
| `wb-hover-tint` | `#e0f2fe` | `#0c4a6e` | Hover fill for nav items, tabs and option buttons (was hover:bg-sky-100). wb-ink on it: 8.2:1 both themes. |
| `wb-primary-soft` | `#e0f2fe` | `#082f49` | Soft blue secondary buttons (was bg-sky-100). Text: wb-ink (8.2:1 / 12.1:1). |
| `wb-primary-soft-hover` | `#bae6fd` | `#0c4a6e` | wb-primary-soft on hover (was hover:bg-sky-200). wb-ink on it: 7.1:1 / 8.2:1. |
| `wb-success-soft` | `#d1fae5` | `#022c22` | Soft green chips and Share button (was bg-emerald-100). Distinct meaning from wb-grammar-bg even though values match. |
| `wb-success-soft-ink` | `#065f46` | `#a7f3d0` | Text on wb-success-soft / -hover (6.8:1 / 11.8:1; on hover 6.0:1 / 7.6:1). |
| `wb-success-soft-hover` | `#a7f3d0` | `#064e3b` | wb-success-soft on hover (was hover:bg-emerald-200). |
| `wb-success-hover` | `#065f46` | `#6ee7b7` | Solid success button on hover. wb-on-success on it: 7.7:1 / 11.7:1. |
| `wb-danger-soft-ink` | `#9f1239` | `#fecdd3` | Text on soft red buttons (wb-danger-soft and its hover): 5.7:1 / 11.1:1. wb-danger on the hover fill is only 4.5:1. |
| `wb-danger-soft-hover` | `#fecdd3` | `#881337` | Soft red button on hover (was hover:bg-rose-200). Use with wb-danger-soft-ink (5.7:1 / 6.8:1). |
| `wb-grammar-surface` | `#ecfdf5` | `#041f19` | Grammar rule card background (was bg-emerald-50); example chips inside use wb-grammar-bg. wb-grammar-ink on it: 7.3:1 / 13.5:1. |
| `wb-auth-from` | `#e0f2fe` | `#0f172a` | Login/Register background gradient start (was from-sky-100). |
| `wb-auth-via` | `#ffffff` | `#1e293b` | Login/Register gradient middle (was via-white). |
| `wb-auth-to` | `#fef3c7` | `#451a03` | Login/Register gradient end (was to-amber-100). wb-ink on it: 8.5:1 / 13:1. |

Rules: `wb-brand-sky` is decorative only, never behind text. Success and danger are never colour
alone — always with a ✓/✗ and a text label. Text on a fill uses its paired `on-*`/`*-ink` token.

### Radius and shadow

| Utility | Value | Usage |
| --- | --- | --- |
| `rounded-wb-md` | 12px | Buttons and inputs (rounded-xl). |
| `rounded-wb-lg` | 16px | Sidebar items, panels (rounded-2xl). |
| `rounded-wb-card` | 24px | Lesson and stat cards (rounded-3xl) — the signature soft shape. |
| `rounded-wb-pill` | 9999px | Tabs, filter pills, level badges (rounded-full). |

| Utility | Usage |
| --- | --- |
| `shadow-wb-card` | Resting cards (shadow-sm). |
| `shadow-wb-raised` | Hovered cards, sidebar (shadow-md). |
| `shadow-wb-overlay` | Auth card and floating panels (was shadow-xl). Plain `shadow` maps to wb-card. |

### Enum-driven colours: `src/theme/variants.ts`

Colours chosen by a backend enum live in typed maps, never inline ternaries:

```ts
export const shareStatusClasses: Record<VocabularyShareStatus, string> = { Private: 'bg-wb-share-private-bg text-wb-share-private-ink border-wb-share-private-border', ... }
export const levelClasses: Record<Level, string> = { Beginner: 'bg-wb-level-beginner-bg text-wb-level-beginner-ink', ... }
```

`Record<Enum, string>` makes a missing member a compile error. Write class strings out in full (no
concatenation) so Tailwind's scanner finds them. Add a new map here for any new enum-driven colour.

### Type roles (Tailwind sizes, not tokens)

| Role | Classes |
| --- | --- |
| display | `text-4xl font-extrabold` |
| page-title | `text-3xl font-extrabold` |
| card-title | `text-xl font-bold` |
| body-lg (child body, status messages) | `text-lg` |
| body (adult body) | `text-base` |
| label | `text-sm font-semibold` |
| button | `text-lg font-bold` |

### Spacing

Spacing tokens (`space-1`…`space-8`) equal Tailwind defaults and are documentation only — use
`gap-2`, `p-5`, `mt-6`, … directly. There are no `--spacing-wb-*` variables.

### Dark mode

Tokens are CSS variables, so components need **no `dark:` prefixes**. Precedence: an explicit
`data-theme="dark"|"light"` on `<html>` wins; otherwise `prefers-color-scheme` decides. No UI sets
the attribute yet and there is no theme-flash script. The dark values appear twice in `index.css`
(media query + attribute) — keep both blocks identical and in the same order.

### Focus ring

A base rule in `index.css` gives every `a, button, input, select, textarea, [tabindex]` a 4px
`wb-focus-ring` box-shadow on `:focus-visible`. Don't add per-component focus styles and never
use a bare `focus:outline-none`.

### CSS Modules and the bundle

Only when a `className` becomes unreadable (more than ~12 utilities or long conditionals): a
`Component.module.css` with `@reference "../index.css";` using only `wb-` utilities or
`var(--color-wb-*)` — no hex, palette classes or transitions. `vite.config.ts` sets
`build.cssCodeSplit: false`, so the app ships exactly one CSS file.

Layout: `grid gap-4 sm:grid-cols-2 lg:grid-cols-3`, sections spaced `mt-4`/`mt-6`/`mt-8`; cards
`rounded-wb-card border-2 p-5`, inputs/buttons `rounded-wb-md`, pills/tabs `rounded-wb-pill`.
Emoji are the icon set (🔤 📐 💬 ⭐ 🔥) — no icon library.

## 3. Motion (Framer Motion only)

- Page entrance: `<motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>`.
- Lists/grids: stagger with `delay: index * 0.05`, `y: 16`, `duration: 0.25`.
- Feedback (correct/wrong, score reveal): `scale` pop or shake via `animate`; keep < 400 ms.
- Reusable variants must be typed: `import { type Variants } from 'framer-motion'`.
- **No CSS transitions or animations at all** — no `transition*`, `duration-*`, `ease-*`,
  `animate-*`, keyframes, or CSS hover movement (`hover:-translate-*`, `hover:scale-*`,
  `active:scale-*`, `hover:shadow-*`). Colour hovers (`hover:bg-wb-primary-hover`) are fine and
  instant.
- Lift and press go through Framer Motion: `motion.create(Link)` / `motion.button` with
  `whileHover={{ y: -4 }}` and `whileTap={{ scale: 0.97 }}`, `transition={{ duration: 0.15 }}`.
  Keep a static `shadow-wb-card`; don't animate colour or shadow in JS.
- `App.tsx` wraps everything in `<MotionConfig reducedMotion="user">`.

## 4. Every data-driven screen has four states

```tsx
{isLoading && <p className="mt-8 text-lg text-wb-ink-muted">Loading …</p>}
{isError && <p className="mt-8 text-lg text-wb-danger">Couldn't load … right now. Please try again in a moment.</p>}
{data && data.length === 0 && <p className="mt-8 text-lg text-wb-ink-muted">No … yet.</p>}
{data && data.length > 0 && /* content */}
```

Copy is friendly and plain — no stack traces, status codes, or "null". A down service must never
blank the page.

## 5. Accessibility & responsiveness

- Real `<button type="button">` / `<Link>`; never clickable `<div>`s.
- Touch targets ≥ 44px (`px-4 py-2` minimum; child screens `px-5 py-3 text-lg`).
- Visible focus: provided globally by the `wb-focus-ring` base rule — don't remove it.
- Contrast: use the paired `*-ink` / `on-*` token for text on a coloured fill (all pairs ≥ 4.5:1).
- Form fields: `<label>` tied to input, errors from Zod shown under the field in `text-wb-danger text-sm`.
- Mobile-first: works at 360px wide, then `sm:`/`lg:` enhancements.

## 6. Checklist before finishing

- [ ] Tailwind utilities or a `Component.module.css` (with `@reference` to `index.css`); colours only from theme tokens; no `style={{}}` except Framer Motion props.
- [ ] Loading / error / empty / content all handled.
- [ ] Child vs. adult presentation considered and stated in your summary.
- [ ] Keyboard-navigable, visible focus, ≥ 44px targets.
- [ ] `npm run build` and `npm run lint` pass in `WordBuddy.UI`.
- [ ] If the change alters behaviour of a feature, note it for `docs/features/<feature>.md`.
