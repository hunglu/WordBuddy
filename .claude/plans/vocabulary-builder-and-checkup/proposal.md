---
title: New Feature to build up vocabulary and check up
status: blocked
type: new
issue: none
affects: docs/features/vocabulary-builder.md
supersedes: none
created: 2026-09-23
---

## Problem

Learners have no way to enter their own vocabulary words (beyond what's provided in lessons), and
no way to self-check retention against their own list — there's no lightweight, configurable
recall check (e.g. "quiz me on 5 random words from my list") separate from full lesson quizzes.

## Goal

Let a learner build up a personal vocabulary set by entering their own words, and periodically
check recall against it using a configurable rule (e.g. number of random words per check
session), with visible progress over time.
All words leaner entered could store in system and share to others.

## Target users

Both children and adults — existing child-account content restrictions still apply.

## Success criteria

- Learner can add/save their own vocabulary words to a personal set.
- Learner can run a recall check with a configurable rule (e.g. check N random words from their
  set).
- Progress is visible — how many words are known vs. still learning, tracked over time.

## Constraints

_Not specified._

## Test status (2026-09-24)

Implementation is complete (all 53 tasks). All 36 backend unit tests pass. Integration tests
(Content, Progress) and new E2E coverage (`e2e/api`, `e2e/ui`) are written and compile/generate
cleanly, but have not been run against a live database — this repo has no non-Docker local DB
connection configured, and Sam chose to defer starting Docker/applying the two pending EF
migrations for now rather than do it in this session. Not a code defect — see
`test-report.md` for the exact commands to run once a live SQL Server is available.
