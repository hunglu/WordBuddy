---
title: WB-32_Move CQRS abstractions to shared library
status: idea
type: change
issue: 32
pr: none
affects: none
supersedes: none
version: 1.0
created: 2026-10-06T22:34:27+07:00
updated: 2026-10-06T22:34:27+07:00
---

## Problem

The CQRS abstractions are declared four times: `ICommand`, `ICommand<TResult>`,
`IQuery<TResult>`, `ICommandHandler` and `IQueryHandler` live in each service's
`Application/Abstractions/` (Identity, Content, Quiz, Progress). Copies can drift, and every new
service must copy them again.

## Goal

- Declare the abstractions once in a shared, versioned NuGet package.
- Identity, Content, Quiz and Progress use the package and delete their local copies.
- No behaviour change: handlers, the validation decorator and DI registration work as today.

## Target users

Both. No user-visible change.

## Success criteria

- No service declares its own `ICommand`, `IQuery`, `ICommandHandler` or `IQueryHandler`.
- All four services build; every existing unit, integration and E2E test passes.
- No project reference between services (ADR 0001 independence model kept; shared code only via
  the package).

## Constraints

- Open point for `/plan`: put them in `WordBuddy.Shared.Kernel` (already has `Result<T>`) or in a
  new `WordBuddy.Shared.Application` package. The Domain layer must not gain a dependency it does
  not need.
- Package version bump and `make publish-shared` (Sam runs it, token needed).
- Refactor only; no living spec changes (`affects: none`).
- Out of scope: Notification (scaffold), the `develop-webapi` skill text beyond updating the
  namespace it shows.
