# Tasks: WB-23_Vocabulary review exercises

## Backend — Content

- [x] Add `ImageAssetId` / `Image` + `AttachImage` to `Sense`, EF config — builds, nullable FK with `NoAction` (same as `Audio`) — Domain/Sense.cs, Persistence/Configurations/SenseConfiguration.cs
- [x] Migration `AddSenseImage` — applies cleanly on an existing DB; existing rows get `null` — Migrations/20261007150225_AddSenseImage(.Designer).cs, ContentDbContextModelSnapshot.cs, docs/database-diagram/content.md
- [x] `GetSensesByIdsQuery` + `Validator` (1–100 ids, no empty Guid) — invalid input returns validation `Result.Failure` — Queries/GetSensesByIds/GetSensesByIdsQuery.cs, GetSensesByIdsQueryValidator.cs, DTOs/SenseReviewDto.cs
- [x] `GetSensesByIdsQueryHandler` — filters with `Sense.IsVisibleTo`, drops unknown ids, `personalContext` only from caller's own link — Queries/GetSensesByIds/GetSensesByIdsQueryHandler.cs, Application/Extensions/ServiceCollectionExtensions.cs
- [x] Repository method to load senses (with audio/image) and caller links by ids — one round trip, no N+1 — Interfaces/IVocabularyWordRepository.cs, Interfaces/SenseReviewCandidate.cs, Repositories/VocabularyWordRepository.cs (GetForReviewAsync)
- [x] `GET /api/vocabulary/senses?ids=` on `PersonalVocabularyController` (authenticated, `ToProblemResult`) — 200 with list; 400 on >100 ids — Api/Controllers/PersonalVocabularyController.cs
- [x] Logging: counts only, no word text — reviewed in handler — GetSensesByIdsQueryHandler.cs (RequestedCount/ReturnedCount only)

## Frontend

- [x] Union types + DTO interfaces in `src/types/index.ts` — no `enum`, no `any` — src/types/index.ts
- [x] `getVocabularySession` / `postVocabularyReview` with `X-Client-CurrentDateTime` header — header has local offset — src/api/progress.ts
- [x] `getSensesByIds` (chunks of 100) — returns merged list — src/api/vocabulary.ts
- [x] Hooks `useVocabularySession`, `useSenses`, `useRecordReview` — keys `['vocabulary-session']`, `['senses', ids]` — src/hooks/useVocabularyReview.ts
- [x] `pickExercise` + session queue reducer (recognition first, re-queue on wrong, skip missing senses) — follows plan table — src/components/vocabulary-review/sessionQueue.ts
- [x] `PictureChoiceExercise`, `ListeningChoiceExercise`, `TypingExercise` — measure ms, report hint, 4 options or Typing fallback — src/components/vocabulary-review/{Picture,Listening}ChoiceExercise.tsx, TypingExercise.tsx, ChoiceOptions.tsx, useResponseTimer.ts
- [x] `PersonalContextNote` — shown only when `personalContext` is set — src/components/vocabulary-review/PersonalContextNote.tsx
- [x] `VocabularyReviewPage` at `/vocabulary/review` with `SessionSummary` — no self-rating; `isError` handled with retry — src/pages/VocabularyReviewPage.tsx, src/components/vocabulary-review/SessionSummary.tsx, src/App.tsx
- [x] Child time cap (10 min notice, 15 min stop) from `authStore.user.ageGroup` — adult has no cap — src/pages/VocabularyReviewPage.tsx
- [x] Replace "Recall check" nav with "Review"; `/vocabulary/check` redirects to `/vocabulary/review` — src/layouts/AppLayout.tsx, src/App.tsx, src/pages/VocabularyBuilderPage.tsx; deleted VocabularyCheckPage.tsx, store/vocabularyCheckStore.ts
- [x] Progress page: old recall section labelled "Past recall checks" — still renders — src/pages/ProgressPage.tsx

## Tests

- [x] Unit: `Sense.AttachImage` — `Sense_AttachImage_SetsImageAssetId` — UnitTests/Domain/SenseTests.cs
- [x] Unit: validator — empty list, >100 ids, empty Guid rejected — UnitTests/Features/PersonalVocabulary/GetSensesByIdsQueryValidatorTests.cs
- [x] Unit: handler — child gets no non-child-visible shared sense; adult gets it; unknown id omitted; other learner's private sense omitted; `personalContext` only from own link — UnitTests/Features/PersonalVocabulary/GetSensesByIdsQueryHandlerTests.cs
- [x] Integration (`WebApplicationFactory` + SQL Server): `senses?ids=` child vs adult with a hidden shared sense, a private foreign sense, an unknown id — hidden ones never in body; same 200 — IntegrationTests/PersonalVocabularyEndpointsTests.cs (LocalDB, passed)
- [x] Integration: migration `AddSenseImage` applies on seeded DB — IntegrationTests/AddSenseImageMigrationTests.cs (LocalDB, passed)
- [x] E2E API (`e2e/api`): adult full session — session → senses → one review per item → states change; repeat for child — e2e/api/WordBuddy.E2E.Api.Tests/VocabularyReviewSessionTests.cs (builds; not run, stack down)
- [x] E2E UI (`e2e/ui`, Gherkin): adult completes a session with all 3 exercise types, no self-rating shown — e2e/ui/features/vocabulary-review.feature, steps/vocabulary-review.steps.ts (bddgen OK; not run)
- [x] E2E UI: child session shows only child-visible words and stops at the time cap — same files, `page.clock` (bddgen OK; not run)
- [x] E2E UI: `/vocabulary/check` redirects to `/vocabulary/review` — e2e/ui/features/vocabulary.feature, steps/vocabulary.steps.ts (old recall-check steps removed; bddgen OK; not run)
