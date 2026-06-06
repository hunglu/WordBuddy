# WordBuddy — Backend

## Project Overview

WordBuddy is a microservices-based English learning web application built on .NET 8. It serves both children and adults with vocabulary lessons, grammar lessons, daily phrases, and quizzes. Content is delivered as text, images, audio pronunciations, and video clips. This repository contains all backend services; the frontend lives in a separate repository (TBD).

## Domain Concepts

| Concept | Description |
|---|---|
| `User` | Learner or admin account with profile and role |
| `Lesson` | A structured learning unit (vocabulary or grammar) |
| `Vocabulary` | A word entry with definition, examples, and media |
| `Grammar` | A grammar rule with explanations and examples |
| `DailyPhrase` | A phrase surfaced to learners each day |
| `Quiz` | A set of questions testing lesson content |
| `MediaAsset` | Text, image, audio, or video content attached to any domain object |
| `LearnerProgress` | Tracks a user's completion and score across lessons and quizzes |

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core Web API, use controller, use class-based (no top-level statement) |
| Database | SQL Server |
| ORM | Entity Framework Core 8 |
| Authentication | JWT (access + refresh tokens) via Auth0 |
| Testing | xUnit, FluentAssertions, Moq |
| Logging | Serilog (structured; sinks: Console, File, Seq) |
| Caching | `IDistributedCache` (Redis) |
| Distributed tracing | OpenTelemetry + Jaeger |
| Distributed messaging | MassTransit (Azure Service Bus in production) |
| Health checks | ASP.NET Core Health Checks |
| Rate limiting | ASP.NET Core Rate Limiting middleware |
| Circuit breaker | ASP.NET Core Resilience (`Microsoft.Extensions.Http.Resilience`) |
| Containerization | Docker + Docker Compose |
| CI/CD | GitHub Actions |

## Architecture

Clean Architecture with CQRS. Each microservice is independently deployable and follows the same internal layer structure.

```
Presentation  →  Application  →  Domain  →  Infrastructure
(Controllers)    (Commands/       (Entities,  (EF Core, Redis,
                  Queries,         Value        MassTransit,
                  DTOs,            Objects,     HTTP clients,
                  Validators)      Events)      Serilog)
```

**Layer rules:**

- Domain has zero dependencies on other layers or external packages.
- Application depends only on Domain; it defines interfaces that Infrastructure implements.
- Infrastructure implements all I/O: database, cache, messaging, external APIs, media storage.
- Presentation depends on Application only — never directly on Infrastructure.
- Use Controller definition (no minimal endpoint), use class-based instead top-level statement.

## Microservices

| Service | Responsibility |
|---|---|
| `IdentityService` | Registration, login, JWT issuance, refresh token rotation |
| `ContentService` | Lessons, Vocabulary, Grammar, DailyPhrases, MediaAssets |
| `QuizService` | Quiz definitions, question banks, answer evaluation |
| `ProgressService` | LearnerProgress tracking, streaks, completion history |
| `NotificationService` | Push/email notifications for daily phrases and reminders |

## Conventions

### General

- SOLID principles throughout.
- `async`/`await` everywhere — no `.Result`, `.Wait()`, or blocking calls.
- No `null` returns from service or repository methods — use `Result<T>` or `Option<T>`.
- Do not use `var` when the type is not immediately obvious from the right-hand side.
- All public API surface is documented with XML doc comments.

### Result Pattern

All application-layer methods return `Result<T>` (or `Result` for void operations).

```csharp
// Success
return Result<LessonDto>.Success(dto);

// Failure
return Result<LessonDto>.Failure(Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."));
```

`Error` is a typed value object (record with `Code` and `Description`). Never throw exceptions for expected business failures — use `Result`.

### CQRS

- Commands mutate state. Queries read state. Keep them strictly separate.
- Do NOT Use MediatR for dispatching commands and queries, instead using the original in dotnet.
- One handler per command or query.
- Handlers live in the Application layer under `Features/<FeatureName>/`.

```
Features/
  Lessons/
    Commands/
      CreateLesson/
        CreateLessonCommand.cs
        CreateLessonCommandHandler.cs
        CreateLessonCommandValidator.cs
    Queries/
      GetLessonById/
        GetLessonByIdQuery.cs
        GetLessonByIdQueryHandler.cs
  Vocabulary/
    Commands/
      AddVocabulary/
        ...
    Queries/
      GetVocabularyByLesson/
        ...
  Quiz/
    Commands/
      SubmitQuizAnswer/
        ...
    Queries/
      GetQuizById/
        ...
```

### Validation

- FluentValidation for all commands and queries.
- Register a MediatR pipeline behavior that runs validation before the handler.
- Return `Result.Failure` with validation errors — never let `ValidationException` propagate past the pipeline.

### Error Handling

- Global exception middleware catches unhandled exceptions and returns RFC 7807 `ProblemDetails`.
- Expected failures flow through `Result<T>` and are never promoted to exceptions.
- Log unhandled exceptions at `Error` level with full stack trace and correlation ID.

### Logging (Serilog)

- Structured logging only — never use string interpolation in log messages; use message templates.
- Enrich every log entry with: `CorrelationId`, `ServiceName`, `Environment`.
- Log levels: `Debug`, `Information`, `Warning`, `Error`, `Fatal`. Use `Verbose` only for local trace-level diagnostics.
- Never log sensitive data: passwords, tokens, PII, or child user data.
- Add configuration on appsetting.json instead hard code in program.cs file to flexible change.
- Using Microsoft extension logging interface and instanse is serilog.

### Distributed Tracing (OpenTelemetry)

- Instrument all inbound HTTP requests, outbound HTTP calls, EF Core queries, and message bus operations.
- Export traces to Jaeger in development; configure via environment variables for other environments.
- Propagate `traceparent` header across all service boundaries.

### Caching

- Cache reads in query handlers using `IDistributedCache`.
- Cache key pattern: `{ServiceName}:{EntityName}:{Id}` — e.g., `content:lesson:42`.
- Always set an absolute expiry. Do not use sliding expiry by default.
- Invalidate via cache-aside on command success (delete the key after a successful write).
- Cache high-read, low-mutation content aggressively: `Vocabulary`, `Grammar`, `DailyPhrase`.

### Health Checks

Every service exposes:

- `GET /health` — liveness (returns 200 if the process is up).
- `GET /health/ready` — readiness (checks DB, Redis, downstream service dependencies).

### Rate Limiting

- Applied globally via ASP.NET Core Rate Limiting middleware.
- Per-endpoint policies defined in `RateLimitingConfiguration`.
- Return `429 Too Many Requests` with a `Retry-After` header.
- Apply stricter limits on quiz submission and auth endpoints to prevent abuse.

### Authentication & Authorization

- JWT bearer tokens issued by `IdentityService` (backed by Auth0).
- Refresh token rotation persisted in the database.
- Use policy-based authorization — avoid hardcoded role strings in `[Authorize]` attributes.
- Child accounts (`AgeGroup = Child`) are subject to additional content restrictions enforced via authorization policies.

### Media Assets

- `MediaAsset` records store the asset type (`Text`, `Image`, `Audio`, `Video`), a storage URL, and metadata.
- Actual binary files are stored in blob storage (Azure Blob or local volume in development); the database holds only references.
- Audio pronunciation files follow the naming convention: `vocab-{vocabularyId}-{locale}.mp3`.
- Video clips are linked per `Lesson` or `Vocabulary` entry and streamed from the CDN URL stored in `MediaAsset`.

### Distributed Messaging

- MassTransit with in-memory transport for local development; Azure Service Bus in staging and production.
- Published domain events (e.g., `QuizCompletedEvent`, `LessonCompletedEvent`) drive `ProgressService` updates asynchronously.
- Consumers are idempotent — processing the same message twice must not corrupt state.
- Message contracts live in `WordBuddy.Shared.Contracts`.

### Testing

- Unit tests for domain logic and application handlers; mock infrastructure interfaces with Moq.
- Integration tests for API endpoints and database using `WebApplicationFactory` and a real SQL Server (Docker).
- Never mock EF Core — integration tests must hit a real database.
- Test class naming: `{ClassUnderTest}_{Method}_{ExpectedOutcome}`.
- One `[Fact]` or `[Theory]` per logical scenario.
- Cover both child-targeted and adult-targeted content paths where behavior differs.

### Docker / Docker Compose

- Each service has its own multi-stage `Dockerfile`.
- `docker-compose.yml` at repo root orchestrates all services + SQL Server + Redis + Jaeger for local development.
- Never hardcode connection strings — use environment variables or `appsettings.{Environment}.json` (excluded from git).

## Environment Configuration

| File | Purpose |
|---|---|
| `appsettings.json` | Non-secret defaults |
| `appsettings.Development.json` | Local overrides (not in git) |
| `appsettings.Production.json` | Production overrides (not in git) |
| Environment variables | Secrets in CI/CD and containers |

Secrets (connection strings, JWT signing keys, Auth0 credentials, storage account keys) are never committed. Use `dotnet user-secrets` locally.

## Build & Run

```bash
# Restore and build
dotnet restore
dotnet build

# Run a specific service
dotnet run --project src/Services/<ServiceName>/<ServiceName>.Api

# Run all services with Docker Compose
docker compose up --build

# Run tests
dotnet test
```

## Project Structure

```
WordBuddy/
├── src/
│   ├── Services/
│   │   ├── Identity/
│   │   │   ├── Identity.Api/
│   │   │   ├── Identity.Application/
│   │   │   ├── Identity.Domain/
│   │   │   └── Identity.Infrastructure/
│   │   ├── Content/
│   │   │   ├── Content.Api/
│   │   │   ├── Content.Application/       # Lessons, Vocabulary, Grammar, DailyPhrase, MediaAsset features
│   │   │   ├── Content.Domain/
│   │   │   └── Content.Infrastructure/
│   │   ├── Quiz/
│   │   │   ├── Quiz.Api/
│   │   │   ├── Quiz.Application/
│   │   │   ├── Quiz.Domain/
│   │   │   └── Quiz.Infrastructure/
│   │   ├── Progress/
│   │   │   ├── Progress.Api/
│   │   │   ├── Progress.Application/      # LearnerProgress features
│   │   │   ├── Progress.Domain/
│   │   │   └── Progress.Infrastructure/
│   │   └── Notification/
│   │       ├── Notification.Api/
│   │       ├── Notification.Application/
│   │       ├── Notification.Domain/
│   │       └── Notification.Infrastructure/
│   └── Shared/
│       ├── WordBuddy.Shared.Kernel/        # Result<T>, Error, base entity, common interfaces
│       ├── WordBuddy.Shared.Infrastructure/ # Serilog setup, OpenTelemetry, health checks, rate limiting
│       └── WordBuddy.Shared.Contracts/     # MassTransit message contracts for inter-service events
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
5. Push to container registry on merge to `main`.
