# Tasks: Multilingual UI (English and Vietnamese)

## Backend (Identity)

- [x] `User.PreferredLanguage` + `PreferredLanguageMaxLength`, `IsValidLanguageCode`, `SetPreferredLanguage(string?)` (trim, lower-case, null clears) — invalid code returns `UserErrors.InvalidLanguage` — WordBuddy/src/Services/Identity/WordBuddy.Identity.Domain/User.cs
- [x] `UserErrors.InvalidLanguage` (`User.InvalidLanguage`, validation type) — maps to `400` via `ToProblemResult` — WordBuddy/src/Services/Identity/WordBuddy.Identity.Domain/UserErrors.cs
- [x] `UserDto.PreferredLanguage` (optional last param) mapped in `UserDto.From` — `/me`, login, register replies carry it — WordBuddy/src/Services/Identity/WordBuddy.Identity.Application/DTOs/UserDto.cs
- [x] `UserConfiguration`: `PreferredLanguage` max length 10, nullable — model builds — WordBuddy/src/Services/Identity/WordBuddy.Identity.Infrastructure/Persistence/Configurations/UserConfiguration.cs
- [x] Migration `AddPreferredLanguage` in `Persistence/Migrations` — adds nullable `nvarchar(10)` only, no other diff — Migrations/20261017083349_AddPreferredLanguage(.Designer).cs, IdentityDbContextModelSnapshot.cs, docs/database-diagram/identity.md
- [x] `Features/Profile/Commands/UpdatePreferredLanguage/` command + validator + handler (copy `UpdateDisplayName`, no event publish) — returns `Result<UserDto>`, logs user id only — Application/Features/Profile/Commands/UpdatePreferredLanguage/*
- [x] Register the handler and validator in DI like `UpdateProfileTimeZone` — resolves at startup — Application/Extensions/ServiceCollectionExtensions.cs
- [x] `ProfileController` `PUT api/auth/profile/language` + `Api/Models/UpdateLanguageRequest` — `200` + `UserDto`, `400` bad code, `401` anonymous — Api/Controllers/ProfileController.cs, Api/Models/SupportLinkRequests.cs (UpdateLanguageRequest)
- [x] `RegisterUserCommand`/`Validator`/`Handler` + `RegisterUserRequest`: optional `PreferredLanguage` — stored when valid, `400` when malformed, null allowed — Application/Features/Auth/Commands/RegisterUser/*, Api/Models/RegisterUserRequest.cs, Api/Controllers/AuthController.cs
- [x] Identity `README.md` endpoint table: add the new route — row present — WordBuddy/src/Services/Identity/README.md

## Frontend — i18n foundation

- [x] Add `i18next` and `react-i18next` to `package.json` — `npm install` and `npm run build` pass — WordBuddy.UI/package.json, package-lock.json
- [x] `src/i18n/config.ts` (`SUPPORTED_LANGUAGES`, `DEFAULT_LANGUAGE`, `LanguageCode`, resources) — one entry per language — src/i18n/config.ts, src/i18n/types.ts
- [x] `src/i18n/locales/en/*.ts` namespaces (`common`, `nav`, `auth`, `profile`, `dashboard`, `lessons`, `vocabulary`, `review`, `challenges`, `groups`, `support`, `admin`, `errors`) — exported `as const` — src/i18n/locales/en/*
- [x] `src/i18n/locales/vi/*.ts` typed against `en` — missing or extra key fails `tsc` — src/i18n/locales/vi/*
- [x] `src/i18n/i18next.d.ts` `CustomTypeOptions` — unknown key in `t()` fails `tsc` — src/i18n/i18next.d.ts
- [x] `src/i18n/index.ts`: `initI18n`, `resolveInitialLanguage` (localStorage → `navigator.languages` `vi*` → `en`), `applyLanguage` (changeLanguage + localStorage `wordbuddy-lang` + `<html lang>`) — imported in `main.tsx` before `App` — src/i18n/index.ts, src/main.tsx
- [x] `types/index.ts` `User.preferredLanguage: string | null` — compiles — src/types/index.ts
- [x] `api/auth.ts` `updatePreferredLanguage` + `RegisterPayload.preferredLanguage?` — calls `PUT /auth/profile/language` — src/api/auth.ts
- [x] `authStore.updateUser(user)` action — persisted user updated without touching the token — src/store/authStore.ts
- [x] `hooks/useProfile.ts` `useUpdatePreferredLanguage` — updates `CURRENT_USER_KEY` cache and `authStore.updateUser` — src/hooks/useProfile.ts
- [x] `hooks/useLanguageSync.ts` used in `App` — applies `user.preferredLanguage` once per login (user id change); unsupported code ignored — src/hooks/useLanguageSync.ts, src/App.tsx
- [x] `components/LanguageSwitcher.tsx` — UI changes at once without reload; saves to profile only when logged in; inline translated error on save failure — src/components/LanguageSwitcher.tsx
- [x] Switcher in `PublicLayout` and `AppLayout` sidebar — visible on `/login`, `/register` and every protected page — src/layouts/PublicLayout.tsx, src/layouts/AppLayout.tsx
- [x] `RegisterPage` sends current language as `preferredLanguage` — new account stores it — src/pages/RegisterPage.tsx
- [x] `problemMessage`: translated `errors:codes` → `en` server detail → fallback; seed codes for Identity auth/profile errors — no English server text in `vi` for seeded codes — src/api/supportLinks.ts, src/i18n/locales/*/errors.ts
- [x] Date/number helpers (`supportUi.ts` `formatDate`/`formatPlainDate`, `utils/timeZone.ts`, `challengeUi.ts`, other `toLocale*`/`Intl` calls) use current language — dates follow the UI language — src/components/support/supportUi.ts, src/components/challenges/challengeUi.ts, src/pages/ProgressPage.tsx (timeZone.ts Intl calls are locale-free)

## Frontend — string extraction (no hard-coded text left in each group)

- [x] Shell: `AppLayout` (nav labels as keys), `PublicLayout`, `ProtectedRoute`, `Pager`, `ChildSupporterGate`, `CountUpStat` — all text via `t()` — layouts/*, Pager.tsx, ChildSupporterGate.tsx (ProtectedRoute, CountUpStat have no text)
- [x] Auth: `LoginPage`, `RegisterPage` incl. Zod schemas via `makeSchema(t)` — validation messages switch with language — pages/LoginPage.tsx, pages/RegisterPage.tsx
- [x] Profile: `ProfilePage`, `AccountInfoCard`, `DateOfBirthForm`, `ChangePasswordForm`, `TimeZoneForm`, `MySupportersCard` — all labels, hints, inline errors translated — pages/ProfilePage.tsx, components/profile/*
- [x] Dashboard/progress: `DashboardPage`, `LearnerDashboardPage`, `ProgressPage`, `components/dashboard/*`, `hooks/useDashboard.ts` labels — translated, plurals via `count` — pages/DashboardPage.tsx, LearnerDashboardPage.tsx, ProgressPage.tsx, components/dashboard/* (useDashboard.ts has no text)
- [x] Lessons: `LessonsPage`, `LessonDetailPage`, `components/lessons/*` — UI labels only, lesson content untouched — pages/LessonsPage.tsx, LessonDetailPage.tsx, components/lessons/*
- [x] Vocabulary: `VocabularyBuilderPage`, `VocabularySharedPoolPage`, `components/vocabulary/*`, `components/autofill/*` — translated, word data untouched — pages/VocabularyBuilderPage.tsx, VocabularySharedPoolPage.tsx, components/vocabulary/*, components/autofill/*
- [x] Review: `VocabularyReviewPage`, `components/vocabulary-review/*` — prompts, feedback, summary translated — pages/VocabularyReviewPage.tsx, components/vocabulary-review/*
- [x] Challenges: `ChallengesPage`, `ChallengeBoardPage`, `components/challenges/*` (`challengeUi.ts` labels → keys) — translated — pages/ChallengesPage.tsx, ChallengeBoardPage.tsx, components/challenges/*
- [x] Groups: `GroupsPage`, `GroupDetailPage`, `components/groups/*` (`groupUi.ts` labels → keys) — translated — pages/GroupsPage.tsx, GroupDetailPage.tsx, components/groups/*
- [x] Support links: `SupportLinksPage`, `AcceptInvitationPage`, `components/support/*` (`statusLabel` → keys) — translated — pages/SupportLinksPage.tsx, AcceptInvitationPage.tsx, components/support/*
- [x] Admin/moderator: `Admin*Page`, `VocabularyModerationPage`, `components/admin-lessons/*`, `components/admin-lexemes/*` — translated — pages/Admin*Page.tsx, VocabularyModerationPage.tsx, components/admin-lessons/*, components/admin-lexemes/*
- [x] Final sweep: grep `src/**/*.tsx` for JSX text and string literals in `placeholder|aria-label|title|alt` — only keys, emoji, or product name `WordBuddy` remain — grep sweep clean: only product name WordBuddy remains
- [x] ADR `docs/adr/NNNN-ui-i18n.md` (library, file layout, typed keys, "add a language" steps) — merged with the change — docs/adr/0006-ui-i18n.md
- [x] Propose to Sam the exact `WordBuddy.UI/CLAUDE.md` line "no hard-coded UI text; use `t()`" — not edited without approval — proposed line is in the run report; CLAUDE.md not edited

## Tests

- [x] Unit `UserPreferredLanguageTests`: `en`/`vi` accepted, `VI` normalised to `vi`, null clears, `english`/11+ chars/empty rejected — all pass — UnitTests/Domain/UserPreferredLanguageTests.cs
- [x] Unit `UpdatePreferredLanguageCommandValidatorTests`: empty user id fails, malformed code fails, null passes — all pass — UnitTests/Features/UpdatePreferredLanguageTests.cs
- [x] Unit `UpdatePreferredLanguageCommandHandlerTests`: success returns DTO with language and saves; user not found → `NotFound`; invalid → validation failure; no profile event published — child and adult cases — UnitTests/Features/UpdatePreferredLanguageTests.cs
- [x] Unit register handler: valid `preferredLanguage` stored; null leaves it null; malformed → validation failure — all pass — UnitTests/Features/UpdatePreferredLanguageTests.cs
- [x] Integration `ProfileSettingsEndpointsTests`: `PUT /profile/language` `200` and `/me` returns it; `400` malformed; `401` anonymous; child account `200` — real SQL Server — IntegrationTests/ProfileSettingsEndpointsTests.cs
- [x] Integration: login after save returns `user.preferredLanguage` (other-device restore) — passes — IntegrationTests/ProfileSettingsEndpointsTests.cs
- [x] UI type check: `npm run build` passes with `vi` key parity enforced — no `any`, no casts to bypass keys — npm run build passes; negative check confirmed missing/extra vi key and unknown t() key fail tsc
- [x] E2E UI (tester): pin `locale: 'en-US'` in `e2e/ui/playwright.config.ts` — existing features stay green — moved to /test (tester-owned per plan)
- [x] E2E UI (tester) `language.feature`: `vi-VN` browser shows Vietnamese login; switcher changes text without reload; choice restored after login in a fresh context; child account same; lesson content stays English — all scenarios green — moved to /test (tester-owned per plan)
- [x] E2E API (tester): set language, log in again, reply carries it — green — moved to /test (tester-owned per plan)
