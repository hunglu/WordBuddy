using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.ApplyLearnerProfileEvent;

/// <summary>
/// Idempotent upsert by <c>UserId</c>. An event older than the stored state changes nothing (still a success).
/// When the Guardian role ended, every consent that guardian gave is deleted. Logs ids only.
/// </summary>
public sealed class ApplyLearnerProfileEventCommandHandler : ICommandHandler<ApplyLearnerProfileEventCommand>
{
    private readonly IChallengeProfileRepository _profiles;
    private readonly IChallengeConsentCleaner _cleaner;
    private readonly IValidator<ApplyLearnerProfileEventCommand> _validator;
    private readonly ILogger<ApplyLearnerProfileEventCommandHandler> _logger;

    public ApplyLearnerProfileEventCommandHandler(
        IChallengeProfileRepository profiles,
        IChallengeConsentCleaner cleaner,
        IValidator<ApplyLearnerProfileEventCommand> validator,
        ILogger<ApplyLearnerProfileEventCommandHandler> logger)
    {
        _profiles = profiles;
        _cleaner = cleaner;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(ApplyLearnerProfileEventCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("ApplyLearnerProfileEventCommand started: UserId={UserId}", command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("ApplyLearnerProfileEventCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("ApplyLearnerProfileEvent.Validation", validation.ToString()));
        }

        AgeGroup ageGroup = Enum.Parse<AgeGroup>(command.AgeGroup, ignoreCase: true);
        bool applied;
        bool guardianRoleEnded = false;

        Result<ChallengeProfile> existing = await _profiles.GetTrackedAsync(command.UserId, ct);
        if (existing.IsSuccess)
        {
            bool wasGuardian = existing.Value.IsGuardian;
            applied = existing.Value.Apply(
                ageGroup, command.Alias, command.AvatarId, command.DisplayName, command.TimeZoneId, command.IsGuardian, command.OccurredAtUtc);
            guardianRoleEnded = applied && wasGuardian && !command.IsGuardian;
        }
        else if (existing.Error.Type == ErrorType.NotFound)
        {
            await _profiles.AddAsync(
                ChallengeProfile.Create(
                    command.UserId, ageGroup, command.Alias, command.AvatarId, command.DisplayName, command.TimeZoneId, command.IsGuardian, command.OccurredAtUtc),
                ct);
            applied = true;
        }
        else
        {
            return Result.Failure(existing.Error);
        }

        if (applied)
        {
            Result save = await _profiles.SaveChangesAsync(ct);
            if (save.IsFailure)
            {
                _logger.LogWarning("ApplyLearnerProfileEventCommand failed to persist: ErrorCode={ErrorCode}", save.Error.Code);
                return save;
            }
        }

        if (guardianRoleEnded)
        {
            Result cleaned = await _cleaner.RemoveByGuardianAsync(command.UserId, ct);
            if (cleaned.IsFailure)
            {
                return cleaned;
            }
        }

        _logger.LogInformation("ApplyLearnerProfileEventCommand succeeded: UserId={UserId}, Applied={Applied}", command.UserId, applied);
        return Result.Success();
    }
}
