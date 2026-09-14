# Content Service

Lessons, vocabulary, grammar rules, daily phrases, and media assets (text/image/audio/video) for
WordBuddy.

Owns the `Lesson`, `Vocabulary`, `Grammar`, `DailyPhrase`, and `MediaAsset` domain entities, plus
the `Level`, `LessonType`, and `MediaAssetType` enums. Has zero project references to any other
WordBuddy service or shared project — see the root [`CLAUDE.md`](../../../CLAUDE.md) for the
independence model this follows.

## Endpoints

_None yet — added in Phase 2 (expect `GET /api/lessons`, `GET /api/lessons/{id}`,
`POST /api/media/upload`, `GET /api/media/{id}`)._

## Running standalone

```bash
dotnet run --project WordBuddy.Content.Api
```

## Configuration

`WordBuddy.Content.Api/appsettings.Development.json` (added in Phase 2) will hold:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyContent`)
- `Jwt:Issuer` (validates tokens issued by Identity)
- `FileStorage:BasePath` — local media storage path (added in Phase 4)

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Content.Api` | Controllers, `Program.cs`, composition root |
| `WordBuddy.Content.Application` | Commands/queries, DTOs, validators, `IFileStorageService` |
| `WordBuddy.Content.Domain` | `Lesson`/`Vocabulary`/`Grammar`/`DailyPhrase`/`MediaAsset` entities |
| `WordBuddy.Content.Infrastructure` | EF Core `ContentDbContext`, repositories, `LocalFileStorageService` |
| `WordBuddy.Content.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Content.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
