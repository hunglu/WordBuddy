---
title: Release — ship what is on main
description: Stage 5 of the workflow. Tags main, deploys to local Kubernetes (kind), and writes release notes for every proposal at status done. Every push and deploy stays ask-gated.
---

## Context

Stage 5 of 5: **propose → plan → code → test → release**. Runs on `main` only, after one or more
proposals reached `status: done` (merged). Deployment target is local kind (see
`WordBuddy/CLAUDE.md` → Running in Docker / local Kubernetes).

Argument: `$ARGUMENTS` is an optional version (`v0.3.0`). If missing, propose the next minor
version from the latest `git tag` and ask Sam to confirm.

## Steps

1. **Validate.** `git branch --show-current` is `main`, working tree clean, and `main` is up to date
   with `origin/main`. Collect every `.claude/plans/*/proposal.md` with `status: done`. If none,
   stop — nothing to release.
2. **Release notes.** Write `docs/releases/<version>.md`: one bullet per proposal (title, type,
   issue `#n`, link to its folder), plus any `docs/adr/` added since the previous tag.
3. **Deploy (ask-gated, Sam approves each).** `make k8s-build-load` then `make k8s-apply`, and
   `make k8s-migrate SERVICE=<service>` for every service whose plan added a migration. Stop on
   the first failure — never retry with a different command to get around a gate.
4. **Smoke check.** `kubectl -n wordbuddy get pods` all `Running`; `GET /health` on each service
   via the ingress.
5. **Record.** Set each released proposal to `status: released` and add `released: <version>`.
   Commit (`release: <version>`), `git tag <version>`, then `git push origin main --tags`
   (ask-gated). If the proposal has an `issue`, remind Sam the issue closes via the release notes
   or close it with `gh issue close <n>`.
6. **Report** the version, what shipped, and the smoke-check result.
