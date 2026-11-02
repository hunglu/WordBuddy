# 0007 — .NET Aspire AppHost for local orchestration only

- Status: proposed
- Date: 2026-11-02 (WB-61)

## Context

- The local dev loop is `docker compose up --build`. Every run rebuilds images and restores
  `WordBuddy.Shared.*` through a BuildKit secret.
- There is no single local view of logs and traces across the 5 services.
- Local secrets live in `.env`.
- ADR 0001 forbids project references between services and to shared code. The default Aspire
  setup breaks that: the AppHost references every service with `AddProject<Projects.X>`, and every
  service references a `ServiceDefaults` project.

## Decision

Add one .NET Aspire AppHost, used for the local dev loop only.

```
aspire run → WordBuddy.AppHost → SQL Server, RabbitMQ (containers)
                               → 5 APIs (path-based AddProject, run as processes)
                               → Vite UI
                               → Aspire dashboard (OTLP)
```

| Topic | Decision |
| --- | --- |
| Location | `WordBuddy/aspire/WordBuddy.AppHost/`, with its own `.slnx`. It is outside `src/Services/` and outside every service solution |
| Service wiring | Non-generic `AddProject(name, "<path>.csproj")`. The AppHost has no `ProjectReference` to any service |
| ServiceDefaults | None. `WordBuddy.Shared.Infrastructure` keeps Serilog, OTel, health and rate limiting |
| Telemetry | The services keep reading `OTEL_EXPORTER_OTLP_ENDPOINT`. Aspire injects it, so service code does not change |
| Configuration | Through environment variables that match the existing keys (`ConnectionStrings__DefaultConnection`, `Jwt__*`, `Messaging__RabbitMq__*`) |
| Secrets | Aspire parameters with `secret: true`, stored in AppHost user-secrets. Never `.env`, never in git |
| Dependency direction | AppHost → services (by path) only. No service knows that the AppHost exists |
| Scope | Dev loop only. `docker-compose.yml`, kind, `k8s/` and the NuGet make targets stay. No `aspire publish` |

## Consequences

- Faster inner loop. Services run as processes, so there is no image rebuild or BuildKit secret per run.
- One dashboard shows traces and console logs for all services. Metrics and structured OTLP logs
  need a change in `WordBuddy.Shared.Infrastructure` (separate proposal).
- Each service can still move to its own repo: drop its `AddProject` line, and the service is unchanged.
- Two local run paths, Aspire and compose. Configuration keys must stay aligned between them.
- Developers need the Aspire CLI and a container runtime (Docker Desktop is already required).
- Migrations: keep the existing Development startup migration (`MigrateAsync`, plus seeding).
  Aspire runs each API with `ASPNETCORE_ENVIRONMENT=Development` and `WaitFor` the database. A
  migrator worker would need a reference to a service's `Infrastructure` project and break ADR 0001.
  Manual `dotnet ef database update` stays a documented fallback.
- Moving to `aspire publish` for k8s manifests later needs a new ADR.
