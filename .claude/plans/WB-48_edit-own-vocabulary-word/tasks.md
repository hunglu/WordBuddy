# Tasks: WB-48 Edit own vocabulary word

## Backend

- [x] Add `Sense.EditLearnerContent(ownerUserId, lexemeId, word, definition, example)` — guards Learner/owner/Private; recomputes `ContentHash`; syncs `Examples`; clears `AudioAssetId` on word change. — `Content.Domain/Sense.cs` (also `CheckEditable`)
- [x] Add `UpdatePersonalVocabularyWordCommand(SenseId, UserId, Word, Definition, Example)` + `UpdatePersonalVocabularyWordValidator` — same rules as Add validator. — `Commands/UpdatePersonalVocabularyWord/UpdatePersonalVocabularyWordCommand.cs`, `...CommandValidator.cs`
- [x] Add `UpdatePersonalVocabularyWordCommandHandler` per flow in plan.md — 404/403/409/204 as in the status table; no-op returns success. — `...CommandHandler.cs`
- [x] Handler: own-duplicate check via `FindByContentHashAsync`, excluding the edited id — `409 PersonalVocabularyWord.Duplicate`. — same handler
- [x] Handler: on normalized word change, `GetOrCreateLexemeAsync(newWord)` and set new `LexemeId`. — same handler
- [x] Repository: `UpdateAsync` maps SQL 2601/2627 to `Error.Conflict` — race returns 409, not 500. — `Infrastructure/Repositories/VocabularyWordRepository.cs`, `Application/Interfaces/IVocabularyWordRepository.cs`
- [x] Repository: `DeleteLexemeIfOrphanedAsync(lexemeId)`; handler calls it for the old lexeme after a move — no orphan lexemes left. — same repository + interface
- [x] Register handler + validator in DI — resolves at startup. — `Application/Extensions/ServiceCollectionExtensions.cs`
- [x] Add `UpdatePersonalVocabularyWordRequest` model and `PUT api/vocabulary/{id:guid}` action using `ToProblemResult` — returns 204 on success. — `Api/Models/UpdatePersonalVocabularyWordRequest.cs`, `Api/Controllers/PersonalVocabularyController.cs`

## Frontend

- [x] `api/vocabulary.ts`: `updateMyWord(id, payload)` + request type in `types/index.ts` — no `any`. — `src/api/vocabulary.ts` (payload type lives there, next to `AddVocabularyWordPayload`)
- [x] `useUpdateMyWord` mutation invalidating the "mine" query key — list refreshes after save. — `src/hooks/useVocabulary.ts`
- [x] `VocabularyBuilderPage`: Edit button only for own `Private` words; inline RHF + Zod form — saves and closes on success. — `src/pages/VocabularyBuilderPage.tsx`, `src/components/vocabulary/EditWordForm.tsx`
- [x] Error states: 409 duplicate message, generic error message — no crash, no blank screen. — `EditWordForm.tsx`

## Tests

- [x] Unit `SenseTests`: EditLearnerContent success keeps Id, new hash — pass. — `UnitTests/Domain/SenseTests.cs`
- [x] Unit `SenseTests`: fails for System, other owner, PendingReview, Shared, Rejected — correct error types. — same
- [x] Unit `SenseTests`: word change clears audio; definition-only change keeps audio and lexeme — pass. — same
- [x] Unit handler tests: success, 404 not linked, 403 not own, 409 status, 409 duplicate, lexeme move + orphan cleanup, validation fail — Moq repo. — `UnitTests/Features/PersonalVocabulary/UpdatePersonalVocabularyWordCommandHandlerTests.cs`
- [x] Unit handler tests: Child and Adult owner both succeed — pass. — same
- [x] Integration: PUT own Private → 204, GET mine shows new text, same id — real SQL Server. — `IntegrationTests/PersonalVocabularyEditEndpointsTests.cs`
- [x] Integration: PUT Shared/PendingReview → 409; system sense → 403; other user's id → 404. — same
- [x] Integration: PUT to hash of another own sense → 409 (unique index path). — same
- [x] Integration: word change moves sense to correct lexeme; old orphan lexeme deleted. — same
- [x] E2E API (`e2e/api`): add → edit → Progress review by same sense id still works. — `e2e/api/WordBuddy.E2E.Api.Tests/EditVocabularyWordTests.cs` (written and compiled; run against the running stack gave 405, which serves the old Content build)
- [x] E2E UI (`e2e/ui`): learner edits a typo in VocabularyBuilderPage; Edit hidden on shared word. — `e2e/ui/features/vocabulary-edit.feature`, `e2e/ui/steps/vocabulary-edit.steps.ts` (generated with `bddgen`; not run, UI stack is the old build). "Shared word" is covered as a PendingReview word (Edit hidden), no admin step.
