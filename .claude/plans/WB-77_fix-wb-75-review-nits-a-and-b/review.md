# Review: WB-77 Fix WB-75 review nits A and B

PR: #78 · Round 1 · Reviewed commit: 0a95ee3 · 2026-11-22T22:34:35+07:00

## Verdict
Approve — no blockers, no majors; 3 optional nits on test strength and the `sa` generator.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `WordBuddy/src/Services/Identity/WordBuddy.Identity.IntegrationTests/MigrationFailureLogTests.cs:58` (same in Content, Quiz, Progress) | The Postgres branch does not catch a regression. Npgsql redacts `Detail` by default, so `PostgresException.Message` holds no key value. If `Message` were logged again, the test would still pass on Postgres. The SQL Server branch is sound: 2627 puts the sentinel in `Message`. | Open the connection with `server.TrimEnd(';') + ";Include Error Detail=true"`. Then `Message` carries `Key (id)=(sentinel...)` and the `NotContain(Sentinel)` assertion has teeth. |
| 2 | nit | `.../MigrationFailureLogTests.cs:65` (4 copies) | The SQL Server branch hardcodes `(localdb)\mssqllocaldb`. `IdentityApiFactory` honours `IDENTITY_TEST_CONNECTION_STRING`; this test does not. On a CI host without LocalDB, it fails while the rest of the suite runs. | Reuse the factory's server (or the `<SERVICE>_TEST_CONNECTION_STRING` variable) with `Database=master`. |
| 3 | nit | `WordBuddy/deploy/single-host/.env.example:26`, `docs/deployment/single-host.md:136` | The `sa` generator yields no digit in about 1 of 280 runs ((52/62)^32 ≈ 0.36 %). Then `sa` fails the 3-of-4 complexity rule, `sqlserver` never gets healthy, and `init.sh` never runs to explain it. | Add "check it has upper, lower and digit; rerun if not", or a one-line loop that repeats until all three classes are present. |

### Focus checks (verified, no finding)

| Check | Result |
| --- | --- |
| Logged SQL Server fields | `Number`, `Class`, `State`, `Procedure`, `LineNumber`: metadata only. `Message`, `Server`, `Errors` text not logged. |
| Logged Postgres fields | `SqlState`, `Severity`, `SchemaName`, `TableName`, `ColumnName`, `ConstraintName`: metadata only. `Detail`, `Hint`, `Where`, `MessageText`, `InternalQuery` not logged. |
| Stack traces | `Exception.StackTrace` holds frames only, no message. |
| 4 copies | `MigrationFailureLog.cs`: identical apart from the service name. Tests: identical apart from the pre-existing `MigrationRunner.RunAsync` signature (Identity takes configuration). |
| Duplicate-key test (SQL Server) | `#wb_dup` primary key → 2627; message holds the sentinel, so the test fails if `Message` is logged. Postgres: see finding 1. |
| Bootstrap email match | Postgres `Email` is `citext` (`PostgresIdentityModelOverrides.cs:36`); SQL Server default collation is case-insensitive. `AnyAsync(u => u.Email == options.Email)` matches the unique index on both. |
| Bootstrap race | Check → insert gap still hits the unique index → `DbUpdateException` → `MigrationRunner` catch → `MigrationFailureLog` (no key value) → exit 1. Fails closed. |
| Clash never promotes | Returns `false` before any write; tests cover Adult and Child. |
| `init.sh` shell | Shebang `#!/bin/bash`, entrypoint `["bash", "/init.sh"]`, mssql/server image ships bash. `${!name:-}` and `[[ =~ ]]` are safe under `set -eu`. Mode `100755` kept. Value never echoed. |
| `${VAR:?msg}` | Valid Compose syntax; covers sqlserver, init, 4 migrate jobs, 4 APIs. Healthcheck uses the container env (`$$MSSQL_SA_PASSWORD`). |
| Docs | Hex (48 chars) still passes the 16+ alnum rule for the service logins; `sa` note matches SQL Server's rule. No NAS mention. |

## Plan conformance
- All 15 tasks in `tasks.md` are reflected in the diff.
- `docs/features/WB-75_...md` gets a "Pending changes" line: expected bookkeeping, not scope creep.
- Plan A3 says Postgres email comparison is case-sensitive; it is `citext` (case-insensitive). The code is correct either way, because it uses the same column comparison as the unique index. Plan text only.
- No out-of-scope application changes.
