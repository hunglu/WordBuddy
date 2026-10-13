---
title: WB-47_Admin lesson authoring
status: done
type: new
issue: 47
pr: 51            # set by /code when it opens the pull request
affects: docs/features/lesson-authoring.md
supersedes: none
version: 1.6
created: 2026-10-13T18:12:59+07:00
updated: 2026-10-13T23:30:00+07:00
---

## Problem

- Lessons and `LessonSenses` are written only by the seeder (`DataSeeder` → `Lesson.AddVocabularyWord`).
- No API or UI to edit, publish, delete a lesson, or attach / detach / reorder its words.
- `POST /api/lessons` exists but has no `AdminOnly` policy — any signed-in user can create a lesson.

## Goal

Admins author lessons end to end in the UI:

```
Lesson CRUD + publish ──► attach senses ──► reorder (SortOrder) ──► learners read
```

## Target users

- Admins: author lessons.
- Children and adults: read only. Children see only child-targeted lessons and child-visible senses.

## Success criteria

- Admin creates, edits, publishes, unpublishes and deletes a lesson from the UI.
- Admin attaches shared or system senses, detaches them, and changes their order.
- Every lesson write endpoint requires `AdminOnly`; non-admins get `403`.
- Child learners never see a sense with `VisibleToChildren = false` in a lesson.

## Constraints

- Grammar rules and daily phrases authoring: out of scope (separate proposal if needed).
- Deleting a lesson must not delete senses (`LessonSenses` cascade only).
- Found in the schema-vs-UI gap analysis of 2026-10-13.
