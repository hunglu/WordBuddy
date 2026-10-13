# Review: WB-47 Admin lesson authoring

PR: #51 · Round 1 · Reviewed commit: 7ecfdfa · 2026-10-13T21:54:25+07:00

## Verdict
Changes requested — one major: four new commands have no FluentValidation validator (CLAUDE.md rule). Logic, auth and child rules are correct.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `Content.Application/Features/Lessons/Commands/PublishLesson/PublishLessonCommandHandler.cs:22`, `UnpublishLesson/UnpublishLessonCommandHandler.cs:22`, `DeleteLesson/DeleteLessonCommandHandler.cs:22`, `DetachLessonSense/DetachLessonSenseCommandHandler.cs:22` | No validator. `WordBuddy/CLAUDE.md` requires FluentValidation for every command; on `main` every Content command has one. Scenario: a handler called with `Guid.Empty` (unit test, future caller) hits the DB and returns 404 instead of a 400 validation failure. | Add `<Command>Validator` (`LessonId` / `SenseId` `NotEmpty()`), inject `IValidator<T>`, register in DI, same shape as `AttachLessonSenseCommandValidator`. Optional for `GetAdminLessons` / `GetAdminLessonDetail` (existing lesson queries also have none). |
| 2 | nit | 11 files, e.g. `LessonsController.cs`, `Lesson.cs`, `LessonRepository.cs`, `WordBuddy.UI/src/types/index.ts` | Line endings changed CRLF → LF. `types/index.ts` shows 728 changed lines for 54 real ones; `LessonsController.cs` 62 for 2. Hides real changes, breaks `git blame`. | Restore original line endings (`git diff -w --ignore-cr-at-eol` shows the real diff). Consider a `.gitattributes` (separate change). |
| 3 | nit | `Content.Infrastructure/Repositories/LessonRepository.cs:83` | `SaveChangesAsync` does not catch `DbUpdateException`. Two admins attach the same sense at once → PK violation → 500 instead of 409. Other repositories (`GroupWordRepository`) map it. | Catch `DbUpdateException` (SQL 2601/2627) → `Error.Conflict("Lesson.SenseAlreadyAttached", ...)`. |
| 4 | nit | `WordBuddy.UI/src/components/admin-lessons/AddWordPicker.tsx:17` | Search query fires on every keystroke (no debounce). Typing "elephant" = 8 requests. | Debounce the search term (~300 ms) before it reaches the query key. |
| 5 | nit | `docs/features/lesson-authoring.md:20` | Spec says children see "only child-visible senses". Code (accepted deviation 2) shows system senses regardless of `VisibleToChildren`. Spec and code differ. | State the rule exactly: "shared-pool senses not cleared for children are hidden; system senses are always shown". |

### Coder deviations — judgement
| # | Deviation | Verdict |
|---|---|---|
| 1 | `GET /api/lessons/admin/senses?search=` | Accepted. Open question 5 allowed it. Under `AdminOnly`; search capped at 100 chars, 50 rows. |
| 2 | Child rule via `Sense.IsHiddenFromChildrenInLessons`; system senses never hidden | Accepted. `Shared` is terminal (no un-share path), so a word cannot leave the pool and lose its filter. Fix the spec wording (#5). |
| 3 | `VocabularyAutofillEndpointsTests` seed lesson published | Accepted. Needed by the new draft filter. |
| 4 | `GET /api/lessons/{id}` 404 for drafts, admin included | Accepted. Admins use `/api/lessons/admin/{id}`; same 404 prevents probing. |
| 5 | E2E API/UI written, not run; `e2e/ui` tsc missing node types | Tester to run in `/test`. Not a review finding. |
| — | Child-hidden sense attachable to child lesson with badge | Accepted (plan decision). Learner detail filters it. |

### Verified OK
- `LessonsAdminController`: class-level `[Authorize(Policy = "AdminOnly")]`; `POST /api/lessons` removed (integration test `LessonsController_Post_IsGone`).
- Attach eligibility: system or `Shared` only; duplicate → 409; non-vocabulary → 400.
- `DetachSense` renumbers 0..n-1; `ReorderSenses` rejects duplicates and partial sets.
- Delete: `Lesson → LessonSense` cascade, `LessonSense → Sense` NoAction; no migration needed.
- `GetLessons` / `GetLessonDetail`: drafts hidden, child gets only child-targeted lessons.
- UI: TanStack Query only, `isError` handled on both pages and picker, `wb-` tokens, Framer Motion `layout`, no `any`/`enum`.

## Plan conformance
- All tasks in `tasks.md` are reflected in the diff.
- Extra task (sense search) is from open question 5 — in scope.
- Out of scope: line-ending churn only (#2).
- No EF migration → no database diagram update needed.

## Previous rounds
None.
