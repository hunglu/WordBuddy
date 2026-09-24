# Tasks: Vocabulary Builder & Recall Check-up

## Backend — Content service

- [x] Add `VocabularyShareStatus` enum (`Private`, `PendingReview`, `Shared`, `Rejected`) to
      `Content.Domain` — compiles, matches other service-local enum style (e.g. `LessonType`).
      — `WordBuddy.Content.Domain/VocabularyShareStatus.cs`
- [x] Add `PersonalVocabularyWord` entity to `Content.Domain` with `RequestShare()`, `Approve(bool
      visibleToChildren, Guid moderatorId)`, `Reject(Guid moderatorId)` domain methods — invalid
      state transitions (e.g. approving an already-`Shared` word) return a `Result`/are guarded,
      not silently allowed. — `WordBuddy.Content.Domain/PersonalVocabularyWord.cs`
- [x] Add `IPersonalVocabularyWordRepository` to `Content.Application.Interfaces` (`AddAsync`,
      `GetOwnedByIdAsync`, `GetByOwnerAsync`, `GetRandomByOwnerAsync`, `GetSharedAsync`,
      `GetPendingModerationAsync`, `UpdateAsync`, `DeleteAsync`) — interface only, no
      implementation yet. — `WordBuddy.Content.Application/Interfaces/IPersonalVocabularyWordRepository.cs`
      (also added `GetByIdAsync`, needed by moderation/adopt-shared-word flows which act on a
      word not owned by the caller)
- [x] Add EF Core configuration + `DbSet<PersonalVocabularyWord>` to `ContentDbContext`, and a
      migration — `dotnet ef migrations add AddPersonalVocabularyWords` produces a clean
      up/down migration against the Content DB. — `WordBuddy.Content.Infrastructure/Persistence/Configurations/PersonalVocabularyWordConfiguration.cs`,
      `ContentDbContext.cs`, `Persistence/Migrations/20260924082936_AddPersonalVocabularyWords.cs`
      (migration generated only — not applied; `dotnet ef database update` is ask-gated)
- [x] Implement `PersonalVocabularyWordRepository` in `Content.Infrastructure` — all interface
      methods work against a real SQL Server instance (verified by integration tests below).
      — `WordBuddy.Content.Infrastructure/Repositories/PersonalVocabularyWordRepository.cs`
- [x] `AddPersonalVocabularyWordCommand` + validator + handler — any authenticated user can add a
      word to their own list with `ShareStatus = Private`. — `Features/PersonalVocabulary/Commands/AddPersonalVocabularyWord/`
- [x] `RequestShareVocabularyWordCommand` + validator + handler — owner-only; moves
      Private/Rejected → PendingReview; fails `NotFound` if the word isn't found or isn't owned
      by the requester. — `Features/PersonalVocabulary/Commands/RequestShareVocabularyWord/`
- [x] `ModerateSharedVocabularyWordCommand` + validator + handler — admin-only; approve sets
      `Shared` + the moderator-chosen `VisibleToChildren`; reject sets `Rejected`.
      — `Features/PersonalVocabulary/Commands/ModerateSharedVocabularyWord/`
- [x] `AddSharedVocabularyWordToMyListCommand` + validator + handler — copies a `Shared` word
      into the caller's own list as a new `Private` entry. — `Features/PersonalVocabulary/Commands/AddSharedVocabularyWordToMyList/`
      (also blocks a Child caller from adopting a non-`VisibleToChildren` word, same NotFound as
      "not shared" — extends the plan's data-level child-safety enforcement to the adopt path)
- [x] `DeletePersonalVocabularyWordCommand` + validator + handler — owner-only delete.
      — `Features/PersonalVocabulary/Commands/DeletePersonalVocabularyWord/`
- [x] `GetMyVocabularyWordsQuery` + handler — returns the caller's full list regardless of
      `ShareStatus`. — `Features/PersonalVocabulary/Queries/GetMyVocabularyWords/`
- [x] `GetSharedVocabularyWordsQuery` + handler — returns only `Shared` items; when the caller's
      `AgeGroup` is `Child`, additionally filters to `VisibleToChildren == true` inside the
      handler itself (not just at the controller/policy level). — `Features/PersonalVocabulary/Queries/GetSharedVocabularyWords/`
- [x] `GetRandomVocabularyWordsForCheckQuery` + validator (`Count` 1–50) + handler — random
      subset of the caller's own words. — `Features/PersonalVocabulary/Queries/GetRandomVocabularyWordsForCheck/`
- [x] `GetPendingVocabularyModerationQuery` + handler — admin-only, lists `PendingReview` items.
      — `Features/PersonalVocabulary/Queries/GetPendingVocabularyModeration/`
- [x] Add `ClaimsPrincipalExtensions` (`GetUserId`, `GetAgeGroup`, `IsAdmin`) to `Content.Api`,
      mirroring Progress's existing extension. — `WordBuddy.Content.Api/Extensions/ClaimsPrincipalExtensions.cs`
      (Progress's own extension only had `GetUserId`; added `GetAgeGroup`/`IsAdmin` fresh here
      per the plan's spec, reading the same `age_group`/`is_admin` claims Identity already issues)
- [x] Register `AdminOnly` and `CanShareVocabulary` authorization policies in `Content.Api`'s
      `AddAuthorization` call. — `WordBuddy.Content.Api/Extensions/ServiceCollectionExtensions.cs`
- [x] `PersonalVocabularyController` (`api/vocabulary`) wiring all commands/queries above to HTTP
      verbs, `[Authorize]` by default, `[Authorize(Policy = "CanShareVocabulary")]` on the share
      endpoint, `[Authorize(Policy = "AdminOnly")]` on the moderation endpoints — every action
      maps `Result`/`Result<T>` via `ToProblemResult`. — `WordBuddy.Content.Api/Controllers/PersonalVocabularyController.cs`,
      `WordBuddy.Content.Api/Models/AddPersonalVocabularyWordRequest.cs`,
      `WordBuddy.Content.Api/Models/ModerateVocabularyWordRequest.cs`
- [x] Register all new handlers/validators in `Content.Application`'s
      `ServiceCollectionExtensions.AddApplication()` and the repository in
      `Content.Infrastructure`'s DI extension. — both `ServiceCollectionExtensions.cs` files
- [x] Cache `GetSharedVocabularyWordsQuery` (`content:vocabulary-shared:{childSafeOnly}`,
      absolute expiry) and invalidate both key variants on `ModerateSharedVocabularyWordCommand`
      success. — `GetSharedVocabularyWordsQueryHandler.cs`, `ModerateSharedVocabularyWordCommandHandler.cs`,
      `Content.Infrastructure/Extensions/ServiceCollectionExtensions.cs` (registers
      `AddDistributedMemoryCache()` — **flag for Sam**: no service in this repo wires a real
      `IDistributedCache`/Redis anywhere yet, no `redis` service in `docker-compose.yml`, despite
      CLAUDE.md listing Redis as the caching backend; code targets the `IDistributedCache`
      abstraction only, so swapping in `AddStackExchangeRedisCache` later is a DI-only change,
      but standing up real Redis infra was out of scope for this task)

## Backend — Progress service

- [x] Add `RecallStatus` enum (`Learning`, `Known`) to `Progress.Domain`. — `WordBuddy.Progress.Domain/RecallStatus.cs`
- [x] Add `VocabularyRecallStat` entity to `Progress.Domain` (`UserId`, `VocabularyWordId`,
      `Word`, `TimesChecked`, `TimesKnown`, `Status`, `LastCheckedAtUtc`) with an
      `ApplyCheckResult(bool known)` domain method that updates the counts and status.
      — `WordBuddy.Progress.Domain/VocabularyRecallStat.cs` (also added `UpdateWordText`, used
      by the repository to refresh the denormalized word text on a re-check)
- [x] Add `VocabularyRecallSession` entity to `Progress.Domain` (`UserId`, `CheckedAtUtc`,
      `WordsChecked`, `WordsKnown`). — `WordBuddy.Progress.Domain/VocabularyRecallSession.cs`
- [x] Add `IVocabularyRecallRepository` to `Progress.Application.Interfaces`
      (`UpsertStatsAsync`, `AddSessionAsync`, `GetStatsByUserAsync`,
      `GetRecentSessionsByUserAsync`). — `WordBuddy.Progress.Application/Interfaces/IVocabularyRecallRepository.cs`
- [x] Add EF Core configurations + `DbSet`s to `ProgressDbContext`, and a migration —
      `dotnet ef migrations add AddVocabularyRecall` produces a clean migration against the
      Progress DB. — `Persistence/Configurations/VocabularyRecallStatConfiguration.cs`,
      `VocabularyRecallSessionConfiguration.cs`, `ProgressDbContext.cs`,
      `Persistence/Migrations/20260924084632_AddVocabularyRecall.cs` (migration generated only —
      not applied; `dotnet ef database update` is ask-gated)
- [x] Implement the repository in `Progress.Infrastructure` against real SQL Server.
      — `WordBuddy.Progress.Infrastructure/Repositories/VocabularyRecallRepository.cs`
- [x] `SubmitVocabularyRecallCheckCommand` + validator (non-empty, max 50 results) + handler —
      upserts each word's `VocabularyRecallStat` and appends one `VocabularyRecallSession`.
      — `Features/VocabularyRecall/Commands/SubmitVocabularyRecallCheck/`
- [x] `GetVocabularyRecallProgressQuery` + handler — returns total/known/learning counts plus
      recent session history. — `Features/VocabularyRecall/Queries/GetVocabularyRecallProgress/`
      (recent-sessions list capped at the 10 most recent — not specified by the plan, a
      reasonable default for a trend view)
- [x] Wire both into `ProgressController` (or a new nested controller) under
      `api/progress/vocabulary-recall`, `[Authorize]`, using the existing
      `ClaimsPrincipalExtensions.GetUserId()`. — new `WordBuddy.Progress.Api/Controllers/VocabularyRecallController.cs`
      (kept separate from `ProgressController` per the plan's own suggestion), `Api/Models/SubmitVocabularyRecallCheckRequest.cs`
- [x] Register the new handlers/validators/repository in Progress's DI extensions.
      — `WordBuddy.Progress.Application/Extensions/ServiceCollectionExtensions.cs`,
      `WordBuddy.Progress.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

## Frontend

- [x] Add `vite.config.ts` proxy entry: `/api/vocabulary` → Content. — `vite.config.ts`
- [x] Add new types to `src/types/index.ts`: `VocabularyShareStatus`, `PersonalVocabularyWord`,
      `VocabularyRecallCheckWord`, `VocabularyRecallResultItem`, `VocabularyRecallProgress`,
      `VocabularyRecallSession` — no `any`. — `src/types/index.ts`
- [x] `src/api/vocabulary.ts` — typed Axios functions for all Content vocabulary endpoints
      (add/mine/share/delete/shared-pool/add-to-mine/random-for-check/pending-moderation/moderate).
      — `src/api/vocabulary.ts`
- [x] Extend `src/api/progress.ts` with `submitVocabularyRecallCheck` and
      `getVocabularyRecallProgress`. — `src/api/progress.ts`
- [x] `VocabularyBuilderPage.tsx` — add-word form (React Hook Form + Zod), personal word list
      with share-status badges, share/delete actions, all data via TanStack Query hooks (no raw
      `useEffect`/`fetch`). — `src/pages/VocabularyBuilderPage.tsx`, `src/hooks/useVocabulary.ts`
- [x] `VocabularyCheckPage.tsx` — configurable word count, flashcard-style check flow (Framer
      Motion transitions, no CSS transitions), submits results via the recall-check mutation.
      — `src/pages/VocabularyCheckPage.tsx`, `src/store/vocabularyCheckStore.ts` (persisted
      "last used count" preference, per the plan's Zustand suggestion)
- [x] `VocabularySharedPoolPage.tsx` — browse the `Shared` pool, "Add to my list" action.
      — `src/pages/VocabularySharedPoolPage.tsx`
- [x] `VocabularyModerationPage.tsx` — admin-only (guarded on `authStore`'s `user.isAdmin`),
      approve/reject queue with the "visible to children" toggle on approve.
      — `src/pages/VocabularyModerationPage.tsx`
- [x] Extend `ProgressPage.tsx` with a Vocabulary Recall section (known/learning counts via
      `CountUpStat`, recent sessions). — `src/pages/ProgressPage.tsx`
- [x] Add routes for the four new pages under `AppLayout` + nav links; moderation route
      redirects/hides for non-admins. — `src/App.tsx`, `src/layouts/AppLayout.tsx`

## Tests

- [x] Unit tests: `PersonalVocabularyWord` domain method transitions (`RequestShare`, `Approve`,
      `Reject`) — valid and invalid-transition cases. — `WordBuddy.Content.UnitTests/Domain/PersonalVocabularyWordTests.cs`
      (8 tests). Verified: `dotnet test WordBuddy.Content.UnitTests.csproj` — 26/26 passing
      (whole project, includes all Content unit tests below).
- [x] Unit tests: `AddPersonalVocabularyWordCommandHandler` — happy path, validation failure.
      — `Features/PersonalVocabulary/AddPersonalVocabularyWordCommandHandlerTests.cs`. Verified
      via the same 26/26 `WordBuddy.Content.UnitTests` run.
- [x] Unit tests: `RequestShareVocabularyWordCommandHandler` — happy path, not-owned/not-found
      case. — `Features/PersonalVocabulary/RequestShareVocabularyWordCommandHandlerTests.cs`.
      Verified via the same run.
- [x] Unit tests: `ModerateSharedVocabularyWordCommandHandler` — approve path (with/without
      `VisibleToChildren`), reject path, non-admin caller rejected at the authorization layer
      (covered by integration test, not the handler unit test). — `Features/PersonalVocabulary/ModerateSharedVocabularyWordCommandHandlerTests.cs`
      (mocks `IDistributedCache` to verify cache-key invalidation on success). Verified via the
      same run.
- [x] Unit tests: `GetSharedVocabularyWordsQueryHandler` — Adult caller sees all `Shared` items,
      Child caller sees only `VisibleToChildren == true` items (explicit child-vs-adult coverage
      per repo convention). — `Features/PersonalVocabulary/GetSharedVocabularyWordsQueryHandlerTests.cs`.
      Verified via the same run.
- [x] Unit tests: `GetRandomVocabularyWordsForCheckQueryHandler` — returns at most `Count` items,
      only from the caller's own list. — `Features/PersonalVocabulary/GetRandomVocabularyWordsForCheckQueryHandlerTests.cs`.
      Verified via the same run.
- [x] Unit tests: `AddSharedVocabularyWordToMyListCommandHandler` — happy path, source word not
      `Shared` (or not found) failure case. — `Features/PersonalVocabulary/AddSharedVocabularyWordToMyListCommandHandlerTests.cs`.
      Verified via the same run.
- [x] Unit tests: `DeletePersonalVocabularyWordCommandHandler` — happy path, not-owned/not-found
      case. — `Features/PersonalVocabulary/DeletePersonalVocabularyWordCommandHandlerTests.cs`.
      Verified via the same run.
- [x] Unit tests: `VocabularyRecallStat.ApplyCheckResult` — known/unknown transitions update
      `Status`/counts correctly across repeated checks. — `WordBuddy.Progress.UnitTests/Domain/VocabularyRecallStatTests.cs`
      (4 tests). Verified: `dotnet test WordBuddy.Progress.UnitTests.csproj` — 10/10 passing
      (whole project, includes both Progress unit test files below).
- [x] Unit tests: `SubmitVocabularyRecallCheckCommandHandler` — new word (creates a stat),
      existing word (updates a stat), validation failure (empty/oversized batch). — `Features/VocabularyRecall/SubmitVocabularyRecallCheckCommandHandlerTests.cs`.
      Verified via the same run.
- [x] Unit tests: `GetVocabularyRecallProgressQueryHandler` — known/learning counts and recent
      sessions computed correctly. — `Features/VocabularyRecall/GetVocabularyRecallProgressQueryHandlerTests.cs`.
      Verified via the same run.
- [x] Integration tests (Content, `WebApplicationFactory` + real SQL Server): add → get-mine →
      share → (as admin) moderate-approve → appears in shared pool; a Child-authenticated
      request to the shared pool never receives a non-`VisibleToChildren` item; a non-admin
      caller gets 403 from the moderation endpoint; a non-owner caller gets 404 from
      share/delete on someone else's word. — `WordBuddy.Content.IntegrationTests/ContentApiFactory.cs`,
      `TestJwtTokenFactory.cs`, `PersonalVocabularyEndpointsTests.cs` (4 tests). **Written, not
      run** — this sandbox has no reachable SQL Server (no LocalDB runtime, Docker daemon not
      started; starting one is a `docker compose up`-equivalent action, ask-gated). Confirmed the
      harness itself is correctly wired (compiles; `dotnet test` gets past config/JWT-auth setup
      and fails only at `SqlException`/"Unable to locate a Local Database Runtime installation" —
      i.e. the *only* remaining blocker is DB connectivity, not test or app code). Run against a
      live SQL Server via `dotnet test src/Services/Content/WordBuddy.Content.IntegrationTests` —
      optionally set `CONTENT_TEST_CONNECTION_STRING` to point at a specific instance.
- [x] Integration tests (Progress, `WebApplicationFactory` + real SQL Server): submit a recall
      check → `GetVocabularyRecallProgress` reflects updated counts and a new session entry.
      — `WordBuddy.Progress.IntegrationTests/ProgressApiFactory.cs`, `TestJwtTokenFactory.cs`,
      `VocabularyRecallEndpointsTests.cs` (2 tests, including a last-check-wins case). **Written,
      not run** — same reason and same verified state as the Content integration tests above
      (`PROGRESS_TEST_CONNECTION_STRING` to override).
- [x] E2E coverage (flag for the `/test` stage — `tester` subagent owns the actual `e2e/`
      files): add a personal word, run a recall check, see it reflected on the Progress page;
      share a word as an adult account, approve as admin, see it in another account's shared
      pool. — no coder action: per `plan.md`'s workflow, `e2e/api` and `e2e/ui` are owned and
      written by the `tester` subagent during `/test`, not `/code`; this line exists in
      `tasks.md` purely as a scope note for that later stage.
