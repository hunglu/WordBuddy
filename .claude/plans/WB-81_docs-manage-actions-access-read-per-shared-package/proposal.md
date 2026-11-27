---
title: WB-81_docs: Manage Actions access Read per shared package
status: implemented
type: bugfix
issue: 81
pr: none
affects: docs/features/WB-75_production-deployment-single-host-docker-compose-and-kubernetes.md
supersedes: none
version: 3.0
created: 2026-11-27T23:27:48+07:00
updated: 2026-11-27T23:31:50+07:00
---

## Problem

The WB-79 docs say that a linked repository plus "Inherit access" lets `images.yml` restore the shared packages with `GITHUB_TOKEN`. That was not enough.

```
linked + Inherit access                         → 403 (run 45852932122)
+ Manage Actions access → WordBuddy (Read)      → 6/6 green (runs 45862707216, 46025041915)
```

A new `WordBuddy.Shared.*` package, or a reader who follows the docs, hits the same 403.

## Goal

The docs state the per-package step: package settings → "Manage Actions access" → add `WordBuddy` with role Read.

## Target users

Both. No behaviour change for learners; this is a docs fix for the build and release path.

## Success criteria

- The `.github/workflows/images.yml` header comment names the step.
- The `WordBuddy/README.md` shared-package note names the step.
- The WB-75 living spec "Images" line names the step.
- The v1.0.1 known issue is resolved by this change (release notes stay immutable).
- No workflow step, package version or application code changes.

## Constraints

- Docs and comments only.
- `WordBuddy/CLAUDE.md` is ask-gated: propose the text for Sam, do not edit it.
- `docs/releases/v1.0.1.md` is immutable once tagged; do not edit it.
- Out of scope: other v1.0.1 known issues (PII in logs, nginx 502 on kind, `sa` generator, Dependabot).
