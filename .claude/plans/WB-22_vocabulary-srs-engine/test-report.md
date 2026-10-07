# Test report: WB-22_Vocabulary SRS engine

Branch `feature/WB-22_vocabulary-srs-engine`, `origin/main` merged (already up to date).

## Backend unit/integration

| Suite | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| Progress.UnitTests | 103 | 0 | 0 |
| Progress.IntegrationTests | 26 | 0 | 0 |
| Content.UnitTests | 111 | 0 | 0 |
| Content.IntegrationTests | 73 | 0 | 0 |

## E2E API

`WordBuddy.E2E.Api.Tests` — 30 passed, 0 failed, 0 skipped. New: `VocabularySrsTests.cs` (14 cases).

| Scenario | Child | Adult |
| --- | --- | --- |
| Add word → New state in `/words` + new session item, cap 10 | yes | yes |
| Correct answer in 3500 ms → rating | Easy | Good |
| Settings PUT 5 → GET 5 | yes | yes |
| No token → 401; unknown sense → 404; `responseMs` 600001 → 400 | — | yes |
| Second attempt in same session does not reschedule (reps = 1) | — | yes |
| Bad `X-Client-CurrentDateTime` (format, +15:00, > 24 h) → 400 | — | yes |
| Cap 51 → 400; republish as non-admin → 403 | — | yes |

## E2E UI

Not applicable — no UI change in this plan (UI is WB-23).

## Merge guards

| Guard | Result |
| --- | --- |
| 1. Review round 2 = Approve; `a8c3eaf` ancestor of HEAD | pass |
| 2. Diff vs `merge-tree(a8c3eaf, origin/main)` | pass — only `proposal.md`, `review.md` (+ this run's allowlisted files) |
| 3. All suites ran, zero failures | pass |

## Failures

None.

## Verdict

Approved — merged into main
