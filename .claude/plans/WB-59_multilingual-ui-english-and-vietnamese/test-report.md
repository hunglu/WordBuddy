# Test report: WB-59_Multilingual UI (English and Vietnamese)

## Run 2 (latest)

Branch `feature/WB-59_multilingual-ui-english-and-vietnamese`, HEAD `8f5c02e` (`origin/main` merged: already up to date). Docker compose stack live: Identity 5080, Content 5081, Quiz 5082, Progress 5083, Notification 5084 (all `/health` 200), UI 3000. Migration `AddPreferredLanguage` applied.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. `review.md` Approve, reviewed sha `b1c9b99` is an ancestor of HEAD | Pass |
| 2. Files changed since review are on the allowlist | Pass |
| 3. Every suite ran with zero failures | Pass |

Guard 2 output (`git diff --name-only <merge-tree base> HEAD`):

```
.claude/plans/WB-59_.../proposal.md
.claude/plans/WB-59_.../review.md
.claude/plans/WB-59_.../test-report.md
e2e/api/WordBuddy.E2E.Api.Tests/PreferredLanguageTests.cs
e2e/ui/features/language.feature
e2e/ui/playwright.config.ts
e2e/ui/steps/language.steps.ts
```

This run's commit adds only `e2e/**`, the plan files, and `docs/features/**` (all allowlisted).

## Backend unit/integration

| Suite | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| Identity.UnitTests | 238 | 0 | 0 |
| Identity.IntegrationTests (`(localdb)\mssqllocaldb`) | 51 | 0 | 0 |

## E2E API

`dotnet test e2e/api/WordBuddy.E2E.Api.Tests`: 93 passed, 0 failed, 0 skipped. Includes `PreferredLanguageTests` (Adult and Child, malformed code `400`, anonymous `401`).

## E2E UI

`npm test` in `e2e/ui`: 61 passed, 0 failed, 0 skipped. Includes all 5 `language.feature` scenarios.

First full run: 57 passed, 4 failed. All four were stale e2e expectations, fixed in e2e/ only (no app code touched):

| Test | Cause | Fix |
| --- | --- | --- |
| `vocabulary-edit` (in review), `vocabulary-sharing` x2 | The share badge showed the raw enum `PendingReview`. WB-59 now shows the translated label `Pending review`. | Steps expect `Pending review` |
| `vocabulary-challenges` (Weekly League) | Test data: the shared database already holds `30 of 30` learners in Weekly League, so the Join button is disabled. Not a WB-59 change. | Feature uses `Word Climber` (1 of 30) |

## Failures

None remaining. See the fixed items above.

## Verdict

Approved — merged into main.

---

## Run 1 (history): not merged, E2E skipped

Branch `feature/WB-59_multilingual-ui-english-and-vietnamese`, HEAD `6a5aa33` (after merging `origin/main`: already up to date).

### Merge guard

| Guard | Result |
| --- | --- |
| 1. `review.md` Approve, reviewed sha `b1c9b99` is an ancestor of HEAD | Pass |
| 2. Files changed since review (vs reviewed code + main) are on the allowlist | Pass — only `.claude/plans/WB-59.../proposal.md` and `review.md` |
| 3. Every suite ran with zero failures | **Fail** — E2E API and E2E UI skipped |

### Backend unit/integration

Identity (`dotnet test src/Services/Identity/WordBuddy.Identity.slnx`, SQL Server `(localdb)\mssqllocaldb`):

| Suite | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| Identity.UnitTests | 238 | 0 | 0 |
| Identity.IntegrationTests | 51 | 0 | 0 |

The coder's WB-59 tests cover the domain, validator, handler, register and endpoint cases (child and adult). No gaps found, so none added.

### E2E API

Skipped — services not running (`localhost:5080` gave no answer) and migration `AddPreferredLanguage` is not applied to any running database. Starting `docker compose` or migrating is ask-gated.

Written and compiled (`dotnet build` OK), not run: `e2e/api/WordBuddy.E2E.Api.Tests/PreferredLanguageTests.cs` — set language, log in again, reply carries it (Adult and Child); malformed code `400`; anonymous `401`.

### E2E UI

Skipped — UI (`localhost:3000`) and services not running, same reason.

Written, `npx bddgen` and `tsc --noEmit` clean, not run:
- `e2e/ui/playwright.config.ts` — `locale: 'en-US'` pinned.
- `e2e/ui/features/language.feature` + `steps/language.steps.ts` — vi-VN login page; switcher without reload; choice restored in a fresh browser (Adult and Child); lesson content stays English.

### Failures

None failed. Two suites could not run (see above).

### Verdict

Not merged — E2E API and E2E UI suites were skipped (services down, migration not applied). Start the stack and apply the Identity migration, then re-run `/test`.
