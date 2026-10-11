# Tasks: WB-29 Vocabulary challenges

## Backend

- [x] Shared.Contracts: add `LearnerProfileUpdated` (incl. `TimeZoneId`, `IsGuardian`), bump version, pack to local feed — restores in Identity and Progress — Shared.Contracts/Profiles/LearnerProfileUpdated.cs, csproj 1.4.0 (packed to local feed); Identity + Progress refs bumped
- [x] Identity Domain: `User.TimeZoneId` + `SetTimeZone` (IANA validation) and `User.IsGuardian` + `SetGuardian` (adult only) — invalid input returns typed `UserErrors` — Identity.Domain User.cs, UserErrors.cs
- [x] Identity Infrastructure: migration `AddTimeZoneAndGuardian` — applies on existing DB, defaults null / false — Identity.Infrastructure UserConfiguration.cs, Migrations/20261011125826_AddTimeZoneAndGuardian*, snapshot; docs/database-diagram/identity.md
- [x] Identity Application/Api: `UpdateProfileTimeZone` command + `PUT /api/auth/profile/time-zone`; register accepts optional `timeZoneId` — validator rejects unknown zones — Features/Profile/Commands/UpdateProfileTimeZone/*, ProfileController, RegisterUser command/validator/handler, API models
- [x] Identity Application/Api: admin `SetGuardianRole` command + `PUT /api/auth/admin/users/{id}/guardian` (policy `AdminOnly`) — non-admin gets 403 — Features/Profile/Commands/SetGuardianRole/*, Api/Controllers/AdminUsersController.cs
- [x] Identity: `guardian` JWT claim when `IsGuardian` — token contains claim only for guardians — Infrastructure/Services/JwtTokenGenerator.cs, UserDto
- [x] Identity: publish `LearnerProfileUpdated` after register, alias/avatar, time zone, guardian change (`DisplayName` null for Child) — one event per success — ILearnerProfileEventPublisher + OutboxLearnerProfileEventPublisher; wired in register, alias, time zone, guardian handlers
- [x] Identity: one-off backfill that publishes `LearnerProfileUpdated` for all existing users (admin endpoint or startup flag) — idempotent, safe to rerun — RepublishLearnerProfiles command + POST api/auth/admin/learner-profiles/republish (admin), IUserRepository.GetPageAsync
- [x] Progress Domain: `Challenge`, enums, `ChallengeErrors` (`AudienceMismatch`, `Full`, `NotOpen`, `NoConsent`, `NotGuardian`, `NoActiveLink`) with forward-only lifecycle — invalid transitions return `Result.Failure` — Progress.Domain/Challenges/Challenge.cs, ChallengeStatus/Kind/Metric/Audience.cs, ChallengeErrors.cs, ChallengeOptions.cs
- [x] Progress Domain: `ChallengeParticipant`, `ChallengeProfile` (with `TimeZoneId`, `IsGuardian`), `ChallengeConsent` — no public setters — Challenges/ChallengeParticipant.cs, ChallengeProfile.cs, ChallengeConsent.cs
- [x] Progress Domain: `ChallengeTemplates` launch set — Listening Sprint never yields a Child instance — Challenges/ChallengeTemplate.cs, ChallengeTemplates.cs
- [x] Progress Domain: `LearnerDayResolver` — local day from IANA zone; unset/unknown → UTC — Challenges/LearnerDayResolver.cs
- [x] Progress Domain: `ChallengeScorer` with point rules, ignore rules, daily goal (10) and 200 cap per local day — pure, settings injected — Challenges/ChallengeScorer.cs
- [x] Progress Infrastructure: EF configs + migration `AddChallenges` (+ `ReviewLogs(UserId, OccurredAtUtc)` index if missing) — applies on existing DB — Infrastructure/Persistence/Configurations/Challenge*Configuration.cs (4), ProgressDbContext.cs, Migrations/20261011130627_AddChallenges*, snapshot; docs/database-diagram/progress.md + README.md (ReviewLogs UserId+OccurredAtUtc index already existed)
- [x] Progress Infrastructure: repositories — interfaces in Application — Application/Interfaces/IChallengeRepository.cs, IChallengeProfileRepository.cs, IReviewLogRepository.GetByUsersAsync; Infrastructure/Repositories/ChallengeRepository.cs, ChallengeProfileRepository.cs, ReviewLogRepository.cs
- [x] Progress Infrastructure: `LearnerProfileUpdatedConsumer` (idempotent upsert, ignores older events; role removed → delete consents given by that guardian) — duplicate message leaves one row — Progress.Infrastructure/Messaging/LearnerProfileUpdatedConsumer.cs, LearnerProfileQueues.cs; Application Features/Challenges/Commands/ApplyLearnerProfileEvent/*
- [x] Progress Infrastructure: extend existing `SupportLinkRevokedConsumer` to delete matching `ChallengeConsent` — consent gone after revoke — done in Application ApplySupportLinkEventCommandHandler (called by the existing SupportLinkRevokedConsumer) + Features/Challenges/ChallengeConsentCleaner.cs
- [x] Progress Application: `ListChallenges` query — caller's audience only; Child without consent gets empty list — Features/Challenges/Queries/ListChallenges/*, DTOs/ChallengeDtos.cs
- [x] Progress Application: `GetChallengeBoard` query, cache `progress:challenge-board:{id}` — Child DTO has only alias, avatar, own rank, top 5; no ranks/rewards beyond rank — Features/Challenges/Queries/GetChallengeBoard/*, ChallengeCache.cs
- [x] Progress Application: `JoinChallenge` / `LeaveChallenge` — audience, status, consent checks; at capacity returns `Challenge.Full`; cache key deleted — Features/Challenges/Commands/JoinChallenge/*, LeaveChallenge/*
- [x] Progress Application: `SetChildChallengeConsent` — fails unless caller is Guardian **and** has active link to child — Features/Challenges/Commands/SetChildChallengeConsent/*
- [x] Progress Application: `AdvanceChallengeLifecycle` — creates next instances, moves statuses by time, no overflow instances — Features/Challenges/Commands/AdvanceChallengeLifecycle/*
- [x] Progress Application: `RecomputeChallengeScores` — scores and dense ranks from `ReviewLog` using each profile's time zone; idempotent — Features/Challenges/Commands/RecomputeChallengeScores/*, ChallengeScoreProjector.cs
- [x] Progress Infrastructure: `ChallengeSchedulerHostedService` (1-min lifecycle, 5-min recompute) — survives handler failure, template logging — Infrastructure/Challenges/ChallengeSchedulerHostedService.cs, registered in Infrastructure ServiceCollectionExtensions
- [x] Progress Api: `ChallengesController` + `GuardianOnly` policy + rate limit on join + `Challenges` config section — failures via `ToProblemResult` — Api/Controllers/ChallengesController.cs, Authorization/ChallengeAuthorization.cs, RateLimitingConfiguration, ResultExtensions, Models/ChallengeRequests.cs, Program.cs, appsettings.json
- [x] Identity + Progress README and `.http` samples — new endpoints documented — Identity + Progress README.md and Identity.Api.http / Progress.Api.http

## Frontend

- [x] `types/index.ts`: challenge types, `timeZoneId`, `isGuardian` — union literals, no `enum`, no `any` — src/types/index.ts
- [x] `api/challenges.ts` (5 functions) + `api/auth.ts` `updateTimeZone`; register sends browser zone — use `apiClient` — src/api/challenges.ts, src/api/auth.ts, src/hooks/useChallenges.ts, src/hooks/useSupportLinks.ts, src/pages/RegisterPage.tsx, src/utils/timeZone.ts
- [x] Profile settings: time zone select (IANA list) with mutation — saved value shown after reload — src/components/profile/TimeZoneForm.tsx, src/pages/ProfilePage.tsx
- [x] `pages/ChallengesPage.tsx` — join/leave, `Challenge.Full` message, child-without-consent message, `isError` handled — src/pages/ChallengesPage.tsx, src/components/challenges/ChallengeCard.tsx, challengeUi.ts
- [x] `pages/ChallengeBoardPage.tsx` — adult full table; child own rank + top 5 with alias + avatar — src/pages/ChallengeBoardPage.tsx
- [x] Supporter dashboard: "Allow challenges" toggle per child, visible only with `guardian` claim — mutation invalidates queries — src/components/challenges/ChallengeConsentToggle.tsx, src/pages/SupportLinksPage.tsx, src/utils/jwt.ts (no read endpoint for consent state: Allow and Stop buttons)
- [x] Routes in `App.tsx` + sidebar link — Framer Motion entrance, theme tokens only — src/App.tsx, src/layouts/AppLayout.tsx

## Tests

- [x] Unit (Identity): `SetTimeZone` valid/invalid; `SetGuardian` adult ok, child fails — Identity.UnitTests/Domain/UserTimeZoneGuardianTests.cs
- [x] Unit (Identity): time zone and guardian handlers publish `LearnerProfileUpdated`; JWT has `guardian` claim only for guardians — Identity.UnitTests/Features/ProfileTimeZoneAndGuardianHandlerTests.cs (event per success); JWT claim asserted in Identity.IntegrationTests/ProfileChallengeEndpointsTests.cs (JwtTokenGenerator is internal to Infrastructure)
- [x] Unit (Identity): `SetGuardianRole` handler — unknown user, child user, success — Identity.UnitTests/Features/ProfileTimeZoneAndGuardianHandlerTests.cs
- [x] Unit: `Challenge` lifecycle transitions — valid and invalid — Progress.UnitTests/Challenges/ChallengeTests.cs
- [x] Unit: `ChallengeTemplates` — Listening Sprint adult only; audiences never mixed — Progress.UnitTests/Challenges/ChallengeTemplatesTests.cs
- [x] Unit: `LearnerDayResolver` — zone ahead/behind UTC across midnight; null and unknown → UTC — Progress.UnitTests/Challenges/LearnerDayResolverTests.cs
- [x] Unit: `ChallengeScorer` — +10 / +5 hint / +5 Recall upgrade / +10 daily goal at 10 correct due; 200 cap — Progress.UnitTests/Challenges/ChallengeScorerTests.cs
- [x] Unit: `ChallengeScorer` — cap and daily goal split by local day (e.g. `Asia/Ho_Chi_Minh` vs `America/New_York`) — Progress.UnitTests/Challenges/ChallengeScorerTests.cs
- [x] Unit: `ChallengeScorer` ignores non-due, wrong, < 800 ms / `TimingAdjusted`, first-review words, rows before join — Progress.UnitTests/Challenges/ChallengeScorerTests.cs
- [x] Unit: `ChallengeScorer` rebuild — same logs + profile give same score twice — Progress.UnitTests/Challenges/ChallengeScorerTests.cs
- [x] Unit: `JoinChallenge` — audience mismatch, `Challenge.Full` at capacity, not open, child without consent, success — Progress.UnitTests/Challenges/JoinChallengeCommandHandlerTests.cs
- [x] Unit: `SetChildChallengeConsent` — link without role fails, role without link fails, both succeed — Progress.UnitTests/Challenges/ChallengeConsentTests.cs
- [x] Unit: consumers — profile upsert idempotent; link revoke and guardian role removal delete consent — Progress.UnitTests/Challenges/LearnerProfileEventTests.cs, ChallengeConsentTests.cs, Features/SupportLinks/ApplySupportLinkEventCommandHandlerTests.cs
- [x] Unit: `GetChallengeBoard` — Child sees only own rank + top 5, no display name or user ids; Adult full board — Progress.UnitTests/Challenges/GetChallengeBoardQueryHandlerTests.cs
- [x] Unit: `RecomputeChallengeScores` + `AdvanceChallengeLifecycle` — ranks, status moves, no overflow — Progress.UnitTests/Challenges/ChallengeLifecycleAndScoresTests.cs (+ scheduler survives a failing handler); ChallengeTestKit.cs holds the fakes
- [x] Integration (Identity): time zone endpoint and admin guardian endpoint, incl. 403 for non-admin — Identity.IntegrationTests/ProfileChallengeEndpointsTests.cs (12 tests incl. 403, claim, events, backfill)
- [x] Integration (Progress): join → review logs → recompute → board for Adult and Child tokens; full challenge returns `Challenge.Full` — Progress.IntegrationTests/ChallengeEndpointsTests.cs (13 tests), Messaging/ChallengeEventConsumerTests.cs, ProgressApiFactory.cs (scheduler off), TestJwtTokenFactory.cs (guardian claim)
- [x] E2E API (`e2e/api`): child board shows only alias, avatar, own rank, top 5; non-guardian supporter cannot give consent — e2e/api/WordBuddy.E2E.Api.Tests/VocabularyChallengeTests.cs (3 tests; built, NOT run: needs the full stack, docker compose is ask-gated)
- [x] E2E UI (`e2e/ui`): adult joins Weekly League and sees board; child without consent sees guardian message — e2e/ui/features/vocabulary-challenges.feature, e2e/ui/steps/vocabulary-challenges.steps.ts, WordBuddy.UI ChallengeCard region label (bddgen passes; NOT run: needs the full stack)
