---
feature: Messaging
services: Content, Progress
audience: Both
state: shipped
last-updated-by: WB-21_messaging-rabbitmq-foundation
---

# Messaging

Content tells Progress which words are in each learner's list, through RabbitMQ.

```mermaid
flowchart LR
    H[Content: add / adopt / delete word] --> DB[(Content DB<br/>LearnerWord + OutboxMessage)]
    DB -->|outbox| MQ[(RabbitMQ)]
    MQ --> C[Progress consumers<br/>inbox dedupe]
    C --> PDB[(Progress DB<br/>LearnerWordMemberships)]
```

## What it does

- Every `LearnerWord` insert in Content publishes `LearnerWordAdded`; every delete publishes `LearnerWordRemoved`.
- Publishing uses the MassTransit EF Core outbox: the event is saved in the same transaction as the change.
- Progress consumes both events into `LearnerWordMemberships` (one row per user + sense).
- Transport: RabbitMQ (`rabbitmq:management`) in docker compose and kind. In-memory transport for tests only.

## Rules

| Rule | Detail |
| --- | --- |
| Payload | Ids and timestamps only. No word text, no personal context, no names. |
| Idempotency | Inbox dedupes on `MessageId`; unique index (`UserId`, `SenseId`) + upsert. |
| Ordering | An event older than `LastEventAtUtc` is ignored. |
| Remove | Soft: `IsActive = false`; the row is kept. |
| System owner | Hand-over to the system owner publishes no event. |
| User deletion | Publishes nothing (users are soft-deleted). |
| Health | Broker check is part of `/health/ready`. |
| Management UI | Internal only: `kubectl port-forward svc/rabbitmq 15672:15672`. No Ingress. |

### Child vs. adult

Same behaviour for both. Events carry no `AgeGroup` and no child data.

## API

No public endpoints.

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

None.

## Pending changes

None.

## Change history

- `WB-21_messaging-rabbitmq-foundation` — RabbitMQ + MassTransit; Content publishes LearnerWordAdded/Removed, Progress consumes them. (#21)
