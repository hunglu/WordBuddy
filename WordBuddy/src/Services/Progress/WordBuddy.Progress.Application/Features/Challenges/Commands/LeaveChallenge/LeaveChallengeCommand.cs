using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.LeaveChallenge;

/// <summary>The caller leaves a challenge (opt-out).</summary>
public sealed record LeaveChallengeCommand(Guid UserId, Guid ChallengeId) : ICommand;
