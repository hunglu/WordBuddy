# Tasks: WB-27_Learning groups

## Backend — Shared.Contracts

- [x] Add `Groups/LearnerGroupMemberActivated`, `LearnerGroupMemberRemoved`, `LearnerGroupDeleted` records (ids only) — pack new minor version to `local-nuget-feed` — Shared.Contracts `Groups/*.cs`, version 1.3.0 (packed to `local-nuget-feed`)
- [x] Bump `WordBuddy.Shared.Contracts` in Identity, Content, Progress — all three slnx build — 3 Infrastructure csproj files; Identity, Content, Progress slnx build

## Backend — Identity

- [x] `Domain/Groups/`: `LearnerGroup`, `LearnerGroupMember`, `GroupMemberStatus`, `GroupMemberRemovedReason`, `LearnerGroupErrors` — entities enforce name length and status transitions — Identity.Domain `Groups/` (5 files)
- [x] `LearnerGroupPolicy` + `LearnerGroupOptions` — active link required; child → pending unless owner is Primary; limits enforced — `LearnerGroupPolicy.cs`, `Settings/LearnerGroupOptions.cs`
- [x] EF configurations + `AddLearnerGroups` migration — filtered unique index `(GroupId, LearnerId)` where not Removed — 2 configurations, `IdentityDbContext`, migration `20261009153236_AddLearnerGroups`, `docs/database-diagram/identity.md`
- [x] `ILearnerGroupRepository` + implementation — queries by owner, by learner, by link pair — `Interfaces/ILearnerGroupRepository.cs`, `Repositories/LearnerGroupRepository.cs`
- [x] Outbox publisher for the 3 group events — events written in the same transaction as the change — `ILearnerGroupEventPublisher`, `OutboxLearnerGroupEventPublisher`
- [x] Commands `CreateGroup`, `RenameGroup`, `DeleteGroup` (+ validators) — owner check, cache key deleted — `Features/Groups/Commands/{CreateGroup,RenameGroup,DeleteGroup}`
- [x] Commands `AddGroupMembers`, `RemoveGroupMember`, `LeaveGroup` (+ validators) — per-learner result; child cannot leave — `Features/Groups/Commands/{AddGroupMembers,RemoveGroupMember,LeaveGroup}`
- [x] Command `RespondToGroupMembership` + query `GetPendingGroupApprovals` — only the child's Primary can act — `RespondToGroupMembership`, `GetPendingGroupApprovals`
- [x] Queries `GetMyGroups`, `GetGroup` — members as `PublicName` + `AvatarId`; learners never see other members — `GetMyGroups`, `GetGroup` (cached `identity:group:{id}`)
- [x] `ILearnerGroupMembershipCleaner`, called from every link-revoke/reject path — revoked link removes member from that supporter's groups and publishes `LearnerGroupMemberRemoved` — `LearnerGroupMembershipCleaner`; called from `RespondUnlink` and `AdminCompleteUnlink` (the only 2 `Revoke(` call sites)
- [x] `GroupsController` (`api/auth/groups`) + rate-limit policy — endpoints map via `ToProblemResult`; logs ids only — `GroupsController`, `GroupRequests`, `RateLimitingConfiguration` (`group-write`)

## Backend — Content

- [x] `LearnerGroupMemberProjection` + consumers + `ApplyLearnerGroupEvent` command — idempotent, ignores older events — Content `LearnerGroupMemberProjection`, `Features/Groups/Commands/ApplyLearnerGroupEvent`, `LearnerGroupEventConsumers`, `LearnerGroupQueues`, repository, config
- [x] `LearnerWord.CreateBySupporter` — sets `AddedBy = Supporter` — `LearnerWord` (also new `AddedByUserId`, so `LearnerWordAdded.AddedBy` is the supporter), `LearnerWordEventCollector`
- [x] `GroupWordAssignment` entity + `AddLearnerGroups` migration — tables created — `GroupWordAssignment`, config, `AddedByUserId` column, migration `20261010142457_AddLearnerGroups`, `docs/database-diagram/content.md`
- [x] Command `AssignWordsToGroup` (+ validator, max 50) — adds missing links per active member, child approval set, blocked senses skipped, `LearnerWordAdded` per new link, counts returned — `Features/Groups/Commands/AssignWordsToGroup`, `IGroupWordRepository`, `GroupWordRepository`, `ISupportLinkProjectionRepository.GetLearnersWithActiveLinkAsync`
- [x] Query `GetGroupWordAssignments` — owner only — `Features/Groups/Queries/GetGroupWordAssignments`
- [x] `GroupVocabularyController` (`api/vocabulary/groups/{groupId}`) — `POST words`, `GET words`, 403 for non-owner — `GroupVocabularyController`, `AssignWordsToGroupRequest`, Content `ResultExtensions` (`.Forbidden` → 403)

## Backend — Progress

- [x] `LearnerGroupMemberProjection` + consumers + `ApplyLearnerGroupEvent` + `AddLearnerGroupProjection` migration — idempotent — Progress projection, command, consumers, queues, migration `20261010142511_AddLearnerGroupProjection`, `docs/database-diagram/progress.md`
- [x] `IDashboardReadRepository` batched `...ForUsersAsync` reads — one query per metric for all members — `IDashboardReadRepository`, `DashboardReadRepository` (4 `...ForUsersAsync` methods)
- [x] Query `GetGroupDashboard` (+ validator `Days` 7/30/90) — per-member rows via `DashboardCalculator`, filtered by active support link, cached 5 min — `Features/Groups/Queries/GetGroupDashboard`, `DashboardCalculator.CalculateGroup`, `GroupDashboardDtos`, `ISupportLinkProjectionRepository.GetLearnersWithActiveLinkAsync`
- [x] `CanManageGroup` policy + `GET api/progress/dashboard/groups/{groupId}` — 403 for non-owner — `Authorization/GroupAuthorization.cs`, `DashboardController.GetGroup`, Progress `ResultExtensions` (`.Forbidden` → 403)

## Frontend

- [x] Types in `src/types/index.ts` — union types, no `enum`, no `any` — `src/types/index.ts`
- [x] `src/api/groups.ts`; extend `vocabulary.ts`, `progress.ts` — all group calls available — `src/api/groups.ts`, `vocabulary.ts`, `progress.ts`
- [x] TanStack Query hooks for groups, approvals, group words, group dashboard — mutations invalidate matching keys — `src/hooks/useGroups.ts`
- [x] `GroupsPage` (`/groups`) + sidebar link — owner list/create; learner read-only list — `pages/GroupsPage.tsx`, `App.tsx` route, `AppLayout.tsx` nav
- [x] `GroupDetailPage` Members tab — add from active supported learners, pending badge, remove — `pages/GroupDetailPage.tsx`, `components/groups/GroupMembersTab.tsx`, `groupUi.ts`
- [x] `GroupDetailPage` Words tab — pick senses, assign, show counts and history — `components/groups/GroupWordsTab.tsx` (words come from the shared pool list, filtered by text)
- [x] `GroupDetailPage` Dashboard tab — member table with avatar + public name, range selector, link to learner dashboard — `components/groups/GroupDashboardTab.tsx`
- [x] "Group requests" section on `SupportLinksPage` — Primary approves/rejects — `components/groups/GroupRequests.tsx`, `pages/SupportLinksPage.tsx`
- [x] Error states — every query shows inline `isError`/403 message, no crash — every group query/mutation shows an inline message; 403 shows "No access" text

## Tests

- [x] Identity unit: `LearnerGroupPolicy` — no link rejected; child pending; child with Primary owner active; adult active; limits — `UnitTests/Domain/LearnerGroupPolicyTests.cs`
- [x] Identity unit: group entity transitions — approve/reject/remove/leave valid and invalid states — `UnitTests/Domain/LearnerGroupEntityTests.cs`
- [x] Identity unit: each group command/query handler — owner vs non-owner; child cannot leave; learner view hides members — `UnitTests/Features/GroupHandlerTests.cs`, `GroupHandlerFixture.cs`
- [x] Identity unit: membership cleaner — revoke removes child and adult members, publishes event, other owners' groups untouched — `UnitTests/Features/LearnerGroupMembershipCleanerTests.cs`
- [x] Identity integration: create group → add child (pending) → Primary approves → active; revoke link → member removed — `IntegrationTests/GroupEndpointsTests.cs` (ran green on localdb)
- [x] Content unit: projection apply — idempotent, older event ignored — `UnitTests/Features/Groups/ApplyLearnerGroupEventCommandHandlerTests.cs`
- [x] Content unit: `AssignWordsToGroup` — adult member added; child member approved by assigner; blocked sense skipped; existing link counted; non-owner 403 — `UnitTests/Features/Groups/AssignWordsToGroupCommandHandlerTests.cs` (adult-only words are skipped for every member, see plan deviation)
- [x] Content integration: `POST words` adds links for all active members once — `IntegrationTests/GroupVocabularyEndpointsTests.cs` (ran green on localdb)
- [x] Progress unit: projection apply + `GetGroupDashboard` — removed member and inactive link excluded; child and adult rows — `UnitTests/Features/Groups/ApplyLearnerGroupEventCommandHandlerTests.cs`, `GetGroupDashboardQueryHandlerTests.cs`
- [x] Progress integration: group dashboard endpoint — owner 200, other supporter 403 — `IntegrationTests/GroupDashboardEndpointsTests.cs` (ran green on localdb)
- [x] E2E API (`e2e/api`): teacher creates group, adds adult + child, assigns list once, both learners have the words; revoke removes learner — moved to `/test` (Sam, 2026-10-10)
- [x] E2E UI (`e2e/ui`): create group, add members, assign words, view group dashboard; child shown by alias + avatar only — moved to `/test` (Sam, 2026-10-10)

## Fix round 1

- [x] Finding 1 (major): `DeletedLearnerGroup` tombstone (group id + deleted-at) in Content and Progress; `LearnerGroupDeleted` writes it; member events at or before the delete are ignored — Content/Progress `Domain/DeletedLearnerGroup.cs`, `DeletedLearnerGroupConfiguration`, DbContexts, `ILearnerGroupProjectionRepository` + implementation, `ApplyLearnerGroupEventCommandHandler`
- [x] Regenerate the branch's own migrations with the new table (not applied anywhere) — Content `20261010142457_AddLearnerGroups`, Progress `20261010142511_AddLearnerGroupProjection` + snapshots; `docs/database-diagram/{content,progress,README}.md`
- [x] Unit tests "delete arrives before add" (no active row, replay, event after delete, delete after add) — Content + Progress `ApplyLearnerGroupEventCommandHandlerTests.cs`
- [x] Finding 2 (nit): dashboard cache — left as is. Key has `days` x client UTC offset; `IDistributedCache` has no prefix delete, so invalidation needs ~300 key removals or a version key. TTL is 5 min, owner check runs before the cache (no leak). Already listed as a scope suggestion (dashboard cache invalidation)
- [x] Finding 3 (nit): group detail cache alias/avatar — left as is. A profile change would have to find every group of the user and clear each key; expiry is short and absolute. Member add/remove already clears the key
