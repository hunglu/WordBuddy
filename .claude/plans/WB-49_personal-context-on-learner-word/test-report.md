# Test report: WB-49 Personal context on learner word

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
The `content-api` and `wordbuddy-ui` images were rebuilt first (`docker compose up -d --build`), because the running ones predated this branch.

## E2E UI

49 passed, 1 failed (`npm test` in `e2e/ui`), including both `vocabulary-personal-context.feature` scenarios.

## Failures

| # | Item | Cause | Action |
| --- | --- | --- | --- |
| 1 | UI: `support-links.feature` › Admin completes an escalated unlink request | Environment, not WB-49. Identity container lacks `SupportLinks__UnlinkOverrideWaitDays=0`. `WordBuddy/.env` sets it, but `docker-compose.yml` does not pass it to `identity-api`. Escalate returns 400 `SupportLink.EscalationTooEarly`. | Pass the variable to `identity-api` and re-run `/test`, or Sam accepts it as known. |

Test defects found and fixed in this run (test code only, no application code changed):

- `e2e/api/.../PersonalContextTests.cs`: read the note from `prompt.personalContext`, not the top level of the exercise.
- `e2e/ui/steps/vocabulary.steps.ts`, `vocabulary-autofill.steps.ts`, `vocabulary-sharing.steps.ts`: `getByLabel('Word')` matched the new "Where did you meet this word" field (5 scenarios broke). Now `{ exact: true }`.

## Known coverage gaps

- No Child UI E2E scenario for the "Only you" hint. No step sets up a Child with a linked supporter. Deferred (review round 2, finding 2).
- `npx tsc --noEmit` in `e2e/ui` fails on missing `@types/node`. Existed before this change.

## Merge guard

- Guard 1: `review.md` verdict Approve; reviewed commit `25d4e52` is an ancestor of HEAD. Pass.
- Guard 2: `git merge-tree` against `origin/main`, then diff to HEAD lists only allowlisted paths:
  - `.claude/plans/WB-49_personal-context-on-learner-word/proposal.md`
  - `.claude/plans/WB-49_personal-context-on-learner-word/review.md`
  - `docs/ai/learnings.md`

  Pass. (`origin/main` merge: already up to date.)
- Guard 3: all suites ran; the E2E UI suite has 1 failure. Fail.

## Verdict

Not merged — E2E UI suite has 1 failure (`support-links` unlink escalation, an environment gap unrelated to WB-49). Every WB-49 test passes.
