namespace WordBuddy.Content.Application.DTOs;

/// <summary>A sense as needed by a review exercise. <paramref name="PersonalContext"/> comes only
/// from the caller's own <c>LearnerWord</c> link; <see langword="null"/> otherwise.</summary>
public sealed record SenseReviewDto(
    Guid SenseId,
    string Word,
    string Definition,
    string? Example,
    string? AudioUrl,
    string? ImageUrl,
    string? PersonalContext);
