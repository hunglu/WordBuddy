# Plan: Configurable messaging bus settings

## Summary

Move every MassTransit time setting and the RabbitMQ settings into a validated `MessagingOptions` class in Shared.Infrastructure (1.1.0 → 1.2.0). Bind it from the `Messaging` section with `ValidateOnStart`. Fail at startup on bad values or on missing RabbitMQ credentials outside Development.

## Verified facts (differ from proposal)

| Proposal says | Code says |
| --- | --- |
| Outbox `QueryDelay` = 100 s | `TimeSpan.FromSeconds(1)` (`MessagingExtensions.cs:96`) |
| Users: Content, Progress | **Identity, Content, Progress** all call `AddWordBuddyMessaging` |
| Shared.Infrastructure 1.1.0 | Correct. Quiz + Notification still on 1.0.2 (no messaging; not touched) |

## Affected services / areas

| Area | Change |
| --- | --- |
| `Shared.Infrastructure` | `MessagingOptions` + validator, refactor `MessagingExtensions`, version 1.2.0 |
| Identity, Content, Progress | Package bump 1.2.0 (Api + Infrastructure csproj), `Messaging` section in `appsettings.json` |
| `docker-compose.yml`, `k8s/configmap.yaml` | Optional env overrides for the new keys (documented examples) |
| Docs | `docs/features/messaging.md`, service READMEs |

## Backend approach

### Options shape

```json
"Messaging": {
  "Transport": "RabbitMq",
  "RabbitMq": {
    "Host": "localhost", "VirtualHost": "/",
    "Username": "", "Password": "",
    "HeartbeatSeconds": 10, "RequestedConnectionTimeoutMs": 10000
  },
  "Outbox": { "QueryDelayMs": 1000, "DuplicateDetectionWindowSeconds": 1800 },
  "Retry": { "IntervalsMs": [200, 1000, 5000] }
}
```

- `MessagingOptions` (nested `RabbitMqOptions`, `OutboxOptions`, `RetryOptions`), `public const string SectionName = "Messaging"`. Defaults = current values (MassTransit default for duplicate window: 30 min).
- Units in key names (`Ms`, `Seconds`) to avoid `TimeSpan` string parsing in env vars.
- Env override: `Messaging__Retry__IntervalsMs__0=500`.

### Validation (`MessagingOptionsValidator : IValidateOptions<MessagingOptions>`)

| Rule | Error text names the key |
| --- | --- |
| `Transport` is `RabbitMq` or `InMemory` | yes |
| `QueryDelayMs` > 0, `DuplicateDetectionWindowSeconds` > 0 | yes |
| `IntervalsMs` not empty, every value > 0 | yes |
| `HeartbeatSeconds` ≥ 0, `RequestedConnectionTimeoutMs` > 0 | yes |
| `Host` not empty (RabbitMq transport) | yes |
| `Username`/`Password` not empty when RabbitMq and env ≠ Development | yes |

- Validator gets `IHostEnvironment` from DI → no signature change to `AddWordBuddyMessaging`.
- Development keeps the `guest`/`guest` fallback (applied only when env is Development).
- Register: `AddOptions<MessagingOptions>().Bind(section).ValidateOnStart()` + validator as singleton.

### Refactor flow

```
AddWordBuddyMessaging(config)
 → bind options (eager copy for registration-time values: Transport, QueryDelay, DuplicateWindow)
 → UsingRabbitMq: read IOptions<MessagingOptions> from context → Host, creds, heartbeat, timeout
 → endpoints callback: retry.Intervals(options.Retry.IntervalsMs)
 → host start → ValidateOnStart → OptionsValidationException on bad values
```

- `Transport` unknown value: keep current immediate `InvalidOperationException` (needed before options are resolved) and also validate.
- No public API removed. Constants (`TransportKey`, etc.) stay.

### Child vs adult

Not applicable. Infrastructure only; no user data, no endpoint change.

## Frontend approach

None.

## Data / migration notes

None. No entity change. `DuplicateDetectionWindow` is a runtime setting, not a schema change.

## Release notes

- `make publish-shared` is ask-gated; local builds use `dotnet pack ... -o local-nuget-feed`.
- Compose/kind: `.env` and `k8s/secret.yaml` must hold RabbitMQ creds, else the pods now fail at startup (by design).

## Open questions

1. Development fallback to `guest`/`guest`: keep (plan assumes yes), or require creds everywhere?
2. Include `HeartbeatSeconds` / `RequestedConnectionTimeoutMs` now (plan assumes yes) or only the outbox and retry values?
3. Integration tests run in which environment? If not `Development`, the InMemory transport must skip the creds rule (plan: creds rule only for RabbitMq transport).
