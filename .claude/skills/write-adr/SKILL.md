---
name: write-adr
description: Writes a new WordBuddy Architecture Decision Record in docs/adr/ from the repo template. Use when Sam asks for an ADR, when a plan introduces a breaking change, a new service, a new external dependency, or changes an earlier ADR's decision. Never edits an accepted ADR — supersedes it instead.
disable-model-invocation: true
---

# Write an ADR

ADRs record **why** a decision was made. They are append-only (root `CLAUDE.md` → Where things
live).

```text
read ADRs + specs → pick next NNNN → draft from _template.md (status: proposed) → Sam accepts
                                    └─ supersedes old? → old ADR: only its Status line changes
```

## Steps

1. Read every `docs/adr/*.md` and the related `docs/features/*.md`. Stop and say so if an existing
   ADR already covers the decision.
2. Next number = highest `NNNN` + 1, zero-padded. File: `docs/adr/NNNN-<kebab-title>.md`.
3. Copy `docs/adr/_template.md`. Fill it:
   - `Status: proposed` — only Sam sets `accepted`.
   - `Date:` from the clock (`date +%Y-%m-%d`), never typed.
   - **Context** — forces and constraints, 3–6 bullets.
   - **Decision** — what we do, in active voice. Add a Mermaid diagram if 3+ components interact.
   - **Consequences** — good and bad, plus the **Child vs adult** impact when users are affected.
   - **Alternatives considered** — a table: option · why rejected.
4. Superseding: in the new ADR add `- Supersedes: NNNN`; in the old ADR change **only** its
   status line to `superseded by NNNN`. Nothing else in an accepted ADR changes.
5. Link the ADR from the proposal / plan that needed it, if any.
6. Follow `.claude/rules/writing-style.md`. Never write into a `CLAUDE.md`.

## Done when

- One new file under `docs/adr/`, status `proposed`, all template sections filled.
- At most one other ADR changed, and only its status line.
