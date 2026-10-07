# Tasks: WB-22_Vocabulary SRS engine

## Backend — Progress domain

- [ ] Confirm FSRS choice (plan Open question 1); quick NuGet check, record result in the PR body — decision noted before scheduler code
- [ ] Add enums `WordStatus`, `FsrsRating`, `VocabularySkill`, `ExerciseType`, `FsrsPhase` — string-serialised, XML docs
- [ ] Add `IFsrsScheduler` + FSRS-6 implementation (default parameters, no fuzz) — pure, no I/O
- [ ] Add `AnswerGrader` with per-exercise thresholds from options — maps answers to the 4 ratings per D10
- [ ] Add `NewWordCapPolicy` (child backlog steps, adult setting) with options `Vocabulary:NewWordCap` — child result always 5–10
- [ ] Add `LearnerWordState` entity (`CreateNew`, `ApplyReview`, `Activate`, `Deactivate`, status rules) — Mastered/Leech thresholds from options
- [ ] Add `ReviewLog` entity, insert-only (factory only, no setters/update methods)
- [ ] Add `VocabularyLearnerSettings` entity (`NewWordsPerDay` 0–50)

## Backend — Progress infrastructure

- [ ] EF configurations for the 3 entities with indexes from the plan — unique (`UserId`, `SenseId`) on states
- [ ] Migration `AddVocabularySrs` — creates 3 tables; applies on a clean DB
- [ ] Repositories `ILearnerWordStateRepository`, `IReviewLogRepository` (`AddAsync`, counts/queries only), `IVocabularyLearnerSettingsRepository` — no update/delete on review logs
- [ ] Options binding + `appsettings.json` defaults for `Vocabulary:Grading`, `Vocabulary:NewWordCap`, `Vocabulary:Scheduling`

## Backend — Progress application + API

- [ ] Extend `RecordLearnerWordAdded`/`Removed` handlers to create/activate/deactivate `LearnerWordState` in the same save — duplicate events create no extra rows
- [ ] `RecordVocabularyReviewCommand` + validator + handler (server-derived `AttemptNo`, `IsDue`, rating; FSRS only on first attempt of a due/new card) — exactly one `ReviewLog` row per call
- [ ] `GetVocabularySessionQuery` + handler; `X-Client-CurrentDateTime` header parsed and validated (offset only, D-2) — due first, then new up to cap minus today's new words
- [ ] `GetLearnerWordStatesQuery` + handler — caller's active states only
- [ ] `GetVocabularySettingsQuery`, `UpdateVocabularySettingsCommand` + validator — all users, `null` = backlog rule (D-3, D-4)
- [ ] `ClaimsPrincipalExtensions.GetAgeGroup()` feeding the grader `AgeGroup` multiplier (D-7)
- [ ] `VocabularySrsController` at `api/progress/vocabulary` (`session`, `reviews`, `words`, `settings`) using `ToProblemResult` — logs ids/counts only
- [ ] Rate-limit policy `vocabulary-review` on `POST reviews` — 429 + `Retry-After`
- [ ] Register handlers/validators/options in DI

## Backend — Content backfill

- [ ] `ILearnerWordEventPublisher` (Application) + outbox implementation (Infrastructure)
- [ ] `RepublishLearnerWordsCommand` + handler (batches of 500, skips `SystemOwner`, original `AddedAtUtc`) — returns published count
- [ ] Endpoint `POST /api/vocabulary/admin/learner-words/republish` with `AdminOnly` — non-admin gets 403
- [ ] Document the one-time backfill run in Content `README.md`

## Docs

- [ ] ADR 0005: status → accepted, fill in FSRS choice, day boundary, backfill and recall-transition decisions — matches the merged behaviour
- [ ] Progress `README.md`: new endpoints and options

## Tests

- [ ] Unit `AnswerGrader` — all 4 ratings, hint, boundary ms values per exercise type
- [ ] Unit FSRS scheduler — matches py-fsrs reference vectors (first review each rating, lapse, multi-step sequence)
- [ ] Unit `NewWordCapPolicy` — each child backlog step, child never outside 5–10, adult setting and default
- [ ] Unit `LearnerWordState` — status transitions (New → Learning → Review → Mastered, Leech at 4 lapses), activate/deactivate keeps FSRS data
- [ ] Unit `ReviewLog` — factory sets all fields; type exposes no mutators
- [ ] Unit `RecordVocabularyReviewCommandHandler` — first due attempt reschedules; retry and not-due only log; inactive word → NotFound
- [ ] Unit `GetVocabularySessionQueryHandler` — order, cap minus today's count, time zone day boundary, child vs adult
- [ ] Unit learner-word handlers — state created on add, deactivated on remove, duplicate add idempotent
- [ ] Unit `RepublishLearnerWordsCommandHandler` — batches, skips `SystemOwner`, count
- [ ] Integration Progress `POST reviews` — one row per call, rows never change on later calls, 400 on bad input, 401 without token
- [ ] Integration Progress `GET session` — child and adult caps, due before new
- [ ] Integration Progress `GET words` and `settings` — child `PUT` → 403, adult `PUT` → 200
- [ ] Integration Progress consumer — `LearnerWordAdded` creates a state row; replay creates none
- [ ] Integration Content republish — admin 200 + outbox messages per link; non-admin 403
