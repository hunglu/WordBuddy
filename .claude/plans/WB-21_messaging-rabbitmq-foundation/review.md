# Review: WB-21 Messaging RabbitMQ foundation

PR: #30 · Round 1 · Reviewed commit: 530d816 · 2026-10-05T21:15:28+07:00

## Verdict
Approve — no blockers or majors. Outbox, inbox, ordering and privacy rules are correct. Four optional nits.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `Content.Infrastructure/Persistence/ContentDbContext.cs` (`SaveChangesAsync`, `catch (DbUpdateException)`) | Only `DbUpdateException` detaches the new outbox rows. An `OperationCanceledException` or a raw `SqlException` leaves them tracked. Today the context is request-scoped, so the risk is low. A later save in the same scope would send an event for a change that did not happen. | Catch all exceptions (`catch`) for the detach step, then rethrow. |
| 2 | nit | `ContentDbContext.cs` | Only the async `SaveChangesAsync` publishes. A future sync `SaveChanges()` call would change `LearnerWord` with no event. No sync caller exists today. | Override `SaveChanges(bool)` to throw `NotSupportedException`, or document the rule in the XML doc. |
| 3 | nit | `Shared.Infrastructure/Messaging/MessagingExtensions.cs:63-64` | Missing `Username`/`Password` silently fall back to `guest`/`guest`. In a container this fails late with an auth error instead of a clear startup error. | Throw on a missing password when transport is `RabbitMq` (outside Development). |
| 4 | nit | `k8s/rabbitmq-deployment.yaml` (`volumes: emptyDir`) | Broker data is lost on pod restart. A message already moved from the outbox to a queue, but not yet consumed, is lost; Progress misses that membership. Acceptable for kind dev. | Note it in the k8s README, or use a PVC/StatefulSet before any shared environment. |

Checked and fine:

- Outbox: events are added to the same `DbContext` before `base.SaveChangesAsync` → one transaction. Rollback test present.
- Inbox: `UseEntityFrameworkOutbox` on every endpoint gives `MessageId` dedupe; unique index + upsert give natural-key dedupe; the unique-violation path re-reads the winner.
- Ordering: `LastEventAtUtc` guard; remove keeps the row (Sam Q3).
- Privacy: contracts carry ids and timestamps only; logs carry ids only; no `AgeGroup`.
- Secrets: `appsettings.json` has no password; `secret.yaml.example` has placeholder values only.
- Independence: no cross-service `ProjectReference`; shared code through packages `1.1.0`.
- k8s: RabbitMQ `ClusterIP` only, no Ingress/NodePort/LoadBalancer; port-forward documented (Sam Q4). Compose binds to `127.0.0.1`.
- DB diagrams: `docs/database-diagram/content.md` and `progress.md` updated with the migrations.

## Plan conformance
- All coder tasks in `tasks.md` are in the diff. E2E is handed to the tester, as planned.
- Sam's decisions are followed: soft flag, `rabbitmq:management`, internal management UI, ADR 0005 not edited, no event on user deletion.
- GitHub Packages publish of `1.1.0` is not done (ask-gated). Docker/CI builds need `make publish-shared` before `/test` in compose or kind.
- Out of scope: none found. `docs/features/messaging.md` is a new living-spec page; fine.
