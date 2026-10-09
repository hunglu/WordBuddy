---
title: WB-26_Supporter dashboard
status: implemented
type: new
issue: 26
pr: 38
affects: docs/features/supporter-dashboard.md
supersedes: none
version: 1.4
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-09T21:45:00+07:00
---

## Problem

A supporter cannot see whether a learner studies, adds words, or actually remembers them.

## Goal

A dashboard in Progress + UI, built as projections of `ReviewLog`:

- Activity: streak, active days per week, calendar heatmap, % of daily goal.
- Words added per day/week by `AddedBy`; words per status; reviews per day.
- **True retention** (main number): % correct on the first attempt of due reviews, also per skill.
- Struggle: leech words, weakest skill, slowest words.
- Gaming signals: very fast wrong answers, many hints, unfinished sessions.
- The learner sees the same dashboard for themselves.

## Target users

Both. Guardian and Teacher see the full view; Peer sees the summary (streak, retention) only.
Time of day is never shown to a Peer.

## Success criteria

- Each metric is computed from `ReviewLog` and can be rebuilt.
- Access follows `CanSupportLearner(permission)`; tests per role and for child learners.
- No child data in logs.

## Constraints

- Depends on WB-22 (ReviewLog) and WB-24 (support links).
- Out of scope: weekly email (phase 2, Notification).
