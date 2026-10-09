# Review: WB-26_Supporter dashboard

PR: #38 · Round 2 · Reviewed commit: 08bdc9b · 2026-10-09T21:46:23+07:00

## Verdict

Approve: round-1 major is fixed by R1 (resumable sessions); all nits fixed. Three new nits, no blockers or majors.

## Round-1 findings

| # | Status | Evidence |
| --- | --- | --- |
| 1 major | Fixed (per R1) | Reload resumes the open session with the same `SessionId` and unanswered items (`GetVocabularySessionQueryHandler.cs:84-108,162-201`). Unfinished = `ExpiresAtUtc <= now` and answered < planned (`DashboardCalculator.cs:519-522`). Tests cover two GETs, expiry, all answered, open session. |
| 2 nit | Fixed | Daily-goal rule in XML doc (`DashboardCalculator.cs:151-155`). |
| 3 nit | Fixed | `LeechesTake = 10`. |
| 4 nit | Fixed | `ProgressPage.tsx` reuses `primaryButtonClass`. |
| 5 nit | Fixed | `learnerId!` with comment (`useDashboard.ts:19`). |

## R1 conformance

| Rule | Result |
| --- | --- |
| Resume: same `SessionId`, remaining items in stored order | Yes (`Position` order) |
| Ends when all answered or expired | Yes: `End()` on resume with 0 left; `GetOpenAsync` filters `ExpiresAtUtc > now` |
| Expiry = min(issued + duration, local day end) | Yes (`VocabularySessionIssue.Create`), unit-tested |
| `Cap` / `IntroducedToday` recomputed on resume | Yes |
| Unfinished counts only expired incomplete sessions | Yes |
| `AddDashboard` regenerated, no second migration | Yes: old file deleted, new migration + snapshot + diagram consistent |
| Same rule child/adult; logs ids/counts + `Resumed` | Yes |

## Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | nit | `GetVocabularySessionQueryHandler.cs:84-143` | Two tabs that GET at the same moment with no open session both create a session. The learner finishes one; the other expires and counts as 1 unfinished. Rare (needs simultaneous first load). | Accept for now, or a filtered unique index on open sessions per user (already listed as a scope suggestion). |
| 2 | nit | `GetVocabularySessionQueryHandler.cs:195-198` + `DashboardCalculator.cs:519-522` | A session ended because its remaining words were removed (inactive) has answered < planned. Once expired it counts as unfinished, though `EndedAtUtc` is set. | Exclude sessions with `EndedAtUtc != null` from unfinished (project `EndedAtUtc` into `DashboardSessionRow`). |
| 3 | nit | `useDashboard.ts`, `README.md`, `appsettings.json`, `VocabularySchedulingOptions.cs`, `GetVocabularySessionQueryHandlerTests.cs`, `docs/database-diagram/progress.md` | Whole-file line-ending rewrites (CRLF/LF). Real change is 2-91 lines; diff shows ~1,000. Hurts blame. | Keep original line endings; add `.gitattributes` if needed. |

## Plan conformance

- All `## Fix round 1` tasks are in the diff (domain, options, EF, repositories, handler, calculator, nits, unit + integration tests).
- No out-of-scope changes.

## Previous rounds

PR: #38 · Round 1 · Reviewed commit: 2bb4fe2 · 2026-10-09T21:02:12+07:00

### Verdict

Changes requested: one major. Reloading the session page inflates "unfinished sessions", a gaming signal shown to supporters.

### Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | major | `Progress.Application/Features/VocabularySrs/Queries/GetVocabularySession/GetVocabularySessionQueryHandler.cs:94,102` | Every `GET session` creates a new `SessionId` (`Guid.NewGuid()`) and now stores a `VocabularySessionIssue`. A learner who opens the review page, answers 5 of 10 words, reloads, and finishes the new session gets 1 false "unfinished session". Each later visit that day adds another. Supporters see this as a gaming signal about the learner (often a child). `DailyGoal` is also affected if the first GET of the day was abandoned. | Record at most one issue per user per local day (skip insert if a row exists for today), or count a session as unfinished only if it has ≥ 1 answer and is not followed by another session the same day. Add a unit test for "two GETs, one finished session → 0 unfinished". |
| 2 | nit | `Progress.Application/Features/Dashboard/DashboardCalculator.cs:291-309` | `answered` counts distinct words of all sessions that day, `planned` is the first session only. Fine with the cap at 100 %, but the rule is not in the XML doc. | Document the rule in the method summary. |
| 3 | nit | `Progress.Application/Features/Dashboard/DashboardCalculator.cs:215-220` | Leech list is unbounded (slowest words take 10). Large list for a learner with many leeches. | Take top N, like slowest words. |
| 4 | nit | `WordBuddy.UI/src/pages/ProgressPage.tsx:39-44` | Long inline class string duplicates `primaryButtonClass`. | Reuse `primaryButtonClass` from `supportUi`. |
| 5 | nit | `WordBuddy.UI/src/hooks/useDashboard.ts:19` | `learnerId ?? ''` fallback is dead code under `enabled`. | Keep, or use a non-null assertion comment; optional. |

Verified fine: policies on both endpoints (`ChildHasSupporter`, `CanSupportLearner`), DTOs carry `DateOnly` only, logs carry ids and counts only, cache key and 5-min absolute expiry, rate limit 429 + `Retry-After`, `AsNoTracking` projections bounded by time, no `.Result`/`.Wait()`, `isError` and 403 handled in UI, theme tokens only, Framer Motion only, migration diagram updated (`docs/database-diagram/progress.md`).

### Plan conformance

- All backend, frontend and test tasks are in the diff. Two E2E tasks moved to `/test` (Sam, 2026-10-09).
- Accepted deviations: `DashboardCalculator` in Application; reviews read for 90 days (streak); in-memory `IDistributedCache` (no Redis yet); empty sessions not stored; capturing logger in log test.
- Q1 default followed: one supporter role, full view. Proposal's Peer summary is not built.
- No out-of-scope changes found.
