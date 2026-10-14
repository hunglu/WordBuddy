---
title: WB-49_Personal context on learner word
status: implemented
type: change
issue: 49
pr: 53              # set by /code when it opens the pull request
affects: docs/features/vocabulary.md
supersedes: none
version: 5.0
created: 2026-10-13T18:12:59+07:00
updated: 2026-10-14T14:06:08+07:00
---

## Problem

- `LearnerWords.PersonalContext` is shown in review (`PersonalContextNote`).
- No endpoint or form can set it, so it is always `null`.

## Goal

A learner adds and edits a short note on where they met a word — when adding it, and later.

## Target users

Both. A child's note is private to the child (and is never logged).

## Success criteria

- Optional `personalContext` (max 500) on every add-to-mine flow (own word, shared pool, auto-fill).
- Endpoint to update or clear the note on an existing `LearnerWord`.
- The note shows in review exercises.
- Notes are never visible to other users, and never written to logs.

## Constraints

- Supporter-written notes for group assignments: out of scope.
- Found in the schema-vs-UI gap analysis of 2026-10-13.
