<!-- owner: Sam. Stable, project-wide rules only. Edits are ask-gated (.claude/settings.json).
     Do NOT record run output, progress, or "what was done" here — that belongs in
     .claude/plans/<slug>/, docs/, or git history. -->

# WordBuddy

English learning web app for children and adults — vocabulary lessons, grammar lessons, daily
phrases, and quizzes, delivered as text, images, audio pronunciations, and video clips.

| Part | Folder | Specific rules |
| --- | --- | --- |
| Backend — 5 independent .NET microservices | `./WordBuddy` | `WordBuddy/CLAUDE.md` |
| Frontend — React + TypeScript SPA | `./WordBuddy.UI` | `WordBuddy.UI/CLAUDE.md` |
| Cross-service E2E tests (API + UI) | `./e2e` | each project's `README.md` |

Versions are not repeated here — the source of truth is `WordBuddy/global.json` + `*.csproj` and
`WordBuddy.UI/package.json`.

@./WordBuddy/CLAUDE.md
@./WordBuddy.UI/CLAUDE.md

## Domain concepts

| Concept | Description | Owning service |
| --- | --- | --- |
| `User` | Learner or admin account with profile, role, `AgeGroup` (Child/Adult) | Identity |
| `Lesson` | A structured learning unit (vocabulary or grammar) | Content |
| `Vocabulary` | A word entry with definition, examples, and media | Content |
| `Grammar` | A grammar rule with explanations and examples | Content |
| `DailyPhrase` | A phrase surfaced to learners each day | Content |
| `MediaAsset` | Text, image, audio, or video attached to any domain object | Content |
| `Quiz` | A set of questions testing lesson content | Quiz |
| `LearnerProgress` | A user's completion and score across lessons and quizzes | Progress |

Child accounts (`AgeGroup = Child`) get extra content restrictions everywhere — backend
authorization policies and UI alike. Every feature must state its child vs. adult behaviour.

## Where things live

| What | Where | Mutability |
| --- | --- | --- |
| Stable rules for Claude | `CLAUDE.md` files (this + per-folder) | rarely; ask-gated |
| Current behaviour of each feature (living spec) | `docs/features/<feature>.md` | updated on every merge |
| Architecture decisions | `docs/adr/NNNN-*.md` | append-only |
| Roadmap / themes | `docs/product/roadmap.md` | freely |
| How we work (workflow, GitHub setup) | `docs/sdlc/` | freely |
| One change = one folder (proposal → plan → tasks → test report) | `.claude/plans/<slug>/` | frozen once `done` |
| Release notes | `docs/releases/<version>.md` | immutable once tagged |
| Backlog & Kanban | GitHub Issues + Project `WordBuddy` | — |

Output of any workflow run is written into its `.claude/plans/<slug>/` folder — never into a
`CLAUDE.md`.

## Workflow: propose → plan → code → test → release

Full description: `docs/sdlc/workflow.md`. GitHub integration: `docs/sdlc/github-integration.md`.

| Command | Agent | Result (`status:` in `proposal.md`) |
| --- | --- | --- |
| `/status` | — | read-only board of all proposals + next step |
| `/propose [#issue] <title>` | — | `proposal.md` → `idea` |
| `/plan <slug>` | `planner` | `plan.md` + `tasks.md` → `planned` — **approval gate** |
| `/code <slug>` | `coder` | `feature/<slug>` branch, one commit per task → `implemented` |
| `/test <slug>` | `tester` | tests + `test-report.md`, living spec updated, `--no-ff` merge on green → `done` / `needs-fixes` |
| `/release [version]` | — | release notes, tag, kind deploy → `released` |

- Changing a shipped feature = a **new** proposal (`type: change`, `affects:`, `supersedes:`).
  Never edit a `done` plan folder.
- Git: feature work only on `feature/<slug>`, never directly on `main`. Only the tester merges
  into `main`, only on a green run. Every `git push` is ask-gated; force-push is denied.

## Project-wide rules

- SOLID; small, focused changes that read like the surrounding code.
- No secrets in git or in output: connection strings, JWT keys, Auth0 credentials, storage keys,
  `.env`, `appsettings.Production.json`. Locally use `dotnet user-secrets` / env vars.
- Never log or display passwords, tokens, PII, or child user data.
- Anything under `ask` in `.claude/settings.json` stops and waits for Sam; never work around it.
  See `.claude/rules/safety-guardrails.md`.

## Local overrides

If `./docs/local-overrides.md` does not exist, tell the user explicitly:
"No local-overrides.md found — create one from docs/local-overrides.md.example if you need
personal config."
@./docs/local-overrides.md
