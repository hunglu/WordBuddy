# Learnings

Lessons Claude picked up while running the workflow. **Append-only for Claude**; Sam promotes or
deletes entries (see `docs/sdlc/workflow.md` → Learnings).

```text
/review, /test ──append──► this file ──(Sam, monthly PR)──► CLAUDE.md / .claude/rules/ / skills
                                                    └────► delete (one-off, obsolete)
```

## Format

One bullet per lesson, newest last. One line, generic (not a run log), no secrets or user data.

```text
- YYYY-MM-DD `<slug>` (<stage>) — <lesson> → suggested home: <CLAUDE.md | rules/x.md | skill y | none>
```

## Entries

- 2026-10-08 `WB-24_learner-support-links` (test) — MassTransit `ConfigureEndpoints` names queues after the consumer class, so two services with same-named consumers share one queue and each event reaches only one of them; prefix endpoint names per service and check with `rabbitmqctl list_queues name consumers` → suggested home: WordBuddy/CLAUDE.md (Messaging)
- 2026-10-09 `WB-25_vocabulary-autofill` (test) — when a feature's happy path depends on a paid external API with no key in the local stack, the plan must name a keyless E2E path (seed endpoint, fake-client switch, or catalog fixture) or the API E2E cannot go green → suggested home: .claude/agents/planner.md (Tests)
- 2026-10-10 `WB-27_learning-groups` (test) — an endpoint cached with an absolute TTL and no invalidation makes a polling E2E check read its own first stale answer; vary a cache-key part per poll (here the client UTC offset) or the test waits for the TTL → suggested home: .claude/agents/tester.md (E2E API)
- 2026-10-13 `WB-20_create-user-profile-settings` (review) — keying a React form on the value it saves remounts it on success, so the mutation `isSuccess` message never shows; key forms on a stable id and reset values instead → suggested home: WordBuddy.UI/CLAUDE.md (Conventions)
- 2026-10-13 `WB-20_create-user-profile-settings` (test) — playwright-bdd step text is global across all step files; a generic phrase like "the message {string} is shown" collides with an older file and fails the whole run, so prefix new step text with the feature name → suggested home: .claude/agents/tester.md (E2E UI)
- 2026-10-14 `WB-49_personal-context-on-learner-word` (review) — the repo has no `.gitattributes` and mixes LF and CRLF files, so an editor save can flip a whole file's line endings (recurring in about 10 reviews); before committing, check that `git diff --shortstat main` equals the same with `--ignore-cr-at-eol` and keep each file's existing EOL → suggested home: .claude/agents/coder.md (Commit)
- 2026-10-14 `WB-50_lexeme-admin-view` (review) — a new upload endpoint must fit the body limit at every hop: UI `nginx.conf` location (1 MB default), ingress-nginx `proxy-body-size` (1 MB default, per Ingress, so it applies to all its paths) and the Kestrel `[RequestSizeLimit]`; integration tests bypass the proxies, so the plan must list these infra edits → suggested home: .claude/agents/planner.md (Infra)
- 2026-10-15 `WB-55_apply-paging-to-table-list` (review) — `placeholderData: keepPreviousData` on a query whose key holds an entity id (learner, group) shows the previous entity's rows while the new one loads; keep previous data only when the id part of the key is unchanged (function form comparing `prevQuery.queryKey`) → suggested home: WordBuddy.UI/CLAUDE.md (Server state)
- 2026-10-15 `WB-56_moderator-role-to-assist-admin` (review) — when an admin-only action moves to a lesser role, check self-dealing: the new actor may be a party to the case (own shared word, own support link), which admin-only code never had to guard → suggested home: .claude/agents/planner.md (Security)
- 2026-10-16 `WB-56_moderator-role-to-assist-admin` (test) — a UI e2e that expects a fresh learner on page 1 of a ranked, paged board breaks once the shared test database holds more than one page of e2e users; the step must follow "Next" until its row shows → suggested home: .claude/agents/tester.md (E2E UI)
- 2026-10-17 `WB-59_multilingual-ui-english-and-vietnamese` (review) — a hook that uses TanStack Query (`useQueryClient`/`useMutation`) called in `App` itself runs above `<QueryClientProvider>` and throws on every load; `tsc`/`vite build` cannot catch it, so put app-wide hooks in a child component inside the provider and smoke-load the app after such edits → suggested home: WordBuddy.UI/CLAUDE.md (Server state)
- 2026-11-02 `WB-61_adopt-net-aspire-for-local-orchestration` (test) — A local `appsettings.*.json` value for an OTel endpoint beats the `OTEL_EXPORTER_OTLP_ENDPOINT` env var that an orchestrator injects; set the config key as an env var in the orchestrator too → suggested home: none
- 2026-11-04 `WB-68_grpc-for-internal-service-calls` (test) — Run backend integration suites and the Aspire suite one after the other, not in parallel: a parallel run made a RabbitMQ-based Content integration test fail once and earlier gave 2 Aspire failures (port clash with a running compose stack); stop compose first → suggested home: .claude/agents/tester.md
- 2026-11-04 `WB-68_grpc-for-internal-service-calls` (test) — `e2e/api` autofill tests need the Content container started with `Autofill__UseFakeClients=true`; plain `docker compose up` fails them with `autofillUnavailable`. Use a compose override file, not an edit to `docker-compose.yml` → suggested home: e2e/api/WordBuddy.E2E.Api.Tests/README.md
