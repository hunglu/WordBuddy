# Plan: Vocabulary Builder & Recall Check-up

## Summary

Let a learner build a personal vocabulary set (their own words, definitions, examples), share
individual words into a moderated community pool that other learners can browse and adopt, and
run a configurable recall "check" (N random words from their own list, self-graded) with
progress tracked over time. `Content` owns the word data (personal + shared pool + moderation
state); `Progress` owns recall-check results and trend data — mirroring the existing
`Lesson`/`LearnerProgress` split (plain cross-service `Guid`s, no FKs, no project references).

## Affected services / areas

- **Content** — new `PersonalVocabulary` feature area: personal words, sharing/moderation
  workflow, community pool browsing, random-word selection for a check session.
- **Progress** — new `VocabularyRecall` feature area: submitting check results, per-word
  known/learning status, session history for trend display.
- **WordBuddy.UI** — new Vocabulary Builder page, recall-check flow, community pool page, admin
  moderation queue, a Progress page section, plus the new API/hook/type plumbing and a
  `vite.config.ts` proxy entry.

No changes to Identity or Quiz. No new MassTransit contracts (see Data/migration notes and Open
questions for why).

## Backend approach

### Content service (`WordBuddy.Content.*`)

**New domain entity** (`Content.Domain`) — deliberately separate from the existing
lesson-scoped `VocabularyItem` (which stays curated, admin-authored, `LessonId`-bound):

- `PersonalVocabularyWord : Entity` — `OwnerUserId` (`Guid`, plain field, no FK — same pattern
  as `LearnerProgress.UserId`), `OwnerAgeGroup` (`AgeGroup`, snapshotted at creation from the
  caller's `age_group` JWT claim — preserves the authoring context for moderation even if the
  account's age group changes later), `Word`, `Definition`, `Example` (nullable), `ShareStatus`
  (new enum `VocabularyShareStatus { Private, PendingReview, Shared, Rejected }`),
  `VisibleToChildren` (`bool`, default `false`, settable only by a moderator on approval),
  `CreatedAtUtc`, `ModeratedAtUtc` (nullable), `ModeratedByUserId` (nullable). Domain methods:
  `RequestShare()` (Private/Rejected → PendingReview), `Approve(bool visibleToChildren,
  Guid moderatorId)`, `Reject(Guid moderatorId)` — guard invalid transitions by returning
  `Result`/throwing only for truly-invalid state (mirrors `LearnerProgress.RecordCompletion`'s
  style of small domain behavior methods).

**New repository interface** `IPersonalVocabularyWordRepository` (`Content.Application.Interfaces`):
`AddAsync`, `GetOwnedByIdAsync(id, ownerUserId)` (returns `NotFound` if missing *or* not owned —
never leaks existence of another user's word), `GetByOwnerAsync(ownerUserId)`,
`GetRandomByOwnerAsync(ownerUserId, count)` (random subset of the caller's own words —
`ORDER BY NEWID()` is fine at this scale), `GetSharedAsync(childSafeOnly)` (only
`ShareStatus == Shared`, additionally filtered to `VisibleToChildren == true` when
`childSafeOnly`), `GetPendingModerationAsync()` (admin queue), `UpdateAsync`, `DeleteAsync`.

**Commands** (`Features/PersonalVocabulary/Commands/`), each following the
`develop-webapi` skill's command/validator/handler template:

- `AddPersonalVocabularyWord(OwnerUserId, OwnerAgeGroup, Word, Definition, Example?)` — any
  authenticated user; creates with `ShareStatus = Private`.
- `RequestShareVocabularyWord(WordId, RequestingUserId)` — moves the caller's own word to
  `PendingReview`. Gated by a new `[Authorize(Policy = "CanShareVocabulary")]` (see
  Authorization below) — **child accounts cannot initiate sharing**; only an adult-owned word
  (or an admin acting on any word) can be submitted for review. This is the feature's
  first-principles child-safety gate on the *write* side.
- `ModerateSharedVocabularyWord(WordId, Approve, VisibleToChildren, ModeratorUserId)` —
  `[Authorize(Policy = "AdminOnly")]`. Approve → `Shared` (+ the moderator's explicit
  `VisibleToChildren` call); reject → `Rejected`. This is the gate on the *publish* side —
  every shared word, regardless of who authored it, needs an explicit human decision before
  it's visible to anyone else, and an explicit second decision before it's visible to children.
- `AddSharedVocabularyWordToMyList(SharedWordId, RequestingUserId)` — copies a `Shared` pool
  word into the caller's own list as a new `Private` `PersonalVocabularyWord` (gives "sharing"
  real utility: other learners can adopt a word into their own practice set, not just view it).
- `DeletePersonalVocabularyWord(WordId, RequestingUserId)` — owner-only delete (`GetOwnedByIdAsync`
  enforces this).

**Queries** (`Features/PersonalVocabulary/Queries/`):

- `GetMyVocabularyWords(OwnerUserId)` — the caller's own list, any `ShareStatus`.
- `GetSharedVocabularyWords(RequestingAgeGroup)` — community pool; the handler applies the
  `VisibleToChildren` filter itself (not just an endpoint-level policy) so a Child-authenticated
  request can never receive a pool item a moderator didn't explicitly clear for children, even
  if called directly. This is a data-level enforcement of the repo's "additional content
  restrictions for `AgeGroup = Child`" convention, layered under the endpoint's `[Authorize]`.
- `GetRandomVocabularyWordsForCheck(OwnerUserId, Count)` — starts a check session; `Count`
  validated 1–50.
- `GetPendingVocabularyModeration()` — `[Authorize(Policy = "AdminOnly")]`, the moderation queue.

**Controller** `PersonalVocabularyController` (`api/vocabulary`), same shape as
`LessonsController`: inject handler interfaces, map `Result`/`Result<T>` via
`ResultExtensions.ToProblemResult`. Add a `ClaimsPrincipalExtensions` to `Content.Api` (mirrors
Progress's) for `GetUserId()`, `GetAgeGroup()`, `IsAdmin()`.

**Authorization** (`Content.Api`'s `AddAuthorization` — currently registered with zero named
policies; this feature adds the first ones):
- `AdminOnly`: `RequireClaim("is_admin", "true")`.
- `CanShareVocabulary`: `RequireAssertion` — `is_admin == true` OR `age_group != "Child"`.

**Caching** — only `GetSharedVocabularyWords` qualifies as "high-read, low-mutation" per
CLAUDE.md's caching guidance; personal lists are per-user and mutate too often to bother caching.
Key `content:vocabulary-shared:{childSafeOnly}`, short absolute expiry (e.g. 5 min), cache-aside
invalidation (delete both key variants) on `ModerateSharedVocabularyWord` success.

**Migration** — one new table, `PersonalVocabularyWords`, via EF Core migration in
`Content.Infrastructure`.

### Progress service (`WordBuddy.Progress.*`)

**New domain entities** (`Progress.Domain`):

- `VocabularyRecallStat : Entity` — `UserId`, `VocabularyWordId` (plain `Guid`, Content's word
  id — no FK, same independence pattern as `LearnerProgress.LessonId`), `Word` (denormalized
  copy of the word text, captured at submit time — see Data/migration notes for why this avoids
  a cross-service call), `TimesChecked`, `TimesKnown`, `Status` (new enum
  `RecallStatus { Learning, Known }` — last-check-wins: `Known` if the most recent check marked
  it known, else `Learning`), `LastCheckedAtUtc`.
- `VocabularyRecallSession : Entity` — `UserId`, `CheckedAtUtc`, `WordsChecked`, `WordsKnown`
  (one row per check submission, purely for the progress-over-time trend).

**Repository** `IVocabularyRecallRepository`: `UpsertStatsAsync(userId, results)` (per-word
insert-or-update), `AddSessionAsync(session)`, `GetStatsByUserAsync(userId)`,
`GetRecentSessionsByUserAsync(userId, take)`.

**Command** `SubmitVocabularyRecallCheck(UserId, Results: IReadOnlyList<(WordId, Word, Known)>)`
— validated non-empty, max 50 items; for each result, upserts the matching
`VocabularyRecallStat` and appends one `VocabularyRecallSession` summarizing the batch. Follows
`RecordProgressCommandHandler`'s shape (`Result`, no return value, `NoContent()` from the
controller).

**Query** `GetVocabularyRecallProgress(UserId)` → `VocabularyRecallProgressDto` (
`TotalWordsTracked`, `KnownCount`, `LearningCount`, `RecentSessions: List<VocabularyRecallSessionDto>`).

**Controller** — new actions on the existing `ProgressController` (or a small new
`VocabularyRecallController` nested under `api/progress/vocabulary-recall` — coder's choice,
whichever keeps `ProgressController` from getting crowded), `[Authorize]`, using
`ClaimsPrincipalExtensions.GetUserId()` exactly as `RecordProgress`/`GetUserProgress` already do.

**Migration** — two new tables, `VocabularyRecallStats` and `VocabularyRecallSessions`, via EF
Core migration in `Progress.Infrastructure`.

## Frontend approach

- **`src/types/index.ts`** — add `VocabularyShareStatus` union type, `PersonalVocabularyWord`,
  `VocabularyRecallCheckWord`, `VocabularyRecallResultItem`, `VocabularyRecallProgress`,
  `VocabularyRecallSession`.
- **`src/api/vocabulary.ts`** (Content, base path `/vocabulary`) — `addVocabularyWord`,
  `getMyVocabularyWords`, `requestShareVocabularyWord`, `deleteVocabularyWord`,
  `getSharedVocabularyWords`, `addSharedWordToMyList`, `getRandomWordsForCheck(count)`, and
  admin-only `getPendingModeration`, `moderateVocabularyWord`.
- **`src/api/progress.ts`** — extend with `submitVocabularyRecallCheck`,
  `getVocabularyRecallProgress`.
- **TanStack Query hooks** (co-located in the pages or a `src/hooks/useVocabulary.ts` — match
  whatever convention the codebase settles on elsewhere; none exists yet, so a dedicated hooks
  file is fine): `['vocabulary','mine']`, `['vocabulary','shared', childSafeOnly]`,
  `['vocabulary','moderation','pending']`, `['progress','vocabulary-recall']`. Mutations
  invalidate the relevant query key(s) on success (e.g. add/delete/share invalidate
  `['vocabulary','mine']`; moderate invalidates `['vocabulary','moderation','pending']` and
  `['vocabulary','shared']`; submit-check invalidates `['progress','vocabulary-recall']`).
- **Pages**:
  - `VocabularyBuilderPage.tsx` — add-word form (React Hook Form + Zod), list of the learner's
    own words with a `ShareStatus` badge, a "Share" button (hidden for Child accounts client-side
    as a UX nicety — the real enforcement is the `CanShareVocabulary` policy server-side), and
    delete.
  - `VocabularyCheckPage.tsx` — pick a count (local component state or a small persisted
    Zustand preference for "last used count" — client-owned UI preference, not server data),
    fetch random words, flip through them (Framer Motion), learner marks known/still-learning
    per word (component-local state until submit), submit the batch.
  - `VocabularySharedPoolPage.tsx` — browse `Shared` pool, "Add to my list" button.
  - `VocabularyModerationPage.tsx` — admin-only (guard on `user.isAdmin` from `authStore`, same
    idea as `ProtectedRoute`'s token-expiry check), list `PendingReview` words, approve (with a
    "visible to children" checkbox) / reject actions.
  - `ProgressPage.tsx` — extend with a "Vocabulary Recall" section (known/learning counts via
    the existing `CountUpStat` component, recent session list).
- **Routing** — add the four new routes under the protected `AppLayout`, plus nav links; the
  moderation route render-guards on `isAdmin` (in addition to the API rejecting a non-admin call).
- **`vite.config.ts`** — add `'/api/vocabulary': { target: CONTENT_URL, changeOrigin: true }`.

## Data / migration notes

- New Content migration: `PersonalVocabularyWords` table.
- New Progress migrations: `VocabularyRecallStats`, `VocabularyRecallSessions` tables.
- **No new MassTransit event.** Deleting a `PersonalVocabularyWord` leaves its historical
  `VocabularyRecallStat`/`VocabularyRecallSession` rows in Progress in place — this is
  intentional, not an oversight: `VocabularyRecallStat.Word` is denormalized (captured at submit
  time from data the frontend already has, since it just fetched the check-session words from
  Content), so Progress never needs a live cross-service lookup to render "words you know," and
  a deleted word's history simply stops growing rather than becoming unreadable. This also avoids
  making this feature the first thing in the codebase to stand up MassTransit end-to-end
  (currently wired nowhere — `WordBuddy.Shared.Contracts` has no message types yet), which would
  be a disproportionate amount of new cross-cutting infrastructure for one feature. See Open
  questions if Sam wants live cleanup instead.

## Open questions

1. **Is "share" opt-in per word, or automatic for everything a learner enters?** The proposal's
   wording ("All words learner entered could store in system and share to others") could mean
   either. This plan assumes **opt-in per word** (`RequestShareVocabularyWord` is a separate,
   explicit action from `AddPersonalVocabularyWord`, and even then requires moderator approval
   before anyone else sees it) — because defaulting every child's personal word list to
   world-visible-pending-moderation, with no explicit learner action, is a materially different
   (and riskier) privacy posture that this proposal didn't explicitly ask for. Please confirm.
2. **Moderation queue staffing/SLA is unspecified.** This plan builds the admin approve/reject
   mechanism but has no opinion on who moderates or how fast — pool words sit in `PendingReview`
   indefinitely until an admin acts. Out of scope unless Sam wants an auto-approve path for
   adult-authored words (not recommended given children can browse the same pool).
3. **Recall-check "configurable rule" is scoped to just "how many words per session," passed as
   a request parameter (default remembered client-side only).** If Sam wants server-persisted
   per-user settings (e.g. a spaced-repetition schedule, daily reminders), that's materially more
   scope (likely touching Notification too) and should be its own follow-up proposal.
4. **No live cross-service cleanup on word deletion** (see Data/migration notes) — flagging in
   case Sam considers orphaned-but-denormalized recall history unacceptable, in which case the
   fix is standing up the first real MassTransit contract in this repo, which is a bigger lift
   than this feature otherwise needs.
