# Review: WB-24_Learner support links

PR: #35 · Round 2 · Reviewed commit: ec1c25a · 2026-10-08T16:29:37+07:00

## Verdict
Approve — the round-1 test blocker (shared support-link queues) is fixed and covered by tests; no new blockers or majors.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | WordBuddy/src/Services/{Content,Progress}/*.Infrastructure/Extensions/ServiceCollectionExtensions.cs:28/35 | The old shared queues (default consumer names) may still exist in a running RabbitMQ, bound to the event exchanges. They fill with messages that nobody consumes. | Delete the old queues once in dev/kind (`docker compose down -v` or the RabbitMQ UI). |
| 2 | nit | same | Events consumed by the wrong service before this fix are not replayed. Existing dev projections can be stale. | Reset the dev databases or relink in E2E seeds; no code change needed. |
| 3 | nit | round 1 #1–#3 | Still open (projection lag, 40-bit code, unlink request after handover). | As in round 1. |

## Plan conformance
- Fix delta: `SupportLinkQueues.cs` per service with prefixes `content-` / `progress-`; endpoints named in both registrations; `SupportLinkQueueTests` in both integration suites check the input address.
- Tester commits (e2e API/UI steps, `ChildSupport` helper, `docker-compose.e2e.yml`, test report, learnings): read lightly. Test-only, no application code.
- Out of scope: none.

## Previous rounds

### Round 1
PR: #35 · Round 1 · Reviewed commit: 3858435 · 2026-10-08T13:57:24+07:00

### Verdict
Approve — no blockers or majors; rules from plan.md (resolved 2026-10-08) are enforced and tested.

### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | WordBuddy/src/Services/Progress/WordBuddy.Progress.Api/Authorization/SupportLinkAuthorization.cs:102 (same in Content) | `ChildHasSupporter` reads the local projection. Right after accept, the child can get 403 `Learner.SupporterRequired` until the outbox event is consumed. | Accept as eventual consistency; let the UI gate retry or refetch after accept. Document in the living spec. |
| 2 | nit | WordBuddy/src/Services/Identity/WordBuddy.Identity.Domain/SupportLinks/SupportLinkInvitation.cs:14 | Code is 8 chars from 32 symbols (40 bits). Safe only with the accept rate limit and 7-day expiry. | Keep the strict accept rate-limit policy; note the dependency in the spec. |
| 3 | nit | WordBuddy/src/Services/Identity/WordBuddy.Identity.Domain/SupportLinks/UnlinkRequest.cs (Cancel/Escalate) | After an admin Primary handover, an open unlink request is tied to the old Primary's user id; the new Primary cannot cancel or escalate it. | Close or reassign open learner-side requests in `AdminHandoverPrimary`, or leave as a known edge case. |

### Plan conformance
- Covered: all 47 implementation tasks (Contracts, Identity domain/infra/app/api, Content and Progress projections + policies, supporter cap, UI pages, docs, unit + integration tests).
- E2E API/UI tasks moved to `/test` by Sam — not a gap.
- Migrations: Identity, Content, Progress added; `docs/database-diagram/{identity,content,progress}.md` updated.
- Out of scope: none found.

Checked: no secrets, no PII in log templates (ids/codes only), `AdminOnly` on admin controller, child cannot act on links (`ChildForbidden`), Primary cannot be unlinked, outbox in same save, idempotent consumers, no `.Result`/`.Wait()`, no `any`/`enum`/inline style in UI diff.
