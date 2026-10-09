# Test report: WB-25_Vocabulary autofill

Run 2 · Branch `feature/WB-25_vocabulary-autofill` (origin/main merged, no conflict) · PR #37 · 2026-10-09

Stack: compose from the branch, Content with `Autofill__UseFakeClients=true` (Development-only fake clients, no network, no Claude key).

## Backend unit/integration

| Suite | Result |
| --- | --- |
| `WordBuddy.Content.UnitTests` | 211 passed, 0 failed (incl. 11 `AutofillClientSwitchTests`) |
| `WordBuddy.Content.IntegrationTests` | 96 passed, 0 failed |

## E2E API

43 passed, 0 failed. `VocabularyAutofillTests.cs` (7 tests, all passed):

| Test | Covers |
| --- | --- |
| `AdultAddsWordViaAutofill_SenseInMine` | Adult: lookup `serendipity` → add-to-mine → in `mine`, `origin = AutoFill` |
| `ChildAddsWordViaAutofill_HiddenUntilSupporterApproves` | Child: add → `awaitingApproval`, no definition → supporter approves → visible |
| `Lookup_UnknownWord_ReturnsUnavailableAndManualAddStillWorks` | Failure path → manual add works |
| `Lookup_ChildWithoutSupporter_ReturnsForbidden` | Child gate (403) |
| `PendingApprovals_NonSupporterAdult_ReturnsForbidden` | `CanSupportLearner` (403) |
| `AdminAutofillQueue_NonAdminForbidden_AdminOk` | `AdminOnly` |
| `AddToMine_UnknownSense_ReturnsNotFound` | 404 |

## E2E UI

26 passed, 0 failed. `features/vocabulary-autofill.feature`, both against the real stack (browser stubs removed, review nit 1):

| Scenario | Result |
| --- | --- |
| Auto-fill shows sense cards and adds the chosen sense (`serendipity`) | Passed |
| Auto-fill is unavailable and the manual form still adds the word | Passed |

## Failures

None.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review round 2 Approve, `55ce542` ancestor of HEAD | Pass |
| 2. Changes since review (vs reviewed + main) | Pass — `.claude/plans/WB-25_vocabulary-autofill/proposal.md`, `.claude/plans/WB-25_vocabulary-autofill/review.md`; this run adds only `e2e/**`, `test-report.md`, `proposal.md`, `docs/features/**` |
| 3. All suites green | Pass |

## Verdict

Approved — merged into main
