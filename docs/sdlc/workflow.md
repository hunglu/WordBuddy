# SDLC workflow

```
Issue ─▶ /propose ─▶ /plan ─▶ [Sam reads plan] ─▶ /code ──────▶ /review ──────▶ /test ──────▶ /release
 idea      idea      planned                      in-progress    reviewed         done           released
                                                  implemented +  changes-         needs-fixes
                                                  PR opened      requested
                                                     ▲               │                │
                                                     └── /code fix ◀─┴────────────────┘
```

| Stage | Command | Output | Board column |
| --- | --- | --- | --- |
| Intake | new GitHub issue (form) | issue `#n` | Backlog |
| Capture | `/propose [#n] <title>` | `proposal.md`, living spec stub or pending line in `docs/features/`, GitHub issue created if none given | Ready |
| Plan | `/plan <slug>` | `plan.md`, `tasks.md` | Planned |
| Build | `/code <slug>` | `feature/<slug>` branch, commits, **pull request** `feature/<slug>` → `main` (`pr:` in proposal) | In progress |
| Review | `/review <slug>` | `review.md` (findings: blocker / major / nit), review posted on the PR | In review |
| Verify | `/test <slug>` | `test-report.md`, living spec update, **PR merged** (`gh pr merge --merge`, `Closes #n`) | Done |
| Ship | `/release` | `docs/releases/<v>.md`, git tag, kind deploy | Released |
| Look | `/status` | table of all proposals + next step | — |

**Entry point:** an issue on GitHub (or `/propose` directly for quick ideas).
**Loop:** nothing runs on its own. Each session: `/status` → take the top item → run its next
command. Human gates: reviewing `plan.md` before `/code`, reading `review.md` before `/test`,
and approving every `git push`, PR post and PR merge.

**Nothing reaches `main` without a review.** `/test` refuses anything not `reviewed`; any code
change after a review (review fixes or test fixes) goes back through `/review`. The reviewer
can't approve its own account's PR on GitHub, so the verdict lives in `review.md` and a PR
comment; branch protection that requires an approving review needs a second GitHub account.

## Proposal versioning

Every `proposal.md` carries three fields in its frontmatter:

```yaml
version: 1.0                        # 1.0 on creation
created: 2026-09-30T23:00:44+07:00  # never changes
updated: 2026-09-30T23:00:44+07:00  # timestamp of the latest edit
```

- Timestamps are ISO 8601 local time with UTC offset, to the second. Always take them from the
  clock (`date +%Y-%m-%dT%H:%M:%S%:z`), never type or estimate them.
- `/propose` writes `version: 1.0` and `created` = `updated` = now.
- **Every** later edit to `proposal.md` — a status change by `/plan`, `/code`, `/test` or
  `/release`, a scope revision, a hand edit — adds 1 to the major number (`1.0` → `2.0` → `3.0`)
  and sets `updated` to now. One edit session = one bump, even if several fields change.
- A scope change (not just a status change) also appends a line to `## Revisions`:
  `- **v<version> — <timestamp>** — <what changed>`. Git history holds the full diff.

## Changing an existing feature

1. Read `docs/features/<feature>.md` (current behaviour).
2. `/propose` with `type: change`, `affects: docs/features/<feature>.md`, `supersedes: <old-slug>`.
3. Normal flow. `/test` rewrites the living spec on merge. The old plan folder is never edited.

## Bugs

`type: bugfix`. Small fixes may keep `plan.md` short but still get a folder + `test-report.md`.
