using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDisplayName;

/// <summary>Updates the display name and stages one <c>LearnerProfileUpdated</c>. Logs ids only, never the name.</summary>
public sealed class UpdateDisplayNameCommandHandler : ICommandHandler<UpdateDisplayNameCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly ILearnerProfileEventPublisher _profileEvents;
    private readonly TimeProvider _time;
    private readonly IValidator<UpdateDisplayNameCommand> _validator;
    private readonly ILogger<UpdateDisplayNameCommandHandler> _logger;

    public UpdateDisplayNameCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        ILearnerProfileEventPublisher profileEvents,
        TimeProvider time,
        IValidator<UpdateDisplayNameCommand> validator,
        ILogger<UpdateDisplayNameCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _profileEvents = profileEvents;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<UserDto>> HandleAsync(UpdateDisplayNameCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("UpdateDisplayNameCommand started: UserId={UserId}", command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdateDisplayNameCommand validation failed: UserId={UserId}", command.UserId);
            return Result.Failure<UserDto>(Error.Validation("UpdateDisplayName.Validation", validation.ToString()));
        }

        Result<User> found = await _users.GetTrackedByIdAsync(command.UserId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<UserDto>(found.Error);
        }

        User user = found.Value;
        Result set = user.SetDisplayName(command.DisplayName);
        if (set.IsFailure)
        {
            _logger.LogWarning(
                "UpdateDisplayNameCommand rejected: UserId={UserId}, ErrorCode={ErrorCode}", command.UserId, set.Error.Code);
            return Result.Failure<UserDto>(set.Error);
        }

        // The publisher drops the display name for a child.
        await _profileEvents.PublishAsync(user, _time.GetUtcNow().UtcDateTime, ct);

        Result save = await _users.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return Result.Failure<UserDto>(save.Error);
        }

        bool hasActiveSupporter = await _links.HasActiveSupporterAsync(user.Id, ct);
        _logger.LogInformation("UpdateDisplayNameCommand succeeded: UserId={UserId}", command.UserId);
        return Result.Success(UserDto.From(user, hasActiveSupporter, DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime)));
    }
}
