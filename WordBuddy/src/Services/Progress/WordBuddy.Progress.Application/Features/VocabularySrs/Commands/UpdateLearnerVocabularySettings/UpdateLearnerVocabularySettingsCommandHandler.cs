using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateLearnerVocabularySettings;

/// <summary>Creates or updates the supporter cap of a learner. Logs ids only.</summary>
public sealed class UpdateLearnerVocabularySettingsCommandHandler
    : ICommandHandler<UpdateLearnerVocabularySettingsCommand, LearnerVocabularySettingsDto>
{
    private readonly IVocabularyLearnerSettingsRepository _settings;
    private readonly IValidator<UpdateLearnerVocabularySettingsCommand> _validator;
    private readonly ILogger<UpdateLearnerVocabularySettingsCommandHandler> _logger;

    public UpdateLearnerVocabularySettingsCommandHandler(
        IVocabularyLearnerSettingsRepository settings,
        IValidator<UpdateLearnerVocabularySettingsCommand> validator,
        ILogger<UpdateLearnerVocabularySettingsCommandHandler> logger)
    {
        _settings = settings;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<LearnerVocabularySettingsDto>> HandleAsync(
        UpdateLearnerVocabularySettingsCommand command,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "UpdateLearnerVocabularySettingsCommand started: SupporterId={SupporterId}, LearnerId={LearnerId}",
            command.SupporterId, command.LearnerId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdateLearnerVocabularySettingsCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<LearnerVocabularySettingsDto>(
                Error.Validation("UpdateLearnerVocabularySettings.Validation", validation.ToString()));
        }

        Result<VocabularyLearnerSettings> saved =
            await _settings.UpsertSupporterCapAsync(command.LearnerId, command.SupporterNewWordCap, command.SupporterId, ct);
        if (saved.IsFailure)
        {
            return Result.Failure<LearnerVocabularySettingsDto>(saved.Error);
        }

        VocabularyLearnerSettings value = saved.Value;
        _logger.LogInformation(
            "UpdateLearnerVocabularySettingsCommand succeeded: LearnerId={LearnerId}, SupporterNewWordCap={SupporterNewWordCap}",
            command.LearnerId, value.SupporterNewWordCap);
        return Result.Success(new LearnerVocabularySettingsDto(
            command.LearnerId, value.NewWordsPerDay, value.SupporterNewWordCap, value.SupporterCapSetBy));
    }
}
