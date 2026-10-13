# Test report: Move CQRS abstractions to shared library

## Backend unit/integration

| Service | Unit | Integration |
| --- | --- | --- |
| Identity | 130 pass | 16 pass |
| Content | 248 pass | 98 pass |
| Quiz | 2 pass | none in project |
| Progress | 397 pass | 84 pass |

All run with `dotnet test src/Services/<Service>/WordBuddy.<Service>.slnx`. 0 failed, 0 skipped.
No new test files were needed; the per-service `SharedCqrsAbstractionsTests.cs` from `/code` cover the change.

## E2E API

55 / 55 pass.

## E2E UI

35 / 36 pass. One failure, accepted by Sam as a known environment gap (see Failures).

## Failures

- `support-links.feature:23` "Admin completes an escalated unlink request" — escalate returned non-204.
  Cause: progress-api lacks `SupportLinks__UnlinkOverrideWaitDays=0`. Not related to this change.

## Merge guard

- Review verdict Approve; reviewed commit `1c9b6d6` is an ancestor of HEAD.
- `origin/main` already merged (up to date).
- Files changed since review, vs reviewed code + main:
  - `.claude/plans/WB-32_move-cqrs-abstractions-to-shared-library/proposal.md`
  - `.claude/plans/WB-32_move-cqrs-abstractions-to-shared-library/review.md`
  - Both on the allowlist.

## Verdict

Approved and merged into main
