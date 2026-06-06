# WordBuddy

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB%2F2022-CC2927?logo=microsoftsqlserver&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT%20Bearer-000000?logo=jsonwebtokens&logoColor=white)
![Serilog](https://img.shields.io/badge/Logging-Serilog-CC342D)

An English learning platform for children and adults. WordBuddy delivers structured lessons — vocabulary, grammar, and daily phrases — alongside quizzes and rich media (text, images, audio pronunciations, and video clips). This repository is the backend API.

---

## Table of Contents

1. [Features](#features)
2. [Tech Stack](#tech-stack)
3. [Solution Structure](#solution-structure)
4. [Domain Entities](#domain-entities)
5. [API Endpoints](#api-endpoints)
6. [Getting Started](#getting-started)
7. [Environment Configuration](#environment-configuration)
8. [Development Conventions](#development-conventions)
9. [Roadmap](#roadmap)

---

## Features

| Area | What it covers |
|---|---|
| **Vocabulary** | Word entries with definitions, phonetic spellings, example sentences, and audio |
| **Grammar** | Rule explanations with ordered, numbered examples |
| **Daily Phrases** | One highlighted phrase per lesson with meaning and usage context |
| **Quizzes** | Question banks and answer evaluation *(planned — Phase 6)* |
| **Progress tracking** | Per-user, per-lesson completion status and score history |
| **Media** | Text, image, audio, and video assets attached to any lesson or vocabulary entry |
| **Auth** | JWT Bearer — registration, login, token issuance |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core Web API (controller-based) |
| Architecture | Clean Architecture + CQRS (no MediatR) |
| Database | SQL Server — LocalDB for development |
| ORM | Entity Framework Core 8 (no-tracking by default) |
| Authentication | JWT Bearer — HS256, `ClockSkew = Zero` |
| Password hashing | BCrypt.Net-Next (work factor 12) |
| Validation | FluentValidation 11 |
| Logging | Serilog (Console + rolling File sinks), `ILogger<T>` interface throughout |
| API docs | Swashbuckle / Swagger UI (development only) |
| Testing | xUnit + FluentAssertions + Moq *(planned)* |

---

## Solution Structure

```
WordBuddy/
├── src/
│   ├── WordBuddy.Domain/               # Zero external dependencies
│   │   ├── Common/                     # Result<T>, Error, base entity types
│   │   ├── Entities/                   # All domain entities
│   │   └── Enums/                      # LessonType, Level, AgeGroup, TargetAgeGroup
│   │
│   ├── WordBuddy.Application/          # Use cases; depends on Domain only
│   │   ├── DTOs/                       # LessonDto, UserDto, AuthTokenDto, LearnerProgressDto, …
│   │   ├── Extensions/                 # AddApplication() — handler + validator DI registration
│   │   ├── Interfaces/                 # ILessonRepository, ILearnerProgressRepository, IAuthService
│   │   └── Features/
│   │       ├── Lessons/
│   │       │   ├── Commands/
│   │       │   │   └── CreateLesson/   # Command · Handler · Validator
│   │       │   └── Queries/
│   │       │       ├── GetLessons/     # Query · Handler · Validator
│   │       │       └── GetLessonDetail/# Query · Handler · Validator
│   │       └── Progress/
│   │           ├── Commands/
│   │           │   └── RecordProgress/ # Command · Handler · Validator (upsert)
│   │           └── Queries/
│   │               └── GetUserProgress/# Query · Handler · Validator
│   │
│   ├── WordBuddy.Infrastructure/       # All I/O; depends on Application + Domain
│   │   ├── Extensions/                 # AddInfrastructure() — EF Core + repository DI registration
│   │   ├── Persistence/
│   │   │   ├── WordBuddyDbContext.cs
│   │   │   ├── WordBuddyDbContextFactory.cs  # Design-time factory for EF migrations
│   │   │   └── Repositories/          # LessonRepository, LearnerProgressRepository
│   │   ├── Seeding/                   # DataSeeder — migrations + dev seed data on startup
│   │   └── Services/                  # AuthService (BCrypt verification, user persistence)
│   │
│   └── WordBuddy.API/                  # Presentation; depends on Application only
│       ├── Controllers/               # AuthController, LessonsController, ProgressController
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs  # AddWordBuddyAuthentication/Database/Services
│       │   └── WebApplicationExtensions.cs     # ConfigureWordBuddyLogging, UseWordBuddyMiddleware
│       ├── Models/Requests/           # CreateLessonRequest, RegisterUserRequest, LoginRequest, …
│       ├── Services/                  # JwtTokenGenerator
│       ├── Settings/                  # JwtSettings (bound from config)
│       ├── appsettings.json           # Production-safe defaults (Warning log level)
│       └── appsettings.Development.json  # Local overrides — not committed to git
│
├── tests/                              # Planned — see Roadmap
├── CLAUDE.md                           # Architecture decisions and coding standards
└── WordBuddy.slnx                      # Visual Studio solution file (.slnx format)
```

---

## Domain Entities

| Entity | Description |
|---|---|
| `User` | Learner or admin account with email, BCrypt-hashed password, display name, age group (`Child` / `Adult`), and level |
| `Lesson` | A structured learning unit with type (`Vocabulary` / `Grammar` / `DailyPhrase`), difficulty level, target age group, published flag, and display order |
| `VocabularyItem` | A word entry belonging to a `Vocabulary` lesson; stores definition, example sentence, phonetic spelling, and optional media asset IDs |
| `GrammarRule` | A single grammar rule within a `Grammar` lesson; has an explanation, display order, and a list of example sentences |
| `DailyPhrase` | A featured phrase within a `DailyPhrase` lesson; stores meaning, usage context, and optional audio/video asset IDs |
| `MediaAsset` | A reference record (type, storage URL, metadata) pointing to binary content stored in blob storage; the database holds only the reference |
| `LearnerProgress` | Tracks one user's completion status and score percentage for one lesson; upserted on every lesson or quiz interaction |

---

## API Endpoints

### Auth — `/api/auth`

| Method | Route | Auth required | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | No | Create a new account; returns a signed JWT token |
| `POST` | `/api/auth/login` | No | Authenticate with email + password; returns a signed JWT token |

**Register request body**

```json
{
  "email": "learner@example.com",
  "password": "Str0ng!Pass",
  "displayName": "Alex",
  "ageGroup": "Adult"
}
```

**Login request body**

```json
{
  "email": "learner@example.com",
  "password": "Str0ng!Pass"
}
```

**Token response**

```json
{
  "token": "<JWT>",
  "expiresAt": "2026-06-07T10:00:00Z",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "displayName": "Alex",
  "email": "learner@example.com"
}
```

---

### Lessons — `/api/lessons`

All routes require `Authorization: Bearer <token>`.

| Method | Route | Auth required | Description |
|---|---|---|---|
| `GET` | `/api/lessons` | Yes | List published lessons; optional query params `type`, `level`, `ageGroup` |
| `GET` | `/api/lessons/{id}` | Yes | Full lesson detail including vocabulary items, grammar rules, and daily phrases |
| `POST` | `/api/lessons` | Yes | Create a new lesson (admin use) |

**GET `/api/lessons` query parameters**

| Param | Type | Values |
|---|---|---|
| `type` | `LessonType` | `Vocabulary`, `Grammar`, `DailyPhrase` |
| `level` | `Level` | `Beginner`, `Intermediate`, `Advanced` |
| `ageGroup` | `AgeGroup` | `Child`, `Adult` |

**POST `/api/lessons` request body**

```json
{
  "title": "Animals",
  "description": "Learn common English animal vocabulary.",
  "type": "Vocabulary",
  "level": "Beginner",
  "targetAgeGroup": "Both",
  "orderIndex": 1
}
```

---

### Progress — `/api/progress`

All routes require `Authorization: Bearer <token>`. The user identity is read from the JWT `sub` claim — the body never takes a `userId`.

| Method | Route | Auth required | Description |
|---|---|---|---|
| `POST` | `/api/progress` | Yes | Record or update completion and score for a lesson (upsert) |
| `GET` | `/api/progress` | Yes | Retrieve all progress records for the authenticated user |

**POST `/api/progress` request body**

```json
{
  "lessonId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isCompleted": true,
  "scorePercent": 85
}
```

---

## Getting Started

### Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.x | Required to build and run the API |
| SQL Server LocalDB | Any recent | Ships with Visual Studio; install [separately](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) if needed |
| [Node.js](https://nodejs.org/) | 18+ LTS | Required by the Claude Code CLI |
| [Claude Code](https://claude.ai/code) | Latest | Optional but recommended for AI-assisted development |

### Clone and build

```powershell
git clone <repo-url>
cd WordBuddy
dotnet restore
dotnet build
```

### Set up `appsettings.Development.json`

This file is excluded from git. Create it at `src/WordBuddy.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WordBuddy;Trusted_Connection=true"
  },
  "Jwt": {
    "Secret": "replace-with-a-random-string-of-at-least-32-characters",
    "Issuer": "WordBuddy",
    "ExpiryMinutes": 60
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "System": "Warning",
        "WordBuddy.Infrastructure.Persistence.Repositories": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": { "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}" }
      },
      {
        "Name": "File",
        "Args": { "path": "logs/wordbuddy-.log", "rollingInterval": "Day" }
      }
    ],
    "Enrich": [ "FromLogContext" ]
  }
}
```

> **Never commit this file.** It is in `.gitignore`. Use `dotnet user-secrets` for shared or CI environments.

### Run EF Core migrations

Run from the solution root. The `WordBuddyDbContextFactory` in Infrastructure lets `dotnet ef` create the `DbContext` without starting the full API host (which requires JWT configuration).

```powershell
dotnet ef migrations add InitialCreate `
  --project src\WordBuddy.Infrastructure `
  --startup-project src\WordBuddy.API

dotnet ef database update `
  --project src\WordBuddy.Infrastructure `
  --startup-project src\WordBuddy.API
```

The database is created at this step. Sample data (3 lessons, 11 content items, 1 admin user) is seeded automatically on first API startup in the Development environment.

### Run the API

```powershell
dotnet run --project src\WordBuddy.API
```

### Open Swagger

Navigate to `https://localhost:{port}/swagger`. The port is printed on startup. Click **Authorize** and paste the JWT token from `POST /api/auth/login` to call protected endpoints.

**Seed admin credentials**

| Field | Value |
|---|---|
| Email | `admin@wordbuddy.com` |
| Password | `Admin@123` |

---

## Environment Configuration

### `appsettings.json` — production-safe defaults

| Key | Type | Description |
|---|---|---|
| `ConnectionStrings.DefaultConnection` | string | SQL Server connection string |
| `Jwt.Secret` | string | HS256 signing secret — **never commit**; must be ≥ 32 characters |
| `Jwt.Issuer` | string | Value placed in the `iss` claim of every issued token |
| `Jwt.ExpiryMinutes` | int | Token lifetime in minutes (default: `60`) |
| `Serilog.MinimumLevel.Default` | string | Baseline log level; `Warning` in production keeps log volume low |
| `Serilog.MinimumLevel.Override.*` | string | Per-namespace overrides (e.g., suppress verbose Microsoft internals) |
| `Serilog.WriteTo` | array | Sink list — Console and rolling File by default |
| `Logging.LogLevel.Default` | string | ASP.NET Core host level before Serilog initialises; `Warning` in production |
| `AllowedHosts` | string | Kestrel host filter; `*` in development |

### `appsettings.Development.json` — local overrides

| Key | Recommended value | Why |
|---|---|---|
| `ConnectionStrings.DefaultConnection` | `Server=(localdb)\\mssqllocaldb;...` | No separate SQL Server install needed |
| `Jwt.Secret` | Any 32+ character string | Local only; never shared |
| `Serilog.MinimumLevel.Default` | `Information` | See handler entry logs, seeder output |
| `WordBuddy.Infrastructure.Persistence.Repositories` override | `Debug` | See per-query repository log entries |

---

## Development Conventions

### Clean Architecture rules

- **Domain** — zero dependencies on any other layer or external NuGet package.
- **Application** — depends on Domain only; declares interfaces (`ILessonRepository`, `IAuthService`, …) that Infrastructure implements.
- **Infrastructure** — implements all I/O (EF Core, auth, seeding); never referenced directly by Presentation.
- **API** — depends on Application only; `Program.cs` is the single composition root allowed to reference Infrastructure.

### Result\<T\> pattern

Application and infrastructure methods never return `null` and never throw for expected business failures. All service and handler methods return `Result<T>` (with data) or `Result` (void).

```csharp
// Success
return Result<LessonDto>.Success(dto);

// Expected failure — flows back through Result, never thrown
return Result<LessonDto>.Failure(
    Error.NotFound("Lesson.NotFound", $"Lesson {id} was not found."));
```

`Error` is a sealed record with a `Code` (dot-namespaced string) and a human-readable `Description`. Callers branch on `Code` — for example, `RecordProgressCommandHandler` checks for `"LearnerProgress.NotFound"` to distinguish a first-access upsert from a real infrastructure failure.

### Logging pattern

Use `ILogger<T>` (the Microsoft interface — Serilog is the registered provider). Always use structured message templates; never string interpolation.

```csharp
// Correct — property name preserved in structured log
_logger.LogInformation("Fetching Lesson by Id={LessonId}", id);

// Wrong — property name lost, defeats structured logging
_logger.LogInformation($"Fetching Lesson by Id={id}");
```

| Level | When to use |
|---|---|
| `Debug` | Repository queries — high volume, off by default in production |
| `Information` | Handler entry points, seeder lifecycle |
| `Warning` | `Result.Failure` returned; entity not found; seed skip |
| `Error` | Unhandled exceptions caught by global middleware |

Never log passwords, JWT tokens, PII, or any child user data.

### Async/await rules

- All I/O is `async`/`await` end-to-end — no `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Every repository and service method accepts `CancellationToken ct = default` and forwards it to every EF Core and HTTP call.
- `var` is used only when the type is immediately obvious from the right-hand side.

### CQRS without MediatR

Commands mutate state; queries read state. Each is a plain `record` and has exactly one handler class with a `HandleAsync(TCommand, CancellationToken)` method. Handlers are registered directly with the DI container and resolved by controllers — no pipeline infrastructure required.

---

## Roadmap

### Phase 1 — Core backend ✅ complete

Domain entities · Clean Architecture layer separation · CQRS handlers and validators (FluentValidation) · EF Core repositories with no-tracking queries · BCrypt authentication · JWT token generation · Swagger UI · structured Serilog logging · development data seeder.

### Phase 2 — Hardening (planned)

Refresh token rotation persisted in the database · Redis `IDistributedCache` for high-read lesson and vocabulary content · cache-aside invalidation on writes · circuit breaker via `Microsoft.Extensions.Http.Resilience` · health check endpoints (`GET /health`, `GET /health/ready`) · global rate limiting middleware with `429 Too Many Requests` and `Retry-After` headers.

### Phase 3 — Frontend (planned)

A separate repository for the learner-facing UI. Technology choice (React / Blazor) to be confirmed. Will consume the REST API with JWT Bearer auth and render lesson content including audio and video media. Child accounts will see age-appropriate content enforced by the `TargetAgeGroup` filter.

### Phase 4 — Infrastructure expansion (planned)

Docker + Docker Compose for local orchestration (SQL Server, Redis, Jaeger) · OpenTelemetry distributed tracing exported to Jaeger · MassTransit with Azure Service Bus for domain events (`LessonCompletedEvent`, `QuizCompletedEvent`) · idempotent message consumers in `ProgressService`.

### Phase 5 — Deployment pipeline (planned)

GitHub Actions CI/CD: restore → build → unit tests → integration tests against a real SQL Server container → Docker image build → push to container registry on merge to `main`. Environment-specific secrets via GitHub environments and repository secrets.

### Phase 6 — Iteration and maintenance (ongoing)

Quiz engine (question banks, answer evaluation, per-attempt score persistence) · `NotificationService` for daily phrase push notifications and email reminders · progress gamification (streaks, badges) · content administration tooling · performance tuning based on production telemetry.
