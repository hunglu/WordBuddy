# Tasks: WB-22_Vocabulary SRS engine

## Backend — Progress domain

- [x] Confirm FSRS choice (plan Open question 1); quick NuGet check, record result in the PR body — decision noted before scheduler code — D-1: own FSRS-6 port, no NuGet dependency; text recorded in ADR 0005 and the coder report for the PR body
- [x] Add enums `WordStatus`, `FsrsRating`, `VocabularySkill`, `ExerciseType`, `FsrsPhase` — string-serialised, XML docs — Domain/`WordStatus.cs`, `FsrsRating.cs`, `VocabularySkill.cs`, `ExerciseType.cs`, `FsrsPhase.cs`, `AgeGroup.cs`
- [x] Add `IFsrsScheduler` + FSRS-6 implementation (default parameters, no fuzz) — pure, no I/O — Domain/`IFsrsScheduler.cs`, `FsrsScheduler.cs`, `FsrsCard.cs`, `VocabularySchedulingOptions.cs`
- [x] Add `AnswerGrader` with per-exercise thresholds from options — maps answers to the 4 ratings per D10 — Domain/`AnswerGrader.cs`, `VocabularyGradingOptions.cs` (D-7 AgeGroup multiplier)
- [x] Add `NewWordCapPolicy` (child backlog steps, adult setting) with options `Vocabulary:NewWordCap` — child result always 5–10 — Domain/`NewWordCapPolicy.cs`, `NewWordCapOptions.cs` (D-3: backlog rule for all users, per-user override)
- [x] Add `LearnerWordState` entity (`CreateNew`, `ApplyReview`, `Activate`, `Deactivate`, status rules) — Mastered/Leech thresholds from options — Domain/`LearnerWordState.cs` (adds `FsrsStep`, needed for FSRS learning steps)
- [x] Add `ReviewLog` entity, insert-only (factory only, no setters/update methods) — Domain/`ReviewLog.cs`
- [x] Add `VocabularyLearnerSettings` entity (`NewWordsPerDay` 0–50) — Domain/`VocabularyLearnerSettings.cs`

## Backend — Progress infrastructure

- [x] EF configurations for the 3 entities with indexes from the plan — unique (`UserId`, `SenseId`) on states — Infrastructure/Persistence/Configurations/`LearnerWordStateConfiguration.cs`, `ReviewLogConfiguration.cs`, `VocabularyLearnerSettingsConfiguration.cs`; `ProgressDbContext.cs`
- [x] Migration `AddVocabularySrs` — creates 3 tables; applies on a clean DB — Migrations/`20261007032854_AddVocabularySrs*.cs`, snapshot; `docs/database-diagram/progress.md`, `README.md` (applied only to the throwaway LocalDB test DB by the integration tests)
- [x] Repositories `ILearnerWordStateRepository`, `IReviewLogRepository` (`AddAsync`, counts/queries only), `IVocabularyLearnerSettingsRepository` — no update/delete on review logs — Application/Interfaces/*, Infrastructure/Repositories/`LearnerWordStateRepository.cs`, `ReviewLogRepository.cs`, `VocabularyLearnerSettingsRepository.cs`, Infrastructure DI
- [x] Options binding + `appsettings.json` defaults for `Vocabulary:Grading`, `Vocabulary:NewWordCap`, `Vocabulary:Scheduling` — Api/Extensions/`ServiceCollectionExtensions.cs` (`AddVocabularySrs`), Api/`appsettings.json`

## Backend — Progress application + API

- [x] Extend `RecordLearnerWordAdded`/`Removed` handlers to create/activate/deactivate `LearnerWordState` in the same save — duplicate events create no extra rows — both handlers; `ILearnerWordMembershipRepository` + `LearnerWordMembershipRepository` (before-save hook)
- [x] `RecordVocabularyReviewCommand` + validator + handler (server-derived `AttemptNo`, `IsDue`, rating; FSRS only on first attempt of a due/new card) — exactly one `ReviewLog` row per call — Application/Features/VocabularySrs/Commands/RecordVocabularyReview/*, DTOs/`VocabularySrsDtos.cs`
- [x] `GetVocabularySessionQuery` + handler; `X-Client-CurrentDateTime` header parsed and validated (offset only, D-2) — due first, then new up to cap minus today's new words — Features/VocabularySrs/Queries/GetVocabularySession/*, `ClientDateTime.cs`
- [x] `GetLearnerWordStatesQuery` + handler — caller's active states only — Features/VocabularySrs/Queries/GetLearnerWordStates/*
- [x] `GetVocabularySettingsQuery`, `UpdateVocabularySettingsCommand` + validator — all users, `null` = backlog rule (D-3, D-4) — Features/VocabularySrs/Queries/GetVocabularySettings/*, Commands/UpdateVocabularySettings/*
- [x] `ClaimsPrincipalExtensions.GetAgeGroup()` feeding the grader `AgeGroup` multiplier (D-7) — Api/Extensions/`ClaimsPrincipalExtensions.cs` (missing/unknown claim → Child)
- [x] `VocabularySrsController` at `api/progress/vocabulary` (`session`, `reviews`, `words`, `settings`) using `ToProblemResult` — logs ids/counts only — Api/Controllers/`VocabularySrsController.cs`, Api/Models/`VocabularySrsRequests.cs`
- [x] Rate-limit policy `vocabulary-review` on `POST reviews` — 429 + `Retry-After` — Api/Extensions/`RateLimitingConfiguration.cs` (new; 60/min/user), `WebApplicationExtensions.cs`, `Program.cs`
- [x] Register handlers/validators/options in DI — Application/Extensions/`ServiceCollectionExtensions.cs`, Api `AddVocabularySrs`

## Backend — Content backfill

- [x] `ILearnerWordEventPublisher` (Application) + outbox implementation (Infrastructure) — Application/Interfaces/`ILearnerWordEventPublisher.cs`, Infrastructure/Messaging/`OutboxLearnerWordEventPublisher.cs`, Infrastructure DI
- [x] `RepublishLearnerWordsCommand` + handler (batches of 500, skips `SystemOwner`, original `AddedAtUtc`) — returns published count — Features/PersonalVocabulary/Commands/RepublishLearnerWords/*, `IVocabularyWordRepository.GetLearnerLinksPageAsync` + `VocabularyWordRepository`, Application DI
- [x] Endpoint `POST /api/vocabulary/admin/learner-words/republish` with `AdminOnly` — non-admin gets 403 — Api/Controllers/`VocabularyAdminController.cs`
- [x] Document the one-time backfill run in Content `README.md` — Content/`README.md`

## Docs

- [x] ADR 0005: status → accepted, fill in FSRS choice, day boundary, backfill and recall-transition decisions — matches the merged behaviour — `docs/adr/0005-learning-state-fsrs-reviewlog-events.md`
- [x] Progress `README.md`: new endpoints and options — Progress/`README.md`

## Tests

- [x] Unit `AnswerGrader` — all 4 ratings, hint, boundary ms values per exercise type — UnitTests/Domain/`AnswerGraderTests.cs`
- [x] Unit FSRS scheduler — matches py-fsrs reference vectors (first review each rating, lapse, multi-step sequence) — UnitTests/Domain/`FsrsSchedulerTests.cs` (vectors from py-fsrs 6.3.2, fuzz off)
- [x] Unit `NewWordCapPolicy` — each child backlog step, child never outside 5–10, adult setting and default — UnitTests/Domain/`NewWordCapPolicyTests.cs` (D-3: one rule for all users)
- [x] Unit `LearnerWordState` — status transitions (New → Learning → Review → Mastered, Leech at 4 lapses), activate/deactivate keeps FSRS data — UnitTests/Domain/`LearnerWordStateTests.cs`
- [x] Unit `ReviewLog` — factory sets all fields; type exposes no mutators — UnitTests/Domain/`ReviewLogTests.cs`
- [x] Unit `RecordVocabularyReviewCommandHandler` — first due attempt reschedules; retry and not-due only log; inactive word → NotFound — UnitTests/Features/VocabularySrs/`RecordVocabularyReviewCommandHandlerTests.cs`, UnitTests/`FixedTimeProvider.cs`
- [x] Unit `GetVocabularySessionQueryHandler` — order, cap minus today's count, time zone day boundary, child vs adult — UnitTests/Features/VocabularySrs/`GetVocabularySessionQueryHandlerTests.cs`
- [x] Unit learner-word handlers — state created on add, deactivated on remove, duplicate add idempotent — UnitTests/Features/LearnerWords/`RecordLearnerWordCommandHandlerTests.cs`
- [x] Unit `RepublishLearnerWordsCommandHandler` — batches, skips `SystemOwner`, count — Content UnitTests/Features/PersonalVocabulary/`RepublishLearnerWordsCommandHandlerTests.cs`
- [x] Integration Progress `POST reviews` — one row per call, rows never change on later calls, 400 on bad input, 401 without token — IntegrationTests/`VocabularySrsEndpointsTests.cs` (+ 404, 429), `TestJwtTokenFactory.cs`
- [x] Integration Progress `GET session` — child and adult caps, due before new — IntegrationTests/`VocabularySrsEndpointsTests.cs`
- [x] Integration Progress `GET words` and `settings` — child `PUT` → 403, adult `PUT` → 200 — IntegrationTests/`VocabularySrsEndpointsTests.cs`; per D-4 child `PUT` → 200, not 403
- [x] Integration Progress consumer — `LearnerWordAdded` creates a state row; replay creates none — IntegrationTests/Messaging/`LearnerWordConsumerTests.cs`
- [x] Integration Content republish — admin 200 + outbox messages per link; non-admin 403 — Content IntegrationTests/Messaging/`LearnerWordEventPublishingTests.cs`

## Fix round 1 (review round 1)

- [x] #1 major — duplicate concurrent answers: unique index `ReviewLogs (UserId, SessionId, SenseId, AttemptNo)` + `RowVersion` on `LearnerWordStates`; duplicate key / concurrency → `409 LearnerWordState.ConcurrentUpdate` — `ReviewLogConfiguration.cs`, `LearnerWordStateConfiguration.cs`, `LearnerWordStateRepository.cs`, migration `20261007050150_AddReviewConcurrencyGuards*.cs` + snapshot, `docs/database-diagram/progress.md`; tests `RecordVocabularyReviewCommandHandlerTests.cs`, `VocabularySrsEndpointsTests.cs` (parallel POSTs + deterministic stale save)
- [x] #2 nit — backfill keyset paging (`Id > lastLinkId`) — `IVocabularyWordRepository.cs`, `VocabularyWordRepository.cs`, `ILearnerWordEventPublisher.cs` (`LearnerWordLink.LinkId`), `RepublishLearnerWordsCommandHandler.cs`, `RepublishLearnerWordsCommandHandlerTests.cs`
- [x] #3 nit — `age_group` difference (Progress → Child, Content fails) documented — Progress `README.md`
- [x] #4 nit — rate limit kept as code constant; noted — Progress `README.md`
