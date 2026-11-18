# Review: Add PostgreSQL as an alternative persistence provider

PR: #72 · Round 1 · Reviewed commit: d60e774 · 2026-11-18T10:28:24+07:00

## Verdict

Approve — no blockers, no majors. SQL Server path is unchanged, the Postgres path works, and both are guarded by tests. 4 nits.

## What was verified

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

## Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.Infrastructure/Repositories/LearnerWordMembershipRepository.cs:91-104` | The "resolve to the existing row" path after a unique violation is not reached on Postgres the way it is on SQL Server. The consumer runs inside the MassTransit EF outbox transaction, which uses `RepeatableRead` by default. On Postgres that is a snapshot, so `FindTrackedAsync` cannot see the concurrent winner → `ConcurrentUpdate` → the consumer throws → the MassTransit retry succeeds in a new transaction. The result is correct, but it relies on the retry, and the tasks.md audit says "none to fix". | Add one line to the code comment or ADR 0009 Consequences: on Postgres, concurrent duplicate events are resolved by the consumer retry. `/test`: check the concurrent duplicate-event case on Postgres. |
| 2 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.IntegrationTests/ProviderParityTests.cs:40,62`, `WordBuddy/src/Services/Identity/WordBuddy.Identity.IntegrationTests/ProviderParityTests.cs:141` | The parity tests accept any `DbUpdateException`. An unrelated failure (for example a FK or NOT NULL error from a test-data change) would also pass, so they do not prove that the unique index fired. | Also assert the cause: resolve `IDatabaseDialect` and assert `IsUniqueViolation(ex, indexName)` in Content, or check the inner exception's index or constraint name in Identity. |
| 3 | nit | `.claude/plans/WB-71_add-postgresql-as-an-alternative-persistence-provider/tasks.md:101` | The task note says "WordBuddy/CLAUDE.md is not edited (ask-gated)", but the PR edits it (Sam approved, as the PR body says). The record is stale. | `/test` (or the next `/code` run) corrects the note: "applied with Sam's approval". |
| 4 | nit | `WordBuddy/k8s/postgres/configmap.yaml:1-19` | A full copy of `k8s/configmap.yaml`, differing only in `Database__Provider`. A key added later to one file can be missed in the other. | Add a header comment "keep in sync with k8s/configmap.yaml" to both files, or use a kustomize overlay later (scope suggestion). |

## Plan conformance

- **Covered:** 77/83 tasks. All Shared, Identity, Content, Quiz, Progress, Orchestration and Docs tasks are in the diff. The 6 open tasks are `/test` tasks by design (e2e on both stacks, Aspire run, 2 GB check).
- **Decisions honoured:** `citext` (#1), `Guid.NewGuid()` kept (#2), metadata-only migration move with pinned-id guard (#3), `postgres:17-alpine` (#4).
- **Beyond the plan's letter (accepted):**
  - `WordBuddy/CLAUDE.md` edited instead of only proposed. Sam approved it (PR body).
  - `docs/features/WB-71_…md` living spec added in `/code`. Normally `/test` updates it on merge; `/test` should review it.
  - The `integration-test` skill is updated next to `ef-migration`. The plan allows this ("if it names a provider").
- **Gaps:** none.
