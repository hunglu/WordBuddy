# Tasks: WB-12_Defect on sharing word

## Backend — Content

- [x] Add `VocabularyWord.TransferToSystem()` — `Learner` + `Shared` → `Source = System`, owner `SystemOwner.UserId`, `OwnerAgeGroup = null`; status, `VisibleToChildren`, moderation fields kept; other states → `Conflict`. — Content.Domain/VocabularyWord.cs
- [x] Add `VocabularyWord.CancelShareRequest()` — `PendingReview` → `Private`; other states → `Conflict`. — Content.Domain/VocabularyWord.cs
- [x] Fix `VocabularyWord.IsVisibleTo` — a `Shared` word always applies the child filter, also when `Source = System`. — Content.Domain/VocabularyWord.cs
- [x] Fix `AddPersonalVocabularyWordCommandHandler.PickVisibleDuplicate` — system candidate must also pass `IsVisibleTo`. — AddPersonalVocabularyWordCommandHandler.cs
- [x] Move shared-pool cache keys to one Application static class; use it in `ModerateSharedVocabularyWordCommandHandler` — no behaviour change. — new Application/Caching/SharedVocabularyCacheKeys.cs; ModerateSharedVocabularyWordCommandHandler.cs
- [x] Add `Confirm` to `DeletePersonalVocabularyWordCommand`; bind `?confirm=true` in the controller — default `false`. — DeletePersonalVocabularyWordCommand.cs; PersonalVocabularyController.cs
- [x] Update `DeletePersonalVocabularyWordCommandHandler` — author + `Shared`/`PendingReview` + no confirm → 409 `PersonalVocabularyWord.DeleteConfirmationRequired`; `Shared` + confirm → transfer, unlink, invalidate both pool keys; `PendingReview` + confirm → cancel, unlink, delete if orphaned; transfer and unlink saved together. — DeletePersonalVocabularyWordCommandHandler.cs (uses tracked GetByIdAsync; UnlinkAsync saves both)
- [x] Add `RequestingUserId` to `GetSharedVocabularyWordsQuery`; return `IsMine` set after cache read — cache stays per age group, not per user. — GetSharedVocabularyWordsQuery(.Handler).cs; PersonalVocabularyWordDto.cs (`IsMine`); PersonalVocabularyController.cs
- [x] Remove `VocabularyWordIdRemap` entity, configuration, `DbSet`, repository + interface, `Features/VocabularyRemaps/**`, DTO, `InternalVocabularyRemapsController`, its models, DI wiring — Content builds. — deleted remap entity/config/repo/interface/DTO/feature/controller/models; ContentDbContext.cs; Application + Infrastructure ServiceCollectionExtensions.cs
- [x] Remove `InternalService` policy if grep shows no other user — no dangling references. — Api/Extensions/ServiceCollectionExtensions.cs; grep: no other user
- [x] Add migration `DropVocabularyWordIdRemaps` — table dropped; `Down` recreates it; no data changes. — Migrations/*_DropVocabularyWordIdRemaps(.Designer).cs; ContentDbContextModelSnapshot.cs; not applied (ask-gated)
- [x] Update Content `README.md` — no remap endpoints; delete/transfer rule and `?confirm=true` documented. — Content/README.md

## Backend — Progress

- [x] Remove `VocabularyIdRemapSyncService`, `ContentVocabularyRemapClient` + interface, `ServiceTokenProvider`, `ContentApiSettings` + validator — Progress builds. — deleted Infrastructure/Services/*, Settings/*; trimmed 4 packages in Infrastructure.csproj
- [x] Remove `SyncVocabularyWordIdRemaps` and `RemapVocabularyWordIds` features, `VocabularyWordIdRemapPair`, remap-only repository/domain methods, DI wiring — no remap references left (`grep -i remap`). — deleted features + DTO + IContentVocabularyRemapClient; IVocabularyRecallRepository.cs; VocabularyRecallRepository.cs; VocabularyRecallStat.cs; both ServiceCollectionExtensions.cs
- [x] Remove `ContentApi` section from `appsettings.json` — Progress starts without it. — Progress.Api/appsettings.json

## Deploy config

- [x] Remove `ContentApi__*` and service-token env vars for Progress from `docker-compose.yml` and `k8s/` — `grep ContentApi` returns nothing. — docker-compose.yml; k8s/progress-deployment.yaml (no service-token env existed; Jwt__Secret kept for token validation)

## Frontend

- [x] Add `isMine: boolean` to the shared-word type in `types/index.ts` — no `any`. — src/types/index.ts
- [x] Add optional `confirm` to the delete API function — sent as `?confirm=true`. — src/api/vocabulary.ts
- [x] Delete mutation invalidates mine + shared query keys — pool refreshes after delete. — src/hooks/useVocabulary.ts
- [x] My Vocabulary: confirm dialog for author delete of `Shared` ("handed over to WordBuddy, stays in the Community Word Pool") and `PendingReview` ("share request cancelled") words; Framer Motion, theme tokens — cancel keeps the word; confirm deletes with `confirm=true`. — new src/components/vocabulary/DeleteWordConfirmDialog.tsx; src/pages/VocabularyBuilderPage.tsx
- [x] Shared Pool: show "Your word" badge instead of **Add to My List** when `isMine` — button never shown for own words. — src/pages/VocabularySharedPoolPage.tsx

## Tests — Content

- [x] Unit: `VocabularyWord_TransferToSystem_SetsSystemOwnerAndKeepsShared` and `..._ReturnsConflictWhenNotShared` (Private, PendingReview, Rejected, System). — UnitTests/Domain/VocabularyWordTests.cs; UnitTests/TestWords.cs
- [x] Unit: `VocabularyWord_CancelShareRequest_SetsPrivate` and `..._ReturnsConflictWhenNotPendingReview`. — UnitTests/Domain/VocabularyWordTests.cs
- [x] Unit: `VocabularyWord_IsVisibleTo_HidesTransferredNonChildSafeWordFromChild` and `..._ShowsSystemLessonWordToChild` (no regression). — UnitTests/Domain/VocabularyWordTests.cs
- [x] Unit: `AddPersonalVocabularyWordCommandHandler_HandleAsync_SkipsInvisibleSystemDuplicateForChild`. — AddPersonalVocabularyWordCommandHandlerTests.cs
- [x] Unit: `DeletePersonalVocabularyWordCommandHandler_HandleAsync_ReturnsConflictWhenSharedAndNotConfirmed`. — DeletePersonalVocabularyWordCommandHandlerTests.cs (old "AuthorOfSharedWord_KeepsWord" test replaced)
- [x] Unit: `..._ReturnsConflictWhenPendingReviewAndNotConfirmed`. — DeletePersonalVocabularyWordCommandHandlerTests.cs
- [x] Unit: `..._TransfersToSystemAndInvalidatesCacheWhenSharedConfirmed` — both cache keys removed; `DeleteIfOrphanedAsync` not called. — DeletePersonalVocabularyWordCommandHandlerTests.cs
- [x] Unit: `..._CancelsShareRequestAndDeletesWhenPendingReviewConfirmed`. — DeletePersonalVocabularyWordCommandHandlerTests.cs
- [x] Unit: `..._AdopterUnlinksWithoutConfirmation` — no transfer, no 409. — DeletePersonalVocabularyWordCommandHandlerTests.cs
- [x] Unit: `RequestShareVocabularyWordCommandHandler_HandleAsync_ReturnsConflictForTransferredWord`. — RequestShareVocabularyWordCommandHandlerTests.cs
- [x] Unit: `GetSharedVocabularyWordsQueryHandler_HandleAsync_SetsIsMineForCaller` — cache hit and miss paths; System-owned word → `false`. — GetSharedVocabularyWordsQueryHandlerTests.cs (+ ...OnCacheHit)
- [x] Integration: `PersonalVocabularyEndpoints_Delete_Returns409WithoutConfirm` — Shared and PendingReview. — PersonalVocabularyEndpointsTests.cs
- [x] Integration: `PersonalVocabularyEndpoints_Delete_TransfersSharedWordToSystem` — 204; gone from author `/mine`; in `/shared` with `isMine = false`; adopter still has it in `/mine`. — PersonalVocabularyEndpointsTests.cs; VocabularyStorageEndpointsTests.cs (existing author-shared delete now uses ?confirm=true)
- [x] Integration: `PersonalVocabularyEndpoints_AddSharedToMine_FormerOwnerGetsAdopterLink` — `isAuthor = false`. — PersonalVocabularyEndpointsTests.cs
- [x] Integration: `PersonalVocabularyEndpoints_Delete_PendingReviewConfirmed_LeavesModerationQueue`. — PersonalVocabularyEndpointsTests.cs
- [x] Integration: `PersonalVocabularyEndpoints_SharedPool_HidesTransferredNonChildSafeWordFromChild` — also add-to-mine → 404 for Child. — PersonalVocabularyEndpointsTests.cs
- [x] Integration: `PersonalVocabularyEndpoints_SharedPool_IsMineTrueOnlyForOwner`. — PersonalVocabularyEndpointsTests.cs
- [x] Delete `InternalVocabularyRemapsEndpointsTests` and remap handler unit tests; integration DB has no `VocabularyWordIdRemaps` table. — deleted test files; TestJwtTokenFactory.cs (service token removed); UnifyVocabularyWordsMigrationTests.cs (pinned to UnifyVocabularyWords migration)

## Tests — Progress

- [x] Delete remap unit/integration tests and `ProgressApiFactory` remap overrides — `dotnet test` green. — deleted 7 test files; ProgressApiFactory.cs; UnitTests.csproj (Infrastructure ref removed)

## Tests — e2e

- [x] `e2e/api`: update `PersonalVocabularyTests` — 409 without confirm; confirm transfers word (stays in pool, `isMine = false`, former owner re-adds as adopter); `PendingReview` confirm-delete; remove or rewrite `InternalEndpointExposureTests` (routes gone → 404). — PersonalVocabularyTests.cs; InternalEndpointExposureTests.cs rewritten (404 on Content). Built; not run (needs full stack)
- [x] `e2e/ui`: scenarios — confirm dialog on shared-word delete with handover text; after confirm the word is gone from My Vocabulary and shown in the pool with **Add to My List**; owner sees "Your word" badge, not **Add to My List**, on own words. — new features/vocabulary-sharing.feature + steps/vocabulary-sharing.steps.ts. bddgen OK; not run (needs full stack)

## Fix round 1 (review.md)

- [x] #1 major: failed delete shows an error; confirm dialog stays open on error; plain delete 409 opens the confirm dialog. — WordBuddy.UI/src/pages/VocabularyBuilderPage.tsx, DeleteWordConfirmDialog.tsx (`errorMessage` prop)
- [x] #2 nit: cache removal after transfer can't fail the request — catch, log `Warning`, return success. — DeletePersonalVocabularyWordCommandHandler.cs; new unit test `..._ReturnsSuccessWhenCacheRemovalFailsAfterTransfer`
- [x] #3 nit: dialog closes on Escape and backdrop click, traps Tab focus, returns focus to the trigger. — DeleteWordConfirmDialog.tsx
- [x] #4 nit: LF restored on every file that was LF on `main` (new files follow their folder). Working-tree diff vs `main` = 1733+/2427− (code only), same as `--ignore-cr-at-eol`. — 36 files, no `.gitattributes`
