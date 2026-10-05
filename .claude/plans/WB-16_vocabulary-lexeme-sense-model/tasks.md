# Tasks: WB-16 Vocabulary lexeme sense model

Service: Content only. Paths are relative to `WordBuddy/src/Services/Content/`. Follow `.claude/skills/develop-webapi` and `.claude/skills/ef-migration`.

Fallback rule (D17): if the data-preserving migration becomes too complex, **stop and ask Sam** before switching to "wipe Content DB + re-seed". Never wipe a database without his explicit approval.

## Backend

### Domain

- [x] Rename `VocabularyWord` → `Sense`. Add `LexemeId`, remove `NormalizedWord`, keep `NormalizeWord` / `ComputeContentHash` and all state methods unchanged — the hash output for the same input is unchanged and every `PersonalVocabularyWord.*` error code is kept. — Domain/Sense.cs (was VocabularyWord.cs), VocabularySource.cs, VocabularyShareStatus.cs, SystemOwner.cs
- [x] Change `Sense.CreateSystem` / `CreateLearner` to take `Guid lexemeId`, with no `Lexeme` navigation — both factories set `LexemeId`, and the other behaviour is as before. — Domain/Sense.cs
- [x] Rename `UserVocabularyWord` → `LearnerWord`, with `VocabularyWordId` → `SenseId` and navigation → `Sense`. Add `AddedBy` (default `Learner`) and `PersonalContext?` (max 500) — both constructors set `AddedBy = Learner` and `PersonalContext = null`. — Domain/LearnerWord.cs (was UserVocabularyWord.cs)
- [x] Add the enum `LearnerWordAddedBy { Learner, Supporter, List }` — it is stored as a string. — Domain/LearnerWordAddedBy.cs, LearnerWordConfiguration.cs
- [x] Rename `LessonVocabularyWord` → `LessonSense`, with `VocabularyWordId` → `SenseId` and navigation → `Sense`. `Lesson.VocabularyWords` and `Lesson.AddVocabularyWord(Sense)` keep their names — lesson order logic is unchanged. — Domain/LessonSense.cs (was LessonVocabularyWord.cs), Domain/Lesson.cs
- [x] Add `Lexeme` (fields as in plan.md) with `Create(id, lemma, PartOfSpeech? partOfSpeech = null)`: trim the lemma, normalize it via `Sense.NormalizeWord`, store the optional POS, fail on empty — returns a `Result<Lexeme>`. — Domain/Lexeme.cs
- [x] Add the enums `CefrLevel` (A1–C2) and `PartOfSpeech` — both are stored as strings. — Domain/CefrLevel.cs, Domain/PartOfSpeech.cs, LexemeConfiguration.cs
- [x] Add `SenseTranslation.Create(id, senseId, locale, text)` with BCP-47 shape validation and case normalization (`vi-vn` → `vi-VN`). `Text` is required, max 500 — invalid input returns `Result.Failure`. — Domain/SenseTranslation.cs

### Infrastructure

- [x] Rename `VocabularyWordConfiguration` → `SenseConfiguration` (table `Senses`, index names `IX_Senses_*`, `UX_Senses_ContentHash_OwnerUserId_Learner`). Add `LexemeId` FK `NoAction` + index — the model matches the plan's schema table. — Configurations/SenseConfiguration.cs (was VocabularyWordConfiguration.cs)
- [x] Rename `UserVocabularyWordConfiguration` → `LearnerWordConfiguration` (table `LearnerWords`, `SenseId`, `AddedBy` string max 20 with default `Learner`, `PersonalContext` max 500) — unique index (`UserId`, `SenseId`). — Configurations/LearnerWordConfiguration.cs (was UserVocabularyWordConfiguration.cs)
- [x] Rename `LessonVocabularyWordConfiguration` → `LessonSenseConfiguration`: table `LessonSenses`, key (`LessonId`, `SenseId`), FK to `Senses` `NoAction`; update the `LessonConfiguration` reference — the model builds and lesson cascade delete is unchanged. — Configurations/LessonSenseConfiguration.cs (was LessonVocabularyWordConfiguration.cs); LessonConfiguration.cs needed no change
- [x] Add `LexemeConfiguration`: unique index (`NormalizedLemma`, `PartOfSpeech`) named `UX_Lexemes_NormalizedLemma_PartOfSpeech` with `.HasFilter(null)`, `PartOfSpeech` nullable string max 20, `WordForms` JSON converter copied from `GrammarRuleConfiguration`, two audio FKs `NoAction` — the generated index has no `WHERE` filter. — Configurations/LexemeConfiguration.cs
- [x] Add `SenseTranslationConfiguration`: FK to `Senses` cascade, unique (`SenseId`, `Locale`), `Locale` max 35 — the configuration builds without errors. — Configurations/SenseTranslationConfiguration.cs
- [x] In `ContentDbContext`, add the DbSets `Senses`, `LearnerWords`, `Lexemes`, `SenseTranslations` and remove the old ones — the solution builds. — Persistence/ContentDbContext.cs (also `LessonSenses` DbSet, replacing `LessonVocabularyWords`)
- [x] Add the migration `SplitVocabularyIntoLexemesAndSenses` and **hand-edit** it to the 8 steps in plan.md: one Lexeme per distinct `NormalizedWord` with `PartOfSpeech = NULL`; `RenameTable` / `RenameColumn` / `RenameIndex` for all three tables (`Senses`, `LearnerWords`, `LessonSenses`); `sp_rename` for PK/FK names; no `DropTable` and no row copy — `dotnet ef migrations has-pending-model-changes` reports no changes. — Migrations/20261005023005_SplitVocabularyIntoLexemesAndSenses.cs + .Designer.cs, ContentDbContextModelSnapshot.cs; has-pending-model-changes: no changes
- [x] Write the migration `Down` so it reverses every step, including `LessonSenses` → `LessonVocabularyWords`. Re-fill `NormalizedWord` with `UPPER(LTRIM(RTRIM(Word)))`. Add an XML summary that states the lossy parts — Down returns the schema to `DropVocabularyWordIdRemaps` with all ids intact. — Migrations/20261005023005_SplitVocabularyIntoLexemesAndSenses.cs
- [x] Repository: change the types to `Sense` / `LearnerWord` and the DbSets to `Senses` / `LearnerWords`; update index names in comments — behaviour is unchanged. — Repositories/VocabularyWordRepository.cs, Repositories/LessonRepository.cs
- [x] Repository: add `GetOrCreateLexemeAsync(string word, ct)`: look up by (`NormalizedLemma`, `PartOfSpeech IS NULL`), create with POS null, re-read on unique violation 2601/2627 — concurrent calls for the same word return the same `Lexeme.Id`. — Repositories/VocabularyWordRepository.cs
- [x] Repository `AddAsync`: on an FK violation 547 for `LexemeId`, call `GetOrCreateLexemeAsync` once and retry; a second failure returns `PersonalVocabularyWord.ConcurrentAdd` — never a 500. — Repositories/VocabularyWordRepository.cs
- [x] Repository `DeleteIfOrphanedAsync`: also delete the Sense's Lexeme when no other Sense references it (guarded delete) — a Lexeme shared with another Sense is never deleted. — Repositories/VocabularyWordRepository.cs (sense delete + guarded lexeme DELETE in one transaction)
- [x] `DataSeeder`: create one `Lexeme` per seed word (POS null), add them to `Lexemes`, and pass `lexeme.Id` to `Sense.CreateSystem` — a fresh dev database seeds without errors. — Seeding/DataSeeder.cs

### Application

- [x] Update `IVocabularyWordRepository` (keep its name): types `Sense` / `LearnerWord`, new `GetOrCreateLexemeAsync`, refreshed XML docs — the Application project builds. — Application/Interfaces/IVocabularyWordRepository.cs
- [x] `AddPersonalVocabularyWordCommandHandler`: dedupe is unchanged; the new-word branch calls `GetOrCreateLexemeAsync`, then `Sense.CreateLearner(…, lexeme.Id, …)`. A lexeme failure returns its `Error` — the existing handler tests still pass. — AddPersonalVocabularyWordCommandHandler.cs
- [x] `AddSharedVocabularyWordToMyList`, `Delete`, `RequestShare`, `Moderate`, the queries, `PersonalVocabularyWordMapper` and `GetLessonDetailQueryHandler` (`LessonSense`): type renames only — the DTOs and their values are unchanged. — PersonalVocabularyWordMapper.cs, GetLessonDetailQueryHandler.cs, 4 command + 4 query handlers, GetSharedVocabularyWordsQuery.cs (type renames only)
- [x] Controllers, request models, routes, policies, error codes and cache keys: no change — `git diff` shows no change under `WordBuddy.Content.Api/Controllers` or `Models` apart from type renames, if any. — no files; `git diff` under WordBuddy.Content.Api is empty

## Frontend

- [x] None — the API JSON is unchanged, so `WordBuddy.UI` is not touched. — no files

## Tests

### Unit (`WordBuddy.Content.UnitTests`)

- [x] Rename `VocabularyWordTests` → `SenseTests` and `TestWords` helpers to `Sense` / `LearnerWord` / `LessonSense`. All current cases keep passing, including the Child/Adult `IsVisibleTo` cases. — UnitTests/Domain/SenseTests.cs (was VocabularyWordTests.cs), TestWords.cs, handler tests
- [x] `SenseTests`: `Sense_ComputeContentHash_IsUnchangedForKnownInput` — assert one fixed hex value taken from the current code, so a future hash change is caught. — UnitTests/Domain/SenseTests.cs
- [x] `SenseTests`: the factories set `LexemeId` — one test for `CreateSystem` and one for `CreateLearner`. — UnitTests/Domain/SenseTests.cs
- [x] `LexemeTests`: `Create` trims and normalizes (`" Apple"` → `Lemma "Apple"`, `NormalizedLemma "APPLE"`); POS defaults to null; `Create(…, PartOfSpeech.Noun)` stores it; an empty or whitespace lemma fails. — UnitTests/Domain/LexemeTests.cs
- [x] `SenseTranslationTests`: valid locales (`vi`, `vi-VN`, `es-419`) pass and are case-normalized; invalid locales (`""`, `vietnamese`, `vi_VN`) fail; empty text and text over 500 characters fail. — UnitTests/Domain/SenseTranslationTests.cs
- [x] `LearnerWordTests`: a new link has `AddedBy = Learner` and `PersonalContext = null`. — UnitTests/Domain/LearnerWordTests.cs
- [x] `LessonTests` (existing lesson order tests): `AddVocabularyWord` creates `LessonSense` links with the same `SortOrder` sequence — tests pass with renamed types. — UnitTests/Domain/LessonTests.cs (new; no lesson tests existed before)
- [x] `AddPersonalVocabularyWordCommandHandlerTests`: the new-word path calls `GetOrCreateLexemeAsync` and creates the Sense with the returned id; the dedupe paths (own, system, shared, Child cannot see) never call it; a lexeme failure is propagated. — UnitTests/Features/PersonalVocabulary/AddPersonalVocabularyWordCommandHandlerTests.cs
- [x] All existing handler tests compile against the renamed types and pass without any change to their assertions. — UnitTests/Features/PersonalVocabulary/*.cs (type renames only)

### Integration (`WordBuddy.Content.IntegrationTests`, real SQL Server)

- [x] New `SplitVocabularyIntoLexemesAndSensesMigrationTests`, modelled on `UnifyVocabularyWordsMigrationTests`. Migrate to `20261002174303_DropVocabularyWordIdRemaps` and seed with SQL. The seed must include: system words in 2 lessons; learner words `" Apple"` / `"apple"` from different owners; a `Shared` word with `VisibleToChildren = 1` and one with `0`; a Child owner's private word; adopter links; and a word with audio. Snapshot every row before migrating. — IntegrationTests/SplitVocabularyIntoLexemesAndSensesMigrationTests.cs; UnifyVocabularyWordsMigrationTests.cs (type ref only)
- [x] Migration test: `…_Up_KeepsEverySenseIdAndContentHashByteIdentical` — `Senses` rows equal the old `VocabularyWords` snapshot for `Id`, `Word`, `Definition`, `Example`, `ContentHash`, `AudioAssetId`, share/moderation fields and `VisibleToChildren`. — SplitVocabularyIntoLexemesAndSensesMigrationTests.cs
- [x] Migration test: `…_Up_CreatesOneLexemePerNormalizedWordWithNullPartOfSpeech` — lexeme count = distinct `NormalizedWord` count; every `PartOfSpeech IS NULL`; `" Apple"` and `"apple"` share one `LexemeId`; the lemma follows the System > Shared > oldest rule. — SplitVocabularyIntoLexemesAndSensesMigrationTests.cs
- [x] Migration test: `…_Up_CreatesUniqueLemmaPartOfSpeechIndexWithoutFilter` — `UX_Lexemes_NormalizedLemma_PartOfSpeech` exists on (`NormalizedLemma`, `PartOfSpeech`), `is_unique = 1`, `has_filter = 0`; inserting a second (`APPLE`, `NULL`) row fails with 2601. — SplitVocabularyIntoLexemesAndSensesMigrationTests.cs
- [x] Migration test: `…_Up_KeepsLearnerWordIdsAndSetsAddedByLearner` — `LearnerWords` equals the old `UserVocabularyWords` snapshot (`Id`, `UserId`, `SenseId`, `AddedAtUtc`, `IsAuthor`), with `AddedBy = 'Learner'` and `PersonalContext IS NULL`. — SplitVocabularyIntoLexemesAndSensesMigrationTests.cs
- [x] Migration test: `…_Up_RenamesLessonLinksToLessonSensesAndKeepsSortOrder` — `LessonSenses` (`LessonId`, `SenseId`, `SortOrder`) rows equal the snapshot; `PK_LessonSenses` and `IX_LessonSenses_SenseId` exist. — SplitVocabularyIntoLexemesAndSensesMigrationTests.cs
- [x] Migration test: `…_Up_RenamesTablesAndCreatesSenseTranslations` — `VocabularyWords`, `UserVocabularyWords`, `LessonVocabularyWords` no longer exist; `Senses`, `LearnerWords`, `LessonSenses`, `Lexemes`, `SenseTranslations` exist; `UX_SenseTranslations_SenseId_Locale` is unique. — SplitVocabularyIntoLexemesAndSensesMigrationTests.cs
- [x] Migration test: `…_Down_RestoresOldSchemaWithSameIds` — after `Down`, `VocabularyWords` / `UserVocabularyWords` / `LessonVocabularyWords` exist with ids, `ContentHash` and `SortOrder` equal to the snapshot, and `NormalizedWord` is filled again. — SplitVocabularyIntoLexemesAndSensesMigrationTests.cs
- [x] `VocabularyStorageEndpointsTests`: add `LessonDetail_Get_JsonShapeUnchanged`, which asserts the exact property names of `vocabularyItems[]` (`id`, `word`, `definition`, `example`, `audio`) and their order by `SortOrder`. — IntegrationTests/VocabularyStorageEndpointsTests.cs
- [x] `PersonalVocabularyEndpointsTests`: add JSON-shape tests for `GET /api/vocabulary/mine` and `GET /api/vocabulary/shared` (property set incl. `isAuthor` / `isMine`), one test per `AgeGroup` for `shared` — Child sees only child-visible senses. — IntegrationTests/PersonalVocabularyEndpointsTests.cs
- [x] `PersonalVocabularyEndpointsTests`: `AddPersonalVocabularyWord_Post_ReusesNullPosLexemeForSameNormalizedWord` — two users add `"Pear"` / `" pear"` with different definitions: 2 senses, 1 lexeme with `PartOfSpeech IS NULL`. — IntegrationTests/PersonalVocabularyEndpointsTests.cs
- [x] `PersonalVocabularyEndpointsTests`: `DeletePersonalVocabularyWord_Delete_RemovesOrphanLexemeOnly` — deleting the last private sense removes its lexeme; deleting one of two senses keeps it. — IntegrationTests/PersonalVocabularyEndpointsTests.cs
- [x] `ConcurrentVocabularyAddTests`: `AddPersonalVocabularyWord_ConcurrentNewLemmaFromTwoUsers_OneLexemeNo500` — parallel adds of a brand-new word by 2 users: all responses 2xx, 1 lexeme (POS null), 2 senses. This proves the 2601 re-read works with a NULL POS. — IntegrationTests/ConcurrentVocabularyAddTests.cs (+ forced 547 retry test)
- [x] Run the full suite: `dotnet test WordBuddy/src/Services/Content/WordBuddy.Content.slnx` — everything is green. If no SQL Server is reachable, report the integration suite as skipped. — LocalDB: unit 103/103, integration 52/52 green
- [x] Run the Progress suite unchanged: `dotnet test WordBuddy/src/Services/Progress/WordBuddy.Progress.slnx` — green, which proves the Sense ids stayed compatible. — unit 10/10, integration 2/2 green

### Docs (tester stage, on merge)

- [ ] `docs/database-diagram/content.md`: new ER diagram (incl. `LessonSenses`), index table and "last migration" line; note that `Senses.Id` = the former `VocabularyWords.Id`, referenced by Progress.
- [ ] `docs/features/vocabulary.md`: replace the ER diagram (`Lexemes`, `Senses`, `LearnerWords`, `LessonSenses`, `SenseTranslations`), restate the Child rule at `Sense` level, move WB-16 from "Pending changes" to "Change history".
- [ ] `docs/features/vocabulary-builder.md`: check for references to old table or type names; update them if any are found.
- [ ] `WordBuddy/src/Services/Content/README.md`: update the entity list (`Lexeme`, `Sense`, `SenseTranslation`, `LearnerWord`, `LessonSense`).
