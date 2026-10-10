using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.CreateGroup;

/// <summary>Creates a group for an adult caller, within the group limit. Logs ids only, never the name.</summary>
public sealed class CreateGroupCommandHandler : ICommandHandler<CreateGroupCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly ILearnerGroupRepository _groups;
    private readonly LearnerGroupOptions _options;
    private readonly TimeProvider _time;
    private readonly IValidator<CreateGroupCommand> _validator;
    private readonly ILogger<CreateGroupCommandHandler> _logger;

    public CreateGroupCommandHandler(
        IUserRepository users,
        ILearnerGroupRepository groups,
        LearnerGroupOptions options,
        TimeProvider time,
        IValidator<CreateGroupCommand> validator,
        ILogger<CreateGroupCommandHandler> logger)
    {
        _users = users;
        _groups = groups;
        _options = options;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(CreateGroupCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("CreateGroupCommand started: OwnerId={OwnerId}", command.OwnerId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateGroupCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<Guid>(Error.Validation("CreateGroup.Validation", validation.ToString()));
        }

        Result<User> owner = await _users.GetByIdAsync(command.OwnerId, ct);
        if (owner.IsFailure)
        {
            return Result.Failure<Guid>(owner.Error);
        }

        int count = await _groups.CountGroupsByOwnerAsync(command.OwnerId, ct);
        Result allowed = LearnerGroupPolicy.CanCreateGroup(new LinkParty(owner.Value.Id, owner.Value.AgeGroup), count, _options.MaxGroupsPerOwner);
        if (allowed.IsFailure)
        {
            _logger.LogWarning("CreateGroupCommand rejected: OwnerId={OwnerId}, ErrorCode={ErrorCode}", command.OwnerId, allowed.Error.Code);
            return Result.Failure<Guid>(allowed.Error);
        }

        Result<LearnerGroup> created = LearnerGroup.Create(Guid.NewGuid(), command.OwnerId, command.Name, _time.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        await _groups.AddGroupAsync(created.Value, ct);
        Result save = await _groups.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("CreateGroupCommand failed to persist: {ErrorCode}", save.Error.Code);
            return Result.Failure<Guid>(save.Error);
        }

        _logger.LogInformation("CreateGroupCommand succeeded: GroupId={GroupId}", created.Value.Id);
        return Result.Success(created.Value.Id);
    }
}
