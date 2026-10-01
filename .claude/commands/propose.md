---
title: Propose — capture a business idea
description: Captures a business proposal from Sam as the "idea" stage of the propose → plan → code → review → test → release workflow. No analysis, no planning — just structured capture.
status: draft
---

## Context

This is stage 1 of 6 in the WordBuddy workflow: **propose → plan → code → review → test → release**. Each stage is
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
   - Type — `new` feature, `change` to an existing one, or `bugfix`. For `change`, which
     existing slug / `docs/features/*.md` it modifies. Never edit a `done` proposal — a change
     always gets its own new folder.
   - GitHub issue number, if the idea came from the Kanban board (see
     `docs/sdlc/github-integration.md`). If `$ARGUMENTS` starts with `#<n>`, that is the issue:
     run `gh issue view <n> --json title,body,labels,state` and fill the fields from it before asking
     Sam anything. If `gh` is missing or not authenticated, say so and continue with what Sam typed.
     **One issue ↔ one proposal:** before going further, check that no existing
     `.claude/plans/*/proposal.md` already has `issue: <n>`. If one does, stop and tell Sam which
     slug owns it (suggest `/plan <that-slug>` or a `type: change` proposal) — never create a
     second folder for the same issue. If the issue is closed, say so and ask before continuing.

   If Sam gives a terse one-liner and says "that's enough, go", don't push back — capture what
   you have and leave the rest as `_Not specified._` in the template. This command must not block
   on completeness.

3. **Write `.claude/plans/<slug>/proposal.md`**:

   ```markdown
   ---
   title: <title>
   status: idea
   type: <new | change | bugfix>
   issue: <GitHub issue number, e.g. 42 — or "none">
   pr: none            # set by /code when it opens the pull request
   affects: <docs/features/<feature>.md paths this changes, comma-separated — or "none">
   supersedes: <slug of an earlier proposal this changes — or "none">
   version: 1.0
   created: <YYYY-MM-DDTHH:MM:SS±HH:MM, local time, e.g. 2026-09-30T23:00:44+07:00>
   updated: <same as created>
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

4. **Living spec (`docs/features/`).** Every proposal points at one or more living specs via
   `affects:`:
   - `type: new` → `affects: docs/features/<slug>.md`. Create that file from
     `docs/features/_template.md` with `state: proposed`, `last-updated-by: <slug>`, the title,
     and "What it does" filled from the proposal's Goal (marked _Proposed — not on main yet_).
     Add a row to `docs/features/README.md` with status `proposed`.
   - `type: change` / `bugfix` → `affects:` names the existing spec(s). Don't rewrite them; append
     one line under their `## Pending changes` section:
     `- <slug> — <one-line goal> (#<issue>)`. If no spec exists yet for that feature, create one
     as for `new` and say so.
   The tester turns `proposed` into `shipped` (and removes the pending line) when it merges.

5. **GitHub issue — link first, create only if nothing matches.** Issues arrive two ways: typed
   on the GitHub website (then `/propose #<n>`), or created here. Never end up with two.

   a. **`#<n>` given** → link only (step 2 already checked it). Never run `gh issue create`.

   b. **No `#<n>`** → search before creating (read-only, not gated):
      ```bash
      gh issue list -R hunglu/WordBuddy --state all --limit 20 \
        --search "wordbuddy-slug:<slug> in:body" --json number,title,state,url
      gh issue list -R hunglu/WordBuddy --state open --limit 20 \
        --search "<title> in:title" --json number,title,state,url
      ```
      - The first search finds issues this command created before (the marker below) — an exact
        match. If found, link it (`issue: <n>`) and **don't** create.
      - The second finds website-created issues with a similar title. If any look like the same
        idea, list them and ask Sam: link one (`issue: <n>`, then the step-2 ownership check) or
        create a new one. Don't decide for him.
      - Only when both searches are empty (or Sam says "new"), create:
      ```bash
      gh issue create -R hunglu/WordBuddy --title "<title>" --label "type:<type>" \
        --body-file <tmp body> --project "WordBuddy"
      ```
      The body is `Plan: .claude/plans/<slug>/`, a blank line, the proposal's Problem/Goal/
      Success criteria, and a last line `<!-- wordbuddy-slug: <slug> -->` (the dedup marker —
      always include it). Write the body to a temp file in the scratchpad, not the repo.

   c. `gh issue create` is ask-gated — Sam approves it. Put the number in `issue:` (part of the
      creation edit, no version bump). If `--project` fails (board not set up), retry once
      without it and tell Sam. If `gh` is missing, unauthenticated, or Sam declines, write
      `issue: pending`, say why, and continue — never block the proposal on GitHub.

   d. **Linking a website issue** (a, or b's "link"): if its body lacks the marker, append
      `Plan: .claude/plans/<slug>/` and `<!-- wordbuddy-slug: <slug> -->` with
      `gh issue edit <n> --body-file` (ask-gated), so later runs find it by marker.

   To link an `issue: pending` proposal later, re-run the searches in b by hand (or ask Claude);
   `/status` lists pending ones so they aren't forgotten.

6. **Report back** with the slug, file paths, and the issue link, and tell Sam the next step is
   `/plan <slug>` — do not start planning yourself.

## Verification

- `.claude/plans/<slug>/proposal.md` exists with `status: idea`, `version: 1.0`, and `created` = `updated` = the current local timestamp (get it with
  `date +%Y-%m-%dT%H:%M:%S%:z` — never guess the time).
- `issue:` is a number, or `pending` with the reason reported.
- That issue number appears in exactly one `.claude/plans/*/proposal.md`, and its body carries
  `<!-- wordbuddy-slug: <slug> -->`.
- `gh issue create` ran at most once, and only after both searches came back empty or Sam chose "new".
- Every path in `affects:` exists under `docs/features/` (new stub or a `## Pending changes` line).
- Nothing outside `.claude/plans/<slug>/` and `docs/features/` was touched.
