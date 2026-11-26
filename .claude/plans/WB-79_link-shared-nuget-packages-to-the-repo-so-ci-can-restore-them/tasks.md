# Tasks: WB-79 Link shared NuGet packages to the repo so CI can restore them

## Backend — shared packages

- [x] Add `WordBuddy/src/Shared/Directory.Build.props` with `RepositoryUrl` `https://github.com/hunglu/WordBuddy`, `RepositoryType` `git`, `PublishRepositoryUrl` `true` (plan §1) — no per-csproj copy of these properties. — WordBuddy/src/Shared/Directory.Build.props
- [x] Bump `Version` in the 5 shared csprojs: Kernel 1.1.1, Application 1.0.1, Infrastructure 1.4.1, Contracts 1.4.1, Protos 1.0.1 — no other csproj change. — 5 Shared csprojs
- [x] `WordBuddy.Shared.Application.csproj`: dependency `WordBuddy.Shared.Kernel` 1.0.1 → 1.1.1 — `dotnet build src/Shared/WordBuddy.Shared.slnx` green. — Shared.Application.csproj; Shared slnx builds 0 errors
- [x] Run `make pack-shared`; open each of the 5 new `.nupkg` and read its `.nuspec` — 5/5 contain `<repository type="git" url="https://github.com/hunglu/WordBuddy"`. — 5/5 nuspecs verified (local-nuget-feed, gitignored)

## Backend — service references

- [x] Identity csprojs → Kernel 1.1.1, Application 1.0.1, Infrastructure 1.4.1, Contracts 1.4.1 — `dotnet build` of `WordBuddy.Identity.slnx` green. — Identity csprojs; build green
- [x] Content csprojs → Kernel 1.1.1, Application 1.0.1, Infrastructure 1.4.1, Contracts 1.3.0 → 1.4.1, Protos 1.0.1 — `WordBuddy.Content.slnx` builds green. — Content csprojs; build green
- [x] Quiz csprojs → Kernel 1.0.1 → 1.1.1, Application 1.0.1, Infrastructure 1.4.1 — `WordBuddy.Quiz.slnx` builds green; if code changes are needed, stop and report (plan Open question 2). — Quiz csprojs; build green, no code change
- [x] Progress csprojs → Kernel 1.1.1, Application 1.0.1, Infrastructure 1.4.1, Contracts 1.4.1, Protos 1.0.1 — `WordBuddy.Progress.slnx` builds green. — Progress csprojs; build green
- [x] Notification Api csproj → Infrastructure 1.0.2 → 1.4.1 — `WordBuddy.Notification.slnx` builds green; if code changes are needed, stop and report. — Notification.Api csproj; build green, no code change
- [x] Grep `Include="WordBuddy.Shared.` across `WordBuddy/src` — only the 5 new versions remain. — only new versions remain

## Publish (ask-gated, Sam)

- [ ] **Ask-gated:** Sam runs (or approves) `make publish-shared` with a PAT that has `write:packages`. Needed before any Docker or CI build can restore the new versions — 5 new versions visible on `github.com/hunglu?tab=packages`.
- [ ] Sam checks each of the 5 package pages — "Repository" shows `hunglu/WordBuddy`; "Inherit access from source repository" is on (turn on if not). Record the result in the test report.

## CI / docs

- [x] `.github/workflows/images.yml` header comment: replace the "Actions access" prerequisite with the linked-repository wording (plan §5) — comment only, no step or action change. — .github/workflows/images.yml
- [x] `WordBuddy/README.md` shared-package note: current `Infrastructure` version and one line on `RepositoryUrl` + `make publish-shared` before a release tag — no PAT value, no NAS mention. — WordBuddy/README.md
- [x] Do not edit `WordBuddy/CLAUDE.md`. Leave the proposed text (plan §6) for Sam — file unchanged in the PR. — unchanged

## Tests

- [x] `dotnet test` for Identity, Content, Quiz, Progress, Notification slnx on SQL Server (`(localdb)\mssqllocaldb`) — all green. — Identity 262+66, Content 481+235, Quiz 17+10, Progress 472+106 pass; Notification has no tests
- [ ] Same 4 data-service suites with `WORDBUDDY_TEST_DB_PROVIDER=Postgres` — all green.
- [ ] Docker restore from GitHub only (after publish): build one service image, e.g. `docker build --secret id=github_token,env=GITHUB_TOKEN WordBuddy/src/Services/Content` (Dockerfile removes the local feed) — restore succeeds; ask-gated if `docker compose`/`make up` is used.
- [ ] kind path still restores: `make k8s-build-load` (or note skipped with reason in the test report) — 5 API images build.
- [ ] Tester: update living spec `docs/features/WB-75_...md` (line 21 packages linked + `GITHUB_TOKEN`; line 25 `Infrastructure` 1.4.1; WB-79 to "Change history") — spec matches the merged state.
- [ ] Release check (done in `/release v1.0.1`, ask-gated tag): `images.yml` run on `v1.0.1` — 6/6 matrix jobs green, GHCR shows `1.0.1` for all 6 images; then Sam applies the CLAUDE.md text from plan §6.
