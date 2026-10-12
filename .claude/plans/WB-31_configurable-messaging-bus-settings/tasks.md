# Tasks: Configurable messaging bus settings

## Backend — Shared.Infrastructure

- [ ] Add `Messaging/MessagingOptions.cs` (+ `RabbitMqOptions`, `OutboxOptions`, `RetryOptions`) with XML docs and defaults from plan — defaults equal current hard-coded values.
- [ ] Add `Messaging/MessagingOptionsValidator.cs` (`IValidateOptions`, uses `IHostEnvironment`) — all rules in plan table; messages name the config key.
- [ ] Refactor `MessagingExtensions.AddWordBuddyMessaging` to bind + `ValidateOnStart`, and read every value from options — no `TimeSpan.From*` literal or `"guest"` left outside the options/Development fallback.
- [ ] Apply heartbeat, connection timeout, outbox `QueryDelay` and `DuplicateDetectionWindow` — values reach MassTransit config.
- [ ] Bump `<Version>` to 1.2.0 and `dotnet pack` to `local-nuget-feed` — package 1.2.0 exists locally.

## Backend — services (Identity, Content, Progress)

- [ ] Bump `WordBuddy.Shared.Infrastructure` to 1.2.0 in Api + Infrastructure csproj of each — all 3 build.
- [ ] Add full `Messaging` section (Outbox, Retry, RabbitMq timeouts) to each `appsettings.json`; no password value — section matches plan shape.
- [ ] Add commented/example override env vars in `docker-compose.yml` and `k8s/configmap.yaml` (e.g. `Messaging__Outbox__QueryDelayMs`) — `docker compose config` valid.

## Docs

- [ ] Update `docs/features/messaging.md` and Content/Progress/Identity READMEs with the key table and creds rule — every key listed with default.

## Tests

- [ ] Unit: `MessagingOptions` binding from in-memory config incl. `IntervalsMs` array and env-style keys — values bound.
- [ ] Unit: defaults when section is missing — equal to plan defaults.
- [ ] Unit: validator rejects negative/zero delay, empty retry list, negative interval, empty host — failure names the key.
- [ ] Unit: validator — missing creds fail in Production, pass in Development, pass with InMemory transport.
- [ ] Integration (existing `MessagingRegistrationTests` in Content): invalid config throws `OptionsValidationException` on resolve/start — test green.
- [ ] Run `dotnet test` for Identity, Content, Progress slnx — all existing suites green.
