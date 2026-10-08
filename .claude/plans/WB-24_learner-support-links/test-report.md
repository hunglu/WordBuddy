# Test report: WB-24_Learner support links

Run 3: 2026-10-08 · Branch `feature/WB-24_learner-support-links` (HEAD 661374f + origin/main, up to date) · PR #35 · stack started by Sam

**Not merged.** Application bug: Content and Progress share one RabbitMQ queue per link event, so each event reaches only one service.

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

36 total: 29 passed, 7 failed.

| Failed test | Cause |
| --- | --- |
| `SupportLinkTests.SupportLink_ChildLifecycle_SecondSupporterLosesAccessAfterUnlink` | Bug 1 |
| `VocabularySrsTests` × 3 (Child cases) | Bug 1, in the `ChildSupport` setup |
| `VocabularyReviewSessionTests` (Child) | Bug 1, in the setup |
| `PersonalVocabularyTests.RequestShare_ChildCaller_ReturnsForbidden` | Bug 1, in the setup |
| `LearnerWordMessagingTests.AddWord_ChildLearner_CreatesActiveMembershipInProgress` | Bug 1, in the setup |

Passed: the other 3 `SupportLinkTests` (Primary cannot be unlinked, adult path, child cannot accept) and all adult cases.

## E2E UI

24 total: 22 passed, 2 failed.

| Failed scenario | Cause |
| --- | --- |
| support-links: Admin completes an escalated unlink request | Environment: Identity runs without `SupportLinks__UnlinkOverrideWaitDays=0`; escalate returns 400 `SupportLink.EscalationTooEarly` |
| vocabulary-review: Child session (setup) | Bug 1 |

Passed: the child "Add a supporter" gate, and an adult accepting an invitation link.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, reviewed commit 3858435 ancestor of HEAD | pass |
| 2. Changes since review (vs `merge-tree 3858435 origin/main`) | pass — only allowlisted paths (`e2e/**`, plan folder files, `docs/ai/learnings.md`) |
| 3. Every suite ran with zero failures | fail — E2E API 7 failed, E2E UI 2 failed |

## Failures

**Bug 1 (application, blocker): each link event reaches only one service.**

```text
Identity --SupportLinkActivated--> queue "support-link-activated" (2 consumers)
                                     |-> Content   (round robin: events A, C, ...)
                                     '-> Progress  (events B, D, ...)
```

- Evidence: `rabbitmqctl list_queues` shows `support-link-activated 2` and `support-link-revoked 2`. Content and Progress both register `SupportLinkActivatedConsumer` / `SupportLinkRevokedConsumer` with default endpoint names.
- Effect: after a link is accepted, one service opens and the other stays 403 `Learner.SupporterRequired` for over 70 s. Which service opens alternates per link. A revoke can also miss one service, so a revoked supporter can keep access there.
- Fix (`/code`): per-service endpoint names (for example a `content-` / `progress-` prefix in `ConfigureEndpoints`). Delete the old shared queues afterwards.
- Files: `WordBuddy/src/Services/{Content,Progress}/*.Infrastructure/Extensions/ServiceCollectionExtensions.cs`.

**Environment: the E2E override is not applied.** Start the stack with `e2e/docker-compose.e2e.yml`, otherwise the admin scenario cannot escalate.

**Test fix in run 3:** the `ChildSupport` / `linkSupporter` probe used `GET /api/vocabulary` (405). It now uses `GET /api/lessons`.

## Verdict

Not merged — application bug: Content and Progress share the support-link event queues; E2E API 7 and E2E UI 2 failures
