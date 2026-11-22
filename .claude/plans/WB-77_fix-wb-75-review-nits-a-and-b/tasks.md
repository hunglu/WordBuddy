# Tasks: WB-77 Fix WB-75 review nits A and B

## Backend

- [x] Identity `Api/Extensions/MigrationFailureLog.cs`: replace `providerMessage` with a data-free `ProviderDetails` string (plan A2); update XML remarks — no `Message`/`MessageText`/`Detail`/`Hint`/`Where` is ever logged. — MigrationFailureLog.cs (Identity)
- [x] Copy the same change to Content, Quiz and Progress `MigrationFailureLog.cs` — the 4 files differ only in the namespace (diff check). — MigrationFailureLog.cs (Content, Quiz, Progress; diff = namespace only)
- [x] Identity `Infrastructure/Seeding/AdminBootstrapper.cs`: after the options check, return `false` with a PII-free `LogError` when a user with `options.Email` exists; never promote that user — no insert runs on a clash. — AdminBootstrapper.cs
- [x] Update the `AdminBootstrapper` XML summary to name the clash case — doc lists all three `false`/`true` outcomes. — AdminBootstrapper.cs

## Deploy

- [x] `deploy/single-host/sqlserver/init.sh`: add `require_password` (empty check + `^[A-Za-z0-9]{16,}$`, indirect expansion, value never echoed) and call it for all 5 variables before the first `sqlcmd` — bad input exits 1 with `init: <NAME> ...` on stderr. — sqlserver/init.sh
- [x] Update the `init.sh` header comment to state the rule — comment matches the check. — sqlserver/init.sh
- [x] `docker-compose.sqlserver.yml`: change every `${SA_PASSWORD}` and `${<S>_DB_PASSWORD}` to `${VAR:?VAR must be set in .env}` — `docker compose -f docker-compose.yml -f docker-compose.sqlserver.yml config` fails with that text when one is missing. — docker-compose.sqlserver.yml (config fails with the message)
- [x] `.env.example`: state the rule (16+ letters and digits) and give a mixed-case generator for `SA_PASSWORD` — the hex generator is no longer suggested for `sa`. — .env.example

## Docs

- [x] `docs/deployment/single-host.md` "SQL Server (optional)": add the password rule, the `sa` complexity note and the fail-fast behaviour — 3 short bullets, no NAS mention. — docs/deployment/single-host.md

## Tests

- [x] Each of the 4 `MigrationFailureLogTests.cs`: add `MigrationFailureLog_Write_OmitsKeyValueOnDuplicateKey` — open a raw connection to the test server (SQL Server `(localdb)\mssqllocaldb`, or `WORDBUDDY_TEST_PG_CONNECTION_STRING`), insert a sentinel value twice into a session temp table with a primary key, wrap the caught `DbException` in an outer exception, call `Write`; assert log contains `2627` (SQL Server) or `23505` (Postgres) and the stack trace, and does not contain the sentinel. — 4x MigrationFailureLogTests.cs
- [x] Each of the 4 `MigrationFailureLogTests.cs`: the existing unreachable-database test still passes — password absent, `provider error code` present. — 4x MigrationFailureLogTests.cs
- [x] Identity `MigrationAndBootstrapTests.cs`: add `AdminBootstrapper_Run_FailsWithoutPiiWhenEmailTaken` — seed a non-admin user with `AdminEmail`, run `MigrationRunner`; exit code 1, no admin exists, the user is unchanged, log has the clash message and not `AdminEmail`. — Identity MigrationAndBootstrapTests.cs
- [x] Identity `MigrationAndBootstrapTests.cs`: same scenario with a `Child` user — user stays Child and non-admin. — Identity MigrationAndBootstrapTests.cs
- [x] `init.sh` check (tester, record in test report): run `bash init.sh` with an empty value, with a value containing `'`, and with a 15-character value — each exits 1 before any `sqlcmd`, message names the variable, value not printed. — checked with host bash: empty, quote, 15 chars, unset all exit 1 before sqlcmd
- [x] Run Identity, Content, Quiz and Progress suites on SQL Server and on Postgres — all green. — 4 suites green on SQL Server localdb and Postgres
