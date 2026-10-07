---
title: WB-20_Create User Profile Setting function
status: idea
type: new
issue: 20
pr: none
affects: docs/features/user-profile.md
supersedes: none
version: 1.0
created: 2026-10-07T09:47:51+07:00
updated: 2026-10-07T09:47:51+07:00
---

## Problem

Logged-in users have no place to see their account information and no way to update it.
Progress also has no learner age: grading uses only `AgeGroup` (Child/Adult), which is coarse
(WB-22 decision D-7).

## Goal

- Profile screen: view and update own information, change password.
- Date of birth on the profile; the user can update it.
- Once date of birth exists, grading thresholds use finer age bands instead of the `AgeGroup`
  multiplier from WB-22. Only future grading changes; past `ReviewLog` ratings stay as recorded.
- A child sees their linked guardian (and other supporters, once WB-24 exists).

## Target users

Both. Date of birth of a child is child data: minimal exposure, never in logs or events.
- Every user, including a child, edits their own date of birth (Sam, 2026-10-07).
- Linked supporters (WB-24) can view it but cannot edit it.

## Success criteria

_Not specified._

## Constraints

- Source: issue #20 and WB-22 decision D-7.
- Related: WB-22 (grading multiplier), WB-24 (support links).
