---
title: Refactor database scheme to store vocabulary item
status: implemented
type: change
issue: 6
pr: none            # set by /code when it opens the pull request
affects: docs/features/vocabulary.md
supersedes: none
version: 1.5
created: 2026-10-01T22:37:46+07:00
updated: 2026-10-02T17:31:42+07:00
---

## Problem

Vocabulary is stored in two tables in the Content service:

- `VocabularyItems`: words the system provides.
- `PersonalVocabularyWords`: words a user enters.

This causes two problems:

- Vocabulary storage and sharing are hard to manage, and the same word can be stored more than
  once.
- The model is hard to extend.

## Goal

- One table holds everything that belongs to a vocabulary word (the word, meaning/definition,
  examples, etc., and any settings), whether the system or a user added it. This prevents
  duplicates and is easier to maintain.
- A separate table links users to words: user ID + word ID, plus any per-user settings.
- System words are linked to a system user (e.g. admin or system).

## Target users

None directly (issue says "None"). This is a data-model refactor; learners should see no change
in behaviour. Child vs. adult behaviour must stay exactly as it is today.

## Success criteria

_Not specified._

## Constraints

_Not specified._
