# WordBuddy — Backend

## Project Overview

WordBuddy is a microservices-based application Serve for English Study built on .NET 8. This repository contains the backend services. The frontend lives in a separate GitHub repository (TBD).

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core Web API |
| Database | SQL Server |
| ORM | Entity Framework Core 8 |
| Authentication | JWT (access + refresh tokens) |
| Testing | xUnit, FluentAssertions, Moq |
| Logging | Serilog (structured, sinks: Console, File, Seq) |
| Caching | IDistributedCache (Redis) |
| Distributed tracing | OpenTelemetry + Jaeger |
| Distributed messaging | TBD (MassTransit / Azure Service Bus) |
| Health checks | ASP.NET Core Health Checks |
| Rate limiting | ASP.NET Core Rate Limiting middleware |
| Circuit breaker | ASP.NET Core Circuit Breaker middleware |
| Containerization | Docker + Docker Compose |
| CI/CD | GitHub Actions |

## Architecture

Clean Architecture with CQRS. Each microservice is independently deployable and follows the same internal layer structure.

```
Presentation  →  Application  →  Domain  →  Infrastructure
(Controllers)    (Commands/       (Entities,  (EF Core, Redis,
                  Queries,         Value        HTTP clients,
                  DTOs)            Objects,     Serilog)
                                   Events)
```

**Key rules:**

- Domain layer has zero dependencies on other layers or external packages.
- Application layer depends only on Domain. It defines interfaces that Infrastructure implements.
- Infrastructure implements all I/O: database, cache, messaging, external APIs.
- Presentation layer depends on Application only (never Infrastructure directly).

## Conventions

### General

- SOLID principles throughout.
- `async`/`await` everywhere — no `.Result`, `.Wait()`, or blocking calls.
- No `null` returns from service/repository methods — use `Result<T>` or `Option<T>`.
- All public API surface is documented with XML doc comments.

### Result Pattern

All application-layer methods return `Result<T>` (or `Result` for void operations).

```csharp
// Success
return Result<UserDto>.Success(dto);

// Failure
return Result<UserDto>.Failure(Error.NotFound("User.NotFound", $"User {id} was not found."));
```

Errors are typed value objects (`Error` record with `Code` and `Description`). Never throw exceptions for expected business failures — use `Result`.

### CQRS

- Commands mutate state, queries read state. Keep them separate.
- Use MediatR for dispatching.
- One handler per command/query.
- Handlers live in the Application layer under `Features/<FeatureName>/`.

```
Features/
  Users/
    Commands/
      CreateUser/
        CreateUserCommand.cs
        CreateUserCommandHandler.cs
        CreateUserCommandValidator.cs
    Queries/
      GetUserById/
        GetUserByIdQuery.cs
        GetUserByIdQueryHandler.cs
```

### Validation

- FluentValidation for all commands/queries.
- Register a MediatR pipeline behavior that runs validation before the handler.
- Return `Result.Failure` with validation errors — never throw `ValidationException` past the pipeline.

### Error Handling

- Global exception middleware catches unhandled exceptions and returns RFC 7807 `ProblemDetails`.
- Expected failures flow through `Result<T>` — they never become exceptions.
- Log unhandled exceptions at `Error` level with full stack trace and correlation ID.

### Logging (Serilog)

- Structured logging only — no string interpolation in log messages, use message templates.
- Enrich every log entry with: `CorrelationId`, `ServiceName`, `Environment`.
- Log levels: `Verbose` (trace-level dev only), `Debug`, `Information`, `Warning`, `Error`, `Fatal`.
- Do not log sensitive data (passwords, tokens, PII).

### Distributed Tracing (OpenTelemetry)

- Instrument all inbound HTTP requests, outbound HTTP calls, EF Core queries, and message bus operations.
- Export traces to Jaeger in development; configure via environment variables for other environments.
- Propagate `traceparent` header across service boundaries.

### Caching

- Cache reads in query handlers using `IDistributedCache`.
- Cache keys follow the pattern: `{ServiceName}:{EntityName}:{Id}`.
- Always set an absolute expiry. Do not use sliding expiry by default.
- Invalidate on write via cache-aside pattern (delete on command success).

### Health Checks

Every service exposes:

- `GET /health` — liveness (returns 200 if the process is up).
- `GET /health/ready` — readiness (checks DB, cache, downstream dependencies).

### Rate Limiting

- Applied globally via ASP.NET Core Rate Limiting middleware.
- Per-endpoint policies defined in `RateLimitingConfiguration`.
- Return `429 Too Many Requests` with `Retry-After` header.

### Authentication & Authorization

- JWT bearer tokens issued by an identity service (using Auth0 as 3rd-Party).
- Refresh token rotation stored in the database.
- Use policy-based authorization — avoid `[Authorize(Roles = "...")]` strings.

### Testing

- Unit tests for domain logic and application handlers (mock infrastructure interfaces).
- Integration tests for API endpoints and database (use `WebApplicationFactory`, real SQL Server via Docker).
- No mocking EF Core — use a real database for integration tests (learned from prior incidents).
- Test class naming: `{ClassUnderTest}_{Method}_{ExpectedOutcome}`.
- One `[Fact]` / `[Theory]` per logical scenario.

### Docker / Docker Compose

- Each service has its own `Dockerfile` (multi-stage build).
- `docker-compose.yml` at repo root orchestrates all services + SQL Server + Redis + Jaeger for local development.
- Never hardcode connection strings — use environment variables or `appsettings.{Environment}.json` (excluded from git).

## Environment Configuration

| File | Purpose |
|---|---|
| `appsettings.json` | Non-secret defaults |
| `appsettings.Development.json` | Local overrides (not in git) |
| `appsettings.Production.json` | Prod overrides (not in git) |
| Environment variables | Secrets in CI/CD and containers |

Secrets (connection strings, JWT signing keys) are never committed. Use `dotnet user-secrets` locally.

Apply User Secrets technique support by IDE Visual Studio and Dotnet 8 framework.

## Build & Run

```bash
# Restore and build
dotnet restore
dotnet build

# Run a specific service (replace <ServiceName>)
dotnet run --project src/Services/<ServiceName>/<ServiceName>.Api

# Run all services with Docker Compose
docker compose up --build

# Run tests
dotnet test
```

## Project Structure

> Fill in as services and shared libraries are added.

```
WordBuddy/
├── src/
│   ├── Services/               # Individual microservices (one folder per service)
│   │   └── <ServiceName>/
│   │       ├── <ServiceName>.Api/          # ASP.NET Core Web API (Presentation)
│   │       ├── <ServiceName>.Application/  # CQRS handlers, DTOs, interfaces
│   │       ├── <ServiceName>.Domain/       # Entities, value objects, domain events
│   │       └── <ServiceName>.Infrastructure/ # EF Core, Redis, HTTP clients
│   └── Shared/                 # Cross-cutting shared libraries
│       ├── WordBuddy.Shared.Kernel/        # Result<T>, Error, base entity, common interfaces
│       ├── WordBuddy.Shared.Infrastructure/ # Serilog setup, OpenTelemetry, health checks
│       └── WordBuddy.Shared.Contracts/     # Shared message contracts for inter-service communication
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
├── docker-compose.yml
├── docker-compose.override.yml
├── .github/
│   └── workflows/
├── .dockerignore
├── .gitignore
└── CLAUDE.md
```

## CI/CD (GitHub Actions)

Pipelines live in `.github/workflows/`. Standard pipeline per service:

1. Restore & build.
2. Run unit tests.
3. Run integration tests (spin up SQL Server + Redis containers).
4. Build Docker image.
5. Push to container registry (on merge to `main`).
