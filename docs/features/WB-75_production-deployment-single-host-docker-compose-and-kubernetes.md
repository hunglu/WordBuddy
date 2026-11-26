---
feature: Production deployment (single host and Kubernetes)
services: Identity | Content | Quiz | Progress | Notification | UI
audience: Both
state: shipped
last-updated-by: WB-77_fix-wb-75-review-nits-a-and-b
---

# Production deployment (single host and Kubernetes)

Runs WordBuddy in production in one of two ways: a single host with Docker Compose, or a Kubernetes cluster. Both use the same versioned GHCR images.

```
GHCR images (built by CI on tag v*)
   ├── Single host: Caddy (HTTPS) → UI + 5 APIs, PostgreSQL, RabbitMQ, nightly backup
   └── Kubernetes: Ingress (TLS) → UI + 5 APIs, PostgreSQL, RabbitMQ, backup CronJob
```

## What it does

- **Images:** `.github/workflows/images.yml` builds 6 images (5 APIs + UI) on a `v*` tag or by hand. Tags: `X.Y.Z`, `X.Y`, `sha-<short>`. The GitHub Packages restore uses a BuildKit secret.
- **Production environment:** `ASPNETCORE_ENVIRONMENT=Production`. Swagger returns 404. No dev seed data.
- **Migrate step:** each data API (Identity, Content, Quiz, Progress) has a `migrate` argument: apply migrations, then exit 0 (ok) or 1 (failed). Failures log the error type, stack, DB error code and non-sensitive error fields (SQL Server: number, class, state; PostgreSQL: SQLSTATE, table, constraint). The provider message is withheld, so a duplicate-key error never leaks a key value (such as an email). Neither the connection string nor the password is logged.
- **Admin bootstrap:** Identity `migrate` creates one Adult admin from `Bootstrap__Admin__*` when no admin exists. It does nothing when one exists, exits 1 when a fresh database has no keys. It never logs the email or password. If a user with the admin email already exists, it fails (exit 1) with a clear error and does not promote that user, so no duplicate-key insert runs.
- **Forwarded headers:** `WordBuddy.Shared.Infrastructure` 1.4.0 trusts `X-Forwarded-*` only from `ForwardedHeaders:KnownNetworks`. Empty = off.
- **Single host** (`WordBuddy/deploy/single-host/`): Caddy is the only service that publishes ports (80/443). HTTP redirects to HTTPS. Postgres init script creates 4 databases and 4 owner roles. A backup container runs `pg_dump -Fc` per database + a media tar at 02:00 UTC and keeps 14 days. `scripts/health.sh` checks `/health/ready` on 5 APIs. `scripts/restore.sh <date>` restores. An optional overlay swaps in SQL Server (one login and database per service, no `sa` for APIs). Its `init.sh` checks all 5 passwords before any SQL: each must match `^[A-Za-z0-9]{16,}$` or the script exits 1 and names the variable (value never printed). The overlay uses `${VAR:?}`, so `docker compose` stops at once when a password is missing. `SA_PASSWORD` also needs the SQL Server complexity rule: use mixed-case letters and digits.
- **Kubernetes** (`WordBuddy/k8s/production/`, kustomize): GHCR images, migrate init containers, Postgres init Job, PVCs for media / RabbitMQ / backups, Ingress with `tls.secretName: wordbuddy-tls`, default-deny NetworkPolicy, nightly backup CronJob. Secrets come from `secret.env` (gitignored).

## Rules

- **Child vs adult:** no difference. HTTPS, secret handling and backups protect both. The bootstrap admin is always `Adult`.
- Traffic is HTTPS only. Database, RabbitMQ and Swagger are not reachable from outside.
- Secrets live in `.env` / `secret.env` (gitignored). Examples hold `CHANGE_ME` only.
- Backup files are mode `0600` (day folders `0700`).
- Local dev paths (`make up`, kind, Aspire) are unchanged.
- Out of scope: auto-scaling, multi-region, managed databases, monitoring stack.

## How to run

Single host (full steps: `docs/deployment/single-host.md`):

```bash
cd WordBuddy/deploy/single-host
cp .env.example .env        # fill every CHANGE_ME
docker compose up -d
./scripts/health.sh
```

Kubernetes (full steps: `docs/deployment/kubernetes.md`):

```bash
cp k8s/production/secret.env.example k8s/production/secret.env   # fill values
make k8s-prod-render OWNER=<github-owner> VERSION=<X.Y.Z>
make k8s-prod-apply  OWNER=<github-owner> VERSION=<X.Y.Z>        # asks to confirm the kube context
make k8s-prod-health
```

Release upgrade: back up, set the new version, start again. The migrate step runs first.

## API

No API change.

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

No UI change. `WordBuddy.UI/nginx.conf` now forwards `X-Forwarded-For` / `X-Forwarded-Proto` on every `/api/*` location.

## Pending changes

- WB-79_link-shared-nuget-packages-to-the-repo-so-ci-can-restore-them — link the shared NuGet packages to the repo so `images.yml` can restore them (#79)

## Change history

- WB-75_production-deployment-single-host-docker-compose-and-kubernetes — first version: single-host and Kubernetes production deployment, CI images, migrate mode, admin bootstrap, backups.
- WB-77_fix-wb-75-review-nits-a-and-b — migration failure logs withhold the provider message (no key values); admin email clash fails without promoting the user; SQL Server init and overlay reject empty or unsafe passwords (#77).
