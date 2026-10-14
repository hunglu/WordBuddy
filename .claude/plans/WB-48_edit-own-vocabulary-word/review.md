# Review: WB-48 Edit own vocabulary word

PR: #52 · Round 2 · Reviewed commit: 72b21d1 · 2026-10-14T09:25:53+07:00

## Verdict

Approve — all round-1 findings are fixed; only nits remain, all on a rare race path.

## Round-1 findings

| # | Severity | Status | Evidence |
|---|---|---|---|
| 1 | major | Fixed | `UpdateAsync` catches `IsLexemeForeignKeyViolation`, re-gets the lexeme once, retries; second failure → 409 `ConcurrentAdd`. Integration test `VocabularyWordRepository_UpdateAsync_MissingLexeme_RecreatesLexemeAndSucceeds` passes. |
| 2 | nit | Fixed | Handler calls `DeleteLexemeIfOrphanedAsync(lexemeId)` on update failure when the lexeme moved; unit test updated. |
| 3 | nit | Fixed | `DeleteLexemeIfOrphanedAsync` catches `DbException`, logs the type only, returns `Success(false)`. |
| 4 | nit | Fixed | `EditWordForm` maps ProblemDetails `title` (= error code, `ResultExtensions.cs:25`) to `Sense.NotEditable` / `Duplicate` / `ConcurrentAdd` messages. |

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `Content.Infrastructure/Repositories/VocabularyWordRepository.cs:378` | Retry sets `entry.State = Modified` → every column is written, incl. `ShareStatus`. Scenario: lexeme vanishes and, in the same window, the user shares the word from another tab → retry writes `Private` back. Needs two races at once. | Set state `Unchanged`, then mark only `Word`, `Definition`, `Example`, `Examples`, `ContentHash`, `LexemeId`, `AudioAssetId` as modified. |
| 2 | nit | `UpdatePersonalVocabularyWordCommandHandler.cs:125`; repo `:345-381` | If the retry save fails (2601 or second 547), the lexeme created in the retry is left orphaned. The handler cleans only the first `lexemeId`. | In the handler, also clean `sense.LexemeId` when it differs from `lexemeId` and `oldLexemeId`. |
| 3 | nit | `Content.IntegrationTests/ConcurrentVocabularyAddTests.cs:135` | The retry test loads the sense directly. The handler loads it via `GetLinkAsync` (tracked `LearnerWord` + `Include(Sense)`), so detach/re-attach with a tracked link is not covered. | Load through `repository.GetLinkAsync(ownerId, created.Id)` in the test. |
| 4 | nit | `UpdatePersonalVocabularyWordCommandHandler.cs:134` | `DeleteLexemeIfOrphanedAsync` can no longer fail, so the `cleanup.IsFailure` branch is dead code. | Drop the branch, or keep it with a comment. |

Verified locally on 72b21d1:

| Check | Result |
|---|---|
| Unit (`UpdatePersonalVocabularyWord*`, `SenseTests`) | 58 passed |
| Integration (`ConcurrentVocabularyAddTests`, `PersonalVocabularyEditEndpointsTests`) | 15 passed |
| UI type-check (`tsc -b`) | clean |
| Logging / PII in new code | ids and exception type only |
| Migration / DB diagram | none needed |

Still open from round 1 (deviation 8): API + UI E2E not run green. The tester must run them on a fresh build.

## Plan conformance

- All tasks in `tasks.md` are in the diff; fix round 1 touched only the round-1 findings.
- Out of scope: nothing found.

## Previous rounds

### Round 1 — commit c00ea2f — 2026-10-14T07:48:37+07:00

Verdict: Changes requested — one major: a word-text edit can hit a vanished lexeme and return 500 instead of retrying, as `AddAsync` already does.

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `Content.Infrastructure/Repositories/VocabularyWordRepository.cs:331-348` (`UpdateAsync`); caller `UpdatePersonalVocabularyWordCommandHandler.cs:97,115` | `UpdateAsync` catches only 2601/2627. A word change sets `LexemeId` to a lexeme found by `GetOrCreateLexemeAsync`. If that lexeme's last sense is deleted in between, the save fails with SQL 547 on `FK_Senses_Lexemes_LexemeId` → 500. | Catch `IsLexemeForeignKeyViolation`, re-get the lexeme once, retry; second failure → `Conflict("PersonalVocabularyWord.ConcurrentAdd")`. Add a test. |
| 2 | nit | `UpdatePersonalVocabularyWordCommandHandler.cs:97,115-121` | On a 409 from `UpdateAsync` the new lexeme stays orphaned. | On failure with `lexemeId != oldLexemeId`, call `DeleteLexemeIfOrphanedAsync(lexemeId)`. |
| 3 | nit | `VocabularyWordRepository.cs:404-414`; handler `:126-134` | `DeleteLexemeIfOrphanedAsync` throws on a DB error → 500 for a saved edit. | Catch `DbException` and return a result. |
| 4 | nit | `WordBuddy.UI/src/components/vocabulary/EditWordForm.tsx:42-45` | Every 409 shows "You already have this word.", also for `Sense.NotEditable`. | Read the ProblemDetails `title` and map per code. |

Round-1 checks with no finding: auth / child vs adult, 403/404/409 mapping, guarded orphan delete, audio cleared on word change, LearnerWord / Progress ids kept, logging / PII, DI, line endings, no migration, frontend conventions. Coder deviations 1–7 accepted; 8 (E2E not run green) open.
