---
title: WB-21_Messaging RabbitMQ foundation
status: done
type: new
issue: 21
pr: 30
affects: docs/features/messaging.md
supersedes: none
version: 1.6
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-06T16:48:21+07:00
---

## Problem

No messaging exists. No service references MassTransit and `WordBuddy.Shared.Contracts` has no
messages. Progress cannot learn which senses are in a learner's list, which Content owns.

## Goal

- RabbitMQ in docker compose and kind; MassTransit wired through `WordBuddy.Shared.Infrastructure`
  (in-memory transport for tests only, D9).
- First contracts in `WordBuddy.Shared.Contracts`: `LearnerWordAdded`, `LearnerWordRemoved`
  (ids only, no learner text, no `PersonalContext`).
- Content publishes them reliably (transactional outbox); Progress has an idempotent consumer that
  records membership.
- Doc fixes from idea.md section 11: remove `/internal/vocabulary-remaps` from
  `docs/architecture.md`; update `docs/product/roadmap.md` with the vocabulary themes.

## Target users

Both. No user-visible change. Events carry ids only, so no child data leaves Content.

## Success criteria

- Adding or removing a word in Content creates the membership row in Progress, or marks it inactive (soft flag, row kept), in
  compose and in kind.
- A duplicate message does not create a duplicate row.
- `/health/ready` reports the broker.
- Integration tests cover publish (Content) and consume (Progress).

## Constraints

- Source: `.claude/plans/vocabulary-module/idea.md` (D1, D9), ADR 0005 (messaging part).
- First of the vocabulary-module follow-ups; WB-22 and WB-24 depend on it.
- Out of scope: Azure Service Bus, scheduling, any UI change.
