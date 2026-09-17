# Notification Service

Push/email notifications for daily phrases and learner reminders.

No domain entities yet — this is a project scaffold only until Phase 2 work reaches it (expect a
`NotificationPreference` entity). Has zero project references to any other WordBuddy service or
shared project — see the root [`CLAUDE.md`](../../../CLAUDE.md) for the independence model this
follows. Will consume domain events (`QuizCompletedEvent`/`LessonCompletedEvent`, from
`WordBuddy.Shared.Contracts`) via MassTransit to trigger notifications asynchronously.

## Endpoints

_None yet — this service has no public HTTP surface planned so far; it reacts to events rather
than serving requests._ It does have the same Serilog/OpenTelemetry/JWT-validation wiring every
other service gets (see `Program.cs`), so Swagger comes up at `http://localhost:5084/swagger`
even with zero controllers — ready to grow into real endpoints without re-deriving the setup.

## Running standalone

```bash
dotnet run --project WordBuddy.Notification.Api --urls http://localhost:5084
```

## Running in Docker / Kubernetes

`Dockerfile` lives in this folder (build context = this folder only, restores
`WordBuddy.Shared.*` from GitHub Packages via a BuildKit `--secret`). Image:
`wordbuddy-notification:dev`. In the `k8s/` manifests: Deployment/Service `notification-api` —
no database, no Ingress route yet (no public endpoints).

## Configuration

Copy `WordBuddy.Notification.Api/appsettings.Development.json.example` to
`appsettings.Development.json` (gitignored) to get started locally. It currently holds only:
- `Jwt:Secret`/`Jwt:Issuer` — **must match Identity's** dev values (no controllers use
  `[Authorize]` yet, but the wiring is in place)
- `Serilog:*` / `OpenTelemetry:OtlpEndpoint` — see the root `CLAUDE.md`'s Logging & Distributed
  Tracing section

`ConnectionStrings:DefaultConnection` will be added once this service has entities to persist.

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Notification.Api` | Composition root (no controllers yet) |
| `WordBuddy.Notification.Application` | Commands/queries, DTOs, validators (empty for now) |
| `WordBuddy.Notification.Domain` | Domain entities (empty for now) |
| `WordBuddy.Notification.Infrastructure` | EF Core `NotificationDbContext`, repositories (empty for now) |
| `WordBuddy.Notification.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Notification.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
