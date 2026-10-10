# Test report: WB-27_Learning groups

Verdict: all suites green. Approved and merged into main.

Tested head: `fc5a4f8` (reviewed code `da34a64`) with `origin/main` (`6e4e2f3`, WB-26) already merged; the merge was a no-op.

## Backend unit/integration

| Service | Suite | Result |
| --- | --- | --- |
| Identity | UnitTests | 108 passed, 0 failed |
| Identity | IntegrationTests (real SQL Server, localdb) | 6 passed, 0 failed |
| Content | UnitTests | 230 passed, 0 failed |
| Content | IntegrationTests | 97 passed, 0 failed |
| Progress | UnitTests | 199 passed, 0 failed |
| Progress | IntegrationTests | 50 passed, 0 failed |
| UI | `npm run build` | passed |

## E2E API

49 passed, 0 failed (`dotnet test e2e/api/WordBuddy.E2E.Api.Tests`). Stack: local compose, Identity / Content / Progress / UI rebuilt from this branch, `e2e/docker-compose.e2e.yml` override, migrations applied to the local compose DB only.

New: `LearningGroupTests.cs` (3 tests)

| Test | Proves |
| --- | --- |
| `LearningGroup_AssignList_AddsWordsToAdultAndApprovedChildAndSkipsAdultOnlyWord` | Adult added `Active`; child added `PendingPrimaryApproval`; only the Primary sees the request; after approval one assign call adds the child-safe word to both (`added 2`), skips the adult-only word (`skippedForChildren 2`); repeat gives `alreadyHad 2`; history lists the word |
| `LearningGroup_Dashboard_ListsActiveMembersByIdOnlyAndRejectsOtherSupporter` | Dashboard lists both members by id, no name / email / alias in the body; other supporter gets 403 on dashboard and on assign |
| `LearningGroup_RevokedSupportLink_RemovesLearnerFromGroup` | After the supporter's link to the child is revoked: Identity drops the child at once (adult stays), Progress dashboard drops the child, Content assigns to the adult only |

## E2E UI

32 passed, 0 failed (`npm test` in `e2e/ui`, chromium). New: `features/learning-groups.feature` + `steps/learning-groups.steps.ts` (3 scenarios).

- Create group, add members, assign a word (`Added 2`), Dashboard tab lists both learners.
- Child is listed by alias and avatar only; real name and email are not on the page.
- Groups page shows an error message when the group requests fail.

## Failures

None in the final run. Findings and test-design notes from getting there:

| # | Type | Detail |
| --- | --- | --- |
| 1 | Behaviour (known, not a defect) | The group dashboard is cached 5 min per (group, days, UTC offset) with no delete on member events. A poll right after add / revoke reads and caches the old answer, so the first API test version timed out. The tests now vary the client UTC offset per poll. Effect for users: after a revoke, the old supporter can still see that child's row for up to 5 min. Already on issue #27 as "dashboard cache invalidation". |
| 2 | Nit (UI) | `useMyGroups` keeps TanStack Query's default 3 retries (about 7 s) before the error text shows; other group queries use `retry: false`. The UI test waits 20 s for it. |
| 3 | Test data | Aliases are unique per user (`User.AliasTaken`, 409). The UI steps draw a fresh alias per scenario. |

## Merge guard

1. `review.md` verdict Approve, reviewed commit `da34a64` is an ancestor of HEAD: yes.
2. Files changed since the review, measured against reviewed code + current main (`git merge-tree` / `git diff --name-only`):

```
.claude/plans/WB-27_learning-groups/proposal.md
.claude/plans/WB-27_learning-groups/review.md
```

Both are on the allowlist. This run adds only `e2e/**`, `docs/features/**`, `docs/ai/learnings.md`, `test-report.md` and `proposal.md`, all on the allowlist.

3. Every suite ran in this session with zero failures: yes.

## Verdict

Approved and merged into main
