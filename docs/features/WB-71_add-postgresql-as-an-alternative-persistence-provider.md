---
feature: Persistence providers (SQL Server / PostgreSQL)
services: Identity | Content | Quiz | Progress
audience: Both
state: shipped
last-updated-by: WB-71_add-postgresql-as-an-alternative-persistence-provider
---

# Persistence providers (SQL Server / PostgreSQL)

Each data service stores its data in SQL Server (default) or PostgreSQL, selected by configuration. ADR 0009.

## What it does

- `Database:Provider` = `SqlServer` (default) | `Postgres`, read by Identity, Content, Quiz and Progress. An unknown value stops startup. Notification has no database.
- One abstract `<Service>DbContext` holds the model. `SqlServer<Service>DbContext` and `Postgres<Service>DbContext` own the migrations (`Persistence/Migrations/SqlServer/`, `.../Postgres/`).
- Provider code lives only in each service's Infrastructure (`Postgres<Service>ModelOverrides`, `IDatabaseDialect`). Domain and Application are unchanged.
- The MassTransit EF outbox/inbox works on both providers.
- On Postgres, concurrent duplicate `LearnerWordAdded` events can fail once under snapshot isolation; the consumer retry then resolves them (one row, no `Fault`).
- Full stack on PostgreSQL: about 1.05 GiB in total (8 containers, measured after the e2e runs), so it fits a 2 GB host.

```
Database:Provider
  ├─ SqlServer → UseSqlServer + SqlServer migrations   (default)
  └─ Postgres  → UseNpgsql    + Postgres migrations
```

## Rules

- Same behaviour on both providers: case-insensitive email and alias lookups (`citext` on Postgres), filtered unique indexes, concurrency tokens (`xmin` on Postgres), row locking (`FOR UPDATE` on Postgres).
- Every schema change ships **two migrations**, one per provider (`ef-migration` skill).
- Integration tests run on both providers: `WORDBUDDY_TEST_DB_PROVIDER=Postgres` and `WORDBUDDY_TEST_PG_CONNECTION_STRING`. SQL Server is the default. 16 Content tests replay SQL Server-only migrations and are skipped on Postgres.
- No data migration from an existing SQL Server database to PostgreSQL.
- **Child vs adult:** no difference; the same policies and child restrictions apply on both providers.

## How to run

| Target | SQL Server (default) | PostgreSQL |
|---|---|---|
| docker compose | `make up` | `make up-postgres` (`docker-compose.postgres.yml`) |
| Migrations | `make migrate SERVICE=<x>` | `make migrate SERVICE=<x> DB=postgres` |
| kind | `make k8s-apply` | `make k8s-apply DB=postgres` (`k8s/postgres/`) |
| Aspire | `aspire run` with `db-provider=SqlServer` + `sql-password` | `aspire run` (default since `appsettings.json` sets `db-provider=Postgres`; needs `pg-password`) |

## API

No API change.

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

No UI change.

## Pending changes

## Change history

- `WB-71_add-postgresql-as-an-alternative-persistence-provider` — added PostgreSQL as a config-selected second provider (#71)
