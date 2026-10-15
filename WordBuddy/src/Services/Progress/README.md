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

| Event (from Identity) | Consumer | Effect |
| --- | --- | --- |
| `LearnerProfileUpdated` | `LearnerProfileUpdatedConsumer` | Upsert `ChallengeProfiles`; older events ignored; Guardian role removed → that guardian consents deleted (WB-29) |
| `SupportLinkRevoked` | `SupportLinkRevokedConsumer` | Also deletes the challenge consent that supporter gave for the learner (WB-29) |

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/progress` | Bearer | Records/updates the caller's progress on a lesson (upsert by user+lesson) |
| GET | `/api/progress` | Bearer | Lists the caller's progress across all lessons |
| GET | `/api/progress/vocabulary/session` | Bearer | Today's session: due words first, then new words up to the cap; issues a `sessionId`. An open session (not ended, not expired) is resumed with the same `sessionId` and its unanswered items |
| POST | `/api/progress/vocabulary/exercises` | Bearer, rate limit `vocabulary-review` | Builds the next exercise for one session word (WB-28); returns `{ exerciseId, exerciseType, skill, prompt, options }` — never the answer |
| POST | `/api/progress/vocabulary/reviews` | Bearer, rate limit `vocabulary-review` | Grades the raw answer to an issued exercise; returns `{ status, dueAtUtc, rating, isCorrect, correctAnswer }` |
| GET | `/api/progress/vocabulary/words` | Bearer | The caller's active word states |
| GET / PUT | `/api/progress/vocabulary/settings` | Bearer | The caller's `newWordsPerDay` (0–50, `null` = backlog rule); any user, child included |
| GET | `/api/progress/dashboard/me?days=7\|30\|90` | Bearer, `ChildHasSupporter`, rate limit `dashboard` | The caller's own dashboard (WB-26) |
| GET | `/api/progress/dashboard/learners/{learnerId}?days=…` | Bearer, `CanSupportLearner`, rate limit `dashboard` | A learner's dashboard for an active supporter; no link or revoked link → 403 |
| POST / GET | `/api/progress/vocabulary-recall` | Bearer | Old self-rated recall check; kept until WB-23 ships |
| GET | `/api/progress/challenges` | Bearer | Pages the Open and Running challenges of the caller audience (from `age_group`); `?page=` (default 1), `?pageSize=` (1-100, default 25). A child without Guardian consent gets an empty page |
| GET | `/api/progress/challenges/{id}/board` | Bearer, same audience | Adult: pages the full ranking (`?page=`, `?pageSize=`); `entries` is `{ items, page, pageSize, totalCount }`. Child: own rank and top 5, alias and avatar only; paging is ignored and the answer is always one page |
| POST | `/api/progress/challenges/{id}/join` | Bearer, rate limit `challenge-join` | Opt in. `409 Challenge.Full` at capacity; `403 Challenge.NoConsent` for a child without consent |
| DELETE | `/api/progress/challenges/{id}/join` | Bearer | Opt out |
| PUT | `/api/progress/challenges/consent/{childId}` | `GuardianOnly` (`guardian` claim) | `{ "enabled": true }`. Needs the Guardian role and an active support link, else `403 Challenge.NotGuardian` / `Challenge.NoActiveLink` |

### Vocabulary SRS (WB-22)

```text
answer → AnswerGrader (rating) → first attempt of a new/due word? → FSRS-6 reschedule
       → one ReviewLog row (insert-only) → one save
```

- The client sends the raw answer only: `exerciseId`, `answer` (`{ optionKey }` or `{ text }`),
  `clientResponseMs`, `hintUsed`. Correctness, exercise type, skill, sense, session, rating, `IsDue`
  and `AttemptNo` are server-derived. Unknown JSON fields (a forged `isCorrect`) are ignored.
- Unknown or removed word → `404 Review.WordNotInList`. Unknown or foreign exercise → `404
  Exercise.NotFound`. Exercise already answered → `409 Exercise.AlreadyAnswered`. Bad input → `400`.
  Over the limit (60 calls/minute/user, exercises and reviews together) → `429` + `Retry-After`.
- Same answer sent twice at once (double tap, retry) → one `200`, the other `409
  LearnerWordState.ConcurrentUpdate`. Guards: unique `(UserId, SessionId, SenseId, AttemptNo)`
  on `ReviewLogs` and a `RowVersion` on `LearnerWordStates`. FSRS is applied once.
- The 60/minute limit is a code constant (`RateLimitingConfiguration`), not an option.
- Header `X-Client-CurrentDateTime` (ISO 8601 with offset, e.g. `2026-10-07T09:30:00+07:00`):
  only the offset sets the client's "today". Missing → UTC. Bad format, offset outside
  −12:00…+14:00, or more than 24 h from server time → `400`. Due checks use server UTC.
- Enums are JSON strings.

| Rule | Child | Adult |
| --- | --- | --- |
| New-word cap | Backlog rule 10/8/6/5, or own setting | Same |
| Grading thresholds | Base × 1.25, or the age band multiplier | Base × 1.0, or the age band multiplier |
| FSRS, ReviewLog | Same | Same |

`age_group` comes from the JWT; missing or unknown → treated as Child (more lenient grading).
This differs from Content, which fails on a missing `age_group` claim. The lenient default is
safe here: the claim only sets grading thresholds, never content access.

**Age band (WB-20).** Identity adds an `age_band` claim (`Under7`, `Age7To9`, `Age10To12`, `Age13To17`, `Adult`) when the user set a date of birth. The claim holds the band only, never the date.

| Claim state | Threshold multiplier |
| --- | --- |
| Known `age_band` | `Vocabulary:Grading:AgeBandMultiplier` for that band |
| Missing or unknown `age_band` | `AgeGroupMultiplier` for `age_group` (WB-22 behaviour) |

Past `ReviewLogs` rows keep the rating they were recorded with.

### Server-side answer checking (WB-28)

```text
session → POST exercises {sessionId, senseId}
        → Content senses (caller's JWT forwarded, cached per session) → ExerciseSelector → VocabularyExercise stored
        → POST reviews {exerciseId, answer, clientResponseMs}
        → AnswerChecker + ResponseTimeEvaluator → AnswerGrader → FSRS → ReviewLog
```

- Progress picks the exercise type and the options. The reply carries opaque option keys and no sense ids.
  The Typing prompt has no word, only its first letter for the hint.
- Choice: the key must equal the stored key. Typing: trim + case-insensitive match. The answer text is
  never stored or logged.
- Timing: the server measures `answeredAt - issuedAt`. The client value is accepted only when
  `0 <= client <= server` and `server - client <= Vocabulary:Grading:ToleranceMs` (default 3000).
  Otherwise `server - tolerance` (floor 0) is used and `ReviewLogs.TimingAdjusted` is set.
- Content down → `503 Content.Unavailable`. A sense Content does not return (hidden for a child, deleted)
  → `404 Exercise.SenseUnavailable`; the UI skips it.
- A word is done for the session after 2 correct answers → `409 Exercise.SenseCompleted` for more exercises.

| Rule | Child | Adult |
| --- | --- | --- |
| Senses used as prompt and distractor | Child-visible only (Content filter, forwarded JWT) | All |
| Timing tolerance | 3000 ms | 3000 ms |

### Dashboard (WB-26)

```text
ReviewLogs + LearnerWordStates + LearnerWordMemberships + VocabularySessionIssues
   → DashboardCalculator (pure) → LearnerDashboardDto → cache 5 min → endpoint
```

- No stored aggregates: every number is computed at read time. A rebuild is a cache delete.
- Window: `days` = 7, 30 or 90 (default `Dashboard:DefaultDays`). Other value → `400`. "Day" is the
  learner's local day, from the `X-Client-CurrentDateTime` offset (same rule as the session).
- The streak looks back up to 90 days whatever `days` is.
- Cache key `progress:dashboard:{learnerId}:{days}:{offsetMinutes}`, absolute 5 min, no explicit
  delete: a new answer shows after the TTL.
- Every active supporter gets the full view (WB-24). The DTO has dates only, never a time of day.
- The session endpoint now writes one `VocabularySessionIssues` row per non-empty session. Earlier
  sessions have no row, so "daily goal" and "unfinished sessions" are empty before release.
- Logs carry ids and counts only.

| Option (`Dashboard:`) | Default |
| --- | --- |
| `DefaultDays` | `30` |
| `MinSample` (answers needed before a retention figure shows) | `10` |
| `FastWrongMs` (wrong and faster = "quick wrong") | `1500` |
| `HintRateFlag` | `0.3` |

### Vocabulary challenges (WB-29)

```text
Identity LearnerProfileUpdated → ChallengeProfiles (alias, avatar, time zone, guardian)
Scheduler (1 min)  → creates next instances, moves Scheduled → Open → Running → Finished
Scheduler (5 min)  → ChallengeScorer(ReviewLogs) → ChallengeParticipants.Score / Rank (dense)
```

- Challenges are made by the system from four templates: Weekly League (ranked, Mon-Sun UTC), 7-Day Memory
  (target 7 days), Word Climber (target 20 recall upgrades, 14 days), Listening Sprint (ranked, adults only).
  Adult and child instances are separate and never mixed. Capacity 30, no overflow instance.
- Scores are a pure projection of `ReviewLogs`; a rebuild is safe at any time. Windows are global UTC instants;
  only the daily cap and the daily goal use the learner local day (IANA zone from Identity, UTC if unset).
- Points: due, correct, first try, no hint +10; with hint +5; first correct typing after only correct choice answers +5;
  daily goal bonus once per local day; per-day cap. Ignored: not due, wrong, adjusted timing or faster than the floor,
  first review of a word, answers before joining.
- Child: needs Guardian consent (Guardian role and active support link). Consent is deleted when the link is revoked
  or the role is removed; the child is then taken out of unfinished challenges. Board cache key
  `progress:challenge-board:{id}`, absolute 5 min, deleted after recompute, join, leave and consent change.
- A revoked support link is handled in `ApplySupportLinkEventCommandHandler` (called by `SupportLinkRevokedConsumer`).
- Logs carry ids and counts only.

| Option (`Challenges:`) | Default |
| --- | --- |
| `DailyGoalCorrectDue` | `10` |
| `DailyGoalBonus` | `10` |
| `DailyPointCap` | `200` |
| `FastAnswerFloorMs` | `800` |
| `Capacity` | `30` |
| `OpenLeadHours` | `24` |
| `LifecycleTickSeconds` / `RecomputeIntervalMinutes` | `60` / `5` |
| `SchedulerEnabled` | `true` |

### Vocabulary options

| Section | Key | Default |
| --- | --- | --- |
| `Vocabulary:Scheduling` | `DesiredRetention` | `0.9` |
| | `MaximumIntervalDays` | `36500` |
| | `LearningStepsMinutes` / `RelearningStepsMinutes` | `[1, 10]` / `[10]` |
| | `MasteredStabilityDays` | `21` |
| | `LeechLapses` | `4` |
| | `MaxDueItems` | `50` |
| | `SessionDurationMinutes` | `30` |
| `Vocabulary:Grading` | `PictureChoice` / `ListeningChoice` / `Typing` (`FastMs`, `SlowMs`) | 3000/10000, 4000/12000, 6000/20000 |
| | `AgeGroupMultiplier` (`Child`, `Adult`) | `1.25`, `1.0` |
| | `AgeBandMultiplier` (`Under7`, `Age7To9`, `Age10To12`, `Age13To17`, `Adult`) | `1.5`, `1.35`, `1.2`, `1.1`, `1.0` |
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

Latest migration: `AddDashboard` (`VocabularySessionIssues`). Before it: `AddVocabularySrs`
(`LearnerWordStates`, `ReviewLogs`, `VocabularyLearnerSettings`). After applying `AddVocabularySrs`, run Content's one-time backfill (see the Content
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
- `Services:Content:BaseUrl` — Content base URL (default `http://localhost:5081`; `Services__Content__BaseUrl` in compose and k8s);
- `Messaging:*` — bus settings, validated at startup (full key table: `docs/features/messaging.md`):
  `Transport`, `RabbitMq:{Host,VirtualHost,Username,HeartbeatSeconds,RequestedConnectionTimeoutMs}`,
  `Outbox:{QueryDelayMs,DuplicateDetectionWindowSeconds}`, `Retry:IntervalsMs` (defaults in `appsettings.json`).
  `Messaging:RabbitMq:Username` and `Password` are required outside Development; set the password
  only via user-secrets or env `Messaging__RabbitMq__Password`. `Messaging:Transport = InMemory` is for tests only.
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
