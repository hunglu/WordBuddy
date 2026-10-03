---
name: ef-migration
description: Adds, reviews or removes an EF Core migration in one WordBuddy microservice (Identity, Content, Quiz, Progress) and keeps docs/database-diagram in sync. Use whenever an entity, its configuration, or the DbContext changes, or Sam asks for a migration. Applying a migration to a database stays ask-gated and is never done by this skill on its own.
---

# EF Core migration

Each service owns its own DbContext and migrations — never touch another service's schema.

```text
change entity/config → build → migrations add → read generated code → update ER diagram → build + tests
                                                                     (database update = Sam, ask-gated)
```

## Paths

| Item | Path |
| --- | --- |
| Migrations | `WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Infrastructure/Persistence/Migrations/` |
| Startup project | `WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Api` |
| ER diagram | `docs/database-diagram/<service>.md` (rules: its `README.md`) |

## Steps

1. Build the service: `dotnet build WordBuddy/src/Services/<Service>/WordBuddy.<Service>.slnx`.
2. Add the migration (PascalCase name that says *what* changes, e.g. `AddPersonalVocabularyWords`):

   ```bash
   dotnet ef migrations add <Name> \
     --project   WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Infrastructure \
     --startup-project WordBuddy/src/Services/<Service>/WordBuddy.<Service>.Api \
     --output-dir Persistence/Migrations
   ```

3. **Read the generated `Up`/`Down`.** Check:
   - no unintended `DropTable` / `DropColumn` (data loss) — if intended, add a data-copy step
     with `migrationBuilder.Sql(...)` and say so in the plan;
   - new non-nullable columns on existing tables have a default;
   - indexes for new foreign keys and lookup columns;
   - `Down` really reverses `Up`.
4. Update `docs/database-diagram/<service>.md` in the same change (reviewer marks a missing update
   as **major**).
5. Build and run the service's tests. For a data-moving migration, add an integration test (see
   the `integration-test` skill; example `UnifyVocabularyWordsMigrationTests.cs` in Content).
6. Remove a wrong, **unapplied and unpushed** migration only with
   `dotnet ef migrations remove` (same `--project` / `--startup-project`). Never delete migration
   files by hand; never edit a migration that is already on `main` — add a new one.

## Applying (never automatic)

`dotnet ef database update`, `make migrate SERVICE=<x>` and `make k8s-migrate SERVICE=<x>` are
ask-gated. Propose the exact command and wait for Sam. Never run them against a non-local
database, and never print the connection string.
