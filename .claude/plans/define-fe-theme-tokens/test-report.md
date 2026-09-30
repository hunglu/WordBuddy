# Test report: Define FE theme tokens

Re-run 2026-10-01 on `feature/define-fe-theme-tokens` (7158b66, already up to date with `origin/main`). The first run was on b1ce1cb. The contrast, type-check and bundle sections below come from that first run, and the frontend code has not changed since.

## Backend unit/integration

Since the first run, the only backend change is the dev-only Identity seeder (7158b66).

`dotnet test src/Services/Identity/WordBuddy.Identity.slnx`: the build passes, and 0 tests were run. Neither `WordBuddy.Identity.UnitTests` nor `WordBuddy.Identity.IntegrationTests` contains any tests yet ("No test is available"). So this run proves that the service compiles, and nothing more. The seeder itself is checked end to end: the E2E login with the seeded account passes (see below).

## E2E API

Not applicable. No API surface changed.

## Build / type check (WordBuddy.UI)

- `npm run build`: pass. `npm run lint` (oxlint): pass, with 1 existing warning (RegisterPage `incompatible-library`, not caused by this change).
- Variant-map type check: I deleted `Rejected` from `shareStatusClasses`, and `tsc -b` failed with
  `TS2741: Property 'Rejected' is missing ... Record<VocabularyShareStatus, string>`. I then reverted the file with `git checkout`, so the working tree is clean.
- Light values in `src/index.css` match `design-tokens.json` for all 61 colour tokens.

## Contrast (design-tokens.json v3, WCAG 2.x)

Result: 39 pairs checked, 0 failures. Text pairs need at least 4.5:1, and borders and rings need at least 3:1.

| Foreground | Background | Light | Dark | Min | Result |
|---|---|---|---|---|---|
| wb-ink | wb-surface-page | 8.87 | 15.56 | 4.5 | pass |
| wb-ink | wb-surface-card | 9.46 | 12.75 | 4.5 | pass |
| wb-ink-muted | wb-surface-page | 5.57 | 10.71 | 4.5 | pass |
| wb-ink-muted | wb-surface-card | 5.93 | 8.77 | 4.5 | pass |
| wb-on-primary | wb-primary | 5.93 | 8.33 | 4.5 | pass |
| wb-on-primary | wb-primary-hover | 7.56 | 10.71 | 4.5 | pass |
| wb-on-success | wb-success | 5.48 | 9.29 | 4.5 | pass |
| wb-on-success | wb-success-hover | 7.68 | 11.71 | 4.5 | pass |
| wb-on-secondary | wb-secondary | 5.02 | 10.69 | 4.5 | pass |
| wb-on-secondary | wb-secondary-hover | 7.09 | 12.38 | 4.5 | pass |
| wb-highlight | wb-surface-card | 5.02 | 8.76 | 4.5 | pass |
| wb-highlight | wb-surface-page | 4.71 | 10.69 | 4.5 | pass |
| wb-danger | wb-surface-page | 5.90 | 6.63 | 4.5 | pass |
| wb-danger | wb-surface-card | 6.29 | 5.44 | 4.5 | pass |
| wb-danger | wb-danger-soft | 5.24 | 5.81 | 4.5 | pass |
| wb-vocab-ink | wb-vocab-bg | 6.59 | 10.46 | 4.5 | pass |
| wb-grammar-ink | wb-grammar-bg | 6.78 | 11.81 | 4.5 | pass |
| wb-grammar-ink | wb-grammar-surface | 7.29 | 13.47 | 4.5 | pass |
| wb-phrase-ink | wb-phrase-bg | 6.37 | 12.03 | 4.5 | pass |
| wb-share-private-ink | wb-share-private-bg | 6.59 | 10.46 | 4.5 | pass |
| wb-share-pending-ink | wb-share-pending-bg | 6.37 | 12.03 | 4.5 | pass |
| wb-share-shared-ink | wb-share-shared-bg | 6.78 | 11.81 | 4.5 | pass |
| wb-share-rejected-ink | wb-share-rejected-bg | 6.68 | 11.08 | 4.5 | pass |
| wb-level-beginner-ink | wb-level-beginner-bg | 6.78 | 11.81 | 4.5 | pass |
| wb-level-intermediate-ink | wb-level-intermediate-bg | 6.59 | 10.46 | 4.5 | pass |
| wb-level-advanced-ink | wb-level-advanced-bg | 7.57 | 10.97 | 4.5 | pass |
| wb-ink | wb-hover-tint | 8.24 | 8.24 | 4.5 | pass |
| wb-ink | wb-primary-soft | 8.24 | 12.10 | 4.5 | pass |
| wb-ink | wb-primary-soft-hover | 7.13 | 8.24 | 4.5 | pass |
| wb-success-soft-ink | wb-success-soft | 6.78 | 11.81 | 4.5 | pass |
| wb-success-soft-ink | wb-success-soft-hover | 5.99 | 7.58 | 4.5 | pass |
| wb-danger-soft-ink | wb-danger-soft | 6.68 | 11.08 | 4.5 | pass |
| wb-danger-soft-ink | wb-danger-soft-hover | 5.68 | 6.78 | 4.5 | pass |
| wb-ink | wb-auth-from | 8.24 | 15.56 | 4.5 | pass |
| wb-ink | wb-auth-via | 9.46 | 12.75 | 4.5 | pass |
| wb-ink | wb-auth-to | 8.49 | 13.05 | 4.5 | pass |
| wb-border-control | wb-surface-card | 4.10 | 3.07 | 3 | pass |
| wb-focus-ring | wb-surface-card | 4.10 | 6.83 | 3 | pass |
| wb-focus-ring | wb-surface-page | 3.84 | 8.33 | 3 | pass |

## E2E UI

I ran this against the Vite dev server at `http://localhost:5173` (`UI_BASE_URL`), which proxies to the running compose backends. Sam decided not to rebuild the `:3000` UI container. The seeded adult account `learner@example.com` was used.

| Scenario | Result |
| --- | --- |
| Login: Successful login | pass |
| Vocabulary Builder: add word + recall check | pass (after fixing 2 test-side defects, see Failures) |
| Theme: data-theme="dark" gives dark surface (#0f172a) | pass |
| Theme: `colorScheme: 'dark'` emulation gives dark surface | pass |
| Theme: keyboard Tab to Log in button gives non-`none` box-shadow | pass |
| Theme: Login has no CSS transition-duration | pass |
| Theme: Lessons has no CSS transition-duration | pass |

Final result: 7 passed, 0 failed. I ran the suite twice in a row, and both runs were 7/7 green.

## Child-account check

- **Automated, adult paths (done):** In `WordBuddy.UI/src` (excluding `index.css`), the removed and added lines of the diff against `main` contain exactly the same text-size, padding, width and height utilities (232 occurrences each side, no differences). `index.css` does not override any built-in `--text-*`/`--spacing` scale. So text sizes and tap targets are unchanged compared with `main`. The login, lessons and vocabulary pages were exercised as the adult account in the suite above.
- **Child-specific check (NOT covered):** Only an adult account is seeded, so nobody has looked at the screens as a Child account in light or dark mode, and the readability of level badges and share chips as a Child has not been checked by eye. The contrast table below covers those token pairs numerically.
- **Decision (Sam, 2026-10-01):** The missing Child-specific check is accepted and does not block the merge.

## Bundle measurements

| Point | CSS files | Raw | Gzip |
| --- | --- | --- | --- |
| Baseline | 1 (`index-*.css`) | 21.44 kB | 4.65 kB |
| After `cssCodeSplit: false` | 1 (`style-*.css`) | 21.45 kB | 4.67 kB |
| After migration | 1 (`style-*.css`) | 27.87 kB (+6.43 kB) | 5.70 kB (+1.05 kB) |

These numbers come from the coder, recorded in tasks.md. I rebuilt today and confirmed that exactly one CSS file is emitted (`style-*.css`).

## Failures

None in the final run. Two defects in the test code were fixed in `e2e/ui/steps/vocabulary.steps.ts`. No application code was changed.

1. `getByRole('button', { name: '5 words' })` also matched "15 words" (a strict-mode violation), so I added `exact: true`.
2. The recall-check loop clicked "I Know This" up to 5 times without waiting, and the card transition swallowed some of those clicks. The run stopped on "Word 3 of 3" while the app was still working. I replaced the loop with a `toPass` retry that clicks once and then waits for the next card or the result screen.

The earlier failures (missing test account) are fixed by the seeder in 7158b66.

## Verdict

Approved — merged into main. The Child-specific visual check is not covered; Sam accepted this as non-blocking.
