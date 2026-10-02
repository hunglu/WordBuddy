using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SyncVocabularyWordIdRemaps;

/// <summary>Pulls pending word-id remaps from Content in batches of <paramref name="BatchSize"/>
/// (1–500), applies them to recall stats, and acknowledges each applied batch. Returns the number
/// of remaps applied. Run by the background sync service, not by a learner.</summary>
public sealed record SyncVocabularyWordIdRemapsCommand(int BatchSize = SyncVocabularyWordIdRemapsCommandValidator.MaxBatchSize) : ICommand<int>;
