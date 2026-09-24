using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SubmitVocabularyRecallCheck;

/// <summary>Upserts each word's <see cref="VocabularyRecallStat"/> and appends one
/// <see cref="VocabularyRecallSession"/> summarizing the batch.</summary>
public sealed class SubmitVocabularyRecallCheckCommandHandler : ICommandHandler<SubmitVocabularyRecallCheckCommand>
{
    private readonly IVocabularyRecallRepository _repository;
    private readonly IValidator<SubmitVocabularyRecallCheckCommand> _validator;
    private readonly ILogger<SubmitVocabularyRecallCheckCommandHandler> _logger;

    public SubmitVocabularyRecallCheckCommandHandler(
        IVocabularyRecallRepository repository,
        IValidator<SubmitVocabularyRecallCheckCommand> validator,
        ILogger<SubmitVocabularyRecallCheckCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(SubmitVocabularyRecallCheckCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "SubmitVocabularyRecallCheckCommand started: UserId={UserId}, ResultCount={ResultCount}",
            command.UserId, command.Results.Count);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("SubmitVocabularyRecallCheckCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("SubmitVocabularyRecallCheck.Validation", validation.ToString()));
        }

        IReadOnlyList<VocabularyRecallResult> results = command.Results
            .Select(r => new VocabularyRecallResult(r.VocabularyWordId, r.Word, r.Known))
            .ToList();

        Result upsertResult = await _repository.UpsertStatsAsync(command.UserId, results, ct);
        if (upsertResult.IsFailure)
        {
            _logger.LogWarning(
                "SubmitVocabularyRecallCheckCommand failed to upsert stats: {ErrorCode} — {ErrorDescription}",
                upsertResult.Error.Code, upsertResult.Error.Description);
            return upsertResult;
        }

        int wordsKnown = command.Results.Count(r => r.Known);
        VocabularyRecallSession session = new(Guid.NewGuid(), command.UserId, command.Results.Count, wordsKnown);

        Result addSessionResult = await _repository.AddSessionAsync(session, ct);
        if (addSessionResult.IsFailure)
        {
            _logger.LogWarning(
                "SubmitVocabularyRecallCheckCommand failed to add session: {ErrorCode} — {ErrorDescription}",
                addSessionResult.Error.Code, addSessionResult.Error.Description);
            return addSessionResult;
        }

        _logger.LogInformation(
            "SubmitVocabularyRecallCheckCommand succeeded: UserId={UserId}, WordsChecked={WordsChecked}, WordsKnown={WordsKnown}",
            command.UserId, command.Results.Count, wordsKnown);
        return Result.Success();
    }
}
