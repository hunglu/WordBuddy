# Review: WB-56 Moderator role to assist admin

PR: #58 · Round 1 · Reviewed commit: d78ee4f · 2026-10-15T23:34:05+07:00

## Verdict

Changes requested — 2 majors: a moderator can act on their own case (approve their own shared word, resolve an unlink they are part of).

Authorization split, child filtering, migration and UI gating are otherwise correct.

## Findings

| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `WordBuddy/src/Services/Content/WordBuddy.Content.Application/Features/PersonalVocabulary/Commands/ModerateSharedVocabularyWord/ModerateSharedVocabularyWordCommandHandler.cs:53` (route `PersonalVocabularyController.cs:236`) | Self-moderation. Nothing checks `Sense.OwnerUserId` against `ModeratorUserId`. Before WB-56 only admins (trusted) could moderate. Scenario: an adult moderator shares a word, then calls `POST moderation/{id}` with `approve=true, visibleToChildren=true`. The word reaches every child with no second review. | Pass `ActorIsAdmin` (as in the unlink commands). When the actor is not an admin and `OwnerUserId == ModeratorUserId`, return a typed `Error.Forbidden`/`Conflict` (e.g. `PersonalVocabularyWord.CannotModerateOwnWord`) with no state change. Optionally hide own words in the moderator's pending list. Add a unit test. |
| 2 | major | `WordBuddy/src/Services/Identity/WordBuddy.Identity.Application/Features/SupportLinks/Commands/AdminCompleteUnlink/AdminCompleteUnlinkCommandHandler.cs:63`, `.../AdminRejectUnlink/AdminRejectUnlinkCommandHandler.cs:55` | Self-adjudication. A moderator can complete or reject an escalated unlink in which they are the learner or the supporter. Scenario: an adult learner escalates an unlink against a supporter who is a moderator; the moderator rejects it with any reason, and the link stays active against the learner's wish. | When `!ActorIsAdmin` and `AdminId` is `link.LearnerId` or `link.SupporterId`, return `SupportLinkErrors.UnlinkRequestNotFound` (same as the child case), no audit row. Filter those requests out of the moderator's list in `GetEscalatedUnlinkRequestsAsync`. Add unit + integration cases. |
| 3 | nit | `WordBuddy/src/Services/Identity/WordBuddy.Identity.Application/Features/SupportLinks/Queries/GetUnlinkRequestsForAdmin/GetUnlinkRequestsForAdminQuery.cs:7` | `IncludeChildLearners = true` is a fail-open default. A future caller that forgets the flag shows child requests to a moderator. | Make it a required parameter, as `ActorIsAdmin` already is. |
| 4 | nit | `WordBuddy/src/Services/Identity/WordBuddy.Identity.Api/Extensions/ServiceCollectionExtensions.cs:42` | `HasClaim("age_group", "Child")` is case-sensitive; Content compares with `OrdinalIgnoreCase`. Safe today (Identity emits `Child`), but the two policies differ. | Use the same `string.Equals(..., OrdinalIgnoreCase)` form as Content. |
| 5 | nit | `WordBuddy/src/Services/Identity/WordBuddy.Identity.IntegrationTests/ModeratorEndpointsTests.cs` | No Identity test sends a token with `moderator=true` and `age_group=Child` to `ManageSupportLinks`. Content covers this case; Identity does not (real logins cannot produce it). | `/test`: add one case with a hand-built token (as Content's `TestJwtTokenFactory`) → 403 on `GET admin/unlink-requests`. |
| 6 | nit | `WordBuddy/src/Services/Identity/WordBuddy.Identity.Infrastructure/Seeding/DataSeeder.cs:50` | The `Result` of `user.SetModerator(true)` is ignored. A seed row edited to Child or admin would silently seed a non-moderator. | Check `IsFailure` and throw (dev seed only). |
| 7 | nit | `WordBuddy.UI/src/pages/AdminSupportLinksPage.tsx:55` | The heading "Support links (admin)" also shows to moderators. | Drop "(admin)" or show it only for admins. |

## Authorization check

| Endpoint | Before | After | Verified |
|---|---|---|---|
| Lessons admin, lexemes admin (class level) | `AdminOnly` | `ManageContent` | yes |
| `moderation/pending`, `moderation/{id}`, `autofill-pending`, `autofill/{id}/approve` | `AdminOnly` | `ManageContent` | yes |
| `learner-words/republish` | `AdminOnly` | `AdminOnly` | yes, test present |
| `admin/unlink-requests` list/complete/reject | `AdminOnly` | `ManageSupportLinks` + child filter | yes, test present |
| `admin/support-links?learnerId=`, `support-links/handover` | `AdminOnly` | `AdminOnly` (new controller, same routes) | yes, test present |
| `admin/users`, `admin/users/{id}/moderator`, `guardian`, `learner-profiles/republish` | `AdminOnly` | `AdminOnly` | yes |
| Quiz, Progress, Notification | — | no change; no admin endpoints | yes |

Child data: `GET admin/users` filters `AgeGroup == Adult` in SQL; `AdminUserDto` has no age or DOB fields. Unlink DTOs are ids only and filtered to adult learners. Autofill admin queue returns `LearnerId = null`. No PII in new logs (search length only).

Migration `AddUserIsModerator`: `bit NOT NULL DEFAULT 0`, snapshot and `docs/database-diagram/identity.md` match.

## Plan conformance

- All 40 tasks are reflected in the diff, except the 2 e2e tasks moved to `/test` (accepted; tester-owned).
- Coder deviations, judged:

| Deviation | Verdict |
|---|---|
| Required `ActorIsAdmin` on unlink commands | OK — safer than a default |
| Non-admin on child request → `UnlinkRequestNotFound`, no change, no audit row | OK — matches plan; check runs before any write |
| New `ISupportLinkRepository.IsChildLearnerLinkAsync` | OK — single SQL join; unknown link → false, then the existing not-found path |
| `GetEscalatedUnlinkRequestsAsync(includeChildLearners)` | OK — filter in SQL, paging counts stay correct (see nit 3) |
| Token-claim unit test → integration test `Login_SeededModerator_TokenHasClaimAndLearnerTokenDoesNot` | OK — generator is internal; real token checked |
| No separate migration-default test | OK — column default is in the migration; all integration tests run on it |

- Out of scope: none found.
- No line-ending churn (`--shortstat` equals `--ignore-cr-at-eol`).
