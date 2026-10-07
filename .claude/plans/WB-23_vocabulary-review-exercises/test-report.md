# Test report: WB-23_Vocabulary review exercises

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Content UnitTests | 123 passed, 0 failed |
| Content IntegrationTests | 77 passed, 0 failed (1 new) |
| Progress UnitTests | 103 passed, 0 failed |
| Progress IntegrationTests | 26 passed, 0 failed |

New: `GetSensesByIds_SenseWithAudioAndImage_ReturnsBothUrls` (review nit 2). `audioUrl` and `imageUrl` both come back from `senses?ids=`.

## E2E API

Skipped — services not running. Starting the stack (`make up`) was denied by the permission gate.

## E2E UI

Skipped — services not running (same reason).

## Failures

- No test failures.
- Not run: `e2e/api` `VocabularyReviewSessionTests`, `e2e/ui` `vocabulary-review.feature` (child + adult). They need `make up` and the Content migration `AddSenseImage`. Watch the child time-cap scenario for token-expiry logout.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, `0c3ca1f` ancestor of HEAD | Pass |
| 2. Changes since review on allowlist | Pass — `proposal.md`, `review.md` (plus this run's test file and report) |
| 3. All suites ran, zero failures | Fail — E2E API and E2E UI skipped |

## Follow-up (app code, not fixed here)

- Review nit 3: `ListeningChoiceExercise.tsx:39` — add `aria-label` to the Play button.
- Review nit 4: `VocabularyReviewPage.tsx:99-101` — queue not rebuilt after a `senses` refetch.

## Verdict

Not merged — E2E API and E2E UI skipped (services not running). Run `make up` + `AddSenseImage` migration, then re-run `/test`.
