---
title: WB-68_gRPC for internal service calls
status: done
type: new
issue: 68
pr: 69              # set by /code when it opens the pull request
affects: docs/features/WB-68_grpc-for-internal-service-calls.md
supersedes: none
version: 1.8
created: 2026-11-03T22:53:59+07:00
updated: 2026-11-04T10:09:40+07:00
---

## Problem

Internal synchronous calls between services use only hand-written HTTP/JSON clients (e.g.
Progress → Content via `ContentSenseClient`).

- Contracts are duplicated DTOs, not typed end to end.
- JSON over HTTP/1.1 adds latency and payload size.

## Goal

gRPC (HTTP/2 + protobuf, `.proto` contracts) becomes the **default** channel for
service-to-service synchronous calls. The existing REST endpoints and HTTP clients stay as a
non-default alternative — nothing is deleted.

```text
Browser/UI ──REST──> Vite proxy / Nginx / Ingress ──REST──> API   (unchanged)
Service A  ──gRPC──> Service B                                    (new, DEFAULT)
Service A  ──REST──> Service B                                    (kept, non-default)
Service A  ──RabbitMQ events──> Service B                         (unchanged)
```

Motivation: performance, typed contracts, and practising the gRPC pattern.

## Target users

Both (children and adults). Internal only — no child vs adult behaviour change; existing
authorization policies stay on the public REST endpoints.

## Success criteria

- Each internal synchronous call has a gRPC variant, and the caller uses it by default.
- Existing REST endpoints and HTTP clients still exist and still work when selected.
- Contracts live in `.proto` files; no service gets a project reference to another service.
- Public REST endpoints and the UI are unchanged.
- Works in Aspire, docker compose and kind.
- Health checks and OpenTelemetry traces cover the gRPC calls.

## Constraints

- gRPC is the default for internal calls, but additive: do not delete any REST endpoint or HTTP
  client.
- Out of scope: browser → API traffic (stays REST request/response).
- Out of scope: RabbitMQ event flows (stay MassTransit).
- Must keep the service-independence model (ADR 0001).
