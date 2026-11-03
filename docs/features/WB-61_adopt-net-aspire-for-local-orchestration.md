---
feature: Local orchestration (.NET Aspire)
services: Identity | Content | Quiz | Progress | Notification | UI
audience: Both
state: shipped
last-updated-by: WB-63_fix-aspire-dashboard-traces-and-accept-adr-0007
---

# Local orchestration (.NET Aspire)

Developers start the whole local stack with one command and watch it in one dashboard.

## What it does

`aspire run` (from `WordBuddy/aspire/`) starts SQL Server, RabbitMQ, the 5 APIs and the Vite UI,
with one dashboard for **traces and console logs**. The AppHost is orchestration-only, so ADR 0001
(service independence) stays intact.

| Resource | Where it runs | Port |
|---|---|---|
| SQL Server (5 databases) | container | dynamic host port |
| RabbitMQ (+ management UI) | container | 5672 / 15672 |
| identity, content, quiz, progress, notification | process | 5080 - 5084 |
| ui (Vite) | process | 3000 |

## Rules

- **Child vs adult:** no difference. This is developer tooling with no learner-facing behaviour.
- **Telemetry:** the dashboard shows traces and console logs only. Metrics and structured (OTLP)
  logs are not exported, because `WordBuddy.Shared.Infrastructure` exports traces only.
- **Traces reach the dashboard on every dev machine:** the AppHost copies the injected `OTEL_EXPORTER_OTLP_ENDPOINT` into `OpenTelemetry__OtlpEndpoint` for each API (adds `/v1/traces` for HTTP). This beats a local `appsettings.Development.json` value of `localhost:4317`. Traces show as `WordBuddy.<Service>`, not under the `identity` resource; console logs stay under the resource.
- **Migrations:** each service migrates (and seeds) its own database at startup in `Development`. No
  `dotnet ef` step.
- **Secrets:** Aspire parameters in AppHost user-secrets. Never `.env`, never in git.
- **Do not run with `make up`.** Both bind ports 5080 - 5084, 5672 and 15672.
- No service references the AppHost, and the AppHost has no project reference to a service.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

None.

## Pending changes

- WB-66_fix-aspire-secrets-prompt-and-pin-dev-ports — load user-secrets without a prompt and pin all dev ports (#66)

## Change history

- `WB-61_adopt-net-aspire-for-local-orchestration` — AppHost added; `aspire run` starts the local stack (#61)
- `WB-63_fix-aspire-dashboard-traces-and-accept-adr-0007` — AppHost sets `OpenTelemetry__OtlpEndpoint` so traces reach the dashboard; ADR 0007 accepted (#63)
