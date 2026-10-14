# Review: WB-49 Personal context on learner word

PR: #53 · Round 2 · Reviewed commit: 25d4e52 · 2026-10-14T14:12:02+07:00

## Verdict
Approve — the line-ending churn is gone and all 6 nits are fixed or knowingly deferred; no new blocker or major.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `VocabularyWordRepository.cs:405-416`, `:429-440` | The two new error paths have no direct test: the "keep loser note" branch of `LinkAsync`, and the `DbUpdateConcurrencyException` → NotFound mapping of `SaveLinkAsync`. `ConcurrentIdenticalAddWithNote_KeepsNote` hits them only by chance. Failure: a later refactor breaks them silently. | `/test`: add an integration test per path (pre-insert the winning link, then call `LinkAsync`; delete the link, then call `SaveLinkAsync`). |
| 2 | nit | `e2e/ui/features/vocabulary-personal-context.feature` | Carried from round 1 (#7): no Child UI scenario; E2E API and UI suites not run. `tasks.md` now claims Adult coverage only, so the plan is honest. | `/test`: run both suites; add the Child hint scenario once a "child with linked supporter" step exists. |

## Round 1 fixes — verified

| R1 # | Result | Evidence |
|---|---|---|
| 1 major — EOL churn | Fixed | `git diff --shortstat origin/main...25d4e52` = `--ignore-cr-at-eol` = 50 files, +1738/−40. `git ls-files --eol`: new files LF; files CRLF on `main` stay CRLF. |
| 2 — duplicated block | Fixed | `VocabularyWordRepositoryExtensions.ApplyNoteToExistingLinkAsync` used by the 3 handlers. Both failure logs carry `{ErrorCode}` only. |
| 3 — `SaveLinkAsync` 500 | Fixed | `VocabularyWordRepository.cs:429` catches `DbUpdateConcurrencyException`, detaches, returns `PersonalVocabularyWord.NotFound` → 404. |
| 4 — race drops note | Fixed | `LinkAsync` detaches the loser, reloads the winner tracked, sets the note only when the winner has none. A stored note is never overwritten. |
| 5 — auto-fill reuses note | Fixed | `AutofillResults` `onAdded` → `setValue('personalContext', '')` in `VocabularyBuilderPage.tsx:151`. |
| 6 — untrimmed length | Fixed | 4 validators use `Trim().Length <= 500`; message has no note text. New test `PaddedTextWithin500AfterTrim_Succeeds`. |
| 7 — Child E2E | Deferred | See finding 2. |

## Checked with no finding
- **Privacy:** no new log line, `Error` message or validation message contains the note. New logs carry `UserId`, `WordId`, `ErrorCode` only. Reads are unchanged from round 1 (note only in `ToDto(LearnerWord, AgeGroup)`, caller-scoped).
- **Child vs adult:** no policy or flow change. A child's awaiting-approval link can receive the race note; it is the child's own link, so no leak.
- **Conventions:** `Result` everywhere, no `var` with unclear type, XML docs on the new public extension, structured logging, no `.Result`/`.Wait()`.
- **Builds/tests (re-run by reviewer):** Content unit tests 365/365 pass; `tsc -b` passes. Integration tests not re-run (140 pass per `/code`).
- **Database:** no migration, so no diagram change.

## Plan conformance
- Backend 1–13, frontend 14–21, unit and integration tests: all present.
- E2E API and UI: written, not run (finding 2).
- Out-of-scope code: none. The extension class and the race/concurrency handling come from round 1 findings.

## Previous rounds

### Round 1
PR: #53 · Round 1 · Reviewed commit: c0aca60 · 2026-10-14T13:55:57+07:00

#### Verdict
Changes requested — the logic and privacy are sound, but the commit converts about 30 existing files from LF to CRLF.

#### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `WordBuddy.UI/src/types/index.ts` (whole file), `PersonalVocabularyController.cs`, `PersonalVocabularyWordMapper.cs`, `LearnerWord.cs`, the 3 add handlers/validators/commands, `useVocabulary.ts`, `useAutofill.ts`, `AutofillResults.tsx`, `VocabularySharedPoolPage.tsx`, 5 test files | Line endings changed from LF (main) to CRLF on every touched existing file. The PR shows +4208/−2628; the real change is +1620/−40 (`git diff --ignore-cr-at-eol`). Failure: `git blame` is lost on these files, and every open branch that touches them (for example WB-50 on Content and `types/index.ts`) gets whole-file merge conflicts. New files are also CRLF, unlike the rest of the repo. | Convert all files in this diff back to LF (for example `dos2unix <files>`), then check that `git diff --stat origin/main...` matches the `--ignore-cr-at-eol` numbers. Check the editor / `core.autocrlf` setting that caused it. |
| 2 | nit | `AddPersonalVocabularyWordCommandHandler.cs:134`, `AddSharedVocabularyWordToMyListCommandHandler.cs:69`, `AddAutofillSenseToMyListCommandHandler.cs:55` | The same 15-line "existing link + non-blank note → set + save" block appears 3 times. Its failure paths do not log, unlike the rest of each handler. | Extract one helper (for example a small application service or an extension on `IVocabularyWordRepository`), and log `{ErrorCode}` on failure. |
| 3 | nit | `VocabularyWordRepository.cs:409` | `SaveLinkAsync` has no catch. Failure: the learner deletes the word in one tab while saving a note in another → `DbUpdateConcurrencyException` → 500 instead of 404. | Catch `DbUpdateConcurrencyException` and return `Error.NotFound("PersonalVocabularyWord.NotFound", ...)`. |
| 4 | nit | `VocabularyWordRepository.cs:396` (`LinkAsync`) | When a concurrent request created the same link first, the duplicate is detached and success is returned. A note on the losing request is silently dropped. Rare double-click case. | Accept and document, or on the unique violation reload the link and apply the note when the stored one is null. |
| 5 | nit | `VocabularyBuilderPage.tsx:147` / `:175`, `AutofillResults.tsx:49` | The note field sits below the auto-fill cards, so a learner may click "Add" on a sense before typing the note. After an auto-fill add the note stays in the form, so the next add reuses it. | Move `PersonalContextField` above `AutofillResults`, or reset the `personalContext` field after a successful auto-fill add. |
| 6 | nit | `UpdatePersonalContextCommandValidator.cs:12` (and the 3 add validators) | `MaximumLength(500)` runs on the untrimmed text; the domain trims. 495 chars + 10 spaces → 400, although the stored text fits. The UI trims and caps at 500, so only API clients see it. | Validate `c.PersonalContext?.Trim()` with `Must(...)`, or leave the domain as the only length check. |
| 7 | nit | `e2e/ui/features/vocabulary-personal-context.feature:14` | Task "Child sees the 'Only you' hint" is ticked, but the scenario runs as an Adult; the Child variant is not written. The E2E API and UI tests have not been run. | `/test`: add the Child scenario and run both suites. |

Verified with no finding:

- **Privacy:** no log line, validation message or `Error` carries the note; only `HasPersonalContext={bool}` is logged. There is no HTTP body logging in the services.
- **Scope of reads:** the note is set only in `ToDto(LearnerWord, AgeGroup)`. Its callers ("mine", recall check) and `GetSensesByIds` all use the JWT user id. The pool, moderation and child-approval DTOs use `Sense`, so they never carry it. Progress exercises are caller-scoped.
- **Child vs adult:** the same rules for both, and no policy change. `ChildHasSupporter` still guards the endpoints. A child's awaiting-approval link returns the note but hides the sense content.
- **Tracking:** `GetLinkAsync` uses `AsTracking()`, so `SaveLinkAsync` persists the change.
- **Optional body:** `EmptyBodyBehavior.Allow` keeps no-body calls working. Integration tests cover both cases.
- **UI rules:** TanStack Query only, no `any`/`enum`, `wb-` tokens only, Framer Motion for the editor, and `isError` handled on every new mutation. `tsc -b` passes.
- **Database:** no migration, so no diagram change is needed.

#### Plan conformance
- Backend tasks 1–13: all present in the diff.
- Frontend tasks 14–21: all present.
- Unit and integration tests: present (364 unit / 140 integration passing, per `/code`).
- E2E API test: written, not run.
- E2E UI test: written, not run, and the Child-hint scenario is missing (finding 7).
- Out-of-scope code: none. The `addWord.isError` and `addToMyList.isError` messages are small additions that match the UI rules.
- Out-of-scope side effect: the line-ending conversion (finding 1).

#### Fix round 1 — response

| # | Status | What was done |
|---|---|---|
| 1 | Fixed | Every touched file now has the EOL it has on `main`: existing LF files stay LF, existing CRLF files (7 UI/Content files that were already CRLF on `main`) stay CRLF, new files are LF. `git diff --shortstat main`: 1697 ins / 40 del (tracked files; the new extension file is untracked); with `--ignore-cr-at-eol`: same. No `.gitattributes`, no git config change. |
| 2 | Fixed | New `VocabularyWordRepositoryExtensions.ApplyNoteToExistingLinkAsync` replaces the 3 copies. Failure paths log `{ErrorCode}` only, never the note. |
| 3 | Fixed | `SaveLinkAsync` catches `DbUpdateConcurrencyException`, detaches the link and returns `Error.NotFound("PersonalVocabularyWord.NotFound")`. |
| 4 | Fixed | On a unique violation in `LinkAsync`, the winning link is reloaded; if it has no note, the losing request's note is saved on it. A stored note is never overwritten by the race. |
| 5 | Fixed | `AutofillResults` has an `onAdded` callback; the add form clears the note field after an auto-fill add. |
| 6 | Fixed | The 4 validators check `Trim().Length <= 500`. New unit test: 10 spaces + 495 chars succeeds. |
| 7 | Deferred | No Child scenario. A child needs a linked supporter to reach My Vocabulary, and no existing UI E2E step sets that up. `tasks.md` line reworded to claim Adult coverage only. E2E suites still not run (`/test`). |
