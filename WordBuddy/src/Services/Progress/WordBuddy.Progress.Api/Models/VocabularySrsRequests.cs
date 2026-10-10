namespace WordBuddy.Progress.Api.Models;

/// <summary>Body of <c>POST /api/progress/vocabulary/exercises</c>.</summary>
/// <param name="SessionId">Id from the session endpoint.</param>
/// <param name="SenseId">Word to practise, one of the session items.</param>
public sealed record CreateVocabularyExerciseRequest(Guid SessionId, Guid SenseId);

/// <summary>The learner raw answer: an option key (choice exercises) or typed text (Typing).</summary>
/// <param name="OptionKey">Picked option key.</param>
/// <param name="Text">Typed text.</param>
public sealed record ReviewAnswerRequest(string? OptionKey, string? Text);

/// <summary>Body of <c>POST /api/progress/vocabulary/reviews</c>. The server decides correctness, rating,
/// due flag and attempt number (ADR 0005 §4). Unknown JSON fields — such as a forged <c>isCorrect</c> — are ignored.</summary>
/// <param name="ExerciseId">Id from the exercises endpoint.</param>
/// <param name="Answer">The raw answer.</param>
/// <param name="ClientResponseMs">Response time measured by the client (0–600000); the server checks it against its own clock.</param>
/// <param name="HintUsed">Whether a hint was used.</param>
public sealed record RecordVocabularyReviewRequest(
    Guid ExerciseId,
    ReviewAnswerRequest Answer,
    int ClientResponseMs,
    bool HintUsed);

/// <summary>Body of <c>PUT /api/progress/vocabulary/settings</c>.</summary>
/// <param name="NewWordsPerDay">Own daily cap 0–50, or <see langword="null"/> for the backlog rule.</param>
public sealed record UpdateVocabularySettingsRequest(int? NewWordsPerDay);

/// <summary>Body of <c>PUT /api/progress/vocabulary/learners/{learnerId}/settings</c> (active supporter only).</summary>
/// <param name="SupporterNewWordCap">Daily cap 0–50 set by the supporter, or <see langword="null"/> to clear it.</param>
public sealed record UpdateLearnerVocabularySettingsRequest(int? SupporterNewWordCap);
