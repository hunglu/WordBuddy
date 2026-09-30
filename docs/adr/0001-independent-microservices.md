# 0001 — Five fully independent microservices

- Status: accepted
- Date: 2026-06 (retro-recorded 2026-09-30)

## Context

Services should be liftable into their own repos later with nothing to untangle.

## Decision

No project references between services or to shared code. Each service has its own `.slnx`,
`nuget.config`, `Dockerfile`, tests. Shared code ships as versioned NuGet packages
(`WordBuddy.Shared.*`) via `local-nuget-feed/` locally and GitHub Packages for CI/Docker.

## Consequences

Shared changes need a repack/publish. No root solution. Docker builds need a GitHub token secret.
