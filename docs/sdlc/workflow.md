# SDLC workflow

```
GitHub Issue (Backlog) ─▶ /propose #n ─▶ /plan ─▶ [Sam review] ─▶ /code ─▶ /test ─▶ /release
        idea                 idea         planned               in-progress  done     released
                                                                implemented  needs-fixes ─┐
                                                                     ▲────────────────────┘
```

| Stage | Command | Output | Board column |
|---|---|---|---|
| Intake | new GitHub issue (form) | issue `#n` | Backlog |
| Capture | `/propose #n <title>` | `.claude/plans/<slug>/proposal.md` | Ready |
| Plan | `/plan <slug>` | `plan.md`, `tasks.md` | Planned |
| Build | `/code <slug>` | `feature/<slug>` branch, commits, PR | In progress |
| Verify | `/test <slug>` | `test-report.md`, living spec update, merge | Done |
| Ship | `/release` | `docs/releases/<v>.md`, git tag, kind deploy | Released |
| Look | `/status` | table of all proposals + next step | — |

**Entry point:** an issue on GitHub (or `/propose` directly for quick ideas).
**Loop:** nothing runs on its own. Each session: `/status` → take the top item → run its next
command. Human gates: reviewing `plan.md` before `/code`, and approving every `git push`.

## Changing an existing feature
1. Read `docs/features/<feature>.md` (current behaviour).
2. `/propose` with `type: change`, `affects: docs/features/<feature>.md`, `supersedes: <old-slug>`.
3. Normal flow. `/test` rewrites the living spec on merge. The old plan folder is never edited.

## Bugs
`type: bugfix`. Small fixes may keep `plan.md` short but still get a folder + `test-report.md`.
