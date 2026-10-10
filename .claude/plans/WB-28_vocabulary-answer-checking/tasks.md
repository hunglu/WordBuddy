# Tasks: WB-28 Vocabulary answer checking

## Backend

- [x] Domain: add `VocabularyExercise` entity with one-shot `Answer(...)` — second answer returns failure — `Progress.Domain/VocabularyExercise.cs`
- [x] Domain: add `ExerciseSelector` (port of UI `pickExercise`) — same outcomes as the living-spec table, < 4 senses → Typing — `Progress.Domain/ExerciseSelector.cs`, `ExerciseLevel.cs`, `ExerciseChoice.cs`
- [x] Domain: add `AnswerChecker` — choice key match; Typing trim + case-insensitive match — `Progress.Domain/AnswerChecker.cs`
- [x] Domain: add `ResponseTimeEvaluator` + `ToleranceMs` (default 3000) in `VocabularyGradingOptions` — returns used ms + `TimingAdjusted` — `Progress.Domain/ResponseTimeEvaluator.cs`, `VocabularyGradingOptions.cs`
- [x] Domain: extend `ReviewLog` with `ExerciseId`, `ClientResponseMs`, `ServerResponseMs`, `TimingAdjusted` — still insert-only — `Progress.Domain/ReviewLog.cs`
- [x] Application: `IContentSenseClient` + Progress-owned `ContentSenseDto` — no reference to Content projects — `Application/Interfaces/IContentSenseClient.cs`, `DTOs/ContentSenseDto.cs`
- [x] Application: `IVocabularyExerciseRepository` — `Application/Interfaces/IVocabularyExerciseRepository.cs` (also `IReviewLogRepository.CountCorrectAsync`)
- [x] Application: `CreateVocabularyExerciseCommand` + validator + handler per `develop-webapi` — returns `VocabularyExerciseDto` without answer or sense ids in options — `Application/Features/VocabularySrs/Commands/CreateVocabularyExercise/*`, `DTOs/VocabularyExerciseDto.cs`
- [x] Application: rework `RecordVocabularyReviewCommand` to `{ExerciseId, Answer, ClientResponseMs, HintUsed}` — correctness and timing come from the server; `NotFound` / `Conflict` errors as in plan — `Application/Features/VocabularySrs/Commands/RecordVocabularyReview/*`
- [x] Application: add `IsCorrect`, `CorrectAnswer` to `VocabularyReviewResultDto` — `Application/DTOs/VocabularySrsDtos.cs`
- [x] Infrastructure: typed `ContentSenseClient` forwarding the caller's bearer token, `Services:Content:BaseUrl` config — Content errors map to `Content.Unavailable` — `Infrastructure/ContentClient/*`, `Extensions/ServiceCollectionExtensions.cs`, `Infrastructure.csproj`, `Api/appsettings.json`
- [x] Infrastructure: session senses cache `progress:session-senses:{sessionId}`, absolute expiry = session expiry — `Infrastructure/ContentClient/ContentSenseClient.cs`
- [x] Infrastructure: `VocabularyExerciseRepository` + EF configuration — `Repositories/VocabularyExerciseRepository.cs`, `Persistence/Configurations/VocabularyExerciseConfiguration.cs`, `ReviewLogConfiguration.cs`, `ReviewLogRepository.cs`, `ProgressDbContext.cs`
- [x] Infrastructure: migration `AddServerAnswerChecking` — new table + nullable ReviewLog columns — `Persistence/Migrations/20261010162311_AddServerAnswerChecking*`, `ProgressDbContextModelSnapshot.cs`, `docs/database-diagram/progress.md`, `docs/database-diagram/README.md` (not applied to a database)
- [x] Infrastructure: add Content check to `/health/ready` — `Infrastructure/ContentClient/ContentHealthCheck.cs`, `Api/Program.cs`
- [x] Api: `POST /api/progress/vocabulary/exercises` with `vocabulary-review` rate limit — 200 / 400 / 404 via `ToProblemResult` — `Api/Controllers/VocabularySrsController.cs`, `Extensions/ResultExtensions.cs` (`Content.Unavailable` → 503)
- [x] Api: slim `RecordVocabularyReviewRequest` (no `IsCorrect`, `ExerciseType`, `Skill`, `SessionId`, `SenseId`) — `Api/Models/VocabularySrsRequests.cs`, `Extensions/ServiceCollectionExtensions.cs` (evaluator registration), `Progress/README.md`
- [x] Config: `Services__Content__BaseUrl` in `docker-compose.yml` and Progress k8s manifest — no secrets added — `WordBuddy/docker-compose.yml`, `WordBuddy/k8s/progress-deployment.yaml`

## Frontend

- [x] Types: `VocabularyExercise`, `ExercisePrompt`, `ExerciseOption`; new `ReviewPayload`; review result adds `isCorrect`, `correctAnswer` — no `any` — `src/types/index.ts`
- [x] API: `postVocabularyExercise(sessionId, senseId)` in `src/api/progress.ts`
- [x] Hook: `useCreateExercise` mutation; review page no longer calls `useSenses` — `src/hooks/useVocabularyReview.ts` (`useSenses` kept: the dashboard still uses it)
- [x] Components: PictureChoice / ListeningChoice / Typing render from `VocabularyExercise` and submit option key or text — `src/components/vocabulary-review/*`
- [x] Page: `VocabularyReviewPage` uses server `isCorrect` for feedback and re-queue; 404 `SenseUnavailable` skips the item; `isError` handled — `src/pages/VocabularyReviewPage.tsx`
- [x] Cleanup: remove `pickExercise`, option building and `isTypingCorrect` from `sessionQueue.ts` — `src/components/vocabulary-review/sessionQueue.ts`

## Tests

- [x] Unit: `VocabularyExerciseTests` — answer once, second answer fails — `UnitTests/Domain/VocabularyExerciseTests.cs`
- [x] Unit: `ExerciseSelectorTests` — every row of the selection table + < 4 senses fallback — `UnitTests/Domain/ExerciseSelectorTests.cs`
- [x] Unit: `AnswerCheckerTests` — correct key, wrong key, Typing case/space variants, wrong text — `UnitTests/Domain/AnswerCheckerTests.cs`
- [x] Unit: `ResponseTimeEvaluatorTests` — accepted, client too fast (forged 0 ms), client > server, boundary at tolerance — `UnitTests/Domain/ResponseTimeEvaluatorTests.cs`
- [x] Unit: `CreateVocabularyExerciseCommandHandlerTests` — closed session, foreign session, sense not in session, sense missing in Content, Content down, options never expose sense ids — `UnitTests/Features/VocabularySrs/CreateVocabularyExerciseCommandHandlerTests.cs`
- [x] Unit: `RecordVocabularyReviewCommandHandlerTests` (update) — wrong answer → Again, adjusted timing affects rating, child × 1.25 path, already answered → Conflict, foreign exercise → NotFound — `UnitTests/Features/VocabularySrs/RecordVocabularyReviewCommandHandlerTests.cs`
- [x] Integration: `VocabularySrsEndpointsTests` — exercise → review happy path with a stubbed Content client — `IntegrationTests/VocabularySrsEndpointsTests.cs`, `StubContentHandler.cs`, `ProgressApiFactory.cs`
- [x] Integration: forged body `isCorrect: true` + wrong answer → rating Again, status not improved — `IntegrationTests/VocabularySrsEndpointsTests.cs`
- [x] Integration: replaying the same `exerciseId` → 409 — `IntegrationTests/VocabularySrsEndpointsTests.cs`
- [x] Integration: child token — Content client receives the child bearer token; hidden senses never used as prompt or distractor — `IntegrationTests/VocabularySrsEndpointsTests.cs`
- [x] Integration: migration applies on real SQL Server; old ReviewLog rows readable — `IntegrationTests/VocabularySrsEndpointsTests.cs` (`Migration_OldReviewLogRows_StayReadable`; the factory migrates the test database on start)
- [x] E2E API (`e2e/api`): session → exercise → wrong + right answers; forged correct flag rejected in effect — `e2e/api/.../VocabularySrsTests.cs`, `VocabularyReviewSessionTests.cs`, `SupporterDashboardTests.cs`, `VocabularyExercises.cs` (builds; not run, needs the full stack)
- [x] E2E UI (`e2e/ui`): daily review flow unchanged for adult and child, all 3 exercise types, wrong answer re-queued — `e2e/ui/features/vocabulary-review.feature`, `steps/vocabulary-review.steps.ts` (not run, needs the full stack; the 3-type scenario uses a stubbed server, see report)
