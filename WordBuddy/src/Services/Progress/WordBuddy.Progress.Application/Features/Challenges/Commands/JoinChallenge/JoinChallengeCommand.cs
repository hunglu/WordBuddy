using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.JoinChallenge;

/// <summary>The caller joins a challenge (opt-in).</summary>
public sealed record JoinChallengeCommand(Guid UserId, Guid ChallengeId, AgeGroup AgeGroup) : ICommand;
