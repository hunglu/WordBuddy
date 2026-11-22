# Review: Production deployment: single host (Docker Compose) and Kubernetes

PR: #76 · Round 1 · Reviewed commit: 9f1c5ac · 2026-11-22T21:04:21+07:00

## Verdict

Changes requested — 4 majors: a documented Kubernetes step breaks Identity, the runbook scripts are not executable, migration failures cannot be diagnosed, and the SQL Server overlay runs every API as `sa`. No blockers.

Security focus areas checked and fine:

| Area | Result |
| --- | --- |
| Secrets in git | None. `.env`, `secret.env`, `backups/`, `.rendered.yaml` are gitignored; examples hold `CHANGE_ME` only |
| Secrets in logs | Bootstrap never logs email or password (test covers it); migrate logs no connection string |
| Admin bootstrap | Always `Adult`, BCrypt 12, only when no admin exists, exit 1 when not configured; keys reach only the migrate container / init container |
| Forwarded headers | Empty config = `ForwardedHeaders.None`; invalid CIDR fails at startup; loopback defaults cleared. Caddy drops client `X-Forwarded-For`, so spoofing from outside does not work |
| Exposed ports | Single host: only Caddy publishes 80/443. Kubernetes: no NodePort/LoadBalancer added |
| NetworkPolicy | Default deny ingress; Postgres only from API pods + init/backup; RabbitMQ only from the 3 messaging APIs |
| Backup files | `umask 077` + `chmod 600`, day folders `0700` |
| CI | BuildKit secret (no build arg), `contents: read` + `packages: write`, semver + sha tags, OCI labels |
| Local dev paths | `docker-compose.yml`, kind manifests and existing Make targets unchanged; `nginx.conf` change is additive |

## Findings

| # | Severity | File:line | Finding | Suggested fix |
| --- | --- | --- | --- | --- |
| 1 | major | `WordBuddy/k8s/production/patches/migrate-init-containers.yaml:31-40`, `docs/deployment/kubernetes.md:82` | The runbook says "Remove `Bootstrap__Admin__Password` from `secret.env` and run `make k8s-prod-apply` again". The init container uses `secretKeyRef` without `optional: true`. Removing the line removes the key, so the new Identity pod fails with `CreateContainerConfigError`. The old pod keeps serving until its next restart or node drain; after that Identity is down (no login for anyone). | Add `optional: true` to both `Bootstrap__Admin__*` refs, or tell the runbook to keep the key with an empty value (`Bootstrap__Admin__Password=`). |
| 2 | major | `WordBuddy/deploy/single-host/scripts/health.sh`, `scripts/restore.sh` (git mode `100644`); `docs/deployment/single-host.md:61,80,107` | Both scripts are committed without the executable bit. On a fresh Linux clone `./scripts/health.sh` fails with "Permission denied", so the runbook breaks at step 5 (success criterion "fresh host by following one runbook"). | `git update-index --chmod=+x` on both scripts, or change the runbook to `sh scripts/health.sh` / `sh scripts/restore.sh`. |
| 3 | major | `WordBuddy.Identity.Api/Extensions/MigrationRunner.cs:32`, `WordBuddy.Content.Api/Extensions/MigrationRunner.cs:29`, `WordBuddy.Quiz.Api/Extensions/MigrationRunner.cs:29`, `WordBuddy.Progress.Api/Extensions/MigrationRunner.cs:29` | On failure only `exception.GetType().Name` is logged. A wrong role password, a missing database and a failing migration script all log the same `NpgsqlException`/`PostgresException`. First deploy and upgrades cannot be diagnosed from `docker compose logs` / `kubectl logs -c migrate`, which is what the runbooks tell the operator to read. Also conflicts with the backend rule "logged at Error with stack trace". | Log the exception object (stack trace) plus `SqlState` / SQL error number. Npgsql and SqlClient messages do not contain the password. A strict option: log `DbException.Message` + `SqlState` and the stack trace. |
| 4 | major | `WordBuddy/deploy/single-host/docker-compose.sqlserver.yml:58,66,74,82,87,92,97,102` | Every migrate job and API connects as `sa`. A flaw in any one API gives sysadmin over all four databases (children's data in Identity and Progress). The plan and ADR 0010 promise "one least-privilege owner role per service"; the SQL Server path silently drops it. | Create one login + user per database in `sqlserver-init` (db_owner on its own database only), or state this as an accepted limitation in ADR 0010 and the runbook's SQL Server section. |
| 5 | nit | `WordBuddy/Makefile:143-145` | `k8s-prod-apply` writes the rendered overlay, including the base64 `wordbuddy-secrets` Secret, to `k8s/production/.rendered.yaml` with default permissions and leaves it there. | Pipe into `kubectl apply -f -`, or write to `mktemp` and delete it after apply. |
| 6 | nit | `WordBuddy/k8s/production/kustomization.yaml:3` | Header says `kubectl kustomize k8s/production`; without `--load-restrictor=LoadRestrictionsNone` it fails. | Use the full command or point to `make k8s-prod-render`. |
| 7 | nit | `docs/deployment/kubernetes.md:30,56` | Section 2 does `cd WordBuddy/k8s/production`; section 3 then says `cd WordBuddy`, which does not exist from there. | Use `cd ../..` or start both from the repo root. |
| 8 | nit | `docs/deployment/single-host.md:132-133` | The SQL Server backup example uses `$SA_PASSWORD` from the host shell (not set; `.env` is not sourced) and puts the password on the command line. `/var/opt/mssql/backup` may not exist. | Run `sqlcmd` via `docker compose exec sqlserver sh -c '... -P "$MSSQL_SA_PASSWORD" ...'` and `mkdir -p` the folder first. |
| 9 | nit | `WordBuddy/k8s/production/networkpolicy.yaml:32` | `kube-system` in the controller allow-list lets every kube-system pod reach UI and APIs. | Keep only the namespace of the real controller; mention k3s (Traefik in kube-system) in the runbook. |
| 10 | nit | `WordBuddy/deploy/single-host/backup/crontab:2`, `docs/deployment/single-host.md:87` | "02:00" is container time, which is UTC. | Say "02:00 UTC" or set `TZ` on the backup container. |
| 11 | nit | `docs/deployment/single-host.md:76-83` | "Take a backup first" comes after the upgrade commands. | Put the backup command first in the code block. |
| 12 | nit | `.github/workflows/images.yml:30-60` | Third-party actions are pinned by major tag, not by commit SHA, in a job holding `packages: write`. | Pin `docker/*` actions to SHAs (Dependabot can update them). |
| 13 | nit | `WordBuddy/src/Services/Quiz/WordBuddy.Quiz.Api/WordBuddy.Quiz.Api.csproj:23` | Quiz jumps from `Shared.Infrastructure` 1.0.2 to 1.4.0 (skips 1.1–1.3 changes, pulls MassTransit transitively) while Kernel stays 1.0.1. Builds, but the Development path of Quiz was not run (`make up` not run). | `/test`: run the Quiz suites and one Quiz Development start. |

## Plan conformance

- **Tasks:** 41/41 reflected in the diff. Login rate-limit test dropped and regression task partial — both accepted by Sam, not findings.
- **Deviations (accepted, sound):**
  - Kustomize base at `k8s/base/` with `--load-restrictor=LoadRestrictionsNone` (a kustomization cannot contain its overlay).
  - Duplicated Postgres init script with a `diff -q` check in both `k8s-prod-*` targets.
  - Forwarded-header tests in `Identity.UnitTests` (no shared test project; ADR 0001).
  - `MigrationRunner` is `public static` so integration tests can call it.
  - `k8s-prod-apply` uses `sed` on the rendered output instead of `kustomize edit set image` in a temp copy.
- **Gaps:** Swagger-off test missing for Notification (no integration test project). Postgres runs of the new integration tests not done yet (`/test`). `images.yml` never run.
- **Out of scope:** nothing outside the plan.
- **Known note:** on kind, `/api/*` through the UI service returns 502 (nginx `resolver 127.0.0.11`); the Ingress routes APIs directly, so browsers are not affected.
- **DB diagrams:** no EF migration in the diff, no diagram change needed.

## Fix round 1

| # | Result | What was done |
| --- | --- | --- |
| 1 | fixed | `optional: true` on both `Bootstrap__Admin__*` refs in `migrate-init-containers.yaml`. Compose uses `${BOOTSTRAP_ADMIN_*:-}`. Both runbooks say the keys may be removed. Identity bootstrapper checked: admin exists + settings missing = exit 0; no admin + settings missing = exit 1 (existing tests cover both) |
| 2 | fixed | `git update-index --chmod=+x` on `scripts/health.sh`, `scripts/restore.sh`, `backup/*.sh`, `postgres/init.sh`, `k8s/production/postgres-init.sh` and the new `sqlserver/init.sh` |
| 3 | fixed | New `MigrationFailureLog` (per service, Api project) logs exception types, stack traces of the full chain, `PostgresException.SqlState` / `SqlException.Number`. The provider message is logged only when the chain holds a `DbException` (server messages hold user, database or host, never the password). Other exception types keep the message withheld, because e.g. an `ArgumentException` from a bad connection string can echo it. The exception object is never passed to the logger. New test `MigrationFailureLogTests` per service (unreachable DB, asserts code and stack trace present, password absent) |
| 4 | fixed | `sqlserver/init.sh` (idempotent) creates the 4 databases, one login + user per database (`wb_identity`, ...) with `db_owner` on its own database only. APIs and migrate jobs use them. `sa` stays only for health check and init. Verified in a real SQL Server container: init runs twice, `wb_identity` can create a table in its database and cannot open another. Runbook and `.env.example` updated |
| 5 | fixed | `k8s-prod-apply` pipes into `kubectl apply -f -`; no rendered Secret on disk |
| 6 | fixed | `kustomization.yaml` header points to `make k8s-prod-render` and names the load restrictor |
| 7 | fixed | `cd` paths in `kubernetes.md` |
| 8 | fixed | SQL Server backup command runs `sqlcmd` inside the container with `$MSSQL_SA_PASSWORD`, `mkdir -p` first |
| 9 | fixed | `kube-system` and `ingress` removed from the allow-list (default `ingress-nginx`, `traefik`); comment and runbook section 4 explain the k3s case |
| 10 | fixed | Documented as 02:00 UTC (not configurable; Alpine image has no tzdata) in both runbooks, CronJob and compose comments |
| 11 | fixed | Backup step first in the upgrade section |
| 12 | fixed | All 5 actions pinned by full commit SHA with version comment; SHAs read with `gh api repos/<owner>/<repo>/git/ref/tags/<tag>` (all `commit` objects) |
| 13 | for /test | Quiz unit tests run and pass (17) |
