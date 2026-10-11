# Test report: WB-29 Vocabulary challenges

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Identity UnitTests | 128 passed, 0 failed |
| Identity IntegrationTests | 16 passed, 0 failed |
| Progress UnitTests | 395 passed, 0 failed |
| Progress IntegrationTests | first run 83 passed, **1 failed**; 3 reruns 84/84; isolated rerun of the failed test passed |

Branch state: `feature/WB-29_vocabulary-challenges` at be92d42, `origin/main` already merged (up to date).

## E2E API

Skipped — services not running (only `wordbuddy-control-plane` is up; `docker compose up` is ask-gated and was not started).
Tests exist but never ran: `e2e/api/WordBuddy.E2E.Api.Tests/VocabularyChallengeTests.cs`.

## E2E UI

Skipped — services not running (same reason).
Scenarios exist but never ran: `e2e/ui/features/vocabulary-challenges.feature`.

## Failures

| Test | Detail |
| --- | --- |
| `ChallengeEndpointsTests.Challenges_ParallelJoinsAroundCapacity_AdmitExactlyCapacityAndNeverFail` | Failed once in the first full Progress integration run (1 s). Passed in 4 later runs. Looks flaky (timing/concurrency). Assertion text was not captured. The join path is the code from review finding 2 (`UPDLOCK, HOLDLOCK`), so a real race is possible. Needs a look by `/code`. |

## Merge guard

1. `review.md` Approve, reviewed commit cf09edc is an ancestor of HEAD: pass.
2. Changed since review (vs reviewed code + main): `proposal.md`, `review.md` only, both allowlisted: pass.
3. Every suite ran with zero failures: **fail** (E2E API and UI skipped; one integration failure on first run).

## Verdict

Not merged — E2E API and E2E UI skipped (services not running), and one Progress integration test failed on its first run (flaky?). Start `docker compose up`, then re-run `/test`; `/code` should check the parallel-join test for a race.
