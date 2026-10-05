using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;

/// <summary>Links a <see cref="VocabularyShareStatus.Shared"/> community-pool word into the caller's
/// own list — no copy is made. Idempotent: adopting the same word again returns the same id.
/// Returns the shared word's id.</summary>
public sealed class AddSharedVocabularyWordToMyListCommandHandler : ICommandHandler<AddSharedVocabularyWordToMyListCommand, Guid>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<AddSharedVocabularyWordToMyListCommand> _validator;
    private readonly ILogger<AddSharedVocabularyWordToMyListCommandHandler> _logger;

    public AddSharedVocabularyWordToMyListCommandHandler(
        IVocabularyWordRepository repository,
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

        Result<Sense> sourceResult = await _repository.GetByIdAsync(command.SharedWordId, ct);
        if (sourceResult.IsFailure)
        {
            _logger.LogWarning("AddSharedVocabularyWordToMyListCommand source word not found: SharedWordId={SharedWordId}", command.SharedWordId);
            return Result.Failure<Guid>(sourceResult.Error);
        }

        Sense source = sourceResult.Value;

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

        Result<LearnerWord> existingLink = await _repository.GetLinkAsync(command.RequestingUserId, source.Id, ct);
        if (existingLink.IsSuccess)
        {
            _logger.LogInformation("AddSharedVocabularyWordToMyListCommand succeeded: WordId={WordId}, AlreadyLinked={AlreadyLinked}", source.Id, true);
            return Result.Success(source.Id);
        }

        bool isAuthor = source.OwnerUserId == command.RequestingUserId;
        LearnerWord link = new(Guid.NewGuid(), command.RequestingUserId, source.Id, isAuthor);

        Result linkResult = await _repository.LinkAsync(link, ct);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "AddSharedVocabularyWordToMyListCommand failed to persist link: {ErrorCode} — {ErrorDescription}",
                linkResult.Error.Code, linkResult.Error.Description);
            return Result.Failure<Guid>(linkResult.Error);
        }

        _logger.LogInformation("AddSharedVocabularyWordToMyListCommand succeeded: WordId={WordId}, IsAuthor={IsAuthor}", source.Id, isAuthor);
        return Result.Success(source.Id);
    }
}
