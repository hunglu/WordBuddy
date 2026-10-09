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
/// for child and adult (D-3). Not cached: the result changes after every answer. Also records one
/// <see cref="VocabularySessionIssue"/> per non-empty session for the dashboard (WB-26). An open
/// session (not ended, not expired) is resumed with the same <c>SessionId</c> and its unanswered
/// items; a new session starts only after the old one ends.
/// </summary>
public sealed class GetVocabularySessionQueryHandler : IQueryHandler<GetVocabularySessionQuery, VocabularySessionDto>
{
    private readonly ILearnerWordStateRepository _states;
    private readonly IVocabularyLearnerSettingsRepository _settings;
    private readonly IVocabularySessionIssueRepository _sessionIssues;
    private readonly NewWordCapPolicy _capPolicy;
    private readonly VocabularySchedulingOptions _schedulingOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GetVocabularySessionQueryHandler> _logger;

    public GetVocabularySessionQueryHandler(
        ILearnerWordStateRepository states,
        IVocabularyLearnerSettingsRepository settings,
        IVocabularySessionIssueRepository sessionIssues,
        NewWordCapPolicy capPolicy,
        VocabularySchedulingOptions schedulingOptions,
        TimeProvider timeProvider,
        ILogger<GetVocabularySessionQueryHandler> logger)
    {
        _states = states;
        _settings = settings;
        _sessionIssues = sessionIssues;
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

        // Resume the open session if it still has unanswered items; close it when all are answered.
        bool endedOld = false;
        Result<VocabularySessionIssue> open = await _sessionIssues.GetOpenAsync(query.UserId, nowUtc, ct);
        if (open.IsFailure && open.Error.Type != ErrorType.NotFound)
        {
            return Result.Failure<VocabularySessionDto>(open.Error);
        }

        if (open.IsSuccess)
        {
            Result<VocabularySessionDto?> resumed = await TryResumeAsync(query.UserId, open.Value, cap, introducedToday.Value, ct);
            if (resumed.IsFailure)
            {
                return Result.Failure<VocabularySessionDto>(resumed.Error);
            }

            if (resumed.Value is not null)
            {
                _logger.LogInformation(
                    "GetVocabularySessionQuery succeeded: UserId={UserId}, SessionId={SessionId}, DueCount={DueCount}, NewCount={NewCount}, Cap={Cap}, Resumed={Resumed}",
                    query.UserId, resumed.Value.SessionId, resumed.Value.DueItems.Count, resumed.Value.NewItems.Count, cap, true);
                return Result.Success(resumed.Value);
            }

            open.Value.End(nowUtc);
            endedOld = true;
        }

        Result<IReadOnlyList<LearnerWordState>> due = await _states.GetDueAsync(query.UserId, nowUtc, _schedulingOptions.MaxDueItems, ct);
        if (due.IsFailure)
        {
            return Result.Failure<VocabularySessionDto>(due.Error);
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

        // The dashboard compares the planned size with the answers. An empty session has nothing to finish.
        List<(Guid SenseId, bool IsNew)> planned = due.Value.Select(s => (s.SenseId, false))
            .Concat(newStates.Value.Select(s => (s.SenseId, true)))
            .ToList();
        if (planned.Count > 0)
        {
            Result staged = await _sessionIssues.AddAsync(
                VocabularySessionIssue.Create(
                    session.SessionId, query.UserId, nowUtc, planned, _schedulingOptions.SessionDurationMinutes, todayEndUtc), ct);
            if (staged.IsFailure)
            {
                return Result.Failure<VocabularySessionDto>(staged.Error);
            }
        }

        if (planned.Count > 0 || endedOld)
        {
            Result saved = await _states.SaveChangesAsync(ct);
            if (saved.IsFailure)
            {
                return Result.Failure<VocabularySessionDto>(saved.Error);
            }
        }

        _logger.LogInformation(
            "GetVocabularySessionQuery succeeded: UserId={UserId}, SessionId={SessionId}, DueCount={DueCount}, NewCount={NewCount}, Cap={Cap}, Resumed={Resumed}",
            query.UserId, session.SessionId, session.DueItems.Count, session.NewItems.Count, cap, false);

        return Result.Success(session);
    }

    /// <summary>Returns the open session with its unanswered items, or <see langword="null"/> when nothing is left to answer.</summary>
    private async Task<Result<VocabularySessionDto?>> TryResumeAsync(
        Guid userId, VocabularySessionIssue issue, int cap, int introducedToday, CancellationToken ct)
    {
        Result<IReadOnlySet<Guid>> answered = await _sessionIssues.GetAnsweredSenseIdsAsync(userId, issue.SessionId, ct);
        if (answered.IsFailure)
        {
            return Result.Failure<VocabularySessionDto?>(answered.Error);
        }

        List<VocabularySessionIssueItem> left = issue.Items
            .Where(i => !answered.Value.Contains(i.SenseId))
            .OrderBy(i => i.Position)
            .ToList();
        if (left.Count == 0)
        {
            return Result.Success<VocabularySessionDto?>(null);
        }

        Result<IReadOnlyList<LearnerWordState>> states = await _states.GetActiveBySenseIdsAsync(userId, left.Select(i => i.SenseId).ToList(), ct);
        if (states.IsFailure)
        {
            return Result.Failure<VocabularySessionDto?>(states.Error);
        }

        Dictionary<Guid, LearnerWordState> bySense = states.Value.ToDictionary(s => s.SenseId);
        List<VocabularySessionItemDto> dueItems = left
            .Where(i => !i.IsNew && bySense.ContainsKey(i.SenseId))
            .Select(i => ToItem(bySense[i.SenseId]))
            .ToList();
        List<VocabularySessionItemDto> newItems = left
            .Where(i => i.IsNew && bySense.ContainsKey(i.SenseId))
            .Select(i => ToItem(bySense[i.SenseId]))
            .ToList();
        if (dueItems.Count + newItems.Count == 0)
        {
            return Result.Success<VocabularySessionDto?>(null);
        }

        return Result.Success<VocabularySessionDto?>(new VocabularySessionDto(issue.SessionId, dueItems, newItems, cap, introducedToday));
    }

    private static VocabularySessionItemDto ToItem(LearnerWordState state) => new(state.SenseId, state.Status, state.DueAtUtc);
}
