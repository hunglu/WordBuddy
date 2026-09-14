---
name: develop-webapi
description: Implements new backend functionality in any WordBuddy .NET 8 microservice (Identity, Content, Quiz, Progress, Notification) — new API endpoints, commands, queries, controllers, or full vertical features. Use this whenever the user asks to add/create/build an endpoint, controller, command, query, use case, or "API for X" in the backend, or to wire up a new domain concept (Lesson, Vocabulary, Grammar, DailyPhrase, Quiz, MediaAsset, LearnerProgress, User) end-to-end through Clean Architecture + CQRS. Also use when extending an existing feature with caching, validation, logging, messaging, or tests to match project conventions.
---

# Develop WordBuddy Web API

Scaffolds and wires a backend feature the way this codebase already does it: Clean Architecture
(Presentation → Application → Domain → Infrastructure), hand-rolled CQRS (no MediatR), `Result<T>`
instead of exceptions/null, FluentValidation called directly inside the handler, and structured
Serilog logging at fixed points in the request lifecycle. Follow the root `CLAUDE.md` for anything
not covered here — this skill exists to make the repeatable parts (folder layout, interfaces,
logging calls, controller wiring) consistent across every feature and every service.

## Before writing anything

1. **Pick the service.** Each domain concept lives in exactly one service:
   `Identity` (users/auth), `Content` (Lesson/Vocabulary/Grammar/DailyPhrase/MediaAsset),
   `Quiz`, `Progress` (LearnerProgress), `Notification`. Don't cross-reference another
   service's DB — talk to it over HTTP (resilient client) or async events (MassTransit).
2. **Read the existing pattern first.** If the service already has one feature implemented
   (e.g. `Features/Lessons/Queries/GetLessonDetail/`), read its Command/Query, Handler,
   Validator, and the controller action that calls it — new features must look like copies
   of that shape with different names, not a reinvention. Only fall back to the templates
   below when nothing comparable exists yet.
3. **Decide Command vs Query.** Mutation → Command (`Commands/<Verb><Noun>/`). Read → Query
   (`Queries/<Verb><Noun>/`). Never mix reads and writes in one handler.
4. **DotNet Solution** 
  - Make sure the new feature's project is already in the solution and has the right references:
   - Presentation → Application, Infrastructure
   - Application → Domain
   - Infrastructure → Application
   - Tests → Application, Domain, Infrastructure
  - Make sure DotNet project using the correct target framework (net10.0) and has the right NuGet packages installed:
   - Microsoft.EntityFrameworkCore.SqlServer
   - Microsoft.EntityFrameworkCore.Tools
   - FluentValidation
   - xUnit + FluentAssertions (for tests)
  - Make sure the project file should not use minimal APIs. Use explicit `Program.cs` and `Startup.cs` if the service already has them and Controllers should be class-based, not minimal API lambdas.
5. **Serilog Logging**
  - The configuration should place in appsettings.json and appsettings.Development.json, not in code. 
  - Use `ILogger<T>` from Microsoft.Extensions.Logging in every handler and controller, never use ISerilog interfaces directly. 
  - Inject `ILogger<T>` into every handler and controller.
  - Log at fixed points: entry, validation failure, repository failure, success.
  - Use structured message templates (`"CreateLessonCommand started: Title={Title}, Type={Type}"`), never string interpolation.
  - Never log passwords, tokens, or raw child-user PII.
6. **Project Folder Structure** 
  - Root 
     |-- WordBuddy - Backend solution folder, it recognizes as the root folder of Backend solution.
     |
     |-- WordBuddy.UI - Frontend solution folder, it regconizes as the root folder of Frontend solution.
## Feature folder layout

Inside `<Service>.Application/Features/<Area>/`:

```
Commands/
  CreateLesson/
    CreateLessonCommand.cs
    CreateLessonCommandHandler.cs
    CreateLessonCommandValidator.cs
Queries/
  GetLessonById/
    GetLessonByIdQuery.cs
    GetLessonByIdQueryHandler.cs
    GetLessonByIdQueryValidator.cs   (optional — only if there's something to validate, e.g. a non-empty id)
```

One handler per command/query. DTOs returned to callers live in `Application/DTOs/`, not inside
the feature folder.

## Core interfaces (Application layer, define once per service under `Application/Abstractions/`)

CLAUDE.md forbids MediatR for dispatch, so there is no pipeline-behavior magic — validation is
called explicitly at the top of every handler. Use these small interfaces instead of a mediator:

```csharp
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct = default);
}

public interface ICommand { }
public interface ICommand<TResult> { }
public interface IQuery<TResult> { }
```

Controllers depend on the concrete handler interfaces via DI (`ICommandHandler<CreateLessonCommand, Guid>`),
never on a mediator/dispatcher object. Register each handler in the service's
`Application/Extensions/ServiceCollectionExtensions.AddApplication()` as `AddScoped<IFooHandler, Foo>()`.

## Templates

**Command / Query** — `sealed record`, implements the marker interface:

```csharp
public sealed record CreateLessonCommand(
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    AgeGroup TargetAgeGroup) : ICommand<Guid>;
```

**Validator** — FluentValidation, one per command/query, named `{Name}Validator`:

```csharp
public sealed class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty();
        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.Level).IsInEnum();
    }
}
```

**Handler** — validate first, log at fixed points, translate repository failure straight through,
never throw for an expected failure:

```csharp
public sealed class CreateLessonCommandHandler : ICommandHandler<CreateLessonCommand, Guid>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<CreateLessonCommand> _validator;
    private readonly ILogger<CreateLessonCommandHandler> _logger;

    public CreateLessonCommandHandler(
        ILessonRepository lessonRepository,
        IValidator<CreateLessonCommand> validator,
        ILogger<CreateLessonCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(CreateLessonCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CreateLessonCommand started: Title={Title}, Type={Type}, Level={Level}",
            command.Title, command.Type, command.Level);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateLessonCommand validation failed: {Errors}", validation.ToString());
            return Result<Guid>.Failure(Error.Validation("CreateLesson.Validation", validation.ToString()));
        }

        Guid id = Guid.NewGuid();
        Lesson lesson = new(id, command.Title, command.Description, command.Type, command.Level, command.TargetAgeGroup);

        Result addResult = await _lessonRepository.AddAsync(lesson, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "CreateLessonCommand failed to persist lesson: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result<Guid>.Failure(addResult.Error);
        }

        _logger.LogInformation("CreateLessonCommand succeeded: LessonId={LessonId}", id);
        return Result<Guid>.Success(id);
    }
}
```

Query handlers follow the same shape but read (via repository `GetXAsync`), and should check the
distributed cache before the repository and populate it after (see Caching below).

**Controller** — class-based, depends only on Application (handler interfaces + DTOs), maps
`Result<T>` to HTTP status via a shared helper, never touches Infrastructure or EF Core types:

```csharp
[ApiController]
[Route("api/lessons")]
[Authorize]
public sealed class LessonsController : ControllerBase
{
    private readonly ICommandHandler<CreateLessonCommand, Guid> _createLesson;
    private readonly IQueryHandler<GetLessonByIdQuery, LessonDetailDto> _getLessonById;

    public LessonsController(
        ICommandHandler<CreateLessonCommand, Guid> createLesson,
        IQueryHandler<GetLessonByIdQuery, LessonDetailDto> getLessonById)
    {
        _createLesson = createLesson;
        _getLessonById = getLessonById;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLessonRequest request, CancellationToken ct)
    {
        Result<Guid> result = await _createLesson.HandleAsync(
            new CreateLessonCommand(request.Title, request.Description, request.Type, request.Level, request.TargetAgeGroup), ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemResult(this);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        Result<LessonDetailDto> result = await _getLessonById.HandleAsync(new GetLessonByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }
}
```

`Result.ToProblemResult(controller)` is a small shared extension (put it in
`WordBuddy.Shared.Kernel` or the service's Presentation layer) that maps `Error.Code` prefixes to
status codes: `NotFound` → 404, `Conflict` → 409, `Validation`/`Failure` → 400, anything else → 500
via the global exception middleware. Write it once per solution and reuse it — don't hand-roll
`if/else` status mapping in every action.

## Result & Error pattern

`Result<T>` / `Result` and `Error` live in `Domain/Common/` (or `WordBuddy.Shared.Kernel` if
shared across services). `Error` is `public sealed record Error(string Code, string Description)`
with static factories: `Error.NotFound`, `Error.Validation`, `Error.Conflict`, `Error.Failure`.
Every application-layer method returns one of these — no `null`, no thrown exceptions for expected
outcomes (not-found, validation failure, duplicate). Reserve exceptions for truly unexpected faults,
caught only by the global exception middleware.

## Cross-cutting checklist

Work through this for any new command/query/endpoint — most features only need a subset:

- **Logging** — inject `ILogger<T>`, use message templates (never string interpolation).
  Handler entry → `LogInformation` with key inputs. Validation/business failure → `LogWarning`
  with the error code. Unhandled exception → caught by global middleware, logged `Error` with
  stack trace + correlation ID. Never log passwords, tokens, or raw child-user PII.
- **Caching** (query handlers on high-read/low-mutation content: Vocabulary, Grammar,
  DailyPhrase) — key pattern `{ServiceName}:{EntityName}:{Id}`, e.g. `content:lesson:42`;
  absolute expiry, no sliding expiry; on the matching command's success, delete the key
  (cache-aside invalidation) rather than trying to update it in place.
- **Tracing** — nothing to add by hand for inbound HTTP/EF Core/outbound HTTP; OpenTelemetry
  instrumentation is configured once at the host level. Just make sure any new outbound
  HTTP client is registered through `IHttpClientFactory` so it picks up the instrumentation
  and the resilience handler.
- **Messaging** — if the feature represents something another service needs to react to
  (e.g. a quiz being completed, a lesson being finished), publish a contract from
  `WordBuddy.Shared.Contracts` via MassTransit after the command's DB write succeeds, not
  before. Consumers must be idempotent — check whether the effect already happened before
  applying it again.
- **Authorization** — `[Authorize(Policy = "...")]` with a named policy, never a raw role
  string. If the endpoint returns or accepts content that differs for `AgeGroup.Child`,
  enforce that distinction via a policy/handler, not an `if` buried in the controller.
- **Rate limiting** — only touch `RateLimitingConfiguration` if the endpoint is auth or quiz
  submission (or the user asks for a new policy); everything else inherits the global policy.
- **Health checks** — only relevant if you're adding a new dependency (a new DB, cache, or
  downstream service call) that `GET /health/ready` should verify.

## Testing

- Unit test the handler with Moq for the repository/validator, xUnit + FluentAssertions for
  assertions. Name tests `{ClassUnderTest}_{Method}_{ExpectedOutcome}`, one `[Fact]`/`[Theory]`
  per scenario: happy path, validation failure, not-found/conflict from the repository.
- If the feature branches on `AgeGroup` (child vs adult), cover both paths explicitly.
- Add an integration test against the real API + SQL Server (via `WebApplicationFactory`) for
  the controller action itself — never mock EF Core in these.

## Common mistakes to avoid here

- Reaching for MediatR — this project deliberately hand-rolls the command/query interfaces above.
- Returning `null` or throwing for a "not found" / "already exists" case — always `Result.Failure`.
- Putting business logic in the controller — controllers only map request → command/query,
  call the handler, and map `Result` → HTTP response.
- String-interpolated log messages (`_logger.LogInformation($"...")`) — always use templates
  (`_logger.LogInformation("... {Prop}", value)`).
- Skipping the validator call because "the DTO already validates types" — FluentValidation is
  still where business rules (ranges, required combinations, cross-field checks) belong.
