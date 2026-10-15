# Tasks: Apply paging to table/list

Reference pattern: `GetAdminLexemesQuery` + `LexemeAdminRepository.SearchAsync` + `LexemesAdminController.GetLexemes`.
Every paged query: `Page`/`PageSize` on the record, `page ≥ 1` + `1 ≤ pageSize ≤ Paging.MaxPageSize` in the validator,
`[FromQuery] int page = 1, int pageSize = Paging.DefaultPageSize` on the action, `Id` as the last order key.

## Backend

### Shared

- [x] Kernel: add `PagedResult<T>` and `Paging` (`DefaultPageSize = 25`, `MaxPageSize = 100`, `Slice<T>`), XML docs, version `1.1.0` — `dotnet pack` to `local-nuget-feed/` succeeds — Shared.Kernel/PagedResult.cs, Paging.cs, csproj (1.1.0, packed to local-nuget-feed)
- [x] Bump `WordBuddy.Shared.Kernel` to `1.1.0` in every project of Content, Identity, Progress — each `.slnx` builds with no NU1605 — csproj of Content, Identity, Progress (11 files)

### Content

- [x] Add `Infrastructure/Persistence/QueryablePagingExtensions.ToPagedResultAsync` — Count + Skip/Take on an ordered query — Content.Infrastructure/Persistence/QueryablePagingExtensions.cs
- [x] Lexemes: replace `AdminLexemePageDto` with `PagedResult<AdminLexemeListItemDto>`; use `Paging.*` constants — JSON response unchanged — AdminLexemeDtos.cs, GetAdminLexemes*, ILexemeAdminRepository, LexemeAdminRepository, LexemesAdminController
- [x] `GET /api/lessons/admin`: page `GetAdminLessonsQuery` (record, validator, repository, controller) — returns `PagedResult<AdminLessonDto>` — GetAdminLessons* (+Validator), ILessonRepository, LessonRepository, LessonsAdminController
- [x] `GET /api/vocabulary/moderation/pending`: page `GetPendingVocabularyModerationQuery` — returns `PagedResult<PersonalVocabularyWordDto>` — GetPendingVocabularyModeration* (+Validator), IVocabularyWordRepository, VocabularyWordRepository, PersonalVocabularyController
- [x] `GET /api/vocabulary/moderation/autofill-pending` + `GET /api/vocabulary/learners/{id}/pending-approvals`: page `GetPendingChildApprovalsQuery` — both routes return `PagedResult<ChildApprovalDto>` — GetPendingChildApprovals* (+Validator), IAutofillRepository, AutofillRepository, VocabularyAutofillController
- [x] `GET /api/vocabulary/mine`: page `GetMyVocabularyWordsQuery` — child filters unchanged, returns a page — GetMyVocabularyWords* (+Validator), IVocabularyWordRepository, VocabularyWordRepository, PersonalVocabularyController
- [x] `GET /api/vocabulary/shared`: add `Search` + paging to `GetSharedVocabularyWordsQuery`; cache read → search filter → `Paging.Slice` → `IsMine` — cache keys/invalidation unchanged; child `totalCount` counts child-visible words only — GetSharedVocabularyWords* (+Validator), Sense.WordMaxLength, PersonalVocabularyController
- [x] `GET /api/vocabulary/groups/{groupId}/words`: page `GetGroupWordAssignmentsQuery` — returns `PagedResult<GroupWordAssignmentDto>` — GetGroupWordAssignments* (+Validator), IGroupWordRepository, GroupWordRepository, GroupVocabularyController
- [x] Update `WordBuddy/src/Services/Content/README.md` endpoint table — paging params listed — Content/README.md

### Identity

- [x] Add `Infrastructure/Persistence/QueryablePagingExtensions.ToPagedResultAsync` — same contract as Content — Identity.Infrastructure/Persistence/QueryablePagingExtensions.cs
- [x] `GET /api/auth/admin/unlink-requests`: page `GetUnlinkRequestsForAdminQuery` — returns `PagedResult<AdminUnlinkRequestDto>` — GetUnlinkRequestsForAdmin* (+Validator), ISupportLinkRepository, SupportLinkRepository, AdminSupportLinksController
- [x] `GET /api/auth/admin/support-links`: page `GetLearnerSupportLinksForAdminQuery` — returns `PagedResult<AdminSupportLinkDto>` — GetLearnerSupportLinksForAdmin* (+Validator), ISupportLinkRepository, SupportLinkRepository, AdminSupportLinksController
- [x] Replace `GetMyGroupsQuery`/`MyGroupsDto`/`GET /api/auth/groups` with `GetOwnedGroupsQuery` (`GET /groups/owned`) and `GetJoinedGroupsQuery` (`GET /groups/joined`) — each paged; child gets an empty owned page — GetOwnedGroups*, GetJoinedGroups* (new), GetMyGroups removed, MyGroupsDto removed, ILearnerGroupRepository, LearnerGroupRepository, GroupsController
- [x] `GET /api/auth/groups/approvals`: page `GetPendingGroupApprovalsQuery` — returns a page — GetPendingGroupApprovals* (+Validator), ILearnerGroupRepository, LearnerGroupRepository, GroupsController
- [x] Add internal `LearnerGroupSnapshot` + `LearnerGroupSnapshotLoader`; `LearnerGroupCache.Key` → `identity:group-snapshot:{id}` — all group commands still invalidate it — Features/Groups/LearnerGroupSnapshot.cs, LearnerGroupSnapshotLoader.cs (public, not internal: injected into public handlers), LearnerGroupCache.cs
- [x] `LearnerGroupDetailDto`: drop `Members`, add `ActiveMemberCount`, `PendingMemberCount`, `MemberLearnerIds`; `GetGroupQueryHandler` uses the loader — detail no longer lists members — LearnerGroupDtos.cs, GetGroupQuery*, GetGroupQueryHandler
- [x] New `GetGroupMembersQuery` + validator + handler + `GET /api/auth/groups/{id}/members` — owner only (same check as `GetGroupQuery`), `Slice` of snapshot members — GetGroupMembers* (+Validator), GroupsController, ServiceCollectionExtensions
- [x] Update `WordBuddy/src/Services/Identity/README.md` endpoint table — new/removed routes listed — Identity/README.md

### Progress

- [x] Add `Infrastructure/Persistence/QueryablePagingExtensions.ToPagedResultAsync` — same contract as Content — Progress.Infrastructure/Persistence/QueryablePagingExtensions.cs
- [x] `GET /api/progress/challenges`: page `ListChallengesQuery` — child without consent gets an empty page — ListChallenges*, IChallengeRepository, ChallengeRepository, ChallengesController
- [x] `GET /api/progress/challenges/{id}/board`: `ChallengeBoardDto.Entries` → `PagedResult<ChallengeBoardEntryDto>`; adult `Slice`s snapshot rows; child ignores paging (top 5 + own row, page 1) — child never sees rows beyond today's rule — GetChallengeBoard*, ChallengeDtos.cs, ChallengesController
- [x] Update `WordBuddy/src/Services/Progress/README.md` endpoint table — paging params listed — Progress/README.md

## Frontend

- [x] `types/index.ts`: add `PagedResult<T>`, `PageParams`; `AdminLexemePage` = `PagedResult<AdminLexemeListItem>`; `LearnerGroupDetail` drops `members`, adds counts + `memberLearnerIds`; `ChallengeBoard.entries` paged — no `any`, strict build passes — types/index.ts
- [x] `api/paging.ts`: `DEFAULT_PAGE_SIZE = 25`, `CARD_PAGE_SIZE = 24` — imported by all paged API calls — api/paging.ts (+ MAX_PAGE_SIZE)
- [x] `hooks/usePagination.ts`: page state, `reset()`, clamp to last page when the current page empties — used by every paged screen — hooks/usePagination.ts (clamp is called during render)
- [x] `components/Pager.tsx`: Previous / Next, "Page X of Y · N items", `aria-label` prop, theme tokens only, hidden when `totalCount = 0` — matches today's lexeme pager look — components/Pager.tsx
- [x] API functions take `PageParams` and return `PagedResult<T>`: `lessons.ts`, `lexemes.ts`, `supportLinks.ts`, `vocabulary.ts`, `groups.ts` (+ `getOwnedGroups`, `getJoinedGroups`, `getGroupMembers`), `challenges.ts` — `getMyGroups` removed — api/lessons.ts, lexemes.ts, supportLinks.ts, vocabulary.ts, groups.ts, challenges.ts
- [x] Paged hooks: params at the end of each query key, `placeholderData: keepPreviousData` — existing prefix invalidations still refresh lists — hooks/useAdminLessons, useSupportLinks, useVocabulary, useAutofill, useGroups, useChallenges
- [x] `AdminLessonsPage`: pager; filter change resets to page 1 — pages/AdminLessonsPage.tsx
- [x] `AdminLexemesPage`: replace inline `<nav>` with `<Pager>` — behaviour unchanged — pages/AdminLexemesPage.tsx
- [x] `AdminSupportLinksPage`: pagers on unlink requests and learner links — pages/AdminSupportLinksPage.tsx
- [x] `VocabularyModerationPage` + `AutofillApprovalsPanel`: pagers on both tabs — pages/VocabularyModerationPage.tsx, components/autofill/AutofillApprovalsPanel.tsx
- [x] `WordsToApprove` (supporter view): pager — components/support/WordsToApprove.tsx
- [x] `VocabularyBuilderPage`: my words paged; delete/edit stays on the current page (clamps when empty) — pages/VocabularyBuilderPage.tsx (uses CARD_PAGE_SIZE: it is a card grid)
- [x] `VocabularySharedPoolPage`: card grid paged with `CARD_PAGE_SIZE` — pages/VocabularySharedPoolPage.tsx
- [x] `GroupsPage`: owned and joined sections each with own query + pager — pages/GroupsPage.tsx
- [x] `GroupRequests`: pager on group approvals — components/groups/GroupRequests.tsx
- [x] `GroupMembersTab`: members from `getGroupMembers` + pager; candidates filtered by `group.memberLearnerIds` — components/groups/GroupMembersTab.tsx
- [x] `GroupDashboardTab` + `GroupWordsTab`: use `group.activeMemberCount` — no use of `group.members` left — components/groups/GroupDashboardTab.tsx (names read with one getGroupMembers call at page size 100), GroupWordsTab.tsx
- [x] `GroupWordsTab`: pool picker uses server `search` (debounced, page 1 on change) + pager, selection kept across pages; history pager — components/groups/GroupWordsTab.tsx
- [x] `ChallengesPage` + `ChallengeBoardPage`: pagers; child board shows one page — pages/ChallengesPage.tsx, pages/ChallengeBoardPage.tsx
- [x] Every paged screen keeps its `isError` message — no blank screen on 502 — all screens above keep their isError message
- [x] `npm run lint` and `npm run build` pass in `WordBuddy.UI` — lint: 0 errors (3 old warnings); build passes

## Tests

### Shared / Content

- [x] Unit: `Paging.Slice` — first, middle, last, past-the-end page, empty source (in `Content.UnitTests`) — Content.UnitTests/Features/KernelPaging/PagingSliceTests.cs
- [x] Update `GetAdminLexemesQueryHandlerTests` + `LexemesAdminEndpointsTests` to `PagedResult` — JSON keys `items/page/pageSize/totalCount` asserted — LexemesAdminEndpointsTests, GetAdminLexemesQueryHandlerTests
- [x] Unit + integration: admin lessons — page 2 content, filters + paging combined, `pageSize=101` → 400 — LessonQueryHandlerTests, PagingEndpointsTests
- [x] Unit + integration: moderation pending — paged, admin only (non-admin 403) — GetPendingVocabularyModerationQueryHandlerTests, PagingEndpointsTests, existing non-admin 403 test
- [x] Unit + integration: autofill pending — both routes paged — AutofillCommandHandlerTests, PagingEndpointsTests
- [x] Unit + integration: my words — paged, `page=0` → 400, child and adult paths — GetMyVocabularyWordsQueryHandlerTests, PagingEndpointsTests
- [x] Unit: shared pool — cache hit sliced, search filter, child `totalCount` excludes non-child words, `IsMine` set on page items — GetSharedVocabularyWordsQueryHandlerTests
- [x] Integration: shared pool — child vs adult `totalCount`, `search` param — PagingEndpointsTests
- [x] Unit + integration: group word history — paged, order stable across pages — GetGroupWordAssignmentsQueryHandlerTests, PagingEndpointsTests

### Identity

- [x] Unit + integration: admin unlink requests and admin learner links — paged — AdminSupportLinkQueryHandlerTests, SupportLinkEndpointsTests
- [x] Unit + integration: owned groups and joined groups — paged; child gets empty owned page; old `GET /api/auth/groups` gone (404/405) — GroupHandlerTests, GroupEndpointsTests (old route returns 404/405)
- [x] Unit + integration: group approvals — paged — GroupHandlerTests, GroupEndpointsTests
- [x] Unit: `LearnerGroupSnapshotLoader` — cache hit, cache miss sets key `identity:group-snapshot:{id}` — GroupHandlerTests (LearnerGroupSnapshotLoader)
- [x] Unit + integration: group detail — counts + `memberLearnerIds`, no `members` — GroupHandlerTests, GroupEndpointsTests
- [x] Unit + integration: group members — owner gets a page; non-owner 403/404 as `GetGroup`; a command invalidates the snapshot — GroupHandlerTests, GroupEndpointsTests (add member refreshes the snapshot)

### Progress

- [x] Unit + integration: challenges list — paged; child without consent → empty page — ListChallengesQueryHandlerTests, ChallengeEndpointsTests
- [x] Unit: board — adult page 2 is the right slice; child with `page=2` still gets top 5 + own row, no other scores — GetChallengeBoardQueryHandlerTests
- [x] Integration: board — adult and child responses use `entries.items` — ChallengeEndpointsTests
