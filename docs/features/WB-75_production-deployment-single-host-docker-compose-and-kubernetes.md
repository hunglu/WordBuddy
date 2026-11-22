---
feature: Production deployment (single host and Kubernetes)
services: Identity | Content | Quiz | Progress | Notification | UI
audience: Both
state: proposed
last-updated-by: WB-75_production-deployment-single-host-docker-compose-and-kubernetes
---

# Production deployment (single host and Kubernetes)

Runs WordBuddy in production in one of two ways: a single host with Docker Compose, or a Kubernetes cluster.

## What it does

_Proposed — not on main yet._

- Same versioned GHCR images for both methods.
- Single host: Docker Compose, PostgreSQL, HTTPS reverse proxy.
- Kubernetes: provider-neutral production manifests, Ingress with TLS.
- Both: Production environment, secrets outside git, a migration step per release, admin bootstrap, backups.

## Rules

- **Child vs adult:** no difference; HTTPS, secret handling and backups protect both.

## API

No API change.

| Method | Route | Service | Auth policy |
|---|---|---|---|

## UI

No UI change.

## Pending changes

## Change history
