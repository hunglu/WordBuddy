---
title: WB-79_Link shared NuGet packages to the repo so CI can restore them
status: released
type: bugfix
issue: 79
pr: 80
released: v1.0.1
affects: docs/features/WB-75_production-deployment-single-host-docker-compose-and-kubernetes.md
supersedes: none
version: 4.0
created: 2026-11-26T00:25:05+07:00
updated: 2026-11-27T21:59:20+07:00
---

## Problem

`images.yml` (WB-75) cannot build the 5 API images for v1.0.0. Only the UI image is built.

```
images.yml (GITHUB_TOKEN) ──restore──► nuget.pkg.github.com/hunglu/WordBuddy.Shared.*  →  403
```

- The 5 `WordBuddy.Shared.*` packages (Kernel, Infrastructure, Contracts, Application, Protos) are private and not linked to any repository.
- Granting Actions access by hand in the package settings did not fix it (run 45660741954).

## Goal

Link the 5 shared packages to `hunglu/WordBuddy`. CI then restores them with `GITHUB_TOKEN`, with no stored secret.

## Target users

Both. No behaviour change for learners; this is a build and release fix.

## Success criteria

- The new versions of all 5 packages show `hunglu/WordBuddy` as their repository.
- The services reference the new versions; local, Docker and kind builds still restore.
- `images.yml` on a new tag (`v1.0.1`) builds and pushes all 6 images to GHCR.
- All unit and integration suites stay green on both providers.

## Constraints

- Option A from the v1.0.0 release: no PAT stored as a repository secret.
- Keep the service independence model (ADR 0001): packages only, no project references.
- Publishing the packages (`make publish-shared`) and the new tag stay ask-gated.
- Out of scope: other WB-75 follow-ups (PII in `UserRepository` logs, PR CI workflow, Dependabot).
