# Identity Service

Registration, login, JWT issuance, and refresh token rotation for WordBuddy learner and admin
accounts.

Owns the `User` domain entity and the `AgeGroup` (Child, Adult) enum. Has zero project
references to any other WordBuddy service or shared project — see the root
[`CLAUDE.md`](../../../CLAUDE.md) for the independence model this follows.

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Creates a new account, returns a JWT. Optional `timeZoneId` (IANA id; unknown id → `400`) |
| POST | `/api/auth/login` | Anonymous | Authenticates an existing account, returns a JWT |
| PUT | `/api/auth/profile/time-zone` | Bearer | Sets or clears the caller IANA time zone (`{ "timeZoneId": "Asia/Ho_Chi_Minh" }`); unknown id → `400 User.UnknownTimeZone` |
| PUT | `/api/auth/admin/users/{id}/guardian` | `AdminOnly` | Grants or removes the Guardian role (`{ "isGuardian": true }`); a child account → `400 User.GuardianMustBeAdult`; non-admin → `403` |
| POST | `/api/auth/admin/learner-profiles/republish` | `AdminOnly` | One-off backfill (WB-29): publishes `LearnerProfileUpdated` for every user. Safe to rerun; returns `{ "published": n }` |

### Profile event and Guardian role (WB-29)

- Every register, alias/avatar, time zone and Guardian change stages one `LearnerProfileUpdated` in the
  outbox (same save as the change). Payload: user id, age group, alias, avatar, time zone, guardian flag, time.
  No email. The display name is sent for adults only (`null` for a child).
- The JWT carries `guardian = true` only for accounts with the Guardian role. A changed role shows in a new token
  after the next login.
- Run the republish endpoint once after release so Progress knows the existing users.

Swagger UI: `http://localhost:5080/swagger` (Development only).

## Running standalone

```bash
dotnet run --project WordBuddy.Identity.Api
```

## Running in Docker / Kubernetes

`Dockerfile` lives in this folder; build context is this folder only (see root `CLAUDE.md`'s
independence model — a Docker build restores `WordBuddy.Shared.*` from GitHub Packages via a
BuildKit `--secret`, never `local-nuget-feed/`). Image: `wordbuddy-identity:dev`. In the `k8s/`
manifests: Deployment/Service `identity-api`, database `WordBuddyIdentity`, Ingress path
`/api/auth`. If you bumped a shared package version, run `make publish-shared` before rebuilding
this image.

## Configuration

Copy `WordBuddy.Identity.Api/appsettings.Development.json.example` to
`appsettings.Development.json` (gitignored) to get started locally. It holds:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyIdentity`)
- `Jwt:Secret` / `Jwt:Issuer` / `Jwt:ExpiryMinutes`
- `Messaging:*` — bus settings, validated at startup (full key table: `docs/features/messaging.md`):
  `Transport`, `RabbitMq:{Host,VirtualHost,Username,HeartbeatSeconds,RequestedConnectionTimeoutMs}`,
  `Outbox:{QueryDelayMs,DuplicateDetectionWindowSeconds}`, `Retry:IntervalsMs` (defaults in `appsettings.json`).
  `Messaging:RabbitMq:Username` and `Password` are required outside Development; set the password
  only via user-secrets or env `Messaging__RabbitMq__Password`. `Messaging:Transport = InMemory` is for tests only.
- `Serilog:*` — sinks/levels (Console + rolling file in dev, Console-only + Warning in prod)
- `OpenTelemetry:OtlpEndpoint` — where traces are exported (Jaeger's OTLP/gRPC port,
  `http://localhost:4317`, by default); harmless if nothing is listening there

## Observability

Logging goes through `ILogger<T>` backed by Serilog (via `WordBuddy.Shared.Infrastructure`),
every entry enriched with `CorrelationId` (the current trace id — ties together every log line
from one request, including Serilog's own request-logging summary), `ServiceName`, and
`Environment`. Distributed tracing (ASP.NET Core, outbound HTTP, EF Core) exports via OTLP to
Jaeger — see `WordBuddy.Shared.Infrastructure`'s `Observability` folder for the shared setup
every service uses.

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Identity.Api` | Controllers, `Program.cs`, composition root |
| `WordBuddy.Identity.Application` | Commands/queries, DTOs, validators |
| `WordBuddy.Identity.Domain` | `User` entity, `AgeGroup` enum |
| `WordBuddy.Identity.Infrastructure` | EF Core `IdentityDbContext`, repositories |
| `WordBuddy.Identity.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Identity.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
