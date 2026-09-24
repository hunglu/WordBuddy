namespace WordBuddy.Progress.Api.Models;

/// <summary>Request body for submitting a recall-check batch.</summary>
public sealed record SubmitVocabularyRecallCheckRequest(IReadOnlyList<VocabularyRecallResultItemRequest> Results);

/// <summary>One word's outcome within a recall-check submission.</summary>
public sealed record VocabularyRecallResultItemRequest(Guid VocabularyWordId, string Word, bool Known);
