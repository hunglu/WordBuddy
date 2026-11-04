# Review: gRPC for internal service calls

PR: #69 · Round 2 · Reviewed commit: 86a6f52 · 2026-11-04T09:45:47+07:00

## Verdict

Approve — no application code changed since round 1 (`fa37612`). The only non-bookkeeping change is the ADR 0008 status line, which Sam approved. Round 1 findings stand (5 nits).

## Delta since fa37612

| File | Change | Assessment |
| --- | --- | --- |
| `docs/adr/0008-grpc-internal-service-calls.md:3` | `accepted (pending Sam's confirmation in the PR)` → `accepted` | OK — Sam approved in round 1 |
| `.claude/plans/WB-68_.../review.md` | Round 1 review | Bookkeeping |
| `.claude/plans/WB-68_.../test-report.md` | `/test` run 1 | Bookkeeping |
| `.claude/plans/WB-68_.../proposal.md` | `pr: 69`, status, version | Bookkeeping |
| `WordBuddy/`, `WordBuddy.UI/`, `e2e/` | none (`git diff fa37612..86a6f52 --stat`) | Round 1 code review still valid |

This round's reviewed commit is `86a6f52`, so the `/test` merge guard (paths changed since review) now passes for the ADR edit.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.Infrastructure/ContentClient/ContentSenseGrpcClient.cs:59` | Round 1 #1, open: aborted request throws `RpcException(Cancelled)` on gRPC, `TaskCanceledException` on REST. | `ThrowOperationCanceledOnCancellation = true` via `.ConfigureChannel(...)`. |
| 2 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Api/Extensions/WebApplicationExtensions.cs:28` | Round 1 #2, open: REST and gRPC are served on both Kestrel endpoints. Same auth, no bypass. | Optional host filter, or document it. |
| 3 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Api/Grpc/RpcStatusMapper.cs:18` | Round 1 #3, open: `Conflict` → `Internal` (REST: 409). Not reachable today. | Map to `FailedPrecondition` when needed. |
| 4 | nit | `docs/features/WB-68_grpc-for-internal-service-calls.md:24` | Round 1 #4, open: says authorization stays on REST only; gRPC enforces the same policies. | Tester rewords at merge. |
| 5 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.IntegrationTests/ProgressGrpcApiFactory.cs:54` | Round 1 #5, open: process-wide env var cleared in `CreateHost`; races if a second parallel collection is added. | Add a warning comment. |

## Not code findings (environment)

| Item | Status | Owner |
| --- | --- | --- |
| `WordBuddy.Shared.Protos` 1.0.0 missing on GitHub Packages → `NU1101` in `docker compose` build | Blocks E2E API | Sam: `make publish-shared` (needs `write:packages`) |
| Aspire AppHost.Tests: 2 of 30 failed in `/test` run 1 | 30/30 pass on rerun → flaky / environment | Re-run in next `/test` |
| `proposal.md` `updated:` set to `12:00` by `/test`, later than the real clock | Corrected in this round | — |

## Plan conformance

| Area | Status |
| --- | --- |
| Code tasks | Unchanged since round 1 — all `[x]` tasks are in the diff |
| ADR 0008 | `accepted` (Sam approved) |
| `WordBuddy/CLAUDE.md` | Proposed in the PR body; not part of this PR's commits. Not a finding |
| Tester tasks | Owned by `/test` |
| Out of scope | Nothing found |

## Previous rounds

### Round 1

PR: #69 · Round 1 · Reviewed commit: fa37612 · 2026-11-04T09:09:42+07:00

#### Verdict

Approve — no blockers, no majors. The gRPC path keeps the same auth and child filter as REST, and REST still works when selected.

```text
Progress ──gRPC h2c :8081 (default)──► ContentSensesGrpcService ─┐
Progress ──REST :8080 (Transport=Rest)──► PersonalVocabularyController ─┤
                                                                       └► same GetSensesByIds handler
   both: [Authorize] + ChildHasSupporter, user id + AgeGroup from JWT only
```

#### What was verified

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

#### Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.Infrastructure/ContentClient/ContentSenseGrpcClient.cs:59` | When the caller's request is aborted, `Grpc.Net.Client` throws `RpcException(Cancelled)`, not `OperationCanceledException`. The REST client throws `TaskCanceledException`. Behaviour differs per transport for aborted requests (both end as 500 today; no special handling in middleware). | Set `ThrowOperationCanceledOnCancellation = true` via `.ConfigureChannel(...)` on the `AddGrpcClient` call (`ServiceCollectionExtensions.cs:92`). |
| 2 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Api/Extensions/WebApplicationExtensions.cs:28` | All routes are served on both Kestrel endpoints. REST controllers also answer on the internal h2c port 8081; gRPC is also mapped on 8080 (fails there, HTTP/1.1). Same auth applies, so no bypass. | Optional: `MapGrpcService<...>().RequireHost("*:8081")`-style host filter, or document it. |
| 3 | nit | `WordBuddy/src/Services/Content/WordBuddy.Content.Api/Grpc/RpcStatusMapper.cs:18` | `ErrorType.Conflict` maps to `Internal`; REST maps it to 409. Not reachable by this query today. | Map `Conflict` → `FailedPrecondition` (or `AlreadyExists`) when a second RPC needs it. |
| 4 | nit | `docs/features/WB-68_grpc-for-internal-service-calls.md:24` | "authorization stays on public REST endpoints" is no longer accurate: the gRPC method enforces the same `[Authorize]` + `ChildHasSupporter`. | Tester: reword when the living spec is finalised at merge. |
| 5 | nit | `WordBuddy/src/Services/Progress/WordBuddy.Progress.IntegrationTests/ProgressGrpcApiFactory.cs:54` | `CreateHost` clears a process-wide env var while building the host. Safe now (one collection, not parallel), but a new collection building a Progress host in parallel would race. | Keep both factories in `ProgressApiCollection` (as now); add a comment warning against a second collection. |

#### Plan conformance

| Area | Status |
| --- | --- |
| Shared package, Content, Progress, orchestration, docs, tests | All `[x]` tasks are in the diff |
| Aspire deviation | No explicit `grpc` endpoint in the AppHost: Aspire models it from Content's `Kestrel:Endpoints`. Allowed by plan Open question 4; model test asserts `grpc` 5091 / `http2` and REST 5081 |
| ADR 0008 | Added; status set to `accepted` (Sam approved) in this round's commit |
| `WordBuddy/CLAUDE.md` | Proposed text in the PR body; not applied (ask-gated). Not a finding |
| Tester tasks (3 unchecked) | Owned by `/test` by design |
| Out of scope | Nothing found outside the plan |
