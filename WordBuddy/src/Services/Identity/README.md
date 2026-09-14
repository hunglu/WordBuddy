# Identity Service

Registration, login, JWT issuance, and refresh token rotation for WordBuddy learner and admin
accounts.

Owns the `User` domain entity and the `AgeGroup` (Child, Adult) enum. Has zero project
references to any other WordBuddy service or shared project — see the root
[`CLAUDE.md`](../../../CLAUDE.md) for the independence model this follows.

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | Creates a new account, returns a JWT |
| POST | `/api/auth/login` | Anonymous | Authenticates an existing account, returns a JWT |

Swagger UI: `http://localhost:5080/swagger` (Development only).

## Running standalone

```bash
dotnet run --project WordBuddy.Identity.Api
```

## Configuration

Copy `WordBuddy.Identity.Api/appsettings.Development.json.example` to
`appsettings.Development.json` (gitignored) to get started locally. It holds:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyIdentity`)
- `Jwt:Secret` / `Jwt:Issuer` / `Jwt:ExpiryMinutes`
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
