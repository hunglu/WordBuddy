# Plan: WB-49 Personal context on learner word

## Summary

Learners can write a private note (max 500) on where they met a word. They can set it on all three add-to-mine flows and edit or clear it later through a new `PUT /api/vocabulary/{id}/personal-context`. The column `LearnerWords.PersonalContext` and the review display (`PersonalContextNote`) already exist. This change adds only the write path and the "mine" read path.

```mermaid
flowchart LR
    A[Add own word<br/>POST /api/vocabulary] --> L[(LearnerWords.PersonalContext)]
    B[Shared pool<br/>POST shared/{id}/add-to-mine] --> L
    C[Auto-fill<br/>POST autofill/{senseId}/add-to-mine] --> L
    E[Edit / clear<br/>PUT {id}/personal-context] --> L
    L --> M[GET /mine → personalContext]
    L --> R[GET /senses → Progress review → PersonalContextNote]
```

## Affected services / areas

| Area | Change |
| --- | --- |
| Content.Domain | `LearnerWord.SetPersonalContext(...)` |
| Content.Application | 3 add commands get `PersonalContext`; new `UpdatePersonalContext` command; DTO field on "mine" |
| Content.Infrastructure | Concurrent-add path keeps the note |
| Content.Api | 2 new optional request bodies, 1 new endpoint |
| Progress | None (already passes `PersonalContext` through) |
| Shared.Contracts | None. `LearnerWordAdded` stays ids only |
| WordBuddy.UI | Add form, auto-fill add, shared pool add, My words note editor |
| Migration | None |

## Backend approach

All new code follows `.claude/skills/develop-webapi/SKILL.md` (command record + `{Name}Validator` + `ICommandHandler`, controller maps via `ToProblemResult`).

### Domain — `LearnerWord`

- New `Result SetPersonalContext(string? text)`:
  - Trim. Empty or whitespace → `null` (clears).
  - Longer than `PersonalContextMaxLength` (500) → `Error.Validation("LearnerWord.PersonalContextTooLong", ...)`. The message never contains the text.
- Update the XML doc ("Always null today" is no longer true).
- Constructors stay as they are. Callers call `SetPersonalContext` after `new LearnerWord(...)`.

### Add flows — optional `PersonalContext`

| Flow | Command | Request change |
| --- | --- | --- |
| Own word | `AddPersonalVocabularyWordCommand` | `AddPersonalVocabularyWordRequest` + `string? PersonalContext` |
| Shared pool | `AddSharedVocabularyWordToMyListCommand` | new optional body `AddToMyListRequest(string? PersonalContext)` |
| Auto-fill | `AddAutofillSenseToMyListCommand` | same `AddToMyListRequest` |

- Each command record gets `string? PersonalContext = null` as the last parameter. Existing callers compile unchanged.
- Each validator: `RuleFor(c => c.PersonalContext).MaximumLength(LearnerWord.PersonalContextMaxLength)`.
- Shared-pool and auto-fill actions bind the body with `[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AddToMyListRequest? request`. A call with no body still works (existing UI and E2E tests).
- Behaviour per link state:

  | Link state | Note in request | Effect |
  | --- | --- | --- |
  | New link | any | `link.SetPersonalContext(note)` before `LinkAsync` / `AddAsync` |
  | Already linked | `null` or blank | no change (same as today) |
  | Already linked | non-blank | overwrite note, save (judgment call, see Open questions) |

- Concurrent add race: `VocabularyWordRepository.AddAsync` creates a fresh author link at line ~323 when another request won. Copy `authorLink.PersonalContext` onto that link, so the note is not lost.
- An "already linked + note" update needs a save. Add `IVocabularyWordRepository.SaveLinkAsync(LearnerWord link, CancellationToken)` (calls `SaveChangesAsync` on the tracked link from `GetLinkAsync`).

### New command — `UpdatePersonalContext`

- Folder: `Features/PersonalVocabulary/Commands/UpdatePersonalContext/`.
- `UpdatePersonalContextCommand(Guid SenseId, Guid UserId, string? PersonalContext) : ICommand`.
- Validator: `SenseId`, `UserId` not empty; `PersonalContext` max 500.
- Handler flow: `GetLinkAsync(userId, senseId)` → 404 if none → `link.SetPersonalContext(...)` → `SaveLinkAsync` → `Result.Success()`.
- Works on every link of the caller: author, adopted, auto-fill (also while awaiting child approval), supporter-assigned.
- Endpoint in `PersonalVocabularyController` (inherits `[Authorize]` + `ChildHasSupporter`):

  | Method | Route | Body | Result |
  | --- | --- | --- | --- |
  | PUT | `/api/vocabulary/{id:guid}/personal-context` | `UpdatePersonalContextRequest(string? PersonalContext)` | 204; 400 over 500 chars; 404 not in caller's list |

  `null`, `""` or whitespace clears the note.

### Read path

- `PersonalVocabularyWordDto` + `string? PersonalContext = null` (last parameter, old JSON intact).
- Set it only in `PersonalVocabularyWordMapper.ToDto(LearnerWord, AgeGroup)` — the caller-scoped view ("mine", recall check). Pool and moderation (`ToDto(Sense)`) never carry it.
- Child awaiting approval: the note is still returned. It is the child's own text, not sense content.
- `GetSensesByIds` already returns the note only for the caller's own link. No change.

### Privacy and logging

- No handler, controller or repository logs the note. Log `HasPersonalContext={bool}` at most.
- Validation messages use the FluentValidation default (length only, no `{PropertyValue}`).
- No event, cache key or supporter / admin DTO carries the note.
- Caching: Content has no cache on "mine" or links, so nothing to invalidate. Progress caches review senses per session (`progress:session-senses:{sessionId}`); an edit shows from the next session. Accepted limitation.

## Frontend approach

| File | Change |
| --- | --- |
| `src/types/index.ts` | `PersonalVocabularyWord.personalContext?: string \| null` |
| `src/api/vocabulary.ts` | `AddVocabularyWordPayload.personalContext?`; `addSharedWordToMyList(id, personalContext?)` and `addAutofillSense(senseId, personalContext?)` send `{ personalContext }`; new `updatePersonalContext(id, personalContext: string \| null)` |
| `src/hooks/useVocabulary.ts` | `useAddSharedWordToMyList` takes `{ id, personalContext }`; new `useUpdatePersonalContext` (invalidates `['vocabulary', 'mine']`) |
| `src/hooks/useAutofill.ts` | `useAddAutofillSense` takes `{ senseId, personalContext }` |
| `src/components/vocabulary/PersonalContextField.tsx` (new) | Labelled textarea "Where did you meet this word? (optional)", 500 max, char counter, hint "Only you can see this note." |
| `src/components/vocabulary/PersonalContextEditor.tsx` (new) | On a My words card: shows the note, "Add note" / "Edit note" button, inline RHF + Zod form with Save and Clear; error text on failure |
| `src/pages/VocabularyBuilderPage.tsx` | Add form gets `PersonalContextField`; Zod `personalContext: z.string().max(500).optional()`; pass the watched value to `AutofillResults` |
| `src/components/autofill/AutofillResults.tsx` | New prop `personalContext?: string`; "Add" sends it |
| `src/pages/VocabularySharedPoolPage.tsx` | Per card "Add a note" toggle reveals `PersonalContextField`; "Add to My List" sends it |

- Server state only through TanStack Query. No Zustand change.
- Every new mutation shows an error message on `isError`; no blank screen.
- Review page: no change. `PersonalContextNote` already renders the note.

## Child vs adult behaviour

| Topic | Child | Adult |
| --- | --- | --- |
| Write / edit / clear own note | Yes | Yes |
| Who can read it | The child only | The adult only |
| Supporter / admin sees it | No | No |
| Logged / in events | Never | Never |
| On a link awaiting child approval | Note editable and returned; sense content still hidden | n/a |

No new authorization policy is needed: every endpoint is scoped to the caller's own link (`UserId` from the JWT).

## Data / migration notes

None. `LearnerWords.PersonalContext nvarchar(500) NULL` exists since WB-16 (`SplitVocabularyIntoLexemesAndSenses`).

## Open questions

1. **Add on an already-linked word with a note.** Plan: a non-blank note overwrites the stored note; a blank one keeps it. Alternative: ignore the note and keep add idempotent. Confirm.
2. **Child note content.** A child may type personal data (a friend's name, a school). Plan keeps it private and unlogged, with a "Only you can see this note" hint. No content filter. Confirm that is enough.
3. **Mid-session edit.** A note edited during a review session shows from the next session (Progress session cache). Accept?
4. **Scope ideas (not in this plan):** supporter-written notes for group assignments (excluded by the proposal); show the note on the recall-check card; search My words by note.
