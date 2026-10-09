using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WordBuddy.Content.Api.Authorization;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Autofill.Commands.AddAutofillSenseToMyList;
using WordBuddy.Content.Application.Features.Autofill.Commands.ApproveChildWord;
using WordBuddy.Content.Application.Features.Autofill.Queries.GetPendingChildApprovals;
using WordBuddy.Content.Application.Features.Autofill.Queries.LookupAutofill;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>Vocabulary auto-fill (WB-25): lookup, add to my list, and the child approval queues of
/// supporters and admins.</summary>
[ApiController]
[Route("api/vocabulary")]
[Authorize]
public sealed class VocabularyAutofillController : ControllerBase
{
    private readonly IQueryHandler<LookupAutofillQuery, AutofillResultDto> _lookup;
    private readonly ICommandHandler<AddAutofillSenseToMyListCommand, Guid> _addToMine;
    private readonly IQueryHandler<GetPendingChildApprovalsQuery, IReadOnlyList<ChildApprovalDto>> _getPending;
    private readonly ICommandHandler<ApproveChildWordCommand> _approve;
    private readonly ILogger<VocabularyAutofillController> _logger;

    public VocabularyAutofillController(
        IQueryHandler<LookupAutofillQuery, AutofillResultDto> lookup,
        ICommandHandler<AddAutofillSenseToMyListCommand, Guid> addToMine,
        IQueryHandler<GetPendingChildApprovalsQuery, IReadOnlyList<ChildApprovalDto>> getPending,
        ICommandHandler<ApproveChildWordCommand> approve,
        ILogger<VocabularyAutofillController> logger)
    {
        _lookup = lookup;
        _addToMine = addToMine;
        _getPending = getPending;
        _approve = approve;
        _logger = logger;
    }

    /// <summary>Auto-fills a typed word. 200 with <c>autofillUnavailable = true</c> when the external
    /// services fail; 429 with <c>Retry-After</c> over 10 calls per minute.</summary>
    [Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]
    [EnableRateLimiting(RateLimitingConfiguration.AutofillPolicy)]
    [HttpGet("autofill")]
    public async Task<IActionResult> Lookup([FromQuery] string? word, CancellationToken ct)
    {
        Result<AutofillResultDto> result = await _lookup.HandleAsync(
            new LookupAutofillQuery(word ?? string.Empty, User.GetUserId(), User.GetAgeGroup()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Adds an auto-filled sense to the caller's list. Idempotent; returns the sense id.</summary>
    [Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]
    [HttpPost("autofill/{senseId:guid}/add-to-mine")]
    public async Task<IActionResult> AddToMine(Guid senseId, CancellationToken ct)
    {
        Result<Guid> result = await _addToMine.HandleAsync(
            new AddAutofillSenseToMyListCommand(senseId, User.GetUserId(), User.GetAgeGroup()), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("AddAutofillSenseToMine succeeded: SenseId={SenseId}", result.Value);
        return Ok(result.Value);
    }

    /// <summary>Supporter queue: auto-filled words the learner waits on.</summary>
    [Authorize(Policy = ServiceCollectionExtensions.AdultOrAdminPolicy)]
    [Authorize(Policy = SupportLinkPolicies.CanSupportLearner)]
    [HttpGet("learners/{learnerId:guid}/pending-approvals")]
    public async Task<IActionResult> GetLearnerPendingApprovals(Guid learnerId, CancellationToken ct)
    {
        Result<IReadOnlyList<ChildApprovalDto>> result = await _getPending.HandleAsync(new GetPendingChildApprovalsQuery(learnerId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Supporter approval: the word becomes visible to this child only.</summary>
    [Authorize(Policy = ServiceCollectionExtensions.AdultOrAdminPolicy)]
    [Authorize(Policy = SupportLinkPolicies.CanSupportLearner)]
    [HttpPost("learners/{learnerId:guid}/words/{senseId:guid}/approve")]
    public async Task<IActionResult> ApproveForLearner(Guid learnerId, Guid senseId, CancellationToken ct)
    {
        Result result = await _approve.HandleAsync(new ApproveChildWordCommand(senseId, learnerId, User.GetUserId()), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("ApproveForLearner succeeded: SenseId={SenseId}", senseId);
        return NoContent();
    }

    /// <summary>Admin queue: auto-filled words any child waits on.</summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("moderation/autofill-pending")]
    public async Task<IActionResult> GetAdminPendingApprovals(CancellationToken ct)
    {
        Result<IReadOnlyList<ChildApprovalDto>> result = await _getPending.HandleAsync(new GetPendingChildApprovalsQuery(null), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Admin approval: the word becomes visible to every child.</summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpPost("moderation/autofill/{senseId:guid}/approve")]
    public async Task<IActionResult> ApproveForChildren(Guid senseId, CancellationToken ct)
    {
        Result result = await _approve.HandleAsync(new ApproveChildWordCommand(senseId, null, User.GetUserId()), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("ApproveForChildren succeeded: SenseId={SenseId}", senseId);
        return NoContent();
    }
}
