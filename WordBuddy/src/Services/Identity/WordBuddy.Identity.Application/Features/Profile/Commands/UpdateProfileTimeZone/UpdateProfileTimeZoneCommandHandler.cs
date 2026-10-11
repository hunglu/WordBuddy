using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileTimeZone;

/// <summary>Updates the time zone and stages one <c>LearnerProfileUpdated</c> with the change.</summary>
public sealed class UpdateProfileTimeZoneCommandHandler : ICommandHandler<UpdateProfileTimeZoneCommand, UserDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly ILearnerProfileEventPublisher _profileEvents;
    private readonly TimeProvider _time;
    private readonly IValidator<UpdateProfileTimeZoneCommand> _validator;
    private readonly ILogger<UpdateProfileTimeZoneCommandHandler> _logger;

    public UpdateProfileTimeZoneCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        ILearnerProfileEventPublisher profileEvents,
        TimeProvider time,
        IValidator<UpdateProfileTimeZoneCommand> validator,
        ILogger<UpdateProfileTimeZoneCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _profileEvents = profileEvents;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<UserDto>> HandleAsync(UpdateProfileTimeZoneCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("UpdateProfileTimeZoneCommand started: UserId={UserId}", command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdateProfileTimeZoneCommand validation failed: UserId={UserId}", command.UserId);
            return Result.Failure<UserDto>(Error.Validation("UpdateProfileTimeZone.Validation", validation.ToString()));
        }

        Result<User> found = await _users.GetTrackedByIdAsync(command.UserId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<UserDto>(found.Error);
        }

        User user = found.Value;
        Result set = user.SetTimeZone(command.TimeZoneId);
        if (set.IsFailure)
        {
            _logger.LogWarning(
                "UpdateProfileTimeZoneCommand rejected: UserId={UserId}, ErrorCode={ErrorCode}", command.UserId, set.Error.Code);
            return Result.Failure<UserDto>(set.Error);
        }

        await _profileEvents.PublishAsync(user, _time.GetUtcNow().UtcDateTime, ct);

        Result save = await _users.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return Result.Failure<UserDto>(save.Error);
        }

        bool hasActiveSupporter = await _links.HasActiveSupporterAsync(user.Id, ct);
        _logger.LogInformation("UpdateProfileTimeZoneCommand succeeded: UserId={UserId}", command.UserId);
        return Result.Success(UserDto.From(user, hasActiveSupporter));
    }
}
