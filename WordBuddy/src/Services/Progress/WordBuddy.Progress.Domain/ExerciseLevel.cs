namespace WordBuddy.Progress.Domain;

/// <summary>Exercise level inside a session: recognition first, then recall.</summary>
public enum ExerciseLevel
{
    /// <summary>The learner recognises the word (picture or audio choice).</summary>
    Recognition,

    /// <summary>The learner recalls the word (audio choice or typing).</summary>
    Recall
}
