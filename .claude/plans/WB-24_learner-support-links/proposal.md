---
title: WB-24_Learner support links
status: implemented
type: new
issue: 24
pr: none
affects: docs/features/learner-support-links.md
supersedes: none
version: 1.4
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-08T00:00:00+07:00
---

## Problem

Parents, teachers and study partners cannot follow or help a learner. A child account has no
owner who gives consent.

## Goal

- `SupportLink` in Identity: `LearnerId`, `SupporterId`, `Role` (Guardian, Teacher, Peer),
  `Status` (Invited, Active, Revoked), `GrantedBy`.
- Lifecycle: invite (code or link) → accept → active → revoke.
- Fixed permissions per role (D3), as in idea.md section 4.
- Identity publishes `SupportLinkActivated` / `SupportLinkRevoked`; Content and Progress keep a
  local copy for a `CanSupportLearner(permission)` policy. Link ids are not put in the JWT.
- `Alias` and `AvatarId` on the profile (adults too), for later challenge boards.
- UI: invite, accept, list and revoke links.
- Added 2026-10-07 (from WB-22 decision D-4):
  - Link and unlink need consent from both sides, the same for all accounts (child and adult),
    so no user is surprised.
  - Admin override (operations, not a learner feature): when the other side (parent or teacher)
    takes no action on an unlink request, an admin can complete the unlink. The action is audited.
    _Wait time before override and how the learner asks for it: to define in `/plan`._
  - A linked Guardian or Teacher sets the vocabulary new-word cap per child
    (overrides the child's own `VocabularyLearnerSettings` from WB-22).

## Target users

Both.

- Child: exactly one active Guardian, who owns the profile. A Teacher link needs guardian
  approval. No Peer links (D5).
- Adult: no guardian; accepts Teacher and Peer links alone; unlink follows the same two-sided consent as every account (Sam, 2026-10-07). Peer links are adult ↔
  adult only.

## Success criteria

- Every rule in the table above is enforced by a policy and covered by tests.
- A revoked link loses access in Content and Progress after the event is consumed.
- No child data appears in events or logs.

## Constraints

- Depends on WB-21 (messaging). Related: issue #20 (user profile settings).
- Out of scope: teacher groups (WB-27), dashboards (WB-26).
