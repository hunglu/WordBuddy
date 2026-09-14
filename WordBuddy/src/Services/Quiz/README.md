# Quiz Service

Quiz definitions, question banks, and answer evaluation for WordBuddy lessons.

Owns the `Quiz` and `QuizQuestion` domain entities, plus the `QuizQuestionType` enum. Has zero
project references to any other WordBuddy service or shared project — see the root
[`CLAUDE.md`](../../../CLAUDE.md) for the independence model this follows.

## Endpoints

_None yet — added in Phase 2 (expect `GET /api/quiz/{id}`, `POST /api/quiz/{id}/submit`)._

## Running standalone

```bash
dotnet run --project Quiz.Api
```

## Configuration

`Quiz.Api/appsettings.Development.json` (added in Phase 2) will hold:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyQuiz`)
- `Jwt:Issuer` (validates tokens issued by Identity)

## Projects

| Project | Purpose |
|---|---|
| `Quiz.Api` | Controllers, `Program.cs`, composition root |
| `Quiz.Application` | Commands/queries, DTOs, validators |
| `Quiz.Domain` | `Quiz`/`QuizQuestion` entities, `QuizQuestionType` enum |
| `Quiz.Infrastructure` | EF Core `QuizDbContext`, repositories |
| `Quiz.UnitTests` | Handler/domain unit tests (Moq) |
| `Quiz.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
