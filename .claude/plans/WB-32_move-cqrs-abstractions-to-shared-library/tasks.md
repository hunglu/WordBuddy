# Tasks: Move CQRS abstractions to shared library

## Backend

- [x] Diff `Application/Abstractions/{ICommand,ICommandHandler,IQueryHandler}.cs` across Identity, Content, Quiz, Progress. Acceptance: the copies are identical apart from the namespace, or the difference is reported before you continue. — none (read-only; copies identical, Identity only has extra XML docs)
- [x] Create `src/Shared/WordBuddy.Shared.Application` (net10.0, PackageId, Version 1.0.0, PackageReference Kernel 1.0.1) and add it to `WordBuddy.Shared.slnx`. Acceptance: `dotnet build src/Shared/WordBuddy.Shared.slnx` passes. — Shared.Application.csproj, WordBuddy.Shared.slnx
- [x] Add `ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler<>` (both arities) and `IQueryHandler<,>` in namespace `WordBuddy.Shared.Application.Abstractions`, with XML docs. Acceptance: signatures match the existing copies. — WordBuddy/src/Shared/WordBuddy.Shared.Application/Abstractions/*.cs
- [x] Pack to `local-nuget-feed/`. Acceptance: `WordBuddy.Shared.Application.1.0.0.nupkg` is present. — local-nuget-feed (gitignored)
- [x] Update the `Makefile` `publish-shared` target to pack and push the new package. Acceptance: the target lists all 4 shared packages. — WordBuddy/Makefile (pack-shared; publish-shared already pushes every nupkg)
- [x] Identity: add the package reference, delete the 3 local files, and fix usings. Acceptance: the slnx builds and no `interface ICommand`/`IQuery` remains under the service. — Application csproj, Abstractions deleted, usings in all .cs
- [x] Content: same as Identity. Acceptance: same as Identity. — Application csproj, Abstractions deleted, usings in all .cs
- [x] Quiz: same as Identity. Acceptance: same as Identity. — Application csproj, Abstractions deleted, usings in all .cs
- [x] Progress: same as Identity. Acceptance: same as Identity. — Application csproj, Abstractions deleted, usings in all .cs
- [x] Check each service's `nuget.config` source mapping and Dockerfile restore for the new package id. Acceptance: the package resolves from both feeds; a change is made only if needed. — no change needed (no packageSourceMapping, Dockerfiles restore from same feeds)
- [x] Update the namespace shown in `.claude/skills/develop-webapi/SKILL.md`. Acceptance: the templates use `WordBuddy.Shared.Application.Abstractions`. — .claude/skills/develop-webapi/SKILL.md
- [x] Propose (do not edit) the `WordBuddy/CLAUDE.md` line change about where `ICommandHandler`/`IQueryHandler` live. Acceptance: the exact text is given to Sam in the `/code` report. — proposed in report

## Tests

- [x] Run `dotnet test` on all 4 service slnx files. Acceptance: all unit and integration tests pass with no test logic changes (usings only).
- [x] Add a unit test in each of the 4 services that resolves one registered command handler and one registered query handler from DI as the shared interface type, for example `ServiceCollectionExtensions_AddApplication_ResolvesSharedHandlerInterfaces`. Acceptance: green in all 4 services. — <Svc>.UnitTests/SharedCqrsAbstractionsTests.cs (registration check on descriptors, not full resolve)
- [x] Add a guard test in each service's UnitTests that reflects over the Application assembly and asserts that no type named `ICommand`, `IQuery`, `ICommandHandler` or `IQueryHandler` is declared there. Acceptance: green, and it fails if a local copy is re-added. — <Svc>.UnitTests/SharedCqrsAbstractionsTests.cs (registration check on descriptors, not full resolve)
- [ ] Run the existing `e2e/api` and `e2e/ui` suites. Acceptance: all pass.
