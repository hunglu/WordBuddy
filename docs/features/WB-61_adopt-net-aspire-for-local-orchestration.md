---
feature: Local orchestration (.NET Aspire)
services: Identity | Content | Quiz | Progress | Notification | UI
audience: Both
state: shipped
last-updated-by: WB-66_fix-aspire-secrets-prompt-and-pin-dev-ports
---

# Local orchestration (.NET Aspire)

Developers start the whole local stack with one command and watch it in one dashboard.

## What it does

`aspire run` (from `WordBuddy/aspire/`) starts SQL Server, RabbitMQ, the 5 APIs and the Vite UI,
with one dashboard for **traces and console logs**. The AppHost is orchestration-only, so ADR 0001
(service independence) stays intact.

| Resource | Where it runs | Port |
|---|---|---|
| SQL Server (5 databases) | container | 14330 (not 1433, so it never blocks `make up`) |
| RabbitMQ (+ management UI) | container | 5672 / 15672 |
| identity, content, quiz, progress, notification | process | 5080 - 5084 |
| ui (Vite) | process | 3000 |
| Aspire dashboard | process | 15180 (OTLP 19180, resource service 20180) |

## Rules

- **Child vs adult:** no difference. This is developer tooling with no learner-facing behaviour.
- **Telemetry:** the dashboard shows traces and console logs only. Metrics and structured (OTLP)
  logs are not exported, because `WordBuddy.Shared.Infrastructure` exports traces only.
- **Traces reach the dashboard on every dev machine:** the AppHost copies the injected `OTEL_EXPORTER_OTLP_ENDPOINT` into `OpenTelemetry__OtlpEndpoint` for each API (adds `/v1/traces` for HTTP). This beats a local `appsettings.Development.json` value of `localhost:4317`. Traces show as `WordBuddy.<Service>`, not under the `identity` resource; console logs stay under the resource.
- **Migrations:** each service migrates (and seeds) its own database at startup in `Development`. No
  `dotnet ef` step.
- **Secrets:** Aspire parameters in AppHost user-secrets. Never `.env`, never in git. Non-secret
  defaults (`jwt-issuer`, `jwt-expiry-minutes`, `rabbitmq-user`) come from the AppHost `appsettings.json`;
  user-secrets override them.
- **Development profile:** `Properties/launchSettings.json` runs the AppHost in `Development`, so
  user-secrets load and the dashboard shows no secret prompt. Without it the AppHost runs as `Production`.
- **Password change = volume reset:** the SQL Server and RabbitMQ volumes keep the old password. After
  changing a secret, reset those volumes by hand, or logins fail and the APIs do not start.
- **Integration tests** use throwaway containers with a dynamic SQL host port, so a persistent `sql`
  container on 14330 does not block them.
- **Do not run with `make up`.** Both bind ports 5080 - 5084, 5672 and 15672.
- No service references the AppHost, and the AppHost has no project reference to a service.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

None.

## Pending changes


## Change history

- `WB-61_adopt-net-aspire-for-local-orchestration` — AppHost added; `aspire run` starts the local stack (#61)
- `WB-63_fix-aspire-dashboard-traces-and-accept-adr-0007` — AppHost sets `OpenTelemetry__OtlpEndpoint` so traces reach the dashboard; ADR 0007 accepted (#63)
- `WB-66_fix-aspire-secrets-prompt-and-pin-dev-ports` — AppHost runs in `Development` (user-secrets load, no prompt); SQL pinned to 14330, dashboard to 15180 (#66)
