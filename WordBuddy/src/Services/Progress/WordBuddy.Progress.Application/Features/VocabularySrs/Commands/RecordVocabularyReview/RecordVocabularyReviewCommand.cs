using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;

/// <summary>The learner's raw answer: an option key for a choice exercise, or typed text for Typing.</summary>
/// <param name="OptionKey">Picked option key (choice exercises).</param>
/// <param name="Text">Typed text (Typing).</param>
public sealed record ReviewAnswer(string? OptionKey, string? Text);

/// <summary>Records one answer to an issued exercise. <paramref name="UserId"/> and <paramref name="AgeGroup"/>
/// come from the JWT. The client sends no correct flag, type, skill, sense or session — the server
/// takes them from the stored exercise and grades the raw answer itself.</summary>
public sealed record RecordVocabularyReviewCommand(
    Guid UserId,
    AgeGroup AgeGroup,
    Guid ExerciseId,
    ReviewAnswer Answer,
    int ClientResponseMs,
    bool HintUsed) : ICommand<VocabularyReviewResultDto>;
