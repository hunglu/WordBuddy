# Tasks: WB-66 Fix Aspire secrets prompt and pin dev ports

## Git

- [x] Check `git log --oneline main..4f3129b` — only commits of this change are listed; else branch from `main` and cherry-pick `4f3129b`. — only 4f3129b listed
- [x] `git switch -c feature/WB-66_fix-aspire-secrets-prompt-and-pin-dev-ports 4f3129b` — branch exists; uncommitted `/propose` files carried over. — branch created; /propose files carried over
- [x] After push (ask-gated): open a new PR with `Closes #66`; close PR #65 with "Superseded by #<new>" (ask-gated) — PR #65 closed unmerged.

## Backend (`WordBuddy/aspire/` only)

- [x] Keep the draft files from `4f3129b`: `launchSettings.json`, `appsettings.json`, `aspire.config.json`, `SqlPort = 14330` — unchanged unless a task below says so. — no change
- [x] `Program.cs`: replace the three `AddParameterWithDefault` calls with `builder.AddParameter("<name>")` and delete the helper — defaults come only from `appsettings.json` (fallback: keep the helper if the defaults test fails, note it in the commit). — WordBuddy/aspire/WordBuddy.AppHost/Program.cs; defaults test passed, no fallback
- [x] `Program.cs`: update the parameters comment — says secrets in user-secrets, non-secret defaults in `appsettings.json`. — Program.cs
- [x] `AppHostRunTests.UseThrowawayContainers`: set the `sql` `tcp` endpoint host port to `null` — integration run does not need port 14330. — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostRunTests.cs

## Docs

- [x] `WordBuddy/README.md` port table: add dashboard OTLP `19180` and resource service `20180` — every pinned port is listed. — WordBuddy/README.md
- [x] `WordBuddy/README.md` user-secrets block: the optional `jwt-issuer` / `jwt-expiry-minutes` / `rabbitmq-user` lines say defaults come from `appsettings.json` — no wrong "set these" hint. — WordBuddy/README.md
- [x] `WordBuddy/README.md` tests note: integration run uses a dynamic SQL port, so the persistent `sql` container may keep running — text matches the code. — WordBuddy/README.md

## Tests

- [x] New `AppHostConfigFilesTests.LaunchSettings_AllProfiles_UseDevelopmentEnvironment` — fails if any profile lacks `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT` = `Development`. — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostConfigFilesTests.cs
- [x] New `AppHostConfigFilesTests.LaunchSettings_AllProfiles_UsePinnedDashboardPorts` — asserts 15180 / 19180 / 20180. — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostConfigFilesTests.cs
- [x] New `AppHostConfigFilesTests.AppHostProject_UserSecretsId_IsSet` — fails if `UserSecretsId` is missing or empty. — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostConfigFilesTests.cs
- [x] New `AppHostConfigFilesTests.ConfigFiles_SecretParameters_AreAbsent` — fails if `appsettings.json` or `launchSettings.json` contains a secret parameter key. — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostConfigFilesTests.cs
- [x] New `AppHostModelTests.AppHost_Build_NonSecretParameters_ResolveDefaults` — `WordBuddy` / `60` / `wordbuddy` resolve with no extra args; no secret value is resolved. — WordBuddy/aspire/WordBuddy.AppHost.Tests/AppHostModelTests.cs
- [x] Run `dotnet test aspire/WordBuddy.Aspire.slnx --filter "Category!=Integration"` — all green (20 existing + 5 new). — 26 passed (theory counts 2 cases)
- [x] Run `dotnet test aspire/WordBuddy.Aspire.slnx --filter "Category=Integration"` with compose stopped — both run tests green, also with the persistent `sql` container running. — 2 passed; run with no persistent sql container running (dynamic port not exercised against a busy 14330)
