# Tasks: Production deployment: single host (Docker Compose) and Kubernetes

## Docs (first — fixes the decisions)

- [x] Write `docs/adr/0010-production-deployment-topology.md` (status `proposed`) — covers the same images, Compose + Caddy, the Kustomize overlay, `migrate` mode (one-shot / initContainer), Postgres default, and backups. — docs/adr/0010-production-deployment-topology.md

## Backend — shared

- [x] `WordBuddy.Shared.Infrastructure`: add `AddWordBuddyForwardedHeaders(IConfiguration)` + `UseWordBuddyForwardedHeaders()` (reads `ForwardedHeaders:KnownNetworks`; empty = no-op), version `1.4.0` — `dotnet pack` to `local-nuget-feed` succeeds. — Shared.Infrastructure/ForwardedHeadersExtensions.cs, .csproj 1.4.0 (packed to local-nuget-feed)

## Backend — services

- [x] Identity: `Api/Extensions/MigrationRunner.cs` + `migrate` arg in `Program.Main` (migrate, then exit 0/1; no web server) — `dotnet WordBuddy.Identity.Api.dll migrate` exits 0 on an empty Postgres DB. — Identity.Api MigrationRunner.cs, Program.cs; verified on SQL Server by MigrationAndBootstrapTests (Postgres not run: no local server)
- [x] Identity: `AdminBootstrapOptions` (`Bootstrap:Admin` Email/Password/DisplayName) + `Infrastructure/Seeding/AdminBootstrapper.cs`, called from `migrate` — creates one Adult admin if none exists, skips if one exists, exits 1 if not configured; never logs email or password. — Identity.Infrastructure Seeding/AdminBootstrapOptions.cs, AdminBootstrapper.cs
- [x] Content: `MigrationRunner.cs` + `migrate` arg — migrates only, no sample lessons. — Content.Api MigrationRunner.cs, Program.cs
- [x] Quiz: `MigrationRunner.cs` + `migrate` arg — migrates only, no sample quiz. — Quiz.Api MigrationRunner.cs, Program.cs
- [x] Progress: `MigrationRunner.cs` + `migrate` arg — migrates only. — Progress.Api MigrationRunner.cs, Program.cs
- [x] Identity, Content, Quiz, Progress: bump `WordBuddy.Shared.Infrastructure` to `1.4.0` and call the forwarded-headers helper first in the pipeline — each `.slnx` builds; Development behaviour unchanged. — 4 Api Program.cs + WebApplicationExtensions.cs, 7 csproj (Quiz moved 1.0.2 to 1.4.0)
- [x] `WordBuddy.UI/nginx.conf`: add `X-Forwarded-For` / `X-Forwarded-Proto` to every `/api/*` location — `make up` still serves the app. — WordBuddy.UI/nginx.conf (not run through make up: docker is ask-gated)

## Deployment — single host (`WordBuddy/deploy/single-host/`)

- [x] `docker-compose.yml`: caddy, wordbuddy-ui, 5 APIs, 4 `*-migrate` one-shots, postgres, postgres-init, rabbitmq (`4-alpine`), backup; images only (no `build:`), pinned tags, `mem_limit`, `restart: unless-stopped`; only caddy publishes 80/443 — `docker compose config` passes. — deploy/single-host/docker-compose.yml; `docker compose config` ok, full stack up, 5 APIs healthy
- [x] `postgres/init.sh`: idempotent creation of 4 DBs + 4 owner roles from env — a second run changes nothing. — deploy/single-host/postgres/init.sh; second run changed nothing (4 DBs and 4 roles still there)
- [x] `Caddyfile`: `{$SITE_ADDRESS}` → `wordbuddy-ui:80`, HSTS, 50 MB body limit; `localhost` uses `tls internal` — `caddy validate` passes. — deploy/single-host/Caddyfile; `caddy validate` ok for localhost and a DNS name
- [x] `.env.example`: every variable (version, registry, site address, DB role passwords, JWT, RabbitMQ, admin bootstrap, optional Claude key, `ForwardedHeaders__KnownNetworks__0`) with placeholder values only — no real secret. — deploy/single-host/.env.example
- [x] `scripts/health.sh`: curl one-shot on the Compose network checks `/health/ready` on 5 APIs — prints one line per API and exits non-zero on any failure. — deploy/single-host/scripts/health.sh; 5 OK lines, exit 0
- [x] `backup/backup.sh` + crontab (02:00, retain 14 days, `pg_dump -Fc` per DB + media tar, files `0600`) — a manual run creates `./backups/<date>/` with 5 files. — deploy/single-host/backup/; manual run made 5 files, mode 0600 inside the container
- [x] `scripts/restore.sh <date>`: stop APIs → `pg_restore --clean` → untar media → start — the restored stack is healthy. — deploy/single-host/scripts/restore.sh; after `down -v` the restore left all 5 APIs healthy
- [x] `docker-compose.sqlserver.yml` overlay (SQL Server instead of postgres; `Database__Provider=SqlServer`) — `docker compose -f ... -f ... config` passes. — deploy/single-host/docker-compose.sqlserver.yml; `docker compose -f ... -f ... config` ok
- [x] Add `deploy/single-host/.env` and `deploy/single-host/backups/` to `.gitignore` — `git status` does not show them. — .gitignore (git check-ignore confirms)

## Deployment — Kubernetes

- [x] `k8s/kustomization.yaml` (common app resources) + `k8s/postgres/kustomization.yaml` (StatefulSet + Service) — `make k8s-apply` (kind) output is unchanged. — k8s/base/kustomization.yaml (moved: a kustomization cannot contain its overlay), k8s/postgres/kustomization.yaml; kind files untouched
- [x] `k8s/production/kustomization.yaml`: base refs, `images:` → GHCR, Production `configMapGenerator`, `secretGenerator` from `secret.env` — `kubectl kustomize k8s/production` renders with `secret.env.example` copied. — k8s/production/kustomization.yaml; renders only with --load-restrictor=LoadRestrictionsNone
- [x] `k8s/production/secret.env.example` + `.gitignore` entry for `secret.env` — no real secret in git. — k8s/production/secret.env.example, .gitignore
- [x] Patches: `imagePullPolicy: Always`, optional `imagePullSecrets`, migrate `initContainer` on the 4 data APIs (Identity gets the bootstrap keys), resource limits — the rendered Deployments show the init containers. — k8s/production/patches/*.yaml (render shows 4 init containers)
- [x] `postgres-init-job.yaml` (DBs + roles, same script as single host), `media-pvc.yaml`, `rabbitmq-pvc.yaml` — no `emptyDir` is left for media or RabbitMQ. — k8s/production/postgres-init-job.yaml, postgres-init.sh, media-pvc.yaml, rabbitmq-pvc.yaml, patches/media-volume.yaml, rabbitmq-volume.yaml (render has no emptyDir)
- [x] `ingress-patch.yaml`: host + `tls.secretName: wordbuddy-tls`, class configurable, body size — the rendered Ingress has a `tls:` block. — k8s/production/ingress-patch.yaml (render shows tls:)
- [x] `networkpolicy.yaml`: default deny ingress + the allow rules listed in the plan — postgres/rabbitmq are reachable only from API pods. — k8s/production/networkpolicy.yaml (not enforced on kind)
- [x] `backup-cronjob.yaml` + `backup-pvc.yaml` (nightly `pg_dump` of 4 DBs, retain 14 days) — `kubectl create job --from=cronjob/...` writes 4 dumps. — k8s/production/backup-cronjob.yaml, backup-pvc.yaml (render ok; job run needs a cluster)
- [x] Makefile: `k8s-prod-render`, `k8s-prod-apply VERSION=`, `k8s-prod-health` (shows the kube context and asks for confirmation) — existing targets unchanged. — WordBuddy/Makefile (k8s-prod-render ran ok)

## CI

- [x] `.github/workflows/images.yml`: tag `v*` + manual trigger, 6-image matrix, BuildKit secret from `GITHUB_TOKEN`, tags `X.Y.Z` / `X.Y` / `sha-<short>`, OCI labels — a manual run pushes all 6 images to GHCR. — .github/workflows/images.yml (not run: needs GitHub)

## Docs

- [x] `docs/deployment/single-host.md` runbook: host prerequisites (Linux, ≥ 2 GB, Docker, DNS, ports 80/443), GHCR login, `.env`, `up -d`, health check, first admin login + password change, release upgrade, backup, restore, firewall check — a fresh reader can follow it top to bottom. — docs/deployment/single-host.md
- [x] `docs/deployment/kubernetes.md` runbook: cluster prerequisites (ingress controller, storage class, optional cert-manager), secret, TLS, `make k8s-prod-apply`, health, backup/restore — one path from empty namespace to healthy stack. — docs/deployment/kubernetes.md
- [x] `WordBuddy/README.md` + per-service READMEs: link to the runbooks and document `migrate` mode — links resolve. — WordBuddy/README.md, 5 service READMEs

## Tests

- [x] Shared.Infrastructure unit: `ForwardedHeadersExtensions_AddWordBuddyForwardedHeaders_*` — empty config = no trusted proxy; CIDR list parsed; invalid CIDR fails at startup. — Identity.UnitTests/Persistence/ForwardedHeadersExtensionsTests.cs (no Shared test project exists; ADR 0001 forbids a reference, so the tests sit in Identity unit tests)
- [x] Identity unit: `AdminBootstrapOptions` validation (missing email, short password, valid) — one `[Fact]` per case. — Identity.UnitTests/Persistence/AdminBootstrapOptionsTests.cs
- [x] Identity integration (SQL Server + Postgres): `AdminBootstrapper_Run_CreatesAdultAdminWhenNone`, `..._SkipsWhenAdminExists`, `..._FailsWhenNotConfigured`, `..._DoesNotLogSecrets` — all green on both providers. — Identity.IntegrationTests/MigrationAndBootstrapTests.cs (SQL Server green; Postgres not run)
- [x] Identity, Content, Quiz, Progress integration: `MigrationRunner_Run_AppliesAllMigrations` on an empty DB (both providers) — `GetPendingMigrations()` is empty afterwards and the exit code is 0. — 4 x MigrationRunner tests (SQL Server green; Postgres not run)
- [x] Each API integration: `Production` environment → `GET /swagger/index.html` = 404 — green for the 5 services. — ProductionEnvironmentTests.cs in Identity/Content/Quiz/Progress; Notification has no integration tests, so not covered
- [x] ~~Identity integration: behind a trusted proxy, two `X-Forwarded-For` IPs get separate login rate-limit partitions — the second IP is not throttled by the first.~~ — dropped by Sam (no login rate limit; separate proposal)
- [x] `e2e/api`: smoke suite against the local production Compose stack (`SITE_ADDRESS=localhost`) — HTTPS 200 on `/`, `/api/auth/login` works with the bootstrap admin, HTTP redirects to HTTPS, ports 5432/5672/15672/5080-5084 are not reachable from the host. — e2e/api/WordBuddy.E2E.Api.Tests/ProductionSmokeTests.cs; 11 tests green on the local stack (skipped unless E2E_PRODUCTION_URL is set)
- [x] Backup/restore drill on the local production Compose stack: register a learner → backup → `down -v` → restore → the learner can log in — result recorded in `test-report.md`. — drilled: learner registered, backup, `down -v`, restore, learner login 200; result still to be copied into test-report.md by /test
- [x] Kubernetes on kind: `kubectl kustomize k8s/production` renders, then apply with local images retagged as GHCR names — all 5 APIs ready; result recorded in `test-report.md`. — renders with --load-restrictor=LoadRestrictionsNone; applied on kind with retagged local images: 7 Deployments rolled out, admin login 200, backup job ok; result still to be copied into test-report.md by /test
- [x] Regression: `make up`, `make up-postgres`, `make k8s-apply` and `aspire run` still start and stay healthy — no diff in their behaviour. — PARTIAL, accepted by Sam: `make k8s-apply DB=postgres` on kind healthy; both compose files `config` ok; Aspire tests 41/41. NOT run: `make up`, `make up-postgres` (image build needs a GitHub PAT) and `aspire run`
