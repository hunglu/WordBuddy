# WordBuddy docs

**Rule of thumb:** `docs/features` = current state; `.claude/plans` = history of changes.

| Path | Content | Mutability |
| --- | --- | --- |
| `architecture.md` | System diagrams (Mermaid): services, request flows | update when topology changes |
| `product/roadmap.md` | Themes and priorities — the rationale behind the backlog | edit freely |
| `features/<feature>.md` | **Living spec** — how a feature behaves on `main` today | updated by `/test` on every merge |
| `adr/NNNN-*.md` | Architecture Decision Records | append-only; supersede, never rewrite |
| `sdlc/` | How we work: workflow, GitHub integration | edit freely |
| `releases/<version>.md` | Release notes, written by `/release` | immutable once tagged |

```text
change request ─► .claude/plans/<slug>/   (proposal → plan → tasks → review → test-report; frozen once done)
                        │ merge
                        ▼
                 docs/features/<feature>.md   (current behaviour)
```

Writing style for every doc: `.claude/rules/writing-style.md`.
