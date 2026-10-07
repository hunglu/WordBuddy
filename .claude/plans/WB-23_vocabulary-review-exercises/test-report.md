# Test report: WB-23_Vocabulary review exercises

Run 2 (after needs-fixes). Stack up via docker compose; Content `AddSenseImage` already applied (`/health/ready` 200, `senses?ids=` E2E passes).

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Content UnitTests | 123 passed, 0 failed |
| Content IntegrationTests | 77 passed, 0 failed |
| Progress UnitTests | 103 passed, 0 failed |
| Progress IntegrationTests | 26 passed, 0 failed |

## E2E API

`WordBuddy.E2E.Api.Tests` — 32 passed, 0 failed (includes `VocabularyReviewSessionTests`).

## E2E UI

21 passed, 0 failed (includes `vocabulary-review.feature` child + adult, `vocabulary.feature`). Child time-cap scenario passed with no token-expiry logout.

First run: 20 passed, 1 failed — `navigation.feature`, `/vocabulary/check` expected "My Vocabulary" selected. The plan redirects `/vocabulary/check` → `/vocabulary/review` and adds a "Review" sidebar item, so the test was stale. Fixed in the test: `/vocabulary/check` → "Review", plus a new `/vocabulary/review` → "Review" row. No app code changed.

## Failures

None.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, `0c3ca1f` ancestor of HEAD | Pass |
| 2. Changes since review on allowlist | Pass — `proposal.md`, `review.md`, `test-report.md`, `Content.IntegrationTests/PersonalVocabularyEndpointsTests.cs`; this run adds `e2e/ui/features/navigation.feature`, `docs/features/vocabulary-builder.md` |
| 3. All suites ran, zero failures | Pass |

## Follow-up (app code, not fixed here)

- Review nit 3: `ListeningChoiceExercise.tsx:39` — add `aria-label` to the Play button.
- Review nit 4: `VocabularyReviewPage.tsx:99-101` — queue not rebuilt after a `senses` refetch.

## Verdict

Approved — merged into main
