---
title: WB-56_Moderator role to assist admin
status: reviewed
type: new
issue: 56
pr: 58
affects: docs/features/WB-56_moderator-role-to-assist-admin.md
supersedes: none
version: 7.0
created: 2026-10-14T21:42:09+07:00
updated: 2026-10-16T15:52:46+07:00
---

## Problem

- Only admins (`is_admin`) can manage content today.
- One admin carries all moderation and data work.
- There is no lower role that can help without full admin power.

## Goal

Add a **Moderator** role (data role) next to **Admin** (user role).

| Role | Manages |
| --- | --- |
| Moderator | Vocabulary moderation, lexemes & lessons, support links |
| Admin | Everything a moderator manages + users + role assignment |

- Admin grants and revokes Moderator from an admin screen.
- Admin accounts are the most powerful accounts and cannot be deleted.

## Target users

Adults only. A child account (`AgeGroup = Child`) can never be a moderator. Learners see no change.

## Success criteria

- A moderator can use vocabulary moderation, lexeme/lesson authoring and support-link admin.
- A moderator cannot assign roles or manage users.
- A moderator cannot see child account data.
- Only adult accounts can receive the Moderator role.
- Admin can grant/revoke Moderator in the UI.
- An admin account cannot be deleted.

## Constraints

_Not specified._
