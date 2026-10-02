using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.VocabularyRemaps.Commands.AcknowledgeVocabularyWordIdRemaps;

/// <summary>Stamps the listed remaps as acknowledged by Progress. Idempotent — ids that are unknown
/// or already acknowledged are left alone.</summary>
public sealed class AcknowledgeVocabularyWordIdRemapsCommandHandler : ICommandHandler<AcknowledgeVocabularyWordIdRemapsCommand>
{
    private readonly IVocabularyWordIdRemapRepository _repository;
    private readonly IValidator<AcknowledgeVocabularyWordIdRemapsCommand> _validator;
    private readonly ILogger<AcknowledgeVocabularyWordIdRemapsCommandHandler> _logger;

    public AcknowledgeVocabularyWordIdRemapsCommandHandler(
        IVocabularyWordIdRemapRepository repository,
        IValidator<AcknowledgeVocabularyWordIdRemapsCommand> validator,
        ILogger<AcknowledgeVocabularyWordIdRemapsCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(AcknowledgeVocabularyWordIdRemapsCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("AcknowledgeVocabularyWordIdRemapsCommand started: Count={Count}", command.OldIds?.Count ?? 0);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AcknowledgeVocabularyWordIdRemapsCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("AcknowledgeVocabularyWordIdRemaps.Validation", validation.ToString()));
        }

        Result<int> result = await _repository.AcknowledgeAsync(command.OldIds!, ct);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "AcknowledgeVocabularyWordIdRemapsCommand repository failure: {ErrorCode} — {ErrorDescription}",
                result.Error.Code, result.Error.Description);
            return Result.Failure(result.Error);
        }

        _logger.LogInformation(
            "AcknowledgeVocabularyWordIdRemapsCommand succeeded: Requested={Requested}, Stamped={Stamped}",
            command.OldIds!.Count, result.Value);
        return Result.Success();
    }
}
