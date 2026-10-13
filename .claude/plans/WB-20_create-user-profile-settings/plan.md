# Plan: WB-20 Create User Profile Setting function

## Summary

Extend the existing `/profile` page and Identity profile endpoints: show account info, edit display name and date of birth, change password, and list linked supporters.
Progress grades with finer age bands from a new `age_band` JWT claim. The date of birth itself never leaves Identity.

```
User (Identity, DateOfBirth) ──login──► JWT { age_group, age_band }
                                              │
Progress RecordVocabularyReview ◄─────────────┘  band multiplier, else AgeGroup multiplier (WB-22)
```

## What already exists (do not rebuild)

| Area | Existing | Source |
| --- | --- | --- |
| Profile page | `WordBuddy.UI/src/pages/ProfilePage.tsx` (alias, avatar, `TimeZoneForm`) | WB-24/WB-29 |
| Profile API | `GET /api/auth/me`, `PUT /api/auth/profile`, `PUT /api/auth/profile/time-zone` in `ProfileController` | Identity |
| Support links | Identity `Features/SupportLinks`, `GET` my support links, UI `SupportLinksPage` | WB-24 (done) |
| Grading | `AnswerGrader.Grade(exerciseType, ageGroup, …)`, `VocabularyGradingOptions.AgeGroupMultiplier` | WB-22 (done) |
| Profile event | `LearnerProfileUpdated` (no email, no password) | `WordBuddy.Shared.Contracts` |
| Password hashing | `IPasswordHasher` / `PasswordHasher` | Identity |

No refresh tokens exist yet, so password change has no sessions to revoke.

## Affected services / areas

| Area | Change |
| --- | --- |
| Identity | `User.DateOfBirth`, `User.DisplayName` editable, `ChangePassword`, new commands, `age_band` claim, migration |
| Progress | Grader reads `age_band` claim; new band multipliers in options |
| Shared | None. `LearnerProfileUpdated` stays unchanged (no date of birth in events) |
| WordBuddy.UI | `ProfilePage` sections: account info, date of birth, password, my supporters |

## Backend approach

All handlers use `ICommandHandler` / `IQueryHandler` from `WordBuddy.Shared.Application.Abstractions` (WB-32). Follow `develop-webapi` templates for folders, validators and `ToProblemResult`.

### Identity

**Domain (`User`)**

| Member | Rule |
| --- | --- |
| `DateOnly? DateOfBirth { get; private set; }` | Null for existing users |
| `Result SetDateOfBirth(DateOnly? dob, DateOnly today)` | Not in future; age 3 to 120; `Child` must be under 18, `Adult` 13 or older (see open question Q2) |
| `Result SetDisplayName(string name)` | 2 to 50 chars, trimmed |
| `Result ChangePassword(string newHash)` | `PasswordHash` gets `private set` |
| `AgeBand? GetAgeBand(DateOnly today)` | Pure; null when no date of birth |

New enum `AgeBand`: `Under7`, `Age7To9`, `Age10To12`, `Age13To17`, `Adult`. New `UserErrors`: `DateOfBirthInvalid`, `DateOfBirthAgeGroupMismatch`, `DisplayNameLength`, `WrongCurrentPassword`, `PasswordTooWeak`.

**Features (`Features/Profile/...`)**

| Use case | Endpoint | Notes |
| --- | --- | --- |
| `UpdateDateOfBirth` command | `PUT /api/auth/profile/date-of-birth` | Own account only. Child and adult both allowed (Sam, 2026-10-07) |
| `UpdateDisplayName` command | `PUT /api/auth/profile/display-name` | Publishes `LearnerProfileUpdated` (adult only carries the name, as today) |
| `ChangePassword` command | `POST /api/auth/profile/password` | Verify current password; new password reuses register rules; returns new JWT so claims refresh. Stricter rate-limit policy (auth policy) |
| `GetCurrentUser` query (change) | `GET /api/auth/me` | Add `dateOfBirth`, `ageBand`, `email` (own data only) |
| `GetMySupportLinks` query (change) | existing | For `asSupporter` Active links, add learner `dateOfBirth` (read-only view for supporters, WB-24 rule). Not added for pending/ended links |

`UpdateDateOfBirth` also returns a new JWT so the `age_band` claim updates without re-login.

**Token:** `JwtTokenGenerator` adds `age_band` claim when date of birth exists. Band, not the date — minimal exposure.

**Caching:** if `me` is cached, delete `identity:user:{id}` after each command.

**Logging:** never log date of birth, passwords or hashes. Log user id only.

### Progress

| Change | Detail |
| --- | --- |
| `VocabularyGradingOptions.AgeBandMultiplier` | Config `Vocabulary:Grading:AgeBandMultiplier`. Proposed: Under7 ×1.5, 7–9 ×1.35, 10–12 ×1.2, 13–17 ×1.1, Adult ×1.0 |
| `AnswerGrader.Grade` | Takes a `GradingAge` (AgeGroup + optional AgeBand). Band multiplier wins; missing band → existing AgeGroup multiplier |
| `ClaimsPrincipalExtensions.GetAgeBand()` | Unknown/missing claim → null (fallback) |
| `RecordVocabularyReviewCommand` | Add `AgeBand? AgeBand` |
| `ReviewLog` | Unchanged. Past ratings stay as recorded (insert-only) |

### Child vs adult

| Behaviour | Child | Adult |
| --- | --- | --- |
| View own profile, edit own date of birth | Yes | Yes |
| Edit display name | Yes (never shown to others; `PublicName` stays alias) | Yes |
| Change password | Yes | Yes |
| Date of birth in events / logs | Never | Never |
| Date of birth seen by others | Active linked supporters only, read-only | Active linked supporters only, read-only |
| "My supporters" section | Shown (guardian marked primary) | Shown if any links exist |
| Grading | Band multiplier, fallback Child ×1.25 | Band `Adult` ×1.0 |

## Frontend approach

`ProfilePage.tsx` gains sections, each a component in `src/components/profile/` (same pattern as `TimeZoneForm`):

| Component | Content |
| --- | --- |
| `AccountInfoCard` | Email, age group, display name form |
| `DateOfBirthForm` | `<input type="date">`, React Hook Form + Zod; child hint "Ask your guardian if you are not sure" |
| `ChangePasswordForm` | Current / new / confirm, Zod match rule; on success store the returned token |
| `MySupportersCard` | Reads existing `useMySupportLinks`; lists `asLearner` Active links, guardian first; link to `/support-links` |

- API: `updateDateOfBirth`, `updateDisplayName`, `changePassword` in `src/api/auth.ts`.
- Hooks: `useUpdateDateOfBirth`, `useUpdateDisplayName`, `useChangePassword` (invalidate `['currentUser']`; token responses call `authStore.login`).
- Types: `User` gains `email`, `dateOfBirth: string | null`, `ageBand: AgeBand | null`; `type AgeBand = 'Under7' | …`.
- Supporter view: `SupportLinksPage` shows linked learner date of birth (read-only) when present.
- Every new query handles `isError`.

## Data / migration notes

- Identity: migration `AddUserDateOfBirth` — nullable `date` column `DateOfBirth` on `Users`. No backfill.
- Progress: none (options only).

## Proposed success criteria

1. A logged-in user (child or adult) sees email, display name, age group, date of birth on `/profile`.
2. A user updates date of birth; a future date or an age that contradicts `AgeGroup` returns `400`.
3. A user changes password; wrong current password returns `400`; old password no longer logs in.
4. After setting date of birth, the next review is graded with the band multiplier; earlier `ReviewLog` rows are unchanged.
5. A user without date of birth is graded exactly as in WB-22.
6. A child sees their linked guardian and other active supporters on `/profile`.
7. An active supporter sees the learner's date of birth, cannot edit it (no endpoint).
8. Date of birth appears in no log line, no event, no JWT (only the band).

## Open questions

- **Q1 — Age transport.** Plan uses an `age_band` JWT claim (fresh per token, no stored copy in Progress). Alternative: add band to `LearnerProfileUpdated`; rejected because bands go stale between events. OK?
- **Q2 — Consistency with `AgeGroup`.** Plan rejects a date of birth that contradicts `AgeGroup` (Child ≥ 18, Adult < 13). Should a birthday instead switch `AgeGroup` automatically? Out of scope here; proposed as follow-up.
- **Q3 — Band cut-offs and multipliers.** Values above are proposals; Sam to confirm.
- **Q4 — Display name for a child.** Editable but never shown to others. Keep editable?
- **Q5 — Email change.** Not in scope (needs verification flow). Read-only on the page.
- Scope beyond goal (not planned): automatic Child → Adult transition at 18; email change with verification; refresh tokens + session revoke on password change.
