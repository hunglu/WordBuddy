# Review: WB-27_Learning groups

PR: #39 · Round 1 · Reviewed commit: 105b417 · 2026-10-10T21:13:37+07:00

## Verdict

Changes requested — one major: an out-of-order `LearnerGroupDeleted` can leave active members of a deleted group in the Content and Progress projections.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `Content.Application/Features/Groups/Commands/ApplyLearnerGroupEvent/ApplyLearnerGroupEventCommandHandler.cs:99-123` (same in Progress `ApplyLearnerGroupEventCommandHandler.cs:62-86`) | The 3 events use 3 separate queues (`LearnerGroupQueues`), so order is not guaranteed. A delete only updates rows that already exist. Scenario: owner adds adult (Activated t1), then deletes the group (Deleted t2). Deleted is consumed first: no rows, no change. Activated t1 then creates an **active** row. Result: the owner can still assign words to that learner (Content) and see that learner in the group dashboard (Progress) for a deleted group. The member-level case is safe (Removed creates an inactive row first). | Keep a group tombstone: on `LearnerGroupDeleted`, store `GroupId + DeletedAtUtc` (small table, or a marker row). In `ApplyToMemberAsync`, ignore any event with `OccurredAtUtc <= DeletedAtUtc`. Add a unit test "Deleted before Activated → no active row" in both services. |
| 2 | nit | `Progress.Application/Features/Groups/Queries/GetGroupDashboard/GetGroupDashboardQueryHandler.cs:73-82` | The cached dashboard (5 min, no delete) can show a removed member's row for up to 5 minutes. The owner check runs before the cache, so no cross-owner leak. This matches the plan. | Optional: drop `progress:dashboard:group:{groupId}:*` keys in the apply handler, or shorten the TTL. |
| 3 | nit | `Identity.Application/Features/Groups/Queries/GetGroup/GetGroupQueryHandler.cs:592-600` | The cached group detail keeps the old alias/avatar after a member changes them, until expiry. | Optional: accept, or invalidate on profile change. |

Verified, no finding:

- Child privacy: group views show `PublicName` + `AvatarId` only; events carry ids only; logs carry ids and counts only.
- Authorization: owner check in every command and query (also on cache hits); child cannot create, leave or approve; only the active Primary approves; `*.Forbidden` → 403 in all 3 services, and no older error code ends in `Forbidden`, so no existing status changes.
- Revoke: both `Revoke(` call sites stage the cleaner changes and outbox events in the same save as the link change.
- Consumers: EF inbox + `OccurredAtUtc` check; replay is a no-op.
- Strict child check in `AssignWordsToGroup`: correct given events carry no age group; adult-only senses are skipped for all members (accepted deviation).
- Migrations: 3 migrations with matching `docs/database-diagram/{identity,content,progress}.md`.
- Frontend: no `any`/`enum`/raw colours/inline style/`useEffect` fetches; `isError` and 403 handled.

Ops note (not a code finding): `make publish-shared` failed with 401, so CI/Docker restore of `WordBuddy.Shared.Contracts` 1.3.0 from GitHub Packages may fail on this PR. Only the GitGuardian check ran on 105b417. Sam needs to publish 1.3.0 before merge.

## Plan conformance

- All backend, frontend and test tasks in `tasks.md` are in the diff.
- E2E API/UI moved to `/test` (Sam, 2026-10-10) — the tester must write them.
- Accepted deviations (plan R1 and coder notes): strict child safety, `LearnerWords.AddedByUserId`, empty groups unknown to Content/Progress (empty list / 400 / 403), handler-level owner checks, delete publishes only `LearnerGroupDeleted`, word picker uses the shared pool list.
- `LearnerGroupDeleted` only: this is acceptable **after** finding 1 is fixed; it is the root of that finding.
- No out-of-scope changes found.
