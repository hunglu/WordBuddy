# Tasks: Refactor database scheme to store vocabulary item

## Shared

- [ ] Add `VocabularyWordIdsRemapped` (list of `OldId`/`NewId` pairs) to `WordBuddy.Shared.Contracts` and bump its minor version — the package packs into `local-nuget-feed`

## Backend — Content domain

- [x] Add `SystemOwner.UserId` constant and `VocabularySource { System, Learner }` enum — XML-documented, Domain-only — `Content.Domain/SystemOwner.cs`, `VocabularySource.cs`
- [x] Add `VocabularyWord` entity (fields per plan, `ContentHash`/`NormalizedWord` computed in factory, `CreateSystem`/`CreateLearner` factories, moved `RequestShare`/`Approve`/`Reject` keeping `PersonalVocabularyWord.*` error codes) — `CreateLearner` rejects `SystemOwner.UserId` — `Content.Domain/VocabularyWord.cs` (SHA-256 hex over UTF-16LE normalized parts, mirrors the migration SQL; also `IsVisibleTo`; `RequestShare` rejects System words)
- [x] Add `UserVocabularyWord` (`UserId`, `VocabularyWordId`, `AddedAtUtc`, `IsAuthor`) and `LessonVocabularyWord` (`LessonId`, `VocabularyWordId`, `SortOrder`) — `Lesson` exposes its words through the link — `Content.Domain/UserVocabularyWord.cs`, `LessonVocabularyWord.cs`, `Lesson.cs` (`VocabularyWords`, `AddVocabularyWord`)
- [x] Remove `VocabularyItem` and `PersonalVocabularyWord` — the solution builds with no references left — deleted both entities, their EF configs, `PersonalVocabularyWordRepository`, `IPersonalVocabularyWordRepository`, `PersonalVocabularyWordTests`; `DataSeeder.cs` uses `VocabularyWord.CreateSystem`

## Backend — Content infrastructure

- [x] EF configurations for `VocabularyWords`, `UserVocabularyWords` (unique `UserId+VocabularyWordId`), `LessonVocabularyWords` (composite PK, Lesson cascade, word NoAction), `VocabularyWordIdRemaps`; unique `(ContentHash, OwnerUserId)` filtered `WHERE Source = 'Learner'` (identical system/lesson words are allowed) — `ContentDbContext` exposes the new DbSets only — `Configurations/VocabularyWordConfiguration.cs`, `UserVocabularyWordConfiguration.cs`, `LessonVocabularyWordConfiguration.cs`, `VocabularyWordIdRemapConfiguration.cs`, `LessonConfiguration.cs`, `Persistence/VocabularyWordIdRemap.cs`, `ContentDbContext.cs`
- [x] Migration `UnifyVocabularyWords` with the data-moving SQL from plan "Migration rules", steps 1–6, including `Down` — generated, not applied (ask-gated) — `Migrations/20261002092015_UnifyVocabularyWords{,.Designer}.cs`, `ContentDbContextModelSnapshot.cs`; Shared/PendingReview learner rows are never collapsed into system/other-owner words (pool + moderation queue keep every entry)
- [x] Replace `IPersonalVocabularyWordRepository` with `IVocabularyWordRepository` plus EF implementation (link/unlink/orphan delete/random-by-link/shared/pending) — DI is registered in `AddInfrastructure` — `Application/Interfaces/IVocabularyWordRepository.cs` (+ `GetLinkAsync`; `AddAsync` saves word + author link together), `Infrastructure/Repositories/VocabularyWordRepository.cs`, `Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- [x] Update `ILessonRepository.GetByIdWithDetailsAsync` to load words via `LessonVocabularyWords` ordered by `SortOrder` — `LessonDetailDto` JSON is unchanged — `LessonRepository.cs`, `GetLessonDetailQueryHandler.cs`
- [ ] Add `VocabularyIdRemapPublisher` `BackgroundService` that publishes unpublished remap rows in batches of 500 and stamps `PublishedAtUtc` — a publish failure leaves the rows unpublished and is logged at Warning

## Backend — Content application

- [x] `AddPersonalVocabularyWord`: link to a visible existing word with the same hash, or create a learner word with an author link — returns the word id; never links to a word the caller couldn't see — `AddPersonalVocabularyWordCommandHandler.cs` (precedence: own word, system word, visible shared word)
- [x] `AddSharedVocabularyWordToMyList`: link instead of copy, idempotent — returns the shared word id; child/not-shared checks unchanged — `AddSharedVocabularyWordToMyListCommandHandler.cs`, controller doc comment
- [x] `DeletePersonalVocabularyWord`: unlink, and delete the word only if the caller is the author, it has no other links, and it is not shared — 404 when there is no link — `DeletePersonalVocabularyWordCommandHandler.cs` (orphan check incl. lesson links lives in `DeleteIfOrphanedAsync`)
- [x] `RequestShareVocabularyWord`: author-only — non-authors get `PersonalVocabularyWord.InvalidShareRequest` Conflict — `RequestShareVocabularyWordCommandHandler.cs`
- [x] `ModerateSharedVocabularyWord`, `GetSharedVocabularyWords`, `GetPendingVocabularyModeration` on `VocabularyWord` — responses are byte-compatible with today — `ModerateSharedVocabularyWordCommandHandler.cs`, `GetSharedVocabularyWordsQuery{,Handler}.cs`, `GetPendingVocabularyModerationQueryHandler.cs`; only change is the additive `isAuthor` (always false on these lists)
- [x] `GetMyVocabularyWords`, `GetRandomVocabularyWordsForCheck` project from links; add `IsAuthor` to `PersonalVocabularyWordDto` — existing fields keep today's values — `GetMyVocabularyWordsQueryHandler.cs`, `GetRandomVocabularyWordsForCheckQueryHandler.cs`, `DTOs/PersonalVocabularyWordDto.cs`, new `DTOs/PersonalVocabularyWordMapper.cs`

## Backend — Progress

- [x] `RemapVocabularyWordIds` command + validator + handler (rewrite in place, or merge counters into the existing `new` stat and delete `old`) — idempotent — `Progress.Application/Features/VocabularyRecall/Commands/RemapVocabularyWordIds/*`, `IVocabularyRecallRepository.cs`, `VocabularyRecallRepository.cs`, `VocabularyRecallStat.cs` (`RemapWordId`, `MergeFrom`), Application DI
- [ ] `VocabularyWordIdsRemappedConsumer` registered with MassTransit, calls the command — consumes the new contract package version
- [x] Check `VocabularyRecallSession` for stored word ids and apply the same remap if present — documented in the commit message either way — checked: sessions hold only UserId/CheckedAtUtc/WordsChecked/WordsKnown, no word ids, so no remap needed (noted in the handler XML doc)

## Frontend

- [x] Add `isAuthor: boolean` to the personal vocabulary type in `src/types/index.ts` — `tsc` strict passes — `WordBuddy.UI/src/types/index.ts`
- [x] Show the Share action in "My words" only when `isAuthor` — adopted/system words show no Share button; error states unchanged — `WordBuddy.UI/src/pages/VocabularyBuilderPage.tsx`

## Tests

- [x] Unit: `VocabularyWord` factories (hash normalization, system-owner guard) and share/approve/reject transitions — one `[Fact]` per transition, including invalid ones — `Content.UnitTests/Domain/VocabularyWordTests.cs`, `TestWords.cs`
- [x] Unit: `AddPersonalVocabularyWordCommandHandler` — new word, links to own duplicate, links to system word, links to child-visible shared word for Child, does NOT link to non-child-visible shared word for Child, does NOT link to another learner's private word — `AddPersonalVocabularyWordCommandHandlerTests.cs`
- [x] Unit: `AddSharedVocabularyWordToMyListCommandHandler` — link created, idempotent second call, Child is blocked from a non-child-visible word, not-shared word gives 404 — `AddSharedVocabularyWordToMyListCommandHandlerTests.cs`
- [x] Unit: `DeletePersonalVocabularyWordCommandHandler` — author orphan is deleted, author with other links is only unlinked, non-author is only unlinked, shared word is kept, no link gives NotFound — `DeletePersonalVocabularyWordCommandHandlerTests.cs`
- [x] Unit: `RequestShareVocabularyWordCommandHandler` — the author succeeds, a non-author gets Conflict — `RequestShareVocabularyWordCommandHandlerTests.cs`
- [x] Unit: `GetMyVocabularyWords`/`GetRandomVocabularyWordsForCheck` handlers — DTO values match the legacy semantics, `IsAuthor` is correct — `GetMyVocabularyWordsQueryHandlerTests.cs`, `GetRandomVocabularyWordsForCheckQueryHandlerTests.cs` (+ Moderate/GetShared tests ported)
- [x] Unit (Progress): `RemapVocabularyWordIdsCommandHandler` — in-place rewrite, merge when a target stat exists, idempotent re-run — `Progress.UnitTests/Features/VocabularyRecall/RemapVocabularyWordIdsCommandHandlerTests.cs`
- [x] Integration (Content, real SQL Server): migration test that seeds the old schema at `AddPersonalVocabularyWords`, then migrates — lesson word ids are preserved, adopted copies are merged, same-owner duplicates are merged, different-owner private duplicates are kept, remap rows are correct, `Down` succeeds — `Content.IntegrationTests/UnifyVocabularyWordsMigrationTests.cs` (run on LocalDB)
- [x] Integration (Content): every `/api/vocabulary/*` endpoint and `GET /api/lessons/{id}` for Child and Adult — status codes and JSON shapes match the existing integration tests — `Content.IntegrationTests/VocabularyStorageEndpointsTests.cs`, `ContentApiCollection.cs` (both API test classes share one factory/DB); run on LocalDB
- [ ] Integration (Progress): publish `VocabularyWordIdsRemapped` through the in-memory harness — stats are rewritten or merged
- [ ] E2E: re-run existing `e2e/api` and `e2e/ui` vocabulary scenarios, then add "adopt a shared word, then delete it from My words; it stays in the shared pool" — all green
