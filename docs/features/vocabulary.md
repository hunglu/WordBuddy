---
feature: Vocabulary storage
services: Content
audience: Both
state: proposed
last-updated-by: refactor-database-scheme-to-store-vocabulary-item
---

# Vocabulary storage

## What it does

_Proposed — not on main yet._ All vocabulary words, whether the system or a user added them, are
stored once in a single table. A separate table links users to the words they have, with any
per-user settings. System words belong to a system user.

## Rules

_To backfill._

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

_To backfill._

## Pending changes

- `refactor-database-scheme-to-store-vocabulary-item` — store all vocabulary in one table, linked to users through a user–word table (#6)

## Change history
