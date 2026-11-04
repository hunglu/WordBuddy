# 0008 — gRPC for internal synchronous service calls

- Status: accepted (pending Sam's confirmation in the PR)
- Date: 2026-11-04 (WB-68)

## Context

- There is one internal synchronous call: Progress asks Content for senses by ids, to build a review exercise.
- It is HTTP/JSON today (`GET /api/vocabulary/senses?ids=`). Progress keeps its own copy of the reply DTO, so the two shapes can drift.
- The call forwards the learner's JWT, so Content applies the child filter. That rule must stay.
- ADR 0001 forbids project references between services and to shared code. Shared code ships as NuGet packages.

## Decision

gRPC is the **default** channel for internal synchronous calls. REST stays, selectable by config.

```
Progress ── gRPC h2c :8081 (Transport=Grpc, default) ──► Content
Progress ── REST :8080     (Transport=Rest, kept)    ──► Content
```

| Topic | Decision |
| --- | --- |
| Contract | `content_senses.proto` (package `wordbuddy.content.v1`) in a new NuGet package `WordBuddy.Shared.Protos`, built with `Grpc.Tools`, stubs for client and server. Same model as other shared code (ADR 0001) |
| Why a new package | `WordBuddy.Shared.Contracts` stays free of protobuf and gRPC dependencies. Copying the `.proto` per service would bring back the drift |
| Switch | `Services:Content:Transport` = `Grpc` (default) or `Rest`. Read once at startup. Unknown value stops the host. No automatic fallback at runtime |
| Auth | The gRPC client reuses `ForwardBearerTokenHandler`. Content applies the same `[Authorize]` and `ChildHasSupporter` policy as the REST controller. User id and `AgeGroup` come from the JWT, never from the message |
| Ports | Content has two Kestrel endpoints from `Kestrel:Endpoints`: `Http` (HTTP/1.1) and `Grpc` (HTTP/2 only, cleartext). Local and Aspire: 5081 and 5091. Containers: 8080 and 8081 |
| Exposure | Internal only: compose `expose`, k8s `ClusterIP` port `grpc` with `appProtocol: kubernetes.io/h2c`, no Ingress rule. The public API, the UI and the Vite proxy do not change |
| Resilience | Same standard resilience settings as the REST client. Any `RpcException` becomes `Content.Unavailable`, like a failed REST call |
| Cache | One `SessionSenseCache` for both clients. Key `progress:session-senses:{sessionId}`, absolute expiry, unchanged |
| Health | Content serves `grpc.health.v1` (liveness only). Progress adds the readiness check `content-grpc` when `Transport=Grpc`. The REST check `content` stays |
| Errors | `Validation` to `InvalidArgument`, `NotFound` to `NotFound`, other to `Internal`. HTTP 401/403 reach the client as `Unauthenticated`/`PermissionDenied` |
| Telemetry | No shared-library change. ASP.NET Core instrumentation covers inbound calls, HTTP client instrumentation covers outbound calls, because the gRPC client runs on `IHttpClientFactory` |
| Transport security | Plaintext h2c inside the cluster and compose network, like REST today. TLS or mTLS is out of scope |
| Rejected | Putting the `.proto` in `Shared.Contracts`. Copying the `.proto` per service. Automatic gRPC to REST fallback (hides outages, doubles the test matrix) |

## Consequences

- Progress and Content share one typed contract. A change is a new package version.
- Content now listens on two ports. `--urls` and `ASPNETCORE_URLS` no longer apply to it; use `Kestrel:Endpoints`.
- The Aspire AppHost needs no endpoint code for this: Aspire reads Content's `Kestrel:Endpoints`, models both endpoints and injects the target ports.
- Operators switch back with one variable: `Services__Content__Transport=Rest`, then restart Progress.
- The gRPC endpoint has no rate limiting, like the REST `senses` route.
- Open: k8s native gRPC probes, service identity tokens instead of user-token forwarding, proto lint and breaking-change checks in CI, gRPC reflection for dev tools.
