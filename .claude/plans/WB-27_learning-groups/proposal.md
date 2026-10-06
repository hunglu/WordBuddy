---
title: WB-27_Learning groups
status: idea
type: new
issue: 27
pr: none
affects: docs/features/learning-groups.md
supersedes: none
version: 1.0
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-05T15:38:12+07:00
---

## Problem

A teacher supports learners one by one. There is no class or cohort to assign words to or to
follow as a whole.

## Goal

- Teacher groups (classes, cohorts) in Identity; members are learners with an active Teacher link.
- Assign a word list to a whole group.
- Group view in the dashboard (Progress + UI).
- Reserve group scope for later challenges.

## Target users

Both. A child joins a group only with guardian approval. Children appear by alias and fixed
avatar only.

## Success criteria

- A teacher creates a group, adds linked learners and assigns a list once for all.
- Revoking a Teacher link removes the learner from the teacher's groups.
- Tests cover child and adult members.

## Constraints

- Depends on WB-24 and WB-26 (D4).
- Out of scope: group challenges (WB-29 or later).
