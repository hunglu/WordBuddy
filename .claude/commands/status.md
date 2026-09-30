---
title: Status — show the workflow board
description: Lists every proposal under .claude/plans/ with its status, type, and linked issue, and suggests the next command for each. Read-only.
---

## Context

The local tracker for the **propose → plan → code → test → release** workflow. Read-only: never
edits any file, never changes a status.

## Steps

1. For each folder in `.claude/plans/`, read `proposal.md` frontmatter (`title`, `status`, `type`,
   `issue`). A folder without `proposal.md` (e.g. a one-off `notes.md`) is listed as `untracked`.
2. Print one markdown table grouped by status, in this order:
   `needs-fixes`, `implemented`, `in-progress`, `planned`, `idea`, `blocked`, `released`, `done`,
   `untracked`.
   Columns: slug · title · type · issue · next step.
3. Next step per status:
   | status | next |
   |---|---|
   | idea | `/plan <slug>` |
   | planned | review `plan.md`, then `/code <slug>` |
   | in-progress | `/code <slug>` (resume) |
   | implemented / needs-fixes | `/test <slug>` (or `/code <slug>` to fix first) |
   | done | `/release` when ready to ship |
   | blocked | read the blocker in `proposal.md` |
4. End with one line recommending the single most useful next action (fix before build before
   plan: `needs-fixes` > `implemented` > `in-progress` > `planned` > `idea`).
