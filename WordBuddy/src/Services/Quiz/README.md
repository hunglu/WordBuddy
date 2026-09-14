# Quiz Service

Quiz definitions, question banks, and answer evaluation for WordBuddy lessons.

Owns the `Quiz` and `QuizQuestion` domain entities, plus the `QuizQuestionType` enum. Has zero
project references to any other WordBuddy service or shared project — see the root
[`CLAUDE.md`](../../../CLAUDE.md) for the independence model this follows.

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/quiz` | Bearer | Lists quizzes; optional `?lessonId=` filter |
| GET | `/api/quiz/{id}` | Bearer | Quiz with its questions (answers not included) |
| POST | `/api/quiz/{id}/submit` | Bearer | Submits one question's answer, returns correctness + explanation |
| POST | `/api/quiz` | Bearer | Creates a quiz with its questions (admin use) |

Swagger UI: `http://localhost:5082/swagger` (Development only). Bearer tokens come from
Identity — this service only validates them (same `Jwt:Secret`/`Jwt:Issuer` config), it never
issues its own. `QuizId` is a plain field, not a foreign key to Content's `Lesson` — services
never reference each other's data directly.

## Running standalone

```bash
dotnet run --project WordBuddy.Quiz.Api --urls http://localhost:5082
```

## Configuration

Copy `WordBuddy.Quiz.Api/appsettings.Development.json.example` to
`appsettings.Development.json` (gitignored) to get started locally. It holds:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyQuiz`)
- `Jwt:Secret`/`Jwt:Issuer` — **must match Identity's** dev values, since Quiz validates tokens
  Identity issued
- `Serilog:*` / `OpenTelemetry:OtlpEndpoint` — see the root `CLAUDE.md`'s Logging & Distributed
  Tracing section

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Quiz.Api` | Controllers, `Program.cs`, composition root |
| `WordBuddy.Quiz.Application` | Commands/queries, DTOs, validators |
| `WordBuddy.Quiz.Domain` | `Quiz`/`QuizQuestion` entities, `QuizQuestionType` enum |
| `WordBuddy.Quiz.Infrastructure` | EF Core `QuizDbContext`, repositories |
| `WordBuddy.Quiz.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Quiz.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
