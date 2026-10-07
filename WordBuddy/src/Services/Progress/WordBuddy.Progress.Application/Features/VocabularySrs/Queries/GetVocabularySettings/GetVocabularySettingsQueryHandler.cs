using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySettings;

/// <summary>Returns saved settings, or the default (<c>NewWordsPerDay = null</c>, backlog rule) when none.</summary>
public sealed class GetVocabularySettingsQueryHandler : IQueryHandler<GetVocabularySettingsQuery, VocabularySettingsDto>
{
    private readonly IVocabularyLearnerSettingsRepository _settings;
    private readonly ILogger<GetVocabularySettingsQueryHandler> _logger;

    public GetVocabularySettingsQueryHandler(IVocabularyLearnerSettingsRepository settings, ILogger<GetVocabularySettingsQueryHandler> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<Result<VocabularySettingsDto>> HandleAsync(GetVocabularySettingsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetVocabularySettingsQuery started: UserId={UserId}", query.UserId);

        Result<VocabularyLearnerSettings> settings = await _settings.GetAsync(query.UserId, ct);
        if (settings.IsFailure)
        {
            return settings.Error.Type == ErrorType.NotFound
                ? Result.Success(new VocabularySettingsDto(null))
                : Result.Failure<VocabularySettingsDto>(settings.Error);
        }

        return Result.Success(new VocabularySettingsDto(settings.Value.NewWordsPerDay));
    }
}
