using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.JoinChallenge;

/// <summary>
/// Joins a challenge. Order of checks: challenge exists, audience matches, status Open or Running, child has Guardian
/// consent, room left (<see cref="ChallengeErrors.Full"/>, no overflow). Joining twice is a success. Deletes the board cache.
/// </summary>
public sealed class JoinChallengeCommandHandler : ICommandHandler<JoinChallengeCommand>
{
    private readonly IChallengeRepository _challenges;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<JoinChallengeCommand> _validator;
    private readonly ILogger<JoinChallengeCommandHandler> _logger;

    public JoinChallengeCommandHandler(
        IChallengeRepository challenges,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<JoinChallengeCommand> validator,
        ILogger<JoinChallengeCommandHandler> logger)
    {
        _challenges = challenges;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(JoinChallengeCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("JoinChallengeCommand started: ChallengeId={ChallengeId}, UserId={UserId}", command.ChallengeId, command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("JoinChallengeCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("JoinChallenge.Validation", validation.ToString()));
        }

        Result<Challenge> found = await _challenges.GetByIdAsync(command.ChallengeId, ct);
        if (found.IsFailure)
        {
            return Result.Failure(found.Error);
        }

        Challenge challenge = found.Value;
        ChallengeAudience audience = ChallengeAudiences.From(command.AgeGroup);
        Result joinable = challenge.CheckJoinable(audience);
        if (joinable.IsFailure)
        {
            _logger.LogWarning(
                "JoinChallengeCommand rejected: ChallengeId={ChallengeId}, ErrorCode={ErrorCode}", command.ChallengeId, joinable.Error.Code);
            return joinable;
        }

        if (audience == ChallengeAudience.Child && !await _challenges.HasConsentAsync(command.UserId, ct))
        {
            _logger.LogWarning("JoinChallengeCommand rejected: child without consent, UserId={UserId}", command.UserId);
            return Result.Failure(ChallengeErrors.NoConsent);
        }

        Result<ChallengeParticipant> existing = await _challenges.GetParticipantTrackedAsync(challenge.Id, command.UserId, ct);
        if (existing.IsSuccess)
        {
            return Result.Success();
        }

        ChallengeParticipant participant = ChallengeParticipant.Create(
            Guid.NewGuid(), challenge.Id, command.UserId, _time.GetUtcNow().UtcDateTime);

        Result added = await _challenges.AddParticipantIfRoomAsync(participant, challenge.Capacity, ct);
        if (added.IsFailure)
        {
            _logger.LogWarning(
                "JoinChallengeCommand failed: ChallengeId={ChallengeId}, ErrorCode={ErrorCode}", command.ChallengeId, added.Error.Code);
            return added;
        }

        await ChallengeCache.InvalidateBoardsAsync(_cache, [challenge.Id], ct);
        _logger.LogInformation("JoinChallengeCommand succeeded: ChallengeId={ChallengeId}, UserId={UserId}", command.ChallengeId, command.UserId);
        return Result.Success();
    }
}
