# Test report: WB-66 Fix Aspire secrets prompt and pin dev ports

## Backend unit/integration

Scope: AppHost only (`WordBuddy/aspire/WordBuddy.Aspire.slnx`). No service, UI or e2e code changed.

| Suite | Filter | Result |
|---|---|---|
| WordBuddy.AppHost.Tests | `Category!=Integration` | 26 passed, 0 failed |
| WordBuddy.AppHost.Tests | `Category=Integration` | 2 passed, 0 failed |

## E2E API

Skipped — not required, no service code changed.

## E2E UI

Skipped — not required, no UI code changed.

## Failures

None.

## Open manual checks (for Sam, not failures)

| Check | Status |
|---|---|
| Root cause confirmed by live `aspire run` after a volume reset (no prompt, dashboard 15180, SQL 14330, APIs healthy) | Open. Not done: Sam forbade removing containers or volumes. Integration tests (AppHost started via the testing builder, throwaway containers) are the automated evidence. |
| Integration tests while a persistent `sql` container holds 14330 | Not exercised. The only running container (`sql-d32bbd0a`) is on host port 18888, not 14330. The AppHost was not started to create one. |

## Merge guard

- `review.md` verdict Approve; reviewed commit `1e8e08b` is an ancestor of HEAD.
- Files changed since review, against reviewed code + current main (`git merge-tree` / `git diff --name-only`):
  - `.claude/plans/WB-66_.../proposal.md`
  - `.claude/plans/WB-66_.../review.md`
- All are on the allowlist.
- Every required suite ran this session with zero failures.

## Verdict

Approved — merged into main
