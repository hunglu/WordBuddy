# Test report: WB-56 Moderator role to assist admin

Run 1 · branch `feature/WB-56_moderator-role-to-assist-admin` (head a1ba585 + `origin/main` already merged) · stack up, Identity migration `AddUserIsModerator` applied (seeded `moderator@wordbuddy.com` logs in with `isModerator = true`).

## Backend unit/integration

| Suite | Passed | Failed |
| --- | --- | --- |
| Identity unit | 213 | 0 |
| Identity integration | 43 | 0 |
| Content unit | 445 | 0 |
| Content integration | 216 | 0 |

## E2E API

89 passed, 0 failed (`e2e/api/WordBuddy.E2E.Api.Tests`). New: `ModeratorRoleTests` (4 tests, fresh accounts, role revoked in `finally`):

- Grant → old token still 403 → re-login carries `isModerator` → moderate another's word (204), edit lexeme (204), lessons open, `republish` 403, `admin/users` 403, role change 403.
- Moderator moderates own word → 403; admin moderates the same word → 204.
- Grant on a child → 400.
- `admin/users`: adults only (child not listed), learner caller → 403.

## E2E UI

56 passed, 0 failed (`e2e/ui`). New: `moderator-role.feature` (3 scenarios):

- Admin grants then revokes Moderator on `/admin/users` (badge appears, then goes).
- Moderator sees Moderation, Lesson authoring, Lexemes, Support links; no "Users & roles"; `/admin/users` redirects home.
- Learner sees no admin menu.

## Failures

First UI run: 1 failure, `vocabulary-challenges` "Adult joins the Weekly League" (`the board lists them as "you"`). Not caused by WB-56. The board is paged (WB-55) and the shared test DB now holds 27 learners, so a 0-point learner is on page 2. Fix is test-only: the step now follows "Next" until the row shows (`e2e/ui/steps/vocabulary-challenges.steps.ts`). Re-run: all 56 pass.

No application code changed.

## Merge guard

1. `review.md` Approve at fab0b7b; fab0b7b is an ancestor of HEAD: yes.
2. `git merge-tree --write-tree fab0b7b origin/main` clean. Paths changed since review vs "reviewed code + main":
   - `.claude/plans/WB-56_moderator-role-to-assist-admin/proposal.md`
   - `.claude/plans/WB-56_moderator-role-to-assist-admin/review.md`

   Both are on the allowlist.
3. All 6 suites ran in this session, zero failures: yes.

## Verdict

Approved — merged into main
