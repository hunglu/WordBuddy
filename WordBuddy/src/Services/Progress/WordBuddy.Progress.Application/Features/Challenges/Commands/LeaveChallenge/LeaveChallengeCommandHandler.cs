using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.LeaveChallenge;

/// <summary>Removes the caller participant row. Leaving a challenge that was not joined is a success. A finished challenge keeps its result.</summary>
public sealed class LeaveChallengeCommandHandler : ICommandHandler<LeaveChallengeCommand>
{
    private readonly IChallengeRepository _challenges;
    private readonly IDistributedCache _cache;
    private readonly IValidator<LeaveChallengeCommand> _validator;
    private readonly ILogger<LeaveChallengeCommandHandler> _logger;

    public LeaveChallengeCommandHandler(
        IChallengeRepository challenges,
        IDistributedCache cache,
        IValidator<LeaveChallengeCommand> validator,
        ILogger<LeaveChallengeCommandHandler> logger)
    {
        _challenges = challenges;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(LeaveChallengeCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("LeaveChallengeCommand started: ChallengeId={ChallengeId}, UserId={UserId}", command.ChallengeId, command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("LeaveChallengeCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("LeaveChallenge.Validation", validation.ToString()));
        }

        Result<Challenge> found = await _challenges.GetByIdAsync(command.ChallengeId, ct);
        if (found.IsFailure)
        {
            return Result.Failure(found.Error);
        }

        if (found.Value.Status == ChallengeStatus.Finished)
        {
            return Result.Failure(ChallengeErrors.NotOpen);
        }

        Result<ChallengeParticipant> participant = await _challenges.GetParticipantTrackedAsync(command.ChallengeId, command.UserId, ct);
        if (participant.IsFailure)
        {
            return Result.Success();
        }

        _challenges.RemoveParticipant(participant.Value);
        Result save = await _challenges.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("LeaveChallengeCommand failed to save: ErrorCode={ErrorCode}", save.Error.Code);
            return save;
        }

        await ChallengeCache.InvalidateBoardsAsync(_cache, [command.ChallengeId], ct);
        _logger.LogInformation("LeaveChallengeCommand succeeded: ChallengeId={ChallengeId}, UserId={UserId}", command.ChallengeId, command.UserId);
        return Result.Success();
    }
}
