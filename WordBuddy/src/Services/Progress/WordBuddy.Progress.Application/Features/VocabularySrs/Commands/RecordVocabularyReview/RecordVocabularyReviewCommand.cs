using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;

/// <summary>Records one answer. <paramref name="UserId"/> and <paramref name="AgeGroup"/> come from the
/// JWT. The client sends no rating, status, due flag or attempt number — the server derives them.</summary>
public sealed record RecordVocabularyReviewCommand(
    Guid UserId,
    AgeGroup AgeGroup,
    Guid SessionId,
    Guid SenseId,
    ExerciseType ExerciseType,
    VocabularySkill Skill,
    bool IsCorrect,
    int ResponseMs,
    bool HintUsed) : ICommand<VocabularyReviewResultDto>;
