---
feature: Internal service calls (gRPC)
services: Content | Progress
audience: Both
state: shipped
last-updated-by: WB-68_grpc-for-internal-service-calls
---

# Internal service calls (gRPC)

Progress calls Content over gRPC by default; learners still reach the APIs over REST.

## What it does

- Progress → Content "get senses by ids" (used when a review session starts) goes over gRPC (HTTP/2 + protobuf).
- The contract is `content_senses.proto` in the shared package `WordBuddy.Shared.Protos`.
- REST stays as a non-default option: set `Services:Content:Transport=Rest` on Progress.
- Browser/UI → API traffic stays REST. RabbitMQ events are unchanged.

| Item | Value |
| --- | --- |
| Default transport | `Grpc` (`Services:Content:Transport`) |
| Content ports | REST 5081, gRPC 5091 (containers: 8080 / 8081) |
| Progress config | `Services:Content:GrpcUrl` (default `http://localhost:5091`) |
| Progress health | `content` (REST) and, under gRPC, `content-grpc` (`Degraded`) |
| Content health | `grpc.health.v1` `Check` (liveness) |
| Exposure | gRPC port is cluster/compose-internal only (no host port, not in the Ingress) |

## Rules

- gRPC is the default for internal calls; REST stays available. No REST endpoint or HTTP client was removed.
- The gRPC service enforces the same `[Authorize]` + `ChildHasSupporter` policy as REST `GET /api/vocabulary/senses`.
- Progress forwards the caller's bearer token; Content takes user id and `AgeGroup` from the JWT.
- **Child vs adult:** identical on both transports. A child with a supporter sees only child-visible senses; a child without a supporter is denied (`PermissionDenied`); an adult sees all permitted senses.
- Failures: bad GUID or 0 / over 100 ids → `InvalidArgument`; unknown or hidden ids are omitted; Content down → `Content.Unavailable` in Progress.
- Unknown `Transport` value fails fast at startup.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|
| gRPC | `wordbuddy.content.v1.ContentSenses/GetSensesByIds` | Content (:5091) | `[Authorize]` + `ChildHasSupporter` |
| gRPC | `grpc.health.v1.Health/Check` | Content (:5091) | none |

## UI

None.

## Pending changes

None.

## Change history

- `WB-68_grpc-for-internal-service-calls` — gRPC as default for Progress → Content, REST kept (#68)
