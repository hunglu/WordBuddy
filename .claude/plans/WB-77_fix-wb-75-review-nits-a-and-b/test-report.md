# Test report: WB-77 Fix WB-75 review nits A and B

Verdict: all suites green on SQL Server and PostgreSQL. Approved.

## Backend unit/integration

Run in this session, on `feature/WB-77_fix-wb-75-review-nits-a-and-b` with `origin/main` merged ("Already up to date").

| Service | Unit (SQL Server) | Integration (SQL Server, localdb) | Unit (PostgreSQL) | Integration (PostgreSQL) |
| --- | --- | --- | --- | --- |
| Identity | 262 pass | 66 pass | 262 pass | 66 pass |
| Content | 481 pass | 235 pass | 481 pass | 219 pass, 16 skipped |
| Quiz | 17 pass | 10 pass | 17 pass | 10 pass |
| Progress | 472 pass | 106 pass | 472 pass | 106 pass |

- 0 failures anywhere.
- The 16 Content skips are `[SqlServerOnlyFact]` (SQL Server migration replay), by design, not new.
- PostgreSQL: Aspire container `pg-d32bbd0a`; password read inline, never printed or written.

Review nits fixed in the 4 `MigrationFailureLogTests.cs` (test projects only):

| Nit | Change |
| --- | --- |
| 1 | The Postgres duplicate-key test connection adds `Include Error Detail=true`, so `Message` would carry the key if it were logged. |
| 2 | The SQL Server duplicate-key test uses `<SERVICE>_TEST_CONNECTION_STRING` (fallback localdb) with `Database=master`. |

Both providers re-ran green after the change.

### `init.sh` checks (bash in `mcr.microsoft.com/mssql/server:2025-latest`, throwaway containers)

| Case | Exit | Output |
| --- | --- | --- |
| `MSSQL_SA_PASSWORD` empty | 1 | `init: MSSQL_SA_PASSWORD is empty` |
| value ends in `'` | 1 | `init: IDENTITY_DB_PASSWORD must be 16+ letters and digits` |
| 15 characters | 1 | `init: CONTENT_DB_PASSWORD must be 16+ letters and digits` |
| `PROGRESS_DB_PASSWORD` unset | 1 | `init: PROGRESS_DB_PASSWORD is empty` |
| 5 valid values | 1 | passes the checks, then `sqlcmd` fails: no `sqlserver` host in this container (expected) |

No password value was printed.

### Compose overlay

`docker compose -f docker-compose.yml -f docker-compose.sqlserver.yml --env-file <throwaway> config -q`:

- All values set: exit 0.
- `SA_PASSWORD` and `QUIZ_DB_PASSWORD` missing: exit 1, `QUIZ_DB_PASSWORD must be set in .env` (and the `SA_PASSWORD` message).
- Throwaway env files were kept in the session scratchpad, outside the repo.

## E2E API

N/A — no API behaviour change (log content, bootstrap clash handling and deploy scripts only). Not counted as a pass.

## E2E UI

N/A — no UI change. Not counted as a pass.

## Failures

None.

## Merge guard

1. `review.md`: Approve, reviewed commit `0a95ee3` is an ancestor of HEAD.
2. Files changed since the review, against reviewed code + current main (`git merge-tree`):
   - `.claude/plans/WB-77_fix-wb-75-review-nits-a-and-b/proposal.md`
   - `.claude/plans/WB-77_fix-wb-75-review-nits-a-and-b/review.md`

   Both are on the allowlist. This run adds only the 4 `MigrationFailureLogTests.cs`, `test-report.md`, `proposal.md`, and the living spec (all allowlisted).
3. Every suite that applies ran with 0 failures; E2E suites are N/A, not skipped.

## Follow-ups (not done here)

- Review nit 3: the `sa` password generator yields no digit in about 1 of 280 runs. Add a check or retry loop to `.env.example` and `docs/deployment/single-host.md`.
- Add a `.gitattributes` rule so `init.sh` keeps LF and mode `755` on Windows checkouts.

## Verdict

Approved — merged into main
