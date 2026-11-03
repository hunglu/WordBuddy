# Review: Fix Aspire dashboard traces and accept ADR 0007

PR: #64 · Round 1 · Reviewed commit: e0766292285cd31f0bf680c0a035894b3f6a436f · 2026-11-03T11:05:13+07:00

## Verdict
Approve — the AppHost fix matches the plan, stays inside the scope guard, and is covered by model and integration tests. Only nits.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `WordBuddy/aspire/WordBuddy.AppHost/Program.cs:151`, `:157` | `TracesUrl.GetValueAsync` returns `value?.TrimEnd('/') + Path`. When the inner provider resolves to `null`, the result is `"/v1/traces"`, not `null`. The exporter then gets a relative URI and `new Uri(...)` throws at service start. Unlikely: Aspire always resolves the dashboard URL. | `return value is null ? null : value.TrimEnd('/') + Path;` |
| 2 | nit | `WordBuddy/aspire/WordBuddy.AppHost/Program.cs:138` | The fallback `_ => endpoint.ToString()!...` turns an unknown value type into a string. If a future Aspire version injects a type that is not `IValueProvider` + `IManifestExpressionProvider`, `ToString()` may return a type name, and the API gets a broken endpoint. | Return `endpoint` unchanged (as for gRPC) in the fallback arm, or fail fast with a clear message. |
| 3 | nit | `WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostModelTests.cs:14` | The comment says keys "mirror docker-compose.yml". `OpenTelemetry__OtlpEndpoint` is not in `docker-compose.yml`, so the comment is now partly wrong. | Add: "plus `OpenTelemetry__OtlpEndpoint` (Aspire only, WB-63)". |
| 4 | nit | `WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostRunTests.cs:108` | `GetFreePort` releases the port before Kestrel binds it. Another process can take it in between, so the test can fail with a bind error. Rare on a dev machine. | Accept as is, or let Kestrel bind port `0` and read the bound address from `IServerAddressesFeature`. |

Checked, no finding:

- Callback order: Aspire's OTLP callback is added inside `AddProject`; `WithDashboardOtlpEndpoint()` is added after it, so it sees the injected value. The gRPC model test proves it for all 5 APIs.
- Override: the APIs run with `ASPNETCORE_ENVIRONMENT=Development`, and each `appsettings.Development.json` sets `OpenTelemetry:OtlpEndpoint=http://localhost:4317`. The integration test therefore exercises the real override case.
- Auth headers and protocol still come from `OTEL_EXPORTER_OTLP_*` env vars; only `Endpoint` is set in code (`OpenTelemetryExtensions.cs`).
- No secrets, no logging of values, no change to authorization. Child vs adult: no difference (developer tooling).
- Independence model: no new `ProjectReference`. The test project only adds `FrameworkReference Microsoft.AspNetCore.App`.
- No EF migration, so no database diagram check.

## Plan conformance

| Area | Result |
| --- | --- |
| AppHost (`WithDashboardOtlpEndpoint`, call in `AddApi`) | Done. Handles the `HostUrl` provider type found in Aspire 13.6.4 |
| Docs (ADR 0007 status, Telemetry row, Consequences; README) | Done. No "not verified" / "may show" text left |
| Tests (fixture, `ExpectedKeys`, gRPC / HTTP string / HTTP provider / no endpoint, integration) | Done. Deviations are documented in `tasks.md` and test XML docs: the testing builder cannot select HTTP, so the HTTP cases switch the protocol in a callback, and the integration receiver is gRPC |
| `/test` tasks (4) | Open by design (owned by `/test`) |
| Scope guard | No path under `src/`, `WordBuddy.UI/`, `docker-compose.yml`, `Makefile`, `k8s/` |
| Out of scope | None. The `Pending changes` line in the WB-61 living spec comes from `/propose` |
