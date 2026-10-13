using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.ListChallenges;

/// <summary>
/// Lists the challenges of the caller audience only. A child without Guardian consent gets an empty list
/// (not an error), so the UI can show "Ask your guardian". Logs ids and counts only.
/// </summary>
public sealed class ListChallengesQueryHandler : IQueryHandler<ListChallengesQuery, IReadOnlyList<ChallengeDto>>
{
    private readonly IChallengeRepository _challenges;
    private readonly IValidator<ListChallengesQuery> _validator;
    private readonly ILogger<ListChallengesQueryHandler> _logger;

    public ListChallengesQueryHandler(
        IChallengeRepository challenges,
        IValidator<ListChallengesQuery> validator,
        ILogger<ListChallengesQueryHandler> logger)
    {
        _challenges = challenges;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ChallengeDto>>> HandleAsync(ListChallengesQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("ListChallengesQuery started: UserId={UserId}, AgeGroup={AgeGroup}", query.UserId, query.AgeGroup);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("ListChallengesQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<IReadOnlyList<ChallengeDto>>(Error.Validation("ListChallenges.Validation", validation.ToString()));
        }

        ChallengeAudience audience = ChallengeAudiences.From(query.AgeGroup);
        if (audience == ChallengeAudience.Child && !await _challenges.HasConsentAsync(query.UserId, ct))
        {
            _logger.LogInformation("ListChallengesQuery: child without consent, UserId={UserId}", query.UserId);
            return Result.Success<IReadOnlyList<ChallengeDto>>([]);
        }

        IReadOnlyList<Challenge> challenges = await _challenges.GetActiveAsync(audience, ct);
        List<Guid> ids = [.. challenges.Select(c => c.Id)];
        IReadOnlyDictionary<Guid, int> counts = await _challenges.GetParticipantCountsAsync(ids, ct);
        Dictionary<Guid, ChallengeParticipant> mine =
            (await _challenges.GetParticipationsAsync(query.UserId, ids, ct)).ToDictionary(p => p.ChallengeId);

        List<ChallengeDto> result = [];
        foreach (Challenge c in challenges)
        {
            bool joined = mine.TryGetValue(c.Id, out ChallengeParticipant? participant);
            result.Add(new ChallengeDto(
                c.Id, c.TemplateKey, ChallengeTemplates.NameOf(c.TemplateKey), c.Audience, c.Kind, c.Metric, c.Target, c.Status,
                c.OpensAtUtc, c.StartsAtUtc, c.EndsAtUtc, c.Capacity, counts.GetValueOrDefault(c.Id), joined,
                joined ? participant!.Score : null, joined ? participant!.Rank : null));
        }

        _logger.LogInformation("ListChallengesQuery succeeded: UserId={UserId}, Count={Count}", query.UserId, result.Count);
        return Result.Success<IReadOnlyList<ChallengeDto>>(result);
    }
}
