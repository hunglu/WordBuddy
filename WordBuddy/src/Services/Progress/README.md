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
dotnet run --project WordBuddy.Progress.Api
```

## Configuration

`WordBuddy.Progress.Api/appsettings.Development.json` (added in Phase 2) will hold:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyProgress`)
- `Jwt:Issuer` (validates tokens issued by Identity)

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Progress.Api` | Controllers, `Program.cs`, composition root |
| `WordBuddy.Progress.Application` | Commands/queries, DTOs, validators |
| `WordBuddy.Progress.Domain` | `LearnerProgress` entity |
| `WordBuddy.Progress.Infrastructure` | EF Core `ProgressDbContext`, repositories |
| `WordBuddy.Progress.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Progress.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
