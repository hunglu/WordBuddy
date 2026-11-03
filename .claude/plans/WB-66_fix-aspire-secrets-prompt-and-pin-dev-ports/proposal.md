---
title: WB-66_Fix Aspire secrets prompt and pin dev ports
status: implemented
type: bugfix
issue: 66
pr: none
affects: docs/features/WB-61_adopt-net-aspire-for-local-orchestration.md
supersedes: none
version: 3.0
created: 2026-11-03T21:29:47+07:00
updated: 2026-11-03T21:43:14+07:00
---

## Problem

```text
aspire run → no Properties/launchSettings.json → AppHost runs as Production
  → user-secrets not loaded → dashboard asks for secrets on every run
  → saved values rewrite user-secrets → no match with the SQL / RabbitMQ volumes
  → logins fail → APIs never start
```

- SQL Server and the dashboard use dynamic ports, which change between runs.
- The AppHost has no `appsettings.json` for non-secret defaults.

## Goal

`aspire run` loads the secrets without a prompt, and every local dev port is fixed and documented.

## Target users

Developers only. No learner-facing change: child and adult behaviour stays the same.

## Success criteria

- No secret prompt in the dashboard when user-secrets are set.
- Fixed ports: dashboard 15180, SQL Server 14330, RabbitMQ 5672 / 15672, APIs 5080-5084, UI 3000.
- SQL Server does not use 1433, so the persistent container never blocks `make up`.
- An AppHost test asserts the infrastructure ports.
- The README documents the ports, the secret prompt and the volume reset after a password change.

## Constraints

- Changes only under `WordBuddy/aspire/` and `WordBuddy/README.md`. No change under `src/`,
  `WordBuddy.UI/`, `docker-compose.yml`, `Makefile` or `k8s/`.
- A draft already exists: branch `feature/aspire-dev-config-fixes`, commit 4f3129b, PR #65.
  `/plan` decides whether `/code` reuses it or moves it to `feature/<slug>`.
- Out of scope: deleting the existing local volumes (Sam's machine, manual step).
