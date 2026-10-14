# Test report: WB-48 Edit own vocabulary word

**Verdict: Approved — merged into main (Run 3).** All suites green.

## Run 1 — 2026-10-14 (stopped, services down)

Branch `feature/WB-48_edit-own-vocabulary-word`, HEAD `253cce8`. `origin/main` already merged ("Already up to date").

| Check | Result |
| --- | --- |
| Merge guard 1 (review approved, `72b21d1` is ancestor) | ✅ |
| Merge guard 2 (only allowlisted paths after review) | ✅ |
| Content unit tests | ✅ 331 passed |
| Content integration tests (LocalDB) | ✅ 129 passed |
| E2E API (`EditVocabularyWordTests.cs`) | ⏸ Not run — port 5081 refused |
| E2E UI (`vocabulary-edit.feature`) | ⏸ Not run — UI not running |

Stale-build check: `PUT /api/vocabulary/<guid>` got no response (HTTP 000). `docker ps` shows only the kind control plane.

## To finish

1. Sam rebuilds and starts Content and the UI from this branch (`docker compose up -d --build`, ask-gated).
2. Unauthenticated `PUT /api/vocabulary/<guid>` returns 401, not 405.
3. `npx playwright install` in `e2e/ui` if not done.
4. Re-run `/test WB-48_edit-own-vocabulary-word`.


---

## Run 2 — 2026-10-14

Stack up (Content `PUT` unauthenticated → 401, UI → 200). HEAD `253cce8`, `origin/main` `d9203d8` already merged. Suites ran one after the other.

### Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, `72b21d1` ancestor of HEAD | ✅ |
| 2. Paths changed since review (vs reviewed code + main) | ✅ only `proposal.md`, `review.md` |
| 3. Every suite zero failures | ❌ E2E UI has failures (below) |

### Results

| Suite | Result |
| --- | --- |
| Content unit | ✅ 331 passed |
| Content integration | ✅ 129 passed (first run: 1 flaky `SupportLinkQueueTests` failure; passed alone and on full re-run) |
| E2E API (whole project, incl. `EditVocabularyWordTests`) | ✅ 68 passed |
| E2E UI (`npm test`, 48 scenarios) | ❌ 46–47 passed, 1–2 failed per run |
| E2E UI `vocabulary-edit.feature` | ✅ 2/2 passed (after test fix) |

### Test fix (mine)

`steps/vocabulary-edit.steps.ts`: in edit mode the word is an input value, so the card no longer matched `hasText`. The step now finds the form by `getByRole('form', { name: 'Edit <word>' })`. This was a test bug, not an app bug.

### Failures

| Test | Cause | WB-48? |
| --- | --- | --- |
| `support-links` › Admin completes an escalated unlink request | Backend needs `SupportLinks__UnlinkOverrideWaitDays=0`; compose runs with the default. Fails every run. | No |
| `learning-groups`, `vocabulary-challenges`, `vocabulary-review` (child) | Flaky: failed in some runs only, differ per run, also with `--workers=1` (learning-groups). | No |

### Verdict

Not merged — E2E UI suite not fully green (guard 3). All WB-48 scenarios pass. Fix: start Content with `SupportLinks__UnlinkOverrideWaitDays=0` and re-run `/test`, or Sam decides to accept the unrelated failures.


---

## Run 3 — 2026-10-14

HEAD `7437cbd`, `origin/main` already merged. Identity ran with `SupportLinks__UnlinkOverrideWaitDays=0` via a local compose override outside the repo. An earlier attempt put it on Content by mistake; that attempt made no commit. Suites ran one after the other.

### Merge guard

| Guard | Result |
| --- | --- |
| 1. Review Approve, `72b21d1` ancestor of HEAD | ✅ |
| 2. Paths changed since review (vs reviewed code + main) | ✅ `proposal.md`, `review.md`, `test-report.md`, `e2e/ui/steps/vocabulary-edit.steps.ts` |
| 3. Every suite zero failures | ✅ |

### Results

| Suite | Result |
| --- | --- |
| Content unit | ✅ 331 passed |
| Content integration | ✅ 129 passed |
| E2E API | ✅ 68 passed |
| E2E UI (`npm test`) | ✅ 48 passed |

### Failures

None.

### Verdict

Approved — merged into main
