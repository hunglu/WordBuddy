# Plan: Batch stage commits and sync issue status

## Summary

Change the workflow tooling so each stage run makes **one bookkeeping commit and one push per
cycle**. The coder stops committing once per task. Instead, `/code` commits once when all tasks
are done, and once per fix round. Also add a **board sync** step. When `/code` starts, it assigns
the issue to the gh-authenticated user (`@me`) and sets the `WordBuddy` project Status to
*In progress*. Each later stage sets the Status that matches the new `status:` value. Every push,
issue edit and board edit stays ask-gated. This change touches only workflow docs and commands. No
application code changes.

## Affected services / areas

None of the 5 services and not WordBuddy.UI. Workflow tooling only:

- `.claude/commands/{propose,plan,code,review,test,release,status}.md`
- `.claude/agents/{coder,reviewer,tester}.md`
- `.claude/settings.json` (new ask-gates)
- `docs/sdlc/workflow.md`, `docs/sdlc/github-integration.md`
- Root `CLAUDE.md`: one table row. It is ask-gated, so this plan gives the exact text for Sam to
  approve.

**Child vs adult:** not applicable. Nothing here changes runtime behaviour, so `AgeGroup = Child`
is unaffected.

## Approach

### 1. Commit/push batching (one commit + one push per stage per cycle)

A "cycle" is one run of a stage command: a first `/code` run, a fix round, a review round, or a
test run.

| Stage | Today | After |
| --- | --- | --- |
| `/code` (first run) | initial `push -u` of an empty branch, one commit per task, "mark implemented" commit + push, "record PR #n" commit + push | branch created locally (not pushed yet). Coder implements **all** tasks with no commits. `/code` makes **one** commit (`<slug>: implement`) with the code, `tasks.md`, the plan folder and `proposal.md` (`status: implemented`). Then **one** `git push -u origin feature/<slug>` and `gh pr create` |
| `/code` (fix round) | per-finding commits + mark implemented | **one** commit `<slug>: fix round <n>` + one push + PR comment |
| `/code` (partial / stopped run) | per-task commits already pushed | **one** commit `<slug>: wip (<k>/<n> tasks)` + one push, status stays `in-progress` |
| `/review` | one commit "review round n" | unchanged (already one). It also carries the `pr:` field (see below) |
| `/test` approved | "tests and test report" commit + push, then "mark done" commit + push, then merge | **one** commit `<slug>: tests, report, mark done` + one push, then merge |
| `/test` not approved | "tests and test report" + "needs fixes" (2 commits, 2 pushes) | **one** commit `<slug>: tests, report, needs fixes` + one push |
| `/test` PR conflicting | report commit + push | **one** commit (report only, status unchanged) + one push. If `git merge origin/main` itself conflicts: nothing, as today |

**Where `pr:` gets recorded without an extra commit.** GitHub can't open a PR until the branch has
commits on `origin`, so the PR number only exists *after* the single push. Rewriting that commit
would need a force-push, which is denied. Decision:

- After `gh pr create`, `/code` writes `pr: <n>` into `proposal.md` in the **working tree only**
  and does not commit it. The next stage's single commit carries it: the reviewer already commits
  `proposal.md`, and if `/review` runs first the tester's commit does.
- So `proposal.md` on the branch can lag by one stage. Every consumer (`/review`, `/test`,
  `/status`) must treat `pr:` as a cache: if it is `none`, it looks the PR up with
  `gh pr list -R hunglu/WordBuddy --head feature/<slug> --state all --json number --jq '.[0].number'`
  before deciding the PR doesn't exist. Then a lost working-tree edit (for example a different
  machine) costs nothing.
- `coder.md`'s "uncommitted changes" check already allows `.claude/plans/<slug>/` files, so the
  uncommitted `pr:` edit doesn't block a fix round.
- The `pr:` edit counts as part of the same `/code` edit session, so it gets no extra version bump.

**Tester "mark done" and the merge guard.** The batched approved-path commit contains tests,
`test-report.md`, `proposal.md` (`done`), `docs/features/**` and the README row. All of these are
on the allowlist, so guard 2 is unaffected. The order becomes:

1. Verdict (guard 1–3).
2. Write tests, report (verdict line "Approved — merged into main"), status `done`, living spec.
3. One commit and one push (ask-gated). If Sam declines the push, nothing is pushed: reset the
   bookkeeping edits locally to pre-`done` and stop. Don't merge.
4. Mergeability check. It now runs *after* the push, because the push includes the `origin/main`
   merge and that changes mergeability.
5. Merge through the PR.

If step 4 reports `CONFLICTING`/`UNKNOWN`, or step 5 fails or is declined, make **one** corrective
commit (`<slug>: undo mark done — <reason>`). That commit uses
`git checkout <sha>^ -- .claude/plans/<slug>/proposal.md docs/features/` plus a verdict-line
edit. It is not a full `git revert`, because a revert would also remove the tests. This is the one
documented exception to "one commit per cycle", and it only happens on the failure path.

The local `git merge origin/main` merge commit that `/test` (and `/code` conflict resolution)
creates is not a bookkeeping commit and isn't counted. `workflow.md` will say so explicitly.

**Coder changes.**
- Remove the per-task `git commit` step. The coder still checks off `tasks.md` and runs
  build self-checks per task.
- Remove its `git push -u`. The coder makes **no** commits or pushes. It returns the list of files
  it touched, and `/code` stages exactly those plus `.claude/plans/<slug>/`. `/code` never uses
  `git add -A`.
- The "stop on an ask-gated command" rule stays. A stopped run is covered by `/code`'s
  partial-run commit.

### 2. Issue assignment + board sync

**Assignee:** `@me`, meaning the gh-authenticated account (`hunglu`, Sam). It is not a
hard-coded login, so it keeps working if the account changes.

**Column ↔ `status:` mapping.** The planner couldn't read the live board, so this uses the
documented options from `github-integration.md` step 4. See Open questions.

| `status:` in proposal.md | Board Status | Set by |
| --- | --- | --- |
| `idea` | Ready | `/propose` |
| `planned` | Planned | `/plan` |
| `in-progress`, `changes-requested`, `needs-fixes` | In progress | `/code` start, `/review` (changes requested), `/test` (needs fixes) |
| `implemented` | In progress | (no change) |
| `reviewed` | In review | `/review` (approve) |
| `done` | Done | `/test` after the merge. GitHub's built-in "PR merged / item closed → Done" usually got there first, so this is just a check |
| `released` | Released | `/release` |
| `blocked` | unchanged | — |

Recommended alternative for Sam to choose (see Open questions): set *In review* when `/code` opens
the PR, i.e. map `implemented` to In review.

**Mechanics.** Document one "Board sync" recipe in `docs/sdlc/github-integration.md` and have every
command reference it instead of repeating it:

```bash
# read-only, not gated: resolve ids once per run
gh project list --owner hunglu --format json                         # → project number + id for "WordBuddy"
gh project field-list <num> --owner hunglu --format json             # → Status field id + option ids by name
gh issue view <issue> -R hunglu/WordBuddy --json projectItems        # → is the issue on the board?
gh project item-list <num> --owner hunglu --format json --limit 200  # → item id for the issue (match content.number)
# gated writes
gh project item-add <num> --owner hunglu --url <issue url>           # only if not on the board yet
gh project item-edit --project-id <pid> --id <item id> --field-id <status field id> --single-select-option-id <option id>
gh issue edit <issue> -R hunglu/WordBuddy --add-assignee @me         # /code start only
```

Rules:

- Skip board sync when `issue:` is `none`/`pending`.
- If `gh` is missing, lacks the `project` scope, or Sam declines, say so and continue. Board sync
  never blocks a stage and never changes `status:`.
- Look option ids up by **name**. If the mapped option name doesn't exist on the board, report
  it and skip. Don't pick a "closest" option.
- Board sync is a GitHub write, not a git commit, so it adds no commits.

**When `/code` syncs:** at the start, before invoking the coder, together with the
`planned → in-progress` edit. That is: assign `@me` (skipped if already assigned to `@me`), then
set the Status to In progress. In a fix round it sets only the Status, back to In progress.

### 3. Ask-gates (`.claude/settings.json`)

`gh issue edit` (which covers `--add-assignee`) is already gated for Bash and PowerShell. Add the
following under `ask`:

- `Bash(gh project item-edit:*)` and `PowerShell(gh project item-edit:*)`
- `Bash(gh project item-add:*)` and `PowerShell(gh project item-add:*)`
- `Bash(gh project item-archive:*)` and `PowerShell(gh project item-archive:*)`, to keep board
  writes consistent

Also add the missing `PowerShell(git push:*)` gate. Today only the Bash form exists, and success
criterion 4 needs every push gated on both shells.

## Frontend approach

None.

## Data / migration notes

None.

## Open questions

1. **Success criteria vs Goal conflict.** The Goal (Sam's edit, which wins) replaces
   one-commit-per-task. Success criterion 1 still says "plus the coder's one-commit-per-task
   commits". This plan follows the Goal. Sam should update criterion 1 to "at most one bookkeeping
   commit and one push per stage per cycle" (a hand edit with a version bump; the planner may not
   edit `proposal.md`).
2. **Live board columns.** The planner couldn't read the board (no `gh` call allowed in planning).
   The mapping uses the options documented in `github-integration.md`:
   `Backlog · Ready · Planned · In progress · In review · Done · Released`. Before `/code`, Sam
   should confirm with `gh project field-list <n> --owner hunglu` that these names exist exactly,
   or send the real names.
3. **When does the card enter *In review*:** when the PR opens (`implemented`) or only when the
   review approves (`reviewed`)? This plan defaults to `reviewed`, which matches today's
   `workflow.md`.
4. **Lagging `pr:` field.** Is it acceptable that `pr:` reaches the branch one stage later, with
   `gh pr list --head` as the fallback? The alternative is a separate "record PR" commit, which is
   exactly what Sam wants removed.
5. **Failure-path second commit in `/test`.** Is the corrective "undo mark done" commit (only when
   the merge fails after the push) an acceptable exception to one-commit-per-cycle? The
   alternative is to check mergeability before marking done, but that check runs on the remote head
   without the local `origin/main` merge, so it is less accurate.
6. **Assignee.** This plan uses `@me` (the gh-authenticated user). Say so if it should instead be
   a fixed login or the issue author.
