# Review: WB-23_Vocabulary review exercises

PR: #34 · Round 1 · Reviewed commit: 0c3ca1f · 2026-10-07T22:49:25+07:00

## Verdict
Approve — no blockers or majors; the visibility filter, the migration and the UI flow match the plan.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `Sense.cs`, `VocabularyWordRepository.cs`, `PersonalVocabularyController.cs`, `ServiceCollectionExtensions.cs`, `IVocabularyWordRepository.cs`, `SenseConfiguration.cs`, `AppLayout.tsx`, `ProgressPage.tsx`, `VocabularyBuilderPage.tsx` | Whole-file line-ending churn (about 1,800 lines). `git diff -w --ignore-cr-at-eol` shows only small real changes. It hides the real diff and breaks `git blame`. | Restore the original line endings; add `.gitattributes` rules if the editor keeps converting them. |
| 2 | nit | `VocabularyWordRepository.cs:125-140` | `Include(Audio/Image)` sits before a `Select` into `SenseReviewCandidate`. No test checks that `audioUrl`/`imageUrl` come back filled. The UI E2E fakes both URLs, so a lost include would go unnoticed. | Tester: add an integration assertion that a sense with audio and image returns both URLs. |
| 3 | nit | `ListeningChoiceExercise.tsx:39` | The emoji in the "Play" label is read aloud by screen readers. | Add `aria-label="Play the word"` or an icon with `aria-hidden`. |
| 4 | nit | `VocabularyReviewPage.tsx:99-101` | The queue is built once from `senses` at mount. A later `senses` refetch (for example, Retry) does not rebuild it. Low impact because `refetchOnWindowFocus` is off. | Accept as is, or key `ReviewSession` on `senses.dataUpdatedAt` as well. |

Checked and fine:
- `GET /api/vocabulary/senses`: class-level `[Authorize]`, the ids come from the query string, and the user and age group come from the JWT.
- Hidden and unknown ids get the same 200. `personalContext` comes only from the caller's own link.
- Logs contain counts only. One query, no N+1. Validation in the handler follows the repo pattern.
- `AddSenseImage`: nullable FK, `NoAction`. The snapshot and `docs/database-diagram/content.md` match.
- UI: TanStack Query only, `isError` with retry, no `any`/`enum`, `wb-` tokens only, Framer Motion only.
- Child: the 10-minute notice and 15-minute stop apply only to child accounts. Content filters out senses children may not see.

## Plan conformance
- All 27 tasks are in the diff.
- Coder decisions not in the plan, all acceptable:
  - Re-queue cap of 3 so that a session always ends.
  - Exercise level tracked per presentation.
  - Hooks moved to `src/hooks`.
  - `VocabularyCheckPage` and its store deleted, with `/vocabulary/check` redirecting to the new page.
  - `PersonalContext` set through reflection in tests.
- E2E (API and UI) compiles but has not run because the stack was down. `/test` must run it before merge.
- The UI E2E fakes `imageUrl`/`audioUrl` and injects a hidden sense id. Both are acceptable test doubles; finding 2 covers the real-URL gap.
- The trimmed `vocabulary.feature` scenario matches the removal of the recall check.
