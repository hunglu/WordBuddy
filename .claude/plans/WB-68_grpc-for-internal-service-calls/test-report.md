# Test report: gRPC for internal service calls

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Content.UnitTests | 453 passed, 0 failed |
| Content.IntegrationTests | 227 passed, 0 failed |
| Progress.UnitTests | 446 passed, 0 failed |
| Progress.IntegrationTests | 101 passed, 0 failed |
| Shared (`WordBuddy.Shared.slnx`) | No test projects — nothing to run |
| Aspire (`WordBuddy.Aspire.slnx`, AppHost.Tests) | **28 passed, 2 FAILED** (30 total, 14m44s) |

Merge of `origin/main`: already up to date.

## E2E API

Skipped — services could not be started. `docker compose up -d --build` failed on `progress-api`:
`NU1101: Unable to find package WordBuddy.Shared.Protos` in `github-wordbuddy` (the package is only in the local feed; `make publish-shared` needs a `write:packages` token).

| Task | Result |
| --- | --- |
| Vocabulary review scenarios, default gRPC | Not run |
| Same scenarios, `Services__Content__Transport=Rest` | Not run |

## E2E UI

Skipped — services not running. This plan has no UI change.

## Aspire trace check (`rpc.system=grpc`)

Not done. No Aspire dashboard was started and nothing was inspected. Not faked.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review is Approve, reviewed sha `fa37612` is an ancestor of HEAD | Pass |
| 2. Paths changed since review (allowlist) | **Fail** — `docs/adr/0008-grpc-internal-service-calls.md` is not on the allowlist (review round changed `Status: accepted (pending ...)` to `accepted`). `proposal.md` and `review.md` are allowed. |
| 3. All suites ran with zero failures | **Fail** — Aspire 2 failures; E2E API/UI skipped |

`git diff --name-only <merge-tree base> HEAD`:
```
.claude/plans/WB-68_grpc-for-internal-service-calls/proposal.md
.claude/plans/WB-68_grpc-for-internal-service-calls/review.md
docs/adr/0008-grpc-internal-service-calls.md
```

## Failures

- Aspire AppHost.Tests: 2 of 30 failed. Names not captured (run output was trimmed); re-run `dotnet test WordBuddy/aspire/WordBuddy.Aspire.slnx` to see them. Unknown whether related to WB-68.
- E2E blocked: publish `WordBuddy.Shared.Protos` 1.0.0 to GitHub Packages (Sam, `make publish-shared`), then re-run `/test`.
- Guard 2: one-line ADR edit outside the allowlist. Sam decides: accept it, or `/review` again.

Not done in this run: living spec `docs/features/WB-68_...md` and README row stay as they are (not shipped). The wording fix for review nit #4 is still open.

## Verdict

Not merged — guard 2 failed (ADR changed after review) and guard 3 failed (Aspire 2 failures; E2E API/UI skipped).
