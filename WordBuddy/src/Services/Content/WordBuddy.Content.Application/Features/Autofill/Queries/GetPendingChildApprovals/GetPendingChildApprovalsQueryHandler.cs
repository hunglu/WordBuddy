using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Autofill.Queries.GetPendingChildApprovals;

/// <summary>Lists only unapproved auto-filled senses that a child linked. Logs counts only.</summary>
public sealed class GetPendingChildApprovalsQueryHandler : IQueryHandler<GetPendingChildApprovalsQuery, IReadOnlyList<ChildApprovalDto>>
{
    private readonly IAutofillRepository _repository;
    private readonly ILogger<GetPendingChildApprovalsQueryHandler> _logger;

    public GetPendingChildApprovalsQueryHandler(IAutofillRepository repository, ILogger<GetPendingChildApprovalsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ChildApprovalDto>>> HandleAsync(GetPendingChildApprovalsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetPendingChildApprovalsQuery started: SupporterQueue={SupporterQueue}", query.LearnerId is not null);

        if (query.LearnerId is { } learnerId)
        {
            if (learnerId == Guid.Empty)
            {
                return Result.Failure<IReadOnlyList<ChildApprovalDto>>(Error.Validation("GetPendingChildApprovals.Validation", "A learner id is required."));
            }

            Result<IReadOnlyList<LearnerWord>> links = await _repository.GetPendingLinksForLearnerAsync(learnerId, ct);
            if (links.IsFailure)
            {
                _logger.LogWarning("GetPendingChildApprovalsQuery repository failure: {ErrorCode}", links.Error.Code);
                return Result.Failure<IReadOnlyList<ChildApprovalDto>>(links.Error);
            }

            List<ChildApprovalDto> forLearner = links.Value
                .Where(l => l.Sense is not null)
                .Select(l => ToDto(l.Sense!, learnerId))
                .ToList();
            _logger.LogInformation("GetPendingChildApprovalsQuery succeeded: Count={Count}", forLearner.Count);
            return Result.Success<IReadOnlyList<ChildApprovalDto>>(forLearner);
        }

        Result<IReadOnlyList<Sense>> senses = await _repository.GetPendingSensesForAdminAsync(ct);
        if (senses.IsFailure)
        {
            _logger.LogWarning("GetPendingChildApprovalsQuery repository failure: {ErrorCode}", senses.Error.Code);
            return Result.Failure<IReadOnlyList<ChildApprovalDto>>(senses.Error);
        }

        List<ChildApprovalDto> forAdmin = senses.Value.Select(s => ToDto(s, null)).ToList();
        _logger.LogInformation("GetPendingChildApprovalsQuery succeeded: Count={Count}", forAdmin.Count);
        return Result.Success<IReadOnlyList<ChildApprovalDto>>(forAdmin);
    }

    private static ChildApprovalDto ToDto(Sense sense, Guid? learnerId)
    {
        SenseDetails details = SenseDetails.From(sense);
        return new ChildApprovalDto(
            sense.Id,
            learnerId,
            sense.Word,
            sense.Definition,
            details.PartOfSpeech,
            details.Examples,
            details.Translations,
            sense.ChildSuitableHint,
            sense.CreatedAtUtc);
    }
}
