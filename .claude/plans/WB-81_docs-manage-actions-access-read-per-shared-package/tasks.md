# Tasks: WB-81 docs: Manage Actions access Read per shared package

## Backend (docs and comments only)

- [x] `.github/workflows/images.yml` lines 3-5: replace the header comment with the plan §1 text — names "Manage Actions access" → `WordBuddy` (Read); no non-comment line changes. — images.yml header, comment only
- [x] `WordBuddy/README.md` after line 196: add the plan §2 paragraph — names the per-package step and "every new `WordBuddy.Shared.*` package". — WordBuddy/README.md
- [x] `WordBuddy/CLAUDE.md`: do not edit. Plan §4 holds the text; the coder repeats it in the PR body for Sam — file unchanged in the diff. — CLAUDE.md untouched; plan §4 text goes in PR body

## Frontend

None.

## Tests

- [x] Diff check: `git diff main --stat` lists only `images.yml`, `WordBuddy/README.md` and files under `.claude/plans/WB-81_*` — no `.csproj`, `src/`, workflow step or release note change. — stat: images.yml, README.md, WB-75 spec Pending line, plan folder only
- [x] `images.yml` comment-only check: `git diff main -- .github/workflows/images.yml` shows only lines starting with `#` — YAML behaviour unchanged. — only `#` lines changed
- [x] Wording check: `Manage Actions access` appears in `images.yml` and `WordBuddy/README.md`; "inherits its access" no longer appears in `images.yml`. — 1 hit each; 'inherits its access' gone
- [x] `docs/releases/v1.0.1.md` unchanged — `git diff main -- docs/releases/` is empty. — empty diff
- [-] (deferred to /test) Tester (at `/test`): update living spec `docs/features/WB-75_...md` line 21 with plan §3 text; move WB-81 from "Pending changes" to "Change history" — spec matches the merged state.
