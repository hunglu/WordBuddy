using System.Globalization;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularySrs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;

/// <summary>
/// Builds the dashboard from source rows, cached 5 minutes (absolute) under
/// <c>progress:dashboard:{learnerId}:{days}:{offsetMinutes}</c>. No explicit delete: a new answer shows
/// up after the TTL. Same result for child and adult learners. Logs ids and counts only — never metric
/// values, timings or child data.
/// </summary>
public sealed class GetLearnerDashboardQueryHandler : IQueryHandler<GetLearnerDashboardQuery, LearnerDashboardDto>
{
    /// <summary>Cache lifetime.</summary>
    public static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(5);

    private readonly IDashboardReadRepository _repository;
    private readonly DashboardCalculator _calculator;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<GetLearnerDashboardQuery> _validator;
    private readonly ILogger<GetLearnerDashboardQueryHandler> _logger;

    public GetLearnerDashboardQueryHandler(
        IDashboardReadRepository repository,
        DashboardCalculator calculator,
        IDistributedCache cache,
        TimeProvider timeProvider,
        IValidator<GetLearnerDashboardQuery> validator,
        ILogger<GetLearnerDashboardQueryHandler> logger)
    {
        _repository = repository;
        _calculator = calculator;
        _cache = cache;
        _timeProvider = timeProvider;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Cache key for one learner, window and client offset.</summary>
    public static string CacheKey(Guid learnerId, int days, TimeSpan offset) =>
        $"progress:dashboard:{learnerId}:{days}:{((int)offset.TotalMinutes).ToString(CultureInfo.InvariantCulture)}";

    public async Task<Result<LearnerDashboardDto>> HandleAsync(GetLearnerDashboardQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetLearnerDashboardQuery started: LearnerId={LearnerId}, Days={Days}", query.LearnerId, query.Days);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetLearnerDashboardQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<LearnerDashboardDto>(Error.Validation("GetLearnerDashboard.Validation", validation.ToString()));
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        Result<TimeSpan> offsetResult = ClientDateTime.ParseOffset(query.ClientCurrentDateTime, nowUtc);
        if (offsetResult.IsFailure)
        {
            _logger.LogWarning("GetLearnerDashboardQuery invalid client date-time header: LearnerId={LearnerId}", query.LearnerId);
            return Result.Failure<LearnerDashboardDto>(offsetResult.Error);
        }

        TimeSpan offset = offsetResult.Value;
        string cacheKey = CacheKey(query.LearnerId, query.Days, offset);

        string? cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            LearnerDashboardDto? hit = JsonSerializer.Deserialize<LearnerDashboardDto>(cached);
            if (hit is not null)
            {
                _logger.LogInformation("GetLearnerDashboardQuery cache hit: LearnerId={LearnerId}, Days={Days}", query.LearnerId, query.Days);
                return Result.Success(hit);
            }
        }

        (DateTime fromUtc, DateTime toUtc) = DashboardCalculator.SourceRange(nowUtc, offset);

        Result<IReadOnlyList<DashboardReviewRow>> reviews = await _repository.GetReviewLogsAsync(query.LearnerId, fromUtc, toUtc, ct);
        if (reviews.IsFailure)
        {
            return Failed(query, reviews.Error);
        }

        Result<IReadOnlyList<DashboardWordRow>> words = await _repository.GetWordStatesAsync(query.LearnerId, ct);
        if (words.IsFailure)
        {
            return Failed(query, words.Error);
        }

        Result<IReadOnlyList<DashboardMembershipRow>> memberships = await _repository.GetMembershipsAsync(query.LearnerId, fromUtc, toUtc, ct);
        if (memberships.IsFailure)
        {
            return Failed(query, memberships.Error);
        }

        Result<IReadOnlyList<DashboardSessionRow>> sessions = await _repository.GetSessionIssuesAsync(query.LearnerId, fromUtc, toUtc, ct);
        if (sessions.IsFailure)
        {
            return Failed(query, sessions.Error);
        }

        LearnerDashboardDto dashboard = _calculator.Calculate(
            query.LearnerId, query.Days, nowUtc, offset, reviews.Value, words.Value, memberships.Value, sessions.Value);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(dashboard),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheLifetime },
            ct);

        _logger.LogInformation(
            "GetLearnerDashboardQuery succeeded: LearnerId={LearnerId}, Days={Days}, ReviewCount={ReviewCount}",
            query.LearnerId, query.Days, reviews.Value.Count);

        return Result.Success(dashboard);
    }

    private Result<LearnerDashboardDto> Failed(GetLearnerDashboardQuery query, Error error)
    {
        _logger.LogWarning(
            "GetLearnerDashboardQuery failed to read data: LearnerId={LearnerId}, ErrorCode={ErrorCode}",
            query.LearnerId, error.Code);
        return Result.Failure<LearnerDashboardDto>(error);
    }
}
