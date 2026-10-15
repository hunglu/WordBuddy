# Plan: Apply paging to table/list

## Summary

Add one paging shape, `PagedResult<T>` (`items, page, pageSize, totalCount`), to the
`WordBuddy.Shared.Kernel` package (1.0.1 → 1.1.0). Change 16 list endpoints in Content, Identity
and Progress to accept `page`/`pageSize` and return it. Add one shared `<Pager>` component and one
`usePagination` hook to the UI and use them on every in-scope list.

```text
UI list ─ usePagination ─▶ GET …?page=N&pageSize=M ─▶ handler ─▶ PagedResult<T> ─▶ <Pager>
                                                     │
                              DB: Count + Skip/Take ◀┤ (default)
                     cached snapshot: Paging.Slice ◀┘ (shared pool, board, group members)
```

## Affected services / areas

| Area | Change |
| --- | --- |
| `WordBuddy.Shared.Kernel` | New `PagedResult<T>` + `Paging` helper. Version 1.1.0 |
| Content | 7 list endpoints paged. Lexemes aligned to the shared type |
| Identity | 6 list endpoints paged (1 new, 1 split in two). Group detail returns counts, not members |
| Progress | Challenge list + challenge board paged |
| WordBuddy.UI | `Pager`, `usePagination`, `PagedResult<T>` type, 14 pages/components |
| Quiz, Notification | None |

## Decision: where the paging shape lives

**`WordBuddy.Shared.Kernel` 1.1.0 (additive minor bump).**

| Option | Verdict |
| --- | --- |
| Kernel package | **Chosen.** Pure data type, no dependencies, same home as `Result<T>`. One JSON shape by construction. Still a versioned package, so the independence model holds (ADR 0001) |
| Per-service copy | Rejected. Three copies of the same record drift apart. That is the exact problem this proposal fixes |
| `WordBuddy.Shared.Application` | Rejected. Infrastructure and Api also need the type, and they do not reference this package |

Not shared:

- EF Core `ToPagedResultAsync` — needs EF Core, so the Kernel cannot hold it. Each service gets
  one small copy in its Infrastructure project.
- FluentValidation rules — the Kernel has no FluentValidation. Each validator writes two
  `RuleFor` lines using the `Paging` constants.

```csharp
// WordBuddy.Shared.Kernel — namespace WordBuddy.Shared.Kernel
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class Paging
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public static PagedResult<T> Slice<T>(IReadOnlyList<T> source, int page, int pageSize);
}
```

JSON (`camelCase`): `{ "items": [...], "page": 1, "pageSize": 25, "totalCount": 312 }`. This is
the same as today's lexeme response, so the lexeme UI keeps working.

## Paging rules (all endpoints)

| Rule | Value |
| --- | --- |
| Query params | `page` (default 1), `pageSize` (default `Paging.DefaultPageSize` = 25) |
| Validation | `page ≥ 1`, `1 ≤ pageSize ≤ 100`. Other values return 400 `<Query>.Validation` |
| Page past the end | 200 with empty `items` and the real `totalCount`. Not 404 |
| Ordering | Keep each list's current order. Add `Id` as the last tie-breaker so pages are stable |
| `totalCount` | Counted **after** auth and child filters. It never reveals hidden rows |

## In-scope endpoints

| # | Route | Svc | Today | After | Data path |
| --- | --- | --- | --- | --- | --- |
| 1 | `GET /api/lessons/admin` | Content | `AdminLessonDto[]` | paged | DB |
| 2 | `GET /api/vocabulary/admin/lexemes` | Content | `AdminLexemePageDto` | `PagedResult<AdminLexemeListItemDto>` | DB (unchanged) |
| 3 | `GET /api/vocabulary/moderation/pending` | Content | array | paged | DB |
| 4 | `GET /api/vocabulary/moderation/autofill-pending` | Content | array | paged | DB |
| 5 | `GET /api/vocabulary/learners/{id}/pending-approvals` | Content | array | paged (same handler as 4) | DB |
| 6 | `GET /api/vocabulary/mine` | Content | array | paged | DB |
| 7 | `GET /api/vocabulary/shared` | Content | array | paged + new `search` | cached snapshot → filter → `Slice` |
| 8 | `GET /api/vocabulary/groups/{groupId}/words` | Content | array | paged | DB |
| 9 | `GET /api/auth/admin/unlink-requests` | Identity | array | paged | DB |
| 10 | `GET /api/auth/admin/support-links?learnerId=` | Identity | array | paged | DB |
| 11 | `GET /api/auth/groups/owned` | Identity | part of `GET /groups` | **new**, paged | DB |
| 12 | `GET /api/auth/groups/joined` | Identity | part of `GET /groups` | **new**, paged | DB |
| 13 | `GET /api/auth/groups/approvals` | Identity | array | paged | DB |
| 14 | `GET /api/auth/groups/{id}/members` | Identity | inside group detail | **new**, paged | cached snapshot → `Slice` |
| 15 | `GET /api/progress/challenges` | Progress | array | paged | DB |
| 16 | `GET /api/progress/challenges/{id}/board` | Progress | `Entries` array | `Entries` is `PagedResult<ChallengeBoardEntryDto>` | cached snapshot → `Slice` |

Removed: `GET /api/auth/groups` (returned `MyGroupsDto`). Rows 11 and 12 replace it, so each
section has its own pager. The UI is its only caller. The Progress → Content HTTP call
(`api/vocabulary/senses`) is not touched.

## Child vs adult behaviour

| List | Adult | Child (`AgeGroup = Child`) |
| --- | --- | --- |
| Admin lists (1–5, 9, 10) | Admin only (`AdminOnly`). Row 5: supporter (unchanged) | No access (unchanged) |
| My words (6) | Own words, paged | Same. Existing child filters stay in the query |
| Shared pool (7) | All shared words | `VisibleToChildren` only. Child cache variant is used, then `search` and `Slice`. `totalCount` counts child-visible words only |
| Group words (8), members (14) | Owner, paged | Same policies as today |
| Owned groups (11) | Adult supporters | Empty page (same as today's empty `Owned`) |
| Joined groups (12), approvals (13) | Paged | Same |
| Challenges (15) | Adult audience, paged | Child without consent → empty page (same as today) |
| Board (16) | Full ranking, paged | **Paging ignored.** Always page 1 with top 5 + own row, `totalCount` = rows returned. A child can never page past the top 5 |

The pager looks and works the same for both age groups.

## Backend approach

All query changes follow `.claude/skills/develop-webapi/SKILL.md`. Each query record gets
`int Page = 1, int PageSize = Paging.DefaultPageSize`. Each validator gets the two paging rules.
Each handler returns `Result<PagedResult<T>>`. Each controller action adds
`[FromQuery] int page = 1, [FromQuery] int pageSize = Paging.DefaultPageSize`. Use
`GetAdminLexemesQuery` + `LexemeAdminRepository.SearchAsync` as the reference pattern.

### Shared

- Kernel: add `PagedResult<T>` and `Paging` (constants + `Slice`). XML docs. Version `1.1.0`.
- Pack to `local-nuget-feed/`.
- Bump `WordBuddy.Shared.Kernel` to `1.1.0` in **every** project of Content, Identity and
  Progress. Mixed versions in one solution cause NU1605 (package downgrade) on the Api project.
  Quiz stays on 1.0.1.

### Per service Infrastructure

`Persistence/QueryablePagingExtensions.ToPagedResultAsync<T>(this IQueryable<T>, page, pageSize, ct)`
→ `CountAsync` + `Skip/Take`. The caller orders the query first.

### Content

| Item | Change |
| --- | --- |
| Lexemes | Delete `AdminLexemePageDto`. Use `PagedResult<AdminLexemeListItemDto>`. Replace `GetAdminLexemesQuery.DefaultPageSize/MaxPageSize` with `Paging.*`. JSON unchanged |
| `GetAdminLessonsQuery` | + paging. Repository returns a page |
| `GetPendingVocabularyModerationQuery` | + paging |
| `GetPendingChildApprovalsQuery` | + paging. Serves rows 4 and 5 |
| `GetMyVocabularyWordsQuery` | + paging |
| `GetSharedVocabularyWordsQuery` | + paging + `Search` (max length = word max length). Cache keys and invalidation stay the same. After the cache read: filter by `search` (case-insensitive contains on `Word`) → `Paging.Slice` → set `IsMine` on the page only |
| `GetGroupWordAssignmentsQuery` | + paging |

### Identity

| Item | Change |
| --- | --- |
| `GetUnlinkRequestsForAdminQuery` | + paging |
| `GetLearnerSupportLinksForAdminQuery` | + paging |
| `GetMyGroupsQuery` + `MyGroupsDto` | Replace with `GetOwnedGroupsQuery` → `PagedResult<OwnedGroupDto>` and `GetJoinedGroupsQuery` → `PagedResult<JoinedGroupDto>` |
| `GetPendingGroupApprovalsQuery` | + paging |
| Group snapshot | New internal `LearnerGroupSnapshot` (group + all members, max 40). Cached under new key `identity:group-snapshot:{id}`, so old cached `LearnerGroupDetailDto` values are never read. Read by a small `LearnerGroupSnapshotLoader` used by both group handlers. `LearnerGroupCache.Key` points at the new key. Commands still delete it |
| `LearnerGroupDetailDto` | Drop `Members`. Add `ActiveMemberCount`, `PendingMemberCount`, `MemberLearnerIds` (ids only, max 40, for the add-member picker) |
| `GetGroupMembersQuery` (new) | `GET /api/auth/groups/{id}/members`. Same owner check as `GetGroupQuery`. Snapshot → `Slice` |

### Progress

| Item | Change |
| --- | --- |
| `ListChallengesQuery` | + paging. DB paged |
| `GetChallengeBoardQuery` | + paging. Adult: `Slice` the cached snapshot rows. Child: current top-5 + own row logic, paging args ignored. `MyRank`/`MyScore`/`ParticipantCount` unchanged |

Rate limiting, auth policies, messaging: no change. Migrations: none.

## Frontend approach

### New shared pieces

| File | Purpose |
| --- | --- |
| `src/types/index.ts` | `PagedResult<T>` and `PageParams { page: number; pageSize: number }`. `AdminLexemePage` becomes `PagedResult<AdminLexemeListItem>` |
| `src/api/paging.ts` | `DEFAULT_PAGE_SIZE = 25`, `CARD_PAGE_SIZE = 24` (card grids: divisible by 2, 3, 4) |
| `src/hooks/usePagination.ts` | Page state. `reset()` on filter change. Clamps to the last page when a mutation empties the current page |
| `src/components/Pager.tsx` | Previous / Next, "Page X of Y · N items", `aria-label` from props. Tailwind + theme tokens, same look as today's lexeme pager. Renders nothing when `totalCount = 0` |

Every paged `useQuery` uses `placeholderData: keepPreviousData`, so the list does not flash
between pages. Query keys add the params at the end (`['vocabulary', 'mine', params]`). Existing
prefix invalidations (`MY_WORDS_KEY`, `GROUPS_KEY`, …) keep working with no change.

### Screens

| Screen / component | Lists with a pager |
| --- | --- |
| `AdminLessonsPage` | lessons (filters reset to page 1) |
| `AdminLexemesPage` | lexemes. Inline `<nav>` replaced by `<Pager>` |
| `AdminSupportLinksPage` | unlink requests, learner links |
| `VocabularyModerationPage` | pending words. `AutofillApprovalsPanel`: autofill pending |
| `WordsToApprove` (supporter) | child pending approvals |
| `VocabularyBuilderPage` | my words |
| `VocabularySharedPoolPage` | shared pool (card grid) |
| `GroupsPage` | owned groups, joined groups (two pagers) |
| `GroupRequests` | group approvals |
| `GroupMembersTab` | members (new endpoint). Candidate filter uses `group.memberLearnerIds` |
| `GroupWordsTab` | pool picker: **server** search (debounced, page 1 on change). Selection kept across pages. History: pager |
| `GroupDashboardTab` | uses `group.activeMemberCount` instead of filtering members |
| `ChallengesPage` | challenges |
| `ChallengeBoardPage` | board entries (child: single page) |

Every page keeps its `isError` state. No Zustand changes: page state is local component state.

## Data / migration notes

- No EF Core migrations.
- New shared package version `WordBuddy.Shared.Kernel 1.1.0`. Docker and CI need
  `make publish-shared` before image builds. That is a push to GitHub Packages, so Sam runs it.
- Breaking response shapes (array → `PagedResult`). Backend and UI ship together in one PR. No
  other consumers.
- Cache: Identity group key changes to `identity:group-snapshot:{id}`. Old `identity:group:*`
  entries expire on their own.

## Open questions

1. **Lists not named in the proposal.** Learner `LessonsPage` (`GET /api/lessons`), quiz list,
   `ProgressPage`, my support links / pending supporter approvals, admin "add word" search picker.
   Default: **out of scope** here, to keep the PR reviewable. One follow-up issue. Sam: confirm,
   or move them in.
2. **Shared pool on the server.** The cached full snapshot is filtered and sliced in memory. The UI
   never gets all rows, but the server still loads the whole pool on a cache miss. Fine at today's
   size. DB paging + a cache version key is a later option.
3. **Pager on short lists.** Default: the pager shows whenever there is at least one item ("Page 1
   of 1 · 3 items"), to meet "every list shows the same pager". Alternative: hide it when there is
   only one page.
4. **Page in the URL.** Default: local state, the same as lexemes today. `?page=` in the URL
   (back button, shareable) is a possible follow-up.
5. **Overlap with WB-56 (moderator role).** WB-56 may change the moderation endpoints (rows 3–4).
   Whichever change merges second must rebase.
