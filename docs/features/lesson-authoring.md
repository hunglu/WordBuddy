---
feature: Lesson authoring
services: Content | UI
audience: Both
state: shipped
last-updated-by: WB-55_apply-paging-to-table-list
---

# Lesson authoring

Admins create and manage lessons and the words they contain.

## What it does

Admins author lessons end to end in the UI:

```
Create draft → attach senses → reorder → publish → learners read
```

- New lessons are drafts (`isPublished = false`).
- Only vocabulary lessons take senses. A sense can be attached once per lesson.
- Eligible senses: system senses and shared-pool senses (`ShareStatus = Shared`). Private, pending or rejected senses are refused.
- Delete removes the lesson and its sense links, never the senses.
- Concurrent duplicate attach returns 409 (`Lesson.SenseAlreadyAttached`).

## Rules

- **Child vs adult:** only admins write (`AdminOnly`). `POST /api/lessons` moved to `/api/lessons/admin`; non-admins get 403.
- **Learner reads:** drafts are never listed. A child gets only published, child-targeted lessons; a draft or adult-targeted lesson detail is 404 for a child.
- **Senses in a lesson:** Children see only child-targeted lessons. Inside a lesson, system words are always shown; shared-pool words not cleared for children are hidden.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|
| GET | `/api/lessons/admin` | Content | AdminOnly |
| GET | `/api/lessons/admin/{id}` | Content | AdminOnly |
| POST | `/api/lessons/admin` | Content | AdminOnly |
| PUT | `/api/lessons/admin/{id}` | Content | AdminOnly |
| DELETE | `/api/lessons/admin/{id}` | Content | AdminOnly |
| POST | `/api/lessons/admin/{id}/publish` | Content | AdminOnly |
| POST | `/api/lessons/admin/{id}/unpublish` | Content | AdminOnly |
| POST | `/api/lessons/admin/{id}/senses` | Content | AdminOnly |
| DELETE | `/api/lessons/admin/{id}/senses/{senseId}` | Content | AdminOnly |
| PUT | `/api/lessons/admin/{id}/senses/order` | Content | AdminOnly |
| GET | `/api/lessons/admin/senses` | Content | AdminOnly |

## UI

| Route | Page | Notes |
|---|---|---|
| `/admin/lessons` | `AdminLessonsPage` | table, status badge, filters, publish toggle, delete with confirm |
| `/admin/lessons/new`, `/admin/lessons/:id` | `AdminLessonEditorPage` | details form, senses panel (detach, up / down reorder, add-word picker with 300 ms debounced search) |

Admin sidebar link only. Both pages handle `isError`.

## Paging (WB-55)

`GET /api/lessons/admin` is paged and ordered by title, then id. The admin lessons page shows a pager; changing a filter resets to page 1.

## Pending changes

None.

## Change history

- `WB-47_admin-lesson-authoring` (#47, PR #51) — admin lesson CRUD, publish, sense attach / detach / reorder, `AdminOnly` on lesson writes, child filter on lesson reads
- `WB-55_apply-paging-to-table-list` (#55, PR #57) — paged admin lesson list
