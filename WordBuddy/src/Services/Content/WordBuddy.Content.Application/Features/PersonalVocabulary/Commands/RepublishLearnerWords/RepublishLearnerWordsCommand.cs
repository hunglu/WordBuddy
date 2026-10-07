using WordBuddy.Content.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RepublishLearnerWords;

/// <summary>Admin backfill: republishes <c>LearnerWordAdded</c> for every existing learner link, in
/// batches of <paramref name="BatchSize"/>. Safe to run twice. Gated by <c>AdminOnly</c> at the controller.</summary>
public sealed record RepublishLearnerWordsCommand(int BatchSize = RepublishLearnerWordsCommand.DefaultBatchSize)
    : ICommand<RepublishLearnerWordsResult>
{
    /// <summary>Links per batch (one save each).</summary>
    public const int DefaultBatchSize = 500;
}

/// <summary>Backfill outcome.</summary>
/// <param name="Published">Number of events published.</param>
public sealed record RepublishLearnerWordsResult(int Published);
