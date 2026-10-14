---
title: WB-50_Lexeme admin view
status: changes-requested
type: new
issue: 50
pr: 54              # set by /code when it opens the pull request
affects: docs/features/lexeme-admin.md
supersedes: none
version: 5.0
created: 2026-10-13T18:12:59+07:00
updated: 2026-10-14T17:18:42+07:00
---

## Problem

- Lexemes are created only as a side effect (add word, auto-fill).
- Admins cannot see senses grouped by lemma.
- Admins cannot correct POS, IPA, syllables, word forms, CEFR, frequency rank or audio.

## Goal

An admin screen that lists lexemes with their senses and lets admins correct lexeme fields.

## Target users

Admins only. No learner or child access.

## Success criteria

- Admin searches lexemes and sees each one's senses.
- Admin edits POS, IPA UK/US, syllables, word forms, CEFR, frequency rank and UK/US audio.
- The unique (lemma, POS) rule is kept; a clash returns `409`.
- Every endpoint requires `AdminOnly`.

## Constraints

- Merging or splitting lexemes, and moving senses between lexemes: out of scope.
- Found in the schema-vs-UI gap analysis of 2026-10-13.

## Revisions

- **v3.0 — 2026-10-14T16:36:14+07:00** — Sam's answers to plan questions: search matches any part of the lemma ("contains", prefix matches ranked first); removing audio physically deletes the file and the `MediaAsset` row when nothing else references it (otherwise unlink only). POS stays required; learner senses stay count-only; concurrent edits stay last-write-wins.
