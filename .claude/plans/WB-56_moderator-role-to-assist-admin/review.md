# Review: WB-56 Moderator role to assist admin

PR: #58 · Round 2 · Reviewed commit: fab0b7b · 2026-10-16T15:52:46+07:00

## Verdict

Approve — both round-1 majors and all 5 nits are fixed and tested. No new blockers or majors.

Tests re-run on fab0b7b: Identity 213 unit / 43 integration, Content 445 unit / 216 integration. All green.

## Round-1 fixes verified

| # | Round-1 finding | Fix in fab0b7b | Verified |
| --- | --- | --- | --- |
| 1 | major: moderator moderates own word | `ModerateSharedVocabularyWordCommand.ActorIsAdmin` (required). A non-admin owner gets `Error.Conflict("PersonalVocabularyWord.ModerateOwnWordForbidden")` before any state change. | yes. `Content.Api/Extensions/ResultExtensions.cs:14` maps codes ending in `Forbidden` to 403. This is the existing convention since WB-27 (`GroupWords.Forbidden`, `Sense.Forbidden`). Unit test checks no update and no cache delete. Integration test covers moderator 403, other owner's word 204, admin's own word 204. |
| 2 | major: moderator resolves unlink they are party to | `IsLinkPartyAsync` (SQL `Any`), checked with the child check before any write → `UnlinkRequestNotFound`. The list filters with `excludePartyUserId` in SQL, so paging counts stay correct. | yes. Unit tests for complete and reject (no save, no audit) plus admin-party success. The integration test confirms the request is hidden from the moderator, both actions return 404, the link stays active, and an admin can still complete it. |
| 3 | nit: fail-open query default | `GetUnlinkRequestsForAdminQuery(int, int, bool, Guid?)` has no defaults | yes |
| 4 | nit: case-sensitive `age_group` | `string.Equals(..., OrdinalIgnoreCase)`, same as Content | yes |
| 5 | nit: no child+moderator token test | `UnlinkAdmin_ChildTokenWithModeratorClaim_Gets403` (list + reject) | yes |
| 6 | nit: seeder ignores `SetModerator` result | throws `InvalidOperationException` with the error code only (no PII) | yes |
| 7 | nit: "(admin)" shown to moderators | heading suffix only when `isAdmin` | yes |

## Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | nit | `WordBuddy.UI/src/pages/VocabularyModerationPage.tsx:98-113`, `WordBuddy.UI/src/hooks/useVocabulary.ts:110` | The 403 for a moderator's own word is silent in the UI. `moderate` has no error state, and the pending list still shows the moderator's own words with active Approve/Reject buttons. Scenario: a moderator clicks Approve on their own word, nothing happens, and they get no reason. | Show `moderate.isError` text under the buttons, for example "Another moderator or an admin must review your own word." Optionally hide or disable own words for non-admins (needs the owner id or a flag in the DTO). |

## Regression check

- No other callers of the changed signatures. All four suites compile and pass.
- `ApproveChildWord` (autofill approve) acts on child-owned words only. A moderator is always adult, so there is no self-approval path.
- New log lines carry ids only. No tokens or PII.
- No application changes outside the 7 fixes. No migration change.

## Plan conformance

- All 40 tasks are still reflected. The 2 e2e tasks stay with `/test` (accepted in round 1).
- Round-2 additions (`IsLinkPartyAsync`, `excludePartyUserId`, `ActorIsAdmin` on moderate) are direct fixes for round-1 findings. They are in scope.
- Out of scope: none found.

## Previous rounds

### Round 1 · Reviewed commit: d78ee4f · 2026-10-15T23:34:05+07:00

Verdict: Changes requested. Two majors: a moderator could act on their own case (approve their own shared word, resolve an unlink they are part of).

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | major | `Content.Application/.../ModerateSharedVocabularyWordCommandHandler.cs:53` | Self-moderation: no check of `Sense.OwnerUserId` against `ModeratorUserId`. | Pass `ActorIsAdmin`; non-admin owner → typed error, no state change; unit test. |
| 2 | major | `Identity.Application/.../AdminCompleteUnlinkCommandHandler.cs:63`, `.../AdminRejectUnlinkCommandHandler.cs:55` | Self-adjudication: a moderator could complete or reject an unlink they are the learner or supporter of. | Non-admin party → `UnlinkRequestNotFound`, no audit; filter the list; unit + integration tests. |
| 3 | nit | `GetUnlinkRequestsForAdminQuery.cs:7` | `IncludeChildLearners = true` is a fail-open default. | Make it required. |
| 4 | nit | `Identity.Api/Extensions/ServiceCollectionExtensions.cs:42` | `HasClaim("age_group", "Child")` is case-sensitive. | Use `OrdinalIgnoreCase`, as Content does. |
| 5 | nit | `Identity.IntegrationTests/ModeratorEndpointsTests.cs` | No child+moderator token test for `ManageSupportLinks`. | Add a hand-built token case → 403. |
| 6 | nit | `Identity.Infrastructure/Seeding/DataSeeder.cs:50` | `SetModerator(true)` result is ignored. | Check `IsFailure` and throw. |
| 7 | nit | `WordBuddy.UI/src/pages/AdminSupportLinksPage.tsx:55` | "Support links (admin)" heading shown to moderators. | Drop "(admin)" or show it only for admins. |

Round 1 also verified the authorization split per endpoint, child data filtering, migration `AddUserIsModerator` and the database diagram. All were correct.
