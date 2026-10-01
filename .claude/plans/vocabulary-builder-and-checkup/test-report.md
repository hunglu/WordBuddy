# Test report: Vocabulary Builder & Recall Check-up

Verification-only run on `main` (2026-10-01). The code landed on `main` in 2778add, before the PR
stage existed, so there is no feature branch and no PR. Tests ran against the live docker compose
stack.

## Backend unit/integration

- `WordBuddy.Content.UnitTests`: 26 passed, 0 failed
- `WordBuddy.Progress.UnitTests`: 10 passed, 0 failed
- `WordBuddy.Content.IntegrationTests`: 4 passed, 0 failed (after a test-only fix, see below)
- `WordBuddy.Progress.IntegrationTests`: 2 passed, 0 failed

The integration tests ran against the docker SQL Server (`localhost,1433`), each in a new
throwaway DB (`WordBuddyContentTests_<ts>` / `WordBuddyProgressTests_<ts>`). They got their
connection strings from `CONTENT_TEST_CONNECTION_STRING` / `PROGRESS_TEST_CONNECTION_STRING`.
The SA password came from the container's environment and was never printed.

Test-only fix: `PersonalVocabularyEndpointsTests.AddShareModerateApprove_WordAppearsInSharedPool`
first failed with `JsonException ... Path: $[0].shareStatus`. The API sends enums as strings
(`JsonStringEnumConverter`), but the test deserialized with the default options. I added a
`JsonOptions` with `JsonStringEnumConverter` to the test class. No application code changed.

Migrations in the running DBs (read-only check of `__EFMigrationsHistory`): Content has
`20260924082936_AddPersonalVocabularyWords` and Progress has `20260924084632_AddVocabularyRecall`.
Both were already applied, so no `dotnet ef database update` was needed.

## E2E API

`dotnet test e2e/api/WordBuddy.E2E.Api.Tests`: 3 passed, 4 failed (7 total)

- `PersonalVocabularyTests`: 3 of 3 passed (add/share/moderate/shared pool, non-admin 403 on
  moderation, child 403 on share)
- `HealthCheckTests.GetHealth_ServiceIsRunning_ReturnsOk`: failed for Identity, Content, Quiz
  and Progress. This is an older defect, not caused by this feature (see Failures).

## E2E UI

`npx bddgen && npx playwright test`: 4 passed, 11 failed (15 total)

All 11 failures happen at login. One of them is
`vocabulary.feature › Learner adds a word, sees it in their list, and completes a recall check`.
The others are `login.feature › Successful login`, all 9 scenarios in `navigation.feature`, and
`theme.feature › Lessons page has no CSS transitions`. Because login fails, the vocabulary UI flow
itself was never exercised.

## Failures

1. **UI Nginx proxy drops the request path (blocks every login).** Defect in application code,
   not caused by this feature, and not fixed here. `WordBuddy.UI/nginx.conf` uses
   `proxy_pass $identity_upstream/api/auth/;` (and the same pattern for lessons, media, quiz and
   progress). When `proxy_pass` contains variables and a URI, nginx replaces the whole request
   URI with that URI. So `POST /api/auth/login` reaches Identity as `POST /api/auth/`, which
   returns 404 with an empty body. I reproduced this: `POST :3000/api/auth/login` gives 404 with
   empty body, the same as `POST :5080/api/auth/`. `POST :5080/api/auth/login` directly gives
   400 `Login.InvalidCredentials`, which is correct for bad credentials. Likely fix: drop the URI
   part (`proxy_pass $identity_upstream;`) so the original URI is passed through unchanged. This
   needs `/code`, then a container rebuild.
2. **No `/health` endpoint on any service.** `GET /health` returns 404 on ports 5080 to 5084.
   No service code under `WordBuddy/src` maps health checks at all, even though
   `WordBuddy/CLAUDE.md` requires `/health` and `/health/ready`. This is an older gap and not
   part of this feature.

## Verdict

Not merged — 2 of 4 suites failed (E2E UI: 11 failures, all blocked at login by the nginx proxy
defect, so the vocabulary UI flow is unverified; E2E API: 4 `HealthCheckTests` failures). Nothing
needed merging, since the code is already on `main`. All of the feature's own backend and API
tests pass.
