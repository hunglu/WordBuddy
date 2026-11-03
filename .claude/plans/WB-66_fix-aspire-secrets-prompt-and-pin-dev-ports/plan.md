# Plan: WB-66 Fix Aspire secrets prompt and pin dev ports

## Summary

Finish the draft commit `4f3129b` on a proper `feature/<slug>` branch. The AppHost then starts in
`Development`, reads user-secrets, takes non-secret defaults from `appsettings.json`, and binds
fixed ports. Add guard tests and close the remaining README gaps.

## Affected services / areas

| Area | Change |
| --- | --- |
| `WordBuddy/aspire/WordBuddy.AppHost` | `launchSettings.json`, `appsettings.json`, `Program.cs` (SQL port, parameter defaults) |
| `WordBuddy/aspire/WordBuddy.AppHost.Tests` | new launch/config guard tests, parameter default test, integration SQL port fix |
| `WordBuddy/aspire/aspire.config.json` | keep (written by the Aspire CLI; holds the AppHost path only) |
| `WordBuddy/README.md` | "Aspire (local dev)" section |
| Services, UI, compose, Makefile, k8s | no change |

Child vs adult: no difference. This is developer tooling with no learner-facing behaviour.

## Branch and PR decision

Decision: **move the draft to `feature/WB-66_fix-aspire-secrets-prompt-and-pin-dev-ports`**. Do not
reuse `feature/aspire-dev-config-fixes`.

```text
4f3129b (feature/aspire-dev-config-fixes, PR #65)
   └─ git switch -c feature/WB-66_fix-aspire-secrets-prompt-and-pin-dev-ports 4f3129b
        └─ /code commit (remaining tasks) → push → new PR "Closes #66"
PR #65 → closed with comment "Superseded by #<new PR>" (no merge)
```

| Why | Detail |
| --- | --- |
| Workflow | `/review` and `/test` find the PR by `feature/<slug>` |
| History | branch from the commit; no rebase, no force-push |
| Clean diff | the new PR shows the draft plus the `/code` delta |

Precondition for `/code`: `git log --oneline main..4f3129b` must list only commits that belong to
this change. If it lists other unmerged commits (for example `e07b01b`), create the branch from
`main` and cherry-pick `4f3129b` instead.

The uncommitted `/propose` bookkeeping (plan folder, "Pending changes" line in the WB-61 spec)
follows the branch switch. It goes into the `/code` bookkeeping commit.

Push, PR creation and closing PR #65 are ask-gated. Deleting the old branch is left to Sam.

## Backend approach

### What the draft already contains (keep)

| File | Content |
| --- | --- |
| `Properties/launchSettings.json` | one profile `http`; `ASPNETCORE_ENVIRONMENT` + `DOTNET_ENVIRONMENT` = `Development`; dashboard `15180`, OTLP `19180`, resource service `20180`; unsecured transport for local http |
| `appsettings.json` | `Logging`; `Parameters` defaults for `jwt-issuer`, `jwt-expiry-minutes`, `rabbitmq-user` (non-secret only) |
| `Program.cs` | `SqlPort = 14330`, `AddSqlServer("sql", sqlPassword, port: SqlPort)` |
| `AppHostModelTests` | `AppHost_Build_InfrastructurePortsArePinned` (SQL 14330, RabbitMQ 5672 / 15672) |

### Changes still needed

1. **One source for non-secret defaults.** `AddParameterWithDefault` duplicates the
   `appsettings.json` values. Replace the three calls with `builder.AddParameter("<name>")`, which
   reads `Parameters:<name>` from configuration, and delete the helper. User-secrets still override,
   because they load after `appsettings.json`.
   - Fallback: if the new default-resolution test shows the testing builder does not load
     `appsettings.json`, keep the helper and record this in the commit message.
2. **Comment update** in `ConfigureModel`: secrets in user-secrets, non-secret defaults in
   `appsettings.json`.
3. **Integration tests must not need 14330.** Aspire's persistent `sql` container keeps running
   after `aspire run` stops and holds 14330. `AppHostRunTests.UseThrowawayContainers` should clear
   the `sql` `tcp` endpoint host port (`Port = null` → dynamic). The APIs get the SQL connection
   string by reference, so they do not depend on the port. RabbitMQ stays pinned to 5672
   (the services have no port setting).

### Tests

| Test (class) | Asserts |
| --- | --- |
| `AppHostConfigFilesTests.LaunchSettings_AllProfiles_UseDevelopmentEnvironment` | every profile sets `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` to `Development` |
| `AppHostConfigFilesTests.LaunchSettings_AllProfiles_UsePinnedDashboardPorts` | `applicationUrl` port 15180, OTLP 19180, resource service 20180 |
| `AppHostConfigFilesTests.AppHostProject_UserSecretsId_IsSet` | csproj has a non-empty `UserSecretsId` (needed to load user-secrets) |
| `AppHostConfigFilesTests.ConfigFiles_SecretParameters_AreAbsent` | `appsettings.json` and `launchSettings.json` hold no `sql-password`, `jwt-secret`, `rabbitmq-password`, `claude-api-key` |
| `AppHostModelTests.AppHost_Build_NonSecretParameters_ResolveDefaults` | `jwt-issuer` = `WordBuddy`, `jwt-expiry-minutes` = `60`, `rabbitmq-user` = `wordbuddy` with no extra args. Secret values are never resolved or asserted |
| `AppHostModelTests.AppHost_Build_InfrastructurePortsArePinned` | already in the draft; keep |
| `AppHostRunTests` (Integration) | still pass while another container holds 14330 |

The file tests read the files from `AppHostFixture.FindBackendRoot()` (same approach as
`ArchitectureGuardTests`) and parse JSON with `System.Text.Json`.

A unit test cannot prove that `aspire run` picks the profile. The manual check below covers that.

### Manual verification (Sam, after `/code`)

```text
reset old sql / rabbitmq volumes (manual, out of scope)
→ aspire run (user-secrets set)
→ expect: no parameter prompt, dashboard on http://localhost:15180, SQL on 14330, all APIs healthy
```

This also confirms the root-cause hypothesis (missing launch profile → `Production`).

## Frontend approach

None. The UI keeps port 3000 and its existing Vite proxy.

## Data / migration notes

None. No EF Core migration. Existing local Aspire volumes with old passwords must be reset by hand
(out of scope, documented in the README).

## Docs

| Doc | Owner | Change |
| --- | --- | --- |
| `WordBuddy/README.md` | `/code` | port table adds OTLP 19180 and resource service 20180; optional user-secrets block says defaults come from `appsettings.json`; integration test note reflects the dynamic test SQL port |
| `docs/features/WB-61_...md` | `/test` | SQL row: `dynamic host port` → `14330`; add dashboard row `15180`; rule: AppHost runs in `Development` via `launchSettings.json`; rule: password change needs a volume reset; move WB-66 to change history |
| ADR 0007 | — | no change; it states no ports, and its "Secrets" row still holds |

## Open questions

1. Branch: plan moves the draft to `feature/<slug>` and closes PR #65. Sam can choose to reuse
   PR #65 instead; then `/review` and `/test` must be pointed at `feature/aspire-dev-config-fixes`.
2. `AddParameterWithDefault` removal depends on the testing builder loading `appsettings.json`.
   Fallback is defined above.
3. The `Production` root cause is not yet confirmed by a live run. The manual check above confirms
   it.
4. Out of scope ideas (not planned): a `make aspire-reset` target for volumes (Makefile is excluded
   by the proposal); a pre-run check that required user-secrets are set.
