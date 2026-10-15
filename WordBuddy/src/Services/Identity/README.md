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
| GET | `/api/auth/admin/unlink-requests` | `AdminOnly` | Pages escalated unlink requests (`?page=`, `?pageSize=`) |
| GET | `/api/auth/admin/support-links?learnerId=` | `AdminOnly` | Pages the links of one learner (`?page=`, `?pageSize=`) |
| GET | `/api/auth/groups/owned` | Bearer | Pages the groups the caller owns, with member counts (`?page=`, `?pageSize=`). Replaces `GET /api/auth/groups` (removed) |
| GET | `/api/auth/groups/joined` | Bearer | Pages the groups the caller joined: name and owner only (`?page=`, `?pageSize=`) |
| GET | `/api/auth/groups/approvals` | Bearer | Pages pending child memberships the caller approves as Primary (`?page=`, `?pageSize=`) |
| GET | `/api/auth/groups/{id}` | Bearer, group owner | Group with `activeMemberCount`, `pendingMemberCount` and `memberLearnerIds` (no member list) |
| GET | `/api/auth/groups/{id}/members` | Bearer, group owner | Pages the pending and active members by public name (`?page=`, `?pageSize=`) |

Every paged route takes `?page=` (default 1, min 1) and `?pageSize=` (default 25, 1-100); other values
return `400`. A page past the end returns `200` with empty `items` and the real `totalCount`. The body is
`{ items, page, pageSize, totalCount }` (`WordBuddy.Shared.Kernel.PagedResult<T>`). The group snapshot is cached
under `identity:group-snapshot:{groupId}`; group commands delete it.

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
