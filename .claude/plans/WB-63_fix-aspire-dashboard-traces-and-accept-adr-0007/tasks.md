# Tasks: Fix Aspire dashboard traces and accept ADR 0007

Scope guard: no change under `src/`, `WordBuddy.UI/`, `docker-compose.yml`, `Makefile`, `k8s/`.
Do not stage the unrelated local edits in `Content.Infrastructure.csproj` and `e2e/api/...Tests.csproj`.

## Backend (AppHost)

- [x] Add `WithDashboardOtlpEndpoint()` in `WordBuddy/aspire/WordBuddy.AppHost/Program.cs` — an env callback copies `OTEL_EXPORTER_OTLP_ENDPOINT` to `OpenTelemetry__OtlpEndpoint`; appends `/v1/traces` when `OTEL_EXPORTER_OTLP_PROTOCOL` is `http/protobuf`; does nothing when the endpoint is absent; XML doc explains the `appsettings.Development.json` override — WordBuddy/aspire/WordBuddy.AppHost/Program.cs (Aspire 13.6.4 injects an internal `HostUrl` provider, not a string: wrapped in `TracesUrl` for the HTTP case)
- [x] Call it from `AddApi` after `AddProject` — all 5 APIs get the key; no other env key changes — WordBuddy/aspire/WordBuddy.AppHost/Program.cs
- [x] Build `WordBuddy/aspire/WordBuddy.Aspire.slnx` — 0 errors, no new warnings — 0 errors; 9 warnings are existing NU1902/NU1903

## Docs (coder — not on the tester allowlist)

- [x] `docs/adr/0007-aspire-local-orchestration.md`: `Status: accepted`, add `Accepted: 2026-11-xx (WB-63)` — status line reads `accepted` — docs/adr/0007-aspire-local-orchestration.md (Accepted: 2026-11-03)
- [x] ADR 0007 Telemetry row: the AppHost sets `OpenTelemetry__OtlpEndpoint` from the injected OTLP endpoint, because a local `appsettings.Development.json` key beats `OTEL_EXPORTER_OTLP_ENDPOINT` — row matches the code — docs/adr/0007-aspire-local-orchestration.md
- [x] ADR 0007 Consequences: traces appear as `WordBuddy.<Service>`, not under the Aspire resource; mapping needs a shared-helper change (separate proposal) — one bullet added — docs/adr/0007-aspire-local-orchestration.md
- [x] `WordBuddy/README.md` "Good to know → Telemetry": verified behaviour — traces reach the dashboard even with a local `OpenTelemetry:OtlpEndpoint`; Traces list `WordBuddy.Identity` ... `WordBuddy.Notification`; console logs stay under `identity` ... — no "not verified" / "may show" text left — WordBuddy/README.md

## Tests

- [x] `AppHostFixture.BaseArgs`: add throwaway `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:18889` — existing tests still pass — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostFixture.cs (`GrpcOtlpArg`; passed in `CreateAsync`)
- [x] `AppHostFixture`: add `GetEnvironmentAsync(IResource)` returning the key/value map (values unresolved) — used by the value tests below — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostFixture.cs (plus optional after-each-callback hook)
- [x] `ExpectedKeys` theory: add `OpenTelemetry__OtlpEndpoint` to all 5 APIs — `AppHost_Build_ApiEnvironmentMatchesComposeKeys` passes for every API — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostModelTests.cs
- [x] `AppHost_Build_GrpcDashboard_OtlpEndpointMatchesInjectedEndpoint` (theory over 5 APIs) — `OpenTelemetry__OtlpEndpoint` equals `OTEL_EXPORTER_OTLP_ENDPOINT` — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostModelTests.cs
- [x] `AppHost_Build_HttpDashboard_OtlpEndpointHasTracesPath` — with only the HTTP dashboard OTLP URL set, value ends in `/v1/traces` — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostModelTests.cs (renamed to `..._HttpProtobufStringEndpoint_...` + `..._HttpProtobufProviderEndpoint_...`; the testing builder cannot select HTTP, so the test switches the injected protocol in the callbacks)
- [x] `AppHost_Build_NoDashboardOtlp_NoOtlpEndpointKey` — without dashboard OTLP config, no API has `OpenTelemetry__OtlpEndpoint` (coder: confirm the testing builder injects nothing by itself; else adjust and document) — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostModelTests.cs (renamed `AppHost_Build_NoInjectedOtlpEndpoint_NoOtlpEndpointKey`; the testing builder always injects `http://localhost:4317`, so the test removes the injected value)
- [x] `AppHost_Run_TracesReachOtlpEndpoint` (`Category=Integration`) — fake OTLP/HTTP receiver on a free port; stack started with throwaway containers (reuse `UseThrowawayContainers`); each API's `/health/ready` called; all 5 `WordBuddy.<Service>` names seen in `POST /v1/traces` bodies within the timeout — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostRunTests.cs, WordBuddy.AppHost.Tests.csproj (fake OTLP/gRPC receiver, not HTTP: the testing builder cannot select HTTP; passed in 3 m 12 s)
- [x] Architecture guard tests unchanged and green — no project reference added — green, no project reference added

## Verification at `/test` (tester)

- [ ] Run AppHost.Tests `Category!=Integration` and `Category=Integration` — all green; ports 5080-5084, 3000, 5672, 15672 free before the run — OWNED BY /test (plan section; Sam, 2026-11-03)
- [ ] Manual dashboard check on a machine with local `appsettings.Development.json` (`OpenTelemetry:OtlpEndpoint=http://localhost:4317`): `aspire run`, call each API, open Traces (Playwright) — traces from all 5 APIs visible; result recorded in `test-report.md` — OWNED BY /test (plan section; Sam, 2026-11-03)
- [ ] Regression: `git diff origin/main...HEAD` lists no path under `src/`, `WordBuddy.UI/`, `docker-compose.yml`, `Makefile`, `k8s/` — OWNED BY /test (plan section; Sam, 2026-11-03)
- [ ] Living spec `docs/features/WB-61_adopt-net-aspire-for-local-orchestration.md`: replace "Known gap" with a Telemetry rule (AppHost sets `OpenTelemetry__OtlpEndpoint`; traces named `WordBuddy.<Service>`); move WB-63 from "Pending changes" to "Change history"; `last-updated-by: WB-63_...` — no "Known gap" text left — OWNED BY /test (plan section; Sam, 2026-11-03)
