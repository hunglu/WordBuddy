using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerVocabularySettings;

/// <summary>Returns the learner settings, or defaults when none are saved.</summary>
public sealed class GetLearnerVocabularySettingsQueryHandler : IQueryHandler<GetLearnerVocabularySettingsQuery, LearnerVocabularySettingsDto>
{
    private readonly IVocabularyLearnerSettingsRepository _settings;
    private readonly ILogger<GetLearnerVocabularySettingsQueryHandler> _logger;

    public GetLearnerVocabularySettingsQueryHandler(
        IVocabularyLearnerSettingsRepository settings,
        ILogger<GetLearnerVocabularySettingsQueryHandler> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result<LearnerVocabularySettingsDto>> HandleAsync(GetLearnerVocabularySettingsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetLearnerVocabularySettingsQuery started: SupporterId={SupporterId}, LearnerId={LearnerId}",
            query.SupporterId, query.LearnerId);

        Result<VocabularyLearnerSettings> settings = await _settings.GetAsync(query.LearnerId, ct);
        if (settings.IsFailure)
        {
            return settings.Error.Type == ErrorType.NotFound
                ? Result.Success(new LearnerVocabularySettingsDto(query.LearnerId, null, null, null))
                : Result.Failure<LearnerVocabularySettingsDto>(settings.Error);
        }

        VocabularyLearnerSettings value = settings.Value;
        return Result.Success(new LearnerVocabularySettingsDto(
            query.LearnerId, value.NewWordsPerDay, value.SupporterNewWordCap, value.SupporterCapSetBy));
    }
}
