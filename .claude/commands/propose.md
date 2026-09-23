---
title: Propose — capture a business idea
description: Captures a business proposal from Sam as the "idea" stage of the idea → plan → code → test workflow. No analysis, no planning — just structured capture.
status: draft
---

## Context

This is stage 1 of 4 in the WordBuddy workflow: **propose → plan → code → test**. Each stage is
its own command; state lives in `.claude/plans/<slug>/` as plain markdown so it's visible and
diffable in git.

This command only *captures* the idea. Do not analyze feasibility, do not sketch an architecture,
do not touch any application code. That is `/plan`'s job, and it only runs when Sam explicitly
invokes it next.

Argument: `$ARGUMENTS` is the proposal title (and optionally a rough description) Sam typed after
`/propose`. If it's empty or too thin to work with, ask Sam directly for a title before doing
anything else.

## Steps

1. **Derive the slug.** Kebab-case the title (lowercase, spaces → hyphens, strip punctuation). If
   `.claude/plans/<slug>/` already exists, stop and tell Sam — don't silently overwrite an
   existing proposal. Suggest either a different title or running `/plan <slug>` if they meant to
   continue that existing one.

2. **Fill out the proposal.** Ask Sam (in one pass, not one question at a time) for whatever of
   this isn't already in their message:
   - Problem / motivation — what's broken or missing today
   - Goal — what success looks like
   - Target users — children, adults, or both (this matters a lot in WordBuddy: child accounts
     have content restrictions enforced via authorization policies)
   - Success criteria — how we'd know it worked
   - Constraints — deadline, dependencies, anything explicitly out of scope

   If Sam gives a terse one-liner and says "that's enough, go", don't push back — capture what
   you have and leave the rest as `_Not specified._` in the template. This command must not block
   on completeness.

3. **Write `.claude/plans/<slug>/proposal.md`**:

   ```markdown
   ---
   title: <title>
   status: idea
   created: <YYYY-MM-DD>
   ---

   ## Problem

   <problem>

   ## Goal

   <goal>

   ## Target users

   <children / adults / both>

   ## Success criteria

   <criteria>

   ## Constraints

   <constraints, or "_Not specified._">
   ```

4. **Report back** with the slug and file path, and tell Sam the next step is
   `/plan <slug>` — do not start planning yourself.

## Verification

- `.claude/plans/<slug>/proposal.md` exists with `status: idea` in its frontmatter.
- No files outside `.claude/plans/<slug>/` were touched.
