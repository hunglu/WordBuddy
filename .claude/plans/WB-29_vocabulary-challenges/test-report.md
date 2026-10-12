# Test report: WB-29 Vocabulary challenges

Run 2. Branch `feature/WB-29_vocabulary-challenges` at 9c89edf; `origin/main` merged (already up to date).

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Identity UnitTests | 128 passed, 0 failed |
| Identity IntegrationTests | 16 passed, 0 failed |
| Progress UnitTests | 395 passed, 0 failed |
| Progress IntegrationTests | 84 passed, 0 failed |
| `Challenges_ParallelJoinsAroundCapacity_AdmitExactlyCapacityAndNeverFail` x20 | 20 passed, 0 failed |

## E2E API

Stack was up (docker compose). 55 passed, 0 failed (includes `VocabularyChallengeTests`: 3 passed).

## E2E UI

36 scenarios: 35 passed, 1 failed. `vocabulary-challenges.feature`: 2 of 2 passed.

## Failures

| Test | Detail |
| --- | --- |
| `support-links.feature` > Admin completes an escalated unlink request | `escalate` returned 400 `SupportLink.EscalationTooEarly`. The step needs the stack to run with `SupportLinks__UnlinkOverrideWaitDays=0`; the running containers do not have it. Environment setup, not a WB-29 code fault (feature is not part of WB-29). Not fixed: restarting compose is ask-gated. |

## Merge guard

1. `review.md` Approve, reviewed commit 67a0e81 is an ancestor of HEAD: pass.
2. Changed since review (vs reviewed code + main): `proposal.md`, `review.md` only, both allowlisted: pass.
3. Every suite ran with zero failures: **fail** (one E2E UI scenario failed).

## Verdict

Not merged — E2E UI: 1 failed scenario (support-links, needs `SupportLinks__UnlinkOverrideWaitDays=0` on the running stack). All WB-29 tests pass. Restart the stack with that setting and re-run `/test`.
