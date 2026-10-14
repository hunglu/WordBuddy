namespace WordBuddy.Content.Api.Models;

/// <summary>Request body for editing one of the caller's own private words.</summary>
public sealed record UpdatePersonalVocabularyWordRequest(string Word, string Definition, string? Example);
