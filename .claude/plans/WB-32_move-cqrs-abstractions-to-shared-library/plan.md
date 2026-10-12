# Plan: Move CQRS abstractions to shared library

## Summary

Create a new NuGet package `WordBuddy.Shared.Application` (1.0.0) that holds `ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler<>` and `IQueryHandler<,>`. The Application projects of Identity, Content, Quiz and Progress reference this package and delete their local copies. Behaviour does not change.

## Decision: new `WordBuddy.Shared.Application` package, not `Shared.Kernel`

| Option | Domain impact | Verdict |
| --- | --- | --- |
| Add to `Shared.Kernel` | Every Domain project references Kernel, so Domain would see CQRS types. This breaks the layer rule. | Rejected |
| New `Shared.Application` | Only Application projects reference it. Domain still sees only `Result`/`Error`/base entity. | **Chosen** |

Reason: the CQRS interfaces belong to the Application layer. The proposal states that Domain must not gain a dependency it does not need.

```
Shared.Kernel  ←  Shared.Application  (package dependency, for Result<T>)
      ↑                    ↑
   <Svc>.Domain      <Svc>.Application
```

## Affected services / areas

- `src/Shared/`: new project `WordBuddy.Shared.Application`, added to `WordBuddy.Shared.slnx`.
- Identity, Content, Quiz, Progress: Application `.csproj`, `Abstractions/` (3 files deleted), and `using` lines in handlers, commands/queries, DI registration, decorators, controllers and tests.
- `Makefile` `publish-shared` target (pack + push the new package).
- `.claude/skills/develop-webapi/SKILL.md`: namespace update only.
- `WordBuddy/CLAUDE.md` contains the line "`ICommandHandler`/`IQueryHandler` live in `Application/Abstractions/`". That file is ask-gated, so the coder proposes the text change to Sam and does not edit it.
- Notification: out of scope.

## Backend approach

1. **New package** `src/Shared/WordBuddy.Shared.Application/`
   - `net10.0`, `PackageId WordBuddy.Shared.Application`, `Version 1.0.0`.
   - `PackageReference WordBuddy.Shared.Kernel` (current version 1.0.1). Do not use a `ProjectReference`, so the package can be consumed on its own.
   - Namespace `WordBuddy.Shared.Application.Abstractions`.
   - Content is copied 1:1 from the existing service copies, plus XML doc comments.
2. **Check that copies match.** Before deleting, diff the 4 services' `Abstractions/` files. If any copy differs, for example in the `ct` default or variance, stop and raise it. Do not silently unify.
3. **Per service** (Identity, Content, Quiz, Progress):
   - Add `PackageReference WordBuddy.Shared.Application 1.0.0` to `<Svc>.Application.csproj`.
   - Delete `Abstractions/ICommand.cs`, `ICommandHandler.cs`, `IQueryHandler.cs`.
   - Replace `using WordBuddy.<Svc>.Application.Abstractions;` with `using WordBuddy.Shared.Application.Abstractions;`. Where other types remain in the service's `Abstractions` namespace (repositories, clients), keep both usings. A global using in the Application project is acceptable if it reduces churn.
   - DI registration (`ServiceCollectionExtensions`) and the validation decorator keep the same shape. Only the namespace changes.
4. **Versioning / feeds:** `dotnet pack` to `local-nuget-feed/` for local builds. Sam runs `make publish-shared` (token needed) before any Docker or CI build. No Kernel version bump is needed because Kernel does not change.
5. **Dockerfiles:** no change expected, because restore pulls from the same feeds. Verify each service's `nuget.config` source mapping. If a pattern lists `WordBuddy.Shared.*`, nothing needs to change. If it lists packages by name, add the new package.

No new entities, endpoints, caching or messaging.

## Frontend approach

None.

## Data / migration notes

None.

## Child vs adult

No impact. Authorization policies and child restrictions do not change.

## Open questions

- If the 4 copies differ, which signature wins? This is answered at task 2. Default: the Content copy, which matches the `develop-webapi` skill.
- Scope idea (not in this change): also move the validation decorator into `Shared.Application`. See the final report.
