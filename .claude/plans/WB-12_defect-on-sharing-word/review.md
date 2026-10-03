# Review: WB-12_Defect on sharing word

PR: #13 · Round 1 · Reviewed commit: 9a6b245 · 2026-10-03T17:55:30+07:00

## Verdict
Changes requested — backend is correct and complete; one major: a failed delete in the UI closes the dialog with no message.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `WordBuddy.UI/src/pages/VocabularyBuilderPage.tsx:44` (also `:39`) | `deleteWord.isError` is never rendered. On a confirmed delete, `onSettled` closes the dialog on error too. Scenario: the word was shared in another tab, so the plain delete gets 409; or Content is down (502). The click does nothing and the user sees no message. Breaks the UI rule "handle `isError`". | Show an error line when `deleteWord.isError` (theme token `text-wb-danger`, like line 132). Close the dialog only `onSuccess`; on error keep it open and show the message inside it. On a 409 from the plain path, open the confirm dialog. |
| 2 | nit | `DeletePersonalVocabularyWordCommandHandler.cs` (cache block after `UnlinkAsync`) | `RemoveAsync` runs after the DB commit. If Redis fails, the request returns 500 while the transfer is already saved; a retry then returns 404. | Wrap the two removals in try/catch, log a `Warning`, return success (the 5-minute expiry covers it). Same pattern as the moderation handler if it has one. |
| 3 | nit | `DeleteWordConfirmDialog.tsx:30` | No Escape-to-close and no click-outside-to-close; focus is not trapped in the dialog. | Add `onKeyDown` Escape → `onCancel`, and backdrop `onClick` → `onCancel` (stop propagation on the panel). |
| 4 | nit | ~20 files, e.g. `WordBuddy.UI/src/types/index.ts`, `docker-compose.yml`, `VocabularyWord.cs` | Line endings changed from LF (main) to CRLF. The diff shows 4521+/5365− lines; the real change is 1583+/2427−. Hard to review and causes blame noise. | Restore LF (`git add --renormalize .` with a `.gitattributes` `* text=auto eol=lf`) in a later commit. |

Checked, no finding:

| Area | Result |
|---|---|
| 409 confirm contract | Author + `Shared`/`PendingReview` + no confirm → `Conflict` `PersonalVocabularyWord.DeleteConfirmationRequired`; detail text has only the status. `?confirm` defaults to `false`. |
| Transfer | `TransferToSystem` only from `Learner`+`Shared`; tracked `GetByIdAsync` (`AsTracking`) so `UnlinkAsync` saves both in one `SaveChanges`. `DeleteIfOrphanedAsync` skipped. |
| PendingReview | `CancelShareRequest` → `Private`, unlink, `DeleteIfOrphanedAsync`. |
| Child safety | `IsVisibleTo`: a `Shared` word always uses the child filter, also when `System`. `PickVisibleDuplicate` system pick now needs `IsVisibleTo`. `GetSharedAsync` filters on `ShareStatus` only, so transferred words stay in the pool with the `VisibleToChildren` filter. |
| `isMine` | Set after cache read and after cache write; cached list never holds a per-user value. Old cache entries deserialize to `false`. |
| Cache | Both pool keys removed after a transfer; keys in one `SharedVocabularyCacheKeys` class. |
| Migration | `Up` drops the table; `Down` recreates PK + `PublishedAtUtc` index; snapshot updated. |
| Remap removal | `git grep -i "remap\|ContentApi__\|wb_service\|InternalService"`: only README note, migrations, and the pinned `UnifyVocabularyWords` migration test remain. Compose and k8s cleaned. |
| UI conventions | Union types, TanStack Query, Framer Motion, `wb-` tokens only, no inline style. |

## Plan conformance
All tasks in `tasks.md` are in the diff. No out-of-scope code changes. e2e API and UI tests are written but not run (need full stack) — the tester runs them.
