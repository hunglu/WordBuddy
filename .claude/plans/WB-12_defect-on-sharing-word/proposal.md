---
title: WB-12_Defect on sharing word
status: implemented
type: bugfix
issue: 12
pr: 13
affects: docs/features/vocabulary.md, docs/features/vocabulary-builder.md
supersedes: none
version: 5.0
created: 2026-10-03T00:05:53+07:00
updated: 2026-10-03T19:46:40+07:00
---

## Problem

Sharing a word has three UI / logic defects and one leftover table.

| # | Area | Defect |
| --- | --- | --- |
| 1 | UI + logic | A `Shared` word can be deleted with no restriction or warning. |
| 2 | UI | In the Shared Pool, the owner sees **Add to My List** on their own word. |
| 3 | Logic | After the owner deletes a shared word, it still appears in the Shared Pool. |
| 4 | Database | `VocabularyWordIdRemaps` no longer serves a purpose and should be removed. |

## Goal

- Deleting a shared word follows a clear, enforced rule, and the user is told what happens.
- The owner never sees **Add to My List** for their own word.
- The Shared Pool never shows a word its owner has deleted.
- `VocabularyWordIdRemaps` is dropped.

## Target users

Both — children and adults. Child sharing restrictions stay as they are.

## Success criteria

_Not specified._

## Constraints

_Not specified._
