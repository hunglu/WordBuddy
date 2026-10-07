using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RepublishLearnerWords;

/// <summary>
/// Reads learner links page by page (system-owner links skipped by the repository) and publishes
/// <c>LearnerWordAdded</c> with the original <c>AddedAtUtc</c>, one outbox save per page. Progress
/// applies an equal timestamp, so missing states get created and existing ones stay unchanged.
/// </summary>
public sealed class RepublishLearnerWordsCommandHandler : ICommandHandler<RepublishLearnerWordsCommand, RepublishLearnerWordsResult>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly ILearnerWordEventPublisher _publisher;
    private readonly IValidator<RepublishLearnerWordsCommand> _validator;
    private readonly ILogger<RepublishLearnerWordsCommandHandler> _logger;

    public RepublishLearnerWordsCommandHandler(
        IVocabularyWordRepository repository,
        ILearnerWordEventPublisher publisher,
        IValidator<RepublishLearnerWordsCommand> validator,
        ILogger<RepublishLearnerWordsCommandHandler> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<RepublishLearnerWordsResult>> HandleAsync(RepublishLearnerWordsCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("RepublishLearnerWordsCommand started: BatchSize={BatchSize}", command.BatchSize);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RepublishLearnerWordsCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<RepublishLearnerWordsResult>(Error.Validation("RepublishLearnerWords.Validation", validation.ToString()));
        }

        int published = 0;
        Guid? lastLinkId = null;

        while (true)
        {
            Result<IReadOnlyList<LearnerWordLink>> page = await _repository.GetLearnerLinksPageAsync(lastLinkId, command.BatchSize, ct);
            if (page.IsFailure)
            {
                return Result.Failure<RepublishLearnerWordsResult>(page.Error);
            }

            if (page.Value.Count == 0)
            {
                break;
            }

            // The repository already skips system-owner links; filter again so no event ever names it.
            IReadOnlyList<LearnerWordLink> learnerLinks = page.Value.Where(l => l.UserId != SystemOwner.UserId).ToList();
            Result<int> batch = learnerLinks.Count == 0
                ? Result.Success(0)
                : await _publisher.PublishAddedAsync(learnerLinks, ct);
            if (batch.IsFailure)
            {
                _logger.LogWarning(
                    "RepublishLearnerWordsCommand batch failed after {Published} events: {ErrorCode}",
                    published, batch.Error.Code);
                return Result.Failure<RepublishLearnerWordsResult>(batch.Error);
            }

            published += batch.Value;
            lastLinkId = page.Value[^1].LinkId;

            if (page.Value.Count < command.BatchSize)
            {
                break;
            }
        }

        _logger.LogInformation("RepublishLearnerWordsCommand succeeded: Published={Published}", published);
        return Result.Success(new RepublishLearnerWordsResult(published));
    }
}
