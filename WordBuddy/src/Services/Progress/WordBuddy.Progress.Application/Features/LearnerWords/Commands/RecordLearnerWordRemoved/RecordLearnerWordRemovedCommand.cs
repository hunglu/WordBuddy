using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;

/// <summary>Records that a sense left a learner's list (from Content's <c>LearnerWordRemoved</c>).</summary>
public sealed record RecordLearnerWordRemovedCommand(
    Guid UserId,
    Guid SenseId,
    DateTime RemovedAtUtc) : ICommand;
