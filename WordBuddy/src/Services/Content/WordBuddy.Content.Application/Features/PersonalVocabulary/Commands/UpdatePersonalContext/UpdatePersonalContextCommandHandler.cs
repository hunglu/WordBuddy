using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalContext;

/// <summary>Sets or clears the caller's private note on one of their own links (author, adopted,
/// auto-fill or supporter-assigned). Not-found when the word is not in the caller's list. The note
/// text is never logged.</summary>
public sealed class UpdatePersonalContextCommandHandler : ICommandHandler<UpdatePersonalContextCommand>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<UpdatePersonalContextCommand> _validator;
    private readonly ILogger<UpdatePersonalContextCommandHandler> _logger;

    public UpdatePersonalContextCommandHandler(
        IVocabularyWordRepository repository,
        IValidator<UpdatePersonalContextCommand> validator,
        ILogger<UpdatePersonalContextCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(UpdatePersonalContextCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "UpdatePersonalContextCommand started: SenseId={SenseId}, UserId={UserId}, HasPersonalContext={HasPersonalContext}",
            command.SenseId, command.UserId, !string.IsNullOrWhiteSpace(command.PersonalContext));

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdatePersonalContextCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("UpdatePersonalContext.Validation", validation.ToString()));
        }

        Result<LearnerWord> linkResult = await _repository.GetLinkAsync(command.UserId, command.SenseId, ct);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning("UpdatePersonalContextCommand word not in caller's list: SenseId={SenseId}", command.SenseId);
            return Result.Failure(linkResult.Error);
        }

        Result setResult = linkResult.Value.SetPersonalContext(command.PersonalContext);
        if (setResult.IsFailure)
        {
            _logger.LogWarning("UpdatePersonalContextCommand rejected note: {ErrorCode}", setResult.Error.Code);
            return setResult;
        }

        Result saveResult = await _repository.SaveLinkAsync(linkResult.Value, ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "UpdatePersonalContextCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation("UpdatePersonalContextCommand succeeded: SenseId={SenseId}", command.SenseId);
        return Result.Success();
    }
}
