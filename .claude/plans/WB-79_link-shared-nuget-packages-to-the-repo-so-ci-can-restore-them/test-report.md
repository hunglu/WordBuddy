# Test report: WB-79 Link shared NuGet packages to the repo so CI can restore them

## Backend unit/integration

No application logic change (package versions + `Directory.Build.props`). Existing suites run on the branch after `git merge origin/main` (already up to date).

| Service | Provider | Unit | Integration |
|---|---|---|---|
| Identity | SQL Server (localdb) | 262 pass | 66 pass |
| Content | SQL Server (localdb) | 481 pass | 235 pass |
| Quiz | SQL Server (localdb) | 17 pass | 10 pass |
| Progress | SQL Server (localdb) | 472 pass | 106 pass |
| Identity | PostgreSQL 16 (throwaway container, removed) | 262 pass | 66 pass |
| Content | PostgreSQL 16 | 481 pass | 219 pass, 16 skipped |
| Quiz | PostgreSQL 16 | 17 pass | 10 pass |
| Progress | PostgreSQL 16 | 472 pass | 106 pass |
| Notification | — | builds; no tests exist | no tests exist |

The 16 Content skips are expected: SQL Server migration-replay tests skip on PostgreSQL (`Skip = "SQL Server migration replay; PostgreSQL has a single InitialCreate."`).

## E2E API

Skipped — not needed. No API behaviour change.

## E2E UI

Skipped — not needed. No UI behaviour change.

## Failures

None.

## Merge guard 2 (changes since review)

`git diff --name-only <merge-tree of e26e71e + origin/main> HEAD`:

- `.claude/plans/WB-79_.../proposal.md`
- `.claude/plans/WB-79_.../review.md`

Both on the allowlist. Review sha e26e71e is an ancestor of HEAD.

## Notes

- Real CI proof (`images.yml` restore with `GITHUB_TOKEN`, 6/6 images) is still open. Run `images.yml` by hand on `main` before tagging `v1.0.1` (review nit 1).
- The `v1.0.1` release check stays deferred to `/release`.

## Verdict

Approved — merged into main
