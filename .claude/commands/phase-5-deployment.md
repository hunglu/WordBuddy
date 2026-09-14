---
title: Phase 5 — Deployment Pipeline
description: GitHub Actions CI/CD → publish shared NuGet packages, build & push per-service images, roll out to local Kubernetes (kind)
status: draft
---

## Context

Two repos: github.com/hunglu/WordBuddy (backend — 5 independent microservices in one monorepo
for now), github.com/hunglu/WordBuddy.UI (frontend). Target (Phase 4): a **local** `kind`
cluster — no cloud cluster exists yet. A GitHub-hosted runner has no network path to a cluster
running on your machine, so CI **cannot** `kubectl apply` to it; CI's job is build → test →
(if shared libs changed) publish NuGet packages → build & push per-service Docker images to
`ghcr.io`. Rolling images onto the local cluster is a `make k8s-apply SERVICE=<name>` step you
run yourself. Once a real, reachable cluster exists later, the deploy step becomes genuine
`kubectl` — this pipeline is written so that swap is the only thing that changes.

Two independent things get path-filtered in this monorepo:
- **Shared library changes** (`src/Shared/**`) → pack + push to **GitHub Packages NuGet feed**
  (`nuget.pkg.github.com`) — the feed every service's Docker build restores from (Phase 4).
- **Per-service changes** (`src/Services/<Service>/**`) → build & push that service's **Docker
  image** to `ghcr.io` (container images, a different GitHub Packages namespace than the NuGet
  feed, but the same underlying registry/auth).

## Requirements

- [ ] `publish-shared-packages` job: on a push to main touching `src/Shared/**`, pack and push
      `WordBuddy.Shared.Kernel`/`Infrastructure`/`Contracts` to the GitHub Packages NuGet feed
- [ ] One `build-and-push-<service>` job per service, path-filtered on that service's folder
      (rebuilding also if `publish-shared-packages` just ran, since a shared version bump
      affects every consumer) — builds using the BuildKit secret from Phase 4, pushes to
      `ghcr.io`
- [ ] GitHub Actions workflow for frontend: build + build & push image to `ghcr.io`
- [ ] Repo `permissions: packages: write` set so the workflow's own `GITHUB_TOKEN` can push both
      NuGet packages and container images — no separate PAT needed for CI
- [ ] Health check endpoint on every service: `GET /api/health`
- [ ] Smoke test per changed service: run the freshly-built image standalone (`docker run`) and
      curl `/api/health`
- [ ] Local rollout: `make k8s-apply SERVICE=<name>` documented to roll the kind cluster onto
      the newly-pushed image tag
- [ ] Rollback strategy: `kubectl rollout undo`, per service
- [ ] Branch protection on main: require CI to pass before merge
- [ ] Root `CLAUDE.md` + `README.md`, and each service's own `README.md`, updated with CI/CD docs

## Implementation Plan

### Step 1 — Health check endpoint (every service)

In every `<Service>.Api`, add a `HealthController`:
- `GET /api/health` → 200 OK with `{ status: "healthy", service: "<service>", timestamp: utcNow }`
- No `[Authorize]` — must be public
- Log Info on each health check call
- Wire into every `<service>-deployment.yaml`'s `livenessProbe`/`readinessProbe` (Phase 4),
  replacing the placeholder `/swagger/index.html` probe

### Step 2 — Publish shared NuGet packages (path-filtered)

In `.github/workflows/ci-cd.yml` (backend repo), a `publish-shared-packages` job, gated by a
paths filter on `src/Shared/**`, only on push to main:
- checkout, setup dotnet 10
- `dotnet pack src/Shared/WordBuddy.Shared.Kernel -c Release -o ./nupkgs` (and the other two)
- `dotnet nuget push ./nupkgs/*.nupkg --source https://nuget.pkg.github.com/hunglu/index.json --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate`

This job only *publishes* — it doesn't bump version numbers or touch consuming services. Bumping
a service's `<PackageReference Version="...">` to pick up the new package is a deliberate,
separate commit in that service (this is the point of the independence model: a shared-lib
release doesn't force every service to redeploy immediately).

### Step 3 — Backend GitHub Actions (path-filtered per service)

Jobs in the same workflow:

1. `build-and-test`:
   - runs-on: ubuntu-latest
   - checkout, setup dotnet 10, then **per service**: `dotnet restore`/`build`/`test` its own
     `.sln` (there's no root solution — loop over the 5)
   - Runs on every push and PR

2. `build-and-push-<service>` (one per service: `identity`, `content`, `quiz`, `progress`,
   `notification`), each depending on `build-and-test` and `publish-shared-packages`, only on
   push to main, gated by a paths filter on `src/Services/<Service>/**` **or** a shared-package
   change:
   - `docker/login-action` against `ghcr.io` using `${{ github.actor }}` /
     `${{ secrets.GITHUB_TOKEN }}` (workflow `permissions: packages: write`)
   - `docker/build-push-action` on `src/Services/<Service>/Dockerfile`, **build context =
     `src/Services/<Service>`** (matches Phase 4 — not the repo root), passing the restore
     credential as a build secret:
     ```yaml
     secrets: |
       github_token=${{ secrets.GITHUB_TOKEN }}
     ```
   - Tags: `ghcr.io/hunglu/wordbuddy-<service>:${{ github.sha }}` and `:latest`
   - Smoke test *before* tagging `:latest`: `docker run -d -p 8080:8080 <sha-tag>`, wait, then
     `curl -f http://localhost:8080/api/health` — fail the job if it doesn't return 200

No extra secrets needed beyond the default `GITHUB_TOKEN` — both the NuGet push and the image
push authenticate against the same GitHub Packages registry with it.

### Step 4 — Frontend GitHub Actions

Unchanged — `.github/workflows/ci-cd.yml` in the WordBuddy.UI repo:

1. `build`: checkout, setup node 20, `npm ci`, `npm run build` — every push/PR
2. `build-and-push` (depends on `build`, push to main only): `ghcr.io` login, build & push
   `ghcr.io/hunglu/wordbuddy-ui:${{ github.sha }}` and `:latest`, smoke test with
   `docker run` + curl `http://localhost:3000/`

### Step 5 — Local rollout (replaces the old SCP/SSH deploy step)

Document (README + Makefile), don't script into CI — runs on your machine against the `kind`
cluster from Phase 4, per service:
```powershell
kind load docker-image ghcr.io/hunglu/wordbuddy-<service>:<sha> --name wordbuddy
kubectl -n wordbuddy set image deployment/<service>-api <service>-api=ghcr.io/hunglu/wordbuddy-<service>:<sha>
kubectl -n wordbuddy rollout status deployment/<service>-api
```
`make k8s-apply SERVICE=<name>` (or `make k8s-apply-all`) wraps this into one command.

### Step 6 — Rollback strategy (per service)

```powershell
kubectl -n wordbuddy rollout undo deployment/<service>-api
kubectl -n wordbuddy rollout status deployment/<service>-api
```
Scoped to whichever service regressed — the other 4 are unaffected, which is the whole point of
keeping them independent.

### Step 7 — Branch protection

Settings → Branches → rule for `main` in both repos: require `build-and-test` (backend) /
`build` (frontend) status checks, require PR before merge, no bypass even for admins. The
per-service `build-and-push-*` and `publish-shared-packages` jobs are conditional (path-filtered)
so they don't gate merges — configure manually in the GitHub UI.

### Step 8 — Docs

Root `CLAUDE.md`: CI/CD section covering the two path-filtered flows (shared-package publish vs.
per-service image build), required permissions, image tags, and the explicit "CI publishes only,
rollout to the local cluster is `make k8s-apply`" note.

Root `README.md`: CI/CD badges, deployment section, and a table — one row per service — image
name / registry / rollout command; plus a short note on how the NuGet package feed works and
when to bump a service's shared-package version.

Each service's `README.md`: its own image name and the one-line rollout/rollback commands scoped
to it.

### Step 9 — Final commit

Backend
```bash
git add .github/ src/Services/*/*.Api/Controllers/HealthController.cs Makefile CLAUDE.md README.md src/Services/*/README.md
git commit -m "feat: phase 5 — publish shared nuget packages + per-service image build/push + health checks"
git push origin main
```

Frontend (WordBuddy.UI)
```bash
git add .github/ CLAUDE.md README.md
git commit -m "feat: phase 5 — github actions build/push pipeline"
git push origin main
```

## Verification

- Push a change touching only `src/Shared/WordBuddy.Shared.Kernel/**` → only
  `publish-shared-packages` runs (no service image rebuilds unless a service also bumped its
  package version in the same push)
- Push a change touching only `src/Services/Content/**` → `build-and-test` +
  `build-and-push-content` run; the other 4 services don't rebuild
- `ghcr.io/hunglu/wordbuddy-<service>` shows a new tag after a run that touched that service
- `nuget.pkg.github.com` shows a new version after a shared-lib push
- `make k8s-apply SERVICE=content` rolls the local kind cluster's Content deployment onto the
  new tag; `kubectl rollout status` succeeds; other services are untouched
- Open a PR with a broken test → CI fails → merge blocked by branch protection
- `kubectl rollout undo deployment/content-api` restores the previous version of just Content
