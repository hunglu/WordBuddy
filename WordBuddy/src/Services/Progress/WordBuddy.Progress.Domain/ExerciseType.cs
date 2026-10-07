namespace WordBuddy.Progress.Domain;

/// <summary>The MVP exercise types. Each has its own grading thresholds.</summary>
public enum ExerciseType
{
    /// <summary>Pick the picture that matches the word.</summary>
    PictureChoice,

    /// <summary>Hear the word, pick the match.</summary>
    ListeningChoice,

    /// <summary>Type the word.</summary>
    Typing
}
