# Review: WB-27_Learning groups

PR: #39 · Round 2 · Reviewed commit: da34a64 · 2026-10-10T21:38:08+07:00

## Verdict

Approve — the round-1 major is fixed in Content and Progress; no new blocker or major.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `Content.Application/.../ApplyLearnerGroupEventCommandHandler.cs:65-70, 86-101` (same in Progress) | Residual race. The member and delete consumers run in parallel on different queues. Activated (t1) reads "no tombstone" while Deleted (t2) reads "no rows"; both commit → active row in a deleted group. Window is milliseconds (add then delete almost at once). Duplicate deletes are safe: PK violation → retry → `MarkDeleted` no-op. | Optional: after the member save, re-check the tombstone and deactivate; or take an app lock on `GroupId` (`sp_getapplock`). Can be a follow-up. |
| 2 | nit | `docs/database-diagram/progress.md` | Whole file rewritten CRLF → LF (411 lines churn); real change is 6 lines. | Optional: keep the original line endings. |
| 3 | nit | `Content.Infrastructure/Persistence/ContentDbContext.cs:42` (same in Progress) | New `DbSet` has a blank line before it and no XML doc, unlike its neighbours. | Optional. |

Verified, no finding:

- Tombstone: `LearnerGroupDeleted` creates or moves forward `DeletedLearnerGroups` (PK `GroupId`); member events with `OccurredAtUtc <= DeletedAtUtc` are ignored and logged with ids only.
- Content and Progress handlers are identical except namespaces.
- Clock: `OccurredAtUtc` comes from the Identity event (`OutboxLearnerGroupEventPublisher`), mapped as-is by the consumers. No consumer clock is used, so no cross-service skew.
- Tests (both services): delete before add, replayed delete (one save), event after delete applied, delete after add deactivates.
- Migrations regenerated (branch-only, never applied): `20261010142457_AddLearnerGroups`, `20261010142511_AddLearnerGroupProjection`. Table, snapshot and `docs/database-diagram/{content,progress,README}.md` match.
- Child privacy: tombstone holds group id + time only; no new PII in logs.
- Nits 2 and 3 from round 1 left as is: **accepted**. TTL is 5 min, absolute, owner check runs before the cache, and `IDistributedCache` has no prefix delete. Already a scope suggestion.

Ops note (not a code finding): `WordBuddy.Shared.Contracts` 1.3.0 is not yet published (`make publish-shared` → 401). CI/Docker restore may fail. Sam must publish before merge.

## Plan conformance

- All `## Fix round 1` tasks are in the diff.
- No out-of-scope changes.

## Previous rounds


PR: #39 · Round 1 · Reviewed commit: 105b417 · 2026-10-10T21:13:37+07:00

### Verdict

Changes requested — one major: an out-of-order `LearnerGroupDeleted` can leave active members of a deleted group in the Content and Progress projections.

### Findings

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

### Plan conformance

- All backend, frontend and test tasks in `tasks.md` are in the diff.
- E2E API/UI moved to `/test` (Sam, 2026-10-10) — the tester must write them.
- Accepted deviations (plan R1 and coder notes): strict child safety, `LearnerWords.AddedByUserId`, empty groups unknown to Content/Progress (empty list / 400 / 403), handler-level owner checks, delete publishes only `LearnerGroupDeleted`, word picker uses the shared pool list.
- `LearnerGroupDeleted` only: this is acceptable **after** finding 1 is fixed; it is the root of that finding.
- No out-of-scope changes found.
