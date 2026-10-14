# Review: WB-48 Edit own vocabulary word

PR: #52 · Round 1 · Reviewed commit: c00ea2f · 2026-10-14T07:48:37+07:00

## Verdict

Changes requested — one major: a word-text edit can hit a vanished lexeme and return 500 instead of retrying, as `AddAsync` already does.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `Content.Infrastructure/Repositories/VocabularyWordRepository.cs:331-348` (`UpdateAsync`); caller `UpdatePersonalVocabularyWordCommandHandler.cs:97,115` | `UpdateAsync` catches only 2601/2627. A word change sets `LexemeId` to a lexeme found by `GetOrCreateLexemeAsync`. If that lexeme's last sense is deleted in between (`DeleteIfOrphanedAsync` / `DeleteLexemeIfOrphanedAsync` delete it), the save fails with SQL 547 on `FK_Senses_Lexemes_LexemeId` → unhandled → 500. Scenario: A edits "cat" → "dog" while B deletes their only private "dog". `AddAsync` (line 266) handles this exact race. | In `UpdateAsync`, catch `IsLexemeForeignKeyViolation`, call `GetOrCreateLexemeAsync(word.Word)` once, set `LexemeId`, retry; on a second failure return `Conflict("PersonalVocabularyWord.ConcurrentAdd")`. Add an integration or unit test for the retry path. |
| 2 | nit | `UpdatePersonalVocabularyWordCommandHandler.cs:97,115-121` | On a 409 from `UpdateAsync` (race on the unique hash index) the new lexeme created by `GetOrCreateLexemeAsync` is already saved and stays orphaned. Task says "no orphan lexemes left". | On update failure with `lexemeId != oldLexemeId`, call `DeleteLexemeIfOrphanedAsync(lexemeId)` (guarded, safe). |
| 3 | nit | `VocabularyWordRepository.cs:404-414`; handler `:126-134` | `DeleteLexemeIfOrphanedAsync` never returns a failure; a DB error throws. The handler comment ("edit is saved; leftover lexeme is harmless") then does not apply — the client gets 500 for a saved edit. | Wrap the SQL in `try/catch (DbException)` and return `Result.Failure`, or catch in the handler. |
| 4 | nit | `WordBuddy.UI/src/components/vocabulary/EditWordForm.tsx:42-45` | Every 409 shows "You already have this word." A 409 `Sense.NotEditable` (word shared from another tab) gets the wrong message. | Read the ProblemDetails `title`; show "This word can no longer be edited." for `Sense.NotEditable`. |

Checks with no finding:

| Check | Result |
|---|---|
| Auth / child vs adult | Controller keeps `[Authorize]` + `ChildHasSupporter`; ownership is the only gate; same for Child and Adult (unit `Theory` covers both). |
| 403 / 404 / 409 mapping | `GetLinkAsync` → 404 (no probing); `Sense.Forbidden` → 403 via the existing suffix rule in `ResultExtensions`; status ≠ Private → 409. |
| Orphan lexeme delete | Guarded in one SQL statement (`NOT EXISTS` on `Senses`), so atomic; runs after the save, never deletes a referenced lexeme. |
| Audio on word change | `AudioAssetId`/`Audio` cleared only when the normalized word changes; image kept. |
| LearnerWord / Progress | Same sense id; events carry ids only; Progress `VocabularyRecallStat.Word` refreshes on the next check by design. |
| Logging / PII | Logs carry ids and error codes only; no word text, no child data. |
| Validator in DI | Handler + validator registered. |
| Line endings | No whole-file rewrites (`--stat` matches the hunks). |
| Migration / DB diagram | No migration, no diagram change needed. |
| Frontend conventions | TanStack mutation, `wb-` tokens, Framer Motion, RHF + Zod, `isError` handled, no `any`/`enum`. |

Coder's deviations:

| # | Deviation | Judgement |
|---|---|---|
| 1 | 403 via `Error.Failure("Sense.Forbidden")` | Accepted — uses the existing `ResultExtensions` suffix rule. |
| 2 | `Sense.CheckEditable` | Accepted — checks before any lexeme save; domain method re-guards. |
| 3 | No-op only on exact match | Accepted — a case-only fix is a valid edit. |
| 4 | Duplicate lookup only on hash change; no merge | Accepted — matches open question 3. |
| 5 | Payload type in `api/vocabulary.ts` | Accepted — sits next to `AddVocabularyWordPayload`. |
| 6 | Separate `EditWordForm.tsx` | Accepted. |
| 7 | UI e2e uses a PendingReview word | Accepted — same UI rule (`shareStatus === 'Private'`). |
| 8 | E2E not run green | Open — the tester must run API + UI E2E on a fresh build. |

## Plan conformance

- All tasks in `tasks.md` are in the diff (domain, command, validator, handler, repo, DI, endpoint, UI, unit, integration, E2E).
- Open questions answered by the implementation: Rejected → 409; audio cleared, image kept; no auto-merge; 204.
- Out of scope: nothing found.
