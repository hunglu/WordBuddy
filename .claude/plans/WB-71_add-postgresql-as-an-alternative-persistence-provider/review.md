# Review: Add PostgreSQL as an alternative persistence provider

PR: #72 · Round 2 · Reviewed commit: d431d55 · 2026-11-18T16:17:45+07:00

## Verdict

Approve — no blockers, no majors. The delta since `d60e774` is test code and bookkeeping only; the polling fix is sound and does not hide a consumer failure. 4 nits.

## Delta reviewed (`d60e774..d431d55`)

```
d60e774 (approved, round 1)
  → 06926c4 review round 1        docs only
  → 1c4fa60 /test round 1         tests + test-report
  → eb8c069 /test round 2         test-report
  → d431d55 fix round 1           SupportLinkQueueTests + tasks.md
```

| Check | Result |
| --- | --- |
| Application code changed after `d60e774` | **None.** 9 files: 4 plan docs, 4 integration test files, 1 e2e test file. `git diff --name-only d60e774..d431d55 -- WordBuddy/src ':!*Tests*'` is empty |
| Root cause of the Postgres-only failure | Plausible and consistent with the code. `Consumed.Any()` returns once the harness inactivity timer has fired. The factory runs `MigrateAsync` after host start, which is slower on Postgres (new database + migrations), so the timer can expire before the consumers run |
| Polling fix masks a failure? | No. It polls `Consumed.Select` for up to 15 s, then asserts the stored `SupportLinkProjection` row (`IsActive = false`). A failing consumer leaves no row or a wrong row, so the test still fails |
| Parity tests (round-1 nit 2) | Now assert the index name in the exception (`IX_Lexemes_…`, `IX_Senses_…`, `IX_UnlinkRequests_LinkId_Open`). Constants come from the configuration classes in Content |
| Concurrent duplicate test (round-1 nit 1) | 6 parallel `LearnerWordAdded` with different message ids → 1 row, no `Fault`. `UpsertAddedAsync` is an upsert, so the retry resolves the loser |
| e2e `LearnerWordMessagingTests` | Detects SQL Server, then Postgres (`psql` over the local socket in the container; no password on the host). Only Guids and booleans are embedded in SQL. Quoted identifiers escape correctly through `sh -c`. With the Postgres override, `sqlserver` is in an inactive profile, so detection cannot pick a stale SQL Server database. Builds clean |
| Reviewer run on Postgres (`wordbuddy-postgres-1`) | Progress `Messaging` tests 12/12 green; `SupportLinkQueueTests` alone (cold start, the failing case) 3/3 green |

## Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.IntegrationTests/Messaging/LearnerWordConsumerTests.cs:119-123` | The test waits 5 s, then asserts no `Fault<LearnerWordAdded>`. The retry budget is 200 + 1000 + 5000 ms = 6.2 s, and the fault is published only after the last retry. If the loser failed every retry, the fault would arrive after the check and the test would still pass. | Wait longer than the retry budget (for example 8 s), or poll `Harness.Published` for the fault until that deadline. |
| 2 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.IntegrationTests/Messaging/SupportLinkQueueTests.cs:40-44` | The projection assertion (`IsActive = false`) proves that the revoke consumer succeeded. It does not prove the activate consumer succeeded: the revoke alone creates the same row. The comment claims "both consumers succeeded". | Also assert that the `SupportLinkActivated` consumed entry has no `Exception`, or that no `Fault<SupportLinkActivated>` was published. |
| 3 | nit | `WordBuddy/k8s/postgres/configmap.yaml:1-19` | Carried from round 1 (nit 4), not addressed. A full copy of `k8s/configmap.yaml`; drift risk only, no current defect. Stays a nit. | Add a "keep in sync with k8s/configmap.yaml" header to both files, or use a kustomize overlay later. |
| 4 | nit | `docs/adr/0009-multi-provider-persistence.md` | Carried from round 1 (nit 1), partly addressed. The behaviour is now tested, but neither ADR 0009 nor the repository comment states that, on Postgres, concurrent duplicate events are resolved by the consumer retry. | Add one line to ADR 0009 Consequences. Docs only; can go with `/test` living-spec update. |

Round-1 nits 2 (parity tests) and 3 (`tasks.md:101` note) are **resolved**.

## Plan conformance

- **Covered:** unchanged from round 1 (77/83 implementation and docs tasks). The delta adds `/test` work only.
- **Out of scope:** none. The e2e provider detection serves the plan task "e2e on both stacks".
- **Gaps:** `E2E_PROGRESS_DB` (kind/CI path) still opens a `SqlConnection`. A Postgres connection string there makes the e2e test fail instead of running or skipping. Not required by the plan (compose stacks only); noted for a later change.

## Previous rounds

### Round 1

PR: #72 · Round 1 · Reviewed commit: d60e774 · 2026-11-18T10:28:24+07:00

#### Verdict

Approve — no blockers, no majors. SQL Server path is unchanged, the Postgres path works, and both are guarded by tests. 4 nits.

#### What was verified

| Check | Result |
| --- | --- |
| SQL Server migration move (60 files) | Diffed each file against its original on `main`: only `namespace`, `using`, `[DbContext]` type and snapshot class name change. No `Up`/`Down` change, ids unchanged. No file left in `Migrations/` root |
| Service independence (ADR 0001) | No cross-service or shared persistence code. `DatabaseOptions`, dialects and overrides are per service. Shared change is one optional parameter |
| Clean Architecture | Domain and Application untouched. All provider code in Infrastructure; repositories drop `Microsoft.Data.SqlClient` |
| Secrets | Only dev placeholders in `.env.example` / `secret.yaml.example` (same pattern as the existing SQL Server values). `k8s/postgres/secret.yaml` gitignored. Test factories read the PG connection string from env vars |
| Child vs adult | No policy or query change. Existing Adult/Child integration tests pass on Postgres |
| Postgres row lock (Progress) | `FOR UPDATE` on the `Challenges` row, then a new `COUNT` statement (READ COMMITTED sees the committed winner). Correct |
| Savepoint (Content guarded delete) | Created before the raw statement, rolled back on FK violation. Correct on both providers |
| `xmin` in Postgres `InitialCreate` | Declared in the migration; Npgsql skips system columns in `CREATE TABLE`. Migration applies (tests green) |
| Health checks | `AddDbContextCheck<XDbContext>`: provider-neutral |
| Database diagrams | Source snapshot renamed; Postgres note per service. Logical schema unchanged → no ER change needed |
| Unit tests (reviewer run) | Identity 253, Content 481, Quiz 17, Progress 472 — all green |
| Integration on Postgres (reviewer run, `wb-pg`) | Identity 56, Quiz 6, Progress 101, Content 215 + 16 skipped (SQL Server-only migration replays) — all green |
| Aspire `AppHostModelTests` (reviewer run) | 26 green |
| Integration on SQL Server | Not re-run by the reviewer; coder reports green. `/test` covers it |

#### Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.Infrastructure/Repositories/LearnerWordMembershipRepository.cs:91-104` | The "resolve to the existing row" path after a unique violation is not reached on Postgres the way it is on SQL Server. The consumer runs inside the MassTransit EF outbox transaction, which uses `RepeatableRead` by default. On Postgres that is a snapshot, so `FindTrackedAsync` cannot see the concurrent winner → `ConcurrentUpdate` → the consumer throws → the MassTransit retry succeeds in a new transaction. The result is correct, but it relies on the retry, and the tasks.md audit says "none to fix". | Add one line to the code comment or ADR 0009 Consequences: on Postgres, concurrent duplicate events are resolved by the consumer retry. `/test`: check the concurrent duplicate-event case on Postgres. |
| 2 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.IntegrationTests/ProviderParityTests.cs:40,62`, `WordBuddy/src/Services/Identity/WordBuddy.Identity.IntegrationTests/ProviderParityTests.cs:141` | The parity tests accept any `DbUpdateException`. An unrelated failure (for example a FK or NOT NULL error from a test-data change) would also pass, so they do not prove that the unique index fired. | Also assert the cause: resolve `IDatabaseDialect` and assert `IsUniqueViolation(ex, indexName)` in Content, or check the inner exception's index or constraint name in Identity. |
| 3 | nit | `.claude/plans/WB-71_add-postgresql-as-an-alternative-persistence-provider/tasks.md:101` | The task note says "WordBuddy/CLAUDE.md is not edited (ask-gated)", but the PR edits it (Sam approved, as the PR body says). The record is stale. | `/test` (or the next `/code` run) corrects the note: "applied with Sam's approval". |
| 4 | nit | `WordBuddy/k8s/postgres/configmap.yaml:1-19` | A full copy of `k8s/configmap.yaml`, differing only in `Database__Provider`. A key added later to one file can be missed in the other. | Add a header comment "keep in sync with k8s/configmap.yaml" to both files, or use a kustomize overlay later (scope suggestion). |

#### Plan conformance

- **Covered:** 77/83 tasks. All Shared, Identity, Content, Quiz, Progress, Orchestration and Docs tasks are in the diff. The 6 open tasks are `/test` tasks by design (e2e on both stacks, Aspire run, 2 GB check).
- **Decisions honoured:** `citext` (#1), `Guid.NewGuid()` kept (#2), metadata-only migration move with pinned-id guard (#3), `postgres:17-alpine` (#4).
- **Beyond the plan's letter (accepted):**
  - `WordBuddy/CLAUDE.md` edited instead of only proposed. Sam approved it (PR body).
  - `docs/features/WB-71_…md` living spec added in `/code`. Normally `/test` updates it on merge; `/test` should review it.
  - The `integration-test` skill is updated next to `ef-migration`. The plan allows this ("if it names a provider").
- **Gaps:** none.
