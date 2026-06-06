# Backend

## Step 1: Scaffold the solution structure

Create a .NET 8 clean architecture solution called WordBuddy with these projects:

- WordBuddy.API (ASP.NET Core Web API)
- WordBuddy.Application (class library)
- WordBuddy.Domain (class library)
- WordBuddy.Infrastructure (class library)
- WordBuddy.Tests (xUnit test project)

Wire them with correct project references (API → Application → Domain, Infrastructure → Application).
Use dotnet CLI commands I can run in PowerShell on Windows.
Do not generate any code yet — just the solution and project scaffold with correct references.

## Step 2: Define the domain models

In WordBuddy.Domain, create these entities with proper C# conventions,
XML doc comments, nullable reference types enabled, no setters (use constructors):

1. User
   - Id (Guid), Email, PasswordHash, DisplayName
   - AgeGroup (enum: Child, Adult)
   - Level (enum: Beginner, Intermediate, Advanced)
   - CreatedAt, UpdatedAt

2. Lesson
   - Id (Guid), Title, Description
   - Type (enum: Vocabulary, Grammar, DailyPhrase)
   - Level (enum: Beginner, Intermediate, Advanced)
   - TargetAgeGroup (enum: Child, Adult, Both)
   - IsPublished (bool), OrderIndex (int)
   - CreatedAt

3. VocabularyItem
   - Id (Guid), LessonId (Guid)
   - Word, Definition, ExampleSentence
   - PhoneticSpelling (nullable)
   - ImageAssetId (Guid, nullable), AudioAssetId (Guid, nullable)

4. GrammarRule
   - Id (Guid), LessonId (Guid)
   - RuleName, Explanation, OrderIndex
   - Examples (`List<string>`)

5. DailyPhrase
   - Id (Guid), LessonId (Guid)
   - Phrase, Meaning, UsageContext
   - AudioAssetId (Guid, nullable), VideoAssetId (Guid, nullable)

6. MediaAsset
   - Id (Guid), FileName, StorageUrl
   - Type (enum: Image, Audio, Video)
   - FileSizeBytes (long), MimeType
   - UploadedAt

7. LearnerProgress
   - Id (Guid), UserId (Guid), LessonId (Guid)
   - IsCompleted (bool), ScorePercent (int, nullable)
   - LastAccessedAt, CompletedAt (nullable)

Also create a `Result<T>` base class in Domain with Success/Failure factory methods and an Error value object.

---

## Step 3: EF Core + database setup

In WordBuddy.Infrastructure:

1. Install these NuGet packages (give me dotnet CLI commands):
   - Microsoft.EntityFrameworkCore.SqlServer
   - Microsoft.EntityFrameworkCore.Tools
   - Microsoft.Extensions.Configuration

2. Create WordBuddyDbContext with DbSets for all domain entities.

3. Create EF Core configurations (`IEntityTypeConfiguration<T>`) for each entity:
   - All primary keys as Guid
   - Required strings with max lengths: Word/Phrase 200, Definition/Explanation 2000
   - Relationships: Lesson → VocabularyItems, GrammarRules, DailyPhrases (one-to-many, cascade delete)
   - LearnerProgress: composite unique index on (UserId, LessonId)
   - GrammarRule.Examples stored as JSON column

4. Register DbContext in a ServiceCollectionExtensions class
   with UseQueryTrackingBehavior(NoTracking) as default.

Connection string placeholder: "Server=(localdb)\\mssqllocaldb;Database=WordBuddy;Trusted_Connection=true"

## Step 4: Repository interfaces + application layer

In WordBuddy.Application:

1. Create generic `IRepository<T>` interface with:
   - GetByIdAsync(Guid id, CancellationToken ct)
   - GetAllAsync(CancellationToken ct)
   - AddAsync(T entity, CancellationToken ct)
   - UpdateAsync(T entity, CancellationToken ct)
   - DeleteAsync(Guid id, CancellationToken ct)

2. Create specific repository interfaces extending `IRepository<T>`:
   - ILessonRepository: add GetByTypeAsync, GetByLevelAsync, GetPublishedAsync
   - ILearnerProgressRepository: add GetByUserIdAsync, GetByUserAndLessonAsync
   - IMediaAssetRepository: add GetByTypeAsync

3. Create these use cases as command/query handler classes
   (no MediatR — plain classes with a HandleAsync method):

   Lessons:
   - GetLessonsQuery: filter by Type, Level, AgeGroup → returns `List<LessonDto>`
   - GetLessonDetailQuery: by lessonId → returns LessonDetailDto with all items
   - CreateLessonCommand: admin creates a lesson

   Progress:
   - RecordProgressCommand: mark lesson complete, save score
   - GetUserProgressQuery: get all progress for a user

4. Create DTOs for all query responses (separate from domain entities).

5. Install and register FluentValidation for command validation.
   Give me the dotnet add package command.

## Step 5: Implement Infrastructure + API controllers

1. In WordBuddy.Infrastructure, implement EF Core repositories for
   ILessonRepository and ILearnerProgressRepository using WordBuddyDbContext.
   All methods async. Use AsNoTracking() on queries.

2. In WordBuddy.API:
   a. Install these packages (give dotnet CLI commands):
      - Microsoft.AspNetCore.Authentication.JwtBearer
      - Swashbuckle.AspNetCore (Swagger)
      - Microsoft extension logging

   b. Create these controllers with full CRUD where appropriate:
      - LessonsController: GET /api/lessons (with query filters), GET /api/lessons/{id}
      - ProgressController: POST /api/progress, GET /api/progress (for current user)
      - AuthController: POST /api/auth/register, POST /api/auth/login (returns JWT)

   c. Configure Program.cs with:
      - JWT Bearer auth (read secret from appsettings)
      - Swagger with JWT support
      - CORS policy (allow any origin for now)
      - Register all services and repositories from Infrastructure
      - Serilog (read from appsettings) and usage Microsoft extension logging interface.

   d. Create appsettings.Development.json with:
      - ConnectionStrings:Default pointing to localdb
      - Jwt:Secret, Jwt:Issuer, Jwt:ExpiryMinutes
      - Serilog

All controller actions must: be async, return IActionResult,
use [Authorize] except login/register, include XML doc comments.

## Step 6: Initial migration + seed data

1. Create the first EF Core migration called InitialCreate.
   Give me the dotnet ef CLI commands to run in PowerShell,
   specifying the Infrastructure project as the migrations project
   and API as the startup project.

2. Create a DataSeeder class in Infrastructure that seeds:
   - 3 sample lessons (one Vocabulary, one Grammar, one DailyPhrase)
   - Each with 3-4 child items
   - 1 admin user (hashed password using BCrypt — add BCrypt.Net-Next package)

3. Call the seeder from Program.cs only when the environment is Development.

```powershell
dotnet ef migrations add InitialCreate `
  --project src/WordBuddy.Infrastructure `
  --startup-project src/WordBuddy.API

dotnet ef database update `
  --project src/WordBuddy.Infrastructure `
  --startup-project src/WordBuddy.API
```

