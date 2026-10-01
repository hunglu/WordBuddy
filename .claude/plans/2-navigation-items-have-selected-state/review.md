# Review: 2 navigation Items have selected state

PR: #7 · Round 1 · Reviewed commit: 1d9a761 · 2026-10-01T17:07:42+07:00

## Verdict
Changes requested. The fix is correct, but two majors remain: an unrelated README edit is in the PR, and `aria-current` is missing on `/vocabulary/check`.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | README:1-3 | This changes the repo-root README heading. That is unrelated to bugfix #5. It was a local edit on main that ended up in commit 2dd8cea. It adds unreviewed noise to a bugfix PR, and history will credit it to this change. | Revert the README hunk on `feature/<slug>` through `/code`. Ship it in its own PR or docs commit. |
| 2 | major | WordBuddy.UI/src/layouts/AppLayout.tsx:50-62 | The highlight is computed from `activePaths`, but NavLink still sets `aria-current` from its own `isActive` (with `end: true`). On `/vocabulary/check`, My Vocabulary looks selected but has no `aria-current="page"`, so screen-reader users get a different answer than sighted users. | Compute `active` once per item, outside `className`. Use it for the class and pass `aria-current={active ? 'page' : undefined}` to the NavLink. A small `isItemActive(item, pathname, isActive)` helper keeps the logic in one place. |
| 3 | nit | e2e/ui/steps/navigation.steps.ts:6-20 | The test detects "selected" through the `bg-wb-primary` styling class, so a styling change would break it. | After #2 is fixed, assert `aria-current="page"` instead. That also covers the a11y behaviour. |
| 4 | nit | e2e/ui (tsc) | `tsc --noEmit` fails because `@types/node` is missing. I verified this is pre-existing: `e2e/ui/package.json` on main has no `@types/node`, and `login.steps.ts` / `vocabulary.steps.ts` on main already use `process.env`. This PR does not cause it. | Add `@types/node` to e2e/ui devDependencies in a separate change. |

Notes:
- The E2E scenarios have not been run yet. That is expected, because `/test` runs them. The scenarios match the success criteria, including the admin Moderation case.
- Correctness check: `matchPath({ path, end: true })` is exact apart from a trailing slash, so `/vocabulary/` still highlights My Vocabulary. Lessons and Progress keep `end: false` for sub-routes, as planned. Shared Pool and Moderation are now exact matches.
- Security: the admin defaults (`admin@wordbuddy.com` / `Admin@123`) are the documented Identity dev seed and can be overridden through env vars, following the existing `login.steps.ts` pattern. Acceptable for a dev seed. Child and adult paths are identical, so no extra coverage is needed.
- Conventions: typed `NavItem`, no `any`/`enum`, only `wb-` tokens, no new state, and TanStack Query is not involved. All OK.

## Plan conformance
All 6 tasks in `tasks.md` are covered by the diff. Out of scope: the root `README` change (finding #1). The `docs/features/app-navigation.md` stub and the `docs/features/README.md` row are an early part of the living-spec work the plan gives to `/test`. They are harmless and marked `proposed`.
