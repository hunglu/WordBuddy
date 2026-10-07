# Content Service

Lessons, vocabulary, grammar rules, daily phrases, and media assets (text/image/audio/video) for
WordBuddy.

Owns the `Lesson`, `Grammar`, `DailyPhrase`, and `MediaAsset` domain entities, and the vocabulary
catalog `Lexeme` → `Sense` → `SenseTranslation`, with `LearnerWord` (a learner's list) and
`LessonSense` (lesson links) — see ADR 0004. Enums: `Level`, `LessonType`, `MediaAssetType`,
`PartOfSpeech`, `CefrLevel`, `LearnerWordAddedBy`. Has zero project references to any other
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
| DELETE | `/api/vocabulary/{id}?confirm=true` | Bearer | Removes a word from the caller's list (see delete rule below) |
| GET | `/api/vocabulary/shared` | Bearer | Community word pool; `isMine` is `true` only on the caller's own words |
| POST | `/api/vocabulary/admin/learner-words/republish` | Bearer, `AdminOnly` | Backfill: republishes `LearnerWordAdded` for every learner link; returns `{ published }` |

There are no internal (service-to-service) endpoints. The vocabulary id remap table and the
`/internal/vocabulary-remaps` routes were removed (migration `DropVocabularyWordIdRemaps`).

### Personal vocabulary delete rule

| Caller | Word status | `?confirm=true` needed | Effect |
|---|---|---|---|
| Author | `Shared` | Yes, else 409 | Word handed over to WordBuddy (System owner); stays in the pool; adopters keep it |
| Author | `PendingReview` | Yes, else 409 | Share request cancelled; word deleted if no one else links to it |
| Author | `Private` / `Rejected` | No | Unlinked; deleted if no one else links to it |
| Adopter | any | No | Unlinked only |

The 409 error code is `PersonalVocabularyWord.DeleteConfirmationRequired`. A Child never authors a
shared word, so the confirm path only applies to Adult callers. A transferred word that is not
child-safe stays hidden from Child callers.

Swagger UI: `http://localhost:5081/swagger` (Development only). Bearer tokens come from
Identity — this service only validates them (same `Jwt:Secret`/`Jwt:Issuer` config), it never
issues its own.

## Messaging (publisher)

Every `LearnerWord` insert/delete publishes an event through the EF Core outbox, in the same
transaction (`ContentDbContext.SaveChangesAsync` → `LearnerWordEventCollector`).

| Change | Event (`WordBuddy.Shared.Contracts.Vocabulary`) |
| --- | --- |
| Add own word, adopt shared word | `LearnerWordAdded` |
| Delete, confirmed hand-over, cancelled share, orphan delete | `LearnerWordRemoved` |
| System-owner link | none |

Payload: ids and timestamps only (no word text, no personal context, no age group). Child and
adult events are identical. Consumer: Progress.

### One-time backfill (WB-22)

Progress creates a learning state per learner word from `LearnerWordAdded`. Words added before
WB-22 have no state until Content republishes them. Run once per environment, after Progress has
the `AddVocabularySrs` migration and is running:

```bash
curl -X POST -H "Authorization: Bearer <admin token>"   http://<host>/api/vocabulary/admin/learner-words/republish
```

- Batches of 500 links, one outbox save per batch. System-owner links are skipped.
- Each event keeps the link's original `AddedAtUtc`. Progress applies it, creates missing states
  and leaves existing ones unchanged, so a second run is harmless.
- Non-admin callers get `403`.

## Running standalone

Needs RabbitMQ on `localhost:5672` (e.g. the `rabbitmq` service from `docker-compose.yml`).

```bash
dotnet user-secrets set "Messaging:RabbitMq:Password" "<password>" --project WordBuddy.Content.Api
dotnet run --project WordBuddy.Content.Api --urls http://localhost:5081
```

Migrations (generate only; applying is ask-gated: `make k8s-migrate SERVICE=content` or
`dotnet ef database update`):

```bash
dotnet ef migrations add <Name> --project WordBuddy.Content.Infrastructure --startup-project WordBuddy.Content.Infrastructure --output-dir Persistence/Migrations
```

Latest messaging migration: `AddMessagingOutbox` (`InboxState`, `OutboxMessage`, `OutboxState`).

## Running in Docker / Kubernetes

`Dockerfile` lives in this folder (build context = this folder only, restores
`WordBuddy.Shared.*` from GitHub Packages via a BuildKit `--secret`). Image:
`wordbuddy-content:dev`. In the `k8s/` manifests: Deployment/Service `content-api`, database
`WordBuddyContent`, Ingress paths `/api/lessons` and `/api/media`. `FileStorage:BasePath` is set
to `/app/media` in Docker/Kubernetes (an `emptyDir` volume in `k8s/content-deployment.yaml` — not
yet a real PersistentVolume, so uploaded media does not survive a pod restart in this phase).

## Configuration

Copy `WordBuddy.Content.Api/appsettings.Development.json.example` to
`appsettings.Development.json` (gitignored) to get started locally. It holds:
- `ConnectionStrings:DefaultConnection` — this service's own database (`WordBuddyContent`)
- `Jwt:Secret`/`Jwt:Issuer` — **must match Identity's** dev values, since Content validates
  tokens Identity issued
- `FileStorage:BasePath` — local media storage root (`C:\WordBuddyMedia` in dev)
- `Messaging:RabbitMq:{Host,VirtualHost,Username}` — broker (defaults in `appsettings.json`);
  `Messaging:RabbitMq:Password` only via user-secrets or env `Messaging__RabbitMq__Password`.
  `Messaging:Transport = InMemory` is for tests only.
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
