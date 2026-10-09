# Tasks: WB-25_Vocabulary autofill

## Backend

- [x] Add `SenseOrigin` enum and enrichment fields to `Sense` (`Examples`, `Collocations`, `Synonyms`, `Antonyms`, `TopicTags`, `RegisterNote`) — fields private-set, lists never null, `ContentHash` unchanged — Domain/SenseOrigin.cs, Domain/Sense.cs (+ `ChildSuitableHint`, `Lexeme`/`Translations` navigations)
- [x] Add `Sense.CreateAutoFill` and `Sense.ApproveForChildren(adminId)` — auto-fill sense is `System` + `Shared` + `VisibleToChildren = false` — Domain/Sense.cs (+ `MoveToLexeme`, `AddTranslation`)
- [x] Add child approval to `LearnerWord` (`ChildApprovedAtUtc`, `ChildApprovedByUserId`, `ApproveForChild`) — second approve returns `Conflict` — Domain/LearnerWord.cs (+ `RequiresChildApproval`, see report)
- [x] Extend child visibility: child sees an `AutoFill` sense only if globally approved or own link approved — adult rule unchanged — Domain/Sense.cs (`IsVisibleTo(userId, ageGroup, link)`, `IsAwaitingChildApproval`)
- [x] Add `Lexeme.Enrich`, `Lexeme.AttachAudio(locale, assetId)`, `Lexeme.RederiveLemma` — lemma equals a visible sense word — Domain/Lexeme.cs
- [x] EF configuration + migration `AddVocabularyAutofill` — existing rows get `Origin = Manual`, lists `[]` — Configurations/SenseConfiguration.cs, LearnerWordConfiguration.cs, SenseTranslationConfiguration.cs, JsonStringListConversion.cs, Migrations/20261009023746_AddVocabularyAutofill*.cs, snapshot, docs/database-diagram/content.md
- [x] Define `IDictionaryClient`, `ISenseGenerator`, `IAudioDownloader` in Application — return `Result<T>`, no exceptions for failures — Application/Interfaces/Autofill/*
- [x] Implement `FreeDictionaryClient` (typed HttpClient, 5 s timeout, resilience handler) — maps IPA UK/US, audio URLs, POS — Infrastructure/Autofill/FreeDictionaryClient.cs, AutofillClientSettings.cs, Extensions/ServiceCollectionExtensions.cs (Microsoft.Extensions.Http.Resilience)
- [x] Implement `ClaudeSenseGenerator` (`Autofill:Claude:Model` = `claude-sonnet-5-5`, key from user-secrets/env, JSON-schema output, child-safe prompt, 15 s timeout) — only the word is sent — Infrastructure/Autofill/ClaudeSenseGenerator.cs (forced tool + input_schema)
- [x] Store dictionary audio as `lexeme-{id}-en-GB.mp3` / `lexeme-{id}-en-US.mp3` `MediaAsset` — linked on the lexeme — Application/Features/Autofill/AutofillCatalogWriter.cs, Infrastructure/Autofill/HttpAudioDownloader.cs, IFileStorageService.SaveNamedAsync, LocalFileStorageService.cs
- [x] `LookupAutofill` query + validator + handler — catalog hit makes no external call; failure returns `autofillUnavailable = true`, saves nothing — Features/Autofill/Queries/LookupAutofill/*
- [x] Save step: lexeme per POS, senses, `vi` translations; move matching System/Shared senses off the null-POS lexeme; delete orphan lexeme — private learner senses untouched — Features/Autofill/AutofillCatalogWriter.cs, Infrastructure/Repositories/AutofillRepository.cs
- [x] Negative cache `content:autofill-miss:{lemma}` (1 h) and catalog cache `content:autofill:{lemma}` — keys deleted after save/approval — Caching/AutofillCacheKeys.cs, LookupAutofillQueryHandler.cs, ApproveChildWordCommandHandler.cs
- [x] `AddAutofillSenseToMyList` command — creates `LearnerWord`; idempotent if already linked — Features/Autofill/Commands/AddAutofillSenseToMyList/*
- [x] `CanSupportLearner` policy in Content on `SupportLinkProjection` — non-supporter gets 403 — already in Content from WB-24 (Api/Authorization/SupportLinkAuthorization.cs); reused + new `AdultOrAdmin` policy
- [x] `GetPendingChildApprovals` (supporter + admin variants) — lists only unapproved auto-fill senses linked by a child — Features/Autofill/Queries/GetPendingChildApprovals/*
- [x] `ApproveChildWord` (supporter → link, admin → global) — cache keys deleted — Features/Autofill/Commands/ApproveChildWord/*
- [x] Hide unapproved auto-fill senses for children in `GetMyVocabularyWords`, `GetSharedVocabularyWords`, `GetSensesByIds`, lesson detail — child gets `awaitingApproval = true` with no content — GetMyVocabularyWords*, GetRandomVocabularyWordsForCheck*, GetSensesByIdsQueryHandler.cs, GetLessonDetail*, PersonalVocabularyWordMapper.cs, LessonsController.cs, PersonalVocabularyController.cs
- [x] Extend word DTOs with POS, IPA, audio URLs, examples, translations, enrichment, `origin` — added fields only, old JSON intact — DTOs/PersonalVocabularyWordDto.cs, VocabularyItemDto.cs, SenseDetails.cs, SenseTranslationDto.cs, AutofillDtos.cs, VocabularyWordRepository.cs, LessonRepository.cs
- [x] Controller endpoints (6 routes in plan) via `ToProblemResult` + `autofill` rate-limit policy 10/min/user — 429 with `Retry-After` — Api/Controllers/VocabularyAutofillController.cs, Extensions/RateLimitingConfiguration.cs, Program.cs, WebApplicationExtensions.cs, ServiceCollectionExtensions.cs
- [x] `appsettings.json` non-secret `Autofill` section (base URLs, model, locales `["vi"]`, timeouts) — no key in git — Api/appsettings.json

## Frontend

- [x] Types in `src/types/index.ts` (`PartOfSpeech`, `SenseOrigin`, `AutofillResult`, enriched word) — union types, no `any` — src/types/index.ts
- [x] API functions in `src/api/vocabulary.ts` for the 6 endpoints — use `apiClient` — src/api/vocabulary.ts
- [x] Hooks `useAutofillLookup`, `useAddAutofillSense`, `usePendingApprovals`, `useApproveChildWord` — keys `['vocabulary', ...]`, invalidate `mine` — src/hooks/useAutofill.ts
- [x] Add-word page: auto-fill button, sense cards, add action — unavailable/`isError` falls back to manual form with word prefilled — src/pages/VocabularyBuilderPage.tsx, src/components/autofill/AutofillResults.tsx
- [x] Sense card component (POS, IPA, UK/US audio play, definition, examples, translation) — CSS Module + theme tokens, Framer Motion entrance — src/components/autofill/SenseCard.tsx, SenseCard.module.css, SenseDetails.tsx
- [x] Word detail shows enrichment fields; child sees "Waiting for approval" badge — no definition shown while waiting — word cards in VocabularyBuilderPage.tsx (no separate detail page exists), src/components/lessons/VocabularyList.tsx
- [x] `/support` learner card: "Words to approve" list with Approve — hidden for child accounts — src/components/support/WordsToApprove.tsx, src/pages/SupportLinksPage.tsx
- [x] Admin moderation: "Auto-filled words" tab with Approve for children — `AdminOnly` route — src/pages/VocabularyModerationPage.tsx (`?tab=autofill`), src/components/autofill/AutofillApprovalsPanel.tsx

## Tests

- [x] Unit: `Sense.CreateAutoFill`, `ApproveForChildren`, child visibility with/without approval — child and adult cases — UnitTests/Domain/SenseAutofillTests.cs
- [x] Unit: `LearnerWord.ApproveForChild` (success, double approve conflict) — UnitTests/Domain/LearnerWordTests.cs
- [x] Unit: `Lexeme.Enrich`, `AttachAudio`, `RederiveLemma` — UnitTests/Domain/LexemeTests.cs
- [x] Unit: `LookupAutofill` handler — catalog hit makes zero calls to `IDictionaryClient`/`ISenseGenerator` (Moq `Times.Never`) — UnitTests/Features/Autofill/LookupAutofillQueryHandlerTests.cs
- [x] Unit: `LookupAutofill` handler — dictionary or Claude failure returns unavailable, nothing saved — UnitTests/Features/Autofill/LookupAutofillQueryHandlerTests.cs
- [x] Unit: save step — senses moved to POS lexeme, orphan deleted, private senses untouched — UnitTests/Features/Autofill/AutofillCatalogWriterTests.cs (+ integration orphan-delete tests)
- [x] Unit: `AddAutofillSenseToMyList`, `ApproveChildWord` (supporter vs admin), `GetPendingChildApprovals` — UnitTests/Features/Autofill/AutofillCommandHandlerTests.cs
- [x] Unit: `ClaudeSenseGenerator` / `FreeDictionaryClient` parsing with fake `HttpMessageHandler` — bad JSON → failure `Result` — UnitTests/Infrastructure/AutofillClientParsingTests.cs (UnitTests now references Infrastructure; InternalsVisibleTo)
- [x] Integration: migration applies; enriched fields round-trip on real SQL Server — IntegrationTests/VocabularyAutofillEndpointsTests.cs
- [x] Integration: child never sees an unapproved auto-filled sense in `mine`, `shared`, `GetSensesByIds`, lesson detail — IntegrationTests/VocabularyAutofillEndpointsTests.cs
- [x] Integration: supporter approve → only that child sees it; admin approve → all children see it; non-supporter 403 — IntegrationTests/VocabularyAutofillEndpointsTests.cs
- [x] Integration: autofill endpoint 429 after 10 calls/min; fake external clients registered in `WebApplicationFactory` — IntegrationTests/VocabularyAutofillEndpointsTests.cs, FakeAutofillClients.cs, ContentApiFactory.cs
- [x] API E2E (`e2e/api`): adult add via auto-fill; child add → hidden → supporter approves → visible — moved to `/test` (Sam, 2026-10-09)
- [x] UI E2E (`e2e/ui`): auto-fill happy path; external failure → manual form still adds the word — moved to `/test` (Sam, 2026-10-09)
