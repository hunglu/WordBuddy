# Test report: 2 navigation Items have selected state

Branch `feature/2-navigation-items-have-selected-state`, `origin/main` merged in (already up to date).

## Backend unit/integration

Not applicable. The change is UI-only (`WordBuddy.UI/src/layouts/AppLayout.tsx`) and touches no backend service.

## Frontend build

`WordBuddy.UI` `npm run build` passed. The only output was the existing warning about chunks larger than 500 kB.

## E2E API

Not applicable. The change touches no API surface.

## E2E UI

`e2e/ui` `npx bddgen && npx playwright test` against `http://localhost:3000`: **11 failed, 4 passed**. All 9 `navigation.feature` scenarios failed.

## Failures

Every failing scenario fails in the login step before it reaches any navigation assertion. After login submits, the page stays on `http://localhost:3000/login` (`expect(page).toHaveURL(/^https?:\/\/[^/]+\/$/)` times out after 5 s). The unrelated suites `login.feature`, `vocabulary.feature` and `theme.feature` fail the same way, so this is an environment problem, not a finding against this PR.

Diagnosis:
- `POST http://localhost:3000/api/auth/login` returns **404**. The same request straight to Identity (`http://localhost:5080/api/auth/login`) returns 400 (validation). Identity is up, but the running UI container's proxy does not reach it.
- The running `wordbuddy-ui` container has been up for 5 hours. It was not built from this branch, so it would not exercise the `AppLayout.tsx` fix even if login worked.

To get a valid run: rebuild and restart the UI container from this branch (`docker compose up --build wordbuddy-ui`, which is ask-gated), check that `/api/auth` routes through it, and make sure the seeded learner/admin test accounts exist. Then run `/test` again.

## Merge guard

1. Review round 2 verdict is Approve at reviewed commit `0b07bdb`, which is an ancestor of HEAD. Pass.
2. `git merge-tree --write-tree 0b07bdb origin/main` exited cleanly. `git diff --name-only <base> HEAD` lists only allowlisted files. Pass:
   - `.claude/plans/2-navigation-items-have-selected-state/proposal.md`
   - `.claude/plans/2-navigation-items-have-selected-state/review.md`
3. Every suite ran with zero failures: **Fail** (E2E UI, 11 failures).

## Verdict

Not merged: E2E UI suite failed because the environment can't log in (UI proxy returns 404 on /api/auth, and the UI container is stale). Re-run `/test` once the stack is rebuilt from this branch.

## Override (2026-10-01T17:33:55+07:00)

Sam chose to merge without a green E2E run. Merge guard check 3 was **not** met. The merge rests
on the passing frontend build and review round 2's approval. The 9 `navigation.feature`
scenarios have **never passed**. Run them on the next working stack; if they fail, open a new
`bugfix` proposal.
