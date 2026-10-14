using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalVocabularyWord;

/// <summary>Edits Word, Definition and Example of the caller's own <c>Private</c> learner word in
/// place (the id stays, so progress history stays valid). 404 when the word is not in the caller's
/// list, 403 for a system word or another author's word, 409 when the word is not Private or the
/// caller already has another word with the same content. A changed word text moves the sense to the
/// matching lexeme and removes the old lexeme if nothing else uses it.</summary>
public sealed class UpdatePersonalVocabularyWordCommandHandler : ICommandHandler<UpdatePersonalVocabularyWordCommand>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<UpdatePersonalVocabularyWordCommand> _validator;
    private readonly ILogger<UpdatePersonalVocabularyWordCommandHandler> _logger;

    public UpdatePersonalVocabularyWordCommandHandler(
        IVocabularyWordRepository repository,
        IValidator<UpdatePersonalVocabularyWordCommand> validator,
        ILogger<UpdatePersonalVocabularyWordCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(UpdatePersonalVocabularyWordCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "UpdatePersonalVocabularyWordCommand started: SenseId={SenseId}, UserId={UserId}",
            command.SenseId, command.UserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdatePersonalVocabularyWordCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("UpdatePersonalVocabularyWord.Validation", validation.ToString()));
        }

        Result<LearnerWord> linkResult = await _repository.GetLinkAsync(command.UserId, command.SenseId, ct);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "UpdatePersonalVocabularyWordCommand word not in caller's list: SenseId={SenseId}", command.SenseId);
            return Result.Failure(linkResult.Error);
        }

        Sense sense = linkResult.Value.Sense!;

        Result editable = sense.CheckEditable(command.UserId);
        if (editable.IsFailure)
        {
            _logger.LogWarning(
                "UpdatePersonalVocabularyWordCommand not allowed: SenseId={SenseId}, ErrorCode={ErrorCode}",
                command.SenseId, editable.Error.Code);
            return editable;
        }

        if (sense.Word == command.Word && sense.Definition == command.Definition && sense.Example == command.Example)
        {
            _logger.LogInformation("UpdatePersonalVocabularyWordCommand no change: SenseId={SenseId}", command.SenseId);
            return Result.Success();
        }

        string newHash = Sense.ComputeContentHash(command.Word, command.Definition, command.Example);
        if (newHash != sense.ContentHash)
        {
            Result<IReadOnlyList<Sense>> candidates = await _repository.FindByContentHashAsync(newHash, ct);
            if (candidates.IsFailure)
            {
                _logger.LogWarning(
                    "UpdatePersonalVocabularyWordCommand failed to look up duplicates: {ErrorCode}", candidates.Error.Code);
                return Result.Failure(candidates.Error);
            }

            bool ownDuplicate = candidates.Value.Any(c =>
                c.Id != sense.Id && c.Source == VocabularySource.Learner && c.OwnerUserId == command.UserId);
            if (ownDuplicate)
            {
                _logger.LogWarning("UpdatePersonalVocabularyWordCommand duplicate: SenseId={SenseId}", command.SenseId);
                return Result.Failure(Error.Conflict(
                    "PersonalVocabularyWord.Duplicate", "You already have this word."));
            }
        }

        Guid oldLexemeId = sense.LexemeId;
        Guid lexemeId = oldLexemeId;
        bool wordChanged = Sense.NormalizeWord(command.Word) != Sense.NormalizeWord(sense.Word);
        if (wordChanged)
        {
            // Runs before the edit: it saves, and must not flush half-applied changes.
            Result<Lexeme> lexemeResult = await _repository.GetOrCreateLexemeAsync(command.Word, ct);
            if (lexemeResult.IsFailure)
            {
                _logger.LogWarning(
                    "UpdatePersonalVocabularyWordCommand failed to get or create lexeme: {ErrorCode}", lexemeResult.Error.Code);
                return Result.Failure(lexemeResult.Error);
            }

            lexemeId = lexemeResult.Value.Id;
        }

        Result editResult = sense.EditLearnerContent(command.UserId, lexemeId, command.Word, command.Definition, command.Example);
        if (editResult.IsFailure)
        {
            _logger.LogWarning("UpdatePersonalVocabularyWordCommand rejected edit: {ErrorCode}", editResult.Error.Code);
            return editResult;
        }

        Result updateResult = await _repository.UpdateAsync(sense, ct);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning(
                "UpdatePersonalVocabularyWordCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                updateResult.Error.Code, updateResult.Error.Description);

            if (lexemeId != oldLexemeId)
            {
                // The new lexeme is already saved; remove it if nothing references it (guarded delete).
                await _repository.DeleteLexemeIfOrphanedAsync(lexemeId, ct);
            }

            return updateResult;
        }

        if (lexemeId != oldLexemeId)
        {
            Result<bool> cleanup = await _repository.DeleteLexemeIfOrphanedAsync(oldLexemeId, ct);
            if (cleanup.IsFailure)
            {
                // The edit is saved; a leftover lexeme is harmless.
                _logger.LogWarning(
                    "UpdatePersonalVocabularyWordCommand could not clean up old lexeme: LexemeId={LexemeId}, ErrorCode={ErrorCode}",
                    oldLexemeId, cleanup.Error.Code);
            }
        }

        _logger.LogInformation(
            "UpdatePersonalVocabularyWordCommand succeeded: SenseId={SenseId}, WordChanged={WordChanged}",
            command.SenseId, wordChanged);
        return Result.Success();
    }
}
