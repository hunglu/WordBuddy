# Test report: WB-12_Defect on sharing word

## Backend unit/integration

| Suite | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| Content.UnitTests | 78 | 0 | 0 |
| Content.IntegrationTests | 37 | 0 | 0 |
| Progress.UnitTests | 10 | 0 | 0 |
| Progress.IntegrationTests | 2 | 0 | 0 |
| UI `npm run build` | OK | — | — |

## E2E API

12 passed, 0 failed (`docker compose up -d --build`, all `/health/ready` = 200).

## E2E UI

17 passed, 0 failed (includes `vocabulary-sharing.feature`: "Your word" badge; confirm and hand-over).

Migration check: Content applied `20261002174303_DropVocabularyWordIdRemaps` at startup; `WordBuddyContent.dbo.VocabularyWordIdRemaps` does not exist.

## Merge guard

| Check | Result |
| --- | --- |
| Review verdict | Approve, round 2, commit `70e1cd4` |
| `70e1cd4` ancestor of HEAD | Yes |
| Changes since review (vs `merge-tree 70e1cd4 origin/main`) | `.claude/plans/WB-12_defect-on-sharing-word/proposal.md`, `.claude/plans/WB-12_defect-on-sharing-word/review.md` — all allowlisted |

## Failures

None.

## Known follow-ups (review nits, not blockers)

| # | Item |
| --- | --- |
| 1 | `DeleteWordConfirmDialog`: focus returns to `body`, not the Delete button (trigger captured after `autoFocus`). |
| 2 | Plain-path 409 dialog text uses the stale `shareStatus`; it may say "handed over" for a `PendingReview` word. |
| 3 | `docs/architecture.md` still shows the `/internal/vocabulary-remaps` arrow and `wb_service` note (not on the `/test` allowlist). |

## Verdict

Approved — merged into main
