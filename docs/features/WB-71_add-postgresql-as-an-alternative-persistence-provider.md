---
feature: Persistence providers (SQL Server / PostgreSQL)
services: Identity | Content | Quiz | Progress
audience: Both
state: proposed
last-updated-by: WB-71_add-postgresql-as-an-alternative-persistence-provider
---

# Persistence providers (SQL Server / PostgreSQL)

Each data service can store its data in SQL Server (default) or PostgreSQL, selected by configuration.

## What it does

_Proposed — not on main yet._

- `Database:Provider` = `SqlServer` (default) | `Postgres`, set per service.
- SQL Server stays the default; existing setups do not change.
- PostgreSQL lets the full stack run on low-resource hosts (2 GB RAM or less).

## Rules

- Same behaviour on both providers (lookups, unique indexes, concurrency, locking).
- Every schema change ships a migration for each provider.
- **Child vs adult:** no difference; the same policies and child restrictions apply on both providers.

## API

No API change.

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

No UI change.

## Pending changes

- `WB-71_add-postgresql-as-an-alternative-persistence-provider` — add PostgreSQL as a config-selected second provider (#71)

## Change history
