# Test report: gRPC for internal service calls

Run 2 (after review round 2). Merge of `origin/main`: already up to date.

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Content.UnitTests | 453 passed, 0 failed |
| Content.IntegrationTests | 227 passed, 0 failed (see note) |
| Progress.UnitTests | 446 passed, 0 failed |
| Progress.IntegrationTests | 101 passed, 0 failed |
| Aspire (`WordBuddy.Aspire.slnx`) | 30 passed, 0 failed |
| Shared | No test projects |

Note: the first Content integration run, started in parallel with the Aspire suite, had 1 failure
(`SupportLinkQueueTests.SupportLinkConsumers_Consume_FromContentPrefixedQueues`, RabbitMQ-based).
Re-run alone: 227/227. Cause: parallel run, not WB-68 code.

## E2E API

`dotnet test e2e/api/WordBuddy.E2E.Api.Tests` against `docker compose` (Shared.Protos 1.0.0 now restores).

| Run | Config | Result |
| --- | --- | --- |
| 1 | Default gRPC, plain compose | 91 / 93 — 2 autofill tests failed (`autofillUnavailable`: Content needs `Autofill__UseFakeClients=true`, unrelated to WB-68) |
| 2 | Default gRPC + `Autofill__UseFakeClients=true` (compose override file) | 93 / 93 passed (incl. `VocabularyReviewSessionTests`) |
| 3 | `Services__Content__Transport=Rest` on Progress + same override | 93 / 93 passed |

Transport proof from Content request logs:

| Phase | gRPC `ContentSenses/GetSensesByIds` | REST `GET /api/vocabulary/senses` |
| --- | --- | --- |
| After run 2 (gRPC) | 10 | 2 |
| After run 3 (Rest) | 10 (no new) | 14 (+12) |

So the default used gRPC and `Transport=Rest` used REST. No public REST change.

## E2E UI

Skipped — no UI change in this plan.

## Trace check (`rpc.system=grpc`)

Aspire dashboard not inspected (`aspire run` not started). Verified another way, on docker compose:

- Content request log shows `HTTP POST /wordbuddy.content.v1.ContentSenses/GetSensesByIds responded 200` for review sessions (10 calls) under the default.
- Progress `/health/ready` was `Healthy` with the gRPC client active.
- The `rpc.system=grpc` span attribute itself was **not** seen.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve; `86a6f52` is an ancestor of HEAD | Pass |
| 2. Paths changed since review vs reviewed code + main | Pass — only `proposal.md`, `review.md` (allowlisted) |
| 3. All suites ran, zero failures | Pass (UI suite not applicable; Content integration re-run clean) |

## Failures

None open. Both transient failures above were environment effects and passed on re-run or with the documented fake-client setting.

Left open on purpose: `tasks.md` Tester boxes are not ticked (file is outside the allowlist). Review nits 1-3 and 5 are not addressed here.

## Verdict

Approved — merged into main.
