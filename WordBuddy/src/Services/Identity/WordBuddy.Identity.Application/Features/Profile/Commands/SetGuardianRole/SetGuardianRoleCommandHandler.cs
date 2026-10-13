using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.SetGuardianRole;

/// <summary>Sets the Guardian flag and stages <c>LearnerProfileUpdated</c> so Progress can drop consents on removal.</summary>
public sealed class SetGuardianRoleCommandHandler : ICommandHandler<SetGuardianRoleCommand>
{
    private readonly IUserRepository _users;
    private readonly ILearnerProfileEventPublisher _profileEvents;
    private readonly TimeProvider _time;
    private readonly IValidator<SetGuardianRoleCommand> _validator;
    private readonly ILogger<SetGuardianRoleCommandHandler> _logger;

    public SetGuardianRoleCommandHandler(
        IUserRepository users,
        ILearnerProfileEventPublisher profileEvents,
        TimeProvider time,
        IValidator<SetGuardianRoleCommand> validator,
        ILogger<SetGuardianRoleCommandHandler> logger)
    {
        _users = users;
        _profileEvents = profileEvents;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(SetGuardianRoleCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "SetGuardianRoleCommand started: AdminId={AdminId}, TargetUserId={TargetUserId}, IsGuardian={IsGuardian}",
            command.AdminId, command.TargetUserId, command.IsGuardian);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("SetGuardianRoleCommand validation failed: TargetUserId={TargetUserId}", command.TargetUserId);
            return Result.Failure(Error.Validation("SetGuardianRole.Validation", validation.ToString()));
        }

        Result<User> found = await _users.GetTrackedByIdAsync(command.TargetUserId, ct);
        if (found.IsFailure)
        {
            _logger.LogWarning("SetGuardianRoleCommand: user not found, TargetUserId={TargetUserId}", command.TargetUserId);
            return Result.Failure(found.Error);
        }

        User user = found.Value;
        Result set = user.SetGuardian(command.IsGuardian);
        if (set.IsFailure)
        {
            _logger.LogWarning(
                "SetGuardianRoleCommand rejected: TargetUserId={TargetUserId}, ErrorCode={ErrorCode}", command.TargetUserId, set.Error.Code);
            return set;
        }

        await _profileEvents.PublishAsync(user, _time.GetUtcNow().UtcDateTime, ct);

        Result save = await _users.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return save;
        }

        _logger.LogInformation("SetGuardianRoleCommand succeeded: TargetUserId={TargetUserId}", command.TargetUserId);
        return Result.Success();
    }
}
