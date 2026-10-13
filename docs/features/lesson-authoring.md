---
feature: Lesson authoring
services: Content | UI
audience: Both
state: proposed
last-updated-by: WB-47_admin-lesson-authoring
---

# Lesson authoring

Admins create and manage lessons and the words they contain.

## What it does

_Proposed — not on main yet._ Admins author lessons end to end in the UI: lesson CRUD, publish /
unpublish, and attach / detach / reorder senses (`LessonSenses`).

## Rules

- **Child vs adult:** only admins write. Children see only child-targeted lessons. Inside a lesson, system words are always shown; shared-pool words not cleared for children are hidden.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

_To be defined by /plan._

## Pending changes

- `WB-47_admin-lesson-authoring` — admin lesson CRUD + sense links, `AdminOnly` on lesson writes (#47)

## Change history
