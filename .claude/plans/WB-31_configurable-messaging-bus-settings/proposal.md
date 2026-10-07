---
title: WB-31_Configurable messaging bus settings
status: idea
type: change
issue: 31
pr: none
affects: docs/features/messaging.md
supersedes: none
version: 1.0
created: 2026-10-06T21:42:26+07:00
updated: 2026-10-06T21:42:26+07:00
---

## Problem

All MassTransit time settings are hard-coded in `MessagingExtensions.cs` (Shared.Infrastructure
1.1.0): outbox `QueryDelay` (100 s), retry intervals (200 ms, 1 s, 5 s). Changing them needs a new
package version and a rebuild of every service. The 100 s outbox delay also looks too slow for
local work. Missing RabbitMQ credentials silently fall back to `guest`/`guest` (review nit 3).

## Goal

- Every bus time setting is read from `appsettings` (section `Messaging`), bound to an options
  class with defaults and validated at startup. At least: outbox query delay, retry intervals
  (or retry count + interval), and any other timeout/delay the bus setup uses (for example
  inbox/outbox duplicate-detection window, broker connection / heartbeat timeouts).
- Each service (Content, Progress) has the section with its values in `appsettings.json`;
  environment variables override them in compose and kind.
- Fail at startup when RabbitMQ username or password is missing outside Development.

## Target users

Both. No user-visible change; no child data involved.

## Success criteria

- No time value is hard-coded in the messaging setup; defaults live in the options class.
- Changing a value in `appsettings` or an env var changes the bus behaviour without a code change.
- Invalid values (negative, empty retry list) stop the service at startup with a clear error.
- Unit tests cover binding, defaults and validation; existing suites stay green.

## Constraints

- Change to `WB-21_messaging-rabbitmq-foundation`. Shared.Infrastructure version bump
  (1.1.0 → 1.2.0) and `make publish-shared` (ask-gated).
- Out of scope: delayed redelivery, durable RabbitMQ storage.
