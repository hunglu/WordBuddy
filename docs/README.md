# WordBuddy docs

| Folder | What lives here | Mutability |
|---|---|---|
| `product/roadmap.md` | Themes and priorities — the *why* behind the backlog | edited freely |
| `features/<feature>.md` | **Living spec** — how a feature behaves *today* | updated by `/test` on every merge |
| `adr/NNNN-*.md` | Architecture Decision Records | append-only; supersede, never rewrite |
| `sdlc/` | How we work: workflow, GitHub integration | edited freely |
| `releases/<version>.md` | Release notes, written by `/release` | immutable once tagged |

Change records (one folder per change, frozen once `done`) live in `../.claude/plans/<slug>/`.
Rule of thumb: **`docs/features` = current state, `.claude/plans` = history of changes.**
