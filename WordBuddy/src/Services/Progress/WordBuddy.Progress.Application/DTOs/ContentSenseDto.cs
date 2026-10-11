namespace WordBuddy.Progress.Application.DTOs;

/// <summary>A sense as read from Content, used to build exercises. Progress owns this shape; it
/// mirrors the fields of Content's review reply that exercises need.</summary>
/// <param name="SenseId">Content's sense id.</param>
/// <param name="Word">The word.</param>
/// <param name="Definition">Meaning shown as prompt or hint.</param>
/// <param name="ImageUrl">Picture URL, if any.</param>
/// <param name="AudioUrl">Pronunciation URL, if any.</param>
/// <param name="PersonalContext">The learner's own note on the word, if any.</param>
public sealed record ContentSenseDto(
    Guid SenseId,
    string Word,
    string Definition,
    string? ImageUrl,
    string? AudioUrl,
    string? PersonalContext);
