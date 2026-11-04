# Tasks: gRPC for internal service calls

## Shared package

- [x] Create `src/Shared/WordBuddy.Shared.Protos` (net10.0, `PackageId`, `Version 1.0.0`, XML docs) with `Grpc.Tools` (PrivateAssets all), `Google.Protobuf`, `Grpc.Core.Api` â€” `dotnet build` succeeds, no ASP.NET dependency. — WordBuddy.Shared.Protos.csproj (Grpc.Core.Api 2.71.0, Google.Protobuf 3.31.1, Grpc.Tools 2.72.0)
- [x] Add `Protos/content/v1/content_senses.proto` exactly as in `plan.md` (`GrpcServices="Both"`; `.proto` also packed as content) â€” package contains `ContentSenses.ContentSensesBase` and `ContentSenses.ContentSensesClient`. — Protos/content/v1/content_senses.proto
- [x] Add the project to `WordBuddy.Shared.slnx` and to the `Makefile` pack / `publish-shared` list â€” `dotnet pack ... -o local-nuget-feed` produces `WordBuddy.Shared.Protos.1.0.0.nupkg`. — WordBuddy.Shared.slnx, Makefile

## Backend â€” Content

- [x] Add `PackageReference`s to `WordBuddy.Content.Api`: `WordBuddy.Shared.Protos` 1.0.0, `Grpc.AspNetCore`, `Grpc.AspNetCore.HealthChecks` â€” no `ProjectReference` outside Content. — WordBuddy.Content.Api.csproj
- [x] Add `Api/Grpc/ContentSensesGrpcService.cs` with class-level `[Authorize]` + `[Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]`; reuse `IQueryHandler<GetSensesByIdsQuery, IReadOnlyList<SenseReviewDto>>`; user id and `AgeGroup` from the JWT â€” returns the same senses as REST `GET /api/vocabulary/senses` for the same caller. — Api/Grpc/ContentSensesGrpcService.cs
- [x] Add a small `Result` â†’ `RpcException` mapper (`Validation` â†’ `InvalidArgument`, `NotFound` â†’ `NotFound`, else `Internal`); bad GUID â†’ `InvalidArgument` â€” no exception escapes for expected failures. — Api/Grpc/RpcStatusMapper.cs
- [x] Register `AddGrpc()` + `AddGrpcHealthChecks()` (liveness semantics) and map `MapGrpcService<ContentSensesGrpcService>()` + `MapGrpcHealthChecksService()` â€” all existing REST routes unchanged. — ServiceCollectionExtensions.cs, WebApplicationExtensions.cs (AddGrpc uses IgnoreUnknownServices, else REST 405 became 404)
- [x] Add `Kestrel:Endpoints` (`Http` 5081, `Grpc` 5091 `Http2`) to `appsettings.json` â€” `dotnet run` serves REST on 5081 and gRPC on 5091. — Content appsettings.json
- [x] Update Content `Dockerfile`: Kestrel env for 8080 (REST) and 8081 (gRPC, `Http2`), `EXPOSE 8080 8081` â€” container serves both ports. — Content/Dockerfile

## Backend â€” Progress

- [x] Add `PackageReference`s to `WordBuddy.Progress.Infrastructure`: `WordBuddy.Shared.Protos` 1.0.0, `Grpc.Net.ClientFactory`, `Grpc.HealthCheck` â€” no `ProjectReference` outside Progress. — Progress.Infrastructure.csproj (Grpc.Net.ClientFactory 2.76.0, Grpc.HealthCheck 2.76.0)
- [x] Extend `ContentOptions` with `Transport` (`ContentTransport` `Rest`/`Grpc`, code default `Grpc`) and `GrpcUrl` (default `http://localhost:5091`); add both to `appsettings.json` with `Transport: Grpc` â€” with no config set, Progress uses gRPC. — ContentClient/ContentOptions.cs (+ FromConfiguration, unknown value fails fast), Progress.Api/appsettings.json
- [x] Extract session cache get/set into internal `ContentClient/SessionSenseCache.cs`; `ContentSenseClient` uses it â€” key `progress:session-senses:{sessionId}` and expiry unchanged, existing tests still green. — ContentClient/SessionSenseCache.cs, ContentSenseClient.cs
- [x] Add `ContentClient/ContentSenseGrpcClient.cs : IContentSenseClient` (cache â†’ 100-id chunks â†’ gRPC â†’ `ContentSenseDto`; `RpcException`/timeouts/circuit-open â†’ `Content.Unavailable`; logs ids/counts only) â€” same `Result` contract as the REST client. — ContentClient/ContentSenseGrpcClient.cs
- [x] Add `ContentClient/ContentGrpcHealthCheck.cs` (`grpc.health.v1` `Check`, 3 s deadline) â€” `Serving` â†’ Healthy, otherwise Unhealthy. — ContentClient/ContentGrpcHealthCheck.cs
- [x] Branch `AddContentClient` on `Transport`: `Rest` (non-default, explicit opt-in) = current registration untouched; `Grpc` (default) = `AddGrpcClient<ContentSenses.ContentSensesClient>` + `ForwardBearerTokenHandler` + same standard resilience settings + `IContentSenseClient â†’ ContentSenseGrpcClient` â€” switching needs config only. — Infrastructure/Extensions/ServiceCollectionExtensions.cs (internal for tests)
- [x] In `Program.cs` register `ContentGrpcHealthCheck` (`"content-grpc"`, `Degraded`, ready tag) only when `Transport=Grpc`; keep REST `ContentHealthCheck` â€” `/health/ready` lists `content` and `content-grpc` under gRPC. — Progress.Api/Program.cs

## Orchestration

- [x] Aspire `Program.cs`: Content gets endpoint `grpc` (port 5091, `http2`) and `Kestrel__Endpoints__Http__Url` / `Kestrel__Endpoints__Grpc__Url` / `Kestrel__Endpoints__Grpc__Protocols`; Progress gets `Services__Content__GrpcUrl` + `Services__Content__Transport=Grpc` â€” `aspire run` serves REST 5081 and gRPC 5091 (fallback: `isProxied: false`, see plan Open question 4). — AppHost/Program.cs: only Progress env keys. Aspire already reads Content Kestrel:Endpoints and injects Kestrel__Endpoints__*__Url, so no extra endpoint/env code (deviation, see report)
- [x] `docker-compose.yml`: Content `expose: ["8081"]` (no host port); Progress `Services__Content__GrpcUrl: http://content-api:8081`, `Services__Content__Transport: Grpc` â€” `make up` review session works over gRPC. — docker-compose.yml
- [x] `k8s/content-deployment.yaml` (`containerPort 8081 name grpc`) and `k8s/content-service.yaml` (port `grpc` 8081, `appProtocol: kubernetes.io/h2c`); `k8s/progress-deployment.yaml` gets the two env vars; `ingress.yaml` untouched â€” gRPC reachable only inside the cluster. — k8s/content-deployment.yaml, content-service.yaml, progress-deployment.yaml

## Docs

- [x] Write `docs/adr/0008-grpc-internal-service-calls.md` from `_template.md` (decision table from `plan.md`) â€” status `accepted` pending Sam. — docs/adr/0008-grpc-internal-service-calls.md
- [x] Update `src/Services/Content/README.md` (ports 5081/5091, 8080/8081, gRPC service) and `src/Services/Progress/README.md` (`Transport`, `GrpcUrl`, health checks) â€” config keys documented. — Content/README.md, Progress/README.md
- [x] Propose (do not apply) the `WordBuddy/CLAUDE.md` edits to Sam: shared package list + `WordBuddy.Shared.Protos`, Content gRPC port in the Docker/k8s table â€” exact text in the PR description. — text is in the final report (CLAUDE.md is ask-gated, not edited)

## Tests

- [x] Content unit: `RpcStatusMapper` â€” `Validation` â†’ `InvalidArgument`, `NotFound` â†’ `NotFound`, other â†’ `Internal` (one `[Fact]` each). — UnitTests/Grpc/RpcStatusMapperTests.cs
- [x] Content unit: `ContentSensesGrpcService_GetSensesByIds_*` with mocked handler â€” maps DTO fields incl. nulls; bad GUID â†’ `InvalidArgument`; passes JWT user id and `AgeGroup` to the query. — UnitTests/Grpc/ContentSensesGrpcServiceTests.cs, FakeServerCallContext.cs, UnitTests csproj
- [x] Content integration (`WebApplicationFactory` + `GrpcChannel` over `Server.CreateHandler()`, real SQL Server): Adult gets senses; Child with supporter gets only child-visible senses; Child without supporter â†’ `PermissionDenied`; no token â†’ `Unauthenticated`; 0 or 101 ids â†’ `InvalidArgument`; hidden/unknown ids omitted â€” same results as the REST test for the same data. — IntegrationTests/ContentSensesGrpcTests.cs, csproj
- [x] Content integration: `grpc.health.v1` `Check` returns `Serving`; REST `/health` and `/api/vocabulary/senses` still pass. — same file (health + REST checks)
- [x] Progress unit: `ContentSenseGrpcClient_GetSessionSensesAsync_*` with mocked `ContentSensesClient` â€” maps reply; chunks 150 ids into 2 calls; cache hit makes no call; `RpcException(Unavailable)` â†’ `Content.Unavailable`; caller cancellation propagates. — UnitTests/Infrastructure/ContentSenseGrpcClientTests.cs
- [x] Progress unit: `SessionSenseCache` â€” same key format and absolute expiry; past expiry is not cached. — UnitTests/Infrastructure/SessionSenseCacheTests.cs
- [x] Progress unit: `ContentGrpcHealthCheck_CheckHealthAsync_*` â€” `Serving` â†’ Healthy; `NotServing` / `RpcException` â†’ Unhealthy. — UnitTests/Infrastructure/ContentGrpcHealthCheckTests.cs
- [x] Progress unit: `AddContentClient` â€” no `Transport` set resolves `ContentSenseGrpcClient` (default); `Transport=Grpc` resolves `ContentSenseGrpcClient`; `Transport=Rest` resolves `ContentSenseClient`. — UnitTests/Infrastructure/AddContentClientTests.cs
- [x] Progress integration (REST selected): `ProgressApiFactory` pins `Services__Content__Transport=Rest`; existing tests stay green on the stub HTTP handler â€” proves REST still works when selected. — ProgressApiFactory.cs (pins Rest), ProgressApiCollection.cs; ContentTransportTests.cs asserts REST client + no content-grpc
- [x] Progress integration (default gRPC): new factory variant with no `Transport` override and a fake `ContentSensesClient` â€” start review session succeeds over gRPC, `/health/ready` reports `content-grpc`. — ProgressGrpcApiFactory.cs, ContentTransportTests.cs
- [x] Aspire `AppHostModelTests`: add new Content and Progress env keys to `ExpectedKeys`; assert Content `grpc` endpoint port 5091 â€” `dotnet test aspire/WordBuddy.Aspire.slnx` green. — AppHostModelTests.cs (28 non-Docker tests green; Integration-trait Docker tests not run)
- [ ] Tester: existing `e2e/api` vocabulary review scenarios green with the default (gRPC) in compose â€” no public REST change.
- [ ] Tester: rerun the same scenarios once with `Services__Content__Transport=Rest` on Progress â€” REST alternative still works when selected.
- [ ] Tester: manual check in the Aspire dashboard â€” one trace shows Progress â†’ Content gRPC span (`rpc.system=grpc`); result noted in `test-report.md`.
