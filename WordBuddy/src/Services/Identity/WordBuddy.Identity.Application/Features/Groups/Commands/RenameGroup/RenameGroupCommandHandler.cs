using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RenameGroup;

/// <summary>Renames a group. Only the owner may. Logs ids only, never the name.</summary>
public sealed class RenameGroupCommandHandler : ICommandHandler<RenameGroupCommand>
{
    private readonly ILearnerGroupRepository _groups;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<RenameGroupCommand> _validator;
    private readonly ILogger<RenameGroupCommandHandler> _logger;

    public RenameGroupCommandHandler(
        ILearnerGroupRepository groups,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<RenameGroupCommand> validator,
        ILogger<RenameGroupCommandHandler> logger)
    {
        _groups = groups;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RenameGroupCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("RenameGroupCommand started: GroupId={GroupId}, CallerId={CallerId}", command.GroupId, command.CallerId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RenameGroupCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RenameGroup.Validation", validation.ToString()));
        }

        Result<LearnerGroup> found = await _groups.GetGroupTrackedAsync(command.GroupId, ct);
        if (found.IsFailure)
        {
            return found;
        }

        LearnerGroup group = found.Value;
        if (group.OwnerId != command.CallerId)
        {
            _logger.LogWarning("RenameGroupCommand rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", group.Id, LearnerGroupErrors.NotOwner.Code);
            return Result.Failure(LearnerGroupErrors.NotOwner);
        }

        Result renamed = group.Rename(command.Name, _time.GetUtcNow().UtcDateTime);
        if (renamed.IsFailure)
        {
            return renamed;
        }

        Result save = await _groups.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("RenameGroupCommand failed to persist: {ErrorCode}", save.Error.Code);
            return save;
        }

        await LearnerGroupCache.InvalidateAsync(_cache, [group.Id], ct);
        _logger.LogInformation("RenameGroupCommand succeeded: GroupId={GroupId}", group.Id);
        return Result.Success();
    }
}
