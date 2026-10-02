---
feature: Vocabulary storage
services: Content, Progress
audience: Both
state: shipped
last-updated-by: refactor-database-scheme-to-store-vocabulary-item
---

# Vocabulary storage

## What it does

Every vocabulary word is stored once in a single Content table, `VocabularyWords`. That includes
system/lesson words and words learners type in. Each word has a `Source` (`System` or `Learner`),
an owner, and the share/moderation state. System words belong to the fixed system owner
(`SystemOwner.UserId`).

- `UserVocabularyWords` links a learner to the words in their "My words" list. Each link records
  `AddedAtUtc` and `IsAuthor`, which is true only for the learner who created the word.
- `LessonVocabularyWords` links lessons to their words, ordered by `SortOrder`.

The `GET /api/lessons/{id}` lesson detail JSON is unchanged. The only API change for learners is
an additive `isAuthor` field on personal vocabulary DTOs. On shared and moderation lists it is
always false.

When the migration merged duplicate rows, each old id is recorded in `VocabularyWordIdRemaps`
(`OldId → NewId`). Progress pulls these remaps from an internal Content endpoint and moves its
recall stats to the surviving id. If a stat already exists under the new id, the counters are
merged.

## Rules

- **Dedupe on add.** Adding a word with the same normalized Word + Definition + Example
  (`ContentHash`) links the learner to an existing word instead of creating a new row. The order of
  preference is: the caller's own word, then a system word, then a shared word the caller can see.
  It never links to another learner's private word. Two different learners' private words with the
  same text stay separate.
- **Adopting a shared word** (`add-to-mine`) creates a link, not a copy. Adopting the same word
  again does nothing and returns the shared word's id.
- **Delete** removes the caller's link. The word row is deleted only if all of these hold: the
  caller is its author, nothing else links to it (no learner or lesson), and it is not shared.
  Deleting an adopted shared word leaves it in the shared pool. Deleting without a link returns 404.
- **Share** can only be requested by the word's author. A non-author (for example, someone who
  adopted the word) gets a 409 Conflict (`PersonalVocabularyWord.InvalidShareRequest`). System words
  cannot be shared.
- **Concurrency.** If the same add or adopt arrives twice at the same moment, both calls resolve to
  the same word or link. They no longer fail with a 500.
- **Child vs adult:**
  - A Child account cannot request sharing (`CanShareVocabulary` policy, 403).
  - A Child only sees and adopts shared words with `VisibleToChildren = true`.
  - Dedupe never links a Child to a word they could not see. The migration follows the same rule:
    it kept a Child's private copy when the matching shared word was not child-visible.
  - Adults see all shared words.
  - Admins moderate (`AdminOnly`) whatever their own age group is.

### Accepted limitations

- **Stale id after acknowledgement.** A recall check sent with a pre-migration word id *after*
  Progress has acknowledged that remap creates a stat under the old id. Nothing remaps that stat
  later. This needs a UI session that stayed open across the deploy.
- **`ß` and similar letters.** The C# hash upper-cases differently from SQL Server for a few
  letters (for example, `ß` is not expanded to `SS`). Words containing them may not dedupe
  against rows the migration hashed in SQL.
- **No rate limit on the internal endpoints.** No service has rate limiting yet. It is tracked as
  a separate proposal.

## API

| Method | Route | Service | Auth policy |
|---|---|---|---|
| POST | `/api/vocabulary` | Content | authenticated |
| GET | `/api/vocabulary/mine` | Content | authenticated |
| POST | `/api/vocabulary/{id}/share` | Content | `CanShareVocabulary` (not Child, or admin) |
| DELETE | `/api/vocabulary/{id}` | Content | authenticated |
| GET | `/api/vocabulary/shared` | Content | authenticated (Child: child-visible only) |
| POST | `/api/vocabulary/shared/{id}/add-to-mine` | Content | authenticated (Child: child-visible only) |
| GET | `/api/vocabulary/check` | Content | authenticated |
| GET | `/api/vocabulary/moderation/pending` | Content | `AdminOnly` |
| POST | `/api/vocabulary/moderation/{id}` | Content | `AdminOnly` |
| GET | `/internal/vocabulary-remaps?limit=1..500` | Content | `InternalService` (`wb_service=progress`) |
| POST | `/internal/vocabulary-remaps/acknowledge` | Content | `InternalService` (`wb_service=progress`) |

The `/internal/*` routes are hidden from Swagger. They are not routed by the UI's nginx, the Vite
dev proxy or the k8s ingress, so they are reachable only inside the cluster or compose network.
Identity tokens never carry `wb_service`.

**Progress sync.** `VocabularyIdRemapSyncService` waits `ContentApi:RemapInitialDelay` (15 s)
after startup, then runs every `ContentApi:RemapPollInterval` (5 min). Each run:

1. Fetches a batch of pending remaps, using a 5-minute HS256 service token.
2. Rewrites or merges the matching recall stats.
3. Acknowledges only the batches it applied.

It loops while a batch is full, up to 50 batches. Content failures are logged and retried on the
next tick. The service is turned off with `ContentApi:RemapSyncEnabled = false`.

## UI

On `/vocabulary` ("My words"), the Share action appears only on words where `isAuthor` is true.
Adopted words and system words show no Share button. Nothing else changes on the page.

## Pending changes

## Change history

- `refactor-database-scheme-to-store-vocabulary-item` — one `VocabularyWords` table plus user and
  lesson link tables. Adopting a word now creates a link, and Progress pulls the id remaps (#6)
