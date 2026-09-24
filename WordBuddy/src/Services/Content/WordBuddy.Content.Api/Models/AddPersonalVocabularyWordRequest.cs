namespace WordBuddy.Content.Api.Models;

/// <summary>Request body for adding a word to the caller's own personal vocabulary list.</summary>
public sealed record AddPersonalVocabularyWordRequest(string Word, string Definition, string? Example);
