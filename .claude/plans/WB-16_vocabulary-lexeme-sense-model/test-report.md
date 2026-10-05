# Test report: WB-16 Vocabulary lexeme sense model

Branch `feature/WB-16_vocabulary-lexeme-sense-model` with `origin/main` merged (already up to date). Run: 2026-10-05T10:07:45+07:00. SQL Server: LocalDB.

## Backend unit/integration

| Suite | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| WordBuddy.Content.UnitTests | 103 | 0 | 0 |
| WordBuddy.Content.IntegrationTests | 52 | 0 | 0 |
| WordBuddy.Progress.UnitTests | 10 | 0 | 0 |
| WordBuddy.Progress.IntegrationTests | 2 | 0 | 0 |

Progress green → Sense ids stay compatible. No new tests added at `/test`: the plan's test tasks were all done in `/code` and reviewed.

## E2E API

Not applicable — plan states no API surface change (JSON unchanged). JSON shape is covered by the Content integration tests (`LessonDetail_Get_JsonShapeUnchanged`, `mine`/`shared` shape tests). Services were not running (`:5081` unreachable).

## E2E UI

Not applicable — plan states no UI change (`WordBuddy.UI`, `e2e`: none).

## Merge guard

| Guard | Result |
| --- | --- |
| 1. Review = Approve, `efd9fc9` ancestor of HEAD | Pass |
| 2. Paths changed since review | `.claude/plans/WB-16_vocabulary-lexeme-sense-model/proposal.md`, `.claude/plans/WB-16_vocabulary-lexeme-sense-model/review.md` — all allowlisted; plus this run's `test-report.md`, `docs/features/vocabulary.md` |
| 3. Every applicable suite ran and passed | Pass |

## Docs

- `docs/features/vocabulary.md` — updated (ER diagram, Child rule per Sense, WB-16 to Change history).
- `docs/features/vocabulary-builder.md` — checked; no old table or type names.
- Not done (outside the merge-guard allowlist), follow-up: `docs/database-diagram/content.md` (stale, flagged in review), `WordBuddy/src/Services/Content/README.md` (entity list).

## Failures

None.

## Verdict

Approved — merged into main
