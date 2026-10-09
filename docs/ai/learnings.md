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
