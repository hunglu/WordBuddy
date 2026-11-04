# Review: gRPC for internal service calls

PR: #69 · Round 1 · Reviewed commit: fa37612 · 2026-11-04T09:09:42+07:00

## Verdict

Approve — no blockers, no majors. The gRPC path keeps the same auth and child filter as REST, and REST still works when selected.

```text
Progress ──gRPC h2c :8081 (default)──► ContentSensesGrpcService ─┐
Progress ──REST :8080 (Transport=Rest)──► PersonalVocabularyController ─┤
                                                                       └► same GetSensesByIds handler
   both: [Authorize] + ChildHasSupporter, user id + AgeGroup from JWT only
```

## What was verified

| Check | Result |
| --- | --- |
| Correctness | Bad GUID → `InvalidArgument`; `Result` failures → `RpcException`; ids chunked by 100; any transport error → `Content.Unavailable`; cache logic moved, key and expiry unchanged |
| Security / child path | Same class-level `[Authorize]` + `ChildHasSupporter` as the REST controller. User id and `AgeGroup` read from `context.GetHttpContext().User`, not the message. Integration tests cover Adult, Child with/without supporter, no token |
| Exposure | gRPC port 8081: compose `expose` only, k8s ClusterIP, Ingress unchanged |
| Independence (ADR 0001) | Contract ships as NuGet `WordBuddy.Shared.Protos` 1.0.0; no `ProjectReference` across services |
| Conventions | Hand-rolled handler reused, no MediatR, async only, structured logs with ids/counts only, XML docs on new types |
| Health / OTel | Content serves `grpc.health.v1` (liveness only, not added to `/health/ready`); Progress adds `content-grpc` only when `Transport=Grpc` |
| Tests run locally | Progress unit 446 passed · Content unit 453 passed · Aspire model 28 passed (Docker-trait tests not run) |
| Migrations / DB diagram | None in this PR |

## Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.Infrastructure/ContentClient/ContentSenseGrpcClient.cs:59` | When the caller's request is aborted, `Grpc.Net.Client` throws `RpcException(Cancelled)`, not `OperationCanceledException`. The REST client throws `TaskCanceledException`. Behaviour differs per transport for aborted requests (both end as 500 today; no special handling in middleware). | Set `ThrowOperationCanceledOnCancellation = true` via `.ConfigureChannel(...)` on the `AddGrpcClient` call (`ServiceCollectionExtensions.cs:92`). |
| 2 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Api/Extensions/WebApplicationExtensions.cs:28` | All routes are served on both Kestrel endpoints. REST controllers also answer on the internal h2c port 8081; gRPC is also mapped on 8080 (fails there, HTTP/1.1). Same auth applies, so no bypass. | Optional: `MapGrpcService<...>().RequireHost("*:8081")`-style host filter, or document it. |
| 3 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Api/Grpc/RpcStatusMapper.cs:18` | `ErrorType.Conflict` maps to `Internal`; REST maps it to 409. Not reachable by this query today. | Map `Conflict` → `FailedPrecondition` (or `AlreadyExists`) when a second RPC needs it. |
| 4 | nit | `docs/features/WB-68_grpc-for-internal-service-calls.md:24` | "authorization stays on public REST endpoints" is no longer accurate: the gRPC method enforces the same `[Authorize]` + `ChildHasSupporter`. | Tester: reword when the living spec is finalised at merge. |
| 5 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.IntegrationTests/ProgressGrpcApiFactory.cs:54` | `CreateHost` clears a process-wide env var while building the host. Safe now (one collection, not parallel), but a new collection building a Progress host in parallel would race. | Keep both factories in `ProgressApiCollection` (as now); add a comment warning against a second collection. |

## Plan conformance

| Area | Status |
| --- | --- |
| Shared package, Content, Progress, orchestration, docs, tests | All `[x]` tasks are in the diff |
| Aspire deviation | No explicit `grpc` endpoint in the AppHost: Aspire models it from Content's `Kestrel:Endpoints`. Allowed by plan Open question 4; model test asserts `grpc` 5091 / `http2` and REST 5081 |
| ADR 0008 | Added; status set to `accepted` (Sam approved) in this round's commit |
| `WordBuddy/CLAUDE.md` | Proposed text in the PR body; not applied (ask-gated). Not a finding |
| Tester tasks (3 unchecked) | Owned by `/test` by design |
| Out of scope | Nothing found outside the plan |
