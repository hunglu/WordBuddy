using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.UpdateVocabularySettings;

/// <summary>Creates or updates the caller's settings.</summary>
public sealed class UpdateVocabularySettingsCommandHandler : ICommandHandler<UpdateVocabularySettingsCommand, VocabularySettingsDto>
{
    private readonly IVocabularyLearnerSettingsRepository _settings;
    private readonly IValidator<UpdateVocabularySettingsCommand> _validator;
    private readonly ILogger<UpdateVocabularySettingsCommandHandler> _logger;

    public UpdateVocabularySettingsCommandHandler(
        IVocabularyLearnerSettingsRepository settings,
        IValidator<UpdateVocabularySettingsCommand> validator,
        ILogger<UpdateVocabularySettingsCommandHandler> logger)
    {
        _settings = settings;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VocabularySettingsDto>> HandleAsync(UpdateVocabularySettingsCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("UpdateVocabularySettingsCommand started: UserId={UserId}", command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdateVocabularySettingsCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<VocabularySettingsDto>(Error.Validation("UpdateVocabularySettings.Validation", validation.ToString()));
        }

        Result<VocabularyLearnerSettings> saved = await _settings.UpsertAsync(command.UserId, command.NewWordsPerDay, ct);
        if (saved.IsFailure)
        {
            return Result.Failure<VocabularySettingsDto>(saved.Error);
        }

        _logger.LogInformation(
            "UpdateVocabularySettingsCommand succeeded: UserId={UserId}, NewWordsPerDay={NewWordsPerDay}",
            command.UserId, saved.Value.NewWordsPerDay);
        return Result.Success(new VocabularySettingsDto(saved.Value.NewWordsPerDay, saved.Value.SupporterNewWordCap));
    }
}
