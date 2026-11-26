# Plan: WB-79 Link shared NuGet packages to the repo so CI can restore them

## Summary

Add `RepositoryUrl` to all 5 `WordBuddy.Shared.*` packages, bump each by one patch version, and move every service to the new versions. GitHub then links the packages to `hunglu/WordBuddy`, and `images.yml` restores them with `GITHUB_TOKEN`. No stored secret, no app behaviour change.

```mermaid
flowchart LR
    P["src/Shared/Directory.Build.props<br/>RepositoryUrl + RepositoryType"] --> N["nupkg nuspec<br/>&lt;repository url=.../&gt;"]
    N -->|make publish-shared (ask-gated)| G["nuget.pkg.github.com/hunglu<br/>package linked to hunglu/WordBuddy"]
    G -->|GITHUB_TOKEN, repo access| CI["images.yml on v1.0.1<br/>6 images to GHCR"]
```

## Affected services / areas

| Area | Files | Change |
| --- | --- | --- |
| Shared (5 packages) | new `WordBuddy/src/Shared/Directory.Build.props`, 5 `*.csproj` | Repository metadata, patch version bump |
| Identity, Content, Quiz, Progress, Notification | `*.csproj` `PackageReference`s | Point to the new versions, remove version drift |
| CI | `.github/workflows/images.yml` | Header comment only |
| Docs | `WordBuddy/README.md` | Shared-package prerequisites |
| `WordBuddy/CLAUDE.md` | — | Ask-gated; exact text proposed below, Sam applies |

No frontend change. No contract, endpoint or schema change.

## Backend approach

### 1. Repository metadata (one place)

No `Directory.Build.props` exists today. Add `WordBuddy/src/Shared/Directory.Build.props`:

| Property | Value | Why |
| --- | --- | --- |
| `RepositoryUrl` | `https://github.com/hunglu/WordBuddy` | GitHub links the package to this repo |
| `RepositoryType` | `git` | Required next to the URL in the nuspec |
| `PublishRepositoryUrl` | `true` | The SDK's Source Link targets only write `<repository>` into the nuspec when this is set; explicit URL stays authoritative |

- MSBuild picks the file up for every project under `src/Shared/` only. Services live under `src/Services/` and do not see it. ADR 0001 is untouched.
- Do not depend on the local git remote: the explicit URL is the source of truth.
- Verify by opening each packed `.nupkg` and reading its `.nuspec`: it must contain `<repository type="git" url="https://github.com/hunglu/WordBuddy" .../>`.

### 2. Patch version bumps

The link applies to newly pushed versions. Every package needs one new version.

| Package | Now | New | Extra change |
| --- | --- | --- | --- |
| `WordBuddy.Shared.Kernel` | 1.1.0 | 1.1.1 | — |
| `WordBuddy.Shared.Application` | 1.0.0 | 1.0.1 | Dependency `Kernel` 1.0.1 → 1.1.1 |
| `WordBuddy.Shared.Infrastructure` | 1.4.0 | 1.4.1 | — |
| `WordBuddy.Shared.Contracts` | 1.4.0 | 1.4.1 | — |
| `WordBuddy.Shared.Protos` | 1.0.0 | 1.0.1 | — |

No code change in any package.

### 3. Service references (and drift cleanup)

All services move to the new versions. Three references drift today; they move too, so CI never asks for an old, unlinked version.

| Service | Kernel | Application | Infrastructure | Contracts | Protos |
| --- | --- | --- | --- | --- | --- |
| Identity | 1.1.0 → 1.1.1 | 1.0.0 → 1.0.1 | 1.4.0 → 1.4.1 | 1.4.0 → 1.4.1 | — |
| Content | 1.1.0 → 1.1.1 | 1.0.0 → 1.0.1 | 1.4.0 → 1.4.1 | **1.3.0** → 1.4.1 | 1.0.0 → 1.0.1 |
| Quiz | **1.0.1** → 1.1.1 | 1.0.0 → 1.0.1 | 1.4.0 → 1.4.1 | — | — |
| Progress | 1.1.0 → 1.1.1 | 1.0.0 → 1.0.1 | 1.4.0 → 1.4.1 | 1.4.0 → 1.4.1 | 1.0.0 → 1.0.1 |
| Notification | — | — | **1.0.2** → 1.4.1 | — | — |

- Bold = drift jump. Notification uses only Serilog/OTel/health helpers, which still exist in 1.4.x. If any drift jump needs more than a version change to compile, the coder stops and reports (see Open question 2).
- No Aspire or `e2e` project references a shared package (checked).

### 4. Order of operations

The service bump is only buildable in Docker/CI after the packages are on GitHub Packages.

```
code: props + bumps + refs → make pack-shared → local build/test (local feed)
  → ASK-GATED: Sam runs `make publish-shared` (PAT with write:packages)
  → Sam checks 5 package pages: repo link + access
  → local docker build restores from GitHub (no local feed)
  → PR → /review → /test (merge)
  → /release v1.0.1 (release note, ask-gated tag) → images.yml builds 6 images
```

- `publish-shared` pushes everything in `local-nuget-feed/` with `--skip-duplicate`. Old versions are skipped; only the 5 new ones are uploaded.
- After publish, each package page should show `hunglu/WordBuddy` under "Repository". In "Package settings", "Inherit access from source repository" should be on; if not, Sam turns it on. With that, `GITHUB_TOKEN` of this repo gets read access.

### 5. Docs

- `images.yml` header: replace "each package grants this repository read access (Actions access)" with "each package is linked to this repository (RepositoryUrl in `src/Shared/Directory.Build.props`) and inherits its access".
- `WordBuddy/README.md` (shared-package note near line 193): state the new `Infrastructure` version and one line on linking: new shared versions carry `RepositoryUrl`; run `make publish-shared` before a release tag when a shared version changes.
- ADR 0010 is append-only and does not mention package access. No change.
- Living spec `docs/features/WB-75_...md`: tester updates line 21 (packages linked, restored with `GITHUB_TOKEN`) and line 25 (`Infrastructure` 1.4.1), moves WB-79 from "Pending changes" to "Change history".
- Release note `docs/releases/v1.0.1.md`: written by `/release`, not by `/code`.

### 6. Proposed `WordBuddy/CLAUDE.md` text (ask-gated, Sam applies)

Replace lines 170-171:

> Known gap: the GitHub Packages restore inside `docker build` has not been verified end-to-end
> with a real PAT (`read:packages`; `write:packages` for `make publish-shared`).

with:

> Shared packages carry `RepositoryUrl` (`src/Shared/Directory.Build.props`), so GitHub links
> them to `hunglu/WordBuddy`. CI (`images.yml`) restores them with `GITHUB_TOKEN`; local
> `make up` / kind builds use a PAT (`read:packages`; `write:packages` for `make publish-shared`).

Apply only after `images.yml` is green on `v1.0.1`.

## Frontend approach

None.

## Data / migration notes

None. No schema change, no EF Core migration.

## Child vs. adult

No difference. Build and release fix only; no runtime behaviour changes for either `AgeGroup`.

## Open questions

1. **If CI still gets 403 after linking.** One plausible cause: without `packageSourceMapping`, NuGet asks the GitHub feed for every package id (MassTransit, Serilog, ...). GitHub may answer 403 for ids it does not hold. Recommended first fallback, as a small follow-up commit on this branch: add `packageSourceMapping` to the 5 service `nuget.config`s (`WordBuddy.Shared.*` → `local-wordbuddy` + `github-wordbuddy`, `*` → `nuget.org`). Not in tasks by default; Sam decides. A stored PAT (Option B) stays out per the constraint.
2. **Drift cleanup.** Plan moves Quiz (Kernel 1.0.1), Content (Contracts 1.3.0) and Notification (Infrastructure 1.0.2) to the new versions. Alternative: bump only the current-version references. Recommended: move all, so CI never needs an unlinked version. If a jump needs code changes, the coder stops and asks.
3. **Existing package-level link.** GitHub links the whole package (all versions) when a version with `RepositoryUrl` is pushed. Old versions should then also be readable. This plan does not rely on it; it is a side benefit.
4. **Verifying before the tag.** `images.yml` also supports `workflow_dispatch`. Sam may trigger it by hand on `main` after the merge (ask-gated push not needed) to confirm green before tagging `v1.0.1`.
