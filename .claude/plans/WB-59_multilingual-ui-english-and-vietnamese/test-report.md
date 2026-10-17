# Test report: WB-59_Multilingual UI (English and Vietnamese)

Branch `feature/WB-59_multilingual-ui-english-and-vietnamese`, HEAD `6a5aa33` (after merging `origin/main`: already up to date).

## Merge guard

| Guard | Result |
| --- | --- |
| 1. `review.md` Approve, reviewed sha `b1c9b99` is an ancestor of HEAD | Pass |
| 2. Files changed since review (vs reviewed code + main) are on the allowlist | Pass — only `.claude/plans/WB-59.../proposal.md` and `review.md` |
| 3. Every suite ran with zero failures | **Fail** — E2E API and E2E UI skipped |

## Backend unit/integration

Identity (`dotnet test src/Services/Identity/WordBuddy.Identity.slnx`, SQL Server `(localdb)\mssqllocaldb`):

| Suite | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| Identity.UnitTests | 238 | 0 | 0 |
| Identity.IntegrationTests | 51 | 0 | 0 |

The coder's WB-59 tests cover the domain, validator, handler, register and endpoint cases (child and adult). No gaps found, so none added.

## E2E API

Skipped — services not running (`localhost:5080` gave no answer) and migration `AddPreferredLanguage` is not applied to any running database. Starting `docker compose` or migrating is ask-gated.

Written and compiled (`dotnet build` OK), not run: `e2e/api/WordBuddy.E2E.Api.Tests/PreferredLanguageTests.cs` — set language, log in again, reply carries it (Adult and Child); malformed code `400`; anonymous `401`.

## E2E UI

Skipped — UI (`localhost:3000`) and services not running, same reason.

Written, `npx bddgen` and `tsc --noEmit` clean, not run:
- `e2e/ui/playwright.config.ts` — `locale: 'en-US'` pinned.
- `e2e/ui/features/language.feature` + `steps/language.steps.ts` — vi-VN login page; switcher without reload; choice restored in a fresh browser (Adult and Child); lesson content stays English.

## Failures

None failed. Two suites could not run (see above).

## Verdict

Not merged — E2E API and E2E UI suites were skipped (services down, migration not applied). Start the stack and apply the Identity migration, then re-run `/test`.
