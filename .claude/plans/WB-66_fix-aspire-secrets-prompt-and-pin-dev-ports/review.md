# Review: WB-66 Fix Aspire secrets prompt and pin dev ports

PR: #67 · Round 1 · Reviewed commit: 1e8e08b · 2026-11-03T21:45:36+07:00

## Verdict

Approve — no blockers or majors. Three nits. Two items still need a live check in `/test`.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `WordBuddy/aspire/WordBuddy.AppHost/Program.cs:36-38` | Small behaviour change: the removed helper treated a blank `Parameters:jwt-issuer` (or `jwt-expiry-minutes`, `rabbitmq-user`) as "use default". `AddParameter("<name>")` now takes the blank value. Scenario: `dotnet user-secrets set "Parameters:jwt-issuer" ""` → APIs get an empty issuer → token validation fails. | Accept as is (unlikely setup), or note in the README that an override must not be blank. |
| 2 | nit | `WordBuddy/aspire/WordBuddy.AppHost/Program.cs:16` | `SqlPort` is `public`, but nothing outside `Program` uses it. Tests hardcode `14330` on purpose (independent guard). | Make it `private const`, or keep `public` and drop the claim that it is shared. |
| 3 | nit | `WordBuddy/aspire/aspire.config.json:5` | No newline at end of file. | Add a trailing newline (only if the Aspire CLI does not rewrite it). |

### Verification items for `/test` (not code defects)

| Item | Why | How |
|---|---|---|
| Root cause (no launch profile → `Production` → user-secrets not loaded) | Still a hypothesis; unit tests cannot prove that `aspire run` picks the profile. The fix matches how the Aspire builder loads user-secrets (Development only). | Plan's manual check: reset volumes → `aspire run` → no prompt, dashboard on 15180, SQL on 14330, all APIs healthy. |
| Integration run while the persistent `sql` container holds 14330 | `tasks.md` states the run was done without that container. The code path (`tcp` endpoint `Port = null`) is correct but not exercised against a busy 14330. | Start `aspire run` once, stop it (persistent `sql` stays), then run `--filter "Category=Integration"`. |

## Checks

| Area | Result |
|---|---|
| Correctness | OK. `AddParameter("<name>")` reads `Parameters:<name>`; user-secrets override `appsettings.json`. SQL connection strings flow by reference, so the dynamic test port is safe. |
| Security / privacy | OK. No secret in `appsettings.json` / `launchSettings.json`; new guard test enforces it. Dashboard still needs the login token (`ASPIRE_ALLOW_UNSECURED_TRANSPORT` only allows http on localhost). |
| Conventions | OK. Explicit types, XML doc comments, test names follow `{Class}_{Method}_{Outcome}`. No `src/`, UI, compose, Makefile or k8s change. |
| Tests present | OK. 5 new tests (4 config-file guards + parameter defaults) plus the port pin test. |
| EF migration / DB diagram | None in the diff. |

## Plan conformance

| Task group | Status |
|---|---|
| Git (branch from `4f3129b`, new PR, close #65) | Done — PR #67 open, head `1e8e08b`. |
| Backend (keep draft, drop `AddParameterWithDefault`, comment, dynamic test SQL port) | Done — no fallback needed. |
| Docs (README port table, user-secrets note, tests note) | Done. README also expands the volume-reset note to RabbitMQ; matches the proposal goal. |
| Tests (5 new + run) | Done — 26 unit passed, 2 integration passed (see verification item 2). |

Out of scope: none. The `Pending changes` line in `docs/features/WB-61_...md` is `/propose` bookkeeping; `/test` moves it to change history.
