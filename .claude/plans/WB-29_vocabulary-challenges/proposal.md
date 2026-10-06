---
title: WB-29_Vocabulary challenges
status: idea
type: new
issue: 29
pr: none
affects: docs/features/vocabulary-challenges.md
supersedes: none
version: 1.0
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-05T15:38:12+07:00
---

## Problem

There is no motivation loop beyond streaks. Plain scoreboards reward volume, not memory.

## Goal

- System-created `Challenge`: audience (Adult | Child, never mixed), kind (Ranked | Target),
  rules from a fixed metric catalog, target, period, capacity (league ≈ 30).
- Lifecycle: Scheduled → Open → Running → Finished. `ChallengeParticipant` with score and rank.
- Scoring as a projection of `ReviewLog`, daily cap 200 (idea.md section 6): due review correct
  first try +10, with hint +5, Recognize → Recall +5, daily goal +10.
- Launch set: Weekly League, 7-Day Memory, Word Climber, Listening Sprint (adult only).

## Target users

Both.

| | Adult | Child |
| --- | --- | --- |
| Join | Opt-in | Guardian enables once, then child-only challenges |
| Board | Full ranking | Own rank + top 5 only |
| Shown as | Display name or alias | Alias + fixed avatar |
| Rewards | Rank, badges | Badges only |

No contact between learners.

## Success criteria

- Scores rebuild from `ReviewLog` and ignore non-due reviews, fast wrong answers and added words.
- Child boards show only alias, avatar, own rank and top 5 (tests).

## Constraints

- Depends on WB-28 (server-side checking), WB-24 (alias, guardian), WB-22.
- Out of scope: admin- or teacher-created challenges, announcements (phase 2).
