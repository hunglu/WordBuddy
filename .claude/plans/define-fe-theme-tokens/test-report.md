# Test report: Define FE theme tokens

Run 2026-10-01 on `feature/define-fe-theme-tokens` (b1ce1cb, already up to date with `origin/main`).

## Backend unit/integration

Not applicable. This change is frontend-only (WordBuddy.UI), and no backend service was touched.

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

I ran this against the Vite dev server at `http://localhost:5173` (`UI_BASE_URL`), which proxies to the running compose backends.
The compose UI container on `:3000` still serves an old build (`index-*.css` with no `wb-` tokens), and rebuilding it
needs `docker compose`, which is ask-gated, so I did not rebuild it.

| Scenario | Result |
| --- | --- |
| Login: Successful login | FAIL (environment) |
| Vocabulary Builder: add word + recall check | FAIL (environment) |
| Theme: data-theme="dark" gives dark surface (#0f172a) | pass |
| Theme: `colorScheme: 'dark'` emulation gives dark surface | pass |
| Theme: keyboard Tab to Log in button gives non-`none` box-shadow | pass |
| Theme: Login has no CSS transition-duration | pass |
| Theme: Lessons has no CSS transition-duration | FAIL (environment) |

Total: 4 passed, 3 failed. As a control, I also ran the theme scenarios against the old `:3000` build, and 4 of 5 failed there. So the new scenarios do detect the change.

## Child-account check

Not done. No Child test account is available, and a check "manual in light and dark" needs someone to look at the screens.
This is **not a pass**.

## Bundle measurements

| Point | CSS files | Raw | Gzip |
| --- | --- | --- | --- |
| Baseline | 1 (`index-*.css`) | 21.44 kB | 4.65 kB |
| After `cssCodeSplit: false` | 1 (`style-*.css`) | 21.45 kB | 4.67 kB |
| After migration | 1 (`style-*.css`) | 27.87 kB (+6.43 kB) | 5.70 kB (+1.05 kB) |

These numbers come from the coder, recorded in tasks.md. I rebuilt today and confirmed that exactly one CSS file is emitted (`style-*.css`).

## Failures

- All 3 failures have the same cause. The E2E test account `learner@example.com` (default `E2E_TEST_EMAIL`) does not exist in the running Identity service: `POST :5080/api/auth/login` returns 400, and the UI shows "Email or password is incorrect."
  The e2e/ui README lists this account as a precondition. No application defect was found.
- The Child-account check has not been done (see above).

## Verdict

Not merged. The E2E UI suite has 3 failures because the test account is missing, and the Child-account check is outstanding.
To finish: create the test account (or set `E2E_TEST_EMAIL`/`E2E_TEST_PASSWORD`), plus a Child account, then re-run `/test define-fe-theme-tokens`.
Ideally, also rebuild the `:3000` UI container first.
