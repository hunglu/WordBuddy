using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;

/// <summary>Copies a <see cref="VocabularyShareStatus.Shared"/> community-pool word into the
/// caller's own list as a new <see cref="VocabularyShareStatus.Private"/> entry.</summary>
public sealed class AddSharedVocabularyWordToMyListCommandHandler : ICommandHandler<AddSharedVocabularyWordToMyListCommand, Guid>
{
    private readonly IPersonalVocabularyWordRepository _repository;
    private readonly IValidator<AddSharedVocabularyWordToMyListCommand> _validator;
    private readonly ILogger<AddSharedVocabularyWordToMyListCommandHandler> _logger;

    public AddSharedVocabularyWordToMyListCommandHandler(
        IPersonalVocabularyWordRepository repository,
        IValidator<AddSharedVocabularyWordToMyListCommand> validator,
        ILogger<AddSharedVocabularyWordToMyListCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(AddSharedVocabularyWordToMyListCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AddSharedVocabularyWordToMyListCommand started: SharedWordId={SharedWordId}, RequestingUserId={RequestingUserId}",
            command.SharedWordId, command.RequestingUserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AddSharedVocabularyWordToMyListCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<Guid>(Error.Validation("AddSharedVocabularyWordToMyList.Validation", validation.ToString()));
        }

        Result<PersonalVocabularyWord> sourceResult = await _repository.GetByIdAsync(command.SharedWordId, ct);
        if (sourceResult.IsFailure)
        {
            _logger.LogWarning("AddSharedVocabularyWordToMyListCommand source word not found: SharedWordId={SharedWordId}", command.SharedWordId);
            return Result.Failure<Guid>(sourceResult.Error);
        }

        PersonalVocabularyWord source = sourceResult.Value;

        bool notVisibleToRequester =
            source.ShareStatus != VocabularyShareStatus.Shared ||
            (command.RequestingAgeGroup == AgeGroup.Child && !source.VisibleToChildren);

        if (notVisibleToRequester)
        {
            // Same error whether the word simply isn't shared or isn't child-safe — never leaks
            // which case it is to the caller.
            _logger.LogWarning(
                "AddSharedVocabularyWordToMyListCommand source word not available: SharedWordId={SharedWordId}, ShareStatus={ShareStatus}",
                command.SharedWordId, source.ShareStatus);
            return Result.Failure<Guid>(Error.NotFound(
                "AddSharedVocabularyWordToMyList.NotShared",
                $"Word {command.SharedWordId} is not available in the shared pool."));
        }

        Guid id = Guid.NewGuid();
        PersonalVocabularyWord copy = new(
            id,
            command.RequestingUserId,
            command.RequestingAgeGroup,
            source.Word,
            source.Definition,
            source.Example);

        Result addResult = await _repository.AddAsync(copy, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "AddSharedVocabularyWordToMyListCommand failed to persist copy: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result.Failure<Guid>(addResult.Error);
        }

        _logger.LogInformation("AddSharedVocabularyWordToMyListCommand succeeded: NewWordId={NewWordId}", id);
        return Result.Success(id);
    }
}
