# Notification Service

Push/email notifications for daily phrases and learner reminders.

No domain entities yet — this is a project scaffold only until Phase 2 work reaches it (expect a
`NotificationPreference` entity). Has zero project references to any other WordBuddy service or
shared project — see the root [`CLAUDE.md`](../../../CLAUDE.md) for the independence model this
follows. Will consume domain events (`QuizCompletedEvent`/`LessonCompletedEvent`, from
`WordBuddy.Shared.Contracts`) via MassTransit to trigger notifications asynchronously.

## Endpoints

_None yet — this service has no public HTTP surface planned so far; it reacts to events rather
than serving requests._

## Running standalone

```bash
dotnet run --project WordBuddy.Notification.Api
```

## Configuration

`WordBuddy.Notification.Api/appsettings.Development.json` (added when this service gains real behavior)
will hold:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyNotification`),
  once it has entities to persist

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Notification.Api` | Composition root (no controllers yet) |
| `WordBuddy.Notification.Application` | Commands/queries, DTOs, validators (empty for now) |
| `WordBuddy.Notification.Domain` | Domain entities (empty for now) |
| `WordBuddy.Notification.Infrastructure` | EF Core `NotificationDbContext`, repositories (empty for now) |
| `WordBuddy.Notification.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Notification.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
