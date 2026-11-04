---
feature: Internal service calls (gRPC)
services: Content | Progress
audience: Both
state: proposed
last-updated-by: WB-68_grpc-for-internal-service-calls
---

# Internal service calls (gRPC)

Services call each other synchronously over gRPC; learners still reach the APIs over REST.

## What it does

_Proposed — not on main yet._

gRPC (HTTP/2 + protobuf, `.proto` contracts) is the default channel for service-to-service
synchronous calls. Existing REST endpoints and HTTP clients stay as a non-default alternative.
Browser/UI → API traffic stays REST request/response. RabbitMQ events stay as they are.

## Rules

- gRPC is the default for internal calls; REST stays available — no REST endpoint or HTTP client is removed.
- **Child vs adult:** no difference — internal calls only; authorization stays on public REST endpoints.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

None.

## Pending changes

- `WB-68_grpc-for-internal-service-calls` — gRPC as default for internal calls, REST kept (#68)

## Change history
