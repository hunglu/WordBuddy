# Progress Service

Learner progress tracking, streaks, and completion history across lessons and quizzes.

Owns the `LearnerProgress` domain entity. Has zero project references to any other WordBuddy
service or shared project — see the root [`CLAUDE.md`](../../../CLAUDE.md) for the independence
model this follows.

## Messaging (consumer)

| Event (from Content) | Consumer | Effect on `LearnerWordMemberships` |
| --- | --- | --- |
| `LearnerWordAdded` | `LearnerWordAddedConsumer` | Insert or re-activate the (user, sense) row |
| `LearnerWordRemoved` | `LearnerWordRemovedConsumer` | `IsActive = false`; row kept |

- EF Core inbox dedupes on `MessageId`; the unique (`UserId`, `SenseId`) index dedupes on the
  natural key.
- Events older than the row's `LastEventAtUtc` are ignored (out-of-order safe).
- A failed command throws so the retry policy runs. Logs carry ids only.

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

Needs RabbitMQ on `localhost:5672` (e.g. the `rabbitmq` service from `docker-compose.yml`).

```bash
dotnet user-secrets set "Messaging:RabbitMq:Password" "<password>" --project WordBuddy.Progress.Api
dotnet run --project WordBuddy.Progress.Api --urls http://localhost:5083
```

Migrations (generate only; applying is ask-gated: `make k8s-migrate SERVICE=progress` or
`dotnet ef database update`):

```bash
dotnet ef migrations add <Name> --project WordBuddy.Progress.Infrastructure --startup-project WordBuddy.Progress.Infrastructure --output-dir Persistence/Migrations
```

Latest migration: `AddLearnerWordMembership` (`LearnerWordMemberships`, `InboxState`,
`OutboxMessage`, `OutboxState`).

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
- `Messaging:RabbitMq:{Host,VirtualHost,Username}` — broker (defaults in `appsettings.json`);
  `Messaging:RabbitMq:Password` only via user-secrets or env `Messaging__RabbitMq__Password`.
  `Messaging:Transport = InMemory` is for tests only.
- `Serilog:*` / `OpenTelemetry:OtlpEndpoint` — see the root `CLAUDE.md`'s Logging & Distributed
  Tracing section

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Progress.Api` | Controllers, `Program.cs`, composition root |
| `WordBuddy.Progress.Application` | Commands/queries, DTOs, validators |
| `WordBuddy.Progress.Domain` | `LearnerProgress`, `LearnerWordMembership` entities |
| `WordBuddy.Progress.Infrastructure` | EF Core `ProgressDbContext`, repositories, MassTransit consumers |
| `WordBuddy.Progress.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Progress.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
