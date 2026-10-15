# Tasks: WB-56 Moderator role to assist admin

## Backend

### Identity

- [x] `User`: add `IsModerator`, `SetModerator(bool)`, `EnsureCanBeDeleted()` and errors `User.ModeratorMustBeAdult`, `User.ModeratorNotForAdmin`, `User.AdminCannotBeDeleted` — child grant and admin grant fail; revoke always succeeds; admin cannot be deleted. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Domain/User.cs, UserErrors.cs
- [ ] EF config + migration `AddUserIsModerator` (`bit NOT NULL DEFAULT 0`), ask-gated — migration applies on a clean DB and on an existing one. — BLOCKED: `dotnet ef migrations add` was denied; model + config done (UserConfiguration.cs)
- [x] `JwtTokenGenerator`: emit `moderator = "true"` only for moderators — the claim is absent for everyone else. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Infrastructure/Services/JwtTokenGenerator.cs
- [x] `UserDto`: add `IsModerator` — `GET /api/auth/me` and the login response carry it. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Application/DTOs/UserDto.cs
- [x] `AuthorizationPolicies.ManageSupportLinks` (admin OR moderator and not Child) + `ClaimsPrincipal.IsAdmin()` extension — registered in `ServiceCollectionExtensions`. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Api/Extensions/AuthorizationPolicies.cs, ClaimsPrincipalExtensions.cs, ServiceCollectionExtensions.cs
- [x] `SetModeratorRoleCommand` + validator + handler (pattern: `SetGuardianRole`) — not found → 404, domain errors → 400, success saves. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Application/Features/Profile/Commands/SetModeratorRole/*, Extensions/ServiceCollectionExtensions.cs
- [x] `PUT /api/auth/admin/users/{id}/moderator` in `AdminUsersController` (`AdminOnly`) + `SetModeratorRequest` model — returns 204. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Api/Controllers/AdminUsersController.cs, Models/SupportLinkRequests.cs
- [x] `GetUsersForAdminQuery` + validator + handler + repository method (adults only, search on email/display name, WB-55 paging, search text not logged) — returns `PagedResult<AdminUserDto>`. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Application/Features/Users/Queries/GetUsersForAdmin/*, DTOs/AdminUserDto.cs, Interfaces/IUserRepository.cs, WordBuddy/src/Services/Identity/WordBuddy.Identity.Infrastructure/Repositories/UserRepository.cs
- [x] `GET /api/auth/admin/users` in `AdminUsersController` (`AdminOnly`) — bad paging → 400. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Api/Controllers/AdminUsersController.cs
- [x] Move handover + learner-links lookup to `AdminSupportLinkHandoverController` (`AdminOnly`, same routes); set `AdminSupportLinksController` to `ManageSupportLinks` — routes unchanged, handover still admin-only. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Api/Controllers/AdminSupportLinkHandoverController.cs (new), AdminSupportLinksController.cs
- [x] `GetUnlinkRequestsForAdminQuery.IncludeChildLearners` + repository filter on learner `AgeGroup` — a moderator never receives a child learner's request. — GetUnlinkRequestsForAdminQuery*.cs, ISupportLinkRepository.cs, SupportLinkRepository.cs
- [x] `AdminCompleteUnlinkCommand` / `AdminRejectUnlinkCommand`: add `ActorIsAdmin`; non-admin on a child learner's request → existing not-found error — no state change, no audit row. — AdminCompleteUnlink*/AdminRejectUnlink* command + handler, ISupportLinkRepository.IsChildLearnerLinkAsync
- [x] `DataSeeder`: add `moderator@wordbuddy.com` (Adult, moderator) — created only when missing, Development only. — WordBuddy/src/Services/Identity/WordBuddy.Identity.Infrastructure/Seeding/DataSeeder.cs (built; not run, needs migration)
- [x] Identity `README.md` endpoint table: new routes and policy changes — matches code. — WordBuddy/src/Services/Identity/README.md

### Content

- [x] Add `ManageContent` policy (admin OR moderator and not Child) in `ServiceCollectionExtensions` with a named constant — registered once. — WordBuddy/src/Services/Content/WordBuddy.Content.Api/Extensions/ServiceCollectionExtensions.cs
- [x] Swap `AdminOnly` → `ManageContent` on `LessonsAdminController`, `LexemesAdminController`, moderation pending/moderate, autofill-pending/approve-for-children — `VocabularyAdminController` stays `AdminOnly`. — WordBuddy/src/Services/Content/WordBuddy.Content.Api/Controllers/{Lessons,Lexemes}AdminController.cs, PersonalVocabularyController.cs, VocabularyAutofillController.cs
- [x] Content `README.md` endpoint table: policy column updated — matches code. — WordBuddy/src/Services/Content/README.md

## Frontend

- [x] `types/index.ts`: `User.isModerator?`, `AdminUser` interface — no `any`. — WordBuddy.UI/src/types/index.ts
- [x] `utils/roles.ts`: `isAdmin`, `canManageContent` — a child with a stray moderator flag returns false. — WordBuddy.UI/src/utils/roles.ts
- [x] `AppLayout`: `CONTENT_NAV_ITEMS` on `canManageContent`, `ADMIN_ONLY_NAV_ITEMS` ("Users & roles") on `isAdmin` — moderator sees 4 items, admin sees 5, learner sees none. — WordBuddy.UI/src/layouts/AppLayout.tsx
- [x] Swap gates to `canManageContent` in `VocabularyModerationPage`, `AdminLessonsPage`, `AdminLessonEditorPage`, `AdminLexemesPage`, `AdminLexemeEditorPage` — moderator can open each page; learner is redirected. — WordBuddy.UI/src/pages/{VocabularyModeration,AdminLessons,AdminLessonEditor,AdminLexemes,AdminLexemeEditor}Page.tsx
- [x] `AdminSupportLinksPage`: gate on `canManageContent`; handover section admin-only; moderator note — moderator sees unlink requests only. — WordBuddy.UI/src/pages/AdminSupportLinksPage.tsx
- [x] `api/adminUsers.ts` + `hooks/useAdminUsers.ts` (query key `['admin-users', search, page, pageSize]`, mutation invalidates `['admin-users']`) — no raw `useEffect` fetch. — WordBuddy.UI/src/api/adminUsers.ts, hooks/useAdminUsers.ts
- [x] `AdminUsersPage` at `/admin/users` (search, `Pager`, Grant/Revoke Moderator, no button on admin rows, next-sign-in note, loading/error/empty states, Framer Motion entrance) — non-admin is redirected. — WordBuddy.UI/src/pages/AdminUsersPage.tsx
- [x] Register `/admin/users` route in `App.tsx` — reachable from the sidebar. — WordBuddy.UI/src/App.tsx

## Tests

### Identity unit

- [x] `User_SetModerator_*`: adult grant ok, child grant fails, admin grant fails, revoke ok — 4 facts pass. — WordBuddy/src/Services/Identity/WordBuddy.Identity.UnitTests/Domain/UserModeratorTests.cs
- [x] `User_EnsureCanBeDeleted_*`: admin fails with `User.AdminCannotBeDeleted`, non-admin ok — pass. — WordBuddy/src/Services/Identity/WordBuddy.Identity.UnitTests/Domain/UserModeratorTests.cs
- [x] `SetModeratorRoleCommandHandler_*`: validation fail, not found, domain fail (no save), success (save once) — pass. — WordBuddy/src/Services/Identity/WordBuddy.Identity.UnitTests/Features/ModeratorHandlerTests.cs
- [x] `GetUsersForAdminQueryHandler_*` + validator: paging bounds, search length, adults-only call to repository — pass. — WordBuddy/src/Services/Identity/WordBuddy.Identity.UnitTests/Features/ModeratorHandlerTests.cs
- [ ] `JwtTokenGenerator_GenerateToken_*`: moderator claim present for moderator, absent otherwise — pass. — NOT DONE as a unit test: the generator is internal to Infrastructure; claim covered by `ModeratorEndpointsTests.Login_SeededModerator_*` (blocked by migration)
- [x] `AdminCompleteUnlinkCommandHandler` / `AdminRejectUnlinkCommandHandler`: moderator on child learner → not found, no save; moderator on adult learner → success — pass. — WordBuddy/src/Services/Identity/WordBuddy.Identity.UnitTests/Features/SupportLinkHandlerTests.cs, AdminSupportLinkQueryHandlerTests.cs

### Identity integration

- [ ] `PUT admin/users/{id}/moderator`: admin → 204 and next login token has `moderator`; moderator → 403; learner → 403; child target → 400; admin target → 400 — pass. — written in ModeratorEndpointsTests.cs, NOT RUN: blocked until the migration exists (PendingModelChangesWarning)
- [ ] `GET admin/users`: admin → 200 adults only, search works; moderator → 403; bad paging → 400 — pass. — written in ModeratorEndpointsTests.cs, NOT RUN: blocked until the migration exists (PendingModelChangesWarning)
- [ ] Unlink admin: moderator lists only adult-learner requests; complete on child request → 404; handover and `support-links?learnerId=` → 403 for moderator; admin unchanged — pass. — written in ModeratorEndpointsTests.cs, NOT RUN: blocked until the migration exists (PendingModelChangesWarning)
- [ ] Migration test: `IsModerator` defaults to false for existing rows — pass. — written in ModeratorEndpointsTests.cs, NOT RUN: blocked until the migration exists (PendingModelChangesWarning)

### Content integration

- [x] `TestJwtTokenFactory`: add optional `isModerator` (emits claim only when true) — existing tests unchanged. — WordBuddy/src/Services/Content/WordBuddy.Content.IntegrationTests/TestJwtTokenFactory.cs
- [x] `ManageContent` endpoints (lessons admin, lexemes admin, moderation pending/moderate, autofill pending/approve): adult moderator → 2xx; child token with moderator claim → 403; learner → 403; admin → 2xx — one `[Theory]` per controller. — WordBuddy/src/Services/Content/WordBuddy.Content.IntegrationTests/ManageContentPolicyTests.cs
- [x] `learner-words/republish`: moderator → 403 — pass. — WordBuddy/src/Services/Content/WordBuddy.Content.IntegrationTests/ManageContentPolicyTests.cs

### E2E (tester, `/test`)

- [ ] API e2e: grant → re-login → moderator can moderate a word and edit a lexeme; cannot call `admin/users` — pass.
- [ ] UI e2e: admin grants Moderator on `/admin/users`; moderator sees content nav, not "Users & roles"; learner sees no admin nav — pass.
