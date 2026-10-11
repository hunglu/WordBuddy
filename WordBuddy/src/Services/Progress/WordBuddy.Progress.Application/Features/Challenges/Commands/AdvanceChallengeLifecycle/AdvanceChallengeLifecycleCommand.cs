using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.AdvanceChallengeLifecycle;

/// <summary>Scheduler tick: creates the next instances from the templates and moves statuses by time. Returns the number of changes. Has no input, so it has no validator.</summary>
public sealed record AdvanceChallengeLifecycleCommand : ICommand<int>;
