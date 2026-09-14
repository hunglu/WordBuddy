# Progress Service

Learner progress tracking, streaks, and completion history across lessons and quizzes.

Owns the `LearnerProgress` domain entity. Has zero project references to any other WordBuddy
service or shared project — see the root [`CLAUDE.md`](../../../CLAUDE.md) for the independence
model this follows. Consumes `QuizCompletedEvent`/`LessonCompletedEvent` (from
`WordBuddy.Shared.Contracts`) via MassTransit once messaging is wired up in a later phase.

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/progress` | Bearer | Records/updates the caller's progress on a lesson (upsert by user+lesson) |
| GET | `/api/progress` | Bearer | Lists the caller's progress across all lessons |

Swagger UI: `http://localhost:5083/swagger` (Development only). Bearer tokens come from
Identity — this service only validates them (same `Jwt:Secret`/`Jwt:Issuer` config), it never
issues its own. The user id comes from the JWT's own `sub` claim, never a client-supplied value.
`LessonId` is a plain field, not a foreign key to Content's `Lesson` — services never reference
each other's data directly, so `GET /api/progress` returns lesson ids only, not titles (the
frontend/a caller that needs titles looks them up from Content separately).

## Running standalone

```bash
dotnet run --project WordBuddy.Progress.Api --urls http://localhost:5083
```

## Running in Docker / Kubernetes

`Dockerfile` lives in this folder (build context = this folder only, restores
`WordBuddy.Shared.*` from GitHub Packages via a BuildKit `--secret`). Image:
`wordbuddy-progress:dev`. In the `k8s/` manifests: Deployment/Service `progress-api`, database
`WordBuddyProgress`, Ingress path `/api/progress`.

## Configuration

Copy `WordBuddy.Progress.Api/appsettings.Development.json.example` to
`appsettings.Development.json` (gitignored) to get started locally. It holds:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyProgress`)
- `Jwt:Secret`/`Jwt:Issuer` — **must match Identity's** dev values, since Progress validates
  tokens Identity issued
- `Serilog:*` / `OpenTelemetry:OtlpEndpoint` — see the root `CLAUDE.md`'s Logging & Distributed
  Tracing section

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Progress.Api` | Controllers, `Program.cs`, composition root |
| `WordBuddy.Progress.Application` | Commands/queries, DTOs, validators |
| `WordBuddy.Progress.Domain` | `LearnerProgress` entity |
| `WordBuddy.Progress.Infrastructure` | EF Core `ProgressDbContext`, repositories |
| `WordBuddy.Progress.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Progress.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
