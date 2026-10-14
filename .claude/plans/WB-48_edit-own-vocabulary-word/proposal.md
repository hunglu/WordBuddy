---
title: WB-48_Edit own vocabulary word
status: needs-fixes
type: change
issue: 48
pr: 52            # set by /code when it opens the pull request
affects: docs/features/vocabulary.md
supersedes: none
version: 4.1
created: 2026-10-13T18:12:59+07:00
updated: 2026-10-14T10:43:26+07:00
---

## Problem

- A learner cannot fix a typo in their own word. `Senses` has no update endpoint.
- The only path is delete and re-add. That creates a new sense id and loses SRS history in Progress.

## Goal

A learner edits word, definition and example of their own `Private` sense in place. The sense id stays the same.

## Target users

Both. Children and adults can edit their own private words.

## Success criteria

- `PUT` on an own `Private` sense updates it and keeps `Senses.Id`.
- `Shared`, `PendingReview` and system senses cannot be edited this way (`409` / `403`).
- `ContentHash` is recomputed; the per-owner duplicate rule still holds (`409` on clash).
- Edit form in the vocabulary UI.

## Constraints

- Editing shared or system senses: out of scope.
- Found in the schema-vs-UI gap analysis of 2026-10-13.
