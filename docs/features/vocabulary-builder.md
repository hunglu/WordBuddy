---
feature: Vocabulary builder & check-up
services: Content | Progress | UI
audience: Both
state: shipped
last-updated-by: vocabulary-builder-and-checkup
---

# Vocabulary builder & check-up

## What it does

Learners keep a personal vocabulary list: they add their own words (word, definition, optional
example), see them with a share-status badge, and delete them. An adult can submit one of their
own words to a shared community pool. An admin must approve the word before anyone else sees it.
Other learners can browse the pool and copy a word into their own list.

A recall check picks N random words from the learner's own list (N from 1 to 50, chosen by the
learner). The learner marks each word as known or still learning, then submits the batch. The
Progress page shows how many words are known and how many are still learning, plus a list of
recent check sessions.

## Rules

- Words are private on creation. Sharing is opt-in per word and only moves the word to
  `PendingReview`. It becomes `Shared` only after an admin approves it. A rejected word can be
  re-submitted.
- **Child accounts cannot share** (`CanShareVocabulary` policy: admin, or `age_group` not
  `Child`). The UI also hides the Share button for children, but the server is what enforces it.
- When approving, the admin makes a separate, explicit decision to set `VisibleToChildren`
  (default `false`). The shared pool is filtered in the query handler: a Child caller only gets
  pool words with `VisibleToChildren = true`. Adults see every `Shared` word.
- Owner-only access: a learner can read, delete or share only their own words. A word owned by
  someone else returns `NotFound`, so its existence is never revealed.
- Recall status uses the most recent check: `Known` if the last check marked the word known,
  otherwise `Learning`. Each submission (1 to 50 results) also records one session row for the
  trend.
- Deleting a word keeps its recall history in Progress. The word text is copied there at submit
  time, and no cross-service call is made.
- The shared pool is cached per child/adult variant (`content:vocabulary-shared:{childSafeOnly}`,
  5 min absolute expiry). Moderation clears the cache.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|
| POST | `/api/vocabulary` | Content | authenticated |
| GET | `/api/vocabulary/mine` | Content | authenticated |
| POST | `/api/vocabulary/{id}/share` | Content | `CanShareVocabulary` |
| DELETE | `/api/vocabulary/{id}` | Content | authenticated (owner) |
| GET | `/api/vocabulary/shared` | Content | authenticated (child filter in handler) |
| POST | `/api/vocabulary/shared/{id}/add-to-mine` | Content | authenticated |
| GET | `/api/vocabulary/check?count=N` | Content | authenticated |
| GET | `/api/vocabulary/moderation/pending` | Content | `AdminOnly` |
| POST | `/api/vocabulary/moderation/{id}` | Content | `AdminOnly` |
| POST | `/api/progress/vocabulary-recall` | Progress | authenticated |
| GET | `/api/progress/vocabulary-recall` | Progress | authenticated |

## UI

- `/vocabulary`: My Vocabulary (add form, list, share and delete)
- `/vocabulary/check`: recall check
- `/vocabulary/shared`: Shared Pool (add to my list)
- `/vocabulary/moderation`: admin moderation queue (render-guarded on `isAdmin`)
- `/progress`: Vocabulary Recall section (known and learning counts, recent sessions)

## Pending changes

_None._

## Change history

- `vocabulary-builder-and-checkup`: personal vocabulary list, moderated shared pool, recall
  check with progress (PR #10)
