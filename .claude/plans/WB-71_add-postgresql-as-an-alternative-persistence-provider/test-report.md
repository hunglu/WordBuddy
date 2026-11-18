# Test report: WB-71 Add PostgreSQL as an alternative persistence provider

Branch `feature/WB-71_add-postgresql-as-an-alternative-persistence-provider`, PR #72. Reviewed commit `d60e774`. `origin/main` merged: already up to date.

## Backend unit/integration

| Suite | SQL Server | Postgres (`wb-pg`, postgres:17) |
| --- | --- | --- |
| Identity unit | 253 pass | same code, provider-neutral |
| Content unit | 481 pass | same |
| Quiz unit | 17 pass | same |
| Progress unit | 472 pass | same |
| Identity integration | 56 pass | 56 pass |
| Content integration | 231 pass | 215 pass, 16 skipped (SQL Server-only migration replays) |
| Quiz integration | 6 pass | 6 pass |
| Progress integration | 101 pass (+1 new test: 102) | 101 pass (+1 new test: 102) |
| Aspire `AppHostModelTests` | 26 pass | n/a |

Counts for Progress above were taken before the new test. After adding it, `LearnerWordConsumerTests` ran 9/9 green on both providers (3 repeats on Postgres).

Test changes made in `/test`:

- Review nit 2: Content and Identity `ProviderParityTests` now assert the exception names the expected unique index (`UX_Lexemes_NormalizedLemma_PartOfSpeech`, `UX_Senses_ContentHash_OwnerUserId_Learner`, `IX_UnlinkRequests_LinkId_Open`). Green on both providers.
- Review nit 1: new `LearnerWordAddedConsumer_ConcurrentDuplicateEvents_OneRowAndNoFault` (6 concurrent events, distinct message ids). One membership row, no `Fault<LearnerWordAdded>` on both providers. The Postgres retry path works.
- `e2e/api` `LearnerWordMessagingTests`: `ProgressDb` now detects SQL Server (`sqlcmd`) or Postgres (`psql` in the compose `postgres` container). Builds; the Postgres path is not yet run.

## E2E API

- SQL Server stack (`docker compose up --build -d`): 93 passed, 0 failed, 0 skipped (includes `LearnerWordMessagingTests`).
- Postgres stack: **Skipped** — see Failures.

## E2E UI

Skipped — needs the Postgres stack, and browsers are not confirmed installed.

## Other tasks

| Task | Result |
| --- | --- |
| 2 GB check (`docker stats` on `make up-postgres`) | Skipped — Postgres stack not started |
| Aspire `aspire run` default and `db-provider=Postgres` | Skipped — compose stack is running on the same ports. Only the 26 `AppHostModelTests` ran |

## Failures

None in the suites that ran. These could not run:

1. `make up-postgres` blocked. Reading `WordBuddy/.env` (needed to check the `*_PG_CONNECTION_STRING` and `POSTGRES_PASSWORD` values) was denied. `docker-compose.postgres.yml` also publishes `127.0.0.1:5432`, which clashes with the standalone `wb-pg` container. Neither was changed without Sam.
2. The SQL Server compose stack is still running (`down` is ask-gated). It must go down before the Postgres stack or Aspire runs.
3. Browsers for `e2e/ui` not confirmed installed (Sam runs `npx playwright install` once).

## Merge guard

| Guard | Result |
| --- | --- |
| 1. `review.md` Approve; `d60e774` ancestor of HEAD | Pass |
| 2. Changed since review vs "reviewed code + main" | Only `proposal.md`, `review.md` (allowlisted) |
| 3. Every suite ran with zero failures | **Fail** — Postgres e2e api, e2e ui, 2 GB check, Aspire run skipped |

Other: `tasks.md:101` still says `WordBuddy/CLAUDE.md` is not edited (review nit 3). `tasks.md` is not on the tester allowlist; `/code` should fix it. The living spec was not reviewed or marked shipped, because the PR is not merged.

## Verdict

Not merged — Postgres-stack suites (e2e api, e2e ui, 2 GB check) and the Aspire run were not executed. Next: Sam frees port 5432 (or approves stopping `wb-pg`), runs `make down`, then `make up-postgres`, then re-run `/test`.

## Round 2 — 2026-11-18

**Verdict: needs fixes. Not merged.** One Progress integration test fails on Postgres every time. The tester stalled (watchdog) after finding it; the main session confirmed the failure and wrote this section.

| Check | Result |
| --- | --- |
| Progress integration, Postgres (compose `postgres`) | 101 pass, **1 fail** (102) |
| Same test alone, compose `postgres` | fails 3/3 |
| Same test alone, fresh `postgres:17-alpine` (throwaway, port 55432) | fails 2/2 |
| Same test at reviewed commit `d60e774`, fresh Postgres | fails 2/2 alone; full suite 100/101, same single failure |
| Same test alone, SQL Server (LocalDB) | pass |

Failing test: `Progress.IntegrationTests.Messaging.SupportLinkQueueTests.SupportLinkConsumers_Consume_FromProgressPrefixedQueues`

```
Expected harness.Consumed.Any<SupportLinkActivated>(LinkId == linkId) to be True, but found False.  (≈200 ms)
```

- No consumer exception inside the test window. The consumers run the EF inbox query (`SELECT *, xmin FROM "InboxState" ... FOR UPDATE`) but the harness reports nothing consumed before it goes inactive.
- The `3D000 database does not exist` errors in the log come later: consumers still retry after the factory dropped the test database. They are a symptom, not the cause.
- Not caused by round-1 test changes (fails at `d60e774`). Earlier runs reported this suite green on `wb-pg`; that could not be reproduced now. Cause is open → `/code`.

Not run this round (blocked by the failure; rerun after the fix):

- `e2e/api` on the Postgres stack, `e2e/ui` smoke (adult + child), 2 GB `docker stats` check, `aspire run` (both modes).

Environment observations:

- `docker compose ... --build` fails on this machine (Compose v5.6.1 bake: `additional privileges requested: pass "--allow=fs.read=src\Services\<X>\Dockerfile"`), also for the base file. `make up` / `make up-postgres` are affected. The Postgres stack was started with `up -d --no-build` using images built from this branch; all 5 APIs `/health/ready` = 200.
- Postgres compose stack left running; `wb-pg` stopped (not removed).

## Round 3 — 2026-11-18

**Verdict: Approved and merged into main.** Reviewed commit `d431d55` (round 2 approve) is an ancestor of HEAD. `origin/main` merged: already up to date.

Test changes in `/test` (round-2 review nits, test code only):

- Nit 1: `LearnerWordConsumerTests` concurrent duplicate test now polls for a `Fault<LearnerWordAdded>` for 8 s (retry budget 6.2 s), then asserts none.
- Nit 2: `SupportLinkQueueTests` also asserts neither consume has an `Exception`.

### Backend unit/integration

| Suite | SQL Server (LocalDB) | Postgres (compose `postgres:17`) |
| --- | --- | --- |
| Identity unit | 253 pass | provider-neutral |
| Content unit | 481 pass | provider-neutral |
| Quiz unit | 17 pass | provider-neutral |
| Progress unit | 472 pass | provider-neutral |
| Identity integration | 56 pass | 56 pass |
| Content integration | 231 pass | 215 pass, 16 skipped (SQL Server-only migration replays, by design) |
| Quiz integration | 6 pass | 6 pass |
| Progress integration | 102 pass | 102 pass (incl. `SupportLinkQueueTests`, fixed in round 2) |
| Aspire `dotnet test` (model + `AppHostRunTests`) | 35 pass | n/a |

### E2E API

| Stack | Result |
| --- | --- |
| Postgres compose stack | 93 passed, 0 failed, 0 skipped (incl. Postgres path of `LearnerWordMessagingTests`) |
| SQL Server compose stack | Reused from round 1: 93 / 93 on identical application code (no application code changed since `d60e774`); not rebuilt because `--build` fails on this machine |

First Postgres run: 91 / 93. The 2 autofill tests failed because Content lacked `Autofill__UseFakeClients=true` (known, see `docs/ai/learnings.md`, WB-68). Re-ran with `../e2e/docker-compose.e2e.yml` as override: 93 / 93.

### E2E UI (Postgres stack)

- Smoke (login, navigation, vocabulary review; adult, admin and child scenarios): 14 / 14 pass.
- Full suite: 61 / 61 pass.

### 2 GB check (`docker stats --no-stream`, Postgres stack after the e2e runs)

| Container | Memory |
| --- | --- |
| content-api | 240 MiB |
| progress-api | 229 MiB |
| identity-api | 210 MiB |
| rabbitmq | 159 MiB |
| postgres | 89 MiB |
| quiz-api | 69 MiB |
| notification-api | 42 MiB |
| ui | 14 MiB |
| **Total** | **about 1053 MiB** (target < 1.6 GB: pass) |

### Aspire

- `dotnet test aspire/WordBuddy.Aspire.slnx`: 35 / 35, including `AppHostRunTests` (default SQL Server: all APIs healthy, traces reach OTLP).
- `AppHost` with `Parameters:db-provider=Postgres` (+ throwaway `pg-password`): `pg-*` container started, all 5 APIs `/health/ready` = 200. Stopped afterwards. First attempt hung waiting for the unset `pg-password` secret (dashboard prompt), killed and rerun with the value.
- Compose stack was down (`down`) before Aspire ran.

### Environment notes

- Recreating `content-api`/`identity-api` with the e2e override also recreated `postgres` (env differed from the original launch). The `postgres-init` step then failed on password auth. Fixed with `ALTER USER` through the local socket, then `up -d --no-build` for the whole stack. No secret printed or stored.
- Aspire leaves persistent `sql-*`, `pg-*` and `rabbitmq-*` containers by design; not removed.

### Merge guard

| Guard | Result |
| --- | --- |
| 1. `review.md` Approve; `d431d55` ancestor of HEAD | Pass |
| 2. Changed vs "reviewed code + main" | `proposal.md`, `review.md` only (allowlisted); this run adds 2 Progress integration test files, the living spec, the README row, `test-report.md` (allowlisted) |
| 3. Every suite ran, zero failures | Pass (Content Postgres has 16 by-design skips) |

### Failures

None.

### Verdict

Approved and merged into main.

Open nits, not blocking: ADR 0009 Consequences does not yet say that concurrent duplicates on Postgres are resolved by the consumer retry (ADR is outside the tester allowlist; the living spec states it). `k8s/postgres/configmap.yaml` sync header. `E2E_PROGRESS_DB` path still opens a `SqlConnection`.
