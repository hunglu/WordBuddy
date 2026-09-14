---
title: Phase 4 — Infrastructure
description: Docker (per independent service), local media storage, local Kubernetes (kind) hosting for 5 microservices
status: draft
---

## Context

Architecture: 5 fully independent microservices (`Identity`, `Content`, `Quiz`, `Progress`,
`Notification`) — per Phase 1's independence model, no service has a project reference to
another service or to a shared project; common code (`WordBuddy.Shared.Kernel`/`Infrastructure`/
`Contracts`) is a versioned NuGet package restored from a local file feed (dev) or GitHub
Packages (CI/Docker). **This has a direct consequence for Docker**: each service's Dockerfile
build context is that service's own folder only (`src/Services/<Service>/`) — not the repo
root — because a real, independent repo would only ever contain that folder. That means Docker
builds can't see `local-nuget-feed/` (it's outside the build context) and must restore shared
packages from GitHub Packages instead, authenticated via a BuildKit secret, never baked into an
image layer or ARG.

Media storage: local filesystem (a PersistentVolume in-cluster), migrate to S3 later. Hosting:
**local Kubernetes via `kind`**, not AWS EC2 — a cloud target (managed EKS vs. self-managed k3s
on EC2) is deliberately deferred. `docker-compose.yml` stays as a fast inner-loop option for
day-to-day dev; Kubernetes is how you test the "real" deployment shape locally. Database: **one
shared SQL Server instance/pod, a separate database per service** (`WordBuddyIdentity`,
`WordBuddyContent`, `WordBuddyQuiz`, `WordBuddyProgress`, `WordBuddyNotification`).

The pattern below is described once per concern and repeats across the 5 services — apply the
same shape with the service name substituted, don't duplicate the detail 5 times.

## Requirements

- [ ] Docker Desktop installed and running, BuildKit enabled (default in current Docker Desktop)
- [ ] `kind` + `kubectl` installed, `ingress-nginx` available for kind
- [ ] One Dockerfile **inside each service's own folder**
      (`src/Services/<Service>/Dockerfile`), build context = that folder, restoring
      `WordBuddy.Shared.*` from GitHub Packages via a BuildKit secret — never a plaintext
      token in an `ARG`/`ENV`/layer
- [ ] Frontend Dockerfile (multi-stage, Nginx, SPA routing, path-based proxy to each service)
- [ ] nginx.conf: SPA fallback, per-service path proxy, gzip, cache headers
- [ ] `IFileStorageService` + `LocalFileStorageService` in `Content.Application`/
      `Content.Infrastructure` (the only service handling media)
- [ ] `MediaController` in `Content.Api`: POST /api/media/upload, GET /api/media/{id}
- [ ] `docker-compose.yml` at the repo root: one shared sqlserver, all 5 service builds (each
      pointed at its own service folder as build context), wordbuddy-ui — for local dev only
- [ ] `.env.example` committed, `.env` in `.gitignore`
- [ ] kind cluster config with ingress port mappings
- [ ] `k8s/` manifests: namespace, ConfigMap, Secret template, one shared SQL Server
      StatefulSet + PVC + init Job (creates all 5 databases), one Deployment + Service per
      microservice, wordbuddy-ui Deployment/Service, one shared Ingress routing by path prefix
- [ ] Makefile: `up`, `down`, `logs`, `migrate`, `clean`, `pack-shared`, `publish-shared`,
      `cluster-up`, `cluster-down`, `k8s-build-load`, `k8s-apply`, `k8s-logs` (service-scoped
      targets take `SERVICE=<name>`)
- [ ] Root `CLAUDE.md` + `README.md` updated (Kubernetes section replaces AWS EC2 section); each
      service's own `README.md` gets a short "Running in Docker/Kubernetes" note

## Implementation Plan

### Step 1 — Per-service Dockerfile (build context = the service's own folder)

At `src/Services/<Service>/Dockerfile` for each of the 5 services:

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN --mount=type=secret,id=github_token \
    dotnet nuget add source https://nuget.pkg.github.com/hunglu/index.json \
      -n github-wordbuddy -u hunglu -p "$(cat /run/secrets/github_token)" --store-password-in-clear-text && \
    dotnet restore ./<Service>.Api && \
    dotnet publish ./<Service>.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN useradd -m appuser
WORKDIR /app
COPY --from=build /app/publish .
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
# Content only:
# VOLUME /app/media
ENTRYPOINT ["dotnet", "<Service>.Api.dll"]
```

Build it with the token passed as a **BuildKit secret** (never `--build-arg`, which leaks into
image history):
```powershell
$env:DOCKER_BUILDKIT=1
docker build --secret id=github_token,env=GITHUB_TOKEN -t wordbuddy-identity:dev src/Services/Identity
```
The service's own `nuget.config` (from Phase 1) already lists `github-wordbuddy` as a source;
this `dotnet nuget add source` call at build time supplies the credential the config file itself
doesn't carry. **Local dev iteration (`dotnet build`/`dotnet run` outside Docker) still uses the
fast `local-nuget-feed/` and needs no token** — only a Docker build needs GitHub Packages, since
it can't see that local folder. Practically: run `make publish-shared` once after changing a
shared library and before rebuilding any service's Docker image.

One shared `.dockerignore`, copied into each service folder (or a root one referenced via
`--ignorefile` if your Docker version supports it): `bin/`, `obj/`, `.vs/`, `*.user`.

### Step 2 — Frontend Dockerfile

Unchanged in shape — at `WordBuddy.UI` root:
Stage 1 (`node:20-alpine`): `npm ci` + `npm run build`.
Stage 2 (`nginx:alpine`): copy `dist` + `nginx.conf`, `EXPOSE 80`.
`nginx.conf`: `try_files` for SPA; `proxy_pass` per path prefix — `/api/auth` →
`identity-api:8080`, `/api/lessons`/`/api/media` → `content-api:8080`, `/api/quiz` →
`quiz-api:8080`, `/api/progress` → `progress-api:8080`. gzip on, cache 1yr for hashed assets,
no-cache for `index.html`.

### Step 3 — Local media storage (Content service only)

In `Content.Application`, create `IFileStorageService`:
- `SaveAsync(Stream, fileName, contentType, ct)` → returns relative path
- `GetAsync(filePath, ct)` → returns `Stream`
- `DeleteAsync(filePath, ct)`

In `Content.Infrastructure`, create `LocalFileStorageService`:
- Reads `FileStorage:BasePath` from config
- `SaveAsync`: generates a `Guid` filename, saves to `BasePath/{type}/{year}/{month}/`
- Inject `ILogger<LocalFileStorageService>`

In `Content.Api`, create `MediaController`:
- `POST /api/media/upload`: validate `IFormFile` (max 50MB), allowed types: image/jpeg,
  image/png, audio/mpeg, audio/wav, video/mp4; save via `IFileStorageService`, create
  `MediaAsset` record, return DTO
- `GET /api/media/{id}`: stream file with `FileStreamResult` + `Content-Type`
- `[Authorize]` on upload, anonymous on GET

`appsettings.Development.json` (Content only): `FileStorage:BasePath` = `C:\WordBuddyMedia`
locally, `/app/media` in Docker/Kubernetes (same env var, different value via ConfigMap).

### Step 4 — docker-compose.yml (local dev inner loop)

At the repo root, kept purely for fast `docker compose up` iteration:
- `sqlserver`: `mcr.microsoft.com/mssql/server:2022-latest`, volume
  `sqlserver-data:/var/opt/mssql`, health check, port 1433; an init step creates all 5 service
  databases
- `identity-api`, `content-api`, `quiz-api`, `progress-api`, `notification-api`: each
  `build: { context: src/Services/<Service>, dockerfile: Dockerfile }`, requires
  `DOCKER_BUILDKIT=1` and a `secrets:`-mounted `github_token` per service (Compose v2 supports
  build secrets), `depends_on: sqlserver` (healthy), env from `.env` (own connection string per
  service), own port mapping (`8081:8080`, `8082:8080`, …)
- `wordbuddy-ui`: build from `WordBuddy.UI`, `depends_on` all 5 APIs, port `3000:80`

`.env.example` with `SA_PASSWORD`, `JWT_SECRET`, `JWT_ISSUER`, `JWT_EXPIRY_MINUTES`, one
`<SERVICE>_CONNECTION_STRING` per service, and `GITHUB_TOKEN` (for the build secret — a PAT with
`read:packages`, not committed). Add `.env` to `.gitignore`.

### Step 5 — kind cluster

Install `kind`, `kubectl` (not Docker Desktop's built-in Kubernetes).

`kind-config.yaml` at the repo root — one control-plane node, ingress-ready label, port
mappings 80→8080 / 443→8443:
```yaml
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
  - role: control-plane
    kubeadmConfigPatches:
      - |
        kind: InitConfiguration
        nodeRegistration:
          kubeletExtraArgs:
            node-labels: "ingress-ready=true"
    extraPortMappings:
      - containerPort: 80
        hostPort: 8080
        protocol: TCP
      - containerPort: 443
        hostPort: 8443
        protocol: TCP
```
```powershell
kind create cluster --name wordbuddy --config kind-config.yaml
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/main/deploy/static/provider/kind/deploy.yaml
kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=120s
```

### Step 6 — Kubernetes manifests

Create a `k8s/` folder at the repo root (this is orchestration/ops config, not owned by any one
service, so it lives at the root rather than inside a service folder):

- `namespace.yaml` — `wordbuddy` namespace
- `configmap.yaml` — shared non-secret env vars (`Jwt__Issuer`, `Jwt__ExpiryMinutes`,
  `ASPNETCORE_ENVIRONMENT=Development`) plus Content's `FileStorage__BasePath=/app/media`
- `secret.yaml.example` — placeholders for `Jwt__Secret`, `SA_PASSWORD`, one
  `<SERVICE>_ConnectionStrings__DefaultConnection` per service; never commit the real
  `secret.yaml` (gitignored next to `.env`)
- `sqlserver-statefulset.yaml` + `sqlserver-service.yaml` — one StatefulSet (1 replica), 10Gi
  PVC, plus `sqlserver-init-job.yaml` that creates the 5 databases once
- Per service — `<service>-deployment.yaml` + `<service>-service.yaml`: image
  `wordbuddy-<service>:dev` (loaded locally — Step 7), `envFrom` ConfigMap + Secret (only its own
  connection-string key), `livenessProbe`/`readinessProbe` on `GET /api/health` (Phase 5) —
  until then `/swagger/index.html`, tight resource requests/limits (`100m/128Mi` request,
  `500m/512Mi` limit — 5 API pods + SQL Server is a lot for a laptop)
- `ui-deployment.yaml` / `ui-service.yaml` — same pattern, port 80
- `ingress.yaml` — one shared Ingress: `/` → `wordbuddy-ui:80`, `/api/auth` → `identity-api:8080`,
  `/api/lessons`+`/api/media` → `content-api:8080`, `/api/quiz` → `quiz-api:8080`,
  `/api/progress` → `progress-api:8080`

### Step 7 — Build, load, and apply

```powershell
$env:DOCKER_BUILDKIT=1
$services = "identity","content","quiz","progress","notification"
foreach ($s in $services) {
    docker build --secret id=github_token,env=GITHUB_TOKEN -t "wordbuddy-$s`:dev" "src/Services/$($s.Substring(0,1).ToUpper()+$s.Substring(1))"
    kind load docker-image "wordbuddy-$s`:dev" --name wordbuddy
}
docker build -t wordbuddy-ui:dev -f WordBuddy.UI/Dockerfile WordBuddy.UI
kind load docker-image wordbuddy-ui:dev --name wordbuddy

kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml -f k8s/secret.yaml
kubectl apply -f k8s/sqlserver-statefulset.yaml -f k8s/sqlserver-service.yaml
kubectl apply -f k8s/sqlserver-init-job.yaml
foreach ($s in $services) { kubectl apply -f "k8s/$s-deployment.yaml" -f "k8s/$s-service.yaml" }
kubectl apply -f k8s/ui-deployment.yaml -f k8s/ui-service.yaml
kubectl apply -f k8s/ingress.yaml

kubectl -n wordbuddy get pods -w
```

Run each service's EF Core migration once its pod is Running (skip Notification — no entities
yet):
```powershell
kubectl -n wordbuddy exec deploy/identity-api -- dotnet ef database update
```

### Step 8 — Makefile

- `up` / `down` / `logs` / `migrate` / `clean` — docker-compose targets for local dev
- `pack-shared` — `dotnet pack` the 3 shared libs into `local-nuget-feed/` (Phase 1)
- `publish-shared` — `dotnet nuget push` the same packages to GitHub Packages — **run this
  before any Docker build that needs a shared-lib change**
- `cluster-up` / `cluster-down` — create/delete the kind cluster (+ install ingress-nginx)
- `k8s-build-load SERVICE=<name>` (or no arg = all 5) — build with the BuildKit secret + `kind
  load docker-image`
- `k8s-apply` — apply everything in `k8s/` in order
- `k8s-logs SERVICE=<name>` / `k8s-migrate SERVICE=<name>`

### Step 9 — Docs

Root `CLAUDE.md` + `README.md`: replace the AWS EC2 section with "Local Kubernetes (kind)",
explain the GitHub Packages requirement for Docker builds (and why — independence model from
Phase 1), note the deferred cloud-cluster decision, and a table: service → image name →
Deployment/Service name → ingress path prefix.

Each service's own `README.md`: its Dockerfile path (inside its own folder), its own env vars,
its own database name, and the one-time `make publish-shared` reminder if it just bumped a
shared package version.

`WordBuddy.UI` `CLAUDE.md`/`README.md`: Docker/Nginx section (unchanged).

### Step 10 — Commit

Backend
```bash
git add docker-compose.yml .env.example kind-config.yaml k8s/ Makefile CLAUDE.md README.md src/Services/*/Dockerfile src/Services/*/.dockerignore src/Services/*/README.md
git commit -m "feat: phase 4 — per-service docker images (github packages) + local kubernetes (kind) manifests"
```

Frontend (WordBuddy.UI)
```bash
git add Dockerfile .dockerignore nginx.conf CLAUDE.md README.md
git commit -m "feat: phase 4 — dockerfile + nginx config"
```

## Verification

- `docker compose up` → sqlserver + all 5 service APIs + ui healthy
- `kind create cluster --config kind-config.yaml` → `kubectl get nodes` shows the node `Ready`
- `kubectl -n wordbuddy get pods` → sqlserver + all 5 `<service>-api` pods + `wordbuddy-ui`
  `Running`/`1/1 Ready`
- `curl http://localhost:8080/` (kind-config port mapping) → React app loads
- `curl http://localhost:8080/api/lessons` (with a valid JWT) returns Content's data, proving
  path-based routing
- Building an image without `--secret id=github_token,...` fails at restore (proves the token
  isn't accidentally baked into the Dockerfile as a plain `ARG`)
- `kind delete cluster --name wordbuddy` cleanly tears everything down
