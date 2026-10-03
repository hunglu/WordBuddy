using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;

/// <summary>Adds a word to the caller's list. If a word with the same normalized content already
/// exists and the caller may see it (their own, a system word, or a shared word they're allowed to
/// see), the caller is linked to it instead of storing a duplicate. Otherwise a new learner word is
/// created with an author link. Returns the word id, which may be an existing id. Never links to,
/// or reveals, a word the caller couldn't see — another learner's private duplicate stays separate.</summary>
public sealed class AddPersonalVocabularyWordCommandHandler : ICommandHandler<AddPersonalVocabularyWordCommand, Guid>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<AddPersonalVocabularyWordCommand> _validator;
    private readonly ILogger<AddPersonalVocabularyWordCommandHandler> _logger;

    public AddPersonalVocabularyWordCommandHandler(
        IVocabularyWordRepository repository,
        IValidator<AddPersonalVocabularyWordCommand> validator,
        ILogger<AddPersonalVocabularyWordCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(AddPersonalVocabularyWordCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AddPersonalVocabularyWordCommand started: OwnerUserId={OwnerUserId}, Word={Word}",
            command.OwnerUserId, command.Word);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AddPersonalVocabularyWordCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<Guid>(Error.Validation("AddPersonalVocabularyWord.Validation", validation.ToString()));
        }

        string contentHash = VocabularyWord.ComputeContentHash(command.Word, command.Definition, command.Example);

        Result<IReadOnlyList<VocabularyWord>> candidatesResult = await _repository.FindByContentHashAsync(contentHash, ct);
        if (candidatesResult.IsFailure)
        {
            _logger.LogWarning(
                "AddPersonalVocabularyWordCommand failed to look up duplicates: {ErrorCode} — {ErrorDescription}",
                candidatesResult.Error.Code, candidatesResult.Error.Description);
            return Result.Failure<Guid>(candidatesResult.Error);
        }

        VocabularyWord? existing = PickVisibleDuplicate(candidatesResult.Value, command.OwnerUserId, command.OwnerAgeGroup);
        if (existing is not null)
        {
            return await LinkToExistingAsync(existing, command, ct);
        }

        Result<VocabularyWord> createResult = VocabularyWord.CreateLearner(
            Guid.NewGuid(),
            command.OwnerUserId,
            command.OwnerAgeGroup,
            command.Word,
            command.Definition,
            command.Example);

        if (createResult.IsFailure)
        {
            _logger.LogWarning(
                "AddPersonalVocabularyWordCommand rejected word: {ErrorCode} — {ErrorDescription}",
                createResult.Error.Code, createResult.Error.Description);
            return Result.Failure<Guid>(createResult.Error);
        }

        VocabularyWord word = createResult.Value;
        UserVocabularyWord authorLink = new(Guid.NewGuid(), command.OwnerUserId, word.Id, isAuthor: true);

        // The repository returns the existing word's id if a concurrent identical add won the race.
        Result<Guid> addResult = await _repository.AddAsync(word, authorLink, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "AddPersonalVocabularyWordCommand failed to persist word: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result.Failure<Guid>(addResult.Error);
        }

        _logger.LogInformation(
            "AddPersonalVocabularyWordCommand succeeded: WordId={WordId}, Created={Created}",
            addResult.Value, addResult.Value == word.Id);
        return Result.Success(addResult.Value);
    }

    /// <summary>The caller's own word wins, then a system word, then a shared word they may see —
    /// within each group, the repository's id order. Invisible candidates are ignored entirely.</summary>
    private static VocabularyWord? PickVisibleDuplicate(IReadOnlyList<VocabularyWord> candidates, Guid userId, AgeGroup ageGroup) =>
        candidates.FirstOrDefault(w => w.Source == VocabularySource.Learner && w.OwnerUserId == userId)
        ?? candidates.FirstOrDefault(w => w.Source == VocabularySource.System && w.IsVisibleTo(userId, ageGroup))
        ?? candidates.FirstOrDefault(w => w.IsVisibleTo(userId, ageGroup));

    private async Task<Result<Guid>> LinkToExistingAsync(VocabularyWord existing, AddPersonalVocabularyWordCommand command, CancellationToken ct)
    {
        Result<UserVocabularyWord> linkResult = await _repository.GetLinkAsync(command.OwnerUserId, existing.Id, ct);
        if (linkResult.IsSuccess)
        {
            _logger.LogInformation("AddPersonalVocabularyWordCommand succeeded: WordId={WordId}, AlreadyLinked={AlreadyLinked}", existing.Id, true);
            return Result.Success(existing.Id);
        }

        bool isAuthor = existing.Source == VocabularySource.Learner && existing.OwnerUserId == command.OwnerUserId;
        UserVocabularyWord link = new(Guid.NewGuid(), command.OwnerUserId, existing.Id, isAuthor);

        Result addLinkResult = await _repository.LinkAsync(link, ct);
        if (addLinkResult.IsFailure)
        {
            _logger.LogWarning(
                "AddPersonalVocabularyWordCommand failed to link existing word: {ErrorCode} — {ErrorDescription}",
                addLinkResult.Error.Code, addLinkResult.Error.Description);
            return Result.Failure<Guid>(addLinkResult.Error);
        }

        _logger.LogInformation(
            "AddPersonalVocabularyWordCommand succeeded: WordId={WordId}, LinkedExisting={LinkedExisting}, IsAuthor={IsAuthor}",
            existing.Id, true, isAuthor);
        return Result.Success(existing.Id);
    }
}
