# Review: WB-25_Vocabulary autofill

PR: #37 · Round 2 · Reviewed commit: 55ce542 · 2026-10-09T15:01:58+07:00

## Verdict
Approve — fake auto-fill clients are gated by Development **and** an opt-in flag set only in the e2e override; no network in fakes.

## Focus checks (round 2)

| Check | Result |
| --- | --- |
| Fakes outside Development | Impossible — `UseFakeClients = isDevelopment && Autofill:UseFakeClients`; `Program.cs` passes `Environment.IsDevelopment()`; default `false` |
| Tests for the switch | OK — `AutofillClientSwitchTests` covers flag on/off × Dev/non-Dev |
| Network in fakes | None — in-memory data; audio URLs use `.invalid` TLD and `DownloadAsync` returns fixed bytes |
| Base compose / appsettings / k8s | Unchanged — flag only in `e2e/docker-compose.e2e.yml` |
| Tester commit 7746ccd | Test files, report, learnings only — no app code |

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | e2e/ui/steps/vocabulary-autofill.steps.ts:14,43 | UI happy path stubs both `/autofill` and `add-to-mine` with `page.route`. It never hits Content, so catalog save, DTO shape and `mine` refresh are untested end to end. | In `/test`, drop the stubs and use the fake-backed stack (word `serendipity`). Keep a stub only for the failure path if needed. |
| 2 | nit | WordBuddy/docker-compose.yml:96, k8s/configmap.yaml:7 | Base compose and kind both run `Development`, so the environment gate alone does not protect the local cluster; the flag is the real guard. Safe today. | When a non-Dev environment is added, keep the flag out of its config. No change now. |
| 3–6 | nit | — | Round 1 nits 1–4 still open (orphan MP3, GET writes, lesson link, silent mask). | Optional. |

## Plan conformance
- Fix round 1 adds test infrastructure only (fake clients + switch). Out of plan, but justified by E2E needs.
- All plan tasks still covered.

## Previous rounds

### Round 1
PR: #37 · Round 1 · Reviewed commit: e7a5232 · 2026-10-09T10:39:31+07:00

### Verdict
Approve — no blockers or majors; child visibility holds on every read path, no secrets or child data leave the service.

### Focus checks

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

### Coder decisions

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

### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | Application/Features/Autofill/AutofillCatalogWriter.cs:174 | MP3 files are written before the DB transaction. On a concurrent-save conflict or DB failure the files stay with no `MediaAsset` row (orphans). | Delete stored files when `SaveAsync` fails, or add a later cleanup job. |
| 2 | nit | Application/Features/Autofill/Queries/LookupAutofill/LookupAutofillQueryHandler.cs:127 | A `GET` query persists catalog rows. Works and is idempotent, but breaks the read-only query convention. | Document in the living spec; consider a command later. |
| 3 | nit | Application/Features/Lessons/Queries/GetLessonDetail/GetLessonDetailQueryHandler.cs:278 | Lesson detail passes `callerLink: null`, so a supporter-approved word in a lesson stays masked for that child. Safe side, but inconsistent with `mine`. | Accept for v1 (admin approval is global); note in spec. |
| 4 | nit | Application/Features/Autofill/Queries/LookupAutofill/LookupAutofillQueryHandler.cs:186 | Child mask silently masks all senses when `GetForReviewAsync` fails, with no log. | Log a warning with the error code. |

### Plan conformance
- All backend, frontend and unit/integration tasks are in the diff.
- 2 E2E tasks moved to `/test` by Sam — not a finding.
- Out of scope: `IFileStorageService.SaveNamedAsync`, UnitTests → Infrastructure reference (for client parsing tests). Both justified.
