# Test report: Fix Aspire dashboard traces and accept ADR 0007

## Backend unit/integration

`WordBuddy/aspire/WordBuddy.Aspire.slnx` — AppHost.Tests: 21 passed, 0 failed, 0 skipped (3 m 53 s).
Includes 19 unit/model tests and 2 integration tests, among them `AppHost_Run_TracesReachOtlpEndpoint`
(real APIs with local `appsettings.Development.json`, fake OTLP/gRPC receiver; all 5 `WordBuddy.<Service>` names seen).
Ports 5080-5084, 3000, 5672, 15672 were free before the run.

## E2E API

Not applicable — the plan touches no API surface (AppHost, docs and tests only). Not run.

## E2E UI

Not applicable — no user-visible UI change. Not run.

## Dashboard check (manual task)

Not browsed with Playwright: the `aspire` CLI is not installed here, so no live dashboard session. The same
claim (traces from all 5 APIs arrive despite the local OTLP override) is proven by `AppHost_Run_TracesReachOtlpEndpoint`.
Sam may confirm once by eye with `aspire run` -> Traces.

## Regression: forbidden paths

`git diff --name-only origin/main...HEAD`: no path under `src/`, `WordBuddy.UI/`, `docker-compose.yml`, `Makefile`, `k8s/`.

## Merge guard 2 (changes since review)

Reviewed commit `e076629` is an ancestor of HEAD. `git diff --name-only <merge-tree base> HEAD`:

- `.claude/plans/WB-63_.../proposal.md`
- `.claude/plans/WB-63_.../review.md`

All on the allowlist.

## Failures

None.

## Verdict

Approved — merged into main
