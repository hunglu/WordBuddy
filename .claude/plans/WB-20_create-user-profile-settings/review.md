# Review: WB-20 Create User Profile Setting function

PR: #46 · Round 1 · Reviewed commit: 96577fb (code in 9ffdb37) · 2026-10-13T11:27:07+07:00

## Verdict
Approve — no blockers or majors; date of birth stays in Identity, supporter access is read-only and Active-only, password change is verified and rate-limited.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `WordBuddy.UI/src/pages/ProfilePage.tsx:26-27` | `AccountInfoCard` and `DateOfBirthForm` are keyed on the saved value. A successful save changes the key, the form remounts with a new mutation, so the "Saved." message never shows. | Key on `user.id`, or reset the form with `reset(values)` on success instead of remounting. |
| 2 | nit | `WordBuddy.Identity.Application/Features/Profile/Commands/ChangePassword/ChangePasswordCommandHandler.cs:802` | Older JWTs stay valid until expiry after a password change. A stolen token survives the change. No revocation exists in the service yet, so this is a known limit. | Track as a follow-up (security stamp or token version claim). See Scope suggestions. |
| 3 | nit | `WordBuddy.Identity.Infrastructure/Services/JwtTokenGenerator.cs:35` | Uses `DateTime.UtcNow` while the handlers use `TimeProvider`. The band in the token and the band in the DTO can differ on a birthday at midnight, and the claim is hard to unit test. | Inject `TimeProvider` into the generator. |
| 4 | nit | `WordBuddy.UI/src/hooks/useProfile.ts:369` | `login(auth)` persists `auth.user`, now including `dateOfBirth`, to `localStorage` (also on login). Child date of birth sits in browser storage. Owner-only, low risk. | Strip `dateOfBirth` before storing in `authStore`; read it from the `['currentUser']` query. |
| 5 | nit | `WordBuddy.Identity.Application/Features/Profile/Commands/UpdateDateOfBirth/UpdateDateOfBirthCommandHandler.cs:934-936` | Invalidates supporter caches for all links, including ended ones. Harmless extra deletes. | Filter on `l.IsActive` or leave as is. |

Verified, no finding:

- Date of birth: not in logs (ids and error codes only), not in `LearnerProfileUpdated`, not in the JWT (`age_band` only); `UserDto` with the date is built only for the caller (me, login, register, own updates).
- Supporter view: `GetMySupportLinksQueryHandler` sets `LearnerDateOfBirth` only for `Active` links in `asSupporter`; no supporter edit endpoint exists (all three endpoints use `User.GetUserId()`).
- Password change: current password verified, new ≠ current, 8–128 chars, `password-change` policy 5/min per user (`UseRateLimiter` runs after `UseAuthentication`, so the user partition works); no password or hash logged.
- Display name: event staged through the outbox before save; publisher nulls a child display name.
- Progress: unknown or missing `age_band` → null → WB-22 `AgeGroup` multiplier; old `ReviewLogs` untouched.
- Migration adds one nullable `date` column; `docs/database-diagram/identity.md` matches the snapshot.
- UI: TanStack Query only, `isError` handled on every mutation and on `MySupportersCard`, `wb-` tokens only, no `any`/`enum`, no inline styles.

## Plan conformance

| Area | Result |
|---|---|
| Identity backend (9 tasks) | All present |
| Progress backend (4 tasks) | All present |
| Frontend (7 tasks) | All present |
| Tests (unit, integration) | Present; E2E and living spec deferred to `/test` (accepted by Sam) |

Deviations, judged:

- Supporter `identity:supportlinks:{id}` invalidation instead of a `me` key — correct, since `me` is not cached and the supporter list now carries the date.
- `JwtTokenGenerator` claim tested through integration tests — acceptable; finding 3 would make a unit test possible.
- `InternalsVisibleTo` on Progress Api for integration tests — acceptable, test-only reach.

Out of scope: none.
