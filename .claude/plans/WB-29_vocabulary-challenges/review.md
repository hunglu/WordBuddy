# Review: WB-29 Vocabulary challenges

PR: #42 · Round 2 · Reviewed commit: cf09edc · 2026-10-11T23:07:56+07:00

## Verdict
Approve — all three majors are fixed; no new blockers or majors.

## Round-1 findings

| # | Round-1 severity | Status | Evidence |
|---|---|---|---|
| 1 | major | Fixed | `ApplySupportLinkEventCommandHandler.cs:53-103`: cleanup now runs whenever the stored link is inactive, also on replay. Cleaner is idempotent (no consents → success). A failure returns `Result.Failure` → consumer throws → MassTransit retries → cleanup runs again. A stale revoke after re-activation does not clean (stored link active) — correct. |
| 2 | major | Fixed | `ChallengeRepository.cs:92-124`: count uses `WITH (UPDLOCK, HOLDLOCK)`, so joins queue instead of deadlocking. SQL is an interpolated `FormattableString` → `ChallengeId` is sent as a parameter (no injection). Table `ChallengeParticipants` and column `ChallengeId` match the EF config. Only 2601/2627 map to `Challenge.AlreadyJoined`; other `DbUpdateException`/`SqlException` map to retryable `Challenge.Conflict`. |
| 3 | major | Fixed | `ReviewLogRepository.cs:25-33`: loads logs before `EndsAtUtc` only, and earlier logs only for senses answered in the window. Indexes `(UserId, OccurredAtUtc)` and `(UserId, SenseId)` cover the query and the correlated `EXISTS`. Scorer doc updated. |
| 4 | nit | Fixed | `ChallengeScorer.cs:94`: Listening count no longer capped; documented. |
| 5 | nit | Accepted | Noted in living spec. |
| 6 | nit | Accepted | Noted in living spec. |

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `ChallengeRepository.cs:117-123` | `Challenge.Conflict` and `Challenge.AlreadyJoined` both return 409; the client must read the error code to know a retry is safe. | Optional: document the code in the spec / UI retry message. |

## Plan conformance
All tasks still covered. Fix round touched only the files of findings 1-4, their tests, and the living spec. No out-of-scope changes. No migration change.

## Previous rounds

### Round 1
### Review: WB-29 Vocabulary challenges

PR: #42 · Round 1 · Reviewed commit: bf050d3 · 2026-10-11T22:55:58+07:00

#### Verdict
Changes requested — revoke cleanup can be lost on retry, and a join deadlock is reported as "already joined".

#### Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `Progress.Application/Features/SupportLinks/Commands/ApplySupportLinkEvent/ApplySupportLinkEventCommandHandler.cs:80-98` | The link revoke is saved first (line 82), consent cleanup is a second save (line 93). If cleanup fails, MassTransit retries, but `Apply` now returns `applied = false` for the replay, so cleanup is skipped forever. The child keeps Guardian consent after the link is revoked (child-privacy rule). | Stage the consent removal before the single `SaveChangesAsync` (same scoped DbContext), or run cleanup whenever the stored link is inactive, not only when `applied`. Add a test: cleanup fails once → replay still removes consent. |
| 2 | major | `Progress.Infrastructure/Repositories/ChallengeRepository.cs:92-112` | Serializable count-then-insert: two concurrent joins both take range S locks, then both insert → SQL Server deadlock (1205). The victim's `DbUpdateException` is mapped to `409 Challenge.AlreadyJoined`, but the learner is not joined. A deadlock on the `CountAsync` read throws `SqlException` (not caught) → 500. Capacity itself is safe. | Use `UPDLOCK, HOLDLOCK` on the count (raw SQL), or map only unique-index violations (2601/2627) to `AlreadyJoined` and return a retryable `Challenge.Conflict` for deadlocks; catch both exception types. Add an integration test with parallel joins at capacity − 1. |
| 3 | major | `Progress.Infrastructure/Repositories/ReviewLogRepository.cs:25-26`, `Progress.Application/Features/Challenges/ChallengeScoreProjector.cs:46` | Recompute loads the full review-log history of every participant of every Running challenge, every 5 min. Up to ~7 challenges × 30 learners × all-time logs, all tracked-free but in memory. Grows without limit. | Load logs up to `EndsAtUtc` only, and for history (first-log / recognize flags) project per sense with a grouped query, or load only senses seen in the window. |
| 4 | nit | `Progress.Domain/Challenges/ChallengeScorer.cs:95` | `ListeningCorrect` caps a daily **count** with `DailyPointCap` (200 points). Unit mismatch; not in plan. | Separate option, or drop the cap and document. |
| 5 | nit | `Progress.Application/Features/Challenges/Commands/SetChildChallengeConsent/SetChildChallengeConsentCommandHandler.cs:79-82` | Deviation (b): one guardian switching off removes every guardian's consent. Safe for privacy, but a second guardian loses their choice silently. | Accept, and state it in the living spec; or remove only the caller's consent. |
| 6 | nit | `Progress.Application/Features/Challenges/Queries/GetChallengeBoard/GetChallengeBoardQueryHandler.cs:209` | Cached snapshot holds `UserId` per row. Not in any DTO (checked), but it sits in Redis. | Acceptable; note it in the spec. |

Verified fine:
- Child board: top 5 + own row, alias or "Child learner" + avatar, no user ids, scores only on own row. `DisplayName` forced null for Child in `ChallengeProfile.Apply`.
- Scoring: +10 / +5 hint / +5 recall upgrade / daily goal / 200 cap per local day; non-due, wrong, timing-adjusted, < 800 ms, first-review and pre-join rows ignored.
- `GuardianOnly` policy plus local `IsGuardian` re-check plus active link.

#### Deviations

| Deviation | Judgment |
|---|---|
| (a) Child losing consent leaves unfinished challenges | Accept — safer for privacy. |
| (b) Switch-off removes all guardians' consent | Accept with note (finding 5). |
| (c) Revoke cleanup in `ApplySupportLinkEventCommandHandler` | Right place; atomicity broken (finding 1). |

#### Plan conformance
All tasks in `tasks.md` have code in the diff. EF migrations come with `docs/database-diagram/identity.md` and `progress.md`. E2E API/UI tests are built but not run (need full stack). No out-of-scope changes found.
