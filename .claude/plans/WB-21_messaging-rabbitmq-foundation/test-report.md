# Test report: WB-21 Messaging RabbitMQ foundation

Branch `feature/WB-21_messaging-rabbitmq-foundation`, `origin/main` merged (already up to date). Run 2026-10-05.

## Backend unit/integration

| Suite | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| Content.UnitTests | 104 | 0 | 0 |
| Content.IntegrationTests (LocalDB, in-memory MassTransit) | 70 | 0 | 0 |
| Progress.UnitTests | 22 | 0 | 0 |
| Progress.IntegrationTests (LocalDB, test harness) | 7 | 0 | 0 |

## E2E API

| Tests | Result |
| --- | --- |
| Existing 12 (health, vocabulary, internal exposure) | 12 passed, against the running compose images built **before** WB-21 |
| New `LearnerWordMessagingTests` (3: add adult, add child, delete → inactive) | **Skipped** — stack runs without RabbitMQ |

Why the new tests did not run:

- Running containers are pre-WB-21 images; no `rabbitmq` container.
- Rebuild needs Shared `1.1.0` on GitHub Packages (`make publish-shared`, ask-gated), RabbitMQ creds in `.env` / `k8s/secret.yaml`, and the two migrations (ask-gated).
- To run: rebuild stack, apply migrations, set `E2E_PROGRESS_DB` to the Progress DB connection string, then `dotnet test e2e/api/WordBuddy.E2E.Api.Tests`. Kind: also set `Services__*`.

## E2E UI

Not applicable — no UI change.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, `530d816` ancestor of HEAD | Pass |
| 2. Changes since review (vs `530d816` + `origin/main`) | Pass — only `proposal.md`, `review.md` |
| 3. All suites ran, zero failures | **Fail** — WB-21 E2E skipped |

## Failures

None. One suite skipped (see E2E API).

## Verdict

Not merged — WB-21 E2E API tests skipped (RabbitMQ stack not running); re-run `/test` once the stack is rebuilt.
