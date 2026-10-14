# Tasks: WB-49 Personal context on learner word

## Backend

- [x] `LearnerWord.SetPersonalContext(string?)` — trims; blank → `null`; > 500 → `Error.Validation("LearnerWord.PersonalContextTooLong")` with no text in the message; XML doc updated. — `Content.Domain/LearnerWord.cs`
- [x] `IVocabularyWordRepository.SaveLinkAsync(LearnerWord, ct)` + implementation — saves a tracked link from `GetLinkAsync`. — `Application/Interfaces/IVocabularyWordRepository.cs`, `Infrastructure/Repositories/VocabularyWordRepository.cs`
- [x] `AddPersonalVocabularyWordCommand` + `string? PersonalContext = null`; validator max 500; handler sets the note on new author link and new dedupe link; non-blank note overwrites on an existing link (`SaveLinkAsync`). — `Commands/AddPersonalVocabularyWord/*`
- [x] Repository `AddAsync` concurrent-add path copies `authorLink.PersonalContext` onto the replacement link — note survives the race. — `VocabularyWordRepository.cs`
- [x] `AddSharedVocabularyWordToMyListCommand` + `PersonalContext`; validator max 500; handler same rule as above. — `Commands/AddSharedVocabularyWordToMyList/*`
- [x] `AddAutofillSenseToMyListCommand` + `PersonalContext`; validator max 500; handler same rule; child-approval flag unchanged. — `Features/Autofill/Commands/AddAutofillSenseToMyList/*`
- [x] New `UpdatePersonalContextCommand(SenseId, UserId, PersonalContext)` + `UpdatePersonalContextCommandValidator` + handler — 404 when no link, success sets or clears the note. — `Commands/UpdatePersonalContext/*`
- [x] Register the new handler + validator in DI — resolves at startup. — `Application/Extensions/ServiceCollectionExtensions.cs`
- [x] `PersonalVocabularyWordDto` + `string? PersonalContext = null`; set only in `PersonalVocabularyWordMapper.ToDto(LearnerWord, AgeGroup)` — pool/moderation never carry it. — `Application/DTOs/*`
- [x] Request models: `AddPersonalVocabularyWordRequest` + `PersonalContext`; new `AddToMyListRequest(string? PersonalContext)`; new `UpdatePersonalContextRequest(string? PersonalContext)`. — `Api/Models/*`
- [x] `PersonalVocabularyController`: pass note on `AddWord` and `AddSharedWordToMyList` (body optional via `EmptyBodyBehavior.Allow`); new `PUT {id:guid}/personal-context` → 204 / `ToProblemResult`. — `Api/Controllers/PersonalVocabularyController.cs`
- [x] `VocabularyAutofillController.AddToMine`: optional `AddToMyListRequest` body, pass the note — no-body call still 200. — `Api/Controllers/VocabularyAutofillController.cs`
- [x] Logging review: no log line, exception message or validation message contains the note; at most `HasPersonalContext`. — all touched files

## Frontend

- [x] `PersonalVocabularyWord.personalContext?: string | null` — no `any`. — `src/types/index.ts`
- [x] API: `personalContext` on `AddVocabularyWordPayload`; `addSharedWordToMyList(id, personalContext?)`, `addAutofillSense(senseId, personalContext?)` send `{ personalContext }`; new `updatePersonalContext(id, personalContext)`. — `src/api/vocabulary.ts`
- [x] Hooks: `useAddSharedWordToMyList` and `useAddAutofillSense` take an object; new `useUpdatePersonalContext` invalidates `['vocabulary', 'mine']`; fix `addSense.variables` check in `AutofillResults`. — `src/hooks/useVocabulary.ts`, `src/hooks/useAutofill.ts`
- [x] New `PersonalContextField` — labelled textarea, max 500, counter, "Only you can see this note." — `src/components/vocabulary/PersonalContextField.tsx`
- [x] `VocabularyBuilderPage` add form: field + Zod `max(500).optional()`; note sent on add and reset on success. — `src/pages/VocabularyBuilderPage.tsx`
- [x] `AutofillResults`: new `personalContext` prop from the add form; "Add" sends it. — `src/components/autofill/AutofillResults.tsx`
- [x] `VocabularySharedPoolPage`: per-card "Add a note" toggle + field; "Add to My List" sends it. — `src/pages/VocabularySharedPoolPage.tsx`
- [x] New `PersonalContextEditor` on each My words card: show note, Add/Edit, Save, Clear, error message on failure. — `src/components/vocabulary/PersonalContextEditor.tsx`, `VocabularyBuilderPage.tsx`

## Tests

- [x] Unit `LearnerWordTests`: set, trim, blank clears, 500 ok, 501 fails without text in message. — `UnitTests/Domain/LearnerWordTests.cs`
- [x] Unit `GetSensesByIdsQueryHandlerTests`: replace reflection setter with `SetPersonalContext`. — `UnitTests/Features/PersonalVocabulary/GetSensesByIdsQueryHandlerTests.cs`
- [x] Unit `AddPersonalVocabularyWordCommandHandlerTests`: note on new word, on dedupe link, overwrite on existing link, blank keeps existing, 501 → validation failure. — `UnitTests/Features/PersonalVocabulary/`
- [x] Unit `AddSharedVocabularyWordToMyListCommandHandlerTests`: note stored; Child and Adult paths. — same folder
- [x] Unit `AddAutofillSenseToMyListCommandHandlerTests`: note stored; Child link still `RequiresChildApproval` with note. — `UnitTests/Features/Autofill/`
- [x] Unit `UpdatePersonalContextCommandHandlerTests`: set, clear, 404 not linked, validation fail, Child and Adult succeed. — `UnitTests/Features/PersonalVocabulary/`
- [x] Unit privacy: capture `ILogger` output in the 4 handlers; assert the note text never appears. — same test files
- [x] Unit mapper: `ToDto(LearnerWord, …)` carries note; `ToDto(Sense)` does not. — `UnitTests/DTOs/PersonalVocabularyWordMapperTests.cs`
- [x] Integration: POST word with note → GET mine shows it; PUT edit → updated; PUT null → cleared; other user's id → 404; 501 chars → 400. — `IntegrationTests/PersonalContextEndpointsTests.cs`
- [x] Integration: shared and auto-fill add-to-mine with note and with no body (both 2xx). — same file
- [x] Integration: user B never sees A's note (`/shared`, `/senses`, `/mine`). — same file
- [x] Integration: concurrent identical add with a note keeps the note. — `IntegrationTests/ConcurrentVocabularyAddTests.cs`
- [x] E2E API: add with note → Progress review exercise carries `personalContext`. — `e2e/api/WordBuddy.E2E.Api.Tests/PersonalContextTests.cs` (written and built, NOT run: needs full stack)
- [x] E2E UI: learner (Adult) adds a word with a note, edits it on My words, clears it; the add form shows the "Only you can see this note" hint. — `e2e/ui/features/vocabulary-personal-context.feature` + steps (written, NOT run). No Child scenario: a child needs a linked supporter to reach My Vocabulary and no existing step sets that up; deferred.

Notes: `PersonalVocabularyWordDto.PersonalContext` has `JsonIgnore(WhenWritingNull)` so old JSON shapes stay intact. Extra files: `UnitTests/CapturingLogger.cs`, `Api/Models/AddToMyListRequest.cs`, `Api/Models/UpdatePersonalContextRequest.cs`.
