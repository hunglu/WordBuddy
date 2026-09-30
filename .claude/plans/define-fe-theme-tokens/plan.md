# Plan: Define FE theme tokens (v5.0)

## Summary

Define every custom colour, radius and shadow once in a Tailwind v4 `@theme` block in `WordBuddy.UI/src/index.css`, all prefixed `wb-` (`--color-wb-*`, `--radius-wb-*`, `--shadow-wb-*`), with names and values taken from `design-tokens.json` (v2, source of truth). Light values and new dark values are CSS variables. Dark mode follows `prefers-color-scheme`, and a `data-theme` attribute overrides it. The 15 UI files that use raw palette classes move to token utilities (`bg-wb-primary`, `text-wb-ink`, ...). Enum-driven colours (share status, level) go through typed `Record<Enum, string>` maps in `src/theme/variants.ts`. All CSS transitions and CSS hover movement are removed. Hover colour changes become instant, and any lift or press motion moves to Framer Motion `whileHover`/`whileTap`. A base rule adds a visible focus ring. `build.cssCodeSplit: false` produces one CSS bundle, measured before and after. Only the frontend changes.

## Affected services / areas

- WordBuddy.UI: `src/index.css`, `vite.config.ts`, new `src/theme/variants.ts`, 2 layouts, 10 pages, 3 lesson components, and optional `*.module.css` files.
- `.claude/skills/frontend-design/SKILL.md` (plus a copy of `design-tokens.json`).
- `docs/features/ui-theme.md`: a new living spec, created by the tester on merge.
- `WordBuddy.UI/CLAUDE.md`: **no change**. The Styling rule already allows CSS Modules and the single bundle. The Animations rule stays as it is, and this plan complies with it.
- Backend: none.

## Backend approach

None. There are no entities, endpoints, migrations or messaging changes.

## Frontend approach

### Build: single CSS bundle (unchanged from v3)

- Baseline: on the unchanged code, run `npm run build` and record the number of `dist/assets/*.css` files plus the raw and gzip size of each.
- Set `build: { cssCodeSplit: false }` in `vite.config.ts`, then verify that exactly one `.css` file is emitted and record its size.
- Measure again after the migration. The tester copies all three rows into `test-report.md`.

### Token definition (`src/index.css`)

```css
@import "tailwindcss";

@custom-variant dark (&:where([data-theme="dark"], [data-theme="dark"] *));

@theme {
  --color-wb-surface-page: var(--wb-surface-page);
  --color-wb-primary: var(--wb-primary);
  /* ... one --color-wb-* per colour token in design-tokens.json (incl. share-*, level-*, secondary*, on-secondary, highlight) ... */
  --radius-wb-md: 12px; --radius-wb-lg: 16px; --radius-wb-card: 24px; --radius-wb-pill: 9999px;
  --shadow-wb-card: ...; --shadow-wb-raised: ...;
  --font-sans: ...;
}

:root { /* light --wb-* values */ color-scheme: light; }
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) { /* dark values */ color-scheme: dark; }
}
[data-theme="dark"] { /* dark values */ color-scheme: dark; }
```

- The theme reaches components through variables, so they need no `dark:` prefixes.
- **Built-in Tailwind tokens are never overridden.** There is no `--radius-md`, `--shadow-sm` or `--spacing` here. `--font-sans` is the only built-in that is set, and it matches the current system stack. Spacing tokens are for documentation only, so there are no `--spacing-wb-*` variables.
- Precedence: an explicit `data-theme` on `<html>` wins; without it, the OS setting decides. No UI sets the attribute (a toggle is out of scope), and there is **no theme-flash script** (Sam's decision in v5).
- The dark values appear twice. Keep both blocks in the same order so they are easy to diff.
- `wb-brand-sky` is decorative only and must never sit behind text.
- The `wb-level-advanced-*` variables are defined together with the rest, but they are **not applied** to any component until Sam confirms violet (see the stop point).

### Focus ring (base rule in `index.css`)

```css
@layer base {
  :where(a, button, input, select, textarea, [tabindex]):focus-visible {
    outline: none;
    box-shadow: 0 0 0 4px var(--color-wb-focus-ring);
  }
}
```

This is a static style, not a transition, so it complies with the Animations rule. Remove any `focus:outline-none` that has no replacement.

### Variant maps (`src/theme/variants.ts`)

This is a plain TS module with no JSX. It returns class strings built only from `wb-` tokens, and it gives the enum-driven colours one place to live:

```ts
import type { Level, VocabularyShareStatus } from '../types';

export const shareStatusClasses: Record<VocabularyShareStatus, string> = {
  Private: 'bg-wb-share-private-bg text-wb-share-private-ink border-wb-share-private-border',
  PendingReview: 'bg-wb-share-pending-bg text-wb-share-pending-ink border-wb-share-pending-border',
  Shared: 'bg-wb-share-shared-bg text-wb-share-shared-ink border-wb-share-shared-border',
  Rejected: 'bg-wb-share-rejected-bg text-wb-share-rejected-ink border-wb-share-rejected-border',
};

export const levelClasses: Record<Level, string> = { /* beginner / intermediate / advanced */ };
```

- Class strings are written out in full (no string concatenation), so Tailwind's scanner picks them up.
- The union member names must match `src/types/index.ts` exactly. The coder checks this; the names above are illustrative. `Record` makes a missing member a compile error.
- Share-status chips are used in VocabularyBuilderPage, VocabularyModerationPage and VocabularySharedPoolPage. Level badges are used in LessonsPage and LessonDetailPage.
- Level badges are coloured for the first time (today they are uncoloured). Beginner (emerald) and intermediate (sky) reuse existing hues. **Advanced (violet) is a new colour family.** Before `levelClasses.Advanced` is filled in with violet tokens, the coder **stops and asks Sam**. Until he confirms, Advanced uses a neutral fallback (`bg-wb-surface-card text-wb-ink border-wb-border-subtle`).

### Secondary and highlight

- `bg-wb-secondary text-wb-on-secondary hover:bg-wb-secondary-hover` replaces the amber action buttons: the "Not sure"/skip button in VocabularyCheckPage and the audio play button in DailyPhraseList. Today these use white on amber-400/500, which is about 2:1.
- `text-wb-highlight` replaces `text-amber-500` streak numbers in DashboardPage and ProgressPage. Phrase card fills and outlines stay on `wb-phrase-*`.

### Motion migration (Animations rule compliance)

- Remove every `transition`, `transition-*`, `duration-*` and `ease-*` utility, plus any `hover:-translate-*`, `hover:scale-*`, `hover:shadow-*` or `active:scale-*` that produces motion.
- Colour hovers such as `hover:bg-wb-primary-hover` stay, but they are **instant**, with no transition.
- Wherever the current UI has lift or press motion (for example lesson cards with `hover:-translate-y-1 hover:shadow-md`, or buttons with `active:scale-95`), switch the element to `motion.*` and use `whileHover={{ y: -4 }}` / `whileTap={{ scale: 0.97 }}` with a short `transition` prop. Framer Motion props do not count as CSS. Replace a hover shadow change with a static `shadow-wb-card`. Do not animate shadow or colour through Framer Motion, because that would need hex values in JS.
- Respect reduced motion by wrapping `App` in `<MotionConfig reducedMotion="user">`, unless something equivalent already exists.

### CSS Modules (optional, unchanged from v3)

A CSS Module is only for a component whose migrated `className` becomes unreadable (more than about 12 utilities, or long conditionals). It is named `Component.module.css`, uses `@reference` to `index.css`, and contains only `wb-` token utilities or `var(--color-wb-*)`. It must not contain hex values, palette classes or transitions. Each extraction is its own commit with a stated reason, and zero extractions is fine.

### Migration mapping

| Today | Token utility |
| --- | --- |
| `bg-sky-50` | `bg-wb-surface-page` |
| `bg-white` | `bg-wb-surface-card` |
| `text-sky-900` / `text-sky-800` | `text-wb-ink` |
| `text-sky-500/600/700` | `text-wb-ink-muted` |
| `bg-sky-500/600` + `text-white` | `bg-wb-primary text-wb-on-primary hover:bg-wb-primary-hover` |
| `border-sky-200` | `border-wb-border-subtle` |
| input/select borders | `border-wb-border-control` |
| `sky-100/300` (vocab) | `wb-vocab-bg` / `wb-vocab-ink` / `wb-vocab-border` |
| `emerald-100/300` (grammar) | `wb-grammar-*` |
| `amber-100/300` (phrase card) | `wb-phrase-*` |
| `amber-400/500` action button + white | `bg-wb-secondary text-wb-on-secondary hover:bg-wb-secondary-hover` |
| `text-amber-500` (streak number) | `text-wb-highlight` |
| `emerald-500/600` + white, success text | `bg-wb-success text-wb-on-success` / `text-wb-success` |
| `rose-*` | `text-wb-danger`, `bg-wb-danger-soft` |
| share-status chip colours | `shareStatusClasses[status]` |
| level badge (uncoloured) | `levelClasses[level]` |
| `rounded-xl/2xl/3xl/full` | `rounded-wb-md` / `rounded-wb-lg` / `rounded-wb-card` / `rounded-wb-pill` |
| `shadow-sm` / `shadow-md` | `shadow-wb-card` / `shadow-wb-raised` |

Built-in classes such as `rounded-md` or `shadow-sm` are not broken by this work, because nothing overrides them. They are still replaced wherever they carry a design role. If a colour has no mapping, the coder records it under "Gaps" in `tasks.md` and does not invent a token.

### Files to migrate

The list is the same 15 files as in v3: `layouts/{PublicLayout,AppLayout}`, `pages/{Dashboard,Lessons,LessonDetail,Login,Register,Progress,VocabularyBuilder,VocabularyCheck,VocabularyModeration,VocabularySharedPool}Page`, and `components/lessons/{VocabularyList,GrammarRuleList,DailyPhraseList}`. Transition, translate or amber matches were found in 13 of these files (46 occurrences). The final grep re-checks `ProtectedRoute.tsx` and `CountUpStat.tsx` as well.

### Skill update

Add a "Theme tokens" section to `.claude/skills/frontend-design/SKILL.md`. It covers:

- the token table using `wb-` names, with usage notes
- the naming convention (`wb-` prefix, built-ins never overridden, variant sets named `wb-<domain>-<variant>-<role>` with the roles bg/ink/border)
- the `Record<Enum,string>` pattern in `src/theme/variants.ts`
- the rules for `wb-brand-sky`, and for success/danger (always shown with a label)
- type roles mapped to Tailwind sizes
- the spacing note
- dark-mode precedence
- the focus ring
- motion: no CSS transitions, only instant colour hovers, with lift and press done through Framer Motion
- when to use a CSS Module, and the single bundle

Copy `design-tokens.json` into the skill folder.

### Child vs adult

- Colours, contrast, focus, dark mode and motion behave the same for both age groups.
- Child screens keep their larger type (`text-lg`/`text-xl`) and tap targets (`py-3 px-4`). The migration changes only colour, radius, shadow and motion classes, and it never changes sizes or padding.
- There are no changes to content or authorization.

### E2E impact

`e2e/ui` selectors are based on role and text. Re-run the full suite. New scenarios cover dark mode (the `data-theme` attribute and `colorScheme: 'dark'` emulation), the keyboard focus ring, and a check that no element computes a non-zero CSS `transition-duration`.

## Data / migration notes

None.

## Living spec

The tester creates `docs/features/ui-theme.md` on merge. It describes the tokens, **the `wb-` naming convention and variant naming**, the variant maps, dark-mode precedence, the focus ring, the motion policy, the single CSS bundle and CSS Module usage, and child/adult behaviour.

## Open questions

1. **Violet for `wb-level-advanced-*`**: this is a stop point in tasks. Advanced uses a neutral fallback until Sam confirms.
2. The union member names for `VocabularyShareStatus` and `Level` in `src/types/index.ts` may differ from the labels used here (for example `PendingReview`). The coder follows the types file and does not rename enums.
3. Colours in the Vocabulary* pages that fall outside the mapping (for example indigo) are flagged under "Gaps", not guessed.
