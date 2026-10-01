# Review: 2 navigation Items have selected state

PR: #7 · Round 2 · Reviewed commit: 0b07bdb · 2026-10-01T17:17:39+07:00

## Verdict
Approve. All round-1 majors are fixed. No blockers or majors remain.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | WordBuddy.UI/src/layouts/AppLayout.tsx:25,39 | My Vocabulary sets `end: true`, but `isItemActive` ignores `end` whenever `activePaths` is set. The field is dead config for that item, and a later edit to it would silently do nothing. | Leave as is, or document on `NavItem.end` that it is ignored when `activePaths` is set. |
| 2 | nit | e2e/ui (tsc) | Carried over from round 1 #4: `@types/node` is missing. This is pre-existing and was deferred on purpose. | Fix in a separate change. |

Round-1 verification:
- R1 #1 (README): `git diff origin/main -- README README.md` is empty. Root README is no longer in the PR. Resolved.
- R1 #2 (aria-current): `isItemActive` computes `active` once per item. It drives both the class and `aria-current`. Switching from NavLink to Link is sound, because NavLink would set its own `aria-current` from its `end` match. Behaviour checked against NavLink:
  - Rendered element: Link still renders `<a href>`, so focus, Tab order, Enter activation and the focus ring are unchanged.
  - Prefix matching: `matchPath` with `end: false` keeps Lessons and My Progress active on sub-routes such as `/lessons/42`, and still respects segment boundaries.
  - Exact matches: Home stays exact.
  - Trailing slash and case: `/vocabulary/` matches, and matching is case-insensitive like NavLink's default.
  - Basename: `useLocation().pathname` is already basename-relative.
  - Lost features: only NavLink's unused `pending`/`transitioning` class states.
  - Plan rules: My Vocabulary is active on `/vocabulary` and `/vocabulary/check`; Shared Pool and Moderation are exact. This matches the plan. Resolved.
- R1 #3 (e2e): the step asserts that the expected link has `aria-current="page"` and that exactly one `aside a[aria-current="page"]` exists. Resolved.
- R1 #4: deferred, see nit #2.

## Plan conformance
All 6 tasks are covered. The plan said to keep NavLink's `isActive` as a fallback. The code uses one `matchPath`-based helper for every item instead. This is an intentional deviation that fixes R1 #2, and the behaviour is the same. The README change is gone, so nothing out of scope remains apart from the early living-spec stub noted in round 1.

## Previous rounds

## Round 1
PR: #7 · Round 1 · Reviewed commit: 1d9a761 · 2026-10-01T17:07:42+07:00

### Verdict
Changes requested. The fix is correct, but two majors remain: an unrelated README edit is in the PR, and `aria-current` is missing on `/vocabulary/check`.

### Findings
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

### Plan conformance
All 6 tasks in `tasks.md` are covered by the diff. Out of scope: the root `README` change (finding #1). The `docs/features/app-navigation.md` stub and the `docs/features/README.md` row are an early part of the living-spec work the plan gives to `/test`. They are harmless and marked `proposed`.
