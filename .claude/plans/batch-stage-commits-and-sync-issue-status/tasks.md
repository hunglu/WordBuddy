# Tasks: Batch stage commits and sync issue status

## Workflow tooling (stands in for "Backend")

- [x] `docs/sdlc/github-integration.md`: add a "Board sync" section with the recipe from
      `plan.md` §2 (id lookup, `item-add`, `item-edit`, `--add-assignee @me`), the status ↔
      column mapping table, the skip/never-block rules, and the `project` scope prerequisite.
      Update the Bước 5 table so the board moves come from commands, not manual drags. — Every
      command can link to one recipe instead of repeating it. — docs/sdlc/github-integration.md
- [x] `docs/sdlc/workflow.md`: state the "one bookkeeping commit + one push per stage per cycle"
      rule; define a cycle; exempt the `origin/main` merge commit and the failure-path "undo mark
      done" commit; describe `pr:` as a lagging cache with the `gh pr list --head` fallback;
      update the Board column table to the new mapping. — The rule is in one place and matches
      the commands. — docs/sdlc/workflow.md
- [x] `.claude/agents/coder.md`: remove the per-task commit step and the `git push -u`. The coder
      makes no commits or pushes, still checks off `tasks.md` and runs a build per task, and
      returns the list of files it touched. — `coder.md` has no `git commit`/`git push`
      instructions. — .claude/agents/coder.md
- [x] `.claude/commands/code.md`: at the start, set `in-progress` and run board sync (assign
      `@me` + In progress; fix rounds Status only). At the end, make one `git add` of the
      coder's files + `.claude/plans/<slug>/`, one commit (`implement` / `fix round <n>` /
      `wip`), and one `git push -u origin feature/<slug>`. Then `gh pr create` (first run) or
      `gh pr comment` (fix round). Write `pr: <n>` to the working tree without committing it. —
      A first run produces exactly one commit and one push. — .claude/commands/code.md
- [x] `.claude/agents/reviewer.md` + `.claude/commands/review.md`: resolve the PR through the
      `gh pr list --head` fallback when `pr:` is `none`. The single review commit includes any
      pending `pr:` edit. After the verdict, run board sync (In review / In progress). — Still
      one commit per review round, and the board matches the verdict. — .claude/agents/reviewer.md,
      .claude/commands/review.md
- [x] `.claude/agents/tester.md` + `.claude/commands/test.md`: replace steps 2–6 with the
      batched order from `plan.md` §1. One commit for each outcome (approved / needs-fixes /
      conflicting); the push comes before the mergeability check; the undo is a path-limited
      checkout, not a `git revert`. Board sync: In progress for needs-fixes, Done check after
      the merge. Update the Verification list to match. — The tester's three outcomes each
      produce one commit and stay on the merge-guard allowlist. — .claude/agents/tester.md,
      .claude/commands/test.md
- [x] `.claude/commands/propose.md`, `plan.md`, `release.md`: board sync to Ready / Planned /
      Released per the mapping table. — Each stage sets its column or reports why it skipped.
      — .claude/commands/propose.md, .claude/commands/plan.md, .claude/commands/release.md
- [x] `.claude/commands/status.md`: use the `gh pr list --head` fallback for the PR column when
      `pr:` is `none`. — `/status` doesn't flag a PR as missing just because `pr:` lags.
      — .claude/commands/status.md
- [x] `.claude/settings.json`: add ask-gates `Bash(...)`/`PowerShell(...)` for
      `gh project item-edit:*`, `gh project item-add:*`, `gh project item-archive:*`, plus
      `PowerShell(git push:*)`. — The JSON parses, and every GitHub write and push is gated on
      both shells. — .claude/settings.json
- [ ] **Root `CLAUDE.md` (ask-gated, Sam approves the exact text).** Replace the `/code` row
      `` | `/code <slug>` | `coder` | `feature/<slug>` branch, one commit per task, pull request opened → `implemented` | ``
      with
      `` | `/code <slug>` | `coder` | issue assigned + board In progress, `feature/<slug>` branch, one commit + one push per run, pull request opened → `implemented` | ``
      and add the bullet: `- One bookkeeping commit and one push per stage run (cycle); board
      Status follows `status:` (see docs/sdlc/github-integration.md → Board sync).` under the
      workflow table. — The CLAUDE.md change goes in only with Sam's approval, through
      Edit/Write, never through Bash.

## Frontend

- None.

## Tests

- [ ] Consistency check: `settings.json` parses (`node -e "JSON.parse(...)"`); no remaining
      "one commit per task", "record PR", separate "mark implemented" or "needs fixes" commit
      instructions (`Grep` across `.claude/` and `docs/sdlc/`); status names and the column
      mapping are identical in `workflow.md`, `github-integration.md` and every command. —
      Grep finds nothing stale and all mappings match.
- [ ] Dry run: run the board-sync id lookups read-only against the real board and confirm every
      mapped option name exists (this answers Open question 2). — Every mapped column resolves
      to an option id.
- [ ] Live run on the next real proposal (or this one's own `/code`): count commits and pushes per
      stage with `git log --oneline main..feature/<slug>`, and confirm the issue is assigned and In
      progress after `/code` starts and the board follows through review/test/done. Record the
      result in `test-report.md`. — At most one bookkeeping commit per stage per cycle, and the
      board matches `status:` at every stage.
- [ ] Ask-gate check: confirm that `gh project item-edit`, `gh issue edit --add-assignee` and
      `git push` each prompt for approval in both Bash and PowerShell. — No GitHub write or push
      runs without a prompt.
