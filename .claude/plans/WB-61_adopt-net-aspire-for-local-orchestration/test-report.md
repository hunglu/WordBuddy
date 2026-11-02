# Test report: Adopt .NET Aspire for local orchestration

Developer tooling only. No learner-facing change, so no child vs adult cases.

## Backend unit/integration

| Suite | Result |
|---|---|
| `aspire/WordBuddy.Aspire.slnx` build | 0 errors (Pkcs NU1902/1903 and ASPIRE010 warnings, already known) |
| AppHost.Tests, `Category!=Integration` | 11 / 11 passed |
| AppHost.Tests, `Category=Integration` (`AppHost_Run_AllApisReachHealthy`) | 1 / 1 passed (2 m 48 s); ports 5080-5084, 3000, 5672, 15672 were free before the run |
| Identity unit / integration | 238 / 238, 51 / 51 passed |
| Content unit / integration | 445 / 445, 216 / 216 passed |
| Progress unit / integration | 425 / 425, 96 / 96 passed |
| Quiz unit / integration | 2 / 2 passed; integration project has no tests (existing scaffold) |
| Notification unit / integration | No tests in either project (existing scaffold) |

Regression: `git diff origin/main...HEAD` lists no path under `src/`, `WordBuddy.UI/`,
`docker-compose.yml`, `Makefile` or `k8s/`.

## E2E API

Not applicable — no API surface changed, no service code changed. Not run, and not counted as a pass.

## E2E UI

Not applicable — no user-visible UI change. Not run, and not counted as a pass.

## Dashboard trace check (deferred task)

Started the AppHost with throwaway parameters, anonymous dashboard, opened it with Playwright.

| Run | Result |
|---|---|
| As shipped | All 5 APIs healthy on 5080-5084, UI on 3000. Traces page: **No traces found** |
| With env `OpenTelemetry__OtlpEndpoint=http://localhost:18889` | Traces appear |

- Cause: each service's `appsettings.Development.json` sets `OpenTelemetry:OtlpEndpoint` to
  `http://localhost:4317`. `AddWordBuddyOpenTelemetry` prefers that key over the
  `OTEL_EXPORTER_OTLP_ENDPOINT` that Aspire injects, so traces go to :4317, not the dashboard.
  (These files are not in git; they exist on Sam's machine.)
- Mapping: traces are named `WordBuddy.Identity`, `WordBuddy.Content`, `WordBuddy.Progress`, … (code
  sets `service.name`). They are **not** grouped under the `identity` / `content` resources.
- README (lines 128-131) says "not verified" and "may show as a separate entry". The mapping is now
  confirmed. The README does not mention the no-traces case. README is outside the allowlist, so it is
  a follow-up.

## Cleanup

All AppHost processes stopped, the `sql` and `rabbitmq` containers removed, ports 5080-5084, 3000,
5672, 15672, 18888, 18889 free. The two unrelated uncommitted csproj edits were not touched.

## Failures

None in the test suites. One product finding (not a test failure, does not block the merge): the
dashboard shows no traces with the shipped AppHost on a machine that has the local
`appsettings.Development.json` files. See the trace check above.

## Merge guard

1. `review.md`: Approve, reviewed commit 2100d28 is an ancestor of HEAD.
2. `git merge-tree` against `origin/main`: no conflict. Changed since review:
   - `.claude/plans/WB-61_adopt-net-aspire-for-local-orchestration/proposal.md`
   - `.claude/plans/WB-61_adopt-net-aspire-for-local-orchestration/review.md`
   Both are on the allowlist.
3. Every applicable suite ran with zero failures.

## Open items for Sam

- ADR 0007 `Status: accepted`: `docs/adr/` is outside the tester allowlist, so it was not edited. Still `proposed`.
- tasks.md open items: dashboard check (done here), ADR status (above).

## Verdict

Approved — merged into main
