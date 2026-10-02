---
title: New Feature to build up vocabulary and check up
status: implemented
type: new
issue: none
pr: 10
affects: docs/features/vocabulary-builder.md
supersedes: none
version: 1.6
created: 2026-09-23T00:00:00+07:00
updated: 2026-10-02T10:50:19+07:00
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

## Test status (2026-10-01)

Ran everything against the live docker compose stack (migrations `AddPersonalVocabularyWords` and
`AddVocabularyRecall` were already applied at service startup). All the feature's own tests pass:
backend unit tests (36), integration tests (Content 4, Progress 2) and E2E API
`PersonalVocabularyTests` (3). Still `needs-fixes` because two older defects, not caused by this
feature, make whole suites fail: (1) the UI's `nginx.conf` proxy drops the request path, so every
UI login gets a 404 and 11 of 15 UI scenarios fail, including `vocabulary.feature`; (2) no service
has a `/health` endpoint, so 4 `HealthCheckTests` fail. See `test-report.md`.
