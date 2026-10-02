# Plan: Refactor database scheme to store vocabulary item

## Summary

Move the Content service's two vocabulary tables (`VocabularyItems`, lesson words written by admins,
and `PersonalVocabularyWords`, words learners add) into one `VocabularyWords` table. Add two link
tables: `UserVocabularyWords` (user to word, plus per-user data) and `LessonVocabularyWords` (lesson
to word). One EF Core migration moves the existing data across and removes duplicates. Every
public HTTP contract and every UI behaviour stays the same; the only addition is one optional DTO
field. Progress stores Content word ids, so any id removed by the dedupe is recorded by the
migration in a Content remap table. Progress **pulls** those remaps from an internal, non-routed
Content HTTP endpoint (startup + timer), rewrites its stored ids, then acknowledges them.

> **Revision 2 (transport).** The first plan sent remaps as a MassTransit event. That transport
> does not exist: no service references MassTransit or `WordBuddy.Shared.Contracts`, Contracts has
> no messages, there is no broker in compose/k8s, and the in-memory transport is single-process.
> This revision replaces only the remap transport with an HTTP pull. Everything else (domain,
> migration, handlers, UI, the Progress `RemapVocabularyWordIds` command) is unchanged and already
> built.

## Affected services / areas

- **Content**: new domain model, three new tables, data-migrating EF migration, repository
  rewrite, handler changes for the personal-vocabulary features and the lesson-detail query.
  Plus two internal endpoints (`/internal/vocabulary-remaps`) for Progress to pull remaps.
- **Progress**: typed `HttpClient` to Content, a service-token provider, a `SyncVocabularyWordIdRemaps`
  command and a hosted service that rewrites `VocabularyRecallStats.VocabularyWordId`. No schema
  change. New config `ContentApi:*`.
- **WordBuddy.Shared.\***: **untouched**. No Contracts message, no version bump, no
  `make publish-shared`.
- **docker-compose.yml / k8s**: one env var on Progress (`ContentApi__BaseUrl`). Nginx, Vite proxy
  and Ingress are deliberately **not** changed (the internal path must stay unrouted).
- **WordBuddy.UI**: one small change. "My words" hides the Share action for words the learner
  adopted rather than wrote (see Frontend).
- **e2e/api, e2e/ui**: run the existing vocabulary scenarios again as a regression check, plus one
  new scenario for adopt-then-delete. The existing `AddRetrieveShareAndModerate` API test must use
  per-run unique word text: with dedupe-on-add, a fixed text links to the word left over from an
  earlier run (already shared or pending), so the share step then returns Conflict. This is a test
  fixture fix, not a behaviour change. Add one check that `/internal/vocabulary-remaps` is not
  reachable through the public origin.
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
- **Remap outbox (built).** Table `VocabularyWordIdRemaps` (`OldId` PK, `NewId`,
  `PublishedAtUtc?`) is filled by the migration. The column keeps its name (the migration is done
  and generated); its meaning is now **"acknowledged by Progress at"**. Documented in the entity's
  XML comment. A row with `PublishedAtUtc = null` is pending.
- **Authorization for learner endpoints**: no policy changes. `CanShareVocabulary` and `AdminOnly`
  stay as they are.

### Remap transport — Progress pulls from Content over HTTP (revision 2)

#### Content: internal endpoints

New feature folder `Features/VocabularyRemaps/` following the develop-webapi templates, and a new
`InternalVocabularyRemapsController` at route **`/internal/vocabulary-remaps`** (deliberately
outside `/api`):

| Endpoint | Use case | Result |
| --- | --- | --- |
| `GET /internal/vocabulary-remaps?limit=500` | `Queries/GetPendingVocabularyWordIdRemaps` | `200 { items: [{ oldId, newId }] }`, rows with `PublishedAtUtc IS NULL` ordered by `OldId`, `limit` 1–500 (validator). Empty list when nothing is pending. |
| `POST /internal/vocabulary-remaps/acknowledge` body `{ oldIds: Guid[] }` | `Commands/AcknowledgeVocabularyWordIdRemaps` | `204`. Stamps `PublishedAtUtc = UtcNow` for listed rows that are still null. Unknown or already-acknowledged ids are ignored, so it is idempotent. Validator: 1–500 ids, no empty Guid. |

- Repository: `IVocabularyWordIdRemapRepository` in `Application/Interfaces` (`GetPendingAsync(limit)`
  returning an Application record `VocabularyWordIdRemapDto(OldId, NewId)`, and
  `AcknowledgeAsync(oldIds)` as a single set-based `ExecuteUpdateAsync`). EF implementation in
  Infrastructure next to `VocabularyWordRepository`. The `VocabularyWordIdRemap` persistence class
  stays in Infrastructure; Application never sees it.
- **No caching** on these queries: the data is mutable, tiny, and read by one caller.
- **Rate limiting**: register an `Internal` policy in `RateLimitingConfiguration` (generous, e.g. 60
  req/min) and apply it to the controller, so the global default doesn't throttle catch-up loops.
- Payload contains only word ids (no user ids, no word text), so no PII and no child data cross the
  wire. Swagger: mark the controller `[ApiExplorerSettings(IgnoreApi = true)]`.

#### Endpoint protection (two layers)

What exists today: every service validates the **same symmetric** `Jwt:Secret` + `Jwt:Issuer`
(Identity issues, the others only validate, `ValidateAudience = false`). There is no
service-to-service auth, no API key mechanism and no client-credentials flow.

1. **Network-only exposure.** The path is `/internal/...`, not `/api/...`. `nginx.conf`,
   `vite.config.ts` and `k8s/ingress.yaml` only route `/api/<prefix>` to Content, and `/` goes to the
   UI. So from outside, the path reaches the SPA's index (or 404), never Content. It is reachable
   only on the container network (`http://content-api:8080`) and in dev on `localhost:5081`. None of
   those three files are changed; a new E2E check pins this.
2. **Service JWT + policy.** New Content policy `InternalService` requiring claim
   `wb_service = progress` (`RequireClaim("wb_service", "progress")`). Progress mints its own
   short-lived token (5 min, issuer = `Jwt:Issuer`, `sub = wordbuddy-progress`,
   `wb_service = progress`) signed with the shared `Jwt:Secret` it already has, through a
   `ServiceTokenProvider` in Progress.Infrastructure (cached until 1 min before expiry). Identity
   never emits `wb_service`, so no learner or admin token passes the policy (tested: 401 without a
   token, 403 with a learner or admin token, 200 with a service token).

   Known limit: any holder of `Jwt:Secret` could mint such a token. All services already hold it, so
   this adds no new trust; it is the same trust boundary as today. A proper client-credentials flow
   belongs to the planned Auth0 move (Open question 11).

#### Progress: client, orchestration, hosted service

- **Application**: `Interfaces/IContentVocabularyRemapClient` with
  `GetPendingAsync(int limit, CancellationToken)` → `Result<IReadOnlyList<VocabularyWordIdRemapPair>>`
  and `AcknowledgeAsync(IReadOnlyList<Guid> oldIds, CancellationToken)` → `Result`. Failures are
  typed errors (`ContentApi.Unavailable`, `ContentApi.Unauthorized`), never thrown.
- **Application**: new command `Features/VocabularyRecall/Commands/SyncVocabularyWordIdRemaps`
  (no input besides batch size; validator checks 1–500). Its handler loops: fetch a batch → if empty,
  return `Success(totalApplied)` → call the existing `RemapVocabularyWordIds` handler (via its
  `ICommandHandler` interface) → on success, acknowledge the batch → repeat while the batch was full.
  If the remap fails, it does **not** acknowledge and returns the failure. Cap at 50 batches per run
  so one run cannot spin forever.
- **Infrastructure**: `ContentVocabularyRemapClient`, a typed `HttpClient` registered in
  `AddInfrastructure` with `BaseAddress = ContentApi:BaseUrl`. A `DelegatingHandler` attaches the
  service JWT. Resilience: `Microsoft.Extensions.Http.Resilience` `AddStandardResilienceHandler`
  (Progress.Infrastructure only), with an attempt timeout of 10 s, a total timeout of 30 s, and 3
  exponential retries on 5xx, 408 and transient network errors. No retry on 401/403/404. A 404 means
  "Content not migrated yet" and maps to `ContentApi.Unavailable`.
- **Infrastructure**: `VocabularyIdRemapSyncService : BackgroundService`. It waits
  `ContentApi:RemapInitialDelay` (default 15 s, so Content can come up), then runs
  `SyncVocabularyWordIdRemaps` in a fresh DI scope every `ContentApi:RemapPollInterval` (default
  5 min). Exceptions are caught and logged, and the host never stops. Disabled when
  `ContentApi:RemapSyncEnabled = false` (the integration-test factory sets this).
- **Idempotency / Progress down**: rows stay pending in Content until acknowledged. If Progress is
  down, nothing is lost: the next startup catches up. If Progress crashes after the remap commit but
  before the acknowledgement, the next run re-applies, which is a no-op because
  `RemapVocabularyWordIds` finds no `old` rows. Two Progress replicas running at once are safe for
  the same reason (the unique index `(UserId, VocabularyWordId)` plus the merge logic). k8s runs one
  replica today anyway.
- **Logging** (`ILogger<T>`, message templates): `Information` for "applied {Count} remaps in
  {Batches} batches" when Count > 0; `Debug` for "no pending remaps"; `Warning` for a Content
  failure with the error code and status (no token, no URLs with secrets); `Error` with the
  exception for unexpected failures. No user ids are logged.
- **Health**: Progress `/health/ready` does **not** depend on Content. The sync is eventually
  consistent background work, and coupling readiness would take Progress out of rotation whenever
  Content restarts. Content's health is unchanged.
- **Config**:
  - `appsettings.json` (Progress, non-secret): `ContentApi:BaseUrl = http://localhost:5081`,
    `RemapSyncEnabled = true`, `RemapPollInterval = 00:05:00`, `RemapInitialDelay = 00:00:15`.
  - `docker-compose.yml` → `progress-api.environment`: `ContentApi__BaseUrl: http://content-api:8080`.
  - `k8s/progress-deployment.yaml` → `env`: `ContentApi__BaseUrl` = `http://content-api:8080`
    (plain value, not a secret; only Progress needs it, so not in the shared configmap).
  - The JWT secret is reused from the existing `Jwt__Secret`; nothing new is secret.
- **Child vs adult**: the transport carries word ids only. It changes nothing a Child or Adult can
  see; recall stats for both age groups are rewritten the same way.

### Progress: remap command (built, unchanged)

- `RemapVocabularyWordIds`: for each `(old, new)` pair, per user, rewrite in place, or merge the
  counters into the existing `new` row and delete `old`. Idempotent. Sessions hold no word ids
  (checked).

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
   - Within the same owner, merge rows with an identical hash. Survivor precedence (as built):
     `Shared` > `PendingReview` > oldest `CreatedAtUtc`. So a pending row beats an older private
     row, which keeps the moderation queue entry alive.
   - Across owners, merge **only `Private`/`Rejected`** learner rows. `Shared` and `PendingReview`
     rows are never collapsed into another owner's or a system word, so the shared pool and the
     moderation queue keep every entry. The merges are:
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

**Normalisation: C# vs SQL upper-casing.** The migration hashes existing rows with SQL
`UPPER(LTRIM(RTRIM(...)))`; new words are hashed in C# (`Trim().ToUpperInvariant()`). The two can
differ for a few rare letters (for example `ß`, ligatures, and some collation-specific cases).
**Decision: accept and document.** The worst case is that a learner later re-adds such a word and
gets a second row instead of a link. That is a harmless duplicate, it violates no constraint, and it
leaks nothing. Work: an XML note on `VocabularyWord`'s hash method and in the migration, a line in
`docs/features/vocabulary.md` (written by `/test`), and one unit test that pins today's C# behaviour
for `ß`. A one-off C# re-hash of migrated rows is the alternative (Open question 10).

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
- Progress: no schema migration. Ids are rewritten by the pull sync.
- Shared packages: no change, **no `make publish-shared`**.
- Order of rollout:
  1. Deploy Progress with the sync service and `ContentApi__BaseUrl`. Until Content is migrated, the
     endpoint returns 404; Progress logs a Warning and retries on its timer.
  2. Migrate (ask-gated) and deploy Content. Flush Redis or wait out `content:lesson:*` expiry.
  3. Within one poll interval (or on the next Progress restart), Progress applies and acknowledges
     all remaps. Verify with `SELECT COUNT(*) FROM VocabularyWordIdRemaps WHERE PublishedAtUtc IS NULL`
     = 0.

  Reverse order also works. A recall check in the gap may create a stat under a new id while the
  old one still exists; the merge branch of `RemapVocabularyWordIds` folds them together.

## Open questions

1. **Success criteria (proposed, please confirm):**
   - (a) After migration, `VocabularyItems` and `PersonalVocabularyWords` no longer exist, and every
     former row is reachable as a `VocabularyWord`, either by its own id or through
     `VocabularyWordIdRemaps`.
   - (b) No two `Learner` rows share `(ContentHash, OwnerUserId)`, and no **`Private`/`Rejected`**
     learner row duplicates a system or another owner's shared word. `Shared`/`PendingReview` rows
     may still match a system word or each other, by design (see Migration rules, step 3).
     Identical system (lesson) words may remain. Hash differences from rare letters (`ß`) are
     accepted.
   - (c) Adopting a shared word creates a link, not a new word row.
   - (d) Every existing Content integration test and every `e2e/api` / `e2e/ui` vocabulary scenario
     passes without changes, except the additive `isAuthor` field.
   - (e) Lesson audio still resolves through `vocab-{id}-{locale}.mp3` for every lesson word.
   - (f) After Progress's remap sync, Progress recall stats for a merged word show the summed counts
     under the surviving id, and Content has no pending `VocabularyWordIdRemaps` rows.
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
9. **Remap transport (revision 2). You didn't choose, so the default is the HTTP pull described
   above.** Alternatives:
   - (a) **Introduce MassTransit for real**: add a broker (RabbitMQ container in compose and k8s,
     later Azure Service Bus), a Contracts message, a version bump and `make publish-shared`, plus
     an outbox publisher in Content and a consumer in Progress. This is the architecture's stated
     direction, but it adds infrastructure for a one-time data fix.
   - (b) **One-off script / manual step**: export `VocabularyWordIdRemaps` from the Content DB and
     run a SQL or CLI step against the Progress DB during rollout. Simplest code, but it breaks
     database-per-service ownership, it is ask-gated and manual, and nothing catches up if it is
     missed.
   The pull keeps each service owning its own DB, needs no broker, and catches up on its own. Once
   every row is acknowledged everywhere, a later `change` proposal could remove the endpoint, the
   sync service and the table. Should that be noted on the roadmap?
10. **`ß` / upper-casing.** Plan: accept and document. Alternative: after the migration, a one-off
    Content startup task re-hashes all rows in C#. That would need merge logic again whenever a
    re-hash creates a collision, which is why it isn't the default.
11. **Service-to-service auth.** The plan uses a self-minted service JWT with the shared symmetric
    secret plus a non-routed path. That is enough for now. A real client-credentials flow
    (Identity- or Auth0-issued, with audience validation) is out of scope and belongs with the
    planned Auth0 move.
12. **Coder-flagged migration choices (already built, please confirm):**
    - Only `Private`/`Rejected` learner words merge across owners or into system words;
      `Shared`/`PendingReview` never do. Criterion 1(b) is narrowed to match.
    - Within one learner's own duplicates, `PendingReview` beats an older `Private` row.
