# Test report: WB-55_Apply paging to table/list

## Backend unit/integration

| Suite | Result |
|---|---|
| Content UnitTests | 443 passed, 0 failed |
| Content IntegrationTests | 202 passed, 0 failed |
| Identity UnitTests | 191 passed, 0 failed |
| Identity IntegrationTests | 33 passed, 0 failed |
| Progress UnitTests | 425 passed, 0 failed |
| Progress IntegrationTests | 96 passed, 0 failed |
| UI `npm run lint` | 0 errors (3 old warnings) |
| UI `npm run build` | passes |

## E2E API

Skipped — services not running (ports 5080-5083 refused). Starting `docker compose` / kind needs `make publish-shared` (Kernel 1.1.0), which is Sam's to run.

Written this run, compiled only (`dotnet build` OK), not executed:

- Updated to the `PagedResult` shape and new group routes: `EditVocabularyWordTests`, `LearningGroupTests`, `PersonalContextTests`, `PersonalVocabularyTests`, `VocabularyAutofillTests`, `VocabularyChallengeTests`. `LessonAuthoringTests` needed no change (`/lessons/admin/senses` is not paged).
- New `PagingTests`: page 2, page past the end, defaults, 400 on `page=0` / `pageSize=0` / `pageSize=101`, admin lessons (admin vs adult), shared pool search and child vs adult, groups `joined`/`approvals`/old route gone, child `owned`, challenges.
- `VocabularyChallengeTests`: child board is one page, and `page=2&pageSize=1` still returns page 1.

## E2E UI

Skipped — services not running (UI `:3000` refused).

Written this run, type-checked and generated only (`tsc`, `bddgen`), not executed:

- `support-links.steps.ts`, `vocabulary-sharing.steps.ts` read `items` from the paged response.
- New `features/paging.feature` + `steps/paging.steps.ts`: my words pager, page 1 then page 2.
- `learning-groups.steps.ts` needs no change: its 502 mock `**/api/auth/groups**` also matches the new routes.

## Failures

None in the suites that ran. Not run: E2E API, E2E UI.

## Merge guard

| Guard | Result |
|---|---|
| 1. `review.md` Approve, sha `b70f37e` is an ancestor of HEAD | Pass |
| 2. Only allowlisted paths changed since review (`merge-tree` vs `origin/main`) | Pass. Changed: `.claude/plans/WB-55_apply-paging-to-table-list/proposal.md`, `.claude/plans/WB-55_apply-paging-to-table-list/review.md`, `docs/ai/learnings.md` (plus this run's `e2e/**` and report) |
| 3. Every suite ran with zero failures | **Fail** — E2E API and E2E UI skipped |

## Verdict

Not merged — E2E API and E2E UI suites were skipped (services not running). Living spec and README row are left for the approved run. Next: Sam runs `make publish-shared` and starts the stack, then re-runs `/test`.
