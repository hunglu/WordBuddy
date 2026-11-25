---
title: WB-71_Add PostgreSQL as an alternative persistence provider
status: released
released: v1.0.0
type: new
issue: 71
pr: 72              # set by /code when it opens the pull request
affects: docs/features/WB-71_add-postgresql-as-an-alternative-persistence-provider.md
supersedes: none
version: 1.10
created: 2026-11-18T08:50:46+07:00
updated: 2026-11-25T17:21:13+07:00
---

## Problem

All data services (Identity, Content, Quiz, Progress) run only on SQL Server.

- SQL Server needs about 2 GB RAM on its own.
- The default container image is Developer edition, which is not licensed for production.
- WordBuddy therefore cannot run on low-resource hosts (2 GB RAM or less).

## Goal

Add PostgreSQL as a **second** persistence provider. This is an extra adapter, not a replacement.

```
Database:Provider = SqlServer (default) | Postgres
        │
        ▼
AddInfrastructure(...)
  ├─ SqlServer → UseSqlServer + SqlServer migrations
  └─ Postgres  → UseNpgsql    + Postgres migrations
```

- Each service selects its provider by configuration, the same pattern as `Services:Content:Transport`.
- SQL Server stays the default. Existing setups (dev, compose, k8s, Aspire) behave exactly as today.
- Domain and Application do not change. All work stays inside each service's Infrastructure.

## Target users

Both. No learner-visible change.

- **Child vs adult:** no change. The same authorization policies, child restrictions and data apply on both providers.

## Success criteria

- Each data service starts, migrates and passes its integration tests on both providers.
- Same behaviour on both providers:
  - case-insensitive email and alias lookups;
  - filtered unique indexes;
  - concurrency tokens (Progress);
  - row locking (Progress challenges).
- The full stack runs on PostgreSQL on a 2 GB RAM host.
- docker compose, k8s and Aspire offer a PostgreSQL option; SQL Server remains the default.
- The `ef-migration` skill creates a migration for each provider on every schema change.
- ADR 0009 records the multi-provider persistence decision.

## Constraints

- Service independence holds (ADR 0001): no shared persistence project; the 6-project layout per service stays.
- The MassTransit EF outbox must work on both providers.
- Known provider differences to handle: filtered index SQL, `rowversion`, `UPDLOCK/HOLDLOCK`, `NEWID()`, `nvarchar(max)`, raw SQL quoting, string case sensitivity.
- Ongoing cost accepted: two migrations per schema change and a two-provider test matrix.
- Out of scope: MongoDB; data migration from an existing SQL Server database to PostgreSQL; Notification (no database).
- Deadline: _Not specified._
