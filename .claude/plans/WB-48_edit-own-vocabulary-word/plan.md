# Plan: WB-48 Edit own vocabulary word

## Summary

Add `PUT /api/vocabulary/{id}` in Content. The owner edits Word, Definition and Example of their own `Private` learner sense in place. `Senses.Id` stays; `ContentHash` is recomputed; a word-text change moves the sense to the matching `Lexeme`.

## Affected services / areas

| Area | Change |
| --- | --- |
| Content.Domain | `Sense.EditLearnerContent(...)` |
| Content.Application | `UpdatePersonalVocabularyWord` command + validator + handler; repo method `HasOwnDuplicateAsync` |
| Content.Infrastructure | `UpdateAsync` maps unique-index clash to `409`; orphan-lexeme cleanup |
| Content.Api | `PUT api/vocabulary/{id}` on `PersonalVocabularyController` |
| WordBuddy.UI | `api/vocabulary.ts`, mutation hook, edit form in `VocabularyBuilderPage` |
| Progress | None. Sense id is unchanged, so SRS history stays valid. |

No migration. No new event.

## Backend approach

Follow `.claude/skills/develop-webapi/SKILL.md`. Pattern to copy: `AddPersonalVocabularyWord` (validator, logging) and `DeletePersonalVocabularyWord` (ownership lookup).

### Flow

```
PUT /api/vocabulary/{id}  {word, definition, example}
 → validate (same rules as Add)
 → repo.GetLinkAsync(userId, id)            404 if not in caller's list
 → sense.Source != Learner or Owner != user  403 (system / linked-not-own)
 → sense.ShareStatus != Private              409 Sense.NotEditable
 → newHash == oldHash and word unchanged     204 (no-op)
 → own duplicate with newHash (other id)     409 PersonalVocabularyWord.Duplicate
 → word text changed (normalized)?           GetOrCreateLexemeAsync(newWord) → new LexemeId
 → sense.EditLearnerContent(...)             recompute ContentHash
 → repo.UpdateAsync                          2601/2627 → 409 (race)
 → old lexeme orphaned?                      delete it
 → 204 No Content
```

### Domain

- `Sense.EditLearnerContent(Guid ownerUserId, Guid lexemeId, string word, string definition, string? example) : Result`.
- Guards: `Source == Learner`, `OwnerUserId == ownerUserId` (else `Error.Forbidden`), `ShareStatus == Private` (else `Error.Conflict("Sense.NotEditable")`).
- Sets Word/Definition/Example, `LexemeId`, `ContentHash = ComputeContentHash(...)`.
- Also sets `Examples` to `[example]` / `[]` so the first-example invariant holds.
- If normalized word changed: clear `AudioAssetId` (audio `vocab-{id}-{locale}.mp3` is now stale). Keep `ImageAssetId` (see open questions).

### Status codes

| Case | Code |
| --- | --- |
| Own Private sense, valid edit | 204 |
| Unknown id or not in caller's list | 404 (no probing, same as `GetLinkAsync`) |
| System sense, or someone else's sense the caller only links to | 403 |
| Own `PendingReview`, `Shared`, `Rejected` | 409 |
| Another own sense already has the new hash | 409 |
| Validation | 400 |

### Why Private-only is safe

- Other learners cannot link to another learner's `Private` sense (Add dedupe only links visible senses).
- Admins attach only system / `Shared` senses to lessons, so no `LessonSenses` link exists.
- Result: the edit only affects the author's own `LearnerWords` row and Progress history.

### Repository

- Add `HasOwnDuplicateAsync(Guid ownerUserId, string contentHash, Guid excludeSenseId)` — or reuse `FindByContentHashAsync` and filter in the handler (preferred; no new method).
- `UpdateAsync`: catch duplicate-key (`IsDuplicateKey`) → `Error.Conflict("PersonalVocabularyWord.Duplicate", ...)`.
- Add `DeleteLexemeIfOrphanedAsync(Guid lexemeId)` (extract from `DeleteIfOrphanedAsync` logic).

### Caching / messaging

- Content has no cache on "mine" today; if a key exists for the sense (`content:sense:{id}`), delete it after success.
- Progress: no event. Progress stores sense ids only; content is read live via `GET api/vocabulary/senses`.

### Child vs adult

- Same behaviour for both. Ownership is the only gate.
- `SupportLinkPolicies.ChildHasSupporter` on the controller still applies.
- An edited `Private` sense is never visible to anyone else, so no new child exposure. If a child later shares it, moderation runs as today.

## Frontend approach

- `src/api/vocabulary.ts`: `updateMyWord(id, { word, definition, example })` → `PUT /vocabulary/{id}`.
- Hook `useUpdateMyWord` (`useMutation`); on success invalidate `['vocabulary', 'mine']` (match existing key).
- `VocabularyBuilderPage`: "Edit" button only on rows with `shareStatus === 'Private'` and `isAuthor`. Inline form reuses Add form fields (React Hook Form + Zod, same schema).
- Show `409` duplicate message ("You already have this word") and generic error; no crash.
- Framer Motion for form open/close; Tailwind tokens only.

## Data / migration notes

None. Existing unique index `UX_Senses_ContentHash_OwnerUserId_Learner` enforces the duplicate rule.

## Open questions

1. `Rejected` senses: proposal lists only Private. Plan returns `409`. Allowing edit of Rejected (fix then resubmit) seems useful — Sam to confirm.
2. Word text change: plan clears `AudioAssetId` (stale audio). Image is kept. OK?
3. Edit to a hash that matches a visible system / shared sense: plan keeps the own sense (no auto-merge into the shared one), since merge would change the id. OK?
4. Response: `204` (plan) vs `200` with the updated DTO.

Ideas beyond scope (not planned):
- Edit enrichment fields (examples list, synonyms, tags) for learner senses.
- Allow edit of `Rejected` then auto-resubmit.
