# Test report: WB-20 Create User Profile Setting function

Stack: docker compose up. Identity migration `20261013034057_AddUserDateOfBirth` confirmed applied (identity-api startup log).

## Backend unit/integration

| Suite | Result |
| --- | --- |
| Identity.UnitTests | 173 passed, 0 failed |
| Identity.IntegrationTests | 30 passed, 0 failed |
| Progress.UnitTests | 414 passed, 0 failed |
| Progress.IntegrationTests | 94 passed, 0 failed |
| UI `npm run build` | OK (chunk-size warning only) |

## E2E API

`e2e/api/WordBuddy.E2E.Api.Tests` — 64 passed, 0 failed. New: `ProfileSettingsTests.cs` (child band `Age7To9`, adult band `Adult`, future date `400`, adult date on child `400`, no token `401`, wrong password `400`, old password stops working for child and adult, active supporter sees learner date and cannot change it).

## E2E UI

`e2e/ui` — 43 passed, 1 failed (accepted).
New: `features/user-profile.feature` + `steps/user-profile.steps.ts`, 8 scenarios, all pass.

| Scenario | Age group |
| --- | --- |
| Save date of birth, persists after reload | Adult |
| Save date of birth, guardian hint shown | Child |
| Adult-age date rejected | Child |
| Future date blocked | Adult |
| Password change, login with new password | Adult |
| Wrong current password rejected | Adult |
| Linked guardian listed with "Guardian" badge | Child |
| No My supporters card without links | Adult |

## Failures

- `support-links.feature:23` "Admin completes an escalated unlink request" — progress-api lacks `SupportLinks__UnlinkOverrideWaitDays=0`. Known environment gap, accepted by Sam. Not related to WB-20.

Known follow-ups from review (all nits, none block):

| # | Nit |
| --- | --- |
| 1 | `ProfilePage` keys the date of birth and account forms on the saved value, so "Saved." never shows. E2E checks persistence by reload instead |
| 2 | Older JWTs stay valid after a password change (no revocation yet) |
| 3 | `JwtTokenGenerator` uses `DateTime.UtcNow`, handlers use `TimeProvider` |
| 4 | `authStore` persists `dateOfBirth` to `localStorage` |
| 5 | Supporter cache invalidation includes ended links (harmless) |

## Merge guard

- Review verdict Approve, reviewed commit `96577fb` is an ancestor of HEAD.
- `git merge origin/main` was already up to date.
- Files changed since review, against reviewed code plus current main (all allowlisted):

```
.claude/plans/WB-20_create-user-profile-settings/proposal.md
.claude/plans/WB-20_create-user-profile-settings/review.md
docs/ai/learnings.md
```

- Every suite ran in this session. The one failing scenario is the accepted environment gap.

## Verdict

Approved and merged into main
