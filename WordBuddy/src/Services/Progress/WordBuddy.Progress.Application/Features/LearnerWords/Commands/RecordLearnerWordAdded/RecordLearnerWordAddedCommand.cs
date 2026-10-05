using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;

/// <summary>Records that a learner now has a sense in their list (from Content's <c>LearnerWordAdded</c>).</summary>
public sealed record RecordLearnerWordAddedCommand(
    Guid UserId,
    Guid SenseId,
    Guid AddedBy,
    DateTime AddedAtUtc) : ICommand;
