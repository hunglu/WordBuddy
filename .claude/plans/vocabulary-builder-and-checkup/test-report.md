# Test report: Vocabulary Builder & Recall Check-up

Run on `feature/vocabulary-builder-and-checkup` (2026-10-02), PR #10, against the live
docker compose stack, which was rebuilt from this branch. `origin/main` was already merged in.
No new test files were written in this run. The existing suites already cover the feature and
the fixes made in this PR.

## Backend unit/integration

- `WordBuddy.Content.UnitTests`: 26 passed, 0 failed
- `WordBuddy.Progress.UnitTests`: 10 passed, 0 failed
- `WordBuddy.Content.IntegrationTests`: 4 passed, 0 failed
- `WordBuddy.Progress.IntegrationTests`: 2 passed, 0 failed

The integration tests ran against the docker SQL Server (`localhost,1433`), each in a new
throwaway DB. They got their connection strings from `CONTENT_TEST_CONNECTION_STRING` /
`PROGRESS_TEST_CONNECTION_STRING`. The SA password came from the container's environment and was
never printed. They also passed on the LocalDB fallback.

## E2E API

`dotnet test e2e/api/WordBuddy.E2E.Api.Tests`: 7 passed, 0 failed. This covers
`HealthCheckTests` for Identity, Content, Quiz and Progress (now green because of the `/health`
fix in this PR) and `PersonalVocabularyTests` (3).

## E2E UI

`npx bddgen && npx playwright test`: 15 passed, 0 failed

- `login.feature`: 1/1 passed (the nginx proxy fix in this PR works live)
- `vocabulary.feature`: 1/1 passed (add a word, see it in the list, complete a recall check)
- `theme.feature`: 5/5 passed
- `navigation.feature` (from the merged #5 fix): 8/8 passed. This is the first live pass for
  these scenarios: 6 per-path selected-state outlines, My Vocabulary to Shared Pool, and the admin
  on Moderation.

## Merge guard

1. `review.md` round 3: Approve, reviewed commit `7c1ca69`. It is an ancestor of HEAD.
2. `git merge-tree --write-tree 7c1ca69 origin/main` gave no conflict. The diff from that tree to
   HEAD, before this run's commit, contained only:
   ```
   .claude/plans/vocabulary-builder-and-checkup/proposal.md
   .claude/plans/vocabulary-builder-and-checkup/review.md
   ```
   Both are on the allowlist. This run adds only `test-report.md`, `proposal.md` and
   `docs/features/**`.
3. All suites ran in this session with 0 failures and 0 skipped.

## Failures

None.

## Verdict

Approved — merged into main
