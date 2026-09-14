---
title: Phase 2 — Backend
description: Domain models, EF Core, repositories, use cases, API controllers, migrations, seed data
status: draft
---

## Context

WordBuddy backend uses clean architecture:
API → Application → Domain, Infrastructure → Application.
All code is async, uses Result<T>, has XML doc comments on public methods.
Logging via Microsoft.Extensions.Logging throughout.
Program.cs uses class-based entry point (not top-level statements).

## Requirements

- [ ] Domain entities created with constructor-only setters
- [ ] Result<T> and Error value object in Domain
- [ ] EF Core configured with WordBuddyDbContext
- [ ] All entity configurations via IEntityTypeConfiguration<T>
- [ ] Repository interfaces in Application
- [ ] Use case handlers (query/command) in Application
- [ ] FluentValidation on all commands
- [ ] EF Core repository implementations in Infrastructure
- [ ] AuthController, LessonsController, ProgressController, MediaController
- [ ] JWT auth configured in Program.cs
- [ ] Swagger with JWT support
- [ ] ILogger<T> injected in controllers, handlers, repositories
- [ ] Class-based Program.cs with ServiceCollectionExtensions
- [ ] InitialCreate migration applied
- [ ] Seed data: 3 lessons, admin user

## Implementation Plan

### Step 1 — Domain entities

In WordBuddy.Domain create:

- Enums: AgeGroup (Child, Adult), Level (Beginner, Intermediate, Advanced),
  LessonType (Vocabulary, Grammar, DailyPhrase), MediaType (Image, Audio, Video)
- Entities: User, Lesson, VocabularyItem, GrammarRule, DailyPhrase,
  MediaAsset, LearnerProgress
- All entities: Guid Id, no public setters, constructors only
- Result<T> class with Success/Failure factory methods
- Error value object with Code + Message

### Step 2 — EF Core setup

In WordBuddy.Infrastructure:

- Install: Microsoft.EntityFrameworkCore.SqlServer,
  Microsoft.EntityFrameworkCore.Tools
- Create WordBuddyDbContext with DbSets for all entities
- Create IEntityTypeConfiguration<T> for each entity:
  - String max lengths: Word/Phrase = 200, Definition/Explanation = 2000
  - Cascade delete: Lesson → VocabularyItems, GrammarRules, DailyPhrases
  - Unique index: LearnerProgress (UserId, LessonId)
  - GrammarRule.Examples as JSON column
- Register in ServiceCollectionExtensions with NoTracking default
- Connection string: Server=(localdb)\mssqllocaldb;Database=WordBuddy;Trusted_Connection=true

### Step 3 — Application layer

In WordBuddy.Application:

- Generic IRepository<T> with CRUD async methods
- ILessonRepository, ILearnerProgressRepository, IMediaAssetRepository
- Use cases (plain handler classes with HandleAsync, no MediatR):
  - GetLessonsQuery, GetLessonDetailQuery, CreateLessonCommand
  - RecordProgressCommand, GetUserProgressQuery
  - UploadMediaCommand
- DTOs for all query responses
- Install FluentValidation, create validators for all commands

### Step 4 — Infrastructure repositories

Implement ILessonRepository and ILearnerProgressRepository
using WordBuddyDbContext. All methods async, AsNoTracking on queries.

### Step 5 — API layer

In WordBuddy.API:

- Install: Microsoft.AspNetCore.Authentication.JwtBearer, Swashbuckle.AspNetCore,
  BCrypt.Net-Next
- Class-based Program.cs:
  - Program class with static Main(string[] args)
  - ServiceCollectionExtensions: AddWordBuddyAuthentication,
    AddWordBuddyDatabase, AddWordBuddyServices
  - WebApplicationExtensions: UseWordBuddyMiddleware
- Controllers:
  - AuthController: POST /api/auth/register, POST /api/auth/login
  - LessonsController: GET /api/lessons, GET /api/lessons/{id}
  - ProgressController: POST /api/progress, GET /api/progress
  - MediaController: POST /api/media/upload, GET /api/media/{id}
- All actions: async, IActionResult, [Authorize] except login/register,
  ILogger<T>, XML doc comments
- appsettings.Development.json: ConnectionStrings, Jwt, Logging, FileStorage

### Step 6 — Logging

Apply ILogger<T> across all layers:

- Controllers: Info on success, Warning on not-found, Error on exceptions
- Handlers: Info on entry, Warning on Result.Failure
- Repositories: Debug on queries, Warning on null returns
- Seeder: Info on start/complete, Warning if data exists
- appsettings.json: Warning (prod), appsettings.Development.json: Information (dev)

### Step 7 — Migration + seed

```batch
dotnet ef migrations add InitialCreate
--project src/WordBuddy.Infrastructure
--startup-project src/WordBuddy.API

dotnet ef database update
--project src/WordBuddy.Infrastructure
--startup-project src/WordBuddy.API
```

DataSeeder in Infrastructure: 3 lessons with child items, 1 admin user (BCrypt hash).
Call seeder from Main in Development environment only.

### Step 8 — Docs

Update README.md: solution structure, API endpoints table,
environment variables table, getting started steps.

### Step 9 — Commit

`git add .`
`git commit -m "feat: phase 2 — backend scaffold with auth, lessons, media, logging"`

## Verification

- `dotnet build` → 0 errors
- `dotnet run --project src/WordBuddy.API` → Swagger opens at <https://localhost:5001/swagger>
- POST /api/auth/login with seeded admin returns JWT
- GET /api/lessons returns 3 seeded lessons
- All log lines appear in terminal during requests
