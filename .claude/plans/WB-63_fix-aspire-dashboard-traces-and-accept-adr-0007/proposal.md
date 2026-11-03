---
title: WB-63_Fix Aspire dashboard traces and accept ADR 0007
status: reviewed
type: change
issue: 63
pr: 64
affects: docs/features/WB-61_adopt-net-aspire-for-local-orchestration.md
supersedes: WB-61_adopt-net-aspire-for-local-orchestration
version: 4.0
created: 2026-11-02T23:48:56+07:00
updated: 2026-11-03T11:05:48+07:00
---

## Problem

WB-61 shipped the Aspire AppHost, but `/test` found 3 gaps.

```text
appsettings.Development.json: OpenTelemetry:OtlpEndpoint = localhost:4317
        beats
Aspire-injected OTEL_EXPORTER_OTLP_ENDPOINT
        → traces go to 4317 → dashboard Traces page is empty
```

- On a machine with a local `appsettings.Development.json` (Sam's), no traces reach the dashboard.
- When traces arrive, they show as `WordBuddy.<Service>`, not under the `identity` / `content`
  resources.
- ADR 0007 is still `Status: proposed`. The README says trace mapping is "not verified".

## Goal

`aspire run` shows traces for all 5 APIs in the dashboard on any dev machine. ADR 0007 is
`accepted`, and the README states the verified behaviour.

## Target users

Developers only. No learner-facing change: child and adult behaviour stays the same.

## Success criteria

- Traces from all 5 APIs appear in the dashboard, even with a local `appsettings.Development.json`
  that sets `OpenTelemetry:OtlpEndpoint`.
- An AppHost test asserts the OTLP endpoint key for every API.
- ADR 0007 reads `Status: accepted`.
- The README describes the verified trace behaviour, with no "not verified" text.
- The WB-61 living spec "Known gap" is resolved.

## Constraints

- Fix in the AppHost only (for example, set `OpenTelemetry__OtlpEndpoint` per API). No change under
  `src/`, `WordBuddy.UI/`, `docker-compose.yml`, `Makefile` or `k8s/`.
- `/test` confirmed the env-var fix by hand during WB-61.
- Open question for `/plan`: also map traces to resources (`OTEL_SERVICE_NAME`), or only document
  the `WordBuddy.<Service>` naming. The shared helper's `AddService(serviceName)` may override it.
- Out of scope: OTLP metrics and logs export in `WordBuddy.Shared.Infrastructure`.
