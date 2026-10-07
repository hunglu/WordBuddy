# Progress Service

Learner progress tracking, streaks, and completion history across lessons and quizzes.

Owns the `LearnerProgress` domain entity. Has zero project references to any other WordBuddy
service or shared project — see the root [`CLAUDE.md`](../../../CLAUDE.md) for the independence
model this follows.

## Messaging (consumer)

| Event (from Content) | Consumer | Effect on `LearnerWordMemberships` | Effect on `LearnerWordStates` |
| --- | --- | --- | --- |
| `LearnerWordAdded` | `LearnerWordAddedConsumer` | Insert or re-activate the (user, sense) row | Create a `New` state, or re-activate (FSRS history kept) |
| `LearnerWordRemoved` | `LearnerWordRemovedConsumer` | `IsActive = false`; row kept | Deactivate if present |

- EF Core inbox dedupes on `MessageId`; the unique (`UserId`, `SenseId`) index dedupes on the
  natural key.
- Events older than the row's `LastEventAtUtc` are ignored (out-of-order safe).
- A failed command throws so the retry policy runs. Logs carry ids only.
- Membership and state change in the same save.

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/progress` | Bearer | Records/updates the caller's progress on a lesson (upsert by user+lesson) |
| GET | `/api/progress` | Bearer | Lists the caller's progress across all lessons |
| GET | `/api/progress/vocabulary/session` | Bearer | Today's session: due words first, then new words up to the cap; issues a `sessionId` |
| POST | `/api/progress/vocabulary/reviews` | Bearer, rate limit `vocabulary-review` | Records one answer; returns `{ status, dueAtUtc, rating }` |
| GET | `/api/progress/vocabulary/words` | Bearer | The caller's active word states |
| GET / PUT | `/api/progress/vocabulary/settings` | Bearer | The caller's `newWordsPerDay` (0–50, `null` = backlog rule); any user, child included |
| POST / GET | `/api/progress/vocabulary-recall` | Bearer | Old self-rated recall check; kept until WB-23 ships |

### Vocabulary SRS (WB-22)

```text
answer → AnswerGrader (rating) → first attempt of a new/due word? → FSRS-6 reschedule
       → one ReviewLog row (insert-only) → one save
```

- The client sends answers only: `sessionId`, `senseId`, `exerciseType`, `skill`, `isCorrect`,
  `responseMs`, `hintUsed`. Rating, `IsDue` and `AttemptNo` are server-derived.
- Unknown or removed word → `404 Review.WordNotInList`. Bad input → `400`. Over the limit
  (60 answers/minute/user) → `429` + `Retry-After`.
- Header `X-Client-CurrentDateTime` (ISO 8601 with offset, e.g. `2026-10-07T09:30:00+07:00`):
  only the offset sets the client's "today". Missing → UTC. Bad format, offset outside
  −12:00…+14:00, or more than 24 h from server time → `400`. Due checks use server UTC.
- Enums are JSON strings.

| Rule | Child | Adult |
| --- | --- | --- |
| New-word cap | Backlog rule 10/8/6/5, or own setting | Same |
| Grading thresholds | Base × 1.25 | Base × 1.0 |
| FSRS, ReviewLog | Same | Same |

`age_group` comes from the JWT; missing or unknown → treated as Child (more lenient grading).

### Vocabulary options

| Section | Key | Default |
| --- | --- | --- |
| `Vocabulary:Scheduling` | `DesiredRetention` | `0.9` |
| | `MaximumIntervalDays` | `36500` |
| | `LearningStepsMinutes` / `RelearningStepsMinutes` | `[1, 10]` / `[10]` |
| | `MasteredStabilityDays` | `21` |
| | `LeechLapses` | `4` |
| | `MaxDueItems` | `50` |
| `Vocabulary:Grading` | `PictureChoice` / `ListeningChoice` / `Typing` (`FastMs`, `SlowMs`) | 3000/10000, 4000/12000, 6000/20000 |
| | `AgeGroupMultiplier` (`Child`, `Adult`) | `1.25`, `1.0` |
| `Vocabulary:NewWordCap` | `LowBacklogMax`/`Cap`, `MediumBacklogMax`/`Cap`, `HighBacklogMax`/`Cap`, `OverflowCap` | 20→10, 40→8, 60→6, else 5 |

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

Latest migration: `AddVocabularySrs` (`LearnerWordStates`, `ReviewLogs`,
`VocabularyLearnerSettings`). After applying it, run Content's one-time backfill (see the Content
`README.md`).

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
| `WordBuddy.Progress.Domain` | `LearnerProgress`, `LearnerWordMembership`, `LearnerWordState`, `ReviewLog`, `VocabularyLearnerSettings` entities; `FsrsScheduler`, `AnswerGrader`, `NewWordCapPolicy` |
| `WordBuddy.Progress.Infrastructure` | EF Core `ProgressDbContext`, repositories, MassTransit consumers |
| `WordBuddy.Progress.UnitTests` | Handler/domain unit tests (Moq) |
| `WordBuddy.Progress.IntegrationTests` | API/DB integration tests (`WebApplicationFactory`) |
