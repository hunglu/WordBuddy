# Test report: WB-50 Lexeme admin view

## Backend unit/integration

| Suite | Pass | Fail |
|---|---|---|
| Content unit tests | 415 | 0 |
| Content integration tests (real SQL Server) | 184 | 0 |

- First integration run: 182 pass, 2 fail (`UnifyVocabularyWordsMigrationTests`, `SplitVocabularyIntoLexemesAndSensesMigrationTests`, 1 ms each, ran while docker was rebuilding). The 16 migration tests pass alone, and the full re-run passes 184/184. Treated as a transient environment fault.

## E2E API

73 pass, 0 fail (includes `LexemeAdminTests`). Run against a rebuilt `content-api`.

## E2E UI

52 pass, 0 fail (includes `lexeme-admin.feature`). Run against a rebuilt `wordbuddy-ui`.

## Upload limit through Nginx (UI origin :3000)

| Upload to `PUT /api/vocabulary/admin/lexemes/{id}/audio/en-GB` | Result |
|---|---|
| 2 MB `audio/mpeg` | 200 (no 413) |
| 7 MB | 413 from nginx (expected, limit 6m) |

The test lexeme had no audio; the audio was removed afterwards (`DELETE` 204).

## Merge guard

1. `review.md` verdict Approve, reviewed commit `c395194` is an ancestor of HEAD: yes.
2. Files changed since the review (`git merge-tree` base vs HEAD), all on the allowlist:
   - `.claude/plans/WB-50_lexeme-admin-view/proposal.md`
   - `.claude/plans/WB-50_lexeme-admin-view/review.md`
   - `docs/ai/learnings.md`
3. All four suites ran, zero failures.

## Known coverage gaps

- The Linux branch of the `LocalFileStorageService` path guard is untested (Windows host).
- The remove/upload race is an accepted risk (last write wins).
- Ingress 50m limit and nginx syntax in kind were not run on a cluster.

## Failures

None.

## Verdict

Approved — merged into main
