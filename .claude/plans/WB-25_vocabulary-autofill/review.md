# Review: WB-25_Vocabulary autofill

PR: #37 · Round 1 · Reviewed commit: e7a5232 · 2026-10-09T10:39:31+07:00

## Verdict
Approve — no blockers or majors; child visibility holds on every read path, no secrets or child data leave the service.

## Focus checks

| Check | Result |
| --- | --- |
| Child never sees unapproved sense: `mine`, recall check, `shared`, lesson detail, `GetSensesByIds` (Progress), lookup | OK — `IsAwaitingChildApproval` / `IsVisibleTo(..., link)` on each path; lookup masks per caller after cache read; repository failure masks all |
| Shared pool for child | OK — `childSafeOnly` filters on `VisibleToChildren`, auto-fill senses start `false` |
| Secrets | OK — `appsettings.json` has no key; key read from `Autofill:Claude:ApiKey` (user-secrets/env); never logged |
| Data sent outside | OK — typed word + System/Shared catalog definitions only (Sam approved); private senses filtered in query and writer |
| Logs | OK — no word, definition or token in log templates; only codes, counts, ids |
| ADR 0004 lemma | OK — `RederiveLemma` from senses on that lexeme before save; DTOs show `Sense.Word` |
| Approve auth | OK — `AdultOrAdmin` + `CanSupportLearner` (supporter), `AdminOnly` (admin); child → 403 |
| DB diagram | OK — `docs/database-diagram/content.md` updated with the migration |

## Coder decisions

| Decision | Judgement |
| --- | --- |
| `LearnerWord.RequiresChildApproval` | Accept — keeps queues exact and cheap to query |
| `Sense.ChildSuitableHint` | Accept — matches plan open question 4; shown to approvers only |
| Child lookup hides content | Accept — needed; matches plan |
| Recall-check filter | Accept — child never drills an unapproved word |
| Negative cache only on not-found | Accept — transient failures must not block for 1 h |
| No Claude retry, 15 s / 5 s timeouts | Accept — user-facing call, failure falls back to manual form |
| Audio failure non-blocking | Accept, see nit 1 |
| `AdultOrAdmin` policy | Accept — needed to stop a child supporter path |
| GET query writes to DB | Accept with nit 2 |

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | Application/Features/Autofill/AutofillCatalogWriter.cs:174 | MP3 files are written before the DB transaction. On a concurrent-save conflict or DB failure the files stay with no `MediaAsset` row (orphans). | Delete stored files when `SaveAsync` fails, or add a later cleanup job. |
| 2 | nit | Application/Features/Autofill/Queries/LookupAutofill/LookupAutofillQueryHandler.cs:127 | A `GET` query persists catalog rows. Works and is idempotent, but breaks the read-only query convention. | Document in the living spec; consider a command later. |
| 3 | nit | Application/Features/Lessons/Queries/GetLessonDetail/GetLessonDetailQueryHandler.cs:278 | Lesson detail passes `callerLink: null`, so a supporter-approved word in a lesson stays masked for that child. Safe side, but inconsistent with `mine`. | Accept for v1 (admin approval is global); note in spec. |
| 4 | nit | Application/Features/Autofill/Queries/LookupAutofill/LookupAutofillQueryHandler.cs:186 | Child mask silently masks all senses when `GetForReviewAsync` fails, with no log. | Log a warning with the error code. |

## Plan conformance
- All backend, frontend and unit/integration tasks are in the diff.
- 2 E2E tasks moved to `/test` by Sam — not a finding.
- Out of scope: `IFileStorageService.SaveNamedAsync`, UnitTests → Infrastructure reference (for client parsing tests). Both justified.
