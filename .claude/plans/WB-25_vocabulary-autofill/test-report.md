# Test report: WB-25_Vocabulary autofill

Branch `feature/WB-25_vocabulary-autofill` (origin/main already merged, no conflict) · PR #37 · 2026-10-09

## Backend unit/integration

| Suite | Result |
| --- | --- |
| `WordBuddy.Content.UnitTests` | 200 passed, 0 failed |
| `WordBuddy.Content.IntegrationTests` | 96 passed, 0 failed |

## E2E API

43 tests: **41 passed, 2 failed**. New class `VocabularyAutofillTests.cs` (7 tests):

| Test | Result |
| --- | --- |
| `AdultAddsWordViaAutofill_SenseInMine` | **Failed** (blocker, see below) |
| `ChildAddsWordViaAutofill_HiddenUntilSupporterApproves` | **Failed** (blocker, see below) |
| `Lookup_UnknownWord_ReturnsUnavailableAndManualAddStillWorks` | Passed |
| `Lookup_ChildWithoutSupporter_ReturnsForbidden` | Passed |
| `PendingApprovals_NonSupporterAdult_ReturnsForbidden` | Passed |
| `AdminAutofillQueue_NonAdminForbidden_AdminOk` | Passed |
| `AddToMine_UnknownSense_ReturnsNotFound` | Passed |

## E2E UI

26 passed, 0 failed. New `features/vocabulary-autofill.feature`:

| Scenario | Backend | Result |
| --- | --- | --- |
| Auto-fill shows sense cards and adds the chosen sense | Look-up and add-to-mine stubbed in the browser (`page.route`) | Passed |
| Auto-fill is unavailable and the manual form still adds the word | Live | Passed |

## Failures

Both API failures have one cause: no auto-filled sense can exist in the live stack.

```
GET /api/vocabulary/autofill?word=serendipity → 200, autofillUnavailable = true
Expected autofillUnavailable to be False ... needs Autofill__Claude__ApiKey in the Content container
```

- Content has no `Autofill__Claude__ApiKey`, so `ClaudeSenseGenerator` returns a failure without a call (correct, no external Claude call).
- A catalog hit needs an `AutoFill` sense. Only the auto-fill save creates one; no existing API seeds it. The catalog-hit path cannot be used.
- Not an application bug. The implementation behaves as planned; unit and integration tests cover the happy path with fake clients.

Options for Sam:

| Option | Effect |
| --- | --- |
| Set a Claude key in the Content container, re-run `/test` | Real Claude calls on every new word in E2E |
| `/code`: add a Development-only fake generator switch (e.g. `Autofill:UseFakeClients`) | Keyless, deterministic E2E; needs `/review` again |

Other notes:

- A live dictionary miss in the UI took more than 5 s once; the UI step waits up to 30 s.

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, `e7a5232` ancestor of HEAD | Pass |
| 2. Changes since review (vs reviewed + main) | Pass — `.claude/plans/WB-25_vocabulary-autofill/proposal.md`, `.claude/plans/WB-25_vocabulary-autofill/review.md`, plus this run's `e2e/**`, `test-report.md`, `docs/ai/learnings.md` |
| 3. All suites green | **Fail** — E2E API 2 failed |

## Verdict

Not merged — E2E API happy path (adult auto-fill add, child approval flow) cannot pass without a Claude key or a keyless fake path; Sam decides.
