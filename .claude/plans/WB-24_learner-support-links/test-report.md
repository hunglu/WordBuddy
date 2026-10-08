# Test report: WB-24_Learner support links

Run: 2026-10-08 · Branch `feature/WB-24_learner-support-links` (HEAD 89dd5e1 + origin/main, up to date) · PR #35

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Identity.UnitTests | 60 passed, 0 failed |
| Identity.IntegrationTests | 4 passed, 0 failed |
| Content.UnitTests | 133 passed, 0 failed |
| Content.IntegrationTests | 82 passed, 0 failed |
| Progress.UnitTests | 122 passed, 0 failed |
| Progress.IntegrationTests | 33 passed, 0 failed |

## E2E API

Skipped — services not running; starting `docker compose` was denied by the permission check.

New: `e2e/api/WordBuddy.E2E.Api.Tests/SupportLinkTests.cs` (builds, not run).

| Test | Covers |
| --- | --- |
| `SupportLink_ChildLifecycle_SecondSupporterLosesAccessAfterUnlink` | child 403 → invite → adult accepts (Primary) → child learns → cap set → second pending → Primary approves → child cannot unlink → unlink + confirm → access lost |
| `SupportLink_PrimaryUnlinkRequest_IsRejected` | Primary link cannot be unlinked |
| `SupportLink_AdultLearner_AcceptedLinkIsActiveWithoutPrimary` | adult path: no gate, no Primary |
| `SupportLink_ChildAcceptsInvitation_IsRejected` | supporter must be Adult |

## E2E UI

Skipped — services not running (same reason).

New: `e2e/ui/features/support-links.feature` + `e2e/ui/steps/support-links.steps.ts` (`bddgen` OK, not run).

- Child sees the "Add a supporter" gate and reaches `/support`.
- Adult accepts a child's invitation link; the gate opens.
- Admin completes an escalated unlink. Needs Identity with `SupportLinks__UnlinkOverrideWaitDays=0`:
  `docker compose -f docker-compose.yml -f ../e2e/docker-compose.e2e.yml up -d` (override in `e2e/docker-compose.e2e.yml`).

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, reviewed commit 3858435 ancestor of HEAD | pass |
| 2. Changes since review (vs `merge-tree 3858435 origin/main`) | pass — only `.claude/plans/WB-24_learner-support-links/proposal.md`, `review.md` |
| 3. Every suite ran with zero failures | fail — E2E API and E2E UI skipped |

## Failures

None in suites that ran.

Risk for the next run: existing E2E tests that register a `Child` and call learning endpoints
(`VocabularyReviewSessionTests`, `VocabularySrsTests`, `PersonalVocabularyTests`,
`LearnerWordMessagingTests`, `vocabulary-review.feature` child scenario) will likely get
403 `Learner.SupporterRequired` now. They need a supporter link in their setup.

## Verdict

Not merged — E2E API and E2E UI skipped (services not running)
