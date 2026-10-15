using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.SetModeratorRole;

/// <summary>Sets the Moderator flag. No event: no other service needs it. The token shows it after the next sign-in.</summary>
public sealed class SetModeratorRoleCommandHandler : ICommandHandler<SetModeratorRoleCommand>
{
    private readonly IUserRepository _users;
    private readonly IValidator<SetModeratorRoleCommand> _validator;
    private readonly ILogger<SetModeratorRoleCommandHandler> _logger;

    public SetModeratorRoleCommandHandler(
        IUserRepository users,
        IValidator<SetModeratorRoleCommand> validator,
        ILogger<SetModeratorRoleCommandHandler> logger)
    {
        _users = users;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(SetModeratorRoleCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "SetModeratorRoleCommand started: AdminId={AdminId}, TargetUserId={TargetUserId}, IsModerator={IsModerator}",
            command.AdminId, command.TargetUserId, command.IsModerator);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("SetModeratorRoleCommand validation failed: TargetUserId={TargetUserId}", command.TargetUserId);
            return Result.Failure(Error.Validation("SetModeratorRole.Validation", validation.ToString()));
        }

        Result<User> found = await _users.GetTrackedByIdAsync(command.TargetUserId, ct);
        if (found.IsFailure)
        {
            _logger.LogWarning("SetModeratorRoleCommand: user not found, TargetUserId={TargetUserId}", command.TargetUserId);
            return Result.Failure(found.Error);
        }

        Result set = found.Value.SetModerator(command.IsModerator);
        if (set.IsFailure)
        {
            _logger.LogWarning(
                "SetModeratorRoleCommand rejected: TargetUserId={TargetUserId}, ErrorCode={ErrorCode}", command.TargetUserId, set.Error.Code);
            return set;
        }

        Result save = await _users.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return save;
        }

        _logger.LogInformation("SetModeratorRoleCommand succeeded: TargetUserId={TargetUserId}", command.TargetUserId);
        return Result.Success();
    }
}
