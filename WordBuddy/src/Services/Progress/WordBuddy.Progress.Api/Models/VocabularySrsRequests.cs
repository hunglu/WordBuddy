using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Api.Models;

/// <summary>Body of <c>POST /api/progress/vocabulary/reviews</c>. No rating, status, due flag or
/// attempt number — the server derives them (ADR 0005 §4).</summary>
/// <param name="SessionId">Id from the session endpoint.</param>
/// <param name="SenseId">Answered sense.</param>
/// <param name="ExerciseType">Exercise shown.</param>
/// <param name="Skill">Skill practised.</param>
/// <param name="IsCorrect">Whether the answer was correct.</param>
/// <param name="ResponseMs">Response time in milliseconds (0–600000).</param>
/// <param name="HintUsed">Whether a hint was used.</param>
public sealed record RecordVocabularyReviewRequest(
    Guid SessionId,
    Guid SenseId,
    ExerciseType ExerciseType,
    VocabularySkill Skill,
    bool IsCorrect,
    int ResponseMs,
    bool HintUsed);

/// <summary>Body of <c>PUT /api/progress/vocabulary/settings</c>.</summary>
/// <param name="NewWordsPerDay">Own daily cap 0–50, or <see langword="null"/> for the backlog rule.</param>
public sealed record UpdateVocabularySettingsRequest(int? NewWordsPerDay);

/// <summary>Body of <c>PUT /api/progress/vocabulary/learners/{learnerId}/settings</c> (active supporter only).</summary>
/// <param name="SupporterNewWordCap">Daily cap 0–50 set by the supporter, or <see langword="null"/> to clear it.</param>
public sealed record UpdateLearnerVocabularySettingsRequest(int? SupporterNewWordCap);
