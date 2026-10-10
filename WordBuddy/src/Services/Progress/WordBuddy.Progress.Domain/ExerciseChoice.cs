namespace WordBuddy.Progress.Domain;

/// <summary>The exercise type and the skill it practises.</summary>
/// <param name="ExerciseType">Exercise shown.</param>
/// <param name="Skill">Skill practised.</param>
public readonly record struct ExerciseChoice(ExerciseType ExerciseType, VocabularySkill Skill);
