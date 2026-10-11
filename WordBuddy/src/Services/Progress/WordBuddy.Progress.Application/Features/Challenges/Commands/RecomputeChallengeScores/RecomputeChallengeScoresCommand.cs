using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.RecomputeChallengeScores;

/// <summary>Scheduler tick: rewrites score and rank of every participant of every Running challenge. Returns the number of challenges. Has no input, so it has no validator.</summary>
public sealed record RecomputeChallengeScoresCommand : ICommand<int>;
