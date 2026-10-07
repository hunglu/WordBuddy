# Review: WB-22_Vocabulary SRS engine

PR: #33 · Round 1 · Reviewed commit: 1ef0efe · 2026-10-07T11:18:35+07:00

## Verdict

Changes requested — one major: concurrent duplicate answers can reschedule a word twice.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `Progress.Application/Features/VocabularySrs/Commands/RecordVocabularyReview/RecordVocabularyReviewCommandHandler.cs:78-92`; `Progress.Infrastructure/Persistence/Configurations/LearnerWordStateConfiguration.cs:10-34`; `ReviewLogConfiguration.cs:28-29` | `AttemptNo` is a read-then-insert count with no guard. Two concurrent POSTs for the same (session, sense) — double tap or client retry — both read 0, both get `AttemptNo = 1`, both call `ApplyReview`. Result: `Reps` +2, a second FSRS step from the stale card, and two log rows with `AttemptNo = 1`. Breaks "FSRS only on the first attempt". | Add a unique index on `ReviewLogs (UserId, SessionId, SenseId, AttemptNo)` and map the duplicate-key `DbUpdateException` to `Error.Conflict` (409). Optionally add a `rowversion` concurrency token on `LearnerWordState`. Add an integration test with two parallel POSTs. |
| 2 | nit | `Content.Infrastructure/Repositories/VocabularyWordRepository.cs:36-50` | Backfill pages with `Skip/Take` on `Id`. A link removed during the run shifts later rows, so one link is skipped silently. Mitigated by "safe to run twice". | Use keyset paging: `Where(l => l.Id > lastId).OrderBy(l => l.Id).Take(n)`. |
| 3 | nit | `Progress.Api/Extensions/ClaimsPrincipalExtensions.cs:32-39` | Missing `age_group` → Child here; Content throws on a missing claim. Lenient default is acceptable (only grading thresholds), but the two services now differ. | Keep; record the difference in the living spec / ADR 0005. |
| 4 | nit | `Progress.Api/Extensions/RateLimitingConfiguration.cs:15` | 60/min is a code constant, not an option. | Fine for MVP; move to config if ops need to tune it. |

Accepted as flagged by the coder (no finding):

- Extra nullable `FsrsStep` column — required by FSRS learning steps; diagram updated.
- Before-save hook on `LearnerWordMembershipRepository` — keeps membership + state in one save; the unique-violation path detaches staged states and re-runs the hook. Correct.
- `X-Client-CurrentDateTime` validated only on GET session — POST does not use the day boundary.
- Optional `BatchSize` on the republish command — validated; endpoint uses the default.
- Child `PUT settings` → 200 and one cap rule for all users — per D-3/D-4.

Checked, no issue: `[Authorize]` on the SRS controller, `AdminOnly` on republish, caller-only data, logs carry ids/counts only, no `.Result`/`.Wait()`, `UseRateLimiter` after authentication, Result<T> everywhere, migration matches `docs/database-diagram/progress.md`.

## Plan conformance

- All tasks in `tasks.md` are reflected in the diff (Progress domain, infra, app/API, Content backfill, docs, tests).
- Deviations match the Decision log (D-3, D-4, D-7).
- Out of scope: none found.
