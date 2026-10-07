---
title: WB-23_Vocabulary review exercises
status: implemented
type: change
issue: 23
pr: none
affects: docs/features/vocabulary-builder.md
supersedes: vocabulary-builder-and-checkup
version: 1.3
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-07T23:00:00+07:00
---

## Problem

Learners review words by rating themselves ("Known" / "Learning"). This over-rates memory and
gives the scheduler nothing reliable.

## Goal

- A daily review session page driven by `GET /api/progress/vocabulary/session` (WB-22).
- Three exercises: picture → choose the word, hear audio → choose the word, type the word.
- Each new word starts at recognition and moves up only after correct answers.
- The UI measures response time and hint use, and posts each answer to
  `POST /api/progress/vocabulary/reviews`.
- Content adds `GET /api/vocabulary/senses?ids=`: returns only senses the caller may see; hidden or
  unknown ids are left out.
- The personal-context sentence is shown in reviews.

## Target users

Both. Child: short sessions (10–15 min), only child-visible senses. Adult: full session.

## Success criteria

- A learner finishes a session without any self-rating.
- `senses?ids=` never reveals a hidden sense (child and adult tests).
- E2E (UI + API) covers one full session for a child and an adult.

## Constraints

- Depends on WB-22.
- Open point: replace the recall check fully, or keep it during a transition. Decide in `/plan`.
- Out of scope: pronunciation, AI-graded sentences (phase 2).
