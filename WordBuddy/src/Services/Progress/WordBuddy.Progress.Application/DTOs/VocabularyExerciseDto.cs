using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.DTOs;

/// <summary>What the learner sees of an exercise. Never carries the answer, the sense ids of the options or the word of a Typing prompt.</summary>
/// <param name="Definition">Meaning of the word (question for Typing, hint for choice).</param>
/// <param name="ImageUrl">Picture, for PictureChoice only.</param>
/// <param name="AudioUrl">Pronunciation, for ListeningChoice only.</param>
/// <param name="PersonalContext">The learner's own note on the word, if any.</param>
/// <param name="HintFirstLetter">First letter of the word, for the Typing hint only.</param>
public sealed record ExercisePromptDto(
    string Definition,
    string? ImageUrl,
    string? AudioUrl,
    string? PersonalContext,
    string? HintFirstLetter);

/// <summary>One answer option of a choice exercise.</summary>
/// <param name="Key">Opaque key to send back as the answer.</param>
/// <param name="Text">The word to show.</param>
public sealed record ExerciseOptionDto(string Key, string Text);

/// <summary>An exercise issued by <c>POST /api/progress/vocabulary/exercises</c>.</summary>
/// <param name="ExerciseId">Id to send back with the answer.</param>
/// <param name="ExerciseType">Exercise to render.</param>
/// <param name="Skill">Skill practised.</param>
/// <param name="Prompt">Prompt data.</param>
/// <param name="Options">Options for choice exercises; empty for Typing.</param>
public sealed record VocabularyExerciseDto(
    Guid ExerciseId,
    ExerciseType ExerciseType,
    VocabularySkill Skill,
    ExercisePromptDto Prompt,
    IReadOnlyList<ExerciseOptionDto> Options);
