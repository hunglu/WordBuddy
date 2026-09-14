---
title: Phase 5 — Deployment Pipeline
description: GitHub Actions CI/CD → auto deploy to AWS EC2 on push to main
status: draft
---

## Context

Two repos: github.com/hunglu/WordBuddy (backend),
github.com/hunglu/WordBuddy.UI (frontend).
Target: AWS EC2 t3.small running Docker Compose (set up in Phase 4).
On every push to main → build → test → SSH deploy to EC2.

## Requirements

- [ ] GitHub Actions workflow for backend: build + test + deploy
- [ ] GitHub Actions workflow for frontend: build + deploy
- [ ] GitHub Secrets configured on both repos
- [ ] Health check endpoint on API: GET /api/health
- [ ] Smoke test in pipeline verifies health check after deploy
- [ ] Rollback strategy: keep previous Docker image, redeploy on failure
- [ ] Branch protection on main: require CI to pass before merge
- [ ] Both CLAUDE.md + README.md updated with CI/CD docs

## Implementation Plan

### Step 1 — Health check endpoint

In WordBuddy.API add HealthController:

- GET /api/health → 200 OK with { status: "healthy", timestamp: utcNow }
- No [Authorize] — must be public
- Log Info on each health check call

### Step 2 — Backend GitHub Actions

Create .github/workflows/ci-cd.yml in WordBuddy backend repo:

Trigger: push to main, pull_request to main.

Jobs:

1. build-and-test:
   - runs-on: ubuntu-latest
   - steps: checkout, setup dotnet 8, restore, build Release, run xUnit tests
   - On pull_request: run only this job (no deploy)

2. deploy (depends on build-and-test, only on push to main):
   - steps:
     - Configure AWS credentials from GitHub Secrets
     - Build Docker image: wordbuddy-api:${{ github.sha }}
     - Save image as tar, SCP to EC2
     - SSH to EC2:
       - Load new image
       - Update docker-compose.yml to use new image tag
       - docker compose up -d wordbuddy-api
       - Wait 15s, curl /api/health
       - If health check fails: roll back to previous image tag + alert

GitHub Secrets needed (add to repo Settings → Secrets):

- EC2_HOST: public IP of EC2
- EC2_USER: ubuntu
- EC2_SSH_KEY: contents of ~/.ssh/wordbuddy-key.pem
- JWT_SECRET, SA_PASSWORD (prod values)

### Step 3 — Frontend GitHub Actions

Create .github/workflows/ci-cd.yml in WordBuddy.UI repo:

Trigger: push to main, pull_request to main.

Jobs:

1. build:
   - runs-on: ubuntu-latest
   - steps: checkout, setup node 20, npm ci, npm run build
   - Upload dist/ as artifact

2. deploy (depends on build, only on push to main):
   - Download dist/ artifact
   - Build Docker image: wordbuddy-ui:${{ github.sha }}
   - SCP image to EC2
   - SSH to EC2: load image, docker compose up -d wordbuddy-ui
   - Smoke test: curl http://{EC2_HOST} → expect 200

GitHub Secrets needed:

- EC2_HOST, EC2_USER, EC2_SSH_KEY (same EC2 as backend)

### Step 4 — Branch protection

In both GitHub repos, Settings → Branches → Add rule for main:

- Require status checks: build-and-test must pass
- Require pull request before merging
- Do not allow bypassing (even for admins)
Tell user to configure this manually in GitHub UI — not scriptable via CLI easily.

### Step 5 — Rollback strategy

In deploy job, before pulling new image:

- SSH: docker tag wordbuddy-api:current wordbuddy-api:previous
On health check failure:
- SSH: docker compose stop wordbuddy-api
- SSH: docker tag wordbuddy-api:previous wordbuddy-api:current
- SSH: docker compose up -d wordbuddy-api
- Exit with code 1 (marks pipeline as failed, sends GitHub notification)

### Step 6 — Docs

Update both CLAUDE.md files:

- CI/CD section: workflow triggers, jobs, required secrets
- Deployment flow diagram (text-based)
- Rollback instructions

Update both README.md files:

- CI/CD badges at top (build passing / failing)
- Deployment section: how pipeline works, how to trigger deploy,
  how to roll back manually
- Secrets table: name, description, where to get the value

### Step 7 — Final commit

Backend

```batch
git add .github/ src/WordBuddy.API/Controllers/HealthController.cs
git commit -m "feat: phase 5 — github actions ci/cd + health check + rollback"
git push origin main
```

Frontend

```batch
git add .github/
git commit -m "feat: phase 5 — github actions ci/cd pipeline"
git push origin main
```

## Verification

- Push a small change to main on both repos
- GitHub Actions tab shows green build
- After deploy job: curl http://{EC2_IP}/api/health → { "status": "healthy" }
- Open a PR with a broken test → CI fails → merge blocked by branch protection
- Manually introduce a bad deploy → rollback fires → previous version stays live
