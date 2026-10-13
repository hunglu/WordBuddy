# Tasks: WB-20 Create User Profile Setting function

## Backend — Identity

- [x] Add `AgeBand` enum and `User.DateOfBirth`, `SetDateOfBirth`, `SetDisplayName`, `ChangePassword`, `GetAgeBand` + new `UserErrors` — domain rules from plan table hold — Identity.Domain `AgeBand.cs`, `User.cs`, `UserErrors.cs`
- [x] EF config + migration `AddUserDateOfBirth` (nullable `date`) — `dotnet ef migrations` script shows one nullable column only — `UserConfiguration.cs`, `Migrations/20261013034057_AddUserDateOfBirth*.cs`, snapshot, `docs/database-diagram/identity.md` (not applied to any DB)
- [x] `JwtTokenGenerator` adds `age_band` claim when date of birth is set — no date of birth in token — `JwtTokenGenerator.cs`
- [x] `UpdateDateOfBirth` command, validator, handler; `PUT /api/auth/profile/date-of-birth` returns new token — own account only — `Features/Profile/Commands/UpdateDateOfBirth/*`, `ProfileController.cs`, `SupportLinkRequests.cs`, Application DI
- [x] `UpdateDisplayName` command, validator, handler; `PUT /api/auth/profile/display-name`; publish `LearnerProfileUpdated` — child event keeps `DisplayName = null` — `Features/Profile/Commands/UpdateDisplayName/*`, `ProfileController.cs`
- [x] `ChangePassword` command, validator, handler; `POST /api/auth/profile/password` with auth rate-limit policy — wrong current password → `400` — `Features/Profile/Commands/ChangePassword/*`, `ProfileController.cs`, `RateLimitingConfiguration.cs` (`password-change`, 5 per minute per user)
- [x] Extend `GetCurrentUser` DTO with `email`, `dateOfBirth`, `ageBand` — returned only for the caller — `UserDto.cs`
- [x] Extend `GetMySupportLinks` `asSupporter` Active entries with learner `dateOfBirth` — absent for pending/ended links — `SupportLinkDtos.cs`, `GetMySupportLinksQueryHandler.cs`
- [x] Delete `me` cache key after each command (if cached); no date of birth / password in any log — grep check clean — `me` is not cached; `UpdateDateOfBirthCommandHandler` deletes the supporters' `identity:supportlinks:{id}` keys because that list now carries the date

## Backend — Progress

- [x] `VocabularyGradingOptions.AgeBandMultiplier` + `appsettings.json` defaults — options bind — Progress.Domain `AgeBand.cs`, `VocabularyGradingOptions.cs`, `appsettings.json`
- [x] `AnswerGrader.Grade` takes optional band; band wins, else `AgeGroup` multiplier — WB-22 behaviour unchanged without band — `AnswerGrader.cs`
- [x] `ClaimsPrincipalExtensions.GetAgeBand()`; pass band into `RecordVocabularyReviewCommand` — unknown claim → null — `ClaimsPrincipalExtensions.cs`, `VocabularySrsController.cs`, `RecordVocabularyReviewCommand.cs`, handler
- [x] Update Progress `README.md` grading section — documents bands and fallback — `README.md`

## Frontend

- [x] Types: `AgeBand` union, `User` gains `email`, `dateOfBirth`, `ageBand`; support link learner `dateOfBirth` — no `any` — `types/index.ts` (`email` already existed)
- [x] API functions `updateDateOfBirth`, `updateDisplayName`, `changePassword` in `src/api/auth.ts` — typed responses — `api/auth.ts`
- [x] Hooks `useUpdateDateOfBirth`, `useUpdateDisplayName`, `useChangePassword` — invalidate `['currentUser']`, store returned token — `hooks/useProfile.ts`
- [x] `AccountInfoCard` with display name form — shows email and age group — `components/profile/AccountInfoCard.tsx`
- [x] `DateOfBirthForm` (RHF + Zod) — future date blocked client-side, server error shown — `components/profile/DateOfBirthForm.tsx`
- [x] `ChangePasswordForm` (RHF + Zod, confirm match) — success message, fields cleared — `components/profile/ChangePasswordForm.tsx`
- [x] `MySupportersCard` on `/profile` — child sees guardian first; `isError` handled — `components/profile/MySupportersCard.tsx`, `pages/ProfilePage.tsx`
- [x] `SupportLinksPage` shows learner date of birth read-only for supporters — no edit control — `pages/SupportLinksPage.tsx`, `components/support/supportUi.ts`

## Tests

- [x] Unit: `User.SetDateOfBirth` (future, too old, child ≥ 18, adult < 13, valid, null) — one fact per case — `Identity.UnitTests/Domain/UserProfileSettingsTests.cs`
- [x] Unit: `User.GetAgeBand` boundaries (6/7, 9/10, 12/13, 17/18, birthday today) — theory passes — same file
- [x] Unit: `User.SetDisplayName`, `ChangePassword` — length and hash rules — same file
- [x] Unit: `UpdateDateOfBirthCommandHandler`, `UpdateDisplayNameCommandHandler`, `ChangePasswordCommandHandler` — success, not found, wrong password; child and adult — `Identity.UnitTests/Features/ProfileSettingsHandlerTests.cs`
- [x] Unit: `JwtTokenGenerator` — `age_band` present only with date of birth; no date in claims — covered by Identity integration tests (generator is `internal` to Infrastructure): `ProfileSettingsEndpointsTests.cs`
- [x] Unit: `AnswerGrader` band multiplier and fallback — existing `AnswerGraderTests` still green — `Progress.UnitTests/Domain/AnswerGraderAgeBandTests.cs`
- [x] Unit: `GetAgeBand()` claim extension — missing/unknown → null — `Progress.IntegrationTests/AgeBandReviewTests.cs` (Api is `internal`; `InternalsVisibleTo` added for IntegrationTests)
- [x] Integration (Identity): three new endpoints + `me` + support links DTO — `400`/`401`/`200` paths, supporter cannot edit — `Identity.IntegrationTests/ProfileSettingsEndpointsTests.cs`
- [x] Integration (Identity): `LearnerProfileUpdated` payload has no date of birth — asserted — same file
- [x] Integration (Progress): review with `age_band` claim grades with band; old `ReviewLog` rows unchanged — `AgeBandReviewTests.cs` (asserts accepted and old rows unchanged; the band multiplier itself is asserted in the grader unit tests, because server-measured timing cannot be controlled over HTTP)
- [ ] E2E (tester): `/profile` scenarios for child and adult — date of birth, password change, supporters list — not done: needs a running docker stack with rebuilt images and the migration applied (ask-gated)
- [ ] Update `docs/features/user-profile.md` living spec — done in `/test`
