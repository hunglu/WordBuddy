---
title: WB-16_Vocabulary lexeme sense model
status: implemented
type: change
issue: 16
pr: none
affects: docs/features/vocabulary.md
supersedes: refactor-database-scheme-to-store-vocabulary-item
version: 3.1
created: 2026-10-04T23:49:41+07:00
updated: 2026-10-05T09:00:00+07:00
---

## Problem

One `VocabularyWords` row mixes a word with one meaning. Meanings of the same word are not
grouped, and there is no place for IPA, word forms, CEFR level or translations.

## Goal

- Migrate `VocabularyWords` → `Lexeme` + `Sense`, keeping every id.
- Migrate `UserVocabularyWords` → `LearnerWord`, keeping every id, with `AddedBy` and an optional
  `PersonalContext`.
- Add `SenseTranslation` per locale.
- No API or behaviour change.

## Target users

Both. Child vs. adult behaviour stays exactly as it is today.

## Success criteria

- The migration keeps every id, lesson link, learner link and Progress reference.
- Existing API JSON is unchanged.
- All existing tests pass.
- The child filter (`VisibleToChildren`) still applies at `Sense` level.

## Constraints

- Follows ADR 0004 (`docs/adr/0004-vocabulary-catalog-lexeme-sense.md`, PR #15) and
  `.claude/plans/vocabulary-module/idea.md`.
- Open points to settle in `/plan`: translation language (Vietnamese only vs. the user's native
  language) and lexeme audio naming.

## Revisions

- **v3.0 — 2026-10-05T00:37:40+07:00** — Re-planned with Sam's answers: `LessonVocabularyWords` →
  `LessonSenses`; lexeme key = normalized lemma + part of speech (NULL until auto-fill); orphan
  lexemes deleted; wipe-and-reseed fallback only with Sam's approval; sense enrichment fields out
  of scope (ADR 0004, idea.md D14–D18).
