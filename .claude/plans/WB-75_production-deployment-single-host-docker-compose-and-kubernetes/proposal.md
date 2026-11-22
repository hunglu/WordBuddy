---
title: WB-75_Production deployment: single host (Docker Compose) and Kubernetes
status: done
type: new
issue: 75
pr: 76
affects: docs/features/WB-75_production-deployment-single-host-docker-compose-and-kubernetes.md
supersedes: none
version: 1.7
created: 2026-11-22T10:44:24+07:00
updated: 2026-11-22T22:00:00+07:00
---

## Problem

WordBuddy has no production deployment path. Compose and `k8s/` are local dev setups only:

- `ASPNETCORE_ENVIRONMENT: Development` everywhere (Swagger exposed, dev behaviour).
- Migrations and seed run only in Development; a Production database stays empty and has no admin user.
- No HTTPS / reverse proxy, no backups, no CI/CD (`.github/workflows` is missing).
- Images are built by hand with `:dev` tags; the GitHub Packages restore inside `docker build` is not verified.

## Goal

Two supported production deployment methods, from the same versioned images:

```
            GHCR images (versioned tags, built by CI)
                 │
     ┌───────────┴────────────┐
     ▼                        ▼
 Single host              Kubernetes
 Docker Compose           manifests / overlay for a real cluster
 + reverse proxy (HTTPS)  + Ingress with TLS
 + PostgreSQL             + PostgreSQL (or SQL Server)
```

- **Single host:** one Linux host (at least 2 GB RAM) runs the full stack with Docker Compose.
- **Kubernetes:** production manifests for a real cluster (cloud provider not chosen; must stay provider-neutral).
- Both: Production environment, secrets outside git, a migration step per release, an admin bootstrap, backups.

## Target users

Both (children and adults). No behaviour change; child data protection applies to secrets, backups and HTTPS.

## Success criteria

- A fresh Linux host goes from empty to a healthy stack (`/health/ready` = 200 on all APIs) by following one runbook.
- A Kubernetes cluster reaches the same state with one `make`/`kubectl` path.
- Traffic is HTTPS only; database, RabbitMQ and Swagger are not reachable from outside.
- A release migrates the 4 databases without manual SQL; the first run creates an admin user.
- A nightly backup exists and a restore is tested once.
- CI builds and pushes versioned images to GHCR.

## Constraints

- Keep the local dev paths (`make up`, `kind`, Aspire) unchanged.
- PostgreSQL is the default provider for production (ADR 0009); SQL Server stays possible.
- No cloud vendor lock-in; no paid services required.
- Secrets never in git or logs (`.env`, k8s Secrets created from examples).
- Likely needs an ADR (deployment topology); `/plan` decides.
- Out of scope: auto-scaling, multi-region, managed database services, monitoring stack beyond the existing OTel/health endpoints.
