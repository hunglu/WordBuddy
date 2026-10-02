# Test report: Refactor database scheme to store vocabulary item

PR #11 · branch `feature/refactor-database-scheme-to-store-vocabulary-item` · 2026-10-02
Tested HEAD `d55fb04`, already up to date with `origin/main`. Reviewed commit: `6944cd4`.

The integration tests ran against the Docker SQL Server (`localhost,1433`), using throwaway
databases `WordBuddyContentTests_*` and `WordBuddyProgressTests_*`. They did not run on LocalDB.

## Backend unit/integration

| Suite | Passed | Failed | Skipped | Notes |
|---|---|---|---|---|
| WordBuddy.Content.UnitTests | 69 | 0 | 0 | |
| WordBuddy.Content.IntegrationTests | 40 | 0 | 0 | Includes `UnifyVocabularyWordsMigrationTests` (Up/Down, rows I/J/K child rule) and `ConcurrentVocabularyAddTests` |
| WordBuddy.Progress.UnitTests | 49 | 0 | 0 | |
| WordBuddy.Progress.IntegrationTests | 5 | 0 | 0 | Includes `SyncVocabularyWordIdRemapsTests` |

## E2E API

`dotnet test e2e/api/WordBuddy.E2E.Api.Tests` ran against the live compose stack, which was built
from this branch: **10 passed, 0 failed.**

- `PersonalVocabularyTests`: 4 tests, including `AdoptSharedWordThenDelete_WordStaysInSharedPool`
  and `AddRetrieveShareAndModerate_WordAppearsInSharedPool`.
- `InternalEndpointExposureTests`: 2 tests. `/internal/vocabulary-remaps` sent through `:3000` does
  not reach Content.
- `HealthCheckTests`: 4 tests.

A second run of `PersonalVocabularyTests` against the same DB also passed, **4/4**.

## E2E UI

`npx bddgen && npx playwright test` (chromium): **15 passed, 0 failed.** This covers login,
navigation, theme and the vocabulary scenario.

## Verified in /test

These read-only queries ran against the dev `WordBuddyContent` and `WordBuddyProgress` databases:

| Check | Result |
|---|---|
| Latest Content migration | `20261002092015_UnifyVocabularyWords` |
| `VocabularyItems` / `PersonalVocabularyWords` exist | 0 / 0 (dropped) |
| `VocabularyWordIdRemaps` total / `PublishedAtUtc IS NULL` | 1 / **0** |
| Remaps whose `NewId` is not a word / `OldId` still a word | 0 / 0. Every old row resolves to a word, either by its own id or through a remap (criterion a) |
| Words by source (after the e2e runs) | System 4 (= 4 lesson links), Learner 18 |
| Learner words without an author link | 0 |
| Duplicate `(ContentHash, OwnerUserId)` among Learner words | 0 (criterion b) |
| Private/Rejected learner words duplicating a system word or another owner's shared word | 0 (criterion b) |
| Progress `VocabularyRecallStats` still on a remapped `OldId` | 0 of 8 (criterion f; the live sync reported `Rewritten=0, Merged=0`) |

There is no Redis container in the stack, so there was nothing to flush.

## Merge guard

1. `review.md` round 2 is Approve, and the reviewed commit `6944cd4` is an ancestor of HEAD. **Pass.**
2. `git merge-tree --write-tree 6944cd4 origin/main` gave tree `f23bcda` with no conflict. Diff to HEAD:
   ```
   .claude/plans/refactor-database-scheme-to-store-vocabulary-item/proposal.md
   .claude/plans/refactor-database-scheme-to-store-vocabulary-item/review.md
   ```
   Every path is on the allowlist. **Pass.** This run's commit adds only `test-report.md`,
   `proposal.md` and `docs/features/**`.
3. Every suite ran in this session with 0 failures and 0 skipped. **Pass.**

## Failures

None.

## Verdict

Approved — merged into main
