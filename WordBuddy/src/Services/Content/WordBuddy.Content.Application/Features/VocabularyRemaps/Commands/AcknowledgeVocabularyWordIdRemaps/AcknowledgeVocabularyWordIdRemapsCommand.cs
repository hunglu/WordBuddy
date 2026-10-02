using WordBuddy.Content.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.VocabularyRemaps.Commands.AcknowledgeVocabularyWordIdRemaps;

/// <summary>Progress has applied the remaps for <paramref name="OldIds"/> (1–500 ids). Unknown or
/// already-acknowledged ids are ignored.</summary>
public sealed record AcknowledgeVocabularyWordIdRemapsCommand(IReadOnlyList<Guid> OldIds) : ICommand;
