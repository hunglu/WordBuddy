using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WordBuddy.Progress.Api.Authorization;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Api.Models;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Challenges.Commands.JoinChallenge;
using WordBuddy.Progress.Application.Features.Challenges.Commands.LeaveChallenge;
using WordBuddy.Progress.Application.Features.Challenges.Commands.SetChildChallengeConsent;
using WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;
using WordBuddy.Progress.Application.Features.Challenges.Queries.ListChallenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Api.Controllers;

/// <summary>
/// Vocabulary challenges (WB-29). Challenges are made by the system. The caller sees only the audience of their
/// <c>age_group</c> claim. A child needs Guardian consent first. Responses hold no user ids and no contact data.
/// </summary>
[ApiController]
[Route("api/progress/challenges")]
[Authorize]
public sealed class ChallengesController : ControllerBase
{
    private readonly IQueryHandler<ListChallengesQuery, IReadOnlyList<ChallengeDto>> _list;
    private readonly IQueryHandler<GetChallengeBoardQuery, ChallengeBoardDto> _board;
    private readonly ICommandHandler<JoinChallengeCommand> _join;
    private readonly ICommandHandler<LeaveChallengeCommand> _leave;
    private readonly ICommandHandler<SetChildChallengeConsentCommand> _consent;

    public ChallengesController(
        IQueryHandler<ListChallengesQuery, IReadOnlyList<ChallengeDto>> list,
        IQueryHandler<GetChallengeBoardQuery, ChallengeBoardDto> board,
        ICommandHandler<JoinChallengeCommand> join,
        ICommandHandler<LeaveChallengeCommand> leave,
        ICommandHandler<SetChildChallengeConsentCommand> consent)
    {
        _list = list;
        _board = board;
        _join = join;
        _leave = leave;
        _consent = consent;
    }

    /// <summary>Lists the Open and Running challenges of the caller audience. A child without consent gets an empty list.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        Result<IReadOnlyList<ChallengeDto>> result = await _list.HandleAsync(new ListChallengesQuery(User.GetUserId(), User.GetAgeGroup()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Gets the board. Adult: full ranking. Child: own rank and the top 5, alias and avatar only.</summary>
    [HttpGet("{id:guid}/board")]
    public async Task<IActionResult> GetBoard(Guid id, CancellationToken ct)
    {
        Result<ChallengeBoardDto> result = await _board.HandleAsync(new GetChallengeBoardQuery(User.GetUserId(), id, User.GetAgeGroup()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Joins a challenge. <c>409 Challenge.Full</c> when there is no room.</summary>
    [HttpPost("{id:guid}/join")]
    [EnableRateLimiting(RateLimitingConfiguration.ChallengeJoinPolicy)]
    public async Task<IActionResult> Join(Guid id, CancellationToken ct)
    {
        Result result = await _join.HandleAsync(new JoinChallengeCommand(User.GetUserId(), id, User.GetAgeGroup()), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Leaves a challenge.</summary>
    [HttpDelete("{id:guid}/join")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        Result result = await _leave.HandleAsync(new LeaveChallengeCommand(User.GetUserId(), id), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>A Guardian allows or stops challenges for a linked child. Needs the Guardian role and an active support link.</summary>
    [HttpPut("consent/{childId:guid}")]
    [Authorize(Policy = ChallengePolicies.GuardianOnly)]
    public async Task<IActionResult> SetConsent(Guid childId, [FromBody] ChallengeConsentRequest request, CancellationToken ct)
    {
        Result result = await _consent.HandleAsync(new SetChildChallengeConsentCommand(User.GetUserId(), childId, request.Enabled), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }
}
