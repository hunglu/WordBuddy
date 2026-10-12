# Review: Configurable messaging bus settings

PR: #44 · Round 2 · Reviewed commit: 6817061 · 2026-10-12T17:35:45+07:00

## Verdict
Approve — the round-1 blocker and major are fixed; no regressions found.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | WordBuddy/src/Services/Content/WordBuddy.Content.UnitTests/Infrastructure/MessagingOptionsTests.cs:1 | Both test files are rewritten in full (line endings changed), which hides the real change in the diff. | Keep the original line endings (`.gitattributes` / editor setting). |

## Round-1 follow-up
| R1 # | Status | Evidence |
|---|---|---|
| 1 blocker | Fixed | `RetryOptions.IntervalsMs` defaults to `[]`; `ApplyDefaults()` runs in `PostConfigure` and on the `eager` copy (MessagingExtensions.cs:52, 59). |
| 2 major | Fixed | `Should().Equal(500, 2500)`; new tests for a 3-item appsettings list and a 1-item override. |
| 3 nit | Accepted | Comment clarified; double bind kept. |
| 4 nit | Fixed | Internal `HostingEnvironment` replaced by a private `IHostEnvironment` stub. |

## Plan conformance
- All tasks in `tasks.md` remain covered. No out-of-scope change. No EF migration.
- Shared.Infrastructure 1.2.0 publish is a known pending rollout step.

## Previous rounds

## Round 1
PR: #44 · Round 1 · Reviewed commit: e12cb55 · 2026-10-12T16:48:37+07:00

### Verdict
Changes requested — the retry list default is appended to, not replaced by, the configured list.

### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | blocker | WordBuddy/src/Shared/WordBuddy.Shared.Infrastructure/Messaging/MessagingOptions.cs:61 | `IntervalsMs` defaults to `[200, 1000, 5000]`. The configuration binder appends config array items to an existing array. Scenario: every service's `appsettings.json` sets `[200, 1000, 5000]` → bound value is 6 intervals (`200,1000,5000,200,1000,5000`). An env override `Retry__IntervalsMs__0=500` gives `200,1000,5000,500,1000,5000`. Retry count can never be lowered. Breaks success criterion "changing a value changes the bus behaviour". | Default to `[]` (or `null`) and apply `[200, 1000, 5000]` only when nothing is bound (e.g. `PostConfigure`, or a getter fallback in `MessagingExtensions`). Same for the `eager` copy. |
| 2 | major | WordBuddy/src/Services/Content/WordBuddy.Content.UnitTests/Infrastructure/MessagingOptionsTests.cs:63 | `Should().Contain([500, 2500])` passes with the appended list, so it hides finding 1. | Assert `Should().Equal(500, 2500)`; add a case binding the full appsettings-style list. |
| 3 | nit | WordBuddy/src/Shared/WordBuddy.Shared.Infrastructure/Messaging/MessagingExtensions.cs:56 | Section bound twice (DI + `eager`). Outbox values come from `eager` and are validated only via the DI copy; both reflect the same config, so no bug today. | Comment is enough; optional single bind helper. |
| 4 | nit | WordBuddy/src/Services/Content/WordBuddy.Content.IntegrationTests/Messaging/MessagingRegistrationTests.cs:56 | Uses `Microsoft.Extensions.Hosting.Internal.HostingEnvironment` (internal-namespace type). | Use a small `Mock<IHostEnvironment>`. |

### Plan conformance
- All tasks in `tasks.md` are reflected in the diff.
- Creds rule, validation, heartbeat, timeout, outbox and duplicate window: implemented as planned.
- No out-of-scope change. No EF migration.
- Shared.Infrastructure 1.2.0 publish (`make publish-shared`) is a known pending rollout step.
