# Review: WB-22_Vocabulary SRS engine

PR: #33 · Round 2 · Reviewed commit: a8c3eaf · 2026-10-07T12:27:33+07:00

## Verdict

Approve — round-1 major is fixed; no new blockers or majors.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `Progress.Infrastructure/Repositories/LearnerWordMembershipRepository.cs:127` | `RowVersion` now also guards the consumer path. A review saved between the consumer's read and save throws `DbUpdateConcurrencyException` (uncaught). It falls back to the shared `UseMessageRetry` (`MessagingExtensions.cs:102`), which wraps the inbox and rolls back. Acceptable: rare, self-healing, logged as a retry. | Keep. Optionally log at Information before rethrow, or map to `Result` like `LearnerWordStateRepository`. |
| 2 | nit | `Progress.Infrastructure/Repositories/LearnerWordStateRepository.cs:101` | Unique-violation catch maps *any* unique key to `LearnerWordState.ConcurrentUpdate`. Today only the attempt index can fire on this path, so it is correct. | Keep; narrow by index name if more unique keys are added. |

Round-1 findings:

| R1 # | Status | Evidence |
|---|---|---|
| 1 major (duplicate attempts) | Fixed | Unique `IX_ReviewLogs_UserId_SessionId_SenseId_AttemptNo` + `RowVersion` on `LearnerWordStates`. `DbUpdateConcurrencyException` and SQL 2601/2627 → `Error.Conflict` → 409 (`ResultExtensions.cs:14`). One `SaveChangesAsync` = one transaction, so state + log roll back together. No exception text leaks. Unit + 2 integration tests (deterministic stale-save test asserts Conflict and one log). |
| 2 nit (Skip/Take backfill) | Fixed | Keyset on `Id > lastId`, ordered by `Id`. |
| 3 nit (age_group default) | Documented | Progress README. |
| 4 nit (rate-limit constant) | Documented | Progress README. |

Migration `AddReviewConcurrencyGuards`: Up adds column + index; Down drops both in reverse. Snapshot and `docs/database-diagram/progress.md` match.

## Plan conformance

- All tasks reflected; fix-round tasks added to `tasks.md`.
- Out of scope: none.

## Previous rounds

### Round 1
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
