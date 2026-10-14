using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UpdateLexeme;

public sealed class UpdateLexemeCommandHandler : ICommandHandler<UpdateLexemeCommand>
{
    private readonly ILexemeAdminRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly IValidator<UpdateLexemeCommand> _validator;
    private readonly ILogger<UpdateLexemeCommandHandler> _logger;

    public UpdateLexemeCommandHandler(
        ILexemeAdminRepository repository,
        IDistributedCache cache,
        IValidator<UpdateLexemeCommand> validator,
        ILogger<UpdateLexemeCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(UpdateLexemeCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "UpdateLexemeCommand started: LexemeId={LexemeId}, PartOfSpeech={PartOfSpeech}", command.LexemeId, command.PartOfSpeech);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdateLexemeCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("UpdateLexeme.Validation", validation.ToString()));
        }

        Result<Lexeme> lexemeResult = await _repository.GetTrackedAsync(command.LexemeId, ct);
        if (lexemeResult.IsFailure)
        {
            _logger.LogWarning(
                "UpdateLexemeCommand lexeme lookup failed: {ErrorCode} — {ErrorDescription}",
                lexemeResult.Error.Code, lexemeResult.Error.Description);
            return Result.Failure(lexemeResult.Error);
        }

        Lexeme lexeme = lexemeResult.Value;

        Result<bool> clash = await _repository.ExistsWithPartOfSpeechAsync(lexeme.NormalizedLemma, command.PartOfSpeech, lexeme.Id, ct);
        if (clash.IsFailure)
        {
            _logger.LogWarning(
                "UpdateLexemeCommand clash check failed: {ErrorCode} — {ErrorDescription}",
                clash.Error.Code, clash.Error.Description);
            return Result.Failure(clash.Error);
        }

        if (clash.Value)
        {
            _logger.LogWarning(
                "UpdateLexemeCommand rejected: LexemeId={LexemeId} has a sibling with PartOfSpeech={PartOfSpeech}",
                lexeme.Id, command.PartOfSpeech);
            return Result.Failure(Error.Conflict(
                "Lexeme.DuplicatePartOfSpeech", $"Another lexeme with the same word already is a {command.PartOfSpeech}."));
        }

        Result correct = lexeme.Correct(
            command.PartOfSpeech, command.IpaUk, command.IpaUs, command.Syllables, command.WordForms, command.CefrLevel, command.FrequencyRank);
        if (correct.IsFailure)
        {
            _logger.LogWarning(
                "UpdateLexemeCommand rejected by lexeme: {ErrorCode} — {ErrorDescription}",
                correct.Error.Code, correct.Error.Description);
            return correct;
        }

        Result save = await _repository.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning(
                "UpdateLexemeCommand failed to persist: {ErrorCode} — {ErrorDescription}", save.Error.Code, save.Error.Description);
            return save;
        }

        await LexemeCacheInvalidator.InvalidateAsync(_cache, lexeme.NormalizedLemma, ct);

        _logger.LogInformation("UpdateLexemeCommand succeeded: LexemeId={LexemeId}", lexeme.Id);
        return Result.Success();
    }
}
