# Test report: WB-21 Messaging RabbitMQ foundation

Branch `feature/WB-21_messaging-rabbitmq-foundation`, `origin/main` merged (already up to date). Run 2026-10-06, round 2.

## Backend unit/integration

| Suite | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| Content.UnitTests | 104 | 0 | 0 |
| Content.IntegrationTests (LocalDB, in-memory MassTransit) | 70 | 0 | 0 |
| Progress.UnitTests | 22 | 0 | 0 |
| Progress.IntegrationTests (LocalDB, test harness) | 7 | 0 | 0 |

## E2E API

15 passed, 0 failed, 0 skipped — against the docker compose stack rebuilt from this branch (RabbitMQ up).

| Tests | Result |
| --- | --- |
| Existing 12 (health, vocabulary, internal exposure) | 12 passed |
| `LearnerWordMessagingTests` (add adult, add child, delete → inactive) | 3 passed |

Test change this round: `LearnerWordMessagingTests` reads the Progress DB via `E2E_PROGRESS_DB` (kind/CI) or, when unset, via `sqlcmd` inside the compose `sqlserver` container (`WordBuddyProgress.LearnerWordMemberships`). The SA password expands inside the container only. Skips when neither path is available.

## E2E UI

Not applicable — no UI change.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, `530d816` ancestor of HEAD | Pass |
| 2. Changes since review (vs `530d816` + `origin/main`) | Pass — `proposal.md`, `review.md`, `test-report.md`, `e2e/api/.../LearnerWordMessagingTests.cs`, `e2e/api/.../WordBuddy.E2E.Api.Tests.csproj` |
| 3. All suites ran, zero failures | Pass |

## Failures

None.

## Verdict

Approved — merged into main
