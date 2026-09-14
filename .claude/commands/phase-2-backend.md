---
title: Phase 2 — Backend
description: Domain models, EF Core, repositories, use cases, API controllers, migrations, seed data — across all 5 microservices
status: draft
---

## Context

WordBuddy backend is 5 independent microservices (`Identity`, `Content`, `Quiz`, `Progress`,
`Notification`), each following the same clean-architecture shape: `Api → Application →
Domain`, `Infrastructure → Application`. Per Phase 1's independence model, **no service has a
project reference to another service or to a shared project** — the 3 shared libraries built in
Phase 1 (`WordBuddy.Shared.Kernel` for `Result<T>`/`Error`, `WordBuddy.Shared.Infrastructure` for
cross-cutting Serilog/health-check/rate-limiting helpers, `WordBuddy.Shared.Contracts` for
MassTransit message contracts) are consumed via `<PackageReference>` from the local NuGet feed
(dev) / GitHub Packages (CI/Docker), exactly like any third-party package. All code is async,
uses `Result<T>` (never exceptions for
expected failures), has XML doc comments on public methods, and uses hand-rolled
`ICommandHandler`/`IQueryHandler` interfaces — **no MediatR**, per root `CLAUDE.md`. Logging via
`Microsoft.Extensions.Logging` (`ILogger<T>`, backed by Serilog) throughout. Each service's
`Program.cs` uses a class-based entry point (not top-level statements). Database: one shared SQL
Server instance for local dev, with **a separate database per service**
(`WordBuddyIdentity`, `WordBuddyContent`, `WordBuddyQuiz`, `WordBuddyProgress`,
`WordBuddyNotification`) — logical isolation without 5x the local resource cost.

The pattern below is described once per concern and repeats across all 5 services with
service-specific entities/endpoints — don't re-derive it per service, copy the shape.

## Domain ownership per service

| Service | Domain entities | Enums |
|---|---|---|
| Identity | `User` | `AgeGroup` (Child, Adult) |
| Content | `Lesson`, `Vocabulary`, `Grammar`, `DailyPhrase`, `MediaAsset` | `Level`, `LessonType`, `MediaAssetType` |
| Quiz | `Quiz`, `QuizQuestion` | `QuizQuestionType` |
| Progress | `LearnerProgress` | — |
| Notification | (scaffold only this phase — no real entities yet; add `NotificationPreference` when Phase 2 work reaches it) | — |

`Result<T>`/`Error` live once in `WordBuddy.Shared.Kernel`, not duplicated per service.

## Requirements

- [ ] Domain entities created per service (constructor-only setters, `Guid Id`), using
      `Result<T>`/`Error` via a `<PackageReference>` on `WordBuddy.Shared.Kernel` (not a
      project reference)
- [ ] Each service has its own `<Service>DbContext` (EF Core) pointed at its own database
      (`WordBuddy<Service>`) on the shared SQL Server instance
- [ ] All entity configurations via `IEntityTypeConfiguration<T>`
- [ ] Repository interfaces in each service's `Application` layer
- [ ] `ICommandHandler`/`IQueryHandler` use case handlers per service (see the
      `develop-webapi` skill for the exact interface shapes and handler template)
- [ ] FluentValidation on all commands
- [ ] EF Core repository implementations in each service's `Infrastructure` layer
- [ ] Controllers: `AuthController` (Identity), `LessonsController` + `MediaController`
      (Content), `QuizController` (Quiz), `ProgressController` (Progress) — Notification gets
      no controller yet, just the project scaffold
- [ ] JWT bearer auth configured in every service's `Program.cs` (Identity issues tokens;
      Content/Quiz/Progress/Notification validate the same signing key/audience — Auth0
      integration from CLAUDE.md is a later swap-in, not this phase)
- [ ] Swagger with JWT support, one Swagger doc per service
- [ ] `ILogger<T>` injected in controllers, handlers, repositories
- [ ] Class-based `Program.cs` with `ServiceCollectionExtensions`/`WebApplicationExtensions` in
      every service
- [ ] `InitialCreate` migration applied per service's database
- [ ] Seed data: 3 lessons (Content), 1 admin user (Identity)

## Implementation Plan

### Step 1 — Domain entities (per service)

In each `<Service>.Domain`, following the ownership table above:

- Entities: constructor-only setters, `Guid Id`, no public setters
- Enums as listed in the table
- `<PackageReference>` on `WordBuddy.Shared.Kernel` for `Result<T>`/`Error` — don't redefine
  them per service, and don't add it as a `ProjectReference`

Representative example (Content):
- `Lesson`, `Vocabulary`, `Grammar`, `DailyPhrase`, `MediaAsset` entities
- `Level` (Beginner, Intermediate, Advanced), `LessonType` (Vocabulary, Grammar, DailyPhrase),
  `MediaAssetType` (Text, Image, Audio, Video)

### Step 2 — EF Core setup (per service)

In each `<Service>.Infrastructure`:

- Install: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`
- Create `<Service>DbContext` with `DbSet`s for just that service's entities
- `IEntityTypeConfiguration<T>` per entity (string max lengths, cascade rules, unique indexes —
  e.g. Content: cascade `Lesson` → `Vocabulary`/`Grammar`/`DailyPhrase`; Progress: unique index
  on `(UserId, LessonId)`)
- Register in `ServiceCollectionExtensions` with `NoTracking` default
- Connection string per service:
  `Server=(localdb)\mssqllocaldb;Database=WordBuddy<Service>;Trusted_Connection=true` locally,
  overridden by the shared-SQL-Server-in-cluster connection string in Phase 4/5 (same host,
  different `Database=` per service)

### Step 3 — Application layer (per service)

In each `<Service>.Application`:

- Define `ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler<T>`,
  `ICommandHandler<T,TResult>`, `IQueryHandler<T,TResult>` once under `Application/Abstractions/`
  (see the `develop-webapi` skill — same shapes, no MediatR)
- Repository interfaces scoped to that service's entities (e.g. `ILessonRepository`,
  `ILearnerProgressRepository`)
- Use case handlers as plain classes implementing the interfaces above, `HandleAsync`,
  validate-then-act-then-log (see skill template)
- DTOs for all query responses
- FluentValidation validators for every command/query that needs one

Representative handlers per service:
- Identity: `RegisterUserCommand`, `LoginCommand`
- Content: `GetLessonsQuery`, `GetLessonDetailQuery`, `CreateLessonCommand`,
  `UploadMediaCommand`
- Quiz: `GetQuizByIdQuery`, `SubmitQuizAnswerCommand`
- Progress: `RecordProgressCommand`, `GetUserProgressQuery`
- Notification: none yet this phase

### Step 4 — Infrastructure repositories (per service)

Implement each service's repository interfaces against its own `<Service>DbContext`. All
methods async, `AsNoTracking` on queries.

### Step 5 — API layer (per service)

In each `<Service>.Api`:

- Install per-service as needed: `Microsoft.AspNetCore.Authentication.JwtBearer`,
  `Swashbuckle.AspNetCore`; Identity only: `BCrypt.Net-Next`
- Class-based `Program.cs`:
  - `Program` class with static `Main(string[] args)`
  - `ServiceCollectionExtensions`: `AddWordBuddyAuthentication`, `AddWordBuddyDatabase`,
    `AddWordBuddyServices`
  - `WebApplicationExtensions`: `UseWordBuddyMiddleware`
- Controllers (see Requirements table above for which service gets which)
- All actions: async, `IActionResult`, `[Authorize]` except Identity's login/register,
  `ILogger<T>`, XML doc comments
- `appsettings.Development.json`: `ConnectionStrings`, `Jwt`, `Logging`, and (Content only)
  `FileStorage`

### Step 6 — Logging (all services, same conventions)

Apply `ILogger<T>` across all layers, in every service:

- Controllers: Info on success, Warning on not-found, Error on exceptions
- Handlers: Info on entry, Warning on `Result.Failure`
- Repositories: Debug on queries, Warning on null returns
- Seeder (Identity, Content): Info on start/complete, Warning if data already exists
- `appsettings.json`: Warning (prod); `appsettings.Development.json`: Information (dev) — same
  in every service

### Step 7 — Migrations + seed (per service, independently)

Run per service, e.g. for Content:
```bash
dotnet ef migrations add InitialCreate --project src/Services/Content/Content.Infrastructure --startup-project src/Services/Content/Content.Api
dotnet ef database update --project src/Services/Content/Content.Infrastructure --startup-project src/Services/Content/Content.Api
```
Repeat for Identity, Quiz, Progress (Notification has no entities yet this phase).

`DataSeeder` in Content.Infrastructure: 3 lessons with child items.
`DataSeeder` in Identity.Infrastructure: 1 admin user (BCrypt hash).
Call each service's seeder from its own `Main`, Development environment only.

### Step 8 — Docs

Update each service's `README.md` (created in Phase 1) with its real endpoint list and env vars.
Update the root `README.md`'s API overview section with links to each service's Swagger.

### Step 9 — Commit

```bash
git add WordBuddy/
git commit -m "feat: phase 2 — per-service domain/application/infrastructure/api scaffold"
```

## Verification

- `dotnet build src/Services/<Service>/<Service>.sln` for each of the 5 services → 0 errors,
  independently (there's no root solution to build all at once — see Phase 1)
- `dotnet run --project src/Services/Identity/Identity.Api` → Swagger opens; POST
  `/api/auth/login` with the seeded admin returns a JWT
- `dotnet run --project src/Services/Content/Content.Api` → GET `/api/lessons` returns the
  3 seeded lessons (with the Identity-issued JWT in the `Authorization` header)
- `dotnet run --project src/Services/Progress/Progress.Api` and
  `src/Services/Quiz/Quiz.Api` start cleanly and expose Swagger
- All log lines appear in each service's own terminal during requests
