# Review: WB-55_Apply paging to table/list

PR: #57 · Round 1 · Reviewed commit: b70f37e · 2026-10-15T08:59:07+07:00

## Verdict

Approve — no blockers or majors; all 16 endpoints and 14 screens follow the plan. 5 optional nits.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `WordBuddy.UI/src/hooks/useSupportLinks.ts:147` (used at `pages/AdminSupportLinksPage.tsx:43`) | `keepPreviousData` on a query keyed by `learnerId`. When the admin loads learner B, learner A's links stay on screen under "New Primary" until B arrives. The server is safe: `AdminHandoverPrimaryCommandHandler` returns `LinkNotFound` for a link of another learner. The screen still briefly shows the wrong child's links. | Keep the previous data only when the learner id is the same: `placeholderData: (prev, prevQuery) => prevQuery?.queryKey[1] === learnerId ? prev : undefined`. Or hide the radios while `isPlaceholderData`. |
| 2 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Domain/Sense.cs:25-28` | The new `WordMaxLength` was inserted under the XML doc of `RegisterNoteMaxLength`. `WordMaxLength` now reads "Maximum length of RegisterNote", and `RegisterNoteMaxLength` has no doc. | Move the doc back to `RegisterNoteMaxLength`. Add `/// <summary>Maximum length of <see cref="Word"/>.</summary>` to `WordMaxLength`. |
| 3 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.IntegrationTests/ChallengeEndpointsTests.cs` | Whole file flipped CRLF → LF. The diff shows +457/−424; the real change is +47/−14. The diff and `git blame` become noisy. | Restore CRLF for this file (see `docs/ai/learnings.md`, entry of 2026-10-14). |
| 4 | nit | `WordBuddy.UI/src/components/groups/GroupDashboardTab.tsx:24` | The new members query has no `isError` branch. On a 502, rows silently show "Learner" without avatar. There is no crash and no blank screen. | Optional: show a short inline note when `members.isError`. |
| 5 | nit | `GetGroupWordAssignmentsQueryHandler.cs:62`, `GetUnlinkRequestsForAdminQueryHandler.cs:44`, `GetPendingChildApprovalsQueryHandler.cs:52` | Orphan rows are dropped **after** the DB page is cut. A page can hold fewer than `pageSize` items, while `totalCount` still counts them. This happens only with orphan data, so the risk is low. | Accept as is, or filter the orphans in the repository query before `ToPagedResultAsync`. |

Checked and fine:

- Auth and child rules: policies unchanged. `totalCount` is counted after the child filters (shared pool cache variant, consent check). The child board ignores paging and always returns the top 5 plus the own row.
- Paging contract: `page ≥ 1` and `1 ≤ pageSize ≤ 100` in every validator. A page past the end returns 200 with empty items. `Id` is the last order key on every DB query.
- Identity snapshot: the new key `identity:group-snapshot:{id}` is used by `LearnerGroupCache.Key`, so every command still invalidates it. The owner check runs on cache hits.
- No `.Result`/`.Wait()`, structured logs only, no PII or tokens in logs. Kernel is 1.1.0 in every Content, Identity and Progress project, and Quiz stays on 1.0.1.
- UI: TanStack Query only, no `any`/`enum`, `wb-` tokens only. Every paged screen keeps its `isError` message. Prefix invalidations still match the new keys.
- No EF migration, so no database diagram update is needed.

## Plan conformance

| Area | Status |
|---|---|
| Shared Kernel (`PagedResult<T>`, `Paging`, 1.1.0) | Done |
| Content (7 endpoints, lexeme shape aligned) | Done |
| Identity (6 endpoints, `GET /groups` removed, snapshot loader) | Done |
| Progress (challenges, board) | Done |
| UI (`Pager`, `usePagination`, 14 screens) | Done |
| Unit + integration tests per `tasks.md` | Present (tester verifies) |

Coder decisions outside the plan, all accepted:

- `LearnerGroupSnapshot`/`LearnerGroupSnapshotLoader` are public, because public handlers inject them.
- The dashboard reads members once with `pageSize=100`, which is safe because a group has at most 40 members.
- "My words" uses `CARD_PAGE_SIZE` 24.
- A child does not send the owned-groups request.
- `clamp` is called during render. This is a valid React pattern: a guarded `setState` on the same component.
- Extra items: `MAX_PAGE_SIZE` in `api/paging.ts` and `Sense.WordMaxLength`. Both are small and in scope.

Known open items for `/test`:

- `e2e/api` and `e2e/ui` still use the old array shapes and the removed `GET /api/auth/groups` (for example `e2e/ui/steps/support-links.steps.ts:143` and `learning-groups.steps.ts:118`).
- The living spec in `docs/features/` is not updated yet.
- Sam must run `make publish-shared` before the Docker/CI builds.
