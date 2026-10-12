# Tasks: Configurable messaging bus settings

## Backend — Shared.Infrastructure

- [x] Add `Messaging/MessagingOptions.cs` (+ `RabbitMqOptions`, `OutboxOptions`, `RetryOptions`) with XML docs and defaults from plan — defaults equal current hard-coded values. — Shared.Infrastructure/Messaging/MessagingOptions.cs
- [x] Add `Messaging/MessagingOptionsValidator.cs` (`IValidateOptions`, uses `IHostEnvironment`) — all rules in plan table; messages name the config key. — Messaging/MessagingOptionsValidator.cs
- [x] Refactor `MessagingExtensions.AddWordBuddyMessaging` to bind + `ValidateOnStart`, and read every value from options — no `TimeSpan.From*` literal or `"guest"` left outside the options/Development fallback. — Messaging/MessagingExtensions.cs
- [x] Apply heartbeat, connection timeout, outbox `QueryDelay` and `DuplicateDetectionWindow` — values reach MassTransit config. — Messaging/MessagingExtensions.cs (heartbeat, timeout, QueryDelay, DuplicateDetectionWindow)
- [x] Bump `<Version>` to 1.2.0 and `dotnet pack` to `local-nuget-feed` — package 1.2.0 exists locally. — Shared.Infrastructure csproj; 1.2.0 packed to local-nuget-feed

## Backend — services (Identity, Content, Progress)

- [x] Bump `WordBuddy.Shared.Infrastructure` to 1.2.0 in Api + Infrastructure csproj of each — all 3 build. — 6 service csproj files
- [x] Add full `Messaging` section (Outbox, Retry, RabbitMq timeouts) to each `appsettings.json`; no password value — section matches plan shape. — 3 appsettings.json
- [x] Add commented/example override env vars in `docker-compose.yml` and `k8s/configmap.yaml` (e.g. `Messaging__Outbox__QueryDelayMs`) — `docker compose config` valid. — docker-compose.yml, k8s/configmap.yaml (comments; compose config valid)

## Docs

- [x] Update `docs/features/messaging.md` and Content/Progress/Identity READMEs with the key table and creds rule — every key listed with default. — docs/features/messaging.md, 3 service READMEs

## Tests

- [x] Unit: `MessagingOptions` binding from in-memory config incl. `IntervalsMs` array and env-style keys — values bound. — Content.UnitTests/Infrastructure/MessagingOptionsTests.cs
- [x] Unit: defaults when section is missing — equal to plan defaults. — same file
- [x] Unit: validator rejects negative/zero delay, empty retry list, negative interval, empty host — failure names the key. — same file
- [x] Unit: validator — missing creds fail in Production, pass in Development, pass with InMemory transport. — same file
- [x] Integration (existing `MessagingRegistrationTests` in Content): invalid config throws `OptionsValidationException` on resolve/start — test green. — Content.IntegrationTests/Messaging/MessagingRegistrationTests.cs
- [x] Run `dotnet test` for Identity, Content, Progress slnx — all existing suites green. — Identity 144, Content 342, Progress 479 tests green
