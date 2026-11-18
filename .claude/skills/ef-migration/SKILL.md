---
name: ef-migration
description: Adds, reviews or removes an EF Core migration in one WordBuddy microservice (Identity, Content, Quiz, Progress) for BOTH database providers (SQL Server and PostgreSQL, ADR 0009) and keeps docs/database-diagram in sync. Use whenever an entity, its configuration, or the DbContext changes, or Sam asks for a migration. Applying a migration to a database stays ask-gated and is never done by this skill on its own.
---

# EF Core migration

Each service owns its own DbContext and migrations — never touch another service's schema.

Since WB-71 every data service has **two** providers (`Database:Provider` = `SqlServer` default or
`Postgres`, ADR 0009). One abstract `<Service>DbContext` holds the model; two thin subclasses own
the migrations. **Every schema change = two migrations with the same name.**

```text
change entity/config → build → migrations add (SqlServer) → migrations add (Postgres) → read both
   → update ER diagram → build + both model guards + tests        (database update = Sam, ask-gated)
```

## Paths

| Item | Path |
| --- | --- |
| SQL Server migrations | `WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Infrastructure/Persistence/Migrations/SqlServer/` |
| Postgres migrations | `WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Infrastructure/Persistence/Migrations/Postgres/` |
| Contexts | `Persistence/<Service>DbContext.cs` (model), `Persistence/SqlServer/SqlServer<Service>DbContext.cs`, `Persistence/Postgres/Postgres<Service>DbContext.cs` |
| Postgres-only model changes | `Persistence/Postgres/Postgres<Service>ModelOverrides.cs` |
| Startup project | `WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Api` |
| ER diagram | `docs/database-diagram/<service>.md` (rules: its `README.md`) |

## Steps

1. Change the shared model in `Persistence/Configurations/`. If the change uses provider-specific
   syntax (filter, column type, default SQL), keep the SQL Server form in the configuration and add
   the Postgres form to `Postgres<Service>ModelOverrides`.
2. Build the service: `dotnet build WordBuddy/src/Services/<Service>/WordBuddy.<Service>.slnx`.
3. Add **both** migrations (PascalCase name that says *what* changes, e.g. `AddPersonalVocabularyWords`; same name for both):

   ```bash
   dotnet ef migrations add <Name> --context SqlServer<Service>DbContext \
     --project   WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Infrastructure \
     --startup-project WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Api \
     --output-dir Persistence/Migrations/SqlServer

   dotnet ef migrations add <Name> --context Postgres<Service>DbContext \
     --project   WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Infrastructure \
     --startup-project WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Api \
     --output-dir Persistence/Migrations/Postgres
   ```

   `migrations add` does not connect to a database. Never omit `--context`: there are two contexts.

4. **Read both generated `Up`/`Down`.** Check:
   - no unintended `DropTable` / `DropColumn` (data loss) — if intended, add a data-copy step
     with `migrationBuilder.Sql(...)` and say so in the plan (a data-moving step is SQL Server
     only: Postgres starts empty);
   - new non-nullable columns on existing tables have a default;
   - indexes for new foreign keys and lookup columns;
   - `Down` really reverses `Up`;
   - Postgres file: no `nvarchar`, `[`, `N'` or `rowversion`; filters use `"Col"` quoting; case-insensitive
     text uses `citext`; unique indexes over nullable columns state `NullsDistinct` on purpose.
5. Update `docs/database-diagram/<service>.md` in the same change (reviewer marks a missing update
   as **major**).
6. Build and run the service's tests. **Both model guards must pass** (`HasPendingModelChanges()` is
   `false` for `SqlServer<Service>DbContext` and `Postgres<Service>DbContext`; the SQL Server migration
   id list test grows by the new id). For a data-moving migration, add an integration test (see the
   `integration-test` skill; example `UnifyVocabularyWordsMigrationTests.cs` in Content; mark it
   SQL Server only).
7. Run the integration tests on both providers (`WORDBUDDY_TEST_DB_PROVIDER=Postgres` plus
   `WORDBUDDY_TEST_PG_CONNECTION_STRING`; see the `integration-test` skill).
8. Remove a wrong, **unapplied and unpushed** migration only with
   `dotnet ef migrations remove --context <the context>` (same `--project` / `--startup-project`).
   Never delete migration files by hand; never edit a migration that is already on `main` — add a new one.

**A new migration without its Postgres twin (or the reverse) is a review major.**

> One-time exception (WB-71, ADR 0009): the existing SQL Server migrations were moved to
> `Migrations/SqlServer/` with metadata-only edits (namespace, `[DbContext]`, snapshot class name).
> Migration ids and `Up`/`Down` are unchanged. This does not allow editing any other applied migration.

## Applying (never automatic)

`dotnet ef database update`, `make migrate SERVICE=<x> [DB=postgres]` and
`make k8s-migrate SERVICE=<x> [DB=postgres]` are ask-gated. Propose the exact command and wait for
Sam. Never run them against a non-local database, and never print the connection string.
