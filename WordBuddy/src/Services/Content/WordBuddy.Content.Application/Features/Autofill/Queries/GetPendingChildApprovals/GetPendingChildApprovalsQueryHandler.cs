using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Autofill.Queries.GetPendingChildApprovals;

/// <summary>Lists only unapproved auto-filled senses that a child linked. Logs counts only.</summary>
public sealed class GetPendingChildApprovalsQueryHandler : IQueryHandler<GetPendingChildApprovalsQuery, PagedResult<ChildApprovalDto>>
{
    private readonly IAutofillRepository _repository;
    private readonly IValidator<GetPendingChildApprovalsQuery> _validator;
    private readonly ILogger<GetPendingChildApprovalsQueryHandler> _logger;

    public GetPendingChildApprovalsQueryHandler(
        IAutofillRepository repository,
        IValidator<GetPendingChildApprovalsQuery> validator,
        ILogger<GetPendingChildApprovalsQueryHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ChildApprovalDto>>> HandleAsync(GetPendingChildApprovalsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetPendingChildApprovalsQuery started: SupporterQueue={SupporterQueue}, Page={Page}, PageSize={PageSize}",
            query.LearnerId is not null, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetPendingChildApprovalsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<ChildApprovalDto>>(Error.Validation("GetPendingChildApprovals.Validation", validation.ToString()));
        }

        if (query.LearnerId is { } learnerId)
        {
            Result<PagedResult<LearnerWord>> links = await _repository.GetPendingLinksForLearnerAsync(learnerId, query.Page, query.PageSize, ct);
            if (links.IsFailure)
            {
                _logger.LogWarning("GetPendingChildApprovalsQuery repository failure: {ErrorCode}", links.Error.Code);
                return Result.Failure<PagedResult<ChildApprovalDto>>(links.Error);
            }

            List<ChildApprovalDto> forLearner = links.Value.Items
                .Where(l => l.Sense is not null)
                .Select(l => ToDto(l.Sense!, learnerId))
                .ToList();
            _logger.LogInformation("GetPendingChildApprovalsQuery succeeded: Count={Count}, TotalCount={TotalCount}", forLearner.Count, links.Value.TotalCount);
            return Result.Success(new PagedResult<ChildApprovalDto>(forLearner, links.Value.Page, links.Value.PageSize, links.Value.TotalCount));
        }

        Result<PagedResult<Sense>> senses = await _repository.GetPendingSensesForAdminAsync(query.Page, query.PageSize, ct);
        if (senses.IsFailure)
        {
            _logger.LogWarning("GetPendingChildApprovalsQuery repository failure: {ErrorCode}", senses.Error.Code);
            return Result.Failure<PagedResult<ChildApprovalDto>>(senses.Error);
        }

        List<ChildApprovalDto> forAdmin = senses.Value.Items.Select(s => ToDto(s, null)).ToList();
        _logger.LogInformation("GetPendingChildApprovalsQuery succeeded: Count={Count}, TotalCount={TotalCount}", forAdmin.Count, senses.Value.TotalCount);
        return Result.Success(new PagedResult<ChildApprovalDto>(forAdmin, senses.Value.Page, senses.Value.PageSize, senses.Value.TotalCount));
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
