# Tasks: Define FE theme tokens (v5.0)

Work top to bottom, one commit per item. "Verify" means run `npm run build && npm run lint` in `WordBuddy.UI`, then do the listed check. Every custom token is `wb-` prefixed. Never override Tailwind built-ins.

## Backend

- (none)

## Frontend: build baseline and bundling

- [x] Baseline measurement: on unchanged code, run `npm run build` and record the `dist/assets/*.css` count and the raw and gzip size of each file under "Measurements". This may be committed together with the next task. Verify: the numbers are recorded. — (measurement only, no files)
- [x] Set `build.cssCodeSplit: false` in `vite.config.ts`. Verify: exactly one `.css` file is in `dist/assets/`, and its sizes are recorded. — `WordBuddy.UI/vite.config.ts`

## Frontend: foundation

- [x] In `src/index.css`, add `@custom-variant dark`, and an `@theme` block with every colour as `--color-wb-*` → `var(--wb-*)`, `--radius-wb-{md,lg,card,pill}`, `--shadow-wb-{card,raised}` and `--font-sans`. Add the light `:root` values, and the dark values under both `@media (prefers-color-scheme: dark) :root:not([data-theme="light"])` and `[data-theme="dark"]`. Values must match `design-tokens.json` v2 exactly. Verify: no built-in token (`--radius-md`, `--shadow-sm`, `--spacing*`) is defined, and `data-theme="dark"` changes the computed `--color-wb-primary`. — `WordBuddy.UI/src/index.css` (generated from design-tokens.json; no built-in radius/shadow/spacing defined; computed-value check left to tester)
- [x] Add the `@layer base` `:focus-visible` ring (4px `var(--color-wb-focus-ring)`), and remove any `focus:outline-none` that has no replacement. Verify: tabbing through Login shows the ring on every link, input and button, in both light and dark. — `src/index.css`, `pages/{Login,Register,VocabularyBuilder}Page.tsx` (visual tab-through left to tester)
- [x] Wrap the app in `<MotionConfig reducedMotion="user">` in `App.tsx`, unless an equivalent already exists. Verify: the build passes and route transitions still animate. — `WordBuddy.UI/src/App.tsx`
- [x] Create `src/theme/variants.ts` with `shareStatusClasses: Record<VocabularyShareStatus, string>` (wb-share-*) and `levelClasses: Record<Level, string>`. Fill Beginner and Intermediate with wb-level-* tokens, and give Advanced the neutral fallback (`bg-wb-surface-card text-wb-ink border-wb-border-subtle`). Write the class strings in full. Verify: the keys match the unions in `src/types/index.ts`, and the build passes with no `any`. — `WordBuddy.UI/src/theme/variants.ts`

## Frontend: migration (use plan.md's mapping, and never change sizes or padding)

In each file, also remove `transition*`/`duration-*`/`ease-*` and CSS hover/active movement. Keep colour hovers, which are now instant. Move lift and press motion to `motion.*` `whileHover`/`whileTap`.

- [x] Migrate `layouts/PublicLayout.tsx` and `layouts/AppLayout.tsx`. Verify: no palette or `transition` classes remain, and the sidebar looks unchanged in light. — `layouts/PublicLayout.tsx`, `layouts/AppLayout.tsx` (unmapped left in place, see Gaps)
- [x] Migrate `pages/LoginPage.tsx`. Verify: the primary button uses `bg-wb-primary text-wb-on-primary hover:bg-wb-primary-hover`, and inputs use `border-wb-border-control`. — `pages/LoginPage.tsx`
- [x] Migrate `pages/RegisterPage.tsx`. Verify: the same checks as Login pass, and the AgeGroup selector still works. — `pages/RegisterPage.tsx`
- [x] Migrate `pages/DashboardPage.tsx`. Verify: streak numbers use `text-wb-highlight`, cards use `shadow-wb-card`/`rounded-wb-card`, and any card lift is done with `whileHover`. — `pages/DashboardPage.tsx` (no streak numbers exist on this page; cards now `motion.create(Link)` with whileHover/whileTap; hover:shadow-lg dropped for static shadow-wb-card)
- [x] Migrate `pages/LessonsPage.tsx`, applying `levelClasses` to level badges. Verify: cards use `wb-vocab-*`/`wb-grammar-*`, the active tab uses `bg-wb-primary`, card hover lift uses Framer Motion, and no `hover:-translate`/`hover:shadow` remains. — `pages/LessonsPage.tsx`
- [x] Migrate `pages/LessonDetailPage.tsx` and `components/lessons/{VocabularyList,GrammarRuleList}.tsx`, applying `levelClasses` where a level is shown. Verify: no palette or `transition` classes remain. — `pages/LessonDetailPage.tsx`, `components/lessons/VocabularyList.tsx`, `components/lessons/GrammarRuleList.tsx` (no level is displayed in these files, so levelClasses not applied)
- [x] Migrate `components/lessons/DailyPhraseList.tsx`. Verify: the card uses `wb-phrase-*`, and the audio button uses `bg-wb-secondary text-wb-on-secondary hover:bg-wb-secondary-hover`. — `components/lessons/DailyPhraseList.tsx` (card fill amber-50 → `wb-phrase-bg`, the closest phrase role)
- [x] Migrate `pages/ProgressPage.tsx`. Verify: success and danger states use `wb-` tokens and keep their labels, and streak numbers use `text-wb-highlight`. — `pages/ProgressPage.tsx`
- [ ] Migrate `pages/VocabularyBuilderPage.tsx`, using `shareStatusClasses` for status chips. Verify: no palette classes remain, or the gaps are listed.
- [ ] Migrate `pages/VocabularyCheckPage.tsx`. Verify: correct and wrong answers use `wb-success`/`wb-danger`/`wb-danger-soft`, each with a label, and the skip button uses `wb-secondary`.
- [ ] Migrate `pages/VocabularyModerationPage.tsx` and `pages/VocabularySharedPoolPage.tsx`, using `shareStatusClasses`. Verify: no palette classes remain, or the gaps are listed.
- [ ] **STOP POINT: ask Sam to confirm violet for `wb-level-advanced-*`.** Only after he approves, set `levelClasses.Advanced` to `bg-wb-level-advanced-bg text-wb-level-advanced-ink`. If he declines, keep the fallback and record his decision under "Gaps". Verify: Sam's decision is recorded in the commit message or under "Gaps".
- [ ] (Optional, one commit per component) Extract any unreadable `className` into a `Component.module.css` that uses `@reference` to `index.css`, with only `wb-` utilities or `var(--color-wb-*)`, and no hex values or transitions. Verify: the page renders the same, there is still one CSS file, and the commit message states the reason. Skip this task if nothing qualifies.
- [ ] Final sweep. Verify: `rg -e "-(slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose)-[0-9]" -e "(bg|text|border)-(white|black)" -e "#[0-9a-fA-F]{3,8}" WordBuddy.UI/src --glob "!index.css"` returns nothing, and `rg -e "transition-|\btransition\b|duration-|hover:-?translate|hover:scale|active:scale|hover:shadow" WordBuddy.UI/src --glob "*.{tsx,ts,css}"` returns only Framer Motion `transition={...}` props.
- [ ] Post-migration measurement. Verify: there is still exactly one `.css` file, and its raw and gzip sizes and the difference from the baseline are recorded.

## Docs / rules

- [ ] Update `.claude/skills/frontend-design/SKILL.md` with a "Theme tokens" section. It covers the token table in `wb-` names, the naming convention (`wb-` prefix, built-ins never overridden, `wb-<domain>-<variant>-<role>` with roles bg/ink/border), the `Record<Enum,string>` variant maps in `src/theme/variants.ts`, type roles, the spacing note, dark mode, the focus ring, the motion policy (no CSS transitions, Framer Motion for lift and press), CSS Module usage, and the single bundle. Copy `design-tokens.json` next to it. Verify: every token name in the skill exists in `index.css`.

## Tests

- [ ] Contrast check, light and dark, for every token text/background pair: ink and ink-muted on surface-page and surface-card; on-primary on primary; on-success on success; on-secondary on secondary; highlight on surface-card; danger on surface-page and danger-soft; vocab, grammar and phrase ink on their backgrounds; `wb-share-*-ink` on `wb-share-*-bg` (4 pairs); `wb-level-*-ink` on `wb-level-*-bg`; border-control and focus-ring against surface-card. Verify: text pairs are at least 4.5:1 and borders and rings at least 3:1, with results recorded in `test-report.md`.
- [ ] Type check for the variant maps. Verify: `npm run build` passes, and temporarily deleting one key from `shareStatusClasses` makes `tsc` fail (note this in `test-report.md`, then revert).
- [ ] Run the full `e2e/ui` suite. Verify: every scenario passes, and any class-dependent selector is replaced with a role or `data-testid`.
- [ ] Add a Gherkin dark-mode scenario covering both `data-theme="dark"` and `colorScheme: 'dark'` emulation. Verify: the Login body background computes to the dark `wb-surface-page` (`#0f172a`).
- [ ] Add a Gherkin keyboard-focus scenario that tabs to the Login primary button. Verify: the computed `box-shadow` is not `none`.
- [ ] Add a Gherkin no-CSS-transition scenario on Lessons and Login. Verify: every button, link and card has a computed `transition-duration` of `0s`.
- [ ] Manual Child-account check in light and dark. Verify: text sizes and tap targets are unchanged from `main`, level badges and share chips are readable, and the results are noted in `test-report.md`.
- [ ] Copy the bundle measurements (baseline, after `cssCodeSplit`, after migration) into `test-report.md`. Verify: all three rows are present.
- [ ] Create `docs/features/ui-theme.md` from `_template.md`. It covers the tokens, the `wb-` naming convention and `wb-<domain>-<variant>-<role>` variant naming, the variant maps, dark-mode precedence, the focus ring, the motion policy, the single CSS bundle, CSS Module usage, and child/adult behaviour. Verify: the file exists and includes the naming convention.

## Measurements (filled in by coder)

| Point | CSS files | Raw | Gzip |
| --- | --- | --- | --- |
| Baseline | 1 (`index-*.css`) | 21.44 kB | 4.65 kB |
| After `cssCodeSplit: false` | 1 (`style-*.css`) | 21.45 kB | 4.67 kB |
| After migration | | | |

## Gaps (filled in by coder if unmapped colours are found, or with Sam's violet decision)
- `layouts/PublicLayout.tsx`: page gradient `from-sky-100 via-white to-amber-100` has no token (left as-is); card `shadow-xl` has no wb- shadow (left as built-in).
- `layouts/AppLayout.tsx`: nav item `hover:bg-sky-100` (generic hover tint, not a vocab card) has no token (left as-is); active nav plain `shadow` left as built-in.
- `pages/LessonsPage.tsx`: inactive type-tab `hover:bg-sky-100` has no token (left as-is); active tab plain `shadow` left as built-in.
- `components/lessons/GrammarRuleList.tsx`: rule card `bg-emerald-50` has no token (`wb-grammar-bg` is emerald-100 and is already used by the example chips inside the card) — left as-is.
- `pages/LessonDetailPage.tsx` (+ Builder/Check/Moderation): `hover:bg-emerald-600` on success buttons dropped — there is no `wb-success-hover` token, so success buttons have no hover colour now.
- `pages/ProgressPage.tsx`: score / "N known" chips `bg-emerald-100 text-emerald-800` are a success-soft chip; there is no `wb-success-soft`/`wb-success-ink` token (wb-grammar-* has the same hex but a different role) — left as-is. Buttons keep plain `shadow` (built-in).
