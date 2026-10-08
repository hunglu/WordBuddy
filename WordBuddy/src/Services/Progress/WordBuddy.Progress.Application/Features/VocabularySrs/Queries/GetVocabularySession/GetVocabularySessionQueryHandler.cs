using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySession;

/// <summary>
/// Due words first (earliest first, up to <see cref="VocabularySchedulingOptions.MaxDueItems"/>), then
/// new words up to the cap minus the new words already introduced in the client's day. Same rule
/// for child and adult (D-3). Not cached: the result changes after every answer.
/// </summary>
public sealed class GetVocabularySessionQueryHandler : IQueryHandler<GetVocabularySessionQuery, VocabularySessionDto>
{
    private readonly ILearnerWordStateRepository _states;
    private readonly IVocabularyLearnerSettingsRepository _settings;
    private readonly NewWordCapPolicy _capPolicy;
    private readonly VocabularySchedulingOptions _schedulingOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GetVocabularySessionQueryHandler> _logger;

    public GetVocabularySessionQueryHandler(
        ILearnerWordStateRepository states,
        IVocabularyLearnerSettingsRepository settings,
        NewWordCapPolicy capPolicy,
        VocabularySchedulingOptions schedulingOptions,
        TimeProvider timeProvider,
        ILogger<GetVocabularySessionQueryHandler> logger)
    {
        _states = states;
        _settings = settings;
        _capPolicy = capPolicy;
        _schedulingOptions = schedulingOptions;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<VocabularySessionDto>> HandleAsync(GetVocabularySessionQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetVocabularySessionQuery started: UserId={UserId}", query.UserId);

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        Result<TimeSpan> offsetResult = ClientDateTime.ParseOffset(query.ClientCurrentDateTime, nowUtc);
        if (offsetResult.IsFailure)
        {
            _logger.LogWarning("GetVocabularySessionQuery invalid client date-time header: UserId={UserId}", query.UserId);
            return Result.Failure<VocabularySessionDto>(offsetResult.Error);
        }

        Result<int> dueCount = await _states.CountDueAsync(query.UserId, nowUtc, ct);
        if (dueCount.IsFailure)
        {
            return Result.Failure<VocabularySessionDto>(dueCount.Error);
        }

        Result<IReadOnlyList<LearnerWordState>> due = await _states.GetDueAsync(query.UserId, nowUtc, _schedulingOptions.MaxDueItems, ct);
        if (due.IsFailure)
        {
            return Result.Failure<VocabularySessionDto>(due.Error);
        }

        Result<VocabularyLearnerSettings> settings = await _settings.GetAsync(query.UserId, ct);
        if (settings.IsFailure && settings.Error.Type != ErrorType.NotFound)
        {
            return Result.Failure<VocabularySessionDto>(settings.Error);
        }

        int? newWordsPerDay = settings.IsSuccess ? settings.Value.NewWordsPerDay : null;
        int? supporterCap = settings.IsSuccess ? settings.Value.SupporterNewWordCap : null;
        int cap = _capPolicy.GetCap(dueCount.Value, supporterCap, newWordsPerDay);

        (DateTime todayStartUtc, DateTime todayEndUtc) = ClientDateTime.TodayUtcRange(nowUtc, offsetResult.Value);
        Result<int> introducedToday = await _states.CountFirstReviewedBetweenAsync(query.UserId, todayStartUtc, todayEndUtc, ct);
        if (introducedToday.IsFailure)
        {
            return Result.Failure<VocabularySessionDto>(introducedToday.Error);
        }

        int remaining = Math.Max(0, cap - introducedToday.Value);
        Result<IReadOnlyList<LearnerWordState>> newStates = await _states.GetNewInAddedOrderAsync(query.UserId, remaining, ct);
        if (newStates.IsFailure)
        {
            return Result.Failure<VocabularySessionDto>(newStates.Error);
        }

        VocabularySessionDto session = new(
            Guid.NewGuid(),
            due.Value.Select(ToItem).ToList(),
            newStates.Value.Select(ToItem).ToList(),
            cap,
            introducedToday.Value);

        _logger.LogInformation(
            "GetVocabularySessionQuery succeeded: UserId={UserId}, SessionId={SessionId}, DueCount={DueCount}, NewCount={NewCount}, Cap={Cap}",
            query.UserId, session.SessionId, session.DueItems.Count, session.NewItems.Count, cap);

        return Result.Success(session);
    }

    private static VocabularySessionItemDto ToItem(LearnerWordState state) => new(state.SenseId, state.Status, state.DueAtUtc);
}
