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
| Build | `/code <slug>` | issue assigned to `@me`, `feature/<slug>` branch, one commit + one push, **pull request** `feature/<slug>` → `main` (`pr:` in proposal) | In progress |
| Review | `/review <slug>` | `review.md` (findings: blocker / major / nit), review posted on the PR | In review (approve) / In progress (changes requested) |
| Verify | `/test <slug>` | `test-report.md`, living spec update, **PR merged** (`gh pr merge --merge`, `Closes #n`) | Done / In progress (needs fixes) |
| Ship | `/release` | `docs/releases/<v>.md`, git tag, kind deploy | Released |
| Look | `/status` | table of all proposals + next step | — |

The board column is set by the command itself through **Board sync**
(`docs/sdlc/github-integration.md` → Board sync), which also holds the full `status:` ↔ column
mapping (`implemented` stays In progress; `blocked` leaves the card where it is). Board sync never
blocks a stage and never changes `status:`.

## One commit + one push per stage per cycle

A **cycle** is one run of a stage command: a first `/code` run, a `/code` fix round, a `/review`
round, or a `/test` run. Each cycle makes **at most one bookkeeping commit and one push**:

| Cycle | Commit |
| --- | --- |
| `/code` first run | `<slug>: implement` — code, `tasks.md`, plan folder, `proposal.md` (`implemented`) |
| `/code` fix round | `<slug>: fix round <n>` |
| `/code` stopped / partial run | `<slug>: wip (<k>/<n> tasks)`, status stays `in-progress` |
| `/review` | `<slug>: review round <n>` |
| `/test` approved | `<slug>: tests, report, mark done` — then merge |
| `/test` not approved | `<slug>: tests, report, needs fixes` |
| `/test` PR conflicting | report only, status unchanged |

The coder subagent makes no commits or pushes; `/code` stages exactly the files it touched plus
`.claude/plans/<slug>/` (never `git add -A`). Not counted as bookkeeping commits:

- the local `git merge origin/main` merge commit that `/test` (or `/code` conflict resolution)
  creates;
- the failure-path `<slug>: undo mark done — <reason>` commit in `/test`, made only when the PR
  turns out not mergeable or the merge fails/is declined after the push.

**`pr:` is a lagging cache.** The PR number only exists after `/code`'s single push, and rewriting
that commit would need a force-push. So `/code` writes `pr: <n>` into `proposal.md` in the working
tree only; the next stage's commit carries it. Every consumer (`/review`, `/test`, `/status`)
treats `pr: none` as "unknown" and looks the PR up first:

```bash
gh pr list -R hunglu/WordBuddy --head feature/<slug> --state all --json number --jq '.[0].number'
```

**Entry point:** an issue on GitHub (or `/propose` directly for quick ideas).
**Loop:** nothing runs on its own. Each session: `/status` → take the top item → run its next
command. Human gates: reviewing `plan.md` before `/code`, reading `review.md` before `/test`,
and approving every `git push`, PR post and PR merge.

## Merge guard

`/test` merges only when **all three** hold (checked by the tester right before merging):

1. **Approved review of this code.** `review.md`'s verdict is Approve, and its
   `Reviewed commit: <sha>` is an ancestor of the branch head
   (`git merge-base --is-ancestor <sha> HEAD`).
2. **Only test-scope changes since the review.** Compare HEAD (which has `origin/main` merged
   in) with "the reviewed code + current main":

   ```bash
   # needs git ≥ 2.38
   if ! out=$(git merge-tree --write-tree <sha> origin/main); then
     echo "GUARD FAIL: reviewed code conflicts with main"
   else
     base=$(printf '%s\n' "$out" | head -n1)
     git diff --name-only "$base" HEAD
   fi
   ```

   - `main`'s own commits are part of `base`, so they never show up — merging `main` in doesn't
     block the merge (that code was reviewed in its own PR).
   - Anything changed on the branch after the review shows up, **including edits made while
     resolving a merge conflict**, because those exist only in HEAD.
   - If `git merge-tree` exits non-zero, the reviewed code conflicts with `main`, so any
     resolution is unreviewed → the guard fails. Always test the exit code: on a conflict the
     command prints conflict lines after the tree id, so its output is not a usable `base`.

   Every listed path must be on the tester allowlist: `**/*.UnitTests/**`,
   `**/*.IntegrationTests/**`, `e2e/**`, `.claude/plans/<slug>/test-report.md`,
   `.claude/plans/<slug>/proposal.md`, `.claude/plans/<slug>/review.md`, `docs/features/**`.
   `plan.md` and `tasks.md` are not on it; `review.md` is, because the reviewer commits it after
   the commit it reviewed. Any other path means code changed after the review → don't merge;
   status `needs-fixes` with "changed after review — run `/review`".
3. **Every suite ran in this session and passed** (a skipped suite is not a pass).

Because of 1–2, `/test` accepts a proposal that is `reviewed` **or** `needs-fixes`: a re-test after
a test-only failure (services down, flaky test fixed in `e2e/`) passes the guard, while a
re-test after an application-code fix fails it and is sent back through `/review`.

**A conflicting PR is left as it is.** If the PR conflicts with `main` (when `/test` merges
`main` in, or when GitHub reports it not mergeable), `/test` changes no status and merges nothing;
the PR stays open until the conflict is resolved on `feature/<slug>` via `/code` and re-reviewed.
`done` is committed and pushed together with the tests in one commit; if the PR then turns out not
mergeable or the merge fails, one corrective commit restores `proposal.md` and `docs/features/`
from before it (path-limited `git checkout <sha>^ -- …`, not a `git revert`, so the tests stay).

**Nothing reaches `main` without a review.** `/test` refuses anything not `reviewed` or
`needs-fixes`, and the merge guard blocks any code change made after the review (review fixes,
test fixes, conflict resolutions) until it has gone through `/review`. The reviewer
can't approve its own account's PR on GitHub, so the verdict lives in `review.md` and a PR
comment; branch protection that requires an approving review needs a second GitHub account.

## Blocked

Any stage may stop on something outside the workflow (a decision only Sam can make, a missing
external service or account, a dependency on another proposal). Then:

- **Enter:** the command that hits it sets `status: blocked` and adds `blocked-from: <status it
  had>` and `blocked-by: <one line: what is needed, from whom>` to the frontmatter (normal
  version bump). It never guesses past the blocker.
- **Exit:** once the blocker is resolved, Sam (or Claude on Sam's word) sets `status` back to the
  `blocked-from` value, removes both fields, bumps the version, and runs that stage's command
  again. `/status` lists blocked proposals with their `blocked-by`.
- A conflicting PR is **not** `blocked` — it keeps its status (see above).

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
