# Review: Move CQRS abstractions to shared library

PR: #45 · Round 1 · Reviewed commit: 1c9b6d6 · 2026-10-13T09:23:25+07:00

## Verdict
Approve — clean refactor; signatures match the old copies; no blockers or majors.

## Findings
| # | Severity | File:line | Finding | Suggested fix |
|---|---|---|---|---|
| 1 | nit | WordBuddy/CLAUDE.md (Architecture section) | Still says `ICommandHandler`/`IQueryHandler` live in `Application/Abstractions/`. New contributors will look in the wrong place. Ask-gated; text was proposed, not applied. | Sam applies the proposed line: interfaces come from the `WordBuddy.Shared.Application` package. |
| 2 | nit | e.g. WordBuddy/src/Services/Quiz/WordBuddy.Quiz.Application/Extensions/ServiceCollectionExtensions.cs:3 | The swapped `using WordBuddy.Shared.Application.Abstractions;` sits in the old slot among service usings, so ordering is mixed across ~220 files. Cosmetic only. | Optional: sort usings in a later cleanup. |
| 3 | nit | WordBuddy/src/Services/*/WordBuddy.*.UnitTests/SharedCqrsAbstractionsTests.cs:16 | Registration test checks descriptors only, not a real resolve (noted in tasks.md). It does not catch a missing dependency of a handler. | Acceptable for a refactor; integration tests cover real resolve. |

## Plan conformance
- All 16 tasks reflected in the diff. New package `WordBuddy.Shared.Application` 1.0.0 → Kernel 1.0.1 via PackageReference; Domain untouched.
- 3 abstraction files deleted in all 4 services; no `interface ICommand/IQuery` left under `src/Services`.
- Makefile `pack-shared` packs the new package; `publish-shared` already pushes every nupkg.
- Dockerfiles / nuget.config: no change needed (no source mapping).
- Line endings: changed blobs keep their original endings; one-line using swaps only.
- E2E: API 55/55, UI 35/36 accepted (env gap `SupportLinks__UnlinkOverrideWaitDays`).
- Out of scope: none. `publish-shared` must run before Docker/CI builds (Sam).
