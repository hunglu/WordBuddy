# Test report: WB-55_Apply paging to table/list

Run 2 (services up). Verdict: all green, approved.

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

These ran in run 1 (commit 8aef46e). No application code changed since, so they were not repeated.

## E2E API

85 passed, 0 failed (`dotnet test e2e/api/WordBuddy.E2E.Api.Tests`, 2m38s). Includes new `PagingTests`.
The stack serves WB-55 code: `groups/owned`, `groups/joined`, `lessons/admin`, `vocabulary/shared`, `progress/challenges` answer 401 (route exists, not 404).

## E2E UI

53 passed, 0 failed (`npm test` in `e2e/ui`). Includes new `paging.feature`.

## Failures

One test-code fix, no app bug. `lesson-authoring.feature` "Admin creates a draft lesson..." failed on the first run: the admin lesson list is now paged and sorted by title, so the new lesson was off page 1. The step now clicks `Next` until the row shows (`e2e/ui/steps/lesson-authoring.steps.ts`). Re-run: 53/53.

## Merge guard

| Guard | Result |
|---|---|
| 1. `review.md` Approve, sha `b70f37e` is an ancestor of HEAD | Pass |
| 2. Only allowlisted paths changed since review | Pass. Changed: proposal/review/test-report in this plan folder, `docs/ai/learnings.md`, `e2e/api/.../{EditVocabularyWord,LearningGroup,Paging,PersonalContext,PersonalVocabulary,VocabularyAutofill,VocabularyChallenge}Tests.cs`, `e2e/ui/features/paging.feature`, `e2e/ui/steps/{paging,support-links,vocabulary-sharing}.steps.ts` (plus `lesson-authoring.steps.ts` and `docs/features/**` from this run, both allowlisted) |
| 3. Every suite ran with zero failures | Pass |

## Verdict

Approved and merged into main
