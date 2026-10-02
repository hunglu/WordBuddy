# Plan: Refactor database scheme to store vocabulary item

## Summary

Move the Content service's two vocabulary tables (`VocabularyItems`, lesson words written by admins,
and `PersonalVocabularyWords`, words learners add) into one `VocabularyWords` table. Add two link
tables: `UserVocabularyWords` (user to word, plus per-user data) and `LessonVocabularyWords` (lesson
to word). One EF Core migration moves the existing data across and removes duplicates. Every
public HTTP contract and every UI behaviour stays the same; the only addition is one optional DTO
field. Progress stores Content word ids, so any id removed by the dedupe is sent to Progress in a
new MassTransit event, and Progress rewrites its stored ids.

## Affected services / areas

- **Content**: new domain model, three new tables, data-migrating EF migration, repository
  rewrite, handler changes for the personal-vocabulary features and the lesson-detail query.
- **Progress**: new idempotent consumer that rewrites `VocabularyRecallStats.VocabularyWordId`.
  No schema change.
- **WordBuddy.Shared.Contracts**: one new message, `VocabularyWordIdsRemapped`. New package version
  and `make publish-shared`.
- **WordBuddy.UI**: one small change. "My words" hides the Share action for words the learner
  adopted rather than wrote (see Frontend).
- **e2e/api, e2e/ui**: run the existing vocabulary scenarios again as a regression check, plus one
  new scenario for adopt-then-delete.
- **Identity**: untouched. Content still never references Identity's tables.

## Current state (verified)

- `VocabularyItem`: `Id`, `LessonId` (required FK, cascade from `Lesson`), `Word`, `Definition`,
  `Example` (required), `AudioAssetId` (FK to `MediaAssets`, NoAction). The audio blob name is
  `vocab-{vocabularyId}-{locale}.mp3`, so **these ids must not change**.
- `PersonalVocabularyWord`: `OwnerUserId` (plain Guid), `OwnerAgeGroup` (snapshot),
  `Word/Definition/Example?`, `ShareStatus` (Private/PendingReview/Shared/Rejected),
  `VisibleToChildren`, moderation fields. **Adopting** a shared word
  (`AddSharedVocabularyWordToMyListCommandHandler`) currently **copies** it into a new row. That
  copy is the main source of duplicates.
- Progress `VocabularyRecallStat` stores `(UserId, VocabularyWordId, Word)` with a unique index on
  `(UserId, VocabularyWordId)`. `VocabularyWordId` is the `PersonalVocabularyWord.Id` returned by
  `GET /api/vocabulary/check`. It is a plain Guid with no FK (ADR 0001).
- Endpoints in `PersonalVocabularyController` (`/api/vocabulary`): `POST`, `GET mine`,
  `POST {id}/share` (`CanShareVocabulary`), `DELETE {id}`, `GET shared`,
  `POST shared/{id}/add-to-mine`, `GET check`, `GET moderation/pending` and `POST moderation/{id}`
  (`AdminOnly`).

## Target model (Content.Domain)

**`VocabularyWord`** replaces both entities. Table `VocabularyWords`:

| Column | Notes |
| --- | --- |
| `Id` | Kept from the source row (see migration rules) |
| `Word`, `Definition`, `Example?` | Same max lengths as today (200 / 2000 / 500). `Example` becomes nullable; lesson words keep a non-empty value. |
| `NormalizedWord` | `UPPER(LTRIM(RTRIM(Word)))`. Indexed. Used for dedupe and lookup. |
| `ContentHash` | Hash of normalized Word + Definition + Example. Non-unique index for lookup, plus a **unique filtered** index on `(ContentHash, OwnerUserId) WHERE Source = 'Learner'`. System (lesson) words are never merged with each other, because their ids name audio blobs, so identical system words may coexist. |
| `AudioAssetId?` | FK to `MediaAssets`, NoAction (unchanged). |
| `Source` | New enum `VocabularySource { System, Learner }`. |
| `OwnerUserId` | The author. For system words this is `SystemOwner.UserId`. |
| `OwnerAgeGroup?` | Null for system words. |
| `ShareStatus`, `VisibleToChildren`, `ModeratedAtUtc?`, `ModeratedByUserId?` | Moved unchanged with their domain methods (`RequestShare`/`Approve`/`Reject`). System words are always `Private`, so they never enter the shared pool. Lesson visibility rules are unchanged. |
| `CreatedAtUtc` | System words get the migration timestamp. |

**System owner.** Add `SystemOwner.UserId` in Content.Domain, a well-known constant Guid
`00000000-0000-0000-0000-00000000c0de`. It is a plain value, not a row in Identity: no Identity
seed and no cross-DB FK. JWTs cannot carry this id, because Identity generates `Guid.NewGuid()`
user ids. A guard in the `VocabularyWord` factory still rejects learner-created words that use it.

**`UserVocabularyWord`**, table `UserVocabularyWords`: `Id`, `UserId`, `VocabularyWordId` (FK,
cascade), `AddedAtUtc`, `IsAuthor` (bool). Unique index on `(UserId, VocabularyWordId)`. This is
where future per-user settings go; the proposal doesn't name any, so none are added now. A
learner's word list ("mine", "check") is exactly their link rows. System words are **not** linked to
the system owner. Ownership comes from `OwnerUserId`, and linking the system user to thousands of
lesson words adds nothing (see Open questions).

**`LessonVocabularyWord`**, table `LessonVocabularyWords`: `LessonId` (FK, cascade from Lesson),
`VocabularyWordId` (FK, NoAction), `SortOrder`. Composite primary key. This replaces
`VocabularyItem.LessonId`, because one deduped word may now belong to several lessons.
`Lesson.Vocabulary` becomes a navigation through this table. `LessonDetailDto` keeps its shape.

## Backend approach

### Content

Follow the develop-webapi skill's existing `Features/PersonalVocabulary/...` and
`Features/Lessons/...` folders. **No new endpoints and no new feature folders.** The handlers stay
where they are; only their bodies and the repository they call change.

- **Repositories.** Replace `IPersonalVocabularyWordRepository` with `IVocabularyWordRepository`:
  - `FindByContentHashAsync`
  - `GetLinkedToUserAsync(userId)`
  - `GetRandomLinkedToUserAsync(userId, count)`
  - `GetSharedAsync(childSafeOnly)`
  - `GetPendingModerationAsync`
  - `GetByIdAsync`
  - `AddAsync`, `UpdateAsync`
  - `LinkAsync`, `UnlinkAsync`
  - `DeleteIfOrphanedAsync`

  `ILessonRepository.GetByIdWithDetailsAsync` includes words through `LessonVocabularyWords`.
- **AddPersonalVocabularyWord**:
  1. Compute the normalized hash.
  2. If a word with that hash exists **and the caller may see it**, link to it. "May see it" means
     the caller authored it, it is a system word, or it is `Shared` and child-visible for Child
     callers.
  3. Otherwise create a new `Learner` word and link it with `IsAuthor = true`.
  4. Return the word id, which can be an existing id.

  A Child caller is never linked to a word they could not see today, such as another learner's
  private or non-child-visible word. In that case a hash collision must not leak that the word
  exists. The plan's answer: uniqueness is `(ContentHash, OwnerUserId)` for `Learner` words only,
  so private duplicates by different authors may coexist. This keeps privacy and stops cross-user
  leakage. Dedupe means the same author never stores a word twice, and adopting or re-adding a
  shared or system word links to it instead of copying. System words are never merged with each
  other, so identical lesson words may remain. This is the main
  judgment call; see Open questions.
- **AddSharedVocabularyWordToMyList**: link instead of copy. The child and shared-status checks are
  unchanged. Adopting twice is idempotent and returns the same id. The return value is now the
  shared word's id; the shape is unchanged (`Guid`).
- **DeletePersonalVocabularyWord**: remove the caller's link. If the caller is the author, the word
  is `Learner`-sourced, no other links exist, and it is not `Shared`, also delete the word.
  Otherwise keep it, so other learners' lists and the pool stay intact. A `NotFound` result when
  the caller has no link keeps today's 404.
- **RequestShareVocabularyWord**: allowed only when the caller is the author. Today a learner can
  share an adopted copy, which put a duplicate back into the pool. For a non-author it now returns
  the same `Conflict` code (`PersonalVocabularyWord.InvalidShareRequest`).
- **Moderate / GetShared / GetPendingModeration**: same logic on `VocabularyWord`. Error code
  strings stay `PersonalVocabularyWord.*`, so API clients see no change.
- **GetMyVocabularyWords / GetRandomVocabularyWordsForCheck**: project from the link rows.
  `PersonalVocabularyWordDto` keeps all its fields and gains `IsAuthor` (additive). For adopted or
  system words, `OwnerUserId` keeps reporting the requester, so the response matches today's
  copy-based behaviour, and `ShareStatus` reports `Private`.
- **Caching**: the existing `content:lesson:{id}` keys are unaffected at runtime. Every lesson key
  is stale once after the migration, which ends at absolute expiry or a Redis flush on deploy
  (noted in the test report). If future word edits touch lesson words, invalidate
  `content:lesson:{lessonId}` for every linked lesson. No such edit endpoint exists today.
- **Messaging**:
  - Content gets an outbox-style table `VocabularyWordIdRemaps` (`OldId`, `NewId`,
    `PublishedAtUtc?`), filled by the migration.
  - A Content `BackgroundService`, `VocabularyIdRemapPublisher`, publishes unpublished rows on
    startup as `VocabularyWordIdsRemapped { IReadOnlyList<(Guid OldId, Guid NewId)> }` in batches
    of 500, then stamps them.
  - MassTransit is in-memory locally, so Progress must be running. If it isn't, the rows stay
    unpublished and the next startup retries.
- **Authorization**: no policy changes. `CanShareVocabulary` and `AdminOnly` stay as they are.

### Progress

- `VocabularyWordIdsRemappedConsumer` in Infrastructure, calling an Application command
  `RemapVocabularyWordIds` (CQRS template).
- For each `(old, new)` pair, per user:
  - If no stat exists for `new`, update `VocabularyWordId` in place.
  - If one exists, merge the counters into the `new` row and delete the `old` row.
- Idempotent, because a second run finds no `old` rows. Sessions store aggregates only (verify while
  coding); if they hold word ids, apply the same rewrite.

### Shared

- `WordBuddy.Shared.Contracts`: add `VocabularyWordIdsRemapped` and bump the minor version. Both
  Content and Progress move to the new package version through `PackageReference`. That is the only
  coupling; there are no project references.

## Migration rules (data)

One Content migration, `UnifyVocabularyWords`. Its `Up` uses raw SQL inside the migration:

1. Create `VocabularyWords`, `UserVocabularyWords`, `LessonVocabularyWords`,
   `VocabularyWordIdRemaps`.
2. **System words.** Insert each `VocabularyItems` row with the **same Id**, `Source = System`,
   `OwnerUserId = SystemOwner`, `ShareStatus = Private`. Insert `LessonVocabularyWords(LessonId, Id)`.
   - If two lesson rows share a hash, keep both. Lesson rows are never merged with each other,
     because their ids name audio blobs. System rows have no uniqueness constraint. If a learner
     word matches several system rows, it collapses into the oldest by `Id` order.
3. **Learner words.**
   - Within the same owner, merge rows with an identical hash. The survivor is the oldest
     `CreatedAtUtc`, unless another row in the group is `Shared`, in which case the `Shared` row
     wins.
   - Across owners, merge **only** these:
     - Adopted copies, meaning a Private row whose hash equals a `Shared` row by another owner.
       The copy collapses into the shared row, and its owner gets a link with `IsAuthor = false`.
     - Rows whose hash equals a system word. These collapse into the system word, and the owner
       gets a link with `IsAuthor = false`.
   - Survivors keep their Id. Every merged-away Id gets a `VocabularyWordIdRemaps(OldId, NewId)` row.
4. Every surviving learner word gets a link for its author with `IsAuthor = true`.
5. Repoint `VocabularyItems.AudioAssetId` onto `VocabularyWords`, drop `VocabularyItems.LessonId`,
   then drop both old tables.
6. `Down` rebuilds both old tables from the new ones. Information from merged rows is lost in
   `Down`, which is acceptable for a one-way refactor; it is documented in the migration's XML
   comment.

Applying the migration (`dotnet ef database update` or `make k8s-migrate SERVICE=content`) is
ask-gated, so Sam runs or approves it.

## Frontend approach

- `src/types/index.ts`: add `isAuthor: boolean` to the personal-vocabulary word type.
- "My words" page/component: show the Share button only when `isAuthor`. Adopted words already
  show `Private` today, so this only removes the action that recreated pool duplicates.
- The other parts have no change: TanStack Query keys, Zustand, routes, `/vocabulary/shared`,
  `/vocabulary/moderation`, and the recall-check page. The `isError` handling stays as it is.
- Child behaviour in the UI is unchanged.

## Child vs. adult behaviour

Child and adult behaviour does not change. Child callers still see only `Shared` words with
`VisibleToChildren = true` in the pool and on adopt. Dedupe-on-add never links a Child, or anyone,
to a word they couldn't see before. Lesson-word visibility still follows the Lesson's existing
rules. `OwnerAgeGroup` is kept for moderation context. Integration tests cover both age groups for
add, adopt and pool listing.

## Data / migration notes

- Content: one new migration, `UnifyVocabularyWords` (schema plus data, described above). Old
  migrations stay untouched.
- Progress: no schema migration. Ids are rewritten by the event consumer.
- Shared.Contracts: package version bump and `make publish-shared` before rebuilding the Docker
  images.
- Order of rollout: publish Contracts, deploy Progress with the consumer, migrate and deploy
  Content. The Content publisher retries until Progress has consumed the remaps.

## Open questions

1. **Success criteria (proposed, please confirm):**
   - (a) After migration, `VocabularyItems` and `PersonalVocabularyWords` no longer exist, and every
     former row is reachable as a `VocabularyWord`, either by its own id or through
     `VocabularyWordIdRemaps`.
   - (b) No two `Learner` rows share `(ContentHash, OwnerUserId)`, and no learner row
     duplicates a system or shared word. Identical system (lesson) words may remain.
   - (c) Adopting a shared word creates a link, not a new word row.
   - (d) Every existing Content integration test and every `e2e/api` / `e2e/ui` vocabulary scenario
     passes without changes, except the additive `isAuthor` field.
   - (e) Lesson audio still resolves through `vocab-{id}-{locale}.mp3` for every lesson word.
   - (f) After the remap event, Progress recall stats for a merged word show the summed counts under
     the surviving id.
2. **How strict is "no duplicates"?** The plan **does not** merge two different learners' private
   words with the same text. Merging them would link one learner to another's private word, or
   reveal that it exists. If you want global dedupe, private words need a different model, such as
   a shared canonical word plus per-user private definitions. That is a bigger change.
3. **Dedupe key.** The plan uses normalized Word + Definition + Example, not Word alone. Words
   like "bank" carry different meanings, so merging by Word alone would lose definitions. Confirm.
4. **Link system words to the system owner?** The plan sets `OwnerUserId = SystemOwner` and adds no
   `UserVocabularyWords` rows for the system user. Say so if you want literal link rows too.
5. **System owner Guid.** Is `00000000-0000-0000-0000-00000000c0de`, defined only in Content, OK?
   Or should Identity own a real "system" account? That needs an Identity seed plus config in
   Content, which is more moving parts and adds no behaviour.
6. **Lesson-to-word as many-to-many.** The plan adds `LessonVocabularyWords`, so one word can sit in
   many lessons later. The migration still never merges two lesson rows, because their ids name
   audio blobs. OK, or keep `LessonId` on the word as one-to-many?
7. **Sharing adopted words.** Non-authors can no longer request sharing for an adopted word. Today
   they can, which duplicates the pool. Confirm this behaviour change; it is the only learner-visible
   one.
8. **Blocked proposal `vocabulary-builder-and-checkup`.** Its integration and E2E tests were never
   run against a live DB, and its `AddPersonalVocabularyWords` migration is unapplied there. This
   refactor builds on that migration. Should `/test` on that slug go first, so this one doesn't
   inherit unverified behaviour? The plan assumes yes.
