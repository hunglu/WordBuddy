# Review: WB-28 Vocabulary answer checking

PR: #40 · Round 1 · Reviewed commit: fad93d5 · 2026-10-10T23:47:48+0700

## Verdict
Changes requested — open exercises can be farmed to find the right option, so a forged answer can still earn a correct first-attempt rating.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | blocker | `Progress.Application/.../CreateVocabularyExercise/CreateVocabularyExerciseCommandHandler.cs:150` (`PickDistractors` at :219) | Every call issues a new exercise with fresh random distractors. The target word is the only option present in all of them. Scenario: a script calls `POST /exercises` 3 times for one sense (none answered), intersects the option texts → the right word. It then answers the last-issued exercise at once → attempt 1, correct, fast → `Good`/`Easy` and reschedule. This breaks the success criterion "a forged answer cannot earn a correct rating". | Allow one open exercise per (user, session, sense): if an unanswered one exists, return it unchanged (same options, same `IssuedAtUtc`). Add a unit test and an integration test "second create returns the same exercise". |
| 2 | major | `Progress.Infrastructure/ContentClient/ContentSenseClient.cs:83` | The catch covers `HttpRequestException`, `TaskCanceledException`, `JsonException`. The standard resilience handler throws `Polly.Timeout.TimeoutRejectedException` (attempt/total timeout) and `Polly.CircuitBreaker.BrokenCircuitException` (circuit open). Scenario: Content hangs or is down for 30 s → circuit opens → unhandled → 500 instead of the planned 503 `Content.Unavailable`, logged as Error. | Add `TimeoutRejectedException` and `BrokenCircuitException` (or `ExecutionRejectedException`) to the filter, as Content's `FreeDictionaryClient.cs:62` already does. Add a unit test with a handler that throws them. |
| 3 | nit | `WordBuddy.UI/src/pages/VocabularyReviewPage.tsx:188,242` | If the review POST succeeded but the reply was lost, "Try again" resends → 409 `Exercise.AlreadyAnswered` → the error box stays; only "Skip" helps. | On 409 `Exercise.AlreadyAnswered`, treat as saved and move on (or show "Next"). |
| 4 | nit | `Progress.Api/Program.cs:33` | Content in `/health/ready` takes all of Progress out of rotation when Content is down, though only the exercise endpoint needs it. Planned, so not a defect. | Consider a `Degraded` result instead of `Unhealthy` for this check. |

### Reported deviations — judgement

| Deviation | Verdict |
|---|---|
| Content down → 503 (plan: 400) | Accepted. 503 is the right code for a downstream outage. See finding 2 for the gap. |
| 409 `Exercise.SenseCompleted` after 2 correct answers | Accepted. Matches the UI queue rule and caps exercise spam; UI skips it. |
| `CorrectWord` column + filtered unique index on `ReviewLogs.ExerciseId` | Accepted. Needed for feedback and to stop a concurrent replay (2601 → 409 via existing `SaveChangesAsync`). |
| Prompt drops `partOfSpeech`, adds `personalContext`, `hintFirstLetter` | Accepted. Same data the UI showed before; no answer leak for Typing beyond the old hint. |
| Infrastructure gets ASP.NET FrameworkReference + Http.Resilience | Accepted. Needed for `IHttpContextAccessor`; same pattern as Content. |
| UI "3 exercise types" E2E uses stubbed replies | Accepted. Real data cannot force each type deterministically. |
| Open questions resolved with plan defaults | Accepted. Q4 (no retry cap) is what enables finding 1 — fix there, not via a cap. |

### Checked, no finding
- Ownership: foreign exercise → 404; session ownership via `GetOpenAsync(userId)`.
- Forged `isCorrect` ignored (request record has no such field; integration test present).
- Child path: caller JWT forwarded; token never logged; cache key per session (one user).
- Logs: ids and counts only; answer text never stored or logged.
- Migration and `docs/database-diagram/progress.md` match the snapshot.
- UI: TanStack mutations, `isError` / load-failed handled, `wb-` tokens, no `any`/`enum`.

## Plan conformance
- All tasks in `tasks.md` are reflected in the diff.
- Out of scope: none beyond the deviations above.
- Tests: unit, integration and E2E exist; E2E not run yet (needs full stack, `/test`).
