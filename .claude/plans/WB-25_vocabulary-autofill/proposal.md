---
title: WB-25_Vocabulary autofill
status: implemented
type: new
issue: 25
pr: 37
affects: docs/features/vocabulary-autofill.md, docs/features/vocabulary.md
supersedes: none
version: 1.5
created: 2026-10-05T15:38:12+07:00
updated: 2026-10-09T15:00:00+07:00
---

## Problem

A learner who adds a word types everything by hand. Lexemes have no part of speech, IPA, audio,
forms or CEFR level, and senses have no translations.

## Goal

- When a word is added, a dictionary API fills IPA (UK/US) and audio; Claude fills a simple
  definition, 2–3 examples and translations.
- Set the part of speech and move each sense to the matching lexeme (D14).
- Add the sense enrichment fields (D18): image, collocations, synonyms/antonyms, register note,
  topic tags, multiple examples.
- The result is stored in the shared catalog and reused.

## Target users

Both. Child: auto-filled senses stay hidden until a supporter with *approve words* or an admin
approves them. Adult: visible at once.

## Success criteria

- Adding a known word reuses the catalog entry and makes no external call.
- A child never sees an unapproved auto-filled sense (tests).
- External API failures leave the word usable with manual data.
- Lexeme lemmas are re-derived from visible senses before lexemes are shown (ADR 0004).

## Constraints

- Approval by a supporter depends on WB-24; admin approval works without it.
- Open points from ADR 0004: translation language (Vietnamese only or the user's native language)
  and lexeme audio file naming. Decide in `/plan`.
- API keys only in user-secrets / env vars. Out of scope: photo capture.
