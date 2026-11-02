# Review: Adopt .NET Aspire for local orchestration

PR: #62 · Round 2 · Reviewed commit: 2100d28 · 2026-11-02T22:58:28+07:00

## Verdict

Approve — all 5 round-1 findings are fixed in 2100d28, with no regressions.

## Round-1 findings

| # | Severity | Status | Evidence |
|---|---|---|---|
| 1 | major | Fixed | `Program.cs:53-55`: `rabbitmq` drops `ContainerLifetime.Persistent` → session lifetime frees 5672/15672 on stop. Data volume kept. `sql` stays persistent; its host port is dynamic, so it does not block `make up`. README:79-81 explains this. |
| 2 | nit | Fixed | `AppHostModelTests.cs:76`: `ASPNETCORE_ENVIRONMENT` now comes from `env`, not appended. Removing it from `AddApi` would now fail the test. |
| 3 | nit | Fixed | README:135-137: integration test needs `make down` and no `aspire run`. |
| 4 | nit | Fixed | `ArchitectureGuardTests.cs:32-36`: checks only `ProjectReference/@Include`. Service csprojs are SDK-style (no MSBuild xmlns), so `Descendants("ProjectReference")` matches. |
| 5 | nit | Fixed | `Program.cs:17-22`: `async Task Main` + `await RunAsync()`. |

## Findings

None. No new blockers, majors or nits in the fix diff.

Checks run:

| Check | Result |
|---|---|
| `dotnet test aspire/WordBuddy.Aspire.slnx --filter "Category!=Integration"` | 11 / 11 passed |
| `AppHostRunTests` lifetime override (line 46) | Still valid; only `sql` carries the annotation now |
| Fix diff touches `src/`, `WordBuddy.UI/`, compose, `Makefile`, `k8s/` | No |
| Secrets / PII | None added |

Not run: `Category=Integration` (needs the full stack) → `/test`.

## Plan conformance

```
tasks.md: 29 / 31 done (unchanged)
  open: dashboard trace check  → deferred to /test (Sam)
  open: ADR 0007 accepted      → merge-time task (Sam)
```

Fix diff stays inside the round-1 findings. No out-of-scope change. No EF migrations.

## Previous rounds

### Round 1

PR: #62 · Round 1 · Reviewed commit: 1a51a4c · 2026-11-02T22:41:50+07:00

#### Verdict

Changes requested — one major: the persistent RabbitMQ container keeps ports 5672/15672 after `aspire run` stops, so `make up` then fails.

#### Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `WordBuddy/aspire/WordBuddy.AppHost/Program.cs:52-55`, `WordBuddy/README.md:79` | RabbitMQ is pinned to host ports 5672/15672 and has `ContainerLifetime.Persistent`. A persistent container keeps running after the AppHost stops. Scenario: developer runs `aspire run`, stops it, then runs `make up` → compose `rabbitmq` fails with "port is already allocated" on 5672. The README only says "do not run together", which reads as "stop Aspire first". This breaks the success criterion "compose still works unchanged" in daily use. | Either drop `WithLifetime(ContainerLifetime.Persistent)` on `rabbitmq` (session lifetime, data volume still keeps the data), or add a README step: "Before `make up`, stop the Aspire containers (`docker ps --filter name=rabbitmq` → `docker stop <id>`)". The same note for the persistent `sql` container is useful, even though its host port is dynamic. |
| 2 | nit | `WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostModelTests.cs:76` | The test always appends `ASPNETCORE_ENVIRONMENT` to the actual keys. The assertion for that key is therefore vacuous: removing `.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")` from `AddApi` would not fail the test, yet startup migrations depend on it. | Add the key only if `env.Contains("ASPNETCORE_ENVIRONMENT")`, or assert it separately. |
| 3 | nit | `WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostRunTests.cs:22`, `WordBuddy/README.md:133` | With `RandomizePorts=false`, the integration test binds 5080-5084, 3000, 5672 and 15672. Running it while `aspire run` or `make up` is up fails with port conflicts. | Add "stop compose / Aspire first" to the README Tests bullet. |
| 4 | nit | `WordBuddy/aspire/WordBuddy.AppHost.Tests/ArchitectureGuardTests.cs:33` | The guard matches any `aspire` substring in a service csproj. A future Aspire client package (for example `Aspire.Microsoft.EntityFrameworkCore.SqlServer`) would fail it, although ADR 0007 forbids only ServiceDefaults and AppHost references. | Match `ProjectReference` paths containing `aspire` or `ServiceDefaults` instead of the whole text, or keep it strict and say so in ADR 0007. |
| 5 | nit | `WordBuddy/aspire/WordBuddy.AppHost/Program.cs:21` | `builder.Build().Run()` blocks synchronously; the backend convention is async everywhere. Harmless at the entry point. | `public static async Task Main(...)` with `await builder.Build().RunAsync();`. |

No blockers. No secrets in code or config: parameters are `secret: true` and live in user-secrets; test values are clearly `test-only-*`. No PII in logs.

#### Plan conformance

```
tasks.md: 29 / 31 done
  open: dashboard trace check  → deferred to /test (Sam, 2026-11-02)
  open: ADR 0007 accepted      → merge-time task (Sam, 2026-11-02)
```

| Check | Result |
|---|---|
| AppHost wiring (resources, ports 5080-5084 + 3000, DB names) | Matches plan |
| Env keys per API | Match `docker-compose.yml` one to one (verified against compose) |
| No `ProjectReference` from AppHost to services | Yes; guard test present |
| No diff under `src/`, `WordBuddy.UI/`, compose, `Makefile`, `k8s/` | Verified empty |
| `aspire/nuget.config` with `<clear/>` + nuget.org | Yes |
| ADR 0007 migration decision, README section, living spec | Present; spec says "traces + console logs" |
| `WordBuddy/CLAUDE.md` | Not edited (proposal only, as planned) |
| Child vs adult | No learner-facing change. Seeded child login not checked because no seeded child account exists |
| Out of scope | One extra test, `AppHost_Build_ClaudeApiKeySet_ContentHasAutofillKey`. Useful; keep |
| EF migrations | None, so no database-diagram update needed |
