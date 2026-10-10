namespace WordBuddy.Progress.Domain;

/// <summary>
/// Checks a learner's raw answer against the stored expected answer. Pure. Choice: the option key
/// must equal the expected key. Typing: trim + case-insensitive equality.
/// </summary>
public static class AnswerChecker
{
    /// <summary>Normalises a word for storing and comparing: trimmed.</summary>
    public static string Normalize(string word) => word.Trim();

    /// <summary>Returns <see langword="true"/> when the answer is right.</summary>
    /// <param name="exerciseType">Exercise that was issued.</param>
    /// <param name="expectedAnswer">Correct option key (choice) or normalised word (Typing).</param>
    /// <param name="optionKey">Picked option key, for choice exercises.</param>
    /// <param name="text">Typed text, for Typing.</param>
    public static bool IsCorrect(ExerciseType exerciseType, string expectedAnswer, string? optionKey, string? text)
    {
        if (exerciseType == ExerciseType.Typing)
        {
            return text is not null
                && string.Equals(
                    Normalize(text).ToUpperInvariant(),
                    Normalize(expectedAnswer).ToUpperInvariant(),
                    StringComparison.Ordinal);
        }

        return optionKey is not null && string.Equals(optionKey, expectedAnswer, StringComparison.Ordinal);
    }
}
