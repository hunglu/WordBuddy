# Review: WB-26_Supporter dashboard

PR: #38 · Round 1 · Reviewed commit: 2bb4fe2 · 2026-10-09T21:02:12+07:00

## Verdict
Changes requested: one major. Reloading the session page inflates "unfinished sessions", a gaming signal shown to supporters.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `Progress.Application/Features/VocabularySrs/Queries/GetVocabularySession/GetVocabularySessionQueryHandler.cs:94,102` | Every `GET session` creates a new `SessionId` (`Guid.NewGuid()`) and now stores a `VocabularySessionIssue`. A learner who opens the review page, answers 5 of 10 words, reloads, and finishes the new session gets 1 false "unfinished session". Each later visit that day adds another. Supporters see this as a gaming signal about the learner (often a child). `DailyGoal` is also affected if the first GET of the day was abandoned. | Record at most one issue per user per local day (skip insert if a row exists for today), or count a session as unfinished only if it has ≥ 1 answer and is not followed by another session the same day. Add a unit test for "two GETs, one finished session → 0 unfinished". |
| 2 | nit | `Progress.Application/Features/Dashboard/DashboardCalculator.cs:291-309` | `answered` counts distinct words of all sessions that day, `planned` is the first session only. Fine with the cap at 100 %, but the rule is not in the XML doc. | Document the rule in the method summary. |
| 3 | nit | `Progress.Application/Features/Dashboard/DashboardCalculator.cs:215-220` | Leech list is unbounded (slowest words take 10). Large list for a learner with many leeches. | Take top N, like slowest words. |
| 4 | nit | `WordBuddy.UI/src/pages/ProgressPage.tsx:39-44` | Long inline class string duplicates `primaryButtonClass`. | Reuse `primaryButtonClass` from `supportUi`. |
| 5 | nit | `WordBuddy.UI/src/hooks/useDashboard.ts:19` | `learnerId ?? ''` fallback is dead code under `enabled`. | Keep, or use a non-null assertion comment; optional. |

Verified fine: policies on both endpoints (`ChildHasSupporter`, `CanSupportLearner`), DTOs carry `DateOnly` only, logs carry ids and counts only, cache key and 5-min absolute expiry, rate limit 429 + `Retry-After`, `AsNoTracking` projections bounded by time, no `.Result`/`.Wait()`, `isError` and 403 handled in UI, theme tokens only, Framer Motion only, migration diagram updated (`docs/database-diagram/progress.md`).

## Plan conformance
- All backend, frontend and test tasks are in the diff. Two E2E tasks moved to `/test` (Sam, 2026-10-09).
- Accepted deviations: `DashboardCalculator` in Application; reviews read for 90 days (streak); in-memory `IDistributedCache` (no Redis yet); empty sessions not stored; capturing logger in log test.
- Q1 default followed: one supporter role, full view. Proposal's Peer summary is not built.
- No out-of-scope changes found.
