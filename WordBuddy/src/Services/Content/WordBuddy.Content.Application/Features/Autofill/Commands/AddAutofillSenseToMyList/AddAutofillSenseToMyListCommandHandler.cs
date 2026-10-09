using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Autofill.Commands.AddAutofillSenseToMyList;

/// <summary>Links an <see cref="SenseOrigin.AutoFill"/> sense into the caller's list
/// (<see cref="LearnerWordAddedBy.Learner"/>). Idempotent: an existing link returns the same id. A Child
/// link to a sense children may not see yet is marked <see cref="LearnerWord.RequiresChildApproval"/>;
/// its content stays hidden until a supporter or admin approves it.</summary>
public sealed class AddAutofillSenseToMyListCommandHandler : ICommandHandler<AddAutofillSenseToMyListCommand, Guid>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<AddAutofillSenseToMyListCommand> _validator;
    private readonly ILogger<AddAutofillSenseToMyListCommandHandler> _logger;

    public AddAutofillSenseToMyListCommandHandler(
        IVocabularyWordRepository repository,
        IValidator<AddAutofillSenseToMyListCommand> validator,
        ILogger<AddAutofillSenseToMyListCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(AddAutofillSenseToMyListCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AddAutofillSenseToMyListCommand started: SenseId={SenseId}, RequestingUserId={RequestingUserId}",
            command.SenseId, command.RequestingUserId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AddAutofillSenseToMyListCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<Guid>(Error.Validation("AddAutofillSenseToMyList.Validation", validation.ToString()));
        }

        Result<Sense> senseResult = await _repository.GetByIdAsync(command.SenseId, ct);
        if (senseResult.IsFailure || senseResult.Value.Origin != SenseOrigin.AutoFill)
        {
            _logger.LogWarning("AddAutofillSenseToMyListCommand sense not available: SenseId={SenseId}", command.SenseId);
            return Result.Failure<Guid>(Error.NotFound(
                "AddAutofillSenseToMyList.NotFound", $"Auto-filled word {command.SenseId} was not found."));
        }

        Sense sense = senseResult.Value;

        Result<LearnerWord> existing = await _repository.GetLinkAsync(command.RequestingUserId, sense.Id, ct);
        if (existing.IsSuccess)
        {
            _logger.LogInformation("AddAutofillSenseToMyListCommand succeeded: SenseId={SenseId}, AlreadyLinked={AlreadyLinked}", sense.Id, true);
            return Result.Success(sense.Id);
        }

        LearnerWord link = new(Guid.NewGuid(), command.RequestingUserId, sense.Id, isAuthor: false);
        bool needsApproval = command.RequestingAgeGroup == AgeGroup.Child && !sense.VisibleToChildren;
        if (needsApproval)
        {
            link.RequireChildApproval();
        }

        Result linkResult = await _repository.LinkAsync(link, ct);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "AddAutofillSenseToMyListCommand failed to persist link: {ErrorCode} — {ErrorDescription}",
                linkResult.Error.Code, linkResult.Error.Description);
            return Result.Failure<Guid>(linkResult.Error);
        }

        _logger.LogInformation(
            "AddAutofillSenseToMyListCommand succeeded: SenseId={SenseId}, RequiresChildApproval={RequiresChildApproval}",
            sense.Id, needsApproval);
        return Result.Success(sense.Id);
    }
}
