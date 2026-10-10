---
title: WB-28_Vocabulary answer checking
status: changes-requested
type: change
issue: 28
pr: 40
affects: docs/features/vocabulary-builder.md
supersedes: none
version: 1.3
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-10T23:47:48+07:00
---

## Problem

In the MVP the UI reports whether an answer is correct (D11). A learner can fake answers, which
is not acceptable once scores are compared in challenges.

## Goal

- The server builds each exercise and keeps the expected answer.
- The UI posts the learner's answer; the server decides correct / wrong and the rating.
- Response time is measured server-side as well, with a sanity check against the client value.

## Target users

Both. No visible change except that answers are checked by the server.

## Success criteria

- `POST /api/progress/vocabulary/reviews` no longer accepts a client "correct" flag.
- A forged answer cannot earn a correct rating (tests).
- Session flow and timing feel unchanged (E2E).

## Constraints

- Depends on WB-23. Required before WB-29 (D11).
