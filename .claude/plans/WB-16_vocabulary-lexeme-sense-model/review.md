# Review: WB-16 Vocabulary lexeme sense model

PR: #17 · Round 1 · Reviewed commit: efd9fc9 · 2026-10-05T09:53:12+07:00

## Verdict
Approve — no blockers or majors; the migration renames in place, keeps every id, and the race paths are covered.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `AddPersonalVocabularyWordCommandHandler.cs:62-86` | `GetOrCreateLexemeAsync` commits a new lexeme before `Sense.CreateLearner` runs. If `CreateLearner` fails, the new lexeme stays with no sense. This conflicts with D16 (no orphan lexemes). Validation runs first, so this is rare. | Build the `Sense` (with a placeholder) first, or validate via `Sense.CreateLearner` before the lexeme call. |
| 2 | nit | `VocabularyWordRepository.cs:305-307` | `IsLexemeForeignKeyViolation` matches on the text `"LexemeId"` in the SQL message. A future FK whose name contains `LexemeId` would also match. | Match the constraint name `FK_Senses_Lexemes_LexemeId`. |
| 3 | nit | `LearnerWord.cs:18-21`, `Lexeme.cs:21-37` | Some public properties (`SenseId`, `Sense`, `AddedAtUtc`, `PartOfSpeech`, `IpaUk`, ...) have no XML doc comments. | Add one-line `<summary>` docs. |

## Checked and fine
- Migration `Up`: 8 steps in plan order, one transaction, unique index unfiltered (matches `.HasFilter(null)`), `sp_rename` for PK/FK, no `DropTable` or row copy.
- Migration `Down`: reverses every step; lossy parts stated in the XML summary.
- `GetOrCreateLexemeAsync`: 2601/2627 → detach → re-read; `AddAsync`: 547 → one retry → `ConcurrentAdd`; never a 500.
- `DeleteIfOrphanedAsync`: sense delete + guarded lexeme `DELETE … NOT EXISTS` in one transaction; parameterized SQL; no retrying execution strategy in Content, so a user transaction is allowed.
- Child rule: `VisibleToChildren` and `IsVisibleTo` unchanged on `Sense`; `PickVisibleDuplicate` unchanged.
- No secrets, no PII in logs (`PersonalContext` never logged), no controller/DTO/route changes.

## Plan conformance
- All Domain, Infrastructure, Application and Test tasks are reflected in the diff.
- The 4 "Docs (tester stage)" tasks are open by design. Note: `docs/database-diagram/content.md` must match the new snapshot at `/test`; it is stale until then.
- Out of scope: none found. `docs/features/vocabulary.md` only gets a "Pending changes" line.
