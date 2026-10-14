# Test report: WB-49 Personal context on learner word

Run 2. Run 1 failed only on an environment gap (`SupportLinks__UnlinkOverrideWaitDays` missing on identity-api). Sam fixed it; no application code changed since.

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Content UnitTests | 365 passed, 0 failed |
| Content IntegrationTests (real SQL Server) | 143 passed, 0 failed (3 new) |

New integration tests (`ConcurrentVocabularyAddTests.cs`), from review round 2 finding 1:

| Test | Path covered |
| --- | --- |
| `VocabularyWordRepository_LinkAsync_DuplicateKeyWinnerHasNoNote_KeepsLoserNote` | `LinkAsync` unique violation: loser note kept when winner has none |
| `VocabularyWordRepository_LinkAsync_DuplicateKeyWinnerHasNote_DoesNotOverwrite` | `LinkAsync` unique violation: stored note never overwritten |
| `VocabularyWordRepository_SaveLinkAsync_LinkDeleted_ReturnsNotFound` | `SaveLinkAsync`: `DbUpdateConcurrencyException` → `PersonalVocabularyWord.NotFound` (404) |

## E2E API

70 passed, 0 failed (`dotnet test e2e/api/WordBuddy.E2E.Api.Tests`), including `PersonalContextTests` (2).

## E2E UI

50 passed, 0 failed (`npm test` in `e2e/ui`), including both `vocabulary-personal-context.feature` scenarios and `support-links.feature` "Admin completes an escalated unlink request".

## Failures

None. Note: the first Content integration run showed 1 failure of 143 (run took 3m55s, LocalDB cold start). A re-run of the same build passed 143/143 in 51s. The failing test name was not captured; treated as a transient environment timing issue.

## Known coverage gaps

- No Child UI E2E scenario for the "Only you" hint. No step sets up a Child with a linked supporter. Deferred (review round 2, finding 2).
- `npx tsc --noEmit` in `e2e/ui` fails on missing `@types/node`. Existed before this change.

## Merge guard

- Guard 1: `review.md` verdict Approve; reviewed commit `25d4e52` is an ancestor of HEAD. Pass.
- Guard 2: `git merge-tree 25d4e52 origin/main`, diff to HEAD lists only allowlisted paths (`origin/main` merge: already up to date):
  - `.claude/plans/WB-49_personal-context-on-learner-word/{proposal,review,test-report}.md`
  - `WordBuddy/src/Services/Content/WordBuddy.Content.IntegrationTests/ConcurrentVocabularyAddTests.cs`
  - `docs/ai/learnings.md`
  - `e2e/api/WordBuddy.E2E.Api.Tests/PersonalContextTests.cs`
  - `e2e/ui/steps/vocabulary{,-autofill,-sharing}.steps.ts`

  Pass.
- Guard 3: all four suites ran with zero failures in the final results. Pass.

## Verdict

Approved — merged into main
