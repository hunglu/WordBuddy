# Review: Add pull request and code review stage to the SDLC workflow

PR: #4 · Round 3 · Reviewed commit: 37ad18d · 2026-10-01T08:55:30+07:00

## Verdict
Changes requested. Both round-2 majors are fixed correctly in `tester.md` and `workflow.md`. One new major remains: `.claude/commands/test.md` still describes the old flow and contradicts Sam's conflict rule.

## Round-2 majors
| # | Status | Evidence |
|---|---|---|
| 1 | Resolved | `tester.md:100-172` now runs in this order: guard, then the "Pending merge" commit, then `needs-fixes` (stop), then the mergeability check (`CONFLICTING` leaves the status unchanged and stops), then the separate "mark done" commit, then `gh pr merge`. If the merge fails, a revert follows. `done` can only reach origin after GitHub has reported `MERGEABLE`. The remaining window between pushing step 5 and merging in step 6 is inherent, and the revert path covers it. |
| 2 | Resolved | `git diff --name-only $(merge-tree <sha> origin/main) HEAD` hides `main`'s commits and shows conflict-resolution edits. The allowlist is narrowed to `test-report.md`, `proposal.md` and `review.md`. I tested this with git 2.55: on a conflict, stdout contains the tree id **plus** the conflicted-file and message lines, and the exit code is 1. `$base` then becomes multi-line, and `git diff` fails with `fatal: failed to stat ... Filename too long` (exit 128). It does not print an empty list, so the guard fails loudly and does not pass silently. See nit 2 for hardening. |

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `.claude/commands/test.md:43-47, 55-60` | This file was not updated for rounds 2 and 3. Step 5 says the tester "merges `feature/<slug>` into `main` with `--no-ff` and pushes `main`", but the merge now goes through `gh pr merge`. Verification says "status is `done` or `needs-fixes`, never left at `implemented`". Under Sam's rule, a conflicting PR keeps `reviewed`, and a revert after a failed merge restores the previous status. The allowlist at line 59 also omits `docs/features/` and still allows all of `.claude/plans/<slug>/`. Scenario: GitHub reports `CONFLICTING`, and the tester correctly leaves the status at `reviewed`. The `/test` command's own verification then flags the run as wrong, and the orchestrator may "fix" it to `needs-fixes`, which breaks Sam's rule. | Step 5: say the tester follows `tester.md` → Commit, approve, and merge (`gh pr merge --merge`; `--no-ff` only when `pr: none`). Verification: `done` ⇔ merged; `needs-fixes` on a guard or suite failure; status unchanged (`reviewed`/`needs-fixes`) with "Not merged — PR has conflicts" or "declined". Point the allowlist to `workflow.md` → Merge guard. |
| 2 | nit | `tester.md:112`, `workflow.md` Merge guard | The `merge-tree` exit code is only mentioned in a comment, and the snippet does not check it. A conflict produces a `fatal` diff, which fails safely but by accident. | Use `if ! base=$(git merge-tree --write-tree <sha> origin/main); then echo "GUARD FAIL: reviewed code conflicts with main"; fi`, or add `--name-only --no-messages` and treat any exit other than 0 as a failure. |
| 3 | nit | `tester.md:159-163` | "Exactly one corrective commit" conflicts with the steps: `git revert --no-edit` commits, and then you "set the verdict line … commit", which makes two commits. | `git revert --no-commit <mark-done sha>`, edit the verdict line in `test-report.md`, then one `git commit -m "<slug>: undo mark done — <reason>"`. |
| 4 | nit | `tester.md:145-147` | If Sam declines the step-5 push, the local branch has the mark-done commit but origin does not. Step 6 would still merge the PR's origin head, so `main` would get no `done` or living-spec update. | "If the step-5 push is declined, stop, undo step 5 locally (`git reset --soft HEAD~1` is fine because it was never pushed, or use a revert), and don't merge." |
| 5 | nit | `tester.md:133` | The `UNKNOWN` result after a retry is not handled. | Treat it like `CONFLICTING`: leave everything as it is and report it. |
| 6 | nit | `docs/sdlc/github-integration.md:74` | "(chỉ khi `reviewed`)" ("only when `reviewed`") is out of date; `needs-fixes` plus the guard also applies. | "(`reviewed`/`needs-fixes`, merge guard)". |
| 7 | nit (carried) | `status.md:31` | Does not mention re-running `/test` directly after `needs-fixes` caused by test-only problems. | Add "or `/test <slug>` if no app code changed". |
| 8 | nit (carried) | `release.md:3,8` | Still says "Stage 5 of 5". | "Stage 6 of 6". |
| 9 | nit (carried) | `code.md`, `plan.md`, `propose.md`, `coder.md`, `planner.md`, `tester.md` headers | The chain is still described as 4 stages. | Update to the 6-stage chain. |
| 10 | nit (carried) | `status.md:13` | `pr` is not in the fields read. | Add `pr`. |
| 11 | nit (carried) | `status.md:17,33` | `blocked` has no entry or exit. | Document it in `workflow.md`. |
| 12 | nit (carried) | `github-integration.md` | Nothing moves the card back on `changes-requested`. | Add "→ back to In progress". |

Verified OK: `workflow.md` and `tester.md` describe the same guard, allowlist and conflict rule. `code.md`, `review.md`, `reviewer.md` and `status.md` agree on the status transitions. Round-2 nit 3 (`workflow.md:50`) and nit 5 (allowlist narrowed) are fixed. No secrets were added.

## Plan conformance
All 8 tasks are still covered. Commit 37ad18d changes only `tester.md`, `workflow.md` and `proposal.md`, which is in scope. The gap is that `test.md` was not carried along with the change.

## Previous rounds

## Round: Add pull request and code review stage to the SDLC workflow

PR: #4 · Round 2 · Reviewed commit: 3a75f78 · 2026-10-01T07:49:57+07:00

### Verdict
Changes requested. All four round-1 majors are fixed, but the fix adds two new majors. The tester's decline and conflict paths leave `status: done` on a branch that was never merged. The merge-guard command also mis-handles merges from `main`.

### Round-1 majors
| # | Status | Evidence |
|---|---|---|
| 1 | Resolved | `workflow.md` → Merge guard is the single rule. `test.md:21-25` and `tester.md:117-129` both point to it, and it applies to both `reviewed` and `needs-fixes` runs. `git merge-base --is-ancestor <sha> HEAD` is correct: exit 0 means the reviewed sha is an ancestor. |
| 2 | Resolved | The allowlist (`**/*.UnitTests/**`, `**/*.IntegrationTests/**`, `e2e/**`, `.claude/plans/<slug>/**`, `docs/features/**`) contains no `src/` app projects, `WordBuddy.UI/`, `k8s/`, `.claude/agents|commands`, or `CLAUDE.md`. `tester.md:112-114` forbids app-code changes. |
| 3 | Resolved, with a new problem in it (see finding 1) | PR path and `pr: none` fallback are split (`tester.md:153-166`). `git merge --abort` now appears only in the fallback. |
| 4 | Resolved | `tester.md:111` and `code.md:43` now say "no Co-Authored-By trailer". No remaining requirement in `.claude/` or `docs/`. |

### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `.claude/agents/tester.md:100-115, 154-159, 170-173` | Steps 1–3 set `status: done`, write "Approved — merged into main", then commit and push. All of this happens **before** `gh pr merge`. The decline bullet then says "status stays `reviewed`/`needs-fixes` as set", but the value that was set is `done`. The conflict bullet says "set `needs-fixes`", while lines 170-173 say not to commit again to fix the report. Scenario: GitHub reports the PR as not mergeable. `feature/<slug>` on origin says `done` / "Approved — merged". `/status` shows it as finished, and `/release` may pick it up. | Either (a) commit the report and status only **after** `gh pr merge` succeeds, or (b) state that on conflict or decline the tester makes one more commit on `feature/<slug>`, setting `needs-fixes` (conflict) or `reviewed` (decline) and the "Not merged — …" verdict, and pushes it. Make lines 170-173 forbid only commits on `main`. |
| 2 | major | `docs/sdlc/workflow.md:33-43`, `.claude/agents/tester.md:121` | `--no-merges` drops only the merge commit itself. `<sha>..HEAD` still lists every `main` commit that a merge brought in, so the claim "Merge commits that only bring in `main` are excluded" is false. Scenario 1: someone merges `main` into the feature branch after review. The guard lists `main`'s `src/` files and refuses the merge, which is a false block. Scenario 2: a conflicted merge whose resolution edits app code. That change exists only in the merge commit, so `--no-merges` hides it, which is an unreviewed pass. | Exclude what `main` already has, and include the merge commit's own diff. Use `git log --name-only --format= -m --first-parent <sha>..HEAD` (`-m --first-parent` shows each merge's diff against the branch side only), or simply say "any merge commit after `<sha>` fails the guard → `/review`". |
| 3 | nit | `docs/sdlc/workflow.md:50` | "`/test` refuses anything not `reviewed`" now contradicts line 46 and `test.md:21` (`reviewed` **or** `needs-fixes`). | "`/test` refuses anything not `reviewed`/`needs-fixes`, and the merge guard decides". |
| 4 | nit | `.claude/commands/status.md:31` | The `needs-fixes` next step is always `/code` → `/review` → `/test`. It doesn't mention the guard-allowed direct re-run of `/test` (services down, test-only fix). | Add "or `/test <slug>` if no app code changed". |
| 5 | nit | `docs/sdlc/workflow.md:40`, `tester.md:122` | `.claude/plans/<slug>/**` lets the tester change `plan.md`/`tasks.md` after review. | Narrow it to `test-report.md` and `proposal.md`. |
| 6 | nit (carried, R1 #5) | `.claude/commands/release.md:3,8` | Still reads "Stage 5 of 5". | "Stage 6 of 6 …". |
| 7 | nit (carried, R1 #6) | `code.md`, `plan.md`, `propose.md`, `agents/coder.md`, `planner.md`, `tester.md` headers | Stale 4-stage chain wording. | Update to the 6-stage chain. |
| 8 | nit (carried, R1 #7) | `.claude/commands/status.md:13` | `pr` is not in the fields read. | Add `pr`. |
| 9 | nit (carried, R1 #8) | `.claude/commands/status.md:17,33` | `blocked` has no entry or exit. | Document it in `workflow.md`. |
| 10 | nit (carried, R1 #9) | `docs/sdlc/github-integration.md` | No card move on `changes-requested`. | Add "→ back to In progress". |

Verified OK: the status lists still agree. `/review` accepts `implemented`. `/code` accepts `changes-requested`/`needs-fixes`. `/test` accepts `reviewed`/`needs-fixes`. `status.md` orders all of them. The `review.md` header carries `Reviewed commit: <sha>`, which the guard needs. `sort -u` runs under the Bash tool (Git Bash). No secrets were added.

### Plan conformance
All 8 tasks are still covered. Commit 3a75f78 changes only the files the round-1 majors named, plus `proposal.md`. Nothing is out of scope.


## Round: Add pull request and code review stage to the SDLC workflow

PR: #4 · Round 1 · Reviewed commit: ed83bc8 · 2026-10-01T01:07:10+07:00

### Verdict
Changes requested — the design is sound, but the `/test` re-test exception can't work because the tester's merge guard contradicts it, the tester's own commits reach `main` without review, the tester's conflict and decline paths still describe a local merge, and two files still require the Co-Authored-By trailer.

### Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | major | `.claude/commands/test.md:21-25` vs `.claude/agents/tester.md:138` | `/test` accepts `needs-fixes` when Sam asks for a re-test without code changes, but the tester only merges when `proposal.md` "was `reviewed` when you started". Scenario: services were down, Sam re-runs `/test`, every suite passes, and the tester refuses to merge. The exception also never checks that there really were no code changes. | Define one guard in both files: merge only if `review.md` says Approve **and** `git log <reviewed sha>..origin/feature/<slug>` has only tester commits (tests, test-report, proposal). Use that same check for the exception instead of relying on Sam's word. |
| 2 | major | `docs/sdlc/workflow.md` ("Nothing reaches `main` without a review") and `.claude/agents/tester.md:111` | The tester commits new tests and test-support changes to `feature/<slug>` after the review and then merges them. The tester can also fix things in a way that touches app code (for example, test seeding). So `main` gets unreviewed commits, which contradicts the new rule. | Either state the exception explicitly ("tester commits limited to test projects, `e2e/`, `test-report.md`, `docs/features/`, `proposal.md`") and have the tester check its own diff against that allowlist before merging, or send it back through `/review`. |
| 3 | major | `.claude/agents/tester.md:142-145` | The conflict and decline handling still describes the local `git merge` path: it tells the tester to run `git merge --abort` and says "the local merge stays". With `gh pr merge`, a conflicted PR fails on the server, and if Sam declines there is no local merge. Scenario: `gh pr merge` fails with "not mergeable" and the tester runs `git merge --abort` with no merge in progress. The verdict text is then wrong. | Split the bullets: for the PR path, a `gh pr merge` failure or a decline means "Not merged — <reason>", status `needs-fixes`, and nothing local to undo. Keep the current bullets for the `pr: none` fallback only. |
| 4 | major | `.claude/agents/tester.md:111`, `.claude/commands/code.md:43` | These lines still require a Co-Authored-By trailer. That contradicts `coder.md:56`, `reviewer.md:87` and Sam's instruction to drop it. | Change both to "no Co-Authored-By trailer". |
| 5 | nit | `.claude/commands/release.md:3,8` | Still reads "Stage 5 of 5 … code → test → release". It now clashes with `/test` being Stage 5 of 6. | Change it to "Stage 6 of 6: propose → plan → code → review → test → release". |
| 6 | nit | `.claude/commands/code.md:3,9`, `plan.md:3,9`, `propose.md:3,9`, `agents/coder.md:8`, `agents/planner.md:8`, `agents/tester.md:8` | Stale "idea → plan → code → test" and "Stage n of 4" wording. | Update to the 6-stage chain. |
| 7 | nit | `.claude/commands/status.md:13` | Step 1 reads only `title,status,type,issue`, but the table now needs `pr`. | Add `pr` to the fields read. |
| 8 | nit | `.claude/commands/status.md:17,33` | No command ever sets `blocked`, and nothing says how to leave it. It has no entry or exit in the state machine. This predates the PR. | In `workflow.md`, document "Sam sets `blocked` by hand; to leave it, restore the previous status". |
| 9 | nit | `docs/sdlc/github-integration.md` Code row | The card moves to **In review** in the Review row, but nothing moves it back to In progress on `changes-requested`. | Add "`changes-requested` → back to In progress". |

Verified OK: `settings.json` is valid JSON. `gh pr create/merge/review/comment/edit` are all ask-gated for both Bash and PowerShell, and nothing the flow needs is denied. The `gh pr merge --merge --subject --body`, `gh pr review --comment --body-file` and `gh pr comment --body-file` flags are valid. `--merge` creates a merge commit, and `Closes #n` in the PR body or merge body closes the issue. The three-dot `origin/main...origin/feature/<slug>` diff is correct. The code, review, test and status validation lists agree: implemented → review; changes-requested/needs-fixes → code → implemented; reviewed → test. No secrets were added.

### Plan conformance
All 8 tasks in `tasks.md` appear in the diff. Nothing is out of scope. The `## Tests` items in `tasks.md` (consistency check, `/status` dry run) are left for `/test`. Findings 1, 2 and 3 are the "`/test` exception must not skip review" risk that `plan.md` already names.
