---
title: WB-61_Adopt .NET Aspire for local orchestration
status: in-progress
type: new
issue: 61
pr: none
affects: docs/features/WB-61_adopt-net-aspire-for-local-orchestration.md
supersedes: none
version: 3.0
created: 2026-11-02T16:06:13+07:00
updated: 2026-11-02T16:22:32+07:00
---

## Problem

The local dev loop is `docker-compose.yml` plus `make up/down/logs/clean`.

- Every run rebuilds images and restores shared packages through a BuildKit secret.
- There is no single local view of logs and traces across the 5 services.
- Local secrets live in `.env`.

## Goal

`aspire run` starts the full local stack, with one dashboard for logs, traces and metrics.

```
aspire run → AppHost → SQL Server + RabbitMQ (containers)
                     → Identity, Content, Quiz, Progress, Notification (processes)
                     → Vite UI
                     → dashboard (OTLP)
```

The AppHost is orchestration-only, so ADR 0001 (service independence) stays intact.

## Target users

Developers only. No learner-facing change: child and adult behaviour stays the same.

## Success criteria

- `aspire run` replaces `make up` for daily work; every service reaches healthy.
- The dashboard shows logs and traces from every service, with no change to service code.
- No service gains a project reference to the AppHost or to a ServiceDefaults project.
- Secrets come from Aspire parameters (user-secrets), not `.env`.
- The compose, kind, `k8s/` and NuGet make targets still work unchanged.

## Constraints

- Scope: dev loop only. Keep `docker-compose.yml`, the kind/`k8s/` targets and
  `pack-shared`/`publish-shared` as they are.
- No `ServiceDefaults` project. `WordBuddy.Shared.Infrastructure` already covers Serilog, OTel
  and health checks.
- Use path-based `AddProject(name, path)`, not `AddProject<Projects.X>`, so the AppHost has no
  project references to services.
- Needs an ADR, because the AppHost touches ADR 0001.
- Open question for `/plan`: how migrations run (a migrator worker per service, or keep the manual
  `dotnet ef database update`).
- Out of scope: generating k8s manifests with `aspire publish`.
