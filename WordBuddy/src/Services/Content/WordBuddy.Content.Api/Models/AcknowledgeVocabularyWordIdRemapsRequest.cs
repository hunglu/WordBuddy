namespace WordBuddy.Content.Api.Models;

/// <summary>Body of <c>POST /internal/vocabulary-remaps/acknowledge</c> — the old word ids Progress
/// has applied.</summary>
public sealed record AcknowledgeVocabularyWordIdRemapsRequest(IReadOnlyList<Guid> OldIds);
