---
title: WB-22_Vocabulary SRS engine
status: done
type: change
issue: 22
pr: 33
affects: docs/features/vocabulary-builder.md
supersedes: none
version: 1.7
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-07T14:00:00+07:00
---

## Problem

The recall check is self-rated and the last check wins. There is no schedule and no answer
history, so nothing can be measured or rebuilt later.

## Goal

Backend only, in Progress:

- `LearnerWordState` per (`UserId`, `SenseId`): status (New, Learning, Review, Mastered, Leech)
  and FSRS state (stability, difficulty, due, reps, lapses).
- `ReviewLog`, append-only: timestamp, exercise type, skill, answer correct, response ms, hint
  used, `IsDue`, `AttemptNo`, derived rating.
- Grading from the answer (D10): wrong → Again; slow or hint → Hard; correct → Good; fast → Easy.
- `GET /api/progress/vocabulary/session`: due reviews first, then new words up to a cap.
- `POST /api/progress/vocabulary/reviews`: records one answer and reschedules.
- Backfill (D13): Content republishes `LearnerWordAdded` for every existing learner word.

## Target users

Both. Child: new-word cap 5–10 per day, lowered automatically when the review backlog grows.
Adult: cap is configurable.

## Success criteria

- Each answer adds exactly one `ReviewLog` row and updates the state; rows are never updated or
  deleted.
- The session order and caps follow the rules above, for child and adult.
- After the backfill, every existing learner word has a state row.
- Unit tests cover the grading and FSRS scheduling; integration tests cover both endpoints.

## Constraints

- Depends on WB-21 (messaging).
- Open point (ADR 0005): maintained .NET FSRS package or own port of the core. Decide in `/plan`;
  ADR 0005 → accepted when this merges.
- MVP trusts correctness and response time from the UI (D11).
- Out of scope: UI (WB-23), dashboards, challenges.
