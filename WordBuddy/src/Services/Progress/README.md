# Progress Service

Learner progress tracking, streaks, and completion history across lessons and quizzes.

Owns the `LearnerProgress` domain entity. Has zero project references to any other WordBuddy
service or shared project — see the root [`CLAUDE.md`](../../../CLAUDE.md) for the independence
model this follows. Consumes `QuizCompletedEvent`/`LessonCompletedEvent` (from
`WordBuddy.Shared.Contracts`) via MassTransit once messaging is wired up in a later phase.

## Endpoints

_None yet — added in Phase 2 (expect `POST /api/progress`, `GET /api/progress`)._

## Running standalone

```bash
dotnet run --project Progress.Api
```

## Configuration

`Progress.Api/appsettings.Development.json` (added in Phase 2) will hold:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyProgress`)
- `Jwt:Issuer` (validates tokens issued by Identity)

## Projects

| Project | Purpose |
|---|---|
| `Progress.Api` | Controllers, `Program.cs`, composition root |
| `Progress.Application` | Commands/queries, DTOs, validators |
| `Progress.Domain` | `LearnerProgress` entity |
| `Progress.Infrastructure` | EF Core `ProgressDbContext`, repositories |
| `Progress.UnitTests` | Handler/domain unit tests (Moq) |
| `Progress.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
