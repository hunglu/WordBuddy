using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.SetChildChallengeConsent;

/// <summary>
/// Needs BOTH: the caller holds the Guardian role (local profile) AND has an active support link to the child.
/// Else <see cref="ChallengeErrors.NotGuardian"/> / <see cref="ChallengeErrors.NoActiveLink"/>. Switching off removes
/// every consent of the child and the child participations in unfinished challenges.
/// </summary>
public sealed class SetChildChallengeConsentCommandHandler : ICommandHandler<SetChildChallengeConsentCommand>
{
    private readonly IChallengeRepository _challenges;
    private readonly IChallengeProfileRepository _profiles;
    private readonly ISupportLinkProjectionRepository _links;
    private readonly IChallengeConsentCleaner _cleaner;
    private readonly TimeProvider _time;
    private readonly IValidator<SetChildChallengeConsentCommand> _validator;
    private readonly ILogger<SetChildChallengeConsentCommandHandler> _logger;

    public SetChildChallengeConsentCommandHandler(
        IChallengeRepository challenges,
        IChallengeProfileRepository profiles,
        ISupportLinkProjectionRepository links,
        IChallengeConsentCleaner cleaner,
        TimeProvider time,
        IValidator<SetChildChallengeConsentCommand> validator,
        ILogger<SetChildChallengeConsentCommandHandler> logger)
    {
        _challenges = challenges;
        _profiles = profiles;
        _links = links;
        _cleaner = cleaner;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(SetChildChallengeConsentCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "SetChildChallengeConsentCommand started: CallerId={CallerId}, ChildId={ChildId}, Enabled={Enabled}",
            command.CallerId, command.ChildId, command.Enabled);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("SetChildChallengeConsentCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("SetChildChallengeConsent.Validation", validation.ToString()));
        }

        Result<ChallengeProfile> caller = await _profiles.GetAsync(command.CallerId, ct);
        if (caller.IsFailure || !caller.Value.IsGuardian)
        {
            _logger.LogWarning("SetChildChallengeConsentCommand rejected: caller is not a guardian, CallerId={CallerId}", command.CallerId);
            return Result.Failure(ChallengeErrors.NotGuardian);
        }

        if (!await _links.HasActiveLinkAsync(command.CallerId, command.ChildId, ct))
        {
            _logger.LogWarning(
                "SetChildChallengeConsentCommand rejected: no active link, CallerId={CallerId}, ChildId={ChildId}", command.CallerId, command.ChildId);
            return Result.Failure(ChallengeErrors.NoActiveLink);
        }

        Result<ChallengeProfile> child = await _profiles.GetAsync(command.ChildId, ct);
        if (child.IsFailure || child.Value.AgeGroup != AgeGroup.Child)
        {
            _logger.LogWarning("SetChildChallengeConsentCommand rejected: not a child, ChildId={ChildId}", command.ChildId);
            return Result.Failure(ChallengeErrors.NotChild);
        }

        if (!command.Enabled)
        {
            return await _cleaner.RemoveAllForChildAsync(command.ChildId, ct);
        }

        IReadOnlyList<ChallengeConsent> mine = await _challenges.GetConsentsTrackedAsync(command.ChildId, command.CallerId, ct);
        if (mine.Count == 0)
        {
            await _challenges.AddConsentAsync(
                ChallengeConsent.Create(command.ChildId, command.CallerId, _time.GetUtcNow().UtcDateTime), ct);
            Result save = await _challenges.SaveChangesAsync(ct);
            if (save.IsFailure)
            {
                _logger.LogWarning("SetChildChallengeConsentCommand failed to save: ErrorCode={ErrorCode}", save.Error.Code);
                return save;
            }
        }

        _logger.LogInformation("SetChildChallengeConsentCommand succeeded: ChildId={ChildId}", command.ChildId);
        return Result.Success();
    }
}
