# Test report: WB-26_Supporter dashboard

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Progress unit tests | 183 passed, 0 failed |
| Progress integration tests (real SQL Server) | 47 passed, 0 failed |
| UI build (`npm run build`) | OK |

Command: `dotnet test src/Services/Progress/WordBuddy.Progress.slnx`. Branch already contained `origin/main` (merge was a no-op).

## E2E API

46 passed, 0 failed, 0 skipped (`dotnet test e2e/api/WordBuddy.E2E.Api.Tests`).

New class `SupporterDashboardTests` (3 tests):

| Test | Proves |
| --- | --- |
| `SupporterDashboard_AfterLearnerReviews_ShowsActivityAndRetention` | Link → learner reviews 4 words → supporter dashboard: streak ≥ 1, heatmap sums to 4, retention object present, no time-of-day field; learner `me` view returns the same learner |
| `SupporterDashboard_NoLink_Returns403` | No link, no access |
| `VocabularySession_GetTwice_ReturnsSameSessionId` | Session resume (R1) |

Limit: new words are not "due", so retention has no sample and its percentage is `null`. The test checks the field and range, not a number. Retention maths is covered by unit tests.

## E2E UI

29 passed, 0 failed (`npm test` in `e2e/ui`). New feature `supporter-dashboard.feature` (3 scenarios):

- Learner opens their own dashboard.
- Supporter opens the learner dashboard from `/support`.
- Error message when Progress is down (dashboard calls stubbed with 502 via `page.route`).

Setup notes:

- Progress and UI images rebuilt; `AddDashboard` migration applied by Progress at startup to the local compose DB only.
- Identity and Content were started with `e2e/docker-compose.e2e.yml`. Without it the WB-24 scenario "Admin completes an escalated unlink request" fails (needs `UnlinkOverrideWaitDays=0`). That is a setup need, not a WB-26 defect. Re-run with the override: pass.

## Failures

None.

## Merge guard

1. `review.md` verdict Approve; reviewed commit `08bdc9b` is an ancestor of HEAD. Pass.
2. `git diff --name-only <merge-tree base> HEAD` before this run's commit:
   - `.claude/plans/WB-26_supporter-dashboard/proposal.md`
   - `.claude/plans/WB-26_supporter-dashboard/review.md`

   Both are on the allowlist. Pass.
3. All suites ran this session with zero failures. Pass.

## Verdict

Approved — merged into main
