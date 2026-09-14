# Content Service

Lessons, vocabulary, grammar rules, daily phrases, and media assets (text/image/audio/video) for
WordBuddy.

Owns the `Lesson`, `Vocabulary`, `Grammar`, `DailyPhrase`, and `MediaAsset` domain entities, plus
the `Level`, `LessonType`, and `MediaAssetType` enums. Has zero project references to any other
WordBuddy service or shared project — see the root [`CLAUDE.md`](../../../CLAUDE.md) for the
independence model this follows.

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/lessons` | Bearer | Lists published lessons; optional `?type=` / `?level=` filters |
| GET | `/api/lessons/{id}` | Bearer | Full lesson content (vocabulary/grammar/daily phrases) |
| POST | `/api/lessons` | Bearer | Creates a lesson (admin use) |
| POST | `/api/media/upload` | Bearer | Uploads a media file (max 50MB; jpeg/png/mp3/wav/mp4) |
| GET | `/api/media/{id}` | Anonymous | Streams a previously uploaded media file |

Swagger UI: `http://localhost:5081/swagger` (Development only). Bearer tokens come from
Identity — this service only validates them (same `Jwt:Secret`/`Jwt:Issuer` config), it never
issues its own.

## Running standalone

```bash
dotnet run --project WordBuddy.Content.Api --urls http://localhost:5081
```

## Configuration

Copy `WordBuddy.Content.Api/appsettings.Development.json.example` to
`appsettings.Development.json` (gitignored) to get started locally. It holds:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyContent`)
- `Jwt:Secret`/`Jwt:Issuer` — **must match Identity's** dev values, since Content validates
  tokens Identity issued
- `FileStorage:BasePath` — local media storage root (`C:\WordBuddyMedia` in dev)
- `Serilog:*` / `OpenTelemetry:OtlpEndpoint` — see the root `CLAUDE.md`'s Logging & Distributed
  Tracing section

## Projects

| Project | Purpose |
|---|---|
| `WordBuddy.Content.Api` | Controllers, `Program.cs`, composition root |
| `WordBuddy.Content.Application` | Commands/queries, DTOs, validators, `IFileStorageService` |
| `WordBuddy.Content.Domain` | `Lesson`/`Vocabulary`/`Grammar`/`DailyPhrase`/`MediaAsset` entities |
| `WordBuddy.Content.Infrastructure` | EF Core `ContentDbContext`, repositories, `LocalFileStorageService` |
| `WordBuddy.Content.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Content.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
