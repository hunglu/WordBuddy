# Review: WB-79 Link shared NuGet packages to the repo so CI can restore them

PR: #80 · Round 1 · Reviewed commit: e26e71e4fefd8ddaa58f1b992fd2256698229055 · 2026-11-26T22:34:43+07:00

## Verdict
Approve. The diff matches the plan, with no code change, no secret and no blocker or major.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | `.github/workflows/images.yml:1-5` | The core success criterion (CI restores with `GITHUB_TOKEN`, 6/6 images) is still unproven. It runs only in `/release`. If the 403 remains, the tag `v1.0.1` already exists and needs a second tag. | After the merge, run `images.yml` by hand (`workflow_dispatch`) on `main` before the tag (plan Open question 4). If it still returns 403, apply the `packageSourceMapping` fallback (Open question 1) first. |

Checks with no finding:

| Check | Result |
|---|---|
| Correctness | Version bumps are consistent. `git grep` shows no old `WordBuddy.Shared.*` version under `WordBuddy/`. `Shared.Application` → Kernel 1.1.1 matches the services. |
| `Directory.Build.props` scope | Only one props file in the repo (`WordBuddy/src/Shared/`). No parent props is shadowed. Services under `src/Services/` do not import it. ADR 0001 is kept. |
| Security | No PAT, token or connection string in the diff. `images.yml` still passes `GITHUB_TOKEN` as a BuildKit secret, not a build arg. |
| Conventions | No C#/TS change. `WordBuddy/CLAUDE.md` is unchanged, as planned. |
| Tests | No new behaviour. Builds and suites on both providers are recorded in `tasks.md` (Content: 16 skipped on Postgres). Docker restore from GitHub only and `make k8s-build-load` are green. |
| DB diagrams | No migration. Not applicable. |

## Plan conformance

| Plan item | Status |
|---|---|
| §1 `Directory.Build.props` (`RepositoryUrl`, `RepositoryType`, `PublishRepositoryUrl`) | Done |
| §2 Patch bumps, 5 packages | Done |
| §3 Service references, including 3 drift jumps (Quiz Kernel, Content Contracts, Notification Infrastructure) | Done, no code change needed |
| §4 Publish (ask-gated) + package page check | Done by Sam; repo link verified with `gh api` |
| §5 `images.yml` header, `WordBuddy/README.md` | Done |
| §5 Living spec WB-75 | Deferred to `/test` (only the "Pending changes" entry is added now, which is expected) |
| §6 `WordBuddy/CLAUDE.md` text | Not applied, as planned (Sam, after `v1.0.1` is green) |
| Release check `v1.0.1` | Deferred to `/release` |

Out-of-scope changes: none.
